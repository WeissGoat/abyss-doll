using System.Collections.Generic;
using UnityEngine;

public class CombatNode : NodeBase {
    public List<string> MonsterIDs { get; set; } = new List<string>();
    
    public override void Init(NodePoolEntry entry) {
        base.Init(entry);
        if (entry != null && entry.MonsterIDs != null) {
            MonsterIDs.AddRange(entry.MonsterIDs);
        }
    }
    
    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered Combat Node {NodeID}. Encountering {MonsterIDs.Count} enemies.");
        GameRoot.Core.Combat.StartCombat(MonsterIDs);
    }
}
