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
        DungeonSafeZoneService.RestoreActiveDollToFull(NodeID, "SafeRoomRest");
        DungeonEventBus.PublishNodeSettlementCompleted();
    }
}
