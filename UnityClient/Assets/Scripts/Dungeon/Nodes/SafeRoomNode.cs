using UnityEngine;

// A way out partway through a layer: the lift at the end of a segment (GDD_13). She can go home with what she
// carries, or go on; going on gives back only what the node is configured to (a lift gives nothing).
public class SafeRoomNode : NodeBase {
    public float RecoverHPRatio { get; private set; }
    public float RecoverSANRatio { get; private set; }

    public override void Init(NodePoolEntry entry) {
        base.Init(entry);
        if (entry != null) {
            RecoverHPRatio = entry.RecoverHPRatio;
            RecoverSANRatio = entry.RecoverSANRatio;
        }
    }

    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered Safe Room Node {NodeID}. Go home or go on.");
        DungeonEventBus.PublishSafeRoomEntered(this);
    }

    public void Evacuate() {
        Debug.Log($"[Dungeon] Player chose to evacuate at Safe Room {NodeID}.");
        DungeonEventBus.PublishDungeonEvacuated();
    }

    public void Continue() {
        if (RecoverHPRatio > 0f || RecoverSANRatio > 0f) {
            DungeonSafeZoneService.Recover(NodeID, "SafeRoomRest", RecoverHPRatio, RecoverSANRatio);
        }

        DungeonEventBus.PublishNodeSettlementCompleted();
    }
}
