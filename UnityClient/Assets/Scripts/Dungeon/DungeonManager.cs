using UnityEngine;
using System.Collections.Generic;

public class DungeonLayer {
    public int LayerID;
    public NodeBase RootNode;
    public NodeBase CurrentNode;

    public void GenerateMapTree(DungeonConfig config) {
        NodeBase prevNode = null;

        // MVP: linear path. ExpectedNodeCount includes the boss; EndNode is configured separately.
        for (int i = 0; i < config.ExpectedNodeCount - 1; i++) {
            NodePoolEntry entry = PickRandomNode(config.NodePool);
            if (entry == null) {
                continue;
            }

            NodeBase newNode = CreateConfiguredNode(entry, $"layer_{config.LayerID}_node_{i}", config.LayerID);
            AppendNode(ref prevNode, newNode);
        }

        CombatNode bossNode = NodeFactory.CreateNode("CombatNode") as CombatNode;
        if (bossNode != null) {
            bossNode.NodeID = $"layer_{config.LayerID}_boss";
            bossNode.MonsterIDs = new List<string> { config.BossNode };
            AppendNode(ref prevNode, bossNode);
        }

        NodeBase endNode = CreateConfiguredNode(config.EndNode, $"layer_{config.LayerID}_end", config.LayerID);
        AppendNode(ref prevNode, endNode);
    }

    private NodeBase CreateConfiguredNode(NodePoolEntry entry, string nodeID, int layerID) {
        if (entry == null) {
            return null;
        }

        NodeBase node = NodeFactory.CreateNode(entry.NodeType);
        if (node == null) {
            return null;
        }

        node.Init(entry);
        node.NodeID = nodeID;

        if (node is StairsNode stairsNode) {
            stairsNode.LayerID = layerID;
        }

        return node;
    }

    private void AppendNode(ref NodeBase prevNode, NodeBase newNode) {
        if (newNode == null) {
            return;
        }

        if (prevNode == null) {
            RootNode = newNode;
        } else {
            prevNode.NextNodes.Add(newNode);
        }

        prevNode = newNode;
    }

    private NodePoolEntry PickRandomNode(List<NodePoolEntry> pool) {
        if (pool == null || pool.Count == 0) {
            return null;
        }

        int totalWeight = 0;
        foreach (var p in pool) {
            totalWeight += p.Weight;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int current = 0;
        foreach (var p in pool) {
            current += p.Weight;
            if (roll < current) {
                return p;
            }
        }

        return pool[pool.Count - 1];
    }
}

public class DungeonManager {
    public DungeonLayer CurrentLayer;
    private readonly List<ItemEntity> _runAcceptedLoot = new List<ItemEntity>();

    public DungeonManager() {
        DungeonEventBus.OnDungeonEvacuated += HandleEvacuate;
        DungeonEventBus.OnDungeonDefeated += HandleDefeat;
        DungeonEventBus.OnNodeSettlementCompleted += HandleNodeSettlementCompleted;
        DungeonEventBus.OnCombatLootCollected += HandleCombatLootCollected;
    }

    ~DungeonManager() {
        DungeonEventBus.OnDungeonEvacuated -= HandleEvacuate;
        DungeonEventBus.OnDungeonDefeated -= HandleDefeat;
        DungeonEventBus.OnNodeSettlementCompleted -= HandleNodeSettlementCompleted;
        DungeonEventBus.OnCombatLootCollected -= HandleCombatLootCollected;
    }

    public void LoadLayer(int layerID) {
        LoadLayer(layerID, true);
    }

    public void LoadLayer(int layerID, bool resetRunLootLedger) {
        if (!ConfigManager.Dungeons.ContainsKey(layerID)) {
            Debug.LogError($"[DungeonManager] Layer {layerID} not found.");
            return;
        }

        var config = ConfigManager.Dungeons[layerID];
        Debug.Log($"[DungeonManager] Entering Layer {layerID}: {config.Name}");

        if (resetRunLootLedger) {
            ResetRunLootLedger();
        }

        CurrentLayer = new DungeonLayer();
        CurrentLayer.LayerID = layerID;
        CurrentLayer.GenerateMapTree(config);

        DungeonEventBus.PublishLayerLoaded();
    }

    public bool CanEnterNextLayer() {
        return CurrentLayer != null && ConfigManager.Dungeons.ContainsKey(CurrentLayer.LayerID + 1);
    }

    public void EnterNextLayer() {
        if (CurrentLayer == null) {
            Debug.LogWarning("[DungeonManager] Cannot enter next layer because CurrentLayer is null.");
            return;
        }

        int nextLayerID = CurrentLayer.LayerID + 1;
        if (!ConfigManager.Dungeons.ContainsKey(nextLayerID)) {
            Debug.Log("[DungeonManager] No next layer configured. Evacuating from abyss end.");
            DungeonEventBus.PublishDungeonEvacuated();
            return;
        }

        Debug.Log($"[DungeonManager] Descending from Layer {CurrentLayer.LayerID} to Layer {nextLayerID}.");
        LoadLayer(nextLayerID, false);
    }

    public void MoveToNode(NodeBase targetNode) {
        if (CurrentLayer == null || targetNode == null) {
            return;
        }

        CurrentLayer.CurrentNode = targetNode;
        targetNode.IsVisited = true;

        int cost = GetSanCostForNode(targetNode);
        DungeonEventBus.PublishNodeEntered(targetNode, cost);

        targetNode.OnEnterNode();
    }

    private int GetSanCostForNode(NodeBase node) {
        if (node is SafeRoomNode || node is StairsNode) {
            return 0;
        }

        return ConfigManager.Dungeons[CurrentLayer.LayerID].SANCostPerNode;
    }

    private void HandleNodeSettlementCompleted() {
        Debug.Log($"[DungeonManager] Node settlement completed: {CurrentLayer?.CurrentNode?.NodeID ?? "UnknownNode"}");
        ContinueAfterCurrentNodeResolved();
    }

    private void HandleCombatLootCollected(CombatLootCollectionResult result) {
        if (result?.AcceptedItems == null || result.AcceptedItems.Count == 0) {
            return;
        }

        _runAcceptedLoot.AddRange(result.AcceptedItems);
        Debug.Log($"[DungeonManager] Run loot ledger updated. AcceptedThisRun={_runAcceptedLoot.Count}");
    }

    private void ContinueAfterCurrentNodeResolved() {
        if (CurrentLayer?.CurrentNode == null) {
            return;
        }

        if (CurrentLayer.CurrentNode.NextNodes == null || CurrentLayer.CurrentNode.NextNodes.Count == 0) {
            Debug.Log("[DungeonManager] Reached a terminal node without next choices. Auto-evacuating as fallback.");
            DungeonEventBus.PublishDungeonEvacuated();
        } else {
            Debug.Log("[DungeonManager] Awaiting player to select the next node on the map...");
            DungeonEventBus.PublishNodeResolutionFinished();
        }
    }

    private void HandleEvacuate() {
        Debug.Log("<color=green>[DungeonManager] Handling Dungeon Evacuation (Victory/Escape).</color>");
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = true
        };

        var activeDoll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (activeDoll != null) {
            BackpackGrid grid = activeDoll.RuntimeGrid as BackpackGrid;
            if (grid != null) {
                List<ItemEntity> carriedRunLoot = CollectAcceptedLootStillInBackpack(grid);
                PopulateRunLootSummary(result, carriedRunLoot);

                foreach (var item in carriedRunLoot) {
                    if (item == null) {
                        continue;
                    }

                    result.LootTransferredCount++;
                    result.LootEstimatedValue += item.BaseValue;
                    result.LootNames.Add(item.Name);
                }

                Debug.Log(carriedRunLoot.Count > 0
                    ? $"[DungeonManager] {carriedRunLoot.Count} run loot items remain in the active backpack after evacuation."
                    : "[DungeonManager] No carried run loot to preserve after evacuation.");
            }
        }

        result.StashCountAfterSettlement = GameRoot.Core.CurrentPlayer.StashInventory.Count;
        Debug.Log($"[DungeonManager] Settlement summary prepared. LootCount={result.LootTransferredCount}, EstimatedValue={result.LootEstimatedValue}, StashCount={result.StashCountAfterSettlement}");
        DungeonEventBus.PublishDungeonSettlementPrepared(result);
        DungeonEventBus.PublishDungeonSettled(true);
        ResetRunLootLedger();
    }

    private void HandleDefeat() {
        Debug.Log("<color=red>[DungeonManager] Handling Dungeon Defeat.</color>");
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = false
        };

        var activeDoll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (activeDoll != null) {
            BackpackGrid grid = activeDoll.RuntimeGrid as BackpackGrid;
            if (grid != null) {
                PopulateRunLootSummary(result, null);
                List<ItemEntity> clearedItems = grid.ClearAllItems();
                PublishInventoryRemovalEvents(clearedItems);
                Debug.LogWarning($"[DungeonManager] Player defeated. All items in Backpack have been lost.");
            }
        }

        result.StashCountAfterSettlement = GameRoot.Core.CurrentPlayer.StashInventory.Count;
        Debug.Log($"[DungeonManager] Defeat settlement prepared. StashCount={result.StashCountAfterSettlement}");
        DungeonEventBus.PublishDungeonSettlementPrepared(result);
        DungeonEventBus.PublishDungeonSettled(false);
        ResetRunLootLedger();
    }

    private void PopulateRunLootSummary(DungeonSettlementResult result, IEnumerable<ItemEntity> preservedItems) {
        if (result == null) {
            return;
        }

        HashSet<string> preservedIds = new HashSet<string>();
        if (preservedItems != null) {
            foreach (var item in preservedItems) {
                if (item != null && !string.IsNullOrEmpty(item.InstanceID)) {
                    preservedIds.Add(item.InstanceID);
                }
            }
        }

        foreach (var item in _runAcceptedLoot) {
            if (item == null) {
                continue;
            }

            result.PickedUpCount++;
            result.PickedUpEstimatedValue += item.BaseValue;
            result.PickedUpNames.Add(item.Name);

            if (preservedIds.Contains(item.InstanceID)) {
                result.BroughtOutCount++;
                result.BroughtOutEstimatedValue += item.BaseValue;
                result.BroughtOutNames.Add(item.Name);
            } else {
                result.LostCount++;
                result.LostEstimatedValue += item.BaseValue;
                result.LostNames.Add(item.Name);
            }
        }
    }

    private List<ItemEntity> CollectAcceptedLootStillInBackpack(BackpackGrid grid) {
        List<ItemEntity> carriedRunLoot = new List<ItemEntity>();
        if (grid == null) {
            return carriedRunLoot;
        }

        HashSet<string> seenIds = new HashSet<string>();
        foreach (var item in _runAcceptedLoot) {
            if (item == null || string.IsNullOrEmpty(item.InstanceID)) {
                continue;
            }

            if (seenIds.Contains(item.InstanceID)) {
                continue;
            }

            if (grid.ContainedItems.Contains(item)) {
                carriedRunLoot.Add(item);
                seenIds.Add(item.InstanceID);
            }
        }

        return carriedRunLoot;
    }

    private void ResetRunLootLedger() {
        _runAcceptedLoot.Clear();
    }

    private void PublishInventoryRemovalEvents(IEnumerable<ItemEntity> removedItems) {
        if (removedItems == null) {
            return;
        }

        foreach (var item in removedItems) {
            if (item != null && !string.IsNullOrEmpty(item.InstanceID)) {
                GameEventBus.PublishItemRemoved(item.InstanceID);
            }
        }
    }
}
