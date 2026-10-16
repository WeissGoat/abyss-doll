using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Mounted on each 1x1 inventory slot prefab.
public class GridSlotUI : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler {
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsLocked { get; private set; }

    private Image _image;

    public void Initialize(int x, int y, bool isLocked) {
        X = x;
        Y = y;
        IsLocked = isLocked;

        _image = GetComponent<Image>();
        ApplyDefaultSkin();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (_image == null) {
            _image = GetComponent<Image>();
        }

        // While an item is held, it draws the drop preview itself (DraggableItemUI.UpdatePlacementPreview).
        if (ResolveDraggedItem(eventData) != null) {
            return;
        }

        VisualUIHelper.ApplyInventorySlotSkin(
            _image,
            IsLocked,
            IsLocked ? VisualAssetService.UIInventorySlotLockedID : VisualAssetService.UIInventorySlotHoverID);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (ResolveDraggedItem(eventData) != null) {
            return;
        }

        ResetAllSlotSkins();
    }

    public void OnDrop(PointerEventData eventData) {
        ResetAllSlotSkins();

        if (IsLocked) {
            return;
        }

        DraggableItemUI draggedItem = ResolveDraggedItem(eventData);
        if (draggedItem == null) {
            return;
        }

        if (!TryPlace(draggedItem)) {
            draggedItem.ReturnToOriginalPosition();
        }
    }

    // Places the held item with the pointer cell on this slot. Also used when the item is released over the gap
    // between cells or over another item's picture, where no slot receives the drop.
    public bool TryPlace(DraggableItemUI draggedItem) {
        if (IsLocked || draggedItem == null || draggedItem.ItemData == null) {
            return false;
        }

        int originX = X - draggedItem.DragCellOffsetX;
        int originY = Y - draggedItem.DragCellOffsetY;

        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("SlotDrop");
        if (InventoryInteractionService.RequestPlace(draggedItem.ItemData, originX, originY, context, out string placeReason)) {
            GridGenerator generator = GetComponentInParent<GridGenerator>();
            Transform targetSlot = generator != null ? generator.GetSlot(originX, originY) : null;
            draggedItem.SnapToSlot(targetSlot != null ? targetSlot : transform, originX, originY);

            Debug.Log($"[UI] Placed item {draggedItem.ItemData.Name} at ({originX}, {originY}).");
            return true;
        }

        Debug.LogWarning($"[UI] Cannot place item {draggedItem.ItemData.Name} at ({originX}, {originY}). {placeReason}");
        return false;
    }

    private DraggableItemUI ResolveDraggedItem(PointerEventData eventData) {
        DraggableItemUI item = eventData?.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableItemUI>()
            : null;
        // Only a real (left button) drag counts; a right-button drag never picks the item up.
        return item != null && item.IsDragging() ? item : null;
    }

    // Cells the item would take with the pointer over this slot, in the valid or invalid skin; true when it fits.
    public bool ShowPlacementPreview(DraggableItemUI draggedItem) {
        return ApplyDragPreviewSkin(draggedItem);
    }

    public static void ClearPreview() {
        ResetAllSlotSkins();
    }

    public bool IsShowingPreviewSkin(bool valid) {
        if (_image == null) {
            _image = GetComponent<Image>();
        }

        string visualID = valid ? VisualAssetService.UIInventorySlotValidID : VisualAssetService.UIInventorySlotInvalidID;
        return _image != null && _image.sprite != null && VisualAssetService.TryGetSprite(visualID, out Sprite sprite) && _image.sprite == sprite;
    }

    private bool ApplyDragPreviewSkin(DraggableItemUI draggedItem) {
        if (draggedItem == null || draggedItem.ItemData == null) {
            ApplyDefaultSkin();
            return false;
        }

        int originX = X - draggedItem.DragCellOffsetX;
        int originY = Y - draggedItem.DragCellOffsetY;
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("SlotDragPreview");
        bool hasPreview = InventoryInteractionService.PreviewPlacement(
            draggedItem.ItemData,
            originX,
            originY,
            context,
            out BackpackPlacementResult placement,
            out _);
        bool canPlace = !IsLocked && hasPreview && placement.CanPlace;
        string visualID = canPlace
            ? VisualAssetService.UIInventorySlotValidID
            : VisualAssetService.UIInventorySlotInvalidID;

        ResetAllSlotSkins();

        if (!hasPreview || placement.OccupiedCells == null || placement.OccupiedCells.Count == 0) {
            VisualUIHelper.ApplyInventorySlotSkin(_image, IsLocked, visualID);
            return canPlace;
        }

        bool highlightedAnyCell = false;
        foreach (int[] cell in placement.OccupiedCells) {
            if (cell == null || cell.Length < 2) {
                continue;
            }

            GridSlotUI slot = FindSlot(cell[0], cell[1]);
            if (slot == null) {
                continue;
            }

            if (slot._image == null) {
                slot._image = slot.GetComponent<Image>();
            }

            VisualUIHelper.ApplyInventorySlotSkin(slot._image, slot.IsLocked, visualID);
            highlightedAnyCell = true;
        }

        if (!highlightedAnyCell) {
            VisualUIHelper.ApplyInventorySlotSkin(_image, IsLocked, visualID);
        }

        return canPlace;
    }

    private void ApplyDefaultSkin() {
        if (_image == null) {
            _image = GetComponent<Image>();
        }

        VisualUIHelper.ApplyInventorySlotSkin(_image, IsLocked);
    }

    private static void ResetAllSlotSkins() {
        foreach (GridSlotUI slot in FindObjectsOfType<GridSlotUI>()) {
            if (slot != null) {
                slot.ApplyDefaultSkin();
            }
        }
    }

    private GridSlotUI FindSlot(int x, int y) {
        foreach (GridSlotUI slot in FindObjectsOfType<GridSlotUI>()) {
            if (slot != null && slot.X == x && slot.Y == y) {
                return slot;
            }
        }

        return null;
    }
}
