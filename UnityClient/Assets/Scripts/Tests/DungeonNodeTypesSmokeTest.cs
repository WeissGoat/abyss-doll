using System.Linq;
using UnityEngine;

public static class DungeonNodeTypesSmokeTest {
    private static CombatLootPickupResult _lastLootResult;
    private static CombatLootCollectionResult _lastCollectionResult;
    private static DungeonNodeResolutionResult _lastNodeResult;
    private static int _nodeSettlementCompletedCount;

    public static void Run() {
        Debug.Log("=== Running Dungeon Node Types Smoke Test ===");

        TestConfiguredNodeTypesAreRegistered();
        TestTreasureNodeUsesRewardPickupFlow();
        TestOutcomeNodePublishesResultAndAppliesEffects();
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
        node.ConfirmLootCollection();
        bool collectionValid = _lastCollectionResult != null
            && _lastCollectionResult.NodeID == node.NodeID
            && _lastLootResult != null
            && _lastCollectionResult.DiscardedItems.Count == _lastLootResult.OfferedItems.Count
            && _nodeSettlementCompletedCount == 1;

        DungeonEventBus.OnCombatLootPrepared -= CaptureLootPrepared;
        DungeonEventBus.OnCombatLootCollected -= CaptureLootCollected;
        DungeonEventBus.OnNodeSettlementCompleted -= CaptureSettlementCompleted;

        if (lootPrepared && collectionValid) {
            Debug.Log("Treasure Node Reward Pickup Flow PASSED.");
        } else {
            Debug.LogError($"Treasure Node Reward Pickup Flow FAILED. LootPrepared={lootPrepared}, CollectionValid={collectionValid}, Settlement={_nodeSettlementCompletedCount}");
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
}
