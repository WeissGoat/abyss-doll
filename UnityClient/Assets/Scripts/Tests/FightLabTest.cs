using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// The fight lab (phase 2 M1, docs/specs/2026-11-04-fight-lab.md): every encounter of layers 1-3 is fought many times
// with that layer's reference loadout (配置表(JSON)/Balance/balance_targets.json, FightLab), full HP, no consumables,
// CombatBot playing. It reports how many turns a fight takes and how much HP it costs, against the targets of
// 设计文档/数值/02_战斗数值.md. Out-of-target encounters are reported, not failed: tuning is a design call. The report is
// Logs/FightLabReport.txt (a table) and Logs/FightLabReport.json.
public static class FightLabTest {
    public static void Run() {
        Debug.Log("=== Running Fight Lab ===");
        string text = BuildAndWrite(out int outOfTarget);
        Debug.Log(text);
        Debug.Log($"=== Fight Lab Finished: {outOfTarget} encounter(s) off target ===");
    }

    // For tuning from MCP: returns the table.
    public static string Report() {
        return BuildAndWrite(out _);
    }

    private static string BuildAndWrite(out int outOfTarget) {
        JObject targets = LoadRepoJson("Balance/balance_targets.json");
        int fights = targets["FightLab"]?["FightsPerEncounter"]?.Value<int>() ?? 30;

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;

        List<FightLabRow> rows = new List<FightLabRow>();
        List<string> carried = new List<string>();
        LogType previousFilter = Debug.unityLogger.filterLogType;
        Debug.unityLogger.filterLogType = LogType.Error;
        try {
            foreach (JToken loadout in targets["FightLab"]["Loadouts"]) {
                int layer = loadout["Layer"].Value<int>();
                JObject layerConfig = LoadRepoJson($"Dungeons/layer_{layer}.json");
                foreach (FightLabEncounter encounter in Encounters(layerConfig, layer)) {
                    rows.Add(Measure(core, loadout, layer, encounter, fights, targets));
                }
            }

            // Shallow gear must not carry a deeper layer (GDD_13): the layer above's loadout against this layer.
            List<JToken> loadouts = targets["FightLab"]["Loadouts"].ToList();
            for (int i = 1; i < loadouts.Count; i++) {
                int layer = loadouts[i]["Layer"].Value<int>();
                List<FightLabRow> own = rows.Where(row => row.Layer == layer).ToList();
                List<FightLabRow> shallow = Encounters(LoadRepoJson($"Dungeons/layer_{layer}.json"), layer)
                    .Select(encounter => Measure(core, loadouts[i - 1], layer, encounter, fights, targets)).ToList();
                carried.Add(
                    $"L{layer} with the L{loadouts[i - 1]["Layer"]} loadout: normal fights lose {AverageLoss(shallow, "Normal"):0.0} HP " +
                    $"(own loadout {AverageLoss(own, "Normal"):0.0}), elites {AverageLoss(shallow, "Elite"):0.0} ({AverageLoss(own, "Elite"):0.0}), " +
                    $"boss {AverageLoss(shallow, "Boss"):0.0} ({AverageLoss(own, "Boss"):0.0}), boss won {shallow.Where(row => row.Kind == "Boss").Sum(row => row.Wins) * 100 / fights}%");
            }
        } finally {
            Debug.unityLogger.filterLogType = previousFilter;
            VisualQueue.Clear();
        }

        outOfTarget = rows.Count(row => row.Problems.Count > 0);
        string logs = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        Directory.CreateDirectory(logs);
        string text = Table(rows, fights) + string.Join(Environment.NewLine, carried) + Environment.NewLine;
        File.WriteAllText(Path.Combine(logs, "FightLabReport.txt"), text);
        File.WriteAllText(Path.Combine(logs, "FightLabReport.json"), JsonConvert.SerializeObject(rows, Formatting.Indented));
        return text;
    }

    private static FightLabRow Measure(CoreBackend core, JToken loadout, int layer, FightLabEncounter encounter, int fights, JObject targets) {
        FightLabRow row = new FightLabRow {
            Layer = layer,
            Kind = encounter.Kind,
            EncounterID = encounter.EncounterID,
            Monsters = string.Join("+", encounter.MonsterIDs)
        };

        List<int> losses = new List<int>();
        List<int> turns = new List<int>();
        for (int i = 0; i < fights; i++) {
            GameRandom.Seed(20261104 + layer * 1000 + i);
            DollEntity doll = PrepareDoll(core, loadout);
            core.Combat.StartCombat(encounter.MonsterIDs);
            int turn = 0;
            while (core.Combat.CurrentState == CombatState.PlayerTurn && turn < 40) {
                turn++;
                CombatBot.PlayTurn(core.Combat, core.Combat.PlayerFaction.Fighters[0] as DollFighter);
                if (core.Combat.CurrentState == CombatState.PlayerTurn) {
                    core.Combat.EndPlayerTurn();
                }
            }

            int hpLeft = Mathf.Max(0, core.Combat.PlayerFaction.Fighters[0].RuntimeHP);
            if (core.Combat.EnemyFaction.IsWipedOut() && hpLeft > 0) {
                row.Wins++;
            }

            losses.Add(doll.Status.HP_Max - hpLeft);
            turns.Add(turn);
            VisualQueue.Clear();
        }

        row.Fights = fights;
        row.AverageTurns = (float)turns.Average();
        row.AverageHPLoss = (float)losses.Average();
        row.WorstHPLoss = losses.OrderBy(value => value).ElementAt(Mathf.Min(losses.Count - 1, Mathf.CeilToInt(losses.Count * 0.9f) - 1));

        JToken fight = targets["Fights"][encounter.Kind];
        int[] turnRange = fight["Turns"].ToObject<int[]>();
        int[] lossRange = fight["HPLoss"].ToObject<int[]>();
        float tolerance = targets["Fights"]["Tolerance"].Value<float>();
        row.TargetTurns = $"{turnRange[0]}–{turnRange[1]}";
        row.TargetHPLoss = $"{lossRange[0]}–{lossRange[1]}";
        if (row.AverageTurns < turnRange[0] * (1f - tolerance)) row.Problems.Add("太快");
        if (row.AverageTurns > turnRange[1] * (1f + tolerance)) row.Problems.Add("太慢");
        if (row.AverageHPLoss < lossRange[0] * (1f - tolerance)) row.Problems.Add("太轻");
        if (row.AverageHPLoss > lossRange[1] * (1f + tolerance)) row.Problems.Add("太痛");
        return row;
    }

    // A fresh doll each fight: full HP, the loadout's chassis, only the loadout's items, arranged.
    private static DollEntity PrepareDoll(CoreBackend core, JToken loadout) {
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        string chassisID = loadout["ChassisID"]?.Value<string>();
        if (!string.IsNullOrEmpty(chassisID) && ConfigManager.Chassis.TryGetValue(chassisID, out ChassisComponent chassis)) {
            doll.Chassis = chassis;
        }

        BackpackGrid grid = new BackpackGrid(doll.Chassis);
        doll.RuntimeGrid = grid;
        foreach (string itemID in loadout["Items"].ToObject<List<string>>()) {
            ItemEntity item = ConfigManager.CreateItem(itemID);
            if (item == null || !grid.TryPlaceFirstAvailable(item, out _, out _)) {
                Debug.LogError($"[FightLab] Could not place {itemID} for layer {loadout["Layer"]}.");
            }
        }

        LoadoutArranger.Arrange(doll, 2);
        doll.Status.HP_Current = doll.Status.HP_Max;
        GridSolver.RecalculateAllEffects(doll);
        return doll;
    }

    // The fights the map can roll: the layer's combat nodes (NodePool; EncounterPools is a design note the game does not
    // read) and the boss. Layer 1 adds the prologue's fight.
    private static List<FightLabEncounter> Encounters(JObject layerConfig, int layer) {
        List<FightLabEncounter> encounters = new List<FightLabEncounter>();
        if (layer == 1) {
            CombatNode prologue = PrologueShallowRun.BuildLayer().NodeRows.SelectMany(row => row).OfType<CombatNode>().FirstOrDefault();
            if (prologue != null) {
                encounters.Add(new FightLabEncounter { EncounterID = "prologue:" + prologue.NodeID, MonsterIDs = prologue.MonsterIDs.ToList(), Kind = "Normal" });
            }
        }

        HashSet<string> seen = new HashSet<string>();
        foreach (JToken entry in layerConfig["NodePool"]) {
            List<string> monsters = entry["MonsterIDs"]?.ToObject<List<string>>();
            if (entry["NodeType"]?.Value<string>() != "CombatNode" || monsters == null || monsters.Count == 0 || !seen.Add(string.Join("+", monsters))) {
                continue;
            }

            string kind = monsters.Any(id => id.StartsWith("elite_")) ? "Elite" : "Normal";
            encounters.Add(new FightLabEncounter { EncounterID = string.Join("+", monsters), MonsterIDs = monsters, Kind = kind });
        }

        string boss = layerConfig["BossNode"]?.Value<string>();
        if (!string.IsNullOrEmpty(boss)) {
            encounters.Add(new FightLabEncounter { EncounterID = boss, MonsterIDs = new List<string> { boss }, Kind = "Boss" });
        }

        return encounters;
    }

    private static float AverageLoss(List<FightLabRow> rows, string kind) {
        List<FightLabRow> picked = rows.Where(row => row.Kind == kind).ToList();
        return picked.Count == 0 ? 0f : picked.Average(row => row.AverageHPLoss);
    }

    private static string Table(List<FightLabRow> rows, int fights) {
        StringBuilder text = new StringBuilder();
        text.AppendLine($"Fight lab: {fights} fights per encounter, reference loadout per layer, full HP, no consumables.");
        text.AppendLine("layer kind   encounter                         turns (target)   HP loss avg/p90 (target)   wins  problems");
        foreach (FightLabRow row in rows) {
            text.AppendLine(
                $"L{row.Layer}    {row.Kind,-6} {row.EncounterID,-33} {row.AverageTurns,4:0.0} ({row.TargetTurns,-5})   " +
                $"{row.AverageHPLoss,5:0.0}/{row.WorstHPLoss,-3} ({row.TargetHPLoss,-5})           " +
                $"{row.Wins * 100 / Mathf.Max(1, row.Fights),3}%  {string.Join(" ", row.Problems)}");
        }

        return text.ToString();
    }

    private static JObject LoadRepoJson(string relative) {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../配置表(JSON)", relative));
        return JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
    }
}

public class FightLabEncounter {
    public string EncounterID;
    public string Kind;
    public List<string> MonsterIDs;
}

public class FightLabRow {
    public int Layer;
    public string Kind;
    public string EncounterID;
    public string Monsters;
    public int Fights;
    public int Wins;
    public float AverageTurns;
    public float AverageHPLoss;
    public int WorstHPLoss;
    public string TargetTurns;
    public string TargetHPLoss;
    public List<string> Problems = new List<string>();
}
