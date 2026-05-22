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

        DraggableItemUI draggedItem = ResolveDraggedItem(eventData);
        if (draggedItem != null) {
            ApplyDragPreviewSkin(draggedItem);
            return;
        }

        VisualUIHelper.ApplyInventorySlotSkin(
            _image,
            IsLocked,
            IsLocked ? VisualAssetService.UIInventorySlotLockedID : VisualAssetService.UIInventorySlotHoverID);
    }

    public void OnPointerExit(PointerEventData eventData) {
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
        Debug.LogWarning($"[UI] Cannot place item {draggedItem.ItemData.Name} at ({originX}, {originY}). {placeReason}");
    }

    private DraggableItemUI ResolveDraggedItem(PointerEventData eventData) {
        return eventData?.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DraggableItemUI>()
            : null;
    }

    private void ApplyDragPreviewSkin(DraggableItemUI draggedItem) {
        if (draggedItem == null || draggedItem.ItemData == null) {
            ApplyDefaultSkin();
            return;
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
            return;
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
    }

    private void ApplyDefaultSkin() {
        if (_image == null) {
            _image = GetComponent<Image>();
        }

        VisualUIHelper.ApplyInventorySlotSkin(_image, IsLocked);
    }

    private void ResetAllSlotSkins() {
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
