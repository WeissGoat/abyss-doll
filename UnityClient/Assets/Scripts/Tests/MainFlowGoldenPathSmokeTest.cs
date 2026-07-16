using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class MainFlowGoldenPathSmokeTest {
    private static readonly List<string> StepSummaries = new List<string>();
    private static CombatLootPickupResult _lastLootPickup;
    private static CombatLootCollectionResult _lastLootCollection;
    private static DungeonSettlementResult _lastSettlement;
    private static ItemEntity _acceptedCombatLoot;

    public static void Run() {
        Debug.Log("=== Running Main Flow Golden Path Smoke Test ===");

        StepSummaries.Clear();
        ResetCaptures();

        bool passed = false;
        string failureReason = string.Empty;

        try {
            passed = RunGoldenPath(out failureReason);
        } catch (Exception ex) {
            failureReason = $"{ex.Message}\n{ex.StackTrace}";
        } finally {
            DungeonEventBus.OnCombatLootPrepared -= CaptureLootPrepared;
            DungeonEventBus.OnCombatLootCollected -= CaptureLootCollected;
            DungeonEventBus.OnDungeonSettlementPrepared -= CaptureSettlementPrepared;
        }

        string summary = BuildSummary();
        if (passed) {
            Debug.Log($"Main Flow Golden Path PASSED.\n{summary}");
        } else {
            Debug.LogError($"Main Flow Golden Path FAILED. {failureReason}\n{summary}");
        }

        Debug.Log("=== Main Flow Golden Path Smoke Test Finished ===");
    }

    private static bool RunGoldenPath(out string failureReason) {
        failureReason = string.Empty;

        CoreBackend core = CreateCoreWithEmptyBackpack();
        DungeonEventBus.OnCombatLootPrepared += CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected += CaptureLootCollected;
        DungeonEventBus.OnDungeonSettlementPrepared += CaptureSettlementPrepared;

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        Step("town_prepare", $"Money={player.Money}, HighestUnlocked={player.HighestUnlockedDungeonLayer}, Backpack={grid?.ContainedItems.Count ?? -1}");

        DiveReadinessResult beforeDive = DiveReadinessService.Evaluate(player, 1, false);
        if (!beforeDive.CanDive) {
            failureReason = $"Dive readiness blocked before run: {beforeDive.BuildSummary()}";
            return false;
        }

        bool started = core.Dungeon.StartRunAtLayer(1);
        if (!started || core.Dungeon.CurrentLayer == null || core.Dungeon.CurrentLayer.LayerID != 1) {
            failureReason = $"StartRunAtLayer(1) failed. Started={started}, Layer={core.Dungeon.CurrentLayer?.LayerID ?? -1}";
            return false;
        }

        Step("dive_to_layer_1", $"Layer={core.Dungeon.CurrentLayer.LayerID}, Entries={core.Dungeon.CurrentLayer.EntryNodes.Count}");

        if (!TryFindPathToStairs(core.Dungeon.CurrentLayer, out List<NodeBase> path, out string pathReason)) {
            failureReason = pathReason;
            return false;
        }

        Step("map_route_selected", $"Path={string.Join(" -> ", path.Select(node => $"{node.GetType().Name}:{node.NodeID}"))}");

        StairsNode reachedStairs = null;
        foreach (NodeBase node in path) {
            if (!core.Dungeon.CanMoveToNode(node)) {
                failureReason = $"Cannot move to node [{node?.NodeID ?? "null"}] from [{core.Dungeon.CurrentLayer?.CurrentNode?.NodeID ?? "start"}].";
                return false;
            }

            ResetNodeCaptures();
            core.Dungeon.MoveToNode(node);

            if (node is StairsNode stairs) {
                reachedStairs = stairs;
                Step("stairs_reached", $"Node={stairs.NodeID}, HighestUnlocked={player.HighestUnlockedDungeonLayer}, HP={doll.Status.HP_Current}/{doll.Status.HP_Max}, SAN={doll.Status.SAN_Current}/{doll.Status.SAN_Max}");
                break;
            }

            if (!ResolveCurrentNode(node, out string resolveReason)) {
                failureReason = resolveReason;
                return false;
            }
        }

        if (reachedStairs == null) {
            failureReason = "Route ended before reaching stairs.";
            return false;
        }

        if (_acceptedCombatLoot == null) {
            failureReason = "No combat loot item was accepted into backpack during the route.";
            return false;
        }

        bool layerTwoUnlocked = player.HighestUnlockedDungeonLayer >= 2 && core.Dungeon.CanStartAtLayer(2);
        if (!layerTwoUnlocked) {
            failureReason = $"Layer 2 was not unlocked at stairs. HighestUnlocked={player.HighestUnlockedDungeonLayer}, CanStart2={core.Dungeon.CanStartAtLayer(2)}";
            return false;
        }

        int moneyBeforeEvacuation = player.Money;
        reachedStairs.ReturnToTown();
        if (_lastSettlement == null || !_lastSettlement.IsVictory) {
            failureReason = "Evacuation did not prepare a victory settlement result.";
            return false;
        }

        bool lootBroughtOut = player.StashInventory.Contains(_acceptedCombatLoot)
            && _lastSettlement.PickedUpCount >= 1
            && _lastSettlement.BroughtOutCount >= 1;
        if (!lootBroughtOut) {
            failureReason = $"Accepted combat loot was not brought out. Picked={_lastSettlement.PickedUpCount}, Brought={_lastSettlement.BroughtOutCount}, StashContains={player.StashInventory.Contains(_acceptedCombatLoot)}";
            return false;
        }

        Step("evacuate_to_town", $"Picked={_lastSettlement.PickedUpCount}, Brought={_lastSettlement.BroughtOutCount}, Stash={player.StashInventory.Count}");

        EconomySellLine expectedSell = TownEconomyService.CalculateItemSellValue(player, _acceptedCombatLoot, EconomySellChannel.DumpBox);
        EconomySellReport sellReport = TownEconomyService.SellItems(player, new[] { _acceptedCombatLoot }, EconomySellChannel.DumpBox);
        bool sold = sellReport.Success
            && sellReport.SoldItems.Count == 1
            && player.Money == moneyBeforeEvacuation + expectedSell.FinalValue
            && !player.StashInventory.Contains(_acceptedCombatLoot);
        if (!sold) {
            failureReason = $"Town sell failed. Success={sellReport.Success}, Sold={sellReport.SoldItems.Count}, Reason={sellReport.Reason}, Money={player.Money}, Expected={moneyBeforeEvacuation + expectedSell.FinalValue}";
            return false;
        }

        Step("town_sell_loot", $"Item={_acceptedCombatLoot.ConfigID}, Income={expectedSell.FinalValue}, Money={player.Money}");

        player.StashInventory.Add(ConfigManager.CreateItem("loot_gear_scrap"));
        player.Money = Mathf.Max(player.Money, 250);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        doll.Status.HP_Current = Mathf.Max(1, doll.Status.HP_Max - 12);
        DiveReadinessResult blockedByWear = DiveReadinessService.Evaluate(player, 2, false);

        MaintenanceApplicationResult maintenance = MaintenanceService.Apply(player, doll, "maint_basic_patch");
        DiveReadinessResult afterMaintenance = DiveReadinessService.Evaluate(player, 2, false);
        bool maintenanceValid = !blockedByWear.CanDive
            && maintenance.Success
            && maintenance.ConsumedItems.Count == 1
            && doll.Status.HP_Current == doll.Status.HP_Max
            && afterMaintenance.CanDive;
        if (!maintenanceValid) {
            failureReason = $"Maintenance did not restore dive readiness. BlockedBefore={blockedByWear.CanDive}, Success={maintenance.Success}, Reason={maintenance.Reason}, Consumed={maintenance.ConsumedItems.Count}, HP={doll.Status.HP_Current}/{doll.Status.HP_Max}, CanDiveAfter={afterMaintenance.CanDive}";
            return false;
        }

        Step("workshop_maintenance", $"MoneySpent={maintenance.MoneySpent}, Consumed={maintenance.ConsumedItems.Count}, Wear={doll.Status.WearAndTear:0}, CanDive2={afterMaintenance.CanDive}");

        bool restartedLayerTwo = core.Dungeon.StartRunAtLayer(2);
        bool layerTwoEntryClickable = restartedLayerTwo
            && core.Dungeon.CurrentLayer != null
            && core.Dungeon.CurrentLayer.LayerID == 2
            && FirstEntryCanBeClicked(core);
        if (!layerTwoEntryClickable) {
            failureReason = $"Layer 2 restart failed. Started={restartedLayerTwo}, Layer={core.Dungeon.CurrentLayer?.LayerID ?? -1}, EntryClickable={FirstEntryCanBeClicked(core)}";
            return false;
        }

        Step("dive_again_layer_2", $"Layer={core.Dungeon.CurrentLayer.LayerID}, Entries={core.Dungeon.CurrentLayer.EntryNodes.Count}");
        return true;
    }

    private static bool ResolveCurrentNode(NodeBase node, out string reason) {
        reason = string.Empty;

        if (node is CombatNode combatNode) {
            combatNode.ResolveAfterVictory();
            if (_lastLootPickup == null || _lastLootPickup.OfferedItems.Count == 0) {
                Step("combat_node_resolved", $"Node={node.NodeID}, Offered=0, AcceptedNow=False, Reason=no item loot result, AcceptedTotal={_acceptedCombatLoot?.ConfigID ?? "none"}");
                return true;
            }

            bool acceptedNow = TryAcceptFirstCombatLoot(_lastLootPickup, out string acceptReason);
            combatNode.ConfirmLootCollection();
            bool collectionValid = _lastLootCollection != null
                && _lastLootCollection.NodeID == node.NodeID
                && _lastLootCollection.AcceptedItems.Count + _lastLootCollection.DiscardedItems.Count == _lastLootPickup.OfferedItems.Count;
            if (!collectionValid) {
                reason = $"Combat node [{node.NodeID}] loot collection result is invalid.";
                return false;
            }

            Step("combat_node_resolved", $"Node={node.NodeID}, Offered={_lastLootPickup.OfferedItems.Count}, AcceptedNow={acceptedNow}, Reason={acceptReason}, AcceptedTotal={_acceptedCombatLoot?.ConfigID ?? "none"}");
            return true;
        }

        if (node is SafeRoomNode safeRoom) {
            safeRoom.Rest();
            Step("safe_room_resolved", $"Node={node.NodeID}");
            return true;
        }

        if (node is ILootPickupNode lootNode) {
            if (_lastLootPickup != null && _lastLootPickup.NodeID == node.NodeID) {
                lootNode.ConfirmLootCollection();
                Step("noncombat_loot_discarded", $"Node={node.NodeID}, Offered={_lastLootPickup.OfferedItems.Count}");
                return true;
            }
        }

        if (node is DungeonOutcomeNode) {
            DungeonEventBus.PublishNodeSettlementCompleted();
            Step("outcome_node_resolved", $"Node={node.NodeID}, Type={node.GetType().Name}");
            return true;
        }

        DungeonEventBus.PublishNodeSettlementCompleted();
        Step("generic_node_resolved", $"Node={node.NodeID}, Type={node.GetType().Name}");
        return true;
    }

    private static bool TryAcceptFirstCombatLoot(CombatLootPickupResult pickup, out string reason) {
        reason = "already accepted a combat loot item";
        if (_acceptedCombatLoot != null) {
            return false;
        }

        ItemEntity item = pickup?.OfferedItems.FirstOrDefault(candidate => candidate != null);
        if (item == null) {
            reason = "no offered item";
            return false;
        }

        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("MainFlowGoldenPathSmokeTest");
        if (!InventoryInteractionService.TryGetGrid(context, out BackpackGrid grid, out reason)) {
            return false;
        }

        if (!grid.TryFindFirstAvailable(item, out int x, out int y)) {
            reason = "no backpack slot available";
            return false;
        }

        if (!InventoryInteractionService.RequestPlace(item, x, y, context, out reason)) {
            return false;
        }

        _acceptedCombatLoot = item;
        reason = $"placed {item.ConfigID} at ({x},{y})";
        return true;
    }

    private static bool TryFindPathToStairs(DungeonLayer layer, out List<NodeBase> path, out string reason) {
        path = new List<NodeBase>();
        reason = string.Empty;

        if (layer == null) {
            reason = "Current dungeon layer is null.";
            return false;
        }

        Queue<List<NodeBase>> queue = new Queue<List<NodeBase>>();
        HashSet<NodeBase> visited = new HashSet<NodeBase>();
        IEnumerable<NodeBase> starts = layer.EntryNodes != null && layer.EntryNodes.Count > 0
            ? layer.EntryNodes
            : new[] { layer.RootNode };

        foreach (NodeBase start in starts) {
            if (start == null) {
                continue;
            }

            queue.Enqueue(new List<NodeBase> { start });
            visited.Add(start);
        }

        List<NodeBase> firstStairsPath = null;
        while (queue.Count > 0) {
            List<NodeBase> currentPath = queue.Dequeue();
            NodeBase current = currentPath[currentPath.Count - 1];
            if (current is StairsNode) {
                firstStairsPath = firstStairsPath ?? currentPath;
                if (currentPath.Any(node => node is CombatNode)) {
                    path = currentPath;
                    return true;
                }

                continue;
            }

            foreach (NodeBase next in current.NextNodes ?? new List<NodeBase>()) {
                if (next == null || !visited.Add(next)) {
                    continue;
                }

                List<NodeBase> nextPath = new List<NodeBase>(currentPath) { next };
                queue.Enqueue(nextPath);
            }
        }

        if (firstStairsPath != null) {
            reason = $"Route to stairs exists but does not include a combat node. Path={string.Join(" -> ", firstStairsPath.Select(node => $"{node.GetType().Name}:{node.NodeID}"))}";
            return false;
        }

        reason = $"No route to stairs found. Layer={layer.LayerID}, Entries={layer.EntryNodes?.Count ?? 0}, Rows={layer.NodeRows?.Count ?? 0}";
        return false;
    }

    private static bool FirstEntryCanBeClicked(CoreBackend core) {
        DungeonLayer layer = core?.Dungeon?.CurrentLayer;
        NodeBase entry = layer?.EntryNodes != null && layer.EntryNodes.Count > 0 ? layer.EntryNodes[0] : layer?.RootNode;
        return entry != null && core.Dungeon.CanMoveToNode(entry);
    }

    private static CoreBackend CreateCoreWithEmptyBackpack() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        if (core.CurrentPlayer?.ActiveDoll?.Chassis != null) {
            core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
            GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);
        }

        return core;
    }

    private static void ResetCaptures() {
        _lastLootPickup = null;
        _lastLootCollection = null;
        _lastSettlement = null;
        _acceptedCombatLoot = null;
    }

    private static void ResetNodeCaptures() {
        _lastLootPickup = null;
        _lastLootCollection = null;
    }

    private static void CaptureLootPrepared(CombatLootPickupResult result) {
        _lastLootPickup = result;
    }

    private static void CaptureLootCollected(CombatLootCollectionResult result) {
        _lastLootCollection = result;
    }

    private static void CaptureSettlementPrepared(DungeonSettlementResult result) {
        _lastSettlement = result;
    }

    private static void Step(string id, string details) {
        string line = $"[GoldenPath] {StepSummaries.Count + 1:00}.{id}: {details}";
        StepSummaries.Add(line);
        Debug.Log(line);
    }

    private static string BuildSummary() {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[GoldenPath] Summary:");
        foreach (string step in StepSummaries) {
            builder.AppendLine(step);
        }

        return builder.ToString().TrimEnd();
    }
}
