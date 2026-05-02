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
    private CombatLootPickupResult _pendingCombatLoot;
    
    public DungeonManager() {
        DungeonEventBus.OnDungeonEvacuated += HandleEvacuate;
        DungeonEventBus.OnDungeonDefeated += HandleDefeat;
        DungeonEventBus.OnCombatNodeCleared += HandleCombatNodeCleared;
    }

    ~DungeonManager() {
        DungeonEventBus.OnDungeonEvacuated -= HandleEvacuate;
        DungeonEventBus.OnDungeonDefeated -= HandleDefeat;
        DungeonEventBus.OnCombatNodeCleared -= HandleCombatNodeCleared;
    }
    
    public void LoadLayer(int layerID) {
        if (!ConfigManager.Dungeons.ContainsKey(layerID)) {
            Debug.LogError($"[DungeonManager] Layer {layerID} not found.");
            return;
        }
        
        var config = ConfigManager.Dungeons[layerID];
        Debug.Log($"[DungeonManager] Entering Layer {layerID}: {config.Name}");
        
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

    private void HandleCombatNodeCleared() {
        Debug.Log("[DungeonManager] Combat node cleared.");
        CombatLootPickupResult lootResult = PrepareCombatLootForCurrentNode();
        if (lootResult != null && lootResult.OfferedItems.Count > 0) {
            _pendingCombatLoot = lootResult;
            Debug.Log($"[DungeonManager] Prepared combat loot pickup. OfferedCount={lootResult.OfferedItems.Count}, EstimatedValue={lootResult.TotalEstimatedValue}");
            DungeonEventBus.PublishCombatLootPrepared(lootResult);
            return;
        }

        ContinueAfterCurrentNodeResolved();
    }

    public void ConfirmPendingCombatLootCollection() {
        if (_pendingCombatLoot != null) {
            BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
            int acceptedCount = 0;
            int discardedCount = 0;

            foreach (var item in _pendingCombatLoot.OfferedItems) {
                if (item == null) {
                    continue;
                }

                if (grid != null && grid.ContainedItems.Contains(item)) {
                    acceptedCount++;
                } else {
                    discardedCount++;
                }
            }

            Debug.Log($"[DungeonManager] Combat loot confirmed. Accepted={acceptedCount}, Discarded={discardedCount}");
            _pendingCombatLoot = null;
        }

        ContinueAfterCurrentNodeResolved();
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

    private CombatLootPickupResult PrepareCombatLootForCurrentNode() {
        if (!(CurrentLayer?.CurrentNode is CombatNode combatNode)) {
            return null;
        }

        if (combatNode.MonsterIDs == null || combatNode.MonsterIDs.Count == 0) {
            Debug.Log("[DungeonManager] Combat node had no MonsterIDs. No loot generated.");
            return null;
        }

        CombatLootPickupResult result = new CombatLootPickupResult {
            NodeID = combatNode.NodeID
        };

        foreach (string monsterID in combatNode.MonsterIDs) {
            ItemEntity droppedItem = RollLootForMonster(monsterID);
            if (droppedItem == null) {
                continue;
            }

            result.SourceMonsterIDs.Add(monsterID);
            result.OfferedItems.Add(droppedItem);
            result.TotalEstimatedValue += droppedItem.BaseValue;
        }

        return result;
    }

    private ItemEntity RollLootForMonster(string monsterID) {
        if (string.IsNullOrEmpty(monsterID) || !ConfigManager.Monsters.TryGetValue(monsterID, out var monster)) {
            Debug.LogWarning($"[DungeonManager] Cannot roll loot. Monster config missing: {monsterID}");
            return null;
        }

        if (monster.LootPool == null || monster.LootPool.Count == 0) {
            Debug.Log($"[DungeonManager] Monster [{monster.Name}] has no loot pool configured.");
            return null;
        }

        int totalWeight = 0;
        foreach (var entry in monster.LootPool) {
            if (entry != null && entry.Weight > 0 && !string.IsNullOrEmpty(entry.ItemID)) {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0) {
            Debug.LogWarning($"[DungeonManager] Monster [{monster.Name}] loot pool has no valid weighted entries.");
            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cursor = 0;
        foreach (var entry in monster.LootPool) {
            if (entry == null || entry.Weight <= 0 || string.IsNullOrEmpty(entry.ItemID)) {
                continue;
            }

            cursor += entry.Weight;
            if (roll < cursor) {
                ItemEntity item = ConfigManager.CreateItem(entry.ItemID);
                if (item == null) {
                    Debug.LogError($"[DungeonManager] Loot config points to missing item: {entry.ItemID}");
                }
                return item;
            }
        }

        return null;
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
                foreach (var item in grid.ContainedItems) {
                    if (item == null) continue;
                    result.LootTransferredCount++;
                    result.LootEstimatedValue += item.BaseValue;
                    result.LootNames.Add(item.Name);
                }
                GameRoot.Core.CurrentPlayer.StashInventory.AddRange(grid.ContainedItems);
                int count = grid.ContainedItems.Count;
                grid.ContainedItems.Clear();
                Debug.Log($"[DungeonManager] {count} items safely transferred from Backpack to StashInventory.");
            }
        }

        result.StashCountAfterSettlement = GameRoot.Core.CurrentPlayer.StashInventory.Count;
        Debug.Log($"[DungeonManager] Settlement summary prepared. LootCount={result.LootTransferredCount}, EstimatedValue={result.LootEstimatedValue}, StashCount={result.StashCountAfterSettlement}");
        DungeonEventBus.PublishDungeonSettlementPrepared(result);
        DungeonEventBus.PublishDungeonSettled(true);
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
                grid.ContainedItems.Clear();
                Debug.LogWarning($"[DungeonManager] Player defeated. All items in Backpack have been lost.");
            }
        }

        result.StashCountAfterSettlement = GameRoot.Core.CurrentPlayer.StashInventory.Count;
        Debug.Log($"[DungeonManager] Defeat settlement prepared. StashCount={result.StashCountAfterSettlement}");
        DungeonEventBus.PublishDungeonSettlementPrepared(result);
        DungeonEventBus.PublishDungeonSettled(false);
    }
}
