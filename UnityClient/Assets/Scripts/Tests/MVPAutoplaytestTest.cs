using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

// Balance regression: 30 bot campaigns play the loop dive -> town (sell, upgrade) -> pre-dive loadout -> dive.
// M1 threshold (PROJECT_STATUS.md): at least 80% of campaigns dive 3+ times and both sell and upgrade.
// Each campaign starts on day 2 (the prologue takes day 1) and plays through day 15, so it meets the first two rent
// bills; the tuning targets it reports against are in docs/specs/2026-10-05-demo-tuning-targets.md.
public static class MVPAutoplaytestTest {
    private const int CampaignCount = 30;
    private const int MaxExpeditionsPerCampaign = 14;
    public const int CampaignStartDay = 2;
    public const int CampaignLastDay = 15;
    private const int LoopTargetDives = 3;
    private const float LoopTargetCampaignRate = 0.8f;
    private const int BaseSeed = 20260510;
    private const int StabilityBatchCount = 5;
    private const int StabilitySeedStride = 1000;

    public static void Run() {
        Debug.Log("=== Running MVP Autoplaytest ===");

        MVPAutoplaytestReport report = BuildReport(BaseSeed);
        WriteReport(report, "MVPAutoplaytestReport.json");
        LogSummary(report);

        if (report.LoopCampaignRate < LoopTargetCampaignRate) {
            Debug.LogError($"[MVPAutoplaytest] Loop threshold FAILED: {report.LoopCampaignRate:P0} of campaigns dived {LoopTargetDives}+ times with a sale and an upgrade (need {LoopTargetCampaignRate:P0}). See Logs/MVPAutoplaytestReport.json.");
        }

        if (report.CalendarMismatches > 0) {
            Debug.LogError($"[MVPAutoplaytest] {report.CalendarMismatches} campaign(s) ended on a day other than 1 + dives + rests.");
        }

        Debug.Log("=== MVP Autoplaytest Finished ===");
    }

    public static void RunStability() {
        Debug.Log("=== Running MVP Autoplaytest Stability Batch ===");

        MVPAutoplaytestStabilityReport stabilityReport = new MVPAutoplaytestStabilityReport {
            BatchCount = StabilityBatchCount,
            CampaignsPerBatch = CampaignCount,
            MaxExpeditionsPerCampaign = MaxExpeditionsPerCampaign,
            InitialBaseSeed = BaseSeed,
            SeedStride = StabilitySeedStride
        };

        for (int batchIndex = 0; batchIndex < StabilityBatchCount; batchIndex++) {
            int batchBaseSeed = BaseSeed + batchIndex * StabilitySeedStride;
            MVPAutoplaytestReport batchReport = BuildReport(batchBaseSeed);
            stabilityReport.Batches.Add(new MVPAutoplaytestBatchSummary(batchIndex + 1, batchReport));
            Debug.Log(
                $"[MVPAutoplaytest][Batch {batchIndex + 1}] BaseSeed={batchBaseSeed}, " +
                $"L1={batchReport.Layer1ClearRate:P0}, L2={batchReport.Layer2EntryRate:P0}, " +
                $"Tier2={batchReport.Tier2CoreRate:P0}, AvgHP={batchReport.AverageFinalHP:0.0}");
        }

        stabilityReport.BuildSummary();
        WriteStabilityReport(stabilityReport);
        LogStabilitySummary(stabilityReport);

        Debug.Log("=== MVP Autoplaytest Stability Batch Finished ===");
    }

    private static MVPAutoplaytestReport BuildReport(int baseSeed) {
        MVPAutoplaytestReport report = new MVPAutoplaytestReport {
            CampaignCount = CampaignCount,
            MaxExpeditionsPerCampaign = MaxExpeditionsPerCampaign,
            BaseSeed = baseSeed
        };

        // Thousands of combat log lines per run fill the Editor log (it reached gigabytes); keep warnings and errors,
        // which still fail the test.
        LogType previousFilter = Debug.unityLogger.filterLogType;
        Debug.unityLogger.filterLogType = LogType.Warning;
        try {
            for (int i = 0; i < CampaignCount; i++) {
                int seed = baseSeed + i;
                GameRandom.Seed(seed);
                MVPAutoplayCampaignReport campaign = new MVPAutoplayCampaign(seed, MaxExpeditionsPerCampaign).Run();
                campaign.ReachedLoopTarget = campaign.Expeditions >= LoopTargetDives
                    && campaign.SoldItems > 0
                    && campaign.ChassisUpgrades + campaign.ProstheticsCrafted > 0;
                report.Campaigns.Add(campaign);
            }
        } finally {
            Debug.unityLogger.filterLogType = previousFilter;
        }

        report.BuildSummary();
        return report;
    }

    private static string GetLogsDir() {
        string logsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        if (!Directory.Exists(logsDir)) {
            Directory.CreateDirectory(logsDir);
        }

        return logsDir;
    }

    private static void WriteReport(MVPAutoplaytestReport report, string fileName) {
        string reportPath = Path.Combine(GetLogsDir(), fileName);
        File.WriteAllText(reportPath, JsonConvert.SerializeObject(report, Formatting.Indented));
        Debug.Log($"[MVPAutoplaytest] Report written: {reportPath}");
    }

    private static void WriteStabilityReport(MVPAutoplaytestStabilityReport report) {
        string reportPath = Path.Combine(GetLogsDir(), "MVPAutoplaytestStabilityReport.json");
        File.WriteAllText(reportPath, JsonConvert.SerializeObject(report, Formatting.Indented));
        Debug.Log($"[MVPAutoplaytest] Stability report written: {reportPath}");
    }

    private static void LogSummary(MVPAutoplaytestReport report) {
        Debug.Log(
            $"[MVPAutoplaytest] Summary: Loop={report.LoopCampaignRate:P0}, AvgDives={report.AverageExpeditions:0.0}, Defeats={report.TotalDefeats}, " +
            $"L1Clear={report.Layer1ClearRate:P0}, L2Entry={report.Layer2EntryRate:P0}, " +
            $"Tier1Core={report.Tier1CoreRate:P0}, Tier2Core={report.Tier2CoreRate:P0}, " +
            $"AvgMoney={report.AverageFinalMoney:0.0}, AvgHP={report.AverageFinalHP:0.0}, AvgSAN={report.AverageFinalSAN:0.0}, " +
            $"BackpackPressure={report.TotalBackpackRejects}, ToxicMoves={report.TotalToxicMoveCosts}, " +
            $"Corrosion={report.TotalCorrosionEvents}, Curses={report.TotalCurseEvents}");

        foreach (string warning in report.Warnings) {
            Debug.LogWarning($"[MVPAutoplaytest] {warning}");
        }
    }

    private static void LogStabilitySummary(MVPAutoplaytestStabilityReport report) {
        Debug.Log(
            $"[MVPAutoplaytest][Stability] Batches={report.BatchCount}, " +
            $"Tier2 Avg/Min/Max={report.AverageTier2CoreRate:P0}/{report.MinTier2CoreRate:P0}/{report.MaxTier2CoreRate:P0}, " +
            $"L1 Avg/Min/Max={report.AverageLayer1ClearRate:P0}/{report.MinLayer1ClearRate:P0}/{report.MaxLayer1ClearRate:P0}, " +
            $"L2 Avg/Min/Max={report.AverageLayer2EntryRate:P0}/{report.MinLayer2EntryRate:P0}/{report.MaxLayer2EntryRate:P0}");
    }
}

public class MVPAutoplaytestBatchSummary {
    public int BatchIndex;
    public int BaseSeed;
    public int CampaignCount;
    public float Layer1ClearRate;
    public float Layer2EntryRate;
    public float Tier1CoreRate;
    public float Tier2CoreRate;
    public float AverageFinalMoney;
    public float AverageFinalHP;
    public float AverageFinalSAN;
    public int TotalBackpackRejects;
    public int TotalBackpackDiscards;
    public int TotalToxicMoveCosts;
    public int TotalCorrosionEvents;
    public int TotalCurseEvents;
    public int TotalChassisUpgrades;
    public int TotalProstheticsCrafted;
    public int StartLayer2Expeditions;

    public MVPAutoplaytestBatchSummary(int batchIndex, MVPAutoplaytestReport report) {
        BatchIndex = batchIndex;
        BaseSeed = report.BaseSeed;
        CampaignCount = report.CampaignCount;
        Layer1ClearRate = report.Layer1ClearRate;
        Layer2EntryRate = report.Layer2EntryRate;
        Tier1CoreRate = report.Tier1CoreRate;
        Tier2CoreRate = report.Tier2CoreRate;
        AverageFinalMoney = report.AverageFinalMoney;
        AverageFinalHP = report.AverageFinalHP;
        AverageFinalSAN = report.AverageFinalSAN;
        TotalBackpackRejects = report.TotalBackpackRejects;
        TotalBackpackDiscards = report.TotalBackpackDiscards;
        TotalToxicMoveCosts = report.TotalToxicMoveCosts;
        TotalCorrosionEvents = report.TotalCorrosionEvents;
        TotalCurseEvents = report.TotalCurseEvents;
        TotalChassisUpgrades = report.TotalChassisUpgrades;
        TotalProstheticsCrafted = report.TotalProstheticsCrafted;
        StartLayer2Expeditions = report.Campaigns
            .SelectMany(campaign => campaign.ExpeditionReports)
            .Count(expedition => expedition.StartLayerID == 2);
    }
}

public class MVPAutoplaytestStabilityReport {
    public int BatchCount;
    public int CampaignsPerBatch;
    public int MaxExpeditionsPerCampaign;
    public int InitialBaseSeed;
    public int SeedStride;
    public List<MVPAutoplaytestBatchSummary> Batches = new List<MVPAutoplaytestBatchSummary>();

    public float AverageLayer1ClearRate;
    public float MinLayer1ClearRate;
    public float MaxLayer1ClearRate;
    public float AverageLayer2EntryRate;
    public float MinLayer2EntryRate;
    public float MaxLayer2EntryRate;
    public float AverageTier2CoreRate;
    public float MinTier2CoreRate;
    public float MaxTier2CoreRate;
    public float AverageFinalHP;
    public float AverageFinalSAN;
    public int TotalBackpackRejects;
    public int TotalBackpackDiscards;
    public int TotalToxicMoveCosts;
    public int TotalCorrosionEvents;
    public int TotalCurseEvents;
    public int TotalChassisUpgrades;
    public int TotalProstheticsCrafted;
    public int TotalStartLayer2Expeditions;

    public void BuildSummary() {
        if (Batches == null || Batches.Count == 0) {
            return;
        }

        AverageLayer1ClearRate = Batches.Average(batch => batch.Layer1ClearRate);
        MinLayer1ClearRate = Batches.Min(batch => batch.Layer1ClearRate);
        MaxLayer1ClearRate = Batches.Max(batch => batch.Layer1ClearRate);
        AverageLayer2EntryRate = Batches.Average(batch => batch.Layer2EntryRate);
        MinLayer2EntryRate = Batches.Min(batch => batch.Layer2EntryRate);
        MaxLayer2EntryRate = Batches.Max(batch => batch.Layer2EntryRate);
        AverageTier2CoreRate = Batches.Average(batch => batch.Tier2CoreRate);
        MinTier2CoreRate = Batches.Min(batch => batch.Tier2CoreRate);
        MaxTier2CoreRate = Batches.Max(batch => batch.Tier2CoreRate);
        AverageFinalHP = Batches.Average(batch => batch.AverageFinalHP);
        AverageFinalSAN = Batches.Average(batch => batch.AverageFinalSAN);
        TotalBackpackRejects = Batches.Sum(batch => batch.TotalBackpackRejects);
        TotalBackpackDiscards = Batches.Sum(batch => batch.TotalBackpackDiscards);
        TotalToxicMoveCosts = Batches.Sum(batch => batch.TotalToxicMoveCosts);
        TotalCorrosionEvents = Batches.Sum(batch => batch.TotalCorrosionEvents);
        TotalCurseEvents = Batches.Sum(batch => batch.TotalCurseEvents);
        TotalChassisUpgrades = Batches.Sum(batch => batch.TotalChassisUpgrades);
        TotalProstheticsCrafted = Batches.Sum(batch => batch.TotalProstheticsCrafted);
        TotalStartLayer2Expeditions = Batches.Sum(batch => batch.StartLayer2Expeditions);
    }
}

public class MVPAutoplaytestReport {
    public int CampaignCount;
    public int MaxExpeditionsPerCampaign;
    public int BaseSeed;
    public List<MVPAutoplayCampaignReport> Campaigns = new List<MVPAutoplayCampaignReport>();
    public List<string> Warnings = new List<string>();

    public float LoopCampaignRate;
    public float AverageExpeditions;
    public int TotalDefeats;
    public int CalendarMismatches;
    public float Layer1ClearRate;
    public float Layer2EntryRate;
    public float Tier1CoreRate;
    public float Tier2CoreRate;
    public float AverageFinalMoney;
    public float AverageFinalHP;
    public float AverageFinalSAN;
    public int TotalBackpackRejects;
    public int TotalBackpackDiscards;
    public int TotalToxicMoveCosts;
    public int TotalCorrosionEvents;
    public int TotalCurseEvents;
    public int TotalChassisUpgrades;
    public int TotalProstheticsCrafted;
    public int TotalLoadoutMoves;

    public void BuildSummary() {
        if (Campaigns == null || Campaigns.Count == 0) {
            Warnings.Add("No campaigns were executed.");
            return;
        }

        int count = Campaigns.Count;
        LoopCampaignRate = Campaigns.Count(c => c.ReachedLoopTarget) / (float)count;
        AverageExpeditions = (float)Campaigns.Average(c => c.Expeditions);
        TotalDefeats = Campaigns.Sum(c => c.Defeats);
        CalendarMismatches = Campaigns.Count(c => c.FinalDay != MVPAutoplaytestTest.CampaignStartDay + c.Expeditions + c.RestDays);
        Layer1ClearRate = Campaigns.Count(c => c.ClearedLayer1) / (float)count;
        Layer2EntryRate = Campaigns.Count(c => c.EnteredLayer2) / (float)count;
        Tier1CoreRate = Campaigns.Count(c => c.ObtainedTier1Core) / (float)count;
        Tier2CoreRate = Campaigns.Count(c => c.ObtainedTier2Core) / (float)count;
        AverageFinalMoney = (float)Campaigns.Average(c => c.FinalMoney);
        AverageFinalHP = (float)Campaigns.Average(c => c.FinalHP);
        AverageFinalSAN = (float)Campaigns.Average(c => c.FinalSAN);
        TotalBackpackRejects = Campaigns.Sum(c => c.BackpackRejects);
        TotalBackpackDiscards = Campaigns.Sum(c => c.BackpackDiscards);
        TotalToxicMoveCosts = Campaigns.Sum(c => c.ToxicMoveCostEvents);
        TotalCorrosionEvents = Campaigns.Sum(c => c.CorrosionEvents);
        TotalCurseEvents = Campaigns.Sum(c => c.CurseEvents);
        TotalChassisUpgrades = Campaigns.Sum(c => c.ChassisUpgrades);
        TotalProstheticsCrafted = Campaigns.Sum(c => c.ProstheticsCrafted);
        TotalLoadoutMoves = Campaigns.Sum(c => c.LoadoutMoves);

        if (Layer1ClearRate < 0.6f) {
            Warnings.Add($"1层完成率偏低：{Layer1ClearRate:P0}，正式开局可能过难。");
        }

        if (Layer1ClearRate > 0.95f && AverageFinalHP > 70f && AverageFinalSAN > 35f) {
            Warnings.Add("1层完成率和剩余资源都偏高，早期压力可能不足。");
        }

        if (Tier1CoreRate + 0.05f >= Layer1ClearRate) {
            // Core delivery is tracking layer completion closely enough; no separate warning needed.
        } else if (Tier1CoreRate < 1f && Layer1ClearRate < 0.8f) {
            Warnings.Add($"一阶核心获得率为 {Tier1CoreRate:P0}，主要受 1 层完成率影响；优先校准 1 层战斗压力。");
        } else if (Tier1CoreRate < 1f) {
            Warnings.Add($"一阶核心获得率不是 100%：{Tier1CoreRate:P0}，请检查 Boss 保底和拾取取舍。");
        }

        if (TotalChassisUpgrades + TotalProstheticsCrafted == 0) {
            Warnings.Add("多轮模拟没有触发任何底盘升级或义体制造，经济/材料回流可能偏慢。");
        }

        if (Layer2EntryRate > 0f && TotalToxicMoveCosts + TotalCorrosionEvents + TotalCurseEvents == 0) {
            Warnings.Add("进入过2层，但没有记录到毒性、腐蚀或诅咒压力。");
        }

        if (TotalBackpackRejects == 0 && TotalBackpackDiscards == 0) {
            Warnings.Add("没有出现背包拾取失败或主动丢弃，空间取舍可能不足。");
        }
    }
}

public class MVPAutoplayCampaignReport {
    public int Seed;
    public bool ClearedLayer1;
    public bool EnteredLayer2;
    public bool ObtainedTier1Core;
    public bool ObtainedTier2Core;
    public bool Defeated;
    public int Expeditions;
    public int Defeats;
    public bool ReachedLoopTarget;
    public int FinalDay;
    public int RestDays;
    public int Maintenances;
    public int RentPaid;
    public int RentOwed;
    public bool Bankrupt;
    public int FinalMoney;
    public int FinalHP;
    public int FinalSAN;
    public int BackpackRejects;
    public int BackpackDiscards;
    public int ToxicMoveCostEvents;
    public int CorrosionEvents;
    public int CurseEvents;
    public int ChassisUpgrades;
    // Items the bot moved so neighbours help each other (LoadoutArranger).
    public int LoadoutMoves;
    public int ProstheticsCrafted;
    public int SoldItems;
    public int SoldValue;
    public int ItemsLostAfterChassisUpgrade;
    public int MaintenanceSpent;
    public int ShopSpent;
    public List<MVPAutoplayRentRecord> RentBills = new List<MVPAutoplayRentRecord>();
    public List<int> MoneyByMorning = new List<int>();
    public List<string> Notes = new List<string>();
    public List<string> DiscardedItems = new List<string>();
    public List<MVPAutoplayExpeditionReport> ExpeditionReports = new List<MVPAutoplayExpeditionReport>();
}

public class MVPAutoplayExpeditionReport {
    public int ExpeditionIndex;
    public int StartLayerID;
    public bool AttemptedLayer2;
    public bool ClearedLayer1;
    public bool EnteredLayer2;
    public bool ReachedTown;
    public bool Defeated;
    public int NodesVisited;
    public int CombatCount;
    public int LootOffered;
    public int LootAccepted;
    public int LootRejected;
    public int HPAfter;
    public int SANAfter;
    public int MoneyAfterWorkshop;
    public string Outcome;
    public string DefeatedBy;
    public int HPBeforeBoss = -1;
    public int NightIncome;
}

public class MVPAutoplayRentRecord {
    public int Day;
    public int Bill;
    public int MoneyBefore;
    public string Status;
}

internal class MVPAutoplayCampaign {
    private readonly int _seed;
    private readonly int _maxExpeditions;
    private readonly MVPAutoplayCampaignReport _report;
    private CoreBackend _core;
    private MVPAutoplayExpeditionReport _currentExpedition;
    private MVPAutoplayExpeditionReport _nightIncomeTarget;
    private bool _settled;
    private bool _settledVictory;

    public MVPAutoplayCampaign(int seed, int maxExpeditions) {
        _seed = seed;
        _maxExpeditions = maxExpeditions;
        _report = new MVPAutoplayCampaignReport {
            Seed = seed
        };
    }

    public MVPAutoplayCampaignReport Run() {
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        _core = new CoreBackend();
        _core.InitAllSystems();
        GameRoot.Core = _core;

        DungeonEventBus.OnCombatLootPrepared += HandleCombatLootPrepared;
        DungeonEventBus.OnDungeonNodeResolutionPrepared += HandleDungeonNodeResolutionPrepared;
        DungeonEventBus.OnNodeEntered += HandleNodeEntered;
        DungeonEventBus.OnDungeonSettled += HandleDungeonSettled;

        _core.CurrentPlayer.CurrentDay = MVPAutoplaytestTest.CampaignStartDay;

        try {
            for (int i = 1; i <= _maxExpeditions && _core.CurrentPlayer.CurrentDay <= MVPAutoplaytestTest.CampaignLastDay; i++) {
                _report.MoneyByMorning.Add(_core.CurrentPlayer.Money);
                if (i > 1) {
                    PrepareLoadout();
                }

                if (!HasUsableWeapon()) {
                    _report.Notes.Add("停止模拟：整备后背包里仍没有可用武器。");
                    break;
                }

                if (!EnsureReadyToDive()) {
                    break;
                }

                bool attemptLayer2 = ShouldAttemptLayer2(i);
                MVPAutoplayExpeditionReport expedition = RunExpedition(i, attemptLayer2);
                if (expedition == null) {
                    break;
                }
                _report.ExpeditionReports.Add(expedition);
                _report.Expeditions = i;

                // Defeat costs the run's loot, not the campaign: bound gear comes home and the next day goes on.
                if (expedition.Defeated) {
                    _report.Defeated = true;
                    _report.Defeats++;
                }

                _nightIncomeTarget = expedition;
                RunWorkshopPhase();
                expedition.MoneyAfterWorkshop = _core.CurrentPlayer.Money;
                if (_report.Bankrupt) {
                    _report.Notes.Add("停止模拟：连续严重拖欠房租，工坊破产。");
                    break;
                }
            }
        } finally {
            DungeonEventBus.OnCombatLootPrepared -= HandleCombatLootPrepared;
            DungeonEventBus.OnDungeonNodeResolutionPrepared -= HandleDungeonNodeResolutionPrepared;
            DungeonEventBus.OnNodeEntered -= HandleNodeEntered;
            DungeonEventBus.OnDungeonSettled -= HandleDungeonSettled;
        }

        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        _report.FinalMoney = _core.CurrentPlayer.Money;
        _report.FinalHP = doll.Status.HP_Current;
        _report.FinalSAN = doll.Status.SAN_Current;
        _report.FinalDay = _core.CurrentPlayer.CurrentDay;
        return _report;
    }

    // Try layer 2 once clearly stronger than the starting kit: the power arm plus a bigger chassis or a weapon better
    // than the starter dagger. After losing there, go back to layer 1 for one dive.
    private bool ShouldAttemptLayer2(int expeditionIndex) {
        MVPAutoplayExpeditionReport last = _report.ExpeditionReports.LastOrDefault();
        if (expeditionIndex <= 1 || (last != null && last.EnteredLayer2 && last.Defeated)) {
            return false;
        }

        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        bool hasArm = doll.EquippedProsthetics.Contains("pros_power_arm");
        bool betterWeapon = GetGrid()?.ContainedItems.Any(item => IsWeapon(item) && item.ConfigID != "gear_rusty_dagger") ?? false;
        return hasArm && (_report.ChassisUpgrades > 0 || betterWeapon);
    }

    private MVPAutoplayExpeditionReport RunExpedition(int expeditionIndex, bool attemptLayer2) {
        int startLayerID = attemptLayer2 && _core.Dungeon.CanStartAtLayer(2) ? 2 : 1;
        _currentExpedition = new MVPAutoplayExpeditionReport {
            ExpeditionIndex = expeditionIndex,
            StartLayerID = startLayerID,
            AttemptedLayer2 = attemptLayer2
        };
        _settled = false;
        _settledVictory = false;

        if (!_core.Dungeon.StartRunAtLayer(startLayerID)) {
            _report.Notes.Add($"停止模拟：第 {startLayerID} 层下潜没能开始。");
            return null;
        }

        if (startLayerID == 2) {
            _report.EnteredLayer2 = true;
            _currentExpedition.EnteredLayer2 = true;
        }

        NodeBase node = _core.Dungeon.CurrentLayer.RootNode;
        int guard = 0;

        while (node != null && !_settled && guard++ < 80) {
            _currentExpedition.NodesVisited++;
            _core.Dungeon.MoveToNode(node);

            if (node is CombatNode) {
                _currentExpedition.CombatCount++;
                if (node.NodeID.EndsWith("_boss", StringComparison.Ordinal)) {
                    _currentExpedition.HPBeforeBoss = _core.CurrentPlayer.ActiveDoll.Status.HP_Current;
                }

                RunCombatUntilResolved();
                if (_settled && !_settledVictory) {
                    _currentExpedition.Defeated = true;
                    _currentExpedition.DefeatedBy = $"L{_core.Dungeon.CurrentLayer?.LayerID}:{string.Join("+", ((CombatNode)node).MonsterIDs)}";
                    break;
                }
            } else if (node is SafeRoomNode safeRoomNode) {
                UseConsumablesOutsideCombat();
                safeRoomNode.Rest();
            } else if (node is DungeonOutcomeNode) {
                // Outcome nodes publish their result through the event bus.
                // The autoplay handler confirms that result and lets the dungeon flow return to the map.
            } else if (node is StairsNode stairsNode) {
                if (_core.Dungeon.CurrentLayer.LayerID == 1) {
                    _report.ClearedLayer1 = true;
                    _currentExpedition.ClearedLayer1 = true;
                }

                if (attemptLayer2 && _core.Dungeon.CurrentLayer.LayerID == 1 && stairsNode.CanEnterNextLayer()) {
                    stairsNode.EnterNextLayer();
                    _report.EnteredLayer2 = true;
                    _currentExpedition.EnteredLayer2 = true;
                    node = _core.Dungeon.CurrentLayer.RootNode;
                    continue;
                }

                stairsNode.ReturnToTown();
                _currentExpedition.ReachedTown = _settledVictory;
                break;
            }

            node = ChooseNextNode(node);
        }

        _currentExpedition.Outcome = _currentExpedition.Defeated ? "Defeated" : "Stairs";
        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        _currentExpedition.HPAfter = doll.Status.HP_Current;
        _currentExpedition.SANAfter = doll.Status.SAN_Current;
        if (guard >= 80) {
            _currentExpedition.Defeated = true;
            _report.Notes.Add("单次下潜超过 80 个节点循环保护，自动终止。");
        }

        MVPAutoplayExpeditionReport finished = _currentExpedition;
        _currentExpedition = null;
        return finished;
    }

    // Like a careful player reading the map: skip the bypassable elite, head for a rest or safe room when hurt,
    // otherwise prefer treasure and events over fights. Ties keep the first route.
    private NodeBase ChooseNextNode(NodeBase node) {
        if (node?.NextNodes == null || node.NextNodes.Count == 0) {
            return null;
        }

        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        bool hurt = doll.Status.HP_Current * 100 < doll.Status.HP_Max * 60;
        return node.NextNodes.OrderByDescending(next => ScoreRoute(next, hurt)).First();
    }

    private static int ScoreRoute(NodeBase node, bool hurt) {
        switch (node) {
            case CombatNode combat when combat.MonsterIDs.Any(id => id.StartsWith("elite_", StringComparison.Ordinal)):
                return -50;
            case CombatNode _:
                return hurt ? -10 : 0;
            case SafeRoomNode _:
            case RestStopNode _:
                return hurt ? 30 : 5;
            case HazardNode _:
                return hurt ? -20 : -5;
            case TreasureNode _:
            case EventNode _:
                return 10;
            default:
                return 0;
        }
    }

    private void RunCombatUntilResolved() {
        int guard = 0;
        while (_core.Combat.CurrentState != CombatState.End && guard++ < 60) {
            if (_core.Combat.CurrentState != CombatState.PlayerTurn) {
                break;
            }

            DollFighter player = _core.Combat.PlayerFaction.Fighters[0] as DollFighter;
            if (player == null) {
                break;
            }

            player.DataRef.Status.HP_Current = player.RuntimeHP;
            UseConsumablesInCombat(player);
            AttackWithBestWeapons(player);

            int curseBefore = CountItemsWithTag("Cursed");
            _core.Combat.EndPlayerTurn();
            DetectPressureAfterEnemyTurn(curseBefore);
        }
    }

    private void AttackWithBestWeapons(DollFighter player) {
        int attackGuard = 0;
        ItemEntity reservedShield = PickShieldToReserve(player);
        while (_core.Combat.CurrentState == CombatState.PlayerTurn && attackGuard++ < 8) {
            FighterEntity target = _core.Combat.EnemyFaction.Fighters.Find(f => f.RuntimeHP > 0);
            if (target == null) {
                return;
            }

            ItemEntity weapon = PickBestUsableWeapon(player.CurrentAP);
            if (weapon == null) {
                break;
            }

            if (reservedShield != null && player.CurrentAP - weapon.Combat.APCost < reservedShield.Combat.APCost) {
                break;
            }

            player.Attack(target, weapon);
            if (_core.Combat.EnemyFaction.IsWipedOut()) {
                return;
            }
        }

        if (reservedShield != null && player.CurrentAP >= reservedShield.Combat.APCost) {
            player.Attack(player, reservedShield);
        }
    }

    // Keeps AP for the shield when hurt or when the enemy shows a big hit (the intent numbers the player sees).
    private ItemEntity PickShieldToReserve(DollFighter player) {
        if (player == null) {
            return null;
        }

        IncomingDamagePreview incoming = CombatReadabilityTextService.BuildIncomingDamagePreview(_core.Combat);
        bool bigHit = incoming != null && (incoming.Lethal || incoming.Total >= 20);
        if (!bigHit && player.RuntimeHP > Mathf.CeilToInt(player.RuntimeMaxHP * 0.7f)) {
            return null;
        }

        if (CanLikelyKillThisTurn(player.CurrentAP)) {
            return null;
        }

        BackpackGrid grid = GetGrid();
        return grid?.ContainedItems
            .Where(item => item?.Combat != null
                && item.Combat.TriggerType == TriggerType.Manual.ToString()
                && item.Combat.DamageType == DamageType.Shield.ToString()
                && item.Combat.APCost <= player.CurrentAP)
            .OrderBy(item => item.Combat.APCost)
            .FirstOrDefault();
    }

    private bool CanLikelyKillThisTurn(int currentAP) {
        int enemyHP = _core.Combat.EnemyFaction.Fighters
            .Where(fighter => fighter.RuntimeHP > 0)
            .Sum(fighter => fighter.RuntimeHP);
        if (enemyHP <= 0) {
            return true;
        }

        int remainingAP = currentAP;
        int possibleDamage = 0;
        int guard = 0;
        while (remainingAP > 0 && guard++ < 8) {
            ItemEntity weapon = PickBestUsableWeapon(remainingAP);
            if (weapon == null) {
                break;
            }

            possibleDamage += Mathf.RoundToInt(weapon.Combat.RuntimeDamage);
            if (!CombatItemEffects.CanUse(weapon, out _)) {
                break;
            }
            remainingAP -= weapon.Combat.APCost;
        }

        return possibleDamage >= enemyHP;
    }

    private ItemEntity PickBestUsableWeapon(int currentAP) {
        BackpackGrid grid = GetGrid();
        if (grid == null) {
            return null;
        }

        // Spent items (uses per fight) are skipped; statuses count as the damage they will do.
        return grid.ContainedItems
            .Where(item => IsWeapon(item) && item.Combat.APCost <= currentAP && CombatItemEffects.CanUse(item, out _))
            .OrderByDescending(item => EffectiveDamage(item) / Mathf.Max(1f, item.Combat.APCost))
            .ThenByDescending(EffectiveDamage)
            .FirstOrDefault();
    }

    private void UseConsumablesInCombat(DollFighter player) {
        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        player.DataRef.Status.HP_Current = player.RuntimeHP;

        if (player.RuntimeHP <= Mathf.CeilToInt(player.RuntimeMaxHP * 0.45f)) {
            TryUseFirstConsumable(DamageType.Heal.ToString());
        }

        if (doll.Status.SAN_Current <= Mathf.CeilToInt(doll.Status.SAN_Max * 0.35f)) {
            TryUseFirstConsumable(DamageType.RestoreSAN.ToString());
        }
    }

    private void UseConsumablesOutsideCombat() {
        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        if (doll.Status.HP_Current <= Mathf.CeilToInt(doll.Status.HP_Max * 0.45f)) {
            TryUseFirstConsumable(DamageType.Heal.ToString());
        }

        if (doll.Status.SAN_Current <= Mathf.CeilToInt(doll.Status.SAN_Max * 0.35f)) {
            TryUseFirstConsumable(DamageType.RestoreSAN.ToString());
        }
    }

    private void TryUseFirstConsumable(string damageType) {
        BackpackGrid grid = GetGrid();
        ItemEntity item = grid?.ContainedItems.FirstOrDefault(candidate =>
            candidate?.ItemType == nameof(ItemType.Consumable) &&
            candidate.Combat != null &&
            candidate.Combat.DamageType == damageType);

        if (item == null) {
            return;
        }

        ItemUseService.TryUseItem(item, out _);
    }

    private void HandleCombatLootPrepared(CombatLootPickupResult result) {
        if (result == null) {
            return;
        }

        ILootPickupNode node = _core?.Dungeon?.CurrentLayer?.CurrentNode as ILootPickupNode;
        if (node == null) {
            return;
        }

        _currentExpedition.LootOffered += result.OfferedItems.Count;
        foreach (ItemEntity item in result.OfferedItems.OrderByDescending(ScoreItem)) {
            bool accepted = TryPlaceLootWithPolicy(item);
            if (accepted) {
                _currentExpedition.LootAccepted++;
                if (item.ConfigID == "mat_core_tier1") {
                    _report.ObtainedTier1Core = true;
                }
                if (item.ConfigID == "mat_core_tier2") {
                    _report.ObtainedTier2Core = true;
                }
            } else {
                _currentExpedition.LootRejected++;
                _report.BackpackRejects++;
                _report.DiscardedItems.Add(item.Name);
            }
        }

        node.ConfirmLootCollection();
    }

    private void HandleDungeonNodeResolutionPrepared(DungeonNodeResolutionResult result) {
        DungeonEventBus.PublishNodeSettlementCompleted();
    }

    private void HandleNodeEntered(NodeBase node, int sanCost) {
        int baseCost = 0;
        int layerID = _core?.Dungeon?.CurrentLayer?.LayerID ?? 0;
        if (!(node is SafeRoomNode) && !(node is StairsNode) && ConfigManager.Dungeons.TryGetValue(layerID, out DungeonConfig dungeon)) {
            baseCost = dungeon.SANCostPerNode;
        }

        if (sanCost > baseCost) {
            _report.ToxicMoveCostEvents++;
        }
    }

    private void HandleDungeonSettled(bool isVictory) {
        _settled = true;
        _settledVictory = isVictory;
    }

    private void DetectPressureAfterEnemyTurn(int curseBefore) {
        int curseAfter = CountItemsWithTag("Cursed");
        if (curseAfter > curseBefore) {
            _report.CurseEvents += curseAfter - curseBefore;
        }

        BackpackGrid grid = GetGrid();
        if (grid == null) {
            return;
        }

        foreach (ItemEntity item in grid.ContainedItems) {
            if (IsWeapon(item) && item.Combat.RuntimeDamage < item.Combat.BaseValue) {
                _report.CorrosionEvents++;
                return;
            }
        }
    }

    private bool TryPlaceLootWithPolicy(ItemEntity item) {
        BackpackGrid grid = GetGrid();
        if (grid == null || item == null) {
            return false;
        }

        if (TryPlaceAnyRotation(grid, item)) {
            return true;
        }

        int guard = 0;
        while (guard++ < 8) {
            ItemEntity discard = FindLowestPriorityDiscard(item);
            if (discard == null) {
                return false;
            }

            grid.RemoveItem(discard);
            _report.BackpackDiscards++;
            _report.DiscardedItems.Add(discard.Name);
            GridSolver.RecalculateAllEffects(_core.CurrentPlayer.ActiveDoll);

            if (TryPlaceAnyRotation(grid, item)) {
                return true;
            }
        }

        return false;
    }

    private ItemEntity FindLowestPriorityDiscard(ItemEntity incoming) {
        BackpackGrid grid = GetGrid();
        if (grid == null) {
            return null;
        }

        int incomingScore = ScoreItem(incoming);
        return grid.ContainedItems
            .Where(item => !IsSurvivalProtected(item))
            .OrderBy(ScoreItem)
            .FirstOrDefault(item => ScoreItem(item) < incomingScore);
    }

    private bool TryPlaceAnyRotation(BackpackGrid grid, ItemEntity item) {
        int originalRotation = item.Grid?.Rotation ?? 0;
        int[] rotations = { 0, 90, 180, 270 };

        foreach (int rotation in rotations) {
            if (item.Grid != null) {
                item.Grid.Rotation = rotation;
            }

            if (grid.TryPlaceFirstAvailable(item, out _, out _)) {
                GridSolver.RecalculateAllEffects(_core.CurrentPlayer.ActiveDoll);
                return true;
            }
        }

        if (item.Grid != null) {
            item.Grid.Rotation = originalRotation;
        }

        return false;
    }

    // Night after the dive: spare loot goes into the dump box and the business opens (income, then rent), then the
    // morning's free actions (rent decision, upgrades), resting while hurt.
    private void RunWorkshopPhase() {
        CloseNight();
        SettlePendingRent();
        RestIfHurt();

        // Upgrades come out of what is left after the next rent bill, like a player saving for rent.
        TryCraft("craft_pros_power_arm", 300);
        int beforeCount = GetGrid()?.ContainedItems.Count ?? 0;
        string beforeChassis = _core.CurrentPlayer.ActiveDoll.Chassis?.ChassisID;
        if (CanSpend(1000)) {
            _core.Workshop.UpgradeDollChassis(_core.CurrentPlayer.ActiveDoll);
        }
        string afterChassis = _core.CurrentPlayer.ActiveDoll.Chassis?.ChassisID;

        if (!string.Equals(beforeChassis, afterChassis, StringComparison.Ordinal)) {
            _report.ChassisUpgrades++;
            int afterCount = GetGrid()?.ContainedItems.Count ?? 0;
            int lost = Mathf.Max(0, beforeCount - afterCount);
            _report.ItemsLostAfterChassisUpgrade += lost;
            if (lost > 0) {
                _report.Notes.Add($"底盘升级后背包物品减少 {lost} 件，需确认升级是否应保留非材料物品。");
            }
        }

        TryCraft("craft_pros_cooling_system", 1000);
    }

    private bool CanSpend(int cost) {
        PlayerProfile player = _core.CurrentPlayer;
        int reserve = TownEconomyService.CalculateMonthlyBill(player, TownEconomyService.GetConfig());
        return player.Money - cost >= reserve;
    }

    // Wear and corruption block the dive at 90 (M2 batch 3). Like a careful player, the bot maintains once either is
    // at 50 or more, then rests (strain drops a little each rest) if the dive is still blocked.
    private bool EnsureReadyToDive() {
        PlayerProfile player = _core.CurrentPlayer;
        MaintainIfWorn();
        DiveReadinessResult ready = DiveReadinessService.Evaluate(player, 1, false);
        int guard = 0;
        while (!ready.CanDive && guard++ < 3 && !_report.Bankrupt) {
            if (DayCycleService.Rest(player, out string reason) == null) {
                _report.Notes.Add("休息失败：" + reason);
                break;
            }

            _report.RestDays++;
            CloseNight();
            SettlePendingRent();
            MaintainIfWorn();
            ready = DiveReadinessService.Evaluate(player, 1, false);
        }

        if (!ready.CanDive) {
            _report.Notes.Add("停止模拟：无法下潜。" + ready.BuildSummary());
        }

        return ready.CanDive;
    }

    private void MaintainIfWorn() {
        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        int guard = 0;
        while (doll.Status.WearAndTear >= 50f && guard++ < 3 && TryMaintain("maint_basic_patch", "maint_quick_repair")) {
        }

        guard = 0;
        while (doll.Status.Corruption >= 50f && guard++ < 3 && TryMaintain("maint_purification_flush", "maint_quick_cleanse")) {
        }
    }

    private bool TryMaintain(params string[] maintenanceIDs) {
        PlayerProfile player = _core.CurrentPlayer;
        foreach (string id in maintenanceIDs) {
            int moneyBefore = player.Money;
            if (MaintenanceService.CanApply(player, player.ActiveDoll, id, out _) && MaintenanceService.Apply(player, player.ActiveDoll, id).Success) {
                _report.Maintenances++;
                _report.MaintenanceSpent += moneyBefore - player.Money;
                return true;
            }
        }

        return false;
    }

    // HP comes home as it is (GDD_02 §5.1). The bot rests (one day each) until Zero is above half HP, like a careful
    // player would; a rest night can bring rent due, which is settled again.
    private void RestIfHurt() {
        PlayerProfile player = _core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        int guard = 0;
        while (doll != null && doll.Status.HP_Current * 2 < doll.Status.HP_Max && guard++ < 3 && !_report.Bankrupt) {
            if (DayCycleService.Rest(player, out string reason) == null) {
                _report.Notes.Add("休息失败：" + reason);
                break;
            }

            _report.RestDays++;
            CloseNight();
            SettlePendingRent();
        }
    }

    // Weekly rent the night settlement could not cover: sell spare loot, then pay, else owe it.
    private void SettlePendingRent() {
        PlayerProfile player = _core.CurrentPlayer;
        if (player.HasPendingMonthlyRent) {
            MonthlyRentSettlementReport paid = TownEconomyService.ResolveMonthlyRent(player);
            if (paid.Status == MonthlyRentSettlementStatus.Paid) {
                _report.RentPaid++;
            } else {
                TownEconomyService.AcceptPendingRentAsDebt(player);
                _report.RentOwed++;
            }
        }

        _report.Bankrupt = TownEconomyService.IsBankrupt(player);
    }

    // 出发前整备: bring last dive's gear back, then fill free space with stash gear and consumables, best first.
    private void PrepareLoadout() {
        PlayerProfile player = _core.CurrentPlayer;
        PreDiveLoadoutService.RestoreLastLoadout(player);

        List<ItemEntity> candidates = player.StashInventory
            .Where(item => PreDiveLoadoutService.Classify(item) != LoadoutStashGroup.LootAndMaterial)
            .OrderByDescending(ScoreItem)
            .ToList();
        foreach (ItemEntity item in candidates) {
            PreDiveLoadoutService.TryBringFromStash(player, item, out _);
        }

        // Lost every weapon: buy a basic one at the 补给铺 when the money allows (M2 batch 1).
        if (!HasUsableWeapon()) {
            int moneyBefore = player.Money;
            WorkshopShopPurchaseResult bought = WorkshopShopService.TryBuy(player, "gear_rusty_dagger");
            if (bought.Success) {
                _report.ShopSpent += moneyBefore - player.Money;
                _report.Notes.Add("没有武器，在补给铺买了生锈短剑。");
                PreDiveLoadoutService.TryBringFromStash(player, bought.Item, out _);
            }
        }

        // Then arrange: boosts next to weapons, chargers and counterweights beside the right neighbours.
        _report.LoadoutMoves += LoadoutArranger.Arrange(player.ActiveDoll);
    }

    // Loot kept for upgrades: scrap for the two prosthetic recipes, the tier-1 core for the chassis.
    private static readonly Dictionary<string, int> KeepForUpgrades = new Dictionary<string, int> {
        { "loot_gear_scrap", 4 },
        { "mat_core_tier1", 1 }
    };

    // After a dive everything sits in the stash (rule card 02 §5.2), so sell from there. Keeps what upgrades need.
    private void CloseNight() {
        SellSpareLoot();
        int moneyBefore = _core.CurrentPlayer.Money;
        DayEndReport night = DayCycleService.FinishNight(_core.CurrentPlayer);
        if (night == null) {
            return;
        }

        int income = (night.Business?.DumpBoxIncome ?? 0) + (night.Business?.ShowcaseIncome ?? 0);
        if (_nightIncomeTarget != null) {
            _nightIncomeTarget.NightIncome = income;
            _nightIncomeTarget = null;
        }

        if (night.Rent != null) {
            _report.RentBills.Add(new MVPAutoplayRentRecord {
                Day = night.EndedDay,
                Bill = night.Rent.TotalBill,
                MoneyBefore = moneyBefore + income,
                Status = night.Rent.Status.ToString()
            });
        }
    }

    // Puts spare loot into the night dump box (it sells when the business opens).
    private void SellSpareLoot() {
        Dictionary<string, int> kept = new Dictionary<string, int>();
        PlayerProfile player = _core.CurrentPlayer;
        List<ItemEntity> owned = new List<ItemEntity>(player.StashInventory);
        BackpackGrid grid = GetGrid();
        if (grid != null) {
            owned.AddRange(grid.ContainedItems);
        }

        foreach (ItemEntity item in owned) {
            if (!IsSellableLoot(item)) {
                continue;
            }

            if (KeepForUpgrades.TryGetValue(item.ConfigID, out int keepLimit)) {
                kept.TryGetValue(item.ConfigID, out int keptCount);
                if (keptCount < keepLimit) {
                    kept[item.ConfigID] = keptCount + 1;
                    continue;
                }
            }

            int value = item.BaseValue;
            if (NightBusinessService.TryMoveToDumpBox(player, item, out _)) {
                _report.SoldItems++;
                _report.SoldValue += value;
            }
        }
    }

    private void TryCraft(string recipeID, int cost) {
        if (!CanSpend(cost) || !_core.Workshop.CraftAndEquipProsthetic(recipeID, _core.CurrentPlayer.ActiveDoll)) {
            return;
        }

        _report.ProstheticsCrafted++;
    }

    private BackpackGrid GetGrid() {
        return _core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
    }

    private bool HasUsableWeapon() {
        BackpackGrid grid = GetGrid();
        return grid != null && grid.ContainedItems.Any(IsWeapon);
    }

    private int CountItemsWithTag(string tag) {
        BackpackGrid grid = GetGrid();
        if (grid == null) {
            return 0;
        }

        return grid.ContainedItems.Count(item => item?.Tags != null && item.Tags.Contains(tag));
    }

    private bool IsWeapon(ItemEntity item) {
        return item?.Combat != null
            && item.ItemType == nameof(ItemType.Weapon)
            && item.Combat.TriggerType == TriggerType.Manual.ToString()
            && (item.Combat.DamageType == DamageType.Physical.ToString() || item.Combat.DamageType == DamageType.Energy.ToString());
    }

    private bool IsSellableLoot(ItemEntity item) {
        if (item == null || item.ItemType != nameof(ItemType.Loot)) {
            return false;
        }

        return !HasTag(item, "CoreMaterial") && !HasTag(item, "Cursed");
    }

    private bool IsSurvivalProtected(ItemEntity item) {
        if (item == null) {
            return false;
        }

        if (IsWeapon(item)) {
            BackpackGrid grid = GetGrid();
            int weaponCount = grid?.ContainedItems.Count(IsWeapon) ?? 0;
            return weaponCount <= 1;
        }

        return false;
    }

    private bool HasTag(ItemEntity item, string tag) {
        return item?.Tags != null && item.Tags.Exists(value => string.Equals(value, tag, StringComparison.OrdinalIgnoreCase));
    }

    // A strike's worth to the bot: its damage, plus poison as the HP it takes over the next turns and 破甲 as half a
    // strike more; with only a few uses per fight it is worth less as a mainstay.
    private static float EffectiveDamage(ItemEntity item) {
        if (item?.Combat == null) {
            return 0f;
        }

        float damage = item.Combat.RuntimeDamage > 0f ? item.Combat.RuntimeDamage : item.Combat.BaseValue;
        foreach (EffectData effect in item.Combat.Effects ?? new List<EffectData>()) {
            float value = effect?.Params != null && effect.Params.Length > 0 ? effect.Params[0] : 0f;
            if (effect?.EffectID == "ApplyStatus" && effect.Trigger != "TurnStart") {
                if (effect.Status == "Poison") damage += value * 2f;
                if (effect.Status == "Vulnerable") damage += 8f * value;
            }
        }

        return damage;
    }

    private int ScoreItem(ItemEntity item) {
        if (item == null) {
            return 0;
        }

        int score = item.BaseValue;
        if (HasTag(item, "CoreMaterial")) score += 10000;
        if (HasTag(item, "Material")) score += 2500;
        if (HasTag(item, "Toxic") && !IsWeapon(item)) score += 400;
        if (IsWeapon(item)) score += 1200 + Mathf.RoundToInt(EffectiveDamage(item) * 10f);
        if (item.ItemType == nameof(ItemType.Armor)) score += 900;
        if (item.ItemType == nameof(ItemType.Consumable)) score += 500;
        if (HasTag(item, "Cursed")) score -= 5000;
        return score;
    }
}
