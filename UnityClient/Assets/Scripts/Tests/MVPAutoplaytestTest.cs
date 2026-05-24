using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class MVPAutoplaytestTest {
    private const int CampaignCount = 30;
    private const int MaxExpeditionsPerCampaign = 4;
    private const int BaseSeed = 20260510;
    private const int StabilityBatchCount = 5;
    private const int StabilitySeedStride = 1000;

    public static void Run() {
        Debug.Log("=== Running MVP Autoplaytest ===");

        MVPAutoplaytestReport report = BuildReport(BaseSeed);
        WriteReport(report, "MVPAutoplaytestReport.json");
        LogSummary(report);

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

        for (int i = 0; i < CampaignCount; i++) {
            int seed = baseSeed + i;
            UnityEngine.Random.InitState(seed);
            MVPAutoplayCampaignReport campaign = new MVPAutoplayCampaign(seed, MaxExpeditionsPerCampaign).Run();
            report.Campaigns.Add(campaign);
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
            $"[MVPAutoplaytest] Summary: L1Clear={report.Layer1ClearRate:P0}, L2Entry={report.Layer2EntryRate:P0}, " +
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

    public void BuildSummary() {
        if (Campaigns == null || Campaigns.Count == 0) {
            Warnings.Add("No campaigns were executed.");
            return;
        }

        int count = Campaigns.Count;
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
    public int FinalMoney;
    public int FinalHP;
    public int FinalSAN;
    public int BackpackRejects;
    public int BackpackDiscards;
    public int ToxicMoveCostEvents;
    public int CorrosionEvents;
    public int CurseEvents;
    public int ChassisUpgrades;
    public int ProstheticsCrafted;
    public int SoldItems;
    public int SoldValue;
    public int ItemsLostAfterChassisUpgrade;
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
}

internal class MVPAutoplayCampaign {
    private readonly int _seed;
    private readonly int _maxExpeditions;
    private readonly MVPAutoplayCampaignReport _report;
    private CoreBackend _core;
    private MVPAutoplayExpeditionReport _currentExpedition;
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

        try {
            for (int i = 1; i <= _maxExpeditions; i++) {
                if (!HasUsableWeapon()) {
                    _report.Notes.Add("停止模拟：背包中没有可用武器。");
                    break;
                }

                bool attemptLayer2 = ShouldAttemptLayer2(i);
                MVPAutoplayExpeditionReport expedition = RunExpedition(i, attemptLayer2);
                _report.ExpeditionReports.Add(expedition);
                _report.Expeditions = i;

                if (expedition.Defeated) {
                    _report.Defeated = true;
                    break;
                }

                RunWorkshopPhase();
                expedition.MoneyAfterWorkshop = _core.CurrentPlayer.Money;

                if (_report.ObtainedTier2Core) {
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
        return _report;
    }

    private bool ShouldAttemptLayer2(int expeditionIndex) {
        return expeditionIndex > 1 && (_report.ChassisUpgrades > 0 || _report.ProstheticsCrafted > 0 || _report.ObtainedTier1Core);
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

        _core.Dungeon.StartRunAtLayer(startLayerID);
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
                RunCombatUntilResolved();
                if (_settled && !_settledVictory) {
                    _currentExpedition.Defeated = true;
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

                if (attemptLayer2 && stairsNode.CanEnterNextLayer()) {
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

            node = node?.NextNodes != null && node.NextNodes.Count > 0 ? node.NextNodes[0] : null;
        }

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

    private ItemEntity PickShieldToReserve(DollFighter player) {
        if (player == null || player.RuntimeHP > Mathf.CeilToInt(player.RuntimeMaxHP * 0.7f)) {
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
            remainingAP -= weapon.Combat.APCost;
        }

        return possibleDamage >= enemyHP;
    }

    private ItemEntity PickBestUsableWeapon(int currentAP) {
        BackpackGrid grid = GetGrid();
        if (grid == null) {
            return null;
        }

        return grid.ContainedItems
            .Where(item => IsWeapon(item) && item.Combat.APCost <= currentAP)
            .OrderByDescending(item => item.Combat.RuntimeDamage / Mathf.Max(1f, item.Combat.APCost))
            .ThenByDescending(item => item.Combat.RuntimeDamage)
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

    private void RunWorkshopPhase() {
        RestoreDollAtTown();
        SellPureLoot();

        int beforeCount = GetGrid()?.ContainedItems.Count ?? 0;
        string beforeChassis = _core.CurrentPlayer.ActiveDoll.Chassis?.ChassisID;
        _core.Workshop.UpgradeDollChassis(_core.CurrentPlayer.ActiveDoll);
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

        TryCraft("craft_pros_power_arm");
        TryCraft("craft_pros_cooling_system");
    }

    private void RestoreDollAtTown() {
        DollEntity doll = _core.CurrentPlayer.ActiveDoll;
        if (doll == null) {
            return;
        }

        doll.Status.HP_Current = doll.Status.HP_Max;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
    }

    private void SellPureLoot() {
        BackpackGrid grid = GetGrid();
        if (grid == null) {
            return;
        }

        foreach (ItemEntity item in grid.ContainedItems.ToList()) {
            if (!IsPureSellLoot(item)) {
                continue;
            }

            int value = item.BaseValue;
            if (_core.Workshop.SellItem(item, _core.CurrentPlayer)) {
                _report.SoldItems++;
                _report.SoldValue += value;
            }
        }
    }

    private void TryCraft(string recipeID) {
        if (!_core.Workshop.CraftAndEquipProsthetic(recipeID, _core.CurrentPlayer.ActiveDoll)) {
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

    private bool IsPureSellLoot(ItemEntity item) {
        if (item == null || item.ItemType != nameof(ItemType.Loot)) {
            return false;
        }

        return !HasTag(item, "Material") && !HasTag(item, "CoreMaterial") && !HasTag(item, "Cursed");
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

    private int ScoreItem(ItemEntity item) {
        if (item == null) {
            return 0;
        }

        int score = item.BaseValue;
        if (HasTag(item, "CoreMaterial")) score += 10000;
        if (HasTag(item, "Material")) score += 2500;
        if (HasTag(item, "Toxic")) score += 400;
        if (IsWeapon(item)) score += 1200 + Mathf.RoundToInt(item.Combat.BaseValue * 10f);
        if (item.ItemType == nameof(ItemType.Armor)) score += 900;
        if (item.ItemType == nameof(ItemType.Consumable)) score += 500;
        if (HasTag(item, "Cursed")) score -= 5000;
        return score;
    }
}
