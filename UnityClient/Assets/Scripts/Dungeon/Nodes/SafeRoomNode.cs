using UnityEngine;

public class SafeRoomNode : NodeBase {
    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered Safe Room Node {NodeID}. You can rest here.");
        // Trigger UI for Safe Room
    }
}
