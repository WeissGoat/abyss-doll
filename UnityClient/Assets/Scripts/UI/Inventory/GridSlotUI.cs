using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Mounted on each 1x1 inventory slot prefab.
public class GridSlotUI : MonoBehaviour, IDropHandler {
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsLocked { get; private set; }

    private Image _image;

    public void Initialize(int x, int y, bool isLocked) {
        X = x;
        Y = y;
        IsLocked = isLocked;

        _image = GetComponent<Image>();
        VisualUIHelper.ApplyInventorySlotSkin(_image, IsLocked);
    }

    public void OnDrop(PointerEventData eventData) {
        if (IsLocked) {
            return;
        }

        DraggableItemUI draggedItem = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableItemUI>()
            : null;
        if (draggedItem == null) {
            return;
        }

        int originX = X - draggedItem.DragCellOffsetX;
        int originY = Y - draggedItem.DragCellOffsetY;

        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("SlotDrop");
        if (InventoryInteractionService.RequestPlace(draggedItem.ItemData, originX, originY, context, out string placeReason)) {
            GridGenerator generator = GetComponentInParent<GridGenerator>();
            Transform targetSlot = generator != null ? generator.GetSlot(originX, originY) : null;
            draggedItem.SnapToSlot(targetSlot != null ? targetSlot : transform, originX, originY);

            Debug.Log($"[UI] Placed item {draggedItem.ItemData.Name} at ({originX}, {originY}).");
            return;
        }

        draggedItem.ReturnToOriginalPosition();
        Debug.LogWarning($"[UI] Cannot place item {draggedItem.ItemData.Name} at ({originX}, {originY}).");
    }
}
