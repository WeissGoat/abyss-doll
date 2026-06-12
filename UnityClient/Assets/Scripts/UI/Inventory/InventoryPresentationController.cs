using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum InventoryPresentationMode {
    Hidden,
    Workshop,
    DungeonMap,
    Combat,
    CombatLoot,
    SafeRoom,
    Stairs
}

public class InventoryPresentationController : MonoBehaviour {
    public static InventoryPresentationController Active { get; private set; }

    public event Action ItemPresentationChanged;

    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform inventoryItemLayer;

    private InventoryPresentationMode _mode = InventoryPresentationMode.Hidden;
    private bool _isDungeonMapInventoryOpen;
    private Coroutine _deferredSyncRoutine;
    private Image _inventoryChassisPanel;

    public bool CanStageDiscard =>
        (_mode == InventoryPresentationMode.DungeonMap && _isDungeonMapInventoryOpen)
        || _mode == InventoryPresentationMode.CombatLoot;

    void Awake() {
        Active = this;
    }

    void OnEnable() {
        GameEventBus.OnItemPlaced += HandleItemPlaced;
        GameEventBus.OnItemRemoved += HandleItemRemoved;
    }

    void OnDisable() {
        GameEventBus.OnItemPlaced -= HandleItemPlaced;
        GameEventBus.OnItemRemoved -= HandleItemRemoved;

        if (_deferredSyncRoutine != null) {
            StopCoroutine(_deferredSyncRoutine);
            _deferredSyncRoutine = null;
        }
    }

    void OnDestroy() {
        if (Active == this) {
            Active = null;
        }
    }

    public void Configure(GameObject itemPrefabRef, Transform itemLayerRef = null) {
        itemPrefab = itemPrefabRef;
        if (itemLayerRef != null) {
            inventoryItemLayer = itemLayerRef;
        }

        EnsureInventoryItemLayer();
    }

    public Transform GetItemLayer() {
        EnsureInventoryItemLayer();
        return inventoryItemLayer;
    }

    public void SetContext(InventoryPresentationMode mode, bool isDungeonMapInventoryOpen = false) {
        _mode = mode;
        _isDungeonMapInventoryOpen = isDungeonMapInventoryOpen;
        ApplyCurrentPresentation();
    }

    public void EnsureGridGenerated() {
        GridGenerator generator = FindObjectOfType<GridGenerator>();
        EnsureInventoryGridGenerated(generator);
    }

    public void SyncNow() {
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        GridGenerator generator = FindObjectOfType<GridGenerator>();
        if (grid == null || generator == null || itemPrefab == null) {
            return;
        }

        EnsureInventoryGridGenerated(generator);
        RemoveStaleInventoryItemUI(grid);

        foreach (ItemEntity item in grid.ContainedItems) {
            if (item?.Grid?.CurrentPos == null || item.Grid.CurrentPos.Length < 2) {
                continue;
            }

            if (TryFindItemUI(item.InstanceID, out var existingUI)) {
                Transform slot = generator.GetSlot(item.Grid.CurrentPos[0], item.Grid.CurrentPos[1]);
                if (slot != null) {
                    existingUI.SnapToSlot(slot, item.Grid.CurrentPos[0], item.Grid.CurrentPos[1]);
                }
                continue;
            }

            SpawnItemUIForGridItem(item, item.Grid.CurrentPos[0], item.Grid.CurrentPos[1]);
        }
    }

    public void QueueDeferredSync() {
        if (!isActiveAndEnabled) {
            return;
        }

        if (_deferredSyncRoutine != null) {
            StopCoroutine(_deferredSyncRoutine);
        }

        _deferredSyncRoutine = StartCoroutine(DeferredInventorySyncRoutine());
    }

    public void DiscardDetachedItems() {
        int discardedCount = 0;
        foreach (var itemUI in FindObjectsOfType<DraggableItemUI>()) {
            if (itemUI != null && itemUI.IsPendingDiscard) {
                ItemLifecycleService.MarkDetachedItemLost(itemUI.ItemData, "InventoryPresentationDiscard", out _);
                discardedCount++;
                DestroyRuntimeObject(itemUI.gameObject);
            }
        }

        if (discardedCount > 0) {
            Debug.Log($"[InventoryPresentation] Discarded {discardedCount} detached backpack item UI objects.");
        }
    }

    private IEnumerator DeferredInventorySyncRoutine() {
        yield return null;

        Canvas.ForceUpdateCanvases();
        SyncNow();
        _deferredSyncRoutine = null;
    }

    private void HandleItemPlaced(string itemInstanceID, int x, int y) {
        if (string.IsNullOrEmpty(itemInstanceID)) {
            return;
        }

        if (TryFindItemUI(itemInstanceID, out _)) {
            ItemPresentationChanged?.Invoke();
            return;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null) {
            return;
        }

        ItemEntity placedItem = grid.GetItemAt(x, y);
        if (placedItem == null || placedItem.InstanceID != itemInstanceID) {
            placedItem = grid.ContainedItems.Find(item => item != null && item.InstanceID == itemInstanceID);
        }

        if (placedItem == null || itemPrefab == null) {
            return;
        }

        SpawnItemUIForGridItem(placedItem, x, y);
        ItemPresentationChanged?.Invoke();
    }

    private void HandleItemRemoved(string itemInstanceID) {
        if (string.IsNullOrEmpty(itemInstanceID)) {
            return;
        }

        if (TryFindItemUI(itemInstanceID, out var existingUI) && existingUI != null && !existingUI.IsDragging()) {
            DestroyRuntimeObject(existingUI.gameObject);
        }

        ItemPresentationChanged?.Invoke();
    }

    private void SpawnItemUIForGridItem(ItemEntity item, int x, int y) {
        if (item == null || itemPrefab == null) {
            return;
        }

        GridGenerator generator = FindObjectOfType<GridGenerator>();
        Transform itemLayer = GetItemLayer();
        if (itemLayer == null || generator == null) {
            return;
        }

        Transform targetSlot = generator.GetSlot(x, y);
        if (targetSlot == null) {
            return;
        }

        GameObject itemGo = Instantiate(itemPrefab, itemLayer);
        DraggableItemUI itemUI = itemGo.GetComponent<DraggableItemUI>();
        if (itemUI == null) {
            return;
        }

        itemUI.SetupData(item);
        itemUI.SnapToSlot(targetSlot, x, y);
        Debug.Log($"[InventoryPresentation] Spawned UI for item {item.Name} at ({x},{y}).");
    }

    public void ApplyCurrentPresentation() {
        bool shouldShowBackpack = ShouldShowBackpack();

        GridGenerator generator = FindObjectOfType<GridGenerator>();
        if (generator?.gridParent != null) {
            EnsureInventoryGridGenerated(generator);
            generator.gridParent.gameObject.SetActive(shouldShowBackpack);
            ApplyInventoryGridLayout(generator);
        }

        if (!shouldShowBackpack && _inventoryChassisPanel != null) {
            _inventoryChassisPanel.gameObject.SetActive(false);
        }

        EnsureInventoryItemLayer();
        if (inventoryItemLayer != null) {
            CanvasGroup canvasGroup = inventoryItemLayer.GetComponent<CanvasGroup>();
            if (canvasGroup == null) {
                canvasGroup = inventoryItemLayer.gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.alpha = shouldShowBackpack ? 1f : 0f;
            canvasGroup.interactable = shouldShowBackpack;
            canvasGroup.blocksRaycasts = shouldShowBackpack;
        }

        if (shouldShowBackpack) {
            PositionInventoryForCurrentScreen(generator);
            BringInventoryLayersToFront(generator);
        }
    }

    private bool ShouldShowBackpack() {
        return _mode == InventoryPresentationMode.Combat
            || _mode == InventoryPresentationMode.CombatLoot
            || _mode == InventoryPresentationMode.SafeRoom
            || _mode == InventoryPresentationMode.Stairs
            || (_mode == InventoryPresentationMode.DungeonMap && _isDungeonMapInventoryOpen);
    }

    private void ApplyInventoryGridLayout(GridGenerator generator) {
        if (generator?.gridParent == null) {
            return;
        }

        GridLayoutGroup layoutGroup = generator.gridParent.GetComponent<GridLayoutGroup>();
        if (layoutGroup != null) {
            InventoryDisplaySpec.ApplyGridLayout(layoutGroup);
        }

        RectTransform gridRect = generator.gridParent as RectTransform;
        RectTransform layerRect = inventoryItemLayer as RectTransform;
        if (gridRect != null && layerRect != null) {
            layerRect.anchorMin = gridRect.anchorMin;
            layerRect.anchorMax = gridRect.anchorMax;
            layerRect.pivot = gridRect.pivot;
            layerRect.anchoredPosition = gridRect.anchoredPosition;
            layerRect.sizeDelta = gridRect.sizeDelta;
            layerRect.localScale = gridRect.localScale;
        }
    }

    private void EnsureInventoryGridGenerated(GridGenerator generator) {
        if (generator?.gridParent == null || generator.gridParent.childCount > 0) {
            return;
        }

        ChassisComponent chassis = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.Chassis;
        if (chassis == null) {
            return;
        }

        generator.GenerateGrid(chassis);
    }

    private void PositionInventoryForCurrentScreen(GridGenerator generator) {
        if (generator?.gridParent == null) {
            return;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        Vector2 gridSize = ResolveInventoryGridSize(grid);
        InventoryLayoutProfile profile = InventoryDisplaySpec.ResolveLayoutProfile(_mode);
        Vector2 position = profile.AnchoredPosition;
        float inventoryScale = profile.Scale;

        RectTransform gridRect = generator.gridParent as RectTransform;
        if (gridRect != null) {
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = position;
            gridRect.sizeDelta = gridSize;
            gridRect.localScale = Vector3.one * inventoryScale;
        }

        EnsureInventoryChassisPanel(generator, gridSize, position);

        RectTransform layerRect = inventoryItemLayer as RectTransform;
        if (layerRect != null && gridRect != null) {
            layerRect.anchorMin = gridRect.anchorMin;
            layerRect.anchorMax = gridRect.anchorMax;
            layerRect.pivot = gridRect.pivot;
            layerRect.anchoredPosition = gridRect.anchoredPosition;
            layerRect.sizeDelta = gridRect.sizeDelta;
            layerRect.localScale = gridRect.localScale;
        }
    }

    private Vector2 ResolveInventoryGridSize(BackpackGrid grid) {
        int width = Mathf.Max(1, grid?.Width ?? 4);
        int height = Mathf.Max(1, grid?.Height ?? 4);
        return InventoryDisplaySpec.ResolveGridSize(width, height);
    }

    private void EnsureInventoryChassisPanel(GridGenerator generator, Vector2 gridSize, Vector2 position) {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null || generator?.gridParent == null) {
            return;
        }

        if (_inventoryChassisPanel == null) {
            Transform existing = canvas.transform.Find("InventoryChassisPanel");
            if (existing != null) {
                _inventoryChassisPanel = existing.GetComponent<Image>();
            }
        }

        if (_inventoryChassisPanel == null) {
            GameObject panelObj = new GameObject("InventoryChassisPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            _inventoryChassisPanel = panelObj.AddComponent<Image>();
        }

        RectTransform panelRect = _inventoryChassisPanel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = position;
        panelRect.sizeDelta = InventoryDisplaySpec.ResolveChassisPanelSize(gridSize);
        panelRect.localScale = Vector3.one * InventoryDisplaySpec.ResolveLayoutProfile(_mode).Scale;

        VisualUIHelper.ApplyContainSprite(
            _inventoryChassisPanel,
            VisualAssetService.UIInventoryChassisPanelID,
            panelRect.sizeDelta,
            Color.white,
            new Color(0.08f, 0.075f, 0.065f, 0.9f),
            false);

        _inventoryChassisPanel.gameObject.SetActive(generator.gridParent.gameObject.activeSelf);
        _inventoryChassisPanel.transform.SetSiblingIndex(Mathf.Max(0, generator.gridParent.GetSiblingIndex()));
    }

    private void BringInventoryLayersToFront(GridGenerator generator) {
        if (_inventoryChassisPanel != null && generator?.gridParent != null) {
            _inventoryChassisPanel.transform.SetSiblingIndex(Mathf.Max(0, generator.gridParent.GetSiblingIndex()));
        }

        if (generator?.gridParent != null) {
            generator.gridParent.SetAsLastSibling();
        }

        if (inventoryItemLayer != null) {
            inventoryItemLayer.SetAsLastSibling();
        }
    }

    private bool TryFindItemUI(string itemInstanceID, out DraggableItemUI foundItemUI) {
        foundItemUI = null;
        if (string.IsNullOrEmpty(itemInstanceID)) {
            return false;
        }

        foreach (var itemUI in FindObjectsOfType<DraggableItemUI>()) {
            if (itemUI != null && itemUI.ItemData != null && itemUI.ItemData.InstanceID == itemInstanceID) {
                foundItemUI = itemUI;
                return true;
            }
        }

        return false;
    }

    private void EnsureInventoryItemLayer() {
        if (inventoryItemLayer != null) {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) {
            return;
        }

        Transform existingLayer = canvas.transform.Find("InventoryItemLayer");
        if (existingLayer != null) {
            inventoryItemLayer = existingLayer;
        } else {
            GameObject layer = new GameObject("InventoryItemLayer");
            layer.transform.SetParent(canvas.transform, false);
            RectTransform layerRect = layer.AddComponent<RectTransform>();
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.sizeDelta = Vector2.zero;
            inventoryItemLayer = layer.transform;
        }

        if (inventoryItemLayer.GetComponent<CanvasGroup>() == null) {
            inventoryItemLayer.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void RemoveStaleInventoryItemUI(BackpackGrid grid) {
        foreach (var itemUI in FindObjectsOfType<DraggableItemUI>()) {
            if (itemUI == null || itemUI.ItemData == null || itemUI.IsPendingDiscard) {
                continue;
            }

            if (inventoryItemLayer != null && itemUI.transform.parent != inventoryItemLayer) {
                continue;
            }

            if (!grid.ContainedItems.Contains(itemUI.ItemData)) {
                DestroyRuntimeObject(itemUI.gameObject);
            }
        }
    }

    private void DestroyRuntimeObject(GameObject target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(target);
        } else {
            DestroyImmediate(target);
        }
    }
}
