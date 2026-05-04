using UnityEngine;
using System.Collections.Generic;

public class DungeonLayer {
    public int LayerID;
    public NodeBase RootNode;
    public NodeBase CurrentNode;
    
    public void GenerateMapTree(DungeonConfig config) {
        NodeBase prevNode = null;
        
        // MVP阶段：生成一条单线路径
        for (int i = 0; i < config.ExpectedNodeCount - 1; i++) {
            NodePoolEntry entry = PickRandomNode(config.NodePool);
            if (entry == null) continue;
            
            NodeBase newNode = NodeFactory.CreateNode(entry.NodeType);
            if (newNode == null) continue;
            
            newNode.Init(entry);
            newNode.NodeID = $"layer_{config.LayerID}_node_{i}";
            
            if (prevNode == null) {
                RootNode = newNode;
            } else {
                prevNode.NextNodes.Add(newNode);
            }
            prevNode = newNode;
        }
        
        // 在最后生成一层 Boss 节点
        CombatNode bossNode = (CombatNode)NodeFactory.CreateNode("CombatNode");
        if (bossNode != null) {
            bossNode.NodeID = $"layer_{config.LayerID}_boss";
            bossNode.MonsterIDs = new List<string> { config.BossNode };
            
            if (prevNode != null) {
                prevNode.NextNodes.Add(bossNode);
            } else {
                RootNode = bossNode;
            }
        }
    }
    
    // 基于权重的随机抽取算法
    private NodePoolEntry PickRandomNode(List<NodePoolEntry> pool) {
        if (pool == null || pool.Count == 0) return null;
        int totalWeight = 0;
        foreach (var p in pool) totalWeight += p.Weight;
        
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int current = 0;
        foreach (var p in pool) {
            current += p.Weight;
            if (roll < current) return p;
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
        if (!ConfigManager.Dungeons.ContainsKey(layerID)) {
            Debug.LogError($"[DungeonManager] Layer {layerID} not found.");
            return;
        }
        
        var config = ConfigManager.Dungeons[layerID];
        Debug.Log($"[DungeonManager] Entering Layer {layerID}: {config.Name}");

        ResetRunLootLedger();
        
        CurrentLayer = new DungeonLayer();
        CurrentLayer.LayerID = layerID;
        CurrentLayer.GenerateMapTree(config);
        
        // 不再自动 MoveToNode，而是告诉前端地图准备好了
        DungeonEventBus.PublishLayerLoaded();
    }
    
    public void MoveToNode(NodeBase targetNode) {
        CurrentLayer.CurrentNode = targetNode;
        targetNode.IsVisited = true;
        
        int cost = ConfigManager.Dungeons[CurrentLayer.LayerID].SANCostPerNode;
        
        // 核心解耦点：不再在 Manager 里面去硬调 Doll 的扣血扣SAN，而是广播事件
        DungeonEventBus.PublishNodeEntered(targetNode, cost);
        
        targetNode.OnEnterNode();
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
        // 如果当前节点没有下一个节点（例如打败了关底 Boss），则自动触发撤离
        if (CurrentLayer.CurrentNode.NextNodes == null || CurrentLayer.CurrentNode.NextNodes.Count == 0) {
            Debug.Log("[DungeonManager] Reached the end of the dungeon. Auto-evacuating.");
            DungeonEventBus.PublishDungeonEvacuated();
        } else {
            // 不自动前进，等待 UI 选择
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
            if (grid != null && grid.ContainedItems.Count > 0) {
                PopulateRunLootSummary(result, grid.ContainedItems);

                foreach (var item in grid.ContainedItems) {
                    if (item == null) continue;
                    result.LootTransferredCount++;
                    result.LootEstimatedValue += item.BaseValue;
                    result.LootNames.Add(item.Name);
                }
                GameRoot.Core.CurrentPlayer.StashInventory.AddRange(grid.ContainedItems);
                List<ItemEntity> clearedItems = grid.ClearAllItems();
                PublishInventoryRemovalEvents(clearedItems);
                int count = clearedItems.Count;
                Debug.Log($"[DungeonManager] {count} items safely transferred from Backpack to StashInventory.");
            } else {
                PopulateRunLootSummary(result, null);
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
