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
        
        MoveToNode(CurrentLayer.RootNode);
    }
    
    public void MoveToNode(NodeBase targetNode) {
        CurrentLayer.CurrentNode = targetNode;
        targetNode.IsVisited = true;
        
        int cost = ConfigManager.Dungeons[CurrentLayer.LayerID].SANCostPerNode;
        
        // 核心解耦点：不再在 Manager 里面去硬调 Doll 的扣血扣SAN，而是广播事件
        DungeonEventBus.PublishNodeEntered(targetNode, cost);
        
        targetNode.OnEnterNode();
    }
}
