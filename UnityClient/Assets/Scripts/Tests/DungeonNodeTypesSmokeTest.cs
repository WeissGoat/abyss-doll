using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DungeonNodeTypesSmokeTest {
    private static CombatLootPickupResult _lastLootResult;
    private static CombatLootCollectionResult _lastCollectionResult;
    private static DungeonNodeResolutionResult _lastNodeResult;
    private static int _nodeSettlementCompletedCount;
    private static int _nodeResolutionFinishedCount;

    public static void Run() {
        Debug.Log("=== Running Dungeon Node Types Smoke Test ===");

        TestConfiguredNodeTypesAreRegistered();
        TestTreasureNodeUsesRewardPickupFlow();
        TestOutcomeNodePublishesResultAndAppliesEffects();
        TestCombatMapNodeLootReturnsToMap();
        TestTreasureMapNodeRewardReturnsToMap();
        TestOutcomeMapNodeResultReturnsToMap();
        TestLayerConfigsContainFormalNodeVariety();

        Debug.Log("=== Dungeon Node Types Smoke Test Finished ===");
    }

    private static void TestConfiguredNodeTypesAreRegistered() {
        CoreBackend core = CreateCore();
        bool registered = NodeFactory.IsNodeTypeRegistered("TreasureNode")
            && NodeFactory.IsNodeTypeRegistered("EventNode")
            && NodeFactory.IsNodeTypeRegistered("RestStopNode")
            && NodeFactory.IsNodeTypeRegistered("HazardNode");

        bool layerContainsNewTypes = false;
        foreach (DungeonConfig dungeon in ConfigManager.Dungeons.Values) {
            if (dungeon?.NodePool == null) {
                continue;
            }

            layerContainsNewTypes |= dungeon.NodePool.Any(entry =>
                entry != null
                && (entry.NodeType == "TreasureNode"
                    || entry.NodeType == "EventNode"
                    || entry.NodeType == "RestStopNode"
                    || entry.NodeType == "HazardNode"));
        }

        if (core != null && registered && layerContainsNewTypes) {
            Debug.Log("Dungeon Node Type Registration PASSED.");
        } else {
            Debug.LogError($"Dungeon Node Type Registration FAILED. Registered={registered}, ConfigContainsNewTypes={layerContainsNewTypes}");
        }
    }

    private static void TestTreasureNodeUsesRewardPickupFlow() {
        CoreBackend core = CreateCore();
        TreasureNode node = new TreasureNode {
            NodeID = "treasure_smoke"
        };
        node.Init(new NodePoolEntry {
            NodeType = "TreasureNode",
            RewardID = "reward_node_treasure_layer1",
            Title = "Smoke Treasure"
        });

        _lastLootResult = null;
        _lastCollectionResult = null;
        _nodeSettlementCompletedCount = 0;
        DungeonEventBus.OnCombatLootPrepared += CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected += CaptureLootCollected;
        DungeonEventBus.OnNodeSettlementCompleted += CaptureSettlementCompleted;

        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = node,
            CurrentNode = node
        };

        node.OnEnterNode();
        bool lootPrepared = _lastLootResult != null
            && _lastLootResult.NodeID == node.NodeID
            && _lastLootResult.OfferedItems.Count > 0;
        bool offeredItemsMarkedInbox = _lastLootResult != null
            && _lastLootResult.OfferedItems.All(item =>
                item != null
                && item.ContainerType == ItemContainerType.Inbox
                && item.OwnerScope == ItemOwnerScope.Run);
        node.ConfirmLootCollection();
        bool collectionValid = _lastCollectionResult != null
            && _lastCollectionResult.NodeID == node.NodeID
            && _lastLootResult != null
            && _lastCollectionResult.DiscardedItems.Count == _lastLootResult.OfferedItems.Count
            && _lastCollectionResult.DiscardedItems.All(item =>
                item != null
                && item.ContainerType == ItemContainerType.Lost
                && item.OwnerScope == ItemOwnerScope.Lost)
            && _nodeSettlementCompletedCount == 1;

        DungeonEventBus.OnCombatLootPrepared -= CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected -= CaptureLootCollected;
        DungeonEventBus.OnNodeSettlementCompleted -= CaptureSettlementCompleted;

        if (lootPrepared && offeredItemsMarkedInbox && collectionValid) {
            Debug.Log("Treasure Node Reward Pickup Flow PASSED.");
        } else {
            Debug.LogError($"Treasure Node Reward Pickup Flow FAILED. LootPrepared={lootPrepared}, Inbox={offeredItemsMarkedInbox}, CollectionValid={collectionValid}, Settlement={_nodeSettlementCompletedCount}");
        }
    }

    private static void TestOutcomeNodePublishesResultAndAppliesEffects() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.HP_Current = 50;
        doll.Status.SAN_Current = 40;

        RestStopNode node = new RestStopNode {
            NodeID = "rest_stop_smoke"
        };
        node.Init(new NodePoolEntry {
            NodeType = "RestStopNode",
            Title = "Smoke Rest",
            Description = "Smoke rest result.",
            OutcomeEffects = {
                new DungeonNodeOutcomeConfig {
                    Type = "ModifyResource",
                    Resource = "HP",
                    Amount = 7
                },
                new DungeonNodeOutcomeConfig {
                    Type = "ModifyResource",
                    Resource = "SAN",
                    Amount = 3
                }
            }
        });

        _lastNodeResult = null;
        DungeonEventBus.OnDungeonNodeResolutionPrepared += CaptureNodeResult;
        node.OnEnterNode();
        DungeonEventBus.OnDungeonNodeResolutionPrepared -= CaptureNodeResult;

        bool resultValid = _lastNodeResult != null
            && _lastNodeResult.NodeID == node.NodeID
            && _lastNodeResult.Title == "Smoke Rest"
            && _lastNodeResult.Summary.Contains("HP: 50 -> 57")
            && _lastNodeResult.Summary.Contains("SAN: 40 -> 43");

        bool stateChanged = doll.Status.HP_Current == 57 && doll.Status.SAN_Current == 43;

        if (resultValid && stateChanged) {
            Debug.Log("Outcome Node Result Effects PASSED.");
        } else {
            Debug.LogError($"Outcome Node Result Effects FAILED. Result={resultValid}, State={stateChanged}, HP={doll.Status.HP_Current}, SAN={doll.Status.SAN_Current}");
        }
    }

    private static void TestCombatMapNodeLootReturnsToMap() {
        CoreBackend core = CreateCoreWithEmptyBackpack();
        CombatNode combatNode = new CombatNode {
            NodeID = "combat_map_flow_smoke"
        };
        combatNode.MonsterIDs.Add("mob_scavenger_bug");

        RestStopNode nextNode = CreateNextRestNode("combat_map_flow_next");
        core.Dungeon.CurrentLayer = BuildLinearLayer(combatNode, nextNode);

        ResetEventCaptures();
        DungeonEventBus.OnCombatLootPrepared += CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected += CaptureLootCollected;
        DungeonEventBus.OnNodeResolutionFinished += CaptureNodeResolutionFinished;

        bool canEnter = core.Dungeon.CanMoveToNode(combatNode);
        core.Dungeon.MoveToNode(combatNode);
        bool entered = core.Dungeon.CurrentLayer.CurrentNode == combatNode && combatNode.IsVisited;

        combatNode.ResolveAfterVictory();
        ItemEntity offeredItem = _lastLootResult?.OfferedItems.FirstOrDefault();
        bool lootPrepared = _lastLootResult != null
            && _lastLootResult.NodeID == combatNode.NodeID
            && offeredItem != null
            && offeredItem.ContainerType == ItemContainerType.Inbox;

        string placeReason = string.Empty;
        bool placed = offeredItem != null && PlaceFirstAvailableViaInventoryService(offeredItem, "CombatMapNodeLootReturn", out placeReason);
        combatNode.ConfirmLootCollection();

        bool collectionValid = _lastCollectionResult != null
            && _lastCollectionResult.NodeID == combatNode.NodeID
            && _lastCollectionResult.AcceptedItems.Contains(offeredItem)
            && _lastCollectionResult.DiscardedItems.Count == 0
            && offeredItem.ContainerType == ItemContainerType.Backpack
            && offeredItem.OwnerScope == ItemOwnerScope.Run;
        bool returnedToMap = _nodeResolutionFinishedCount >= 1 && core.Dungeon.CanMoveToNode(nextNode);

        DungeonEventBus.OnCombatLootPrepared -= CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected -= CaptureLootCollected;
        DungeonEventBus.OnNodeResolutionFinished -= CaptureNodeResolutionFinished;

        if (canEnter && entered && lootPrepared && placed && collectionValid && returnedToMap) {
            Debug.Log("Combat Map Node Loot Return Flow PASSED.");
        } else {
            Debug.LogError($"Combat Map Node Loot Return Flow FAILED. CanEnter={canEnter}, Entered={entered}, LootPrepared={lootPrepared}, Placed={placed} ({placeReason}), Collection={collectionValid}, Returned={returnedToMap}, Finished={_nodeResolutionFinishedCount}");
        }
    }

    private static void TestTreasureMapNodeRewardReturnsToMap() {
        CoreBackend core = CreateCoreWithEmptyBackpack();
        TreasureNode treasureNode = new TreasureNode {
            NodeID = "treasure_map_flow_smoke"
        };
        treasureNode.Init(new NodePoolEntry {
            NodeType = "TreasureNode",
            RewardID = "reward_node_treasure_layer1",
            Title = "Smoke Treasure Map Flow"
        });

        RestStopNode nextNode = CreateNextRestNode("treasure_map_flow_next");
        core.Dungeon.CurrentLayer = BuildLinearLayer(treasureNode, nextNode);

        ResetEventCaptures();
        DungeonEventBus.OnCombatLootPrepared += CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected += CaptureLootCollected;
        DungeonEventBus.OnNodeResolutionFinished += CaptureNodeResolutionFinished;

        bool canEnter = core.Dungeon.CanMoveToNode(treasureNode);
        core.Dungeon.MoveToNode(treasureNode);
        bool entered = core.Dungeon.CurrentLayer.CurrentNode == treasureNode && treasureNode.IsVisited;

        ItemEntity offeredItem = _lastLootResult?.OfferedItems.FirstOrDefault();
        bool lootPrepared = _lastLootResult != null
            && _lastLootResult.NodeID == treasureNode.NodeID
            && offeredItem != null
            && offeredItem.ContainerType == ItemContainerType.Inbox
            && offeredItem.OwnerScope == ItemOwnerScope.Run;

        treasureNode.ConfirmLootCollection();
        bool collectionValid = _lastCollectionResult != null
            && _lastCollectionResult.NodeID == treasureNode.NodeID
            && _lastCollectionResult.AcceptedItems.Count == 0
            && _lastCollectionResult.DiscardedItems.Contains(offeredItem)
            && offeredItem.ContainerType == ItemContainerType.Lost
            && offeredItem.OwnerScope == ItemOwnerScope.Lost;
        bool returnedToMap = _nodeResolutionFinishedCount >= 1 && core.Dungeon.CanMoveToNode(nextNode);

        DungeonEventBus.OnCombatLootPrepared -= CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected -= CaptureLootCollected;
        DungeonEventBus.OnNodeResolutionFinished -= CaptureNodeResolutionFinished;

        if (canEnter && entered && lootPrepared && collectionValid && returnedToMap) {
            Debug.Log("Treasure Map Node Reward Return Flow PASSED.");
        } else {
            Debug.LogError($"Treasure Map Node Reward Return Flow FAILED. CanEnter={canEnter}, Entered={entered}, LootPrepared={lootPrepared}, Collection={collectionValid}, Returned={returnedToMap}, Finished={_nodeResolutionFinishedCount}");
        }
    }

    private static void TestOutcomeMapNodeResultReturnsToMap() {
        CoreBackend core = CreateCoreWithEmptyBackpack();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.HP_Current = 60;
        doll.Status.SAN_Current = 50;

        RestStopNode restNode = new RestStopNode {
            NodeID = "outcome_map_flow_smoke"
        };
        restNode.Init(new NodePoolEntry {
            NodeType = "RestStopNode",
            Title = "Smoke Outcome Map Flow",
            Description = "Smoke outcome map flow result.",
            OutcomeEffects = {
                new DungeonNodeOutcomeConfig {
                    Type = "ModifyResource",
                    Resource = "HP",
                    Amount = 5
                },
                new DungeonNodeOutcomeConfig {
                    Type = "ModifyResource",
                    Resource = "SAN",
                    Amount = 4
                }
            }
        });

        RestStopNode nextNode = CreateNextRestNode("outcome_map_flow_next");
        core.Dungeon.CurrentLayer = BuildLinearLayer(restNode, nextNode);

        ResetEventCaptures();
        DungeonEventBus.OnDungeonNodeResolutionPrepared += CaptureNodeResult;
        DungeonEventBus.OnNodeResolutionFinished += CaptureNodeResolutionFinished;

        bool canEnter = core.Dungeon.CanMoveToNode(restNode);
        core.Dungeon.MoveToNode(restNode);
        bool entered = core.Dungeon.CurrentLayer.CurrentNode == restNode && restNode.IsVisited;
        int expectedSanAfterMoveAndOutcome = Mathf.Clamp(
            50 - ConfigManager.Dungeons[1].SANCostPerNode + 4,
            0,
            doll.Status.SAN_Max);
        bool resultPrepared = _lastNodeResult != null
            && _lastNodeResult.NodeID == restNode.NodeID
            && _lastNodeResult.Summary.Contains("HP: 60 -> 65")
            && _lastNodeResult.Summary.Contains($"SAN: {50 - ConfigManager.Dungeons[1].SANCostPerNode} -> {expectedSanAfterMoveAndOutcome}");
        bool stateChanged = doll.Status.HP_Current == 65 && doll.Status.SAN_Current == expectedSanAfterMoveAndOutcome;

        DungeonEventBus.PublishNodeSettlementCompleted();
        bool returnedToMap = _nodeResolutionFinishedCount >= 1 && core.Dungeon.CanMoveToNode(nextNode);

        DungeonEventBus.OnDungeonNodeResolutionPrepared -= CaptureNodeResult;
        DungeonEventBus.OnNodeResolutionFinished -= CaptureNodeResolutionFinished;

        if (canEnter && entered && resultPrepared && stateChanged && returnedToMap) {
            Debug.Log("Outcome Map Node Result Return Flow PASSED.");
        } else {
            Debug.LogError($"Outcome Map Node Result Return Flow FAILED. CanEnter={canEnter}, Entered={entered}, Result={resultPrepared}, State={stateChanged}, Returned={returnedToMap}, Finished={_nodeResolutionFinishedCount}");
        }
    }

    private static void TestLayerConfigsContainFormalNodeVariety() {
        CoreBackend core = CreateCore();
        DungeonConfig layer1 = ConfigManager.Dungeons.ContainsKey(1) ? ConfigManager.Dungeons[1] : null;
        DungeonConfig layer2 = ConfigManager.Dungeons.ContainsKey(2) ? ConfigManager.Dungeons[2] : null;

        bool layer1Valid = ContainsNodeType(layer1, "TreasureNode")
            && ContainsNodeType(layer1, "EventNode")
            && ContainsNodeType(layer1, "RestStopNode")
            && ContainsNodeType(layer1, "HazardNode");
        bool layer2Valid = ContainsNodeType(layer2, "TreasureNode")
            && ContainsNodeType(layer2, "EventNode")
            && ContainsNodeType(layer2, "HazardNode");

        if (core != null && layer1Valid && layer2Valid) {
            Debug.Log("Layer Config Formal Node Variety PASSED.");
        } else {
            Debug.LogError($"Layer Config Formal Node Variety FAILED. Layer1={layer1Valid}, Layer2={layer2Valid}");
        }
    }

    private static bool ContainsNodeType(DungeonConfig dungeon, string nodeType) {
        return dungeon?.NodePool != null
            && dungeon.NodePool.Any(entry => entry != null && entry.NodeType == nodeType);
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static CoreBackend CreateCoreWithEmptyBackpack() {
        CoreBackend core = CreateCore();
        if (core.CurrentPlayer?.ActiveDoll?.Chassis != null) {
            core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
        }

        return core;
    }

    private static DungeonLayer BuildLinearLayer(NodeBase entryNode, NodeBase nextNode) {
        DungeonLayer layer = new DungeonLayer {
            LayerID = 1,
            RootNode = entryNode,
            CurrentNode = null
        };

        if (entryNode != null && nextNode != null && !entryNode.NextNodes.Contains(nextNode)) {
            entryNode.NextNodes.Add(nextNode);
        }

        if (entryNode != null) {
            entryNode.MapRow = 0;
            entryNode.MapColumn = 0;
            layer.EntryNodes.Add(entryNode);
            layer.NodeRows.Add(new List<NodeBase> { entryNode });
        }

        if (nextNode != null) {
            nextNode.MapRow = 1;
            nextNode.MapColumn = 0;
            layer.NodeRows.Add(new List<NodeBase> { nextNode });
        }

        return layer;
    }

    private static RestStopNode CreateNextRestNode(string nodeID) {
        RestStopNode node = new RestStopNode {
            NodeID = nodeID
        };
        node.Init(new NodePoolEntry {
            NodeType = "RestStopNode",
            Title = "Smoke Next Node",
            Description = "Next node placeholder."
        });
        return node;
    }

    private static bool PlaceFirstAvailableViaInventoryService(ItemEntity item, string source, out string reason) {
        reason = string.Empty;
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll(source);
        if (!InventoryInteractionService.TryGetGrid(context, out BackpackGrid grid, out reason)) {
            return false;
        }

        if (!grid.TryFindFirstAvailable(item, out int x, out int y)) {
            reason = "背包没有可放置该战利品的位置。";
            return false;
        }

        return InventoryInteractionService.RequestPlace(item, x, y, context, out reason);
    }

    private static void ResetEventCaptures() {
        _lastLootResult = null;
        _lastCollectionResult = null;
        _lastNodeResult = null;
        _nodeSettlementCompletedCount = 0;
        _nodeResolutionFinishedCount = 0;
    }

    private static void CaptureLootPrepared(CombatLootPickupResult result) {
        _lastLootResult = result;
    }

    private static void CaptureLootCollected(CombatLootCollectionResult result) {
        _lastCollectionResult = result;
    }

    private static void CaptureNodeResult(DungeonNodeResolutionResult result) {
        _lastNodeResult = result;
    }

    private static void CaptureSettlementCompleted() {
        _nodeSettlementCompletedCount++;
    }

    private static void CaptureNodeResolutionFinished() {
        _nodeResolutionFinishedCount++;
    }
}
