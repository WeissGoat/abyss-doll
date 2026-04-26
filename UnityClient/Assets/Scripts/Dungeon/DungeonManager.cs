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
        // 如果当前节点没有下一个节点（例如打败了关底 Boss），则自动触发撤离
        if (CurrentLayer.CurrentNode.NextNodes == null || CurrentLayer.CurrentNode.NextNodes.Count == 0) {
            Debug.Log("[DungeonManager] Reached the end of the dungeon. Auto-evacuating.");
            DungeonEventBus.PublishDungeonEvacuated();
        } else {
            // 不自动前进，等待 UI 选择
            Debug.Log("[DungeonManager] Awaiting player to select the next node on the map...");
        }
    }

    private void HandleEvacuate() {
        Debug.Log("<color=green>[DungeonManager] Handling Dungeon Evacuation (Victory/Escape).</color>");
        
        var activeDoll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (activeDoll != null) {
            BackpackGrid grid = activeDoll.RuntimeGrid as BackpackGrid;
            if (grid != null && grid.ContainedItems.Count > 0) {
                GameRoot.Core.CurrentPlayer.StashInventory.AddRange(grid.ContainedItems);
                int count = grid.ContainedItems.Count;
                grid.ContainedItems.Clear();
                Debug.Log($"[DungeonManager] {count} items safely transferred from Backpack to StashInventory.");
            }
        }
        
        DungeonEventBus.PublishDungeonSettled(true);
    }

    private void HandleDefeat() {
        Debug.Log("<color=red>[DungeonManager] Handling Dungeon Defeat.</color>");
        
        var activeDoll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (activeDoll != null) {
            BackpackGrid grid = activeDoll.RuntimeGrid as BackpackGrid;
            if (grid != null) {
                grid.ContainedItems.Clear();
                Debug.LogWarning($"[DungeonManager] Player defeated. All items in Backpack have been lost.");
            }
        }
        
        DungeonEventBus.PublishDungeonSettled(false);
    }
}
