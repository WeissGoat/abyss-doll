using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 挂载在物品预制体（如剑、药水）上
public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler {
    private const KeyCode RotateHeldItemKey = KeyCode.R;

    // A held item draws above every panel and modal (540) but under the hover card (ItemTooltip.SortingOrder).
    public const int HeldSortingOrder = 900;
    // The held item covers the very cells its drop preview colours, so it is see-through while held.
    public const float HeldAlpha = 0.7f;

    public ItemEntity ItemData { get; private set; }
    public bool IsPendingDiscard { get; private set; }
    
    private Vector3 _originalPosition;
    private Transform _originalParent;
    private CanvasGroup _canvasGroup;
    private bool _isDragging;
    
    // 记录在后端的合法位置，防止拖拽失败时丢失
    private int _lastValidX = -1;
    private int _lastValidY = -1;
    private bool _wasInGrid = false;
    private InventoryItemPlacement _lastPlacement = InventoryItemPlacement.Missing;
    private Vector3 _dragOffset;
    private Camera _dragEventCamera;
    private int _dragStartRotation;
    private bool _pendingDiscardAtDragStart;
    private Canvas _heldCanvas;
    private GridSlotUI[] _previewSlots;
    private GridSlotUI _previewSlot;
    private int _previewKey = int.MinValue;

    // [新增] 用于网格对齐计算：当前抓取的相对单元格坐标偏移
    public int DragCellOffsetX { get; private set; }
    public int DragCellOffsetY { get; private set; }

    public void SetupData(ItemEntity itemData) {
        ItemData = itemData;
        _wasInGrid = false;
        _lastPlacement = InventoryItemPlacement.Missing;
        IsPendingDiscard = false;
        
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        ApplyGridFootprint();

        ApplyVisualStyle();
    }

    // Rotation: while dragging press R or right click; on an item lying in the backpack (or the loot tray) right click.
    private void Update() {
        if (!_isDragging) {
            return;
        }

        if (Input.GetKeyDown(RotateHeldItemKey) || Input.GetMouseButtonDown(1)) {
            TryRotateHeldItem(Input.mousePosition, _dragEventCamera);
        }

        UpdatePlacementPreview(Input.mousePosition);
    }

    // Colours the cells the item would take if dropped now: green when it fits, red when it does not. Worked out from
    // the pointer position every frame, so it follows rotation and works over cells other items cover (the slot under
    // them never gets a pointer-enter).
    public void UpdatePlacementPreview(Vector2 screenPosition) {
        GridSlotUI hovered = FindSlotUnder(screenPosition);
        int rotation = ItemData?.Grid != null ? BackpackGrid.NormalizeRotation(ItemData.Grid.Rotation) : 0;
        int key = hovered == null ? -1 : (((hovered.X * 64 + hovered.Y) * 8 + rotation / 90) * 64 + DragCellOffsetX) * 64 + DragCellOffsetY;
        if (key == _previewKey && hovered == _previewSlot) {
            return;
        }

        _previewKey = key;
        _previewSlot = hovered;
        if (hovered == null) {
            GridSlotUI.ClearPreview();
        } else {
            hovered.ShowPlacementPreview(this);
        }
    }

    public GridSlotUI PreviewSlot => _previewSlot;

    private GridSlotUI FindSlotUnder(Vector2 screenPosition) {
        if (_previewSlots == null) {
            _previewSlots = FindObjectsOfType<GridSlotUI>();
        }

        // The nearest slot whose cell (grown by half the gap) holds the pointer, so the preview does not blink off
        // in the spacing between cells.
        GridSlotUI best = null;
        float bestDistance = float.MaxValue;
        Vector3[] corners = new Vector3[4];
        foreach (GridSlotUI slot in _previewSlots) {
            if (slot == null || !slot.isActiveAndEnabled) {
                continue;
            }

            ((RectTransform)slot.transform).GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(_dragEventCamera, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(_dragEventCamera, corners[2]);
            Vector2 margin = (max - min) * 0.1f;
            if (screenPosition.x < min.x - margin.x || screenPosition.x > max.x + margin.x
                || screenPosition.y < min.y - margin.y || screenPosition.y > max.y + margin.y) {
                continue;
            }

            float distance = (screenPosition - (min + max) * 0.5f).sqrMagnitude;
            if (distance < bestDistance) {
                bestDistance = distance;
                best = slot;
            }
        }

        return best;
    }

    private void EndPlacementPreview() {
        if (_previewSlot != null || _previewKey != int.MinValue) {
            GridSlotUI.ClearPreview();
        }

        _previewSlots = null;
        _previewSlot = null;
        _previewKey = int.MinValue;
    }

    private void ApplyVisualStyle() {
        Image image = GetComponent<Image>();
        if (image == null) {
            image = gameObject.AddComponent<Image>();
        }

        if (image != null && ItemData != null) {
            string iconID = VisualAssetService.ResolveItemIconID(ItemData);
            bool hasRegisteredIcon = VisualAssetService.TryGetSprite(iconID, out Sprite iconSprite);
            if (!hasRegisteredIcon) {
                iconSprite = VisualAssetService.GetSprite(iconID);
            }

            VisualUIHelper.ApplySlicedSprite(
                image,
                VisualAssetService.UIPanelInfoID,
                ResolveItemTint(ItemData, false),
                ResolveItemTint(ItemData, false),
                true);

            Image iconImage = EnsureIconImage();
            if (iconImage != null) {
                iconImage.sprite = iconSprite;
                iconImage.type = Image.Type.Simple;
                iconImage.preserveAspect = true;
                iconImage.color = hasRegisteredIcon ? Color.white : ResolveItemTint(ItemData, true);
                iconImage.raycastTarget = false;
                ApplyIconRotation(iconImage.rectTransform);
            }
        }

        Text label = GetComponentInChildren<Text>();
        if (label != null && ItemData != null) {
            string secondLine = ItemPresentationRules.BuildCompactSummary(ItemData);

            label.text = string.IsNullOrEmpty(secondLine)
                ? ItemData.Name
                : $"{ItemData.Name}\n{secondLine}";
        }
    }

    private Image EnsureIconImage() {
        Transform existing = transform.Find("Icon_Image");
        GameObject iconObject = existing != null ? existing.gameObject : new GameObject("Icon_Image");
        if (existing == null) {
            iconObject.transform.SetParent(transform, false);
        }

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        if (iconRect == null) {
            iconRect = iconObject.AddComponent<RectTransform>();
        }

        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        iconRect.localScale = Vector3.one;

        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage == null) {
            iconImage = iconObject.AddComponent<Image>();
        }

        iconObject.transform.SetAsFirstSibling();
        return iconImage;
    }

    private Color ResolveItemTint(ItemEntity item, bool forIcon) {
        float alpha = forIcon ? 0.95f : 0.82f;
        switch (item.ItemType) {
            case nameof(ItemType.Weapon):
                return new Color(0.75f, 0.18f, 0.18f, alpha);
            case nameof(ItemType.Armor):
                return new Color(0.35f, 0.45f, 0.7f, alpha);
            case nameof(ItemType.Consumable):
                return new Color(0.22f, 0.65f, 0.3f, alpha);
            case nameof(ItemType.Loot):
                return new Color(0.82f, 0.63f, 0.18f, alpha);
            default:
                return new Color(0.45f, 0.45f, 0.45f, alpha);
        }
    }

    private void ApplyGridFootprint() {
        if (ItemData?.Grid == null) {
            return;
        }

        if (!BackpackGrid.TryGetRotatedBounds(ItemData, ItemData.Grid.Rotation, out int cols, out int rows)) {
            return;
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) {
            return;
        }

        rect.sizeDelta = InventoryDisplaySpec.ResolveItemSize(cols, rows);
        rect.pivot = InventoryDisplaySpec.ResolveItemPivot(cols, rows);

        Image iconImage = transform.Find("Icon_Image")?.GetComponent<Image>();
        if (iconImage != null) {
            ApplyIconRotation(iconImage.rectTransform);
        }
    }

    private void ApplyIconRotation(RectTransform iconRect) {
        if (iconRect == null || ItemData?.Grid == null) {
            return;
        }

        int rotation = BackpackGrid.NormalizeRotation(ItemData.Grid.Rotation);
        iconRect.localRotation = Quaternion.Euler(0f, 0f, -rotation);
    }

    private void UpdateDragCellOffset(Vector2 screenPosition, Camera eventCamera) {
        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) {
            DragCellOffsetX = 0;
            DragCellOffsetY = 0;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPosition, eventCamera, out Vector2 localPoint);
        Vector2Int offset = InventoryDisplaySpec.ResolveDragCellOffset(localPoint, ItemData);
        DragCellOffsetX = offset.x;
        DragCellOffsetY = offset.y;
    }

    private void TryRotateHeldItem(Vector2 screenPosition, Camera eventCamera) {
        if (ItemData == null) {
            return;
        }

        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DragRotate");
        if (!InventoryInteractionService.RequestRotateHeldItem(ItemData, BackpackGrid.RotationStep, context, out int newRotation, out string reason)) {
            if (!string.IsNullOrEmpty(reason)) {
                PlayerToast.Show(BackpackGrid.CanRotateItem(ItemData) ? reason : $"{ItemData.Name}不能旋转。", PlayerToastKind.Warning);
            }
            return;
        }

        ApplyGridFootprint();
        UpdateDragCellOffset(screenPosition, eventCamera);
        Debug.Log($"[UI] 旋转物品 {ItemData.Name} 到 {newRotation} 度。");
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (_isDragging || ItemData == null || (eventData != null && eventData.dragging)) {
            return;
        }

        ItemTooltip.Show(ItemData, (RectTransform)transform);
    }

    public void OnPointerExit(PointerEventData eventData) {
        ItemTooltip.HideFor((RectTransform)transform);
    }

    private void OnDisable() {
        ItemTooltip.HideFor(transform as RectTransform);
        SetHeldOnTop(false);
        if (_isDragging) {
            EndPlacementPreview();
        }
    }

    // The backpack grid and its item layer are the canvas's last siblings, so an item dragged in from the loot tray or
    // another panel used to slide under the grid. While held, a nested canvas lifts it above everything without
    // reparenting (which would break the return-to-tray paths); the canvas is switched off again on release.
    private void SetHeldOnTop(bool onTop) {
        if (_canvasGroup != null) {
            _canvasGroup.alpha = onTop ? HeldAlpha : 1f;
        }

        if (!onTop) {
            if (_heldCanvas != null) {
                _heldCanvas.overrideSorting = false;
                _heldCanvas.enabled = false;
            }
            return;
        }

        if (transform.parent == null || transform.parent.GetComponentInParent<Canvas>() == null) {
            return;
        }

        if (_heldCanvas == null) {
            _heldCanvas = GetComponent<Canvas>();
            if (_heldCanvas == null) {
                _heldCanvas = gameObject.AddComponent<Canvas>();
            }
        }

        _heldCanvas.enabled = true;
        _heldCanvas.overrideSorting = true;
        _heldCanvas.sortingOrder = HeldSortingOrder;
    }

    public bool IsHeldOnTop => _heldCanvas != null && _heldCanvas.enabled && _heldCanvas.overrideSorting;

    public void OnPointerDown(PointerEventData eventData) {
        Debug.Log($"[DraggableItemUI] 鼠标按下: {gameObject.name}");
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (ItemData == null) {
            Debug.LogWarning("[UI] 点击了未绑定物品数据的背包物品 UI。");
            return;
        }

        if (eventData != null && eventData.button == PointerEventData.InputButton.Right) {
            RotateFromClick(eventData);
            return;
        }

        Debug.Log($"[UI] 你点击了 {ItemData.Name}");
        
        // On the pre-dive loadout screen a click sends the item back to the stash instead of using it.
        if (PreDiveLoadoutUIController.Active != null && PreDiveLoadoutUIController.Active.HandleBackpackItemClicked(ItemData)) {
            return;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(ItemData)) {
            Debug.LogWarning($"[UI] 物品【{ItemData.Name}】当前不在背包网格中，无法使用。请先将它放入背包。");
            return;
        }

        if (!ItemUseService.TryUseItem(ItemData, out string failureReason)) {
            if (!string.IsNullOrEmpty(failureReason)) {
                Debug.LogWarning($"[UI] {failureReason}");
            }
        } else {
            string usageHint = ItemPresentationRules.BuildUseHint(ItemData);
            if (!string.IsNullOrEmpty(usageHint)) {
                Debug.Log($"[UI] {usageHint}");
            }
        }
    }
    private void RotateFromClick(PointerEventData eventData) {
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(ItemData)) {
            TryRotateHeldItem(eventData.position, eventData.pressEventCamera);
            return;
        }

        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("ClickRotate");
        if (!InventoryInteractionService.RequestRotateInPlace(ItemData, context, out InventoryItemPlacement placement, out string reason)) {
            PlayerToast.Show(reason, PlayerToastKind.Warning);
            return;
        }

        GridGenerator generator = FindObjectOfType<GridGenerator>();
        Transform slot = generator != null ? generator.GetSlot(placement.X, placement.Y) : null;
        if (slot != null) {
            SnapToSlot(slot, placement.X, placement.Y);
        } else {
            ApplyGridFootprint();
        }
    }

public void OnBeginDrag(PointerEventData eventData) {
    if (eventData != null && eventData.button != PointerEventData.InputButton.Left) {
        return;
    }

    if (ItemData == null) {
        Debug.LogError("[DraggableItemUI] ItemData is null! SetupData was not called properly.");
        return;
    }

    Debug.Log($"[UI] 开始拖拽 {ItemData.Name}");
    _isDragging = true;
    _originalPosition = transform.position;
    _originalParent = transform.parent;
    _dragEventCamera = eventData.pressEventCamera;
    _dragStartRotation = ItemData.Grid != null ? BackpackGrid.NormalizeRotation(ItemData.Grid.Rotation) : 0;
    _pendingDiscardAtDragStart = IsPendingDiscard;

    // [核心修复] 记录鼠标抓取点与物体真实位置的偏移量，防止抖动瞬移！
    _dragOffset = transform.position - (Vector3)eventData.position;
    
    // 计算网格格数偏移：当前点击的究竟是这个物品的哪一格？
    UpdateDragCellOffset(eventData.position, eventData.pressEventCamera);

    transform.SetAsLastSibling();
    SetHeldOnTop(true);
    ItemTooltip.Hide();
    _previewSlots = null;
    _previewSlot = null;
    _previewKey = int.MinValue;

    if (_canvasGroup != null) {
        _canvasGroup.blocksRaycasts = false;
    }

    InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DragBegin");
    if (InventoryInteractionService.RequestPickUp(ItemData, context, out InventoryItemPlacement placement, out string pickUpReason)) {
        _wasInGrid = true;
        _lastPlacement = placement;
        _lastValidX = placement.X;
        _lastValidY = placement.Y;
    } else {
        _wasInGrid = false;
        _lastPlacement = InventoryItemPlacement.Missing;
        if (!string.IsNullOrEmpty(pickUpReason)) {
            Debug.LogWarning($"[UI] {pickUpReason}");
        }
    }
}

public void OnDrag(PointerEventData eventData) {
    if (!_isDragging) {
        return;
    }

    // 让物品跟着鼠标走，并保持抓取部位的偏移量
    transform.position = (Vector3)eventData.position + _dragOffset;
}

public void OnEndDrag(PointerEventData eventData) {
    // Always take raycasts back: a failed slot drop has already ended the drag (ReturnToOriginalPosition), and
    // leaving them off made the item impossible to pick up again.
    if (_canvasGroup != null) {
        _canvasGroup.blocksRaycasts = true;
    }

    SetHeldOnTop(false);
    GridSlotUI previewSlot = _previewSlot;
    EndPlacementPreview();
    if (!_isDragging) {
        return;
    }

    // No slot took the drop (released in the gap between cells or over another item), but the preview showed a
    // cell: drop there, so what the player saw is what happens.
    BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
    if (previewSlot != null && grid != null && !grid.ContainedItems.Contains(ItemData) && previewSlot.TryPlace(this)) {
        _isDragging = false;
        return;
    }

    _isDragging = false;

    // 如果拖拽结束后，物品没有在后端的网格里（说明它被扔在了空地，或者放置失败了）
    if (grid == null || !grid.ContainedItems.Contains(ItemData)) {
        bool allowStageDiscard = CanStageDiscard();
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DragEnd", allowStageDiscard);
        if (InventoryInteractionService.RequestStageDiscard(ItemData, context, out _)) {
            // Where the screen allows it (loot pickup, the map's backpack) an item dropped outside the backpack stays
            // where it landed. Taken out of the backpack it is discarded when the screen closes; loot that never went
            // in stays unclaimed in the loot tray.
            LeaveDetachedAtCurrentPosition(_wasInGrid || _pendingDiscardAtDragStart);
        } else {
            ReturnToOriginalPosition();
        }
    }
}

    public void SnapToSlot(Transform newParentSlot, int gridX, int gridY) {
        Transform itemLayer = ResolveInventoryItemLayer();
        if (itemLayer != null) {
            transform.SetParent(itemLayer);
        } else {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) {
                transform.SetParent(canvas.transform);
            } else {
                transform.SetParent(newParentSlot.parent.parent);
            }
        }
        
        transform.position = newParentSlot.position;
        transform.SetAsLastSibling();
        
        _wasInGrid = true;
        IsPendingDiscard = false;
        _lastValidX = gridX;
        _lastValidY = gridY;
        _lastPlacement = new InventoryItemPlacement {
            WasInGrid = true,
            X = gridX,
            Y = gridY,
            Rotation = ItemData?.Grid != null ? BackpackGrid.NormalizeRotation(ItemData.Grid.Rotation) : 0
        };
        ApplyGridFootprint();
        
        _originalParent = transform.parent;
        _originalPosition = transform.position;
    }

    public void ReturnToOriginalPosition() {
        GameAudio.PlaySfx("error");
        _isDragging = false;
        transform.SetParent(_originalParent);
        transform.position = _originalPosition;
        // Back in the backpack it is kept; an item that was already lying outside keeps its discard mark.
        IsPendingDiscard = !_wasInGrid && _pendingDiscardAtDragStart;
        if (_canvasGroup != null) {
            _canvasGroup.blocksRaycasts = true;
        }

        RestoreDragStartRotationIfNeeded();
        
        // 【关键修复】如果是从网格拿起来的但放置失败，必须把它重新注册回后端！
        if (_wasInGrid) {
            BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
            if (grid != null && !grid.ContainedItems.Contains(ItemData)) {
                InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DragCancel");
                if (!InventoryInteractionService.RequestRestore(ItemData, _lastPlacement, context, out string restoreReason)) {
                    Debug.LogWarning($"[UI] 无法将 {ItemData.Name} 放回原背包位置：{restoreReason}");
                } else {
                    ApplyGridFootprint();
                }
            }
        }
    }

    public bool IsDragging() {
        return _isDragging;
    }

    private void LeaveDetachedAtCurrentPosition(bool takenOutOfBackpack) {
        _isDragging = false;
        if (takenOutOfBackpack) {
            Transform itemLayer = ResolveInventoryItemLayer();
            if (itemLayer != null) {
                transform.SetParent(itemLayer);
            }
        }

        // Keep the item's centre on screen so it can always be picked up again.
        Vector3 position = transform.position;
        transform.position = new Vector3(Mathf.Clamp(position.x, 0f, Screen.width), Mathf.Clamp(position.y, 0f, Screen.height), position.z);
        transform.SetAsLastSibling();
        _originalParent = transform.parent;
        _originalPosition = transform.position;
        IsPendingDiscard = takenOutOfBackpack;
        if (takenOutOfBackpack) {
            Debug.Log($"[UI] 物品 {ItemData.Name} 已从背包中取出，关闭背包时将被丢弃。");
        }
    }

    private bool CanStageDiscard() {
        if (InventoryPresentationController.Active != null) {
            return InventoryPresentationController.Active.CanStageDiscard;
        }

        return GameFlowController.Instance != null && GameFlowController.Instance.CanStageRemovedBackpackItems();
    }

    private Transform ResolveInventoryItemLayer() {
        if (InventoryPresentationController.Active != null) {
            return InventoryPresentationController.Active.GetItemLayer();
        }

        return GameFlowController.Instance != null ? GameFlowController.Instance.GetInventoryItemLayer() : null;
    }

    private void RestoreDragStartRotationIfNeeded() {
        if (ItemData?.Grid == null) {
            return;
        }

        ItemData.Grid.Rotation = _dragStartRotation;
        ApplyGridFootprint();
    }
}
