using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 挂载在物品预制体（如剑、药水）上
public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler {
    public ItemEntity ItemData { get; private set; }
    public bool IsPendingDiscard { get; private set; }
    
    private Vector3 _originalPosition;
    private Transform _originalParent;
    private CanvasGroup _canvasGroup;
    
    // 记录在后端的合法位置，防止拖拽失败时丢失
    private int _lastValidX = -1;
    private int _lastValidY = -1;
    private bool _wasInGrid = false;
    private Vector3 _dragOffset;

    // [新增] 用于网格对齐计算：当前抓取的相对单元格坐标偏移
    public int DragCellOffsetX { get; private set; }
    public int DragCellOffsetY { get; private set; }

    public void SetupData(ItemEntity itemData) {
        ItemData = itemData;
        _wasInGrid = false;
        IsPendingDiscard = false;
        
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // 根据后端 Shape 数据，动态计算 UI 尺寸和中心锚点(Pivot)
        if (ItemData.Grid != null && ItemData.Grid.Shape != null) {
            int maxX = 0;
            int maxY = 0;
            foreach (var p in ItemData.Grid.Shape) {
                if (p[0] > maxX) maxX = p[0];
                if (p[1] > maxY) maxY = p[1];
            }
            int cols = maxX + 1;
            int rows = maxY + 1;
            
            RectTransform rect = GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cols * 100, rows * 100);
            
            // 将 Pivot 设置为左上角第一个格子的中心点
            float pivotX = 0.5f / cols;
            float pivotY = 1.0f - (0.5f / rows);
            rect.pivot = new Vector2(pivotX, pivotY);
        }

        ApplyVisualStyle();
    }

    private void ApplyVisualStyle() {
        Image image = GetComponent<Image>();
        if (image != null && ItemData != null) {
            string iconID = VisualAssetService.ResolveItemIconID(ItemData);
            bool hasRegisteredIcon = VisualAssetService.TryGetSprite(iconID, out Sprite iconSprite);
            if (!hasRegisteredIcon) {
                iconSprite = VisualAssetService.GetSprite(iconID);
            }

            image.sprite = iconSprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;

            switch (ItemData.ItemType) {
                case nameof(ItemType.Weapon):
                    image.color = hasRegisteredIcon ? Color.white : new Color(0.75f, 0.18f, 0.18f, 1f);
                    break;
                case nameof(ItemType.Armor):
                    image.color = hasRegisteredIcon ? Color.white : new Color(0.35f, 0.45f, 0.7f, 1f);
                    break;
                case nameof(ItemType.Consumable):
                    image.color = hasRegisteredIcon ? Color.white : new Color(0.22f, 0.65f, 0.3f, 1f);
                    break;
                case nameof(ItemType.Loot):
                    image.color = hasRegisteredIcon ? Color.white : new Color(0.82f, 0.63f, 0.18f, 1f);
                    break;
                default:
                    image.color = hasRegisteredIcon ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f);
                    break;
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
                grid.PlaceItem(ItemData, _lastValidX, _lastValidY);
                GridSolver.RecalculateAllEffects(GameRoot.Core.CurrentPlayer.ActiveDoll);
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
    _originalPosition = transform.position;
    _originalParent = transform.parent;

    // [核心修复] 记录鼠标抓取点与物体真实位置的偏移量，防止抖动瞬移！
    _dragOffset = transform.position - (Vector3)eventData.position;
    
    // 计算网格格数偏移：当前点击的究竟是这个物品的哪一格？
    RectTransform rect = GetComponent<RectTransform>();
    RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
    // 因为 Anchor/Pivot 被我们设定在 (0.5/Cols, 1 - 0.5/Rows)
    // localPoint 是相对于 Pivot 的像素坐标，比如 100x100 的格子，往右一格是 +100，往下一格是 -100
    DragCellOffsetX = Mathf.RoundToInt(localPoint.x / 100f);
    DragCellOffsetY = Mathf.RoundToInt(-localPoint.y / 100f); // UI 坐标系 Y 轴向上，而网格是 Y 轴向下，因此取负

    transform.SetAsLastSibling();

    if (_canvasGroup != null) {
        _canvasGroup.blocksRaycasts = false;
    }

    BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
    if (grid != null && grid.ContainedItems != null && grid.ContainedItems.Contains(ItemData)) {
        _wasInGrid = true;
        _lastValidX = ItemData.Grid.CurrentPos[0];
        _lastValidY = ItemData.Grid.CurrentPos[1];

        // 从后端网格中拿走
        grid.RemoveItem(ItemData);
        GridSolver.RecalculateAllEffects(GameRoot.Core.CurrentPlayer.ActiveDoll);
        GameEventBus.PublishItemRemoved(ItemData.InstanceID);
    } else {
        _wasInGrid = false;
    }
}

public void OnDrag(PointerEventData eventData) {
    // 让物品跟着鼠标走，并保持抓取部位的偏移量
    transform.position = (Vector3)eventData.position + _dragOffset;
}

public void OnEndDrag(PointerEventData eventData) {
    if (_canvasGroup != null) {
        _canvasGroup.blocksRaycasts = true;
    }

    // 如果拖拽结束后，物品没有在后端的网格里（说明它被扔在了空地，或者放置失败了）
    BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
    if (grid == null || !grid.ContainedItems.Contains(ItemData)) {
        if (_wasInGrid && GameFlowController.Instance != null && GameFlowController.Instance.CanStageRemovedBackpackItems()) {
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
        
        _originalParent = transform.parent;
        _originalPosition = transform.position;
    }

    public void ReturnToOriginalPosition() {
        transform.SetParent(_originalParent);
        transform.position = _originalPosition;
        IsPendingDiscard = false;
        
        // 【关键修复】如果是从网格拿起来的但放置失败，必须把它重新注册回后端！
        if (_wasInGrid) {
            BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
            if (grid != null && !grid.ContainedItems.Contains(ItemData)) {
                grid.PlaceItem(ItemData, _lastValidX, _lastValidY);
                GridSolver.RecalculateAllEffects(GameRoot.Core.CurrentPlayer.ActiveDoll);
                GameEventBus.PublishItemPlaced(ItemData.InstanceID, _lastValidX, _lastValidY);
            }
        }
    }

    private void LeaveDetachedAtCurrentPosition() {
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
}
