using UnityEngine;

public class SafeRoomNode : NodeBase {
    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered Safe Room Node {NodeID}. You can rest here.");
        DungeonEventBus.PublishSafeRoomEntered(this);
    }

    public void Evacuate() {
        Debug.Log($"[Dungeon] Player chose to evacuate at Safe Room {NodeID}.");
        DungeonEventBus.PublishDungeonEvacuated();
    }
    
    public void Rest() {
        // 恢复所有状态的 MVP 简单实现
        var doll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (doll != null) {
            doll.Status.HP_Current = doll.Status.HP_Max;
            doll.Status.SAN_Current = doll.Status.SAN_Max;
            
            GameEventBus.PublishHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
            GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
            
            Debug.Log($"[Dungeon] Rested at Safe Room. HP and SAN restored.");
        }
        
        // 休息后可继续前进，相当于清理了节点
        DungeonEventBus.PublishCombatNodeCleared();
    }
}
