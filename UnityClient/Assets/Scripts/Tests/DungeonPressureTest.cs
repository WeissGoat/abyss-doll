using System.Collections.Generic;
using UnityEngine;

// The road tax on deeper layers (GDD_02 §3.5), the lift's parcel (§4.3A), events with choices (§3.3) and the
// growth gates between layers (GDD_13).
public static class DungeonPressureTest {
    private static DungeonSettlementResult _settlement;

    public static void Run() {
        Debug.Log("=== Running Dungeon Pressure Test ===");
        TestSanTaxRules();
        TestStepCostCarriesTheTax();
        TestParcelSendsLootHomeAndSurvivesDefeat();
        TestParcelSurvivesASave();
        TestChoiceEventWaitsForTheChoice();
        TestPermitFee();
        TestChassisGate();
        Debug.Log("=== Dungeon Pressure Test Finished ===");
    }

    private static void TestSanTaxRules() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);
        DungeonSanTaxRules rules = new DungeonSanTaxRules { LowHPExtra = 2, OverloadExtra = 1, CorruptionExtra = 1 };
        DungeonConfig layer = new DungeonConfig { SANTax = rules };

        doll.Status.HP_Current = doll.Status.HP_Max;
        doll.Status.Corruption = 0f;
        bool calm = DungeonSanTaxService.Resolve(layer, doll, grid).Count == 0;

        doll.Status.HP_Current = Mathf.FloorToInt(doll.Status.HP_Max * 0.2f);
        doll.Status.Corruption = 60f;
        FillBackpack(grid, 0.85f);
        List<SanTaxLine> heavy = DungeonSanTaxService.Resolve(layer, doll, grid);
        bool all = DungeonSanTaxService.Total(heavy) == 4 && heavy.Count == 3;

        bool tutorialFree = DungeonSanTaxService.Resolve(ConfigManager.Dungeons[1], doll, grid).Count == 0;
        Check(calm && all && tutorialFree, "San tax rules", $"calm={calm}, heavy={DungeonSanTaxService.Describe(heavy)}, layer1 free={tutorialFree}");
    }

    private static void TestStepCostCarriesTheTax() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        // Layer 3 asks for the level-2 chassis (its growth gate).
        doll.Chassis = ConfigManager.Chassis["chassis_lv2_expanded"];
        ResetBackpack(doll);
        core.CurrentPlayer.HighestUnlockedDungeonLayer = 3;
        core.CurrentPlayer.Money = 5000;
        core.Dungeon.LoadLayer(3);
        NodeBase entry = core.Dungeon.CurrentLayer.EntryNodes[0];
        doll.Status.HP_Current = doll.Status.HP_Max;
        doll.Status.Corruption = 0f;
        int calm = core.Dungeon.PreviewSanCost(entry);
        doll.Status.HP_Current = 1;
        int hurt = core.Dungeon.PreviewSanCost(entry);
        int expected = ConfigManager.Dungeons[3].SANTax.LowHPExtra;
        Check(hurt - calm == expected && expected > 0, "Step cost carries the tax", $"calm {calm}, hurt {hurt}, expected +{expected}");
    }

    private static void TestParcelSendsLootHomeAndSurvivesDefeat() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);
        core.Dungeon.LoadLayer(1);
        ItemEntity small = PickUp(grid, "loot_gear_scrap", 0, 0);
        ItemEntity big = PickUp(grid, "trade_cracked_relic", 1, 0);

        bool refusedOnRoute = !core.Dungeon.TrySendHome(small, out _);
        StandOnLift(core.Dungeon.CurrentLayer);
        int capacity = core.Dungeon.LiftParcelCapacity();
        bool tooBig = DungeonManager.ItemCells(big) > capacity && !core.Dungeon.TrySendHome(big, out _);
        bool sent = core.Dungeon.TrySendHome(small, out string reason);
        bool home = core.CurrentPlayer.StashInventory.Contains(small) && !grid.ContainedItems.Contains(small);

        _settlement = null;
        DungeonEventBus.OnDungeonSettlementPrepared += Capture;
        DungeonEventBus.PublishDungeonDefeated();
        DungeonEventBus.OnDungeonSettlementPrepared -= Capture;
        bool kept = core.CurrentPlayer.StashInventory.Contains(small)
            && _settlement != null && _settlement.BroughtOutIDs.Contains(small.InstanceID) && _settlement.LostCount == 1;

        Check(refusedOnRoute && tooBig && sent && home && kept, "Lift parcel sends loot home and keeps it through a defeat",
            $"refused on route={refusedOnRoute}, too big={tooBig}, sent={sent} ({reason}), home={home}, kept={kept}, lost={_settlement?.LostCount ?? -1}");
    }

    private static void TestParcelSurvivesASave() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);
        core.Dungeon.LoadLayer(1);
        ItemEntity scrap = PickUp(grid, "loot_gear_scrap", 0, 0);
        StandOnLift(core.Dungeon.CurrentLayer);
        core.Dungeon.TrySendHome(scrap, out _);

        DungeonRunSnapshot snapshot = core.Dungeon.CaptureRun();
        bool saved = snapshot.SentHomeIDs.Contains(scrap.InstanceID);
        bool restored = core.Dungeon.RestoreRun(snapshot);
        bool counted = core.Dungeon.LiftParcelUsed() == DungeonManager.ItemCells(scrap);
        Check(saved && restored && counted, "Lift parcel survives a save", $"saved={saved}, restored={restored}, used={core.Dungeon.LiftParcelUsed()}");
    }

    private static void TestChoiceEventWaitsForTheChoice() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        ResetBackpack(doll);
        core.Dungeon.LoadLayer(1);
        EventNode node = new EventNode { NodeID = "pressure_choice" };
        node.Init(new NodePoolEntry { NodeType = "EventNode", Title = "裂纹圣牌", SceneID = "ev_l1_cracked_relic", RewardID = "reward_node_event_layer1_cracked_relic" });

        EventNode asked = null;
        DungeonNodeResolutionResult card = null;
        DungeonEventBus.OnChoiceEventEntered += entered => asked = entered;
        DungeonEventBus.OnDungeonNodeResolutionPrepared += result => card = result;
        doll.Status.SAN_Current = 20;
        node.OnEnterNode();
        bool waited = asked == node && card == null && doll.Status.SAN_Current == 20;

        SceneChoice pray = node.Choices.Find(choice => choice.ID == "pray");
        node.Choose("pray");
        int gained = pray?.Outcome.Count > 0 ? pray.Outcome[0].Amount : 0;
        bool resolved = card != null && card.Summary.StartsWith(pray?.Result ?? "?") && doll.Status.SAN_Current == 20 + gained && gained > 0;
        Check(waited && resolved && node.Choices[node.Choices.Count - 1].ID == "leave", "Choice event waits for the choice",
            $"waited={waited}, resolved={resolved}, SAN={doll.Status.SAN_Current}, card={card?.Summary}");
    }

    private static void TestPermitFee() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        ResetBackpack(player.ActiveDoll);
        player.HighestUnlockedDungeonLayer = 2;
        int fee = ConfigManager.Dungeons[2].DirectStartFee;

        player.Money = fee - 1;
        bool blocked = !DiveReadinessService.Evaluate(player, 2).CanDive;
        player.Money = fee + 100;
        bool started = core.Dungeon.StartRunAtLayer(2);
        Check(fee > 0 && blocked && started && player.Money == 100, "Permit fee for a direct start",
            $"fee {fee}, blocked when short={blocked}, started={started}, money left {player.Money}");
    }

    private static void TestChassisGate() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        ResetBackpack(player.ActiveDoll);
        player.HighestUnlockedDungeonLayer = 3;
        player.Money = 5000;
        int required = ConfigManager.Dungeons[3].RequiredChassisLevel;
        bool small = player.ActiveDoll.Chassis.Level < required;
        DiveReadinessResult readiness = DiveReadinessService.Evaluate(player, 3);
        bool blockedFromTown = readiness.Issues.Exists(issue => issue.Code == DiveReadinessIssueCode.ChassisTooSmall) && !readiness.CanDive;

        core.Dungeon.StartRunAtLayer(2);
        bool stairsShut = !core.Dungeon.CanEnterNextLayer() && !string.IsNullOrEmpty(core.Dungeon.NextLayerBlockReason()) && !core.Dungeon.EnterNextLayer();
        Check(required > 0 && small && blockedFromTown && stairsShut, "Chassis gate on layer 3",
            $"required {required}, chassis {player.ActiveDoll.Chassis.Level}, blocked from town={blockedFromTown}, stairs shut={stairsShut}");
    }

    private static ItemEntity PickUp(BackpackGrid grid, string configID, int x, int y) {
        ItemEntity item = ConfigManager.CreateItem(configID);
        grid.PlaceItem(item, x, y);
        DungeonEventBus.PublishCombatLootCollected(new CombatLootCollectionResult {
            NodeID = "pressure_test",
            AcceptedItems = new List<ItemEntity> { item }
        });
        return item;
    }

    private static void StandOnLift(DungeonLayer layer) {
        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row.Count == 1 && row[0] is SafeRoomNode) {
                row[0].IsVisited = true;
                layer.CurrentNode = row[0];
                return;
            }
        }
    }

    private static void FillBackpack(BackpackGrid grid, float share) {
        int target = Mathf.CeilToInt(grid.UsableCellCount() * share);
        for (int i = 0; i < 64 && grid.FilledCellCount() < target; i++) {
            ItemEntity scrap = ConfigManager.CreateItem("loot_gear_scrap");
            if (!grid.TryPlaceFirstAvailable(scrap, out _, out _)) {
                break;
            }
        }
    }

    private static void Capture(DungeonSettlementResult result) {
        _settlement = result;
    }

    private static void Check(bool passed, string name, string detail) {
        if (passed) {
            Debug.Log($"{name} PASSED.");
        } else {
            Debug.LogError($"{name} FAILED. {detail}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static BackpackGrid ResetBackpack(DollEntity doll) {
        BackpackGrid grid = new BackpackGrid(doll.Chassis);
        doll.RuntimeGrid = grid;
        return grid;
    }
}
