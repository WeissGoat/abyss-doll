using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Phase 2 M0 (GDD_02 §5.3): a dive saves on the map and as she steps onto a node; continuing rebuilds the
// same map from the seed, marks the walked nodes, and enters the half-done node again. The prologue saves at its
// stages; town autosaves wait during a run and during the prologue.
public static class SaveRunTest {
    public static void Run() {
        Debug.Log("=== Running Save Run Test ===");
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        try {
            TestDiveCheckpoint(Check);
            TestPrologueRun(Check);
            TestSaveGates(Check);
        } finally {
            GameRoot.Core = null;
            PrologueShallowRun.Reset();
        }

        if (failures.Count == 0) {
            Debug.Log("Save Run PASSED.");
        } else {
            Debug.LogError("Save Run FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Save Run Test Finished ===");
    }

    private static CoreBackend Init() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();
        PrologueShallowRun.Reset();
        return core;
    }

    // What the game writes at each checkpoint, without touching the disk.
    private static List<string> RecordCheckpoints(CoreBackend core) {
        List<string> saves = new List<string>();
        DungeonEventBus.OnRunCheckpoint += () => saves.Add(SaveService.Serialize(core.CurrentPlayer, core.Dungeon.CaptureRun()));
        return saves;
    }

    private static CoreBackend Continue(string json) {
        SaveFile file = SaveService.Deserialize(json);
        CoreBackend fresh = Init();
        SaveService.Restore(fresh, file);
        return fresh;
    }

    private static void TestDiveCheckpoint(System.Action<bool, string> check) {
        CoreBackend core = Init();
        List<string> saves = RecordCheckpoints(core);
        check(core.Dungeon.StartRunAtLayer(1), "the dive did not start");
        check(saves.Count == 1, $"starting a dive wrote {saves.Count} checkpoint(s), expected 1 on the map");
        DungeonLayer layer = core.Dungeon.CurrentLayer;
        NodeBase entry = layer.EntryNodes[0];
        core.Dungeon.MoveToNode(entry);
        check(saves.Count == 2, "stepping onto a node wrote no checkpoint");
        string mid = saves[saves.Count - 1];

        CoreBackend resumed = Continue(mid);
        DungeonLayer rebuilt = resumed.Dungeon.CurrentLayer;
        check(resumed.Dungeon.IsRunInProgress && rebuilt != null && rebuilt.RunSeed == layer.RunSeed, "the saved dive did not come back on the same seed");
        check(rebuilt != null && SameMap(layer, rebuilt), "the rebuilt map differs from the one she was on");
        check(rebuilt != null && rebuilt.CurrentNode == null && !resumed.Dungeon.FindNode(entry.NodeID).IsVisited, "the node she was stepping onto already counts as walked");
        check(DiveLogService.Current != null && DiveLogService.Current.LayerID == 1, "the dive's journal page was lost");

        List<NodeBase> entered = new List<NodeBase>();
        DungeonEventBus.OnNodeEntered += (node, cost) => entered.Add(node);
        resumed.Dungeon.ResumeRestoredRun();
        check(entered.Count == 1 && entered[0].NodeID == entry.NodeID && rebuilt.CurrentNode == entered[0], "continuing did not enter the half-done node again");
    }

    // The prologue's voice, then the map: continuing keeps what she heard, and the route is the fixed one.
    private static void TestPrologueRun(System.Action<bool, string> check) {
        CoreBackend core = Init();
        PrologueProgress.Reach(core.CurrentPlayer, PrologueProgress.HalfOpen);
        List<string> saves = RecordCheckpoints(core);
        check(core.Dungeon.StartPrologueShallowRun(), "the prologue run did not start");
        PrologueVoiceNode voice = core.Dungeon.CurrentLayer.RootNode as PrologueVoiceNode;
        core.Dungeon.MoveToNode(voice);
        voice.Choose(true);
        string atMap = saves[saves.Count - 1];

        CoreBackend resumed = Continue(atMap);
        check(resumed.Dungeon.IsPrologueRun && resumed.Dungeon.CurrentLayer.CurrentNode?.NodeID == voice.NodeID, "the prologue dive did not come back after the voice");
        check(PrologueShallowRun.Current != null && PrologueShallowRun.Current.ListenedToVoice, "the prologue forgot that she listened");
        check(PrologueShallowRun.BringBackOptions().Contains(PrologueShallowRun.MemoryNoiseID), "the memory shard is no longer offered");

        // Bring the dust home: the return home is the next save point and plays again on continue.
        resumed.Dungeon.ResumeRestoredRun();
        CombatNode bug = resumed.Dungeon.CurrentLayer.CurrentNode.NextNodes[0] as CombatNode;
        resumed.Dungeon.MoveToNode(bug);
        bug.ResolveAfterVictory();
        PrologueBringBackNode cart = bug.NextNodes[0] as PrologueBringBackNode;
        resumed.Dungeon.MoveToNode(cart);
        cart.Choose(PrologueShallowRun.CoreDustID);
        check(PrologueProgress.IsAt(resumed.CurrentPlayer, PrologueProgress.ReturnHome), "the prologue dive did not lead to the return home");
        string home = SaveService.Serialize(resumed.CurrentPlayer, resumed.Dungeon.CaptureRun());

        CoreBackend atHome = Continue(home);
        check(atHome.Dungeon.CurrentLayer == null && PrologueReturnHome.IsPending(atHome.CurrentPlayer), "continuing at the return home does not play it");
        check(PrologueReturnHome.BuildRecap(atHome.CurrentPlayer).Contains("稳定核心碎屑"), "the return home forgot what she brought");
        PrologueReturnHome.Finish(atHome.CurrentPlayer);
        check(!PrologueProgress.InPrologue(atHome.CurrentPlayer), "finishing the return home did not end the prologue");
    }

    // On disk: town autosaves wait during a dive and during the prologue; checkpoints write (but not the opening).
    private static void TestSaveGates(System.Action<bool, string> check) {
        string folder = Path.Combine(Path.GetTempPath(), "p3_save_run_test_" + System.Guid.NewGuid().ToString("N"));
        bool sessionWas = SaveService.SessionStarted;
        SaveService.DirectoryOverride = folder;
        SaveService.Enabled = true;
        try {
            CoreBackend core = Init();
            PlayerProfile player = core.CurrentPlayer;
            SaveService.BeginSession();
            PrologueProgress.Reach(player, PrologueProgress.Opening);
            check(!SaveService.AutoSave(player) && !SaveService.SaveCheckpoint(player) && !File.Exists(SaveService.SavePath), "the prologue opening was saved");
            PrologueProgress.Reach(player, PrologueProgress.HalfOpen);
            check(!SaveService.AutoSave(player), "a town autosave wrote during the prologue");
            check(SaveService.SaveCheckpoint(player) && SaveService.ReadSave(out _)?.Player.PrologueStage == PrologueProgress.HalfOpen, "the half-open workshop was not saved as a prologue stage");

            PrologueProgress.Finish(player);
            core.Dungeon.StartRunAtLayer(1);
            check(!SaveService.AutoSave(player), "a town autosave wrote during a dive");
            check(SaveService.SaveCheckpoint(player) && SaveService.ReadSave(out _)?.Run != null, "a dive checkpoint did not save the run");
        } finally {
            SaveService.EndSession();
            if (sessionWas) {
                SaveService.BeginSession();
            }

            SaveService.ClearEnabledOverride();
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(folder)) {
                Directory.Delete(folder, true);
            }
        }
    }

    private static bool SameMap(DungeonLayer a, DungeonLayer b) {
        if (a.NodeRows.Count != b.NodeRows.Count) {
            return false;
        }

        for (int row = 0; row < a.NodeRows.Count; row++) {
            if (a.NodeRows[row].Count != b.NodeRows[row].Count) {
                return false;
            }

            for (int column = 0; column < a.NodeRows[row].Count; column++) {
                NodeBase x = a.NodeRows[row][column];
                NodeBase y = b.NodeRows[row][column];
                if (x.NodeID != y.NodeID || x.GetType() != y.GetType() || x.RewardID != y.RewardID || x.NextNodes.Count != y.NextNodes.Count) {
                    return false;
                }
            }
        }

        return true;
    }
}
