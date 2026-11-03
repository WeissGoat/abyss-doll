using UnityEngine;

public class StairsNode : NodeBase {
    public int LayerID { get; set; }

    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered Stairs Node {NodeID}. Choose to descend or return to town.");
        GameRoot.Core?.Dungeon?.TryUnlockNextStartLayerFromClearedLayer(LayerID);
        DungeonSafeZoneRules rules = ConfigManager.Dungeons != null && ConfigManager.Dungeons.TryGetValue(LayerID, out DungeonConfig config)
            ? config.SafeZoneRules ?? new DungeonSafeZoneRules()
            : new DungeonSafeZoneRules();
        DungeonSafeZoneService.Recover(NodeID, "LayerEndSafeZone", rules.RecoverHPRatio, rules.RecoverSANRatio);
        DungeonEventBus.PublishStairsEntered(this);
    }

    public bool CanEnterNextLayer() {
        return GameRoot.Core?.Dungeon != null && GameRoot.Core.Dungeon.CanEnterNextLayer();
    }

    public void EnterNextLayer() {
        Debug.Log($"[Dungeon] Player chose to descend from Layer {LayerID} stairs.");
        GameRoot.Core?.Dungeon?.EnterNextLayer();
    }

    public void ReturnToTown() {
        Debug.Log($"[Dungeon] Player chose to return to town at Stairs {NodeID}.");
        DungeonEventBus.PublishDungeonEvacuated();
    }
}
