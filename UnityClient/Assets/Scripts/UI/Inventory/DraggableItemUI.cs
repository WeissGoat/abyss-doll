using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 挂载在物品预制体（如剑、药水）上
public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler {
    private const float CellSize = 100f;
    private const KeyCode RotateHeldItemKey = KeyCode.R;

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

    private void Update() {
        if (!_isDragging || !Input.GetKeyDown(RotateHeldItemKey)) {
            return;
        }

        TryRotateHeldItem(Input.mousePosition, _dragEventCamera);
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

        rect.sizeDelta = new Vector2(cols * CellSize, rows * CellSize);
        rect.pivot = new Vector2(0.5f / cols, 1.0f - (0.5f / rows));

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
        int offsetX = Mathf.RoundToInt(localPoint.x / CellSize);
        int offsetY = Mathf.RoundToInt(-localPoint.y / CellSize);

        if (ItemData?.Grid != null && BackpackGrid.TryGetRotatedBounds(ItemData, ItemData.Grid.Rotation, out int cols, out int rows)) {
            offsetX = Mathf.Clamp(offsetX, 0, cols - 1);
            offsetY = Mathf.Clamp(offsetY, 0, rows - 1);
        }

        DragCellOffsetX = offsetX;
        DragCellOffsetY = offsetY;
    }

    private void TryRotateHeldItem(Vector2 screenPosition, Camera eventCamera) {
        if (ItemData == null) {
            return;
        }

        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DragRotate");
        if (!InventoryInteractionService.RequestRotateHeldItem(ItemData, BackpackGrid.RotationStep, context, out int newRotation, out string reason)) {
            if (!string.IsNullOrEmpty(reason)) {
                Debug.LogWarning($"[UI] {reason}");
            }
            return;
        }

        ApplyGridFootprint();
        UpdateDragCellOffset(screenPosition, eventCamera);
        Debug.Log($"[UI] 旋转物品 {ItemData.Name} 到 {newRotation} 度。");
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Debug.Log($"[DraggableItemUI] 鼠标悬停进入: {gameObject.name}");
    }

    public void OnPointerDown(PointerEventData eventData) {
        Debug.Log($"[DraggableItemUI] 鼠标按下: {gameObject.name}");
    }

    public void OnPointerClick(PointerEventData eventData) {
        Debug.Log($"[UI] 你点击了 {ItemData.Name}");
        
        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        
        // 因为深渊结算或者切换地图时，底层的 RuntimeGrid 实例会被 new 重置！
        // 如果 UI 记录的 _wasInGrid 为真，但底层不包含它，我们需要自动尝试修复或报错
        if (grid == null || !grid.ContainedItems.Contains(ItemData)) {
            // 如果它在 UI 上仍然显示在格子里，可能需要将其重新放置进去
            if (_wasInGrid && grid != null) {
                Debug.LogWarning($"[UI] 自动修复：将失联的武器【{ItemData.Name}】重新注册到网格 ({_lastValidX},{_lastValidY})。");
                InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("ItemClickAutoRepair");
                if (!InventoryInteractionService.RequestPlace(ItemData, _lastValidX, _lastValidY, context, out string repairReason)) {
                    Debug.LogWarning($"[UI] 自动修复失败：{repairReason}");
                    return;
                }
            } else {
                Debug.LogWarning($"[UI] 武器【{ItemData.Name}】还没有被放入背包网格！请先将它拖入网格中才能在战斗里使用！");
                return;
            }
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
public void OnBeginDrag(PointerEventData eventData) {
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

    // [核心修复] 记录鼠标抓取点与物体真实位置的偏移量，防止抖动瞬移！
    _dragOffset = transform.position - (Vector3)eventData.position;
    
    // 计算网格格数偏移：当前点击的究竟是这个物品的哪一格？
    UpdateDragCellOffset(eventData.position, eventData.pressEventCamera);

    transform.SetAsLastSibling();

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
    // 让物品跟着鼠标走，并保持抓取部位的偏移量
    transform.position = (Vector3)eventData.position + _dragOffset;
}

public void OnEndDrag(PointerEventData eventData) {
    _isDragging = false;

    if (_canvasGroup != null) {
        _canvasGroup.blocksRaycasts = true;
    }

    // 如果拖拽结束后，物品没有在后端的网格里（说明它被扔在了空地，或者放置失败了）
    BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
    if (grid == null || !grid.ContainedItems.Contains(ItemData)) {
        bool allowStageDiscard = GameFlowController.Instance != null && GameFlowController.Instance.CanStageRemovedBackpackItems();
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DragEnd", allowStageDiscard);
        if (_wasInGrid && InventoryInteractionService.RequestStageDiscard(ItemData, context, out _)) {
            LeaveDetachedAtCurrentPosition();
        } else {
            ReturnToOriginalPosition();
        }
    }
}

    public void SnapToSlot(Transform newParentSlot, int gridX, int gridY) {
        Transform itemLayer = GameFlowController.Instance != null ? GameFlowController.Instance.GetInventoryItemLayer() : null;
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
        _isDragging = false;
        transform.SetParent(_originalParent);
        transform.position = _originalPosition;
        IsPendingDiscard = false;
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

    private void LeaveDetachedAtCurrentPosition() {
        _isDragging = false;
        Transform itemLayer = GameFlowController.Instance != null ? GameFlowController.Instance.GetInventoryItemLayer() : null;
        if (itemLayer != null) {
            transform.SetParent(itemLayer);
        }

        transform.SetAsLastSibling();
        _originalParent = transform.parent;
        _originalPosition = transform.position;
        IsPendingDiscard = true;
        Debug.Log($"[UI] 物品 {ItemData.Name} 已从背包中取出，关闭背包时将被丢弃。");
    }

    private void RestoreDragStartRotationIfNeeded() {
        if (ItemData?.Grid == null) {
            return;
        }

        ItemData.Grid.Rotation = _dragStartRotation;
        ApplyGridFootprint();
    }
}
