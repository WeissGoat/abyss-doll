using UnityEngine;

public static class InventoryInteractionServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Inventory Interaction Service Smoke Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity item = grid?.ContainedItems.Count > 0 ? grid.ContainedItems[0] : null;
        if (grid == null || item == null) {
            Debug.LogError("Inventory Interaction Bootstrap FAILED: missing grid or item.");
            return;
        }

        int originalX = item.Grid.CurrentPos[0];
        int originalY = item.Grid.CurrentPos[1];
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionSmoke");

        bool pickedUp = InventoryInteractionService.RequestPickUp(item, context, out InventoryItemPlacement placement, out string pickUpReason);
        bool removedFromGrid = !grid.ContainedItems.Contains(item);
        bool restored = InventoryInteractionService.RequestRestore(item, placement, context, out string restoreReason);
        bool restoredToOriginalCell = grid.GetItemAt(originalX, originalY) == item;

        InventoryInteractionContext noDiscardContext = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionSmokeNoDiscard");
        bool rejectDiscard = !InventoryInteractionService.RequestStageDiscard(item, noDiscardContext, out string rejectDiscardReason)
            && rejectDiscardReason.Contains("不允许");

        InventoryInteractionContext allowDiscardContext = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionSmokeAllowDiscard", true);
        bool allowDiscard = InventoryInteractionService.RequestStageDiscard(item, allowDiscardContext, out string allowDiscardReason);

        if (pickedUp && removedFromGrid && restored && restoredToOriginalCell && rejectDiscard && allowDiscard) {
            Debug.Log("Inventory Interaction Service PASSED.");
        } else {
            Debug.LogError($"Inventory Interaction Service FAILED. PickedUp={pickedUp} ({pickUpReason}), Removed={removedFromGrid}, Restored={restored} ({restoreReason}), OriginalCell={restoredToOriginalCell}, RejectDiscard={rejectDiscard}, AllowDiscard={allowDiscard} ({allowDiscardReason})");
        }

        Debug.Log("=== Inventory Interaction Service Smoke Test Finished ===");
    }
}
