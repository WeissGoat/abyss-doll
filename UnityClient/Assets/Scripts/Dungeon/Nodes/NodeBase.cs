using System.Collections.Generic;
using UnityEngine;

public abstract class NodeBase {
    public string NodeID { get; set; }
    public bool IsVisited { get; set; }
    public List<NodeBase> NextNodes { get; set; } = new List<NodeBase>();
    public string RewardID { get; set; }
    public string NodeIconID { get; set; }
    
    public virtual void Init(NodePoolEntry entry) {
        if (entry != null) {
            RewardID = entry.RewardID;
            NodeIconID = entry.NodeIconID;
        }
    }
    
    public abstract void OnEnterNode();
}
