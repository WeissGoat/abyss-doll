using UnityEngine;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour {
    private enum GameScreenState {
        Workshop,
        DungeonMap,
        Combat,
        CombatLoot,
        SafeRoom,
        Settlement
    }

    public static GameFlowController Instance { get; private set; }

    public GameObject workshopPanel;
    public GameObject dungeonMapPanel;
    public GameObject combatPanel;
    public GameObject combatLootPanel;
    public GameObject safeRoomPanel;
    public GameObject settlementPanel;
    public GameObject testItemPrefab;
    public Transform inventoryItemLayer;
    
    private GameScreenState _currentScreen;
    private CombatLootPickupResult _pendingCombatLootResult;
    private DungeonSettlementResult _pendingSettlementResult;
    private bool _isDungeonMapInventoryOpen;

    void Awake() {
        Instance = this;
    }

    void Start() {
        // 延迟初始化，等待 GameRoot 和 Configs 加载完毕
        Invoke("InitGame", 1.0f);
    }

    void InitGame() {
        Debug.Log("[GameFlow] Initializing MVP Game Loop...");
        
        GameRoot.Core.CurrentPlayer.Money = 1500;
        var coreMaterial = ConfigManager.CreateItem("mat_core_tier1");
        if (coreMaterial != null) {
            GameRoot.Core.CurrentPlayer.StashInventory.Add(coreMaterial);
        }

        var myChassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
        if (GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid == null) {
            GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(myChassis);
        }
        FindObjectOfType<GridGenerator>().GenerateGrid(myChassis);
        EnsureInventoryItemLayer();
        EnsureCombatLootPanel();

        DungeonEventBus.OnLayerLoaded += EnterDungeonMap;
        DungeonEventBus.OnNodeResolutionFinished += EnterDungeonMap;
        DungeonEventBus.OnCombatLootPrepared += HandleCombatLootPrepared;
        DungeonEventBus.OnDungeonSettled += HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettlementPrepared += HandleDungeonSettlementPrepared;
        GameEventBus.OnItemPlaced += HandleItemPlaced;

        // 当进入具体的 Node 时切界面
        DungeonEventBus.OnNodeEntered += HandleNodeEntered;
        DungeonEventBus.OnSafeRoomEntered += HandleSafeRoomEntered;

        EnterWorkshop();
    }

    public void EnterWorkshop() {
        TransitionToScreen(GameScreenState.Workshop);
    }

    public void EnterDungeonMap() {
        TransitionToScreen(GameScreenState.DungeonMap);
    }

    public void EnterCombat() {
        TransitionToScreen(GameScreenState.Combat);
    }

    public void EnterCombatLoot(CombatLootPickupResult result) {
        TransitionToScreen(GameScreenState.CombatLoot, result);
    }
    
    public void EnterSafeRoom(SafeRoomNode node) {
        TransitionToScreen(GameScreenState.SafeRoom, node);
    }

    public void DepartToDungeon() {
        Debug.Log("[GameFlow] 玩家启程，加载深渊...");
        GameRoot.Core.Dungeon.LoadLayer(1);
    }

    public void OpenDungeonMapInventory() {
        if (_currentScreen != GameScreenState.DungeonMap) {
            return;
        }

        SetDungeonMapInventoryOpen(true, false);
    }

    public void CloseDungeonMapInventory() {
        SetDungeonMapInventoryOpen(false, true);
    }

    public bool CanStageRemovedBackpackItems() {
        return _currentScreen == GameScreenState.DungeonMap && _isDungeonMapInventoryOpen;
    }

    public Transform GetInventoryItemLayer() {
        EnsureInventoryItemLayer();
        return inventoryItemLayer;
    }

    private void HandleDungeonSettled(bool isVictory) {
        if (_pendingSettlementResult != null && settlementPanel != null) {
            QueueSettlementPresentation(_pendingSettlementResult);
            _pendingSettlementResult = null;
            return;
        }
        
        EnterWorkshop();
    }

    private void HandleDungeonSettlementPrepared(DungeonSettlementResult result) {
        _pendingSettlementResult = result;
    }

    private void HandleCombatLootPrepared(CombatLootPickupResult result) {
        _pendingCombatLootResult = result;
        EnterCombatLoot(result);
    }

    private void HandleNodeEntered(NodeBase node, int cost) {
        if (node is CombatNode) {
            EnterCombat();
        }
    }

    private void HandleSafeRoomEntered(NodeBase node) {
        EnterSafeRoom(node as SafeRoomNode);
    }

    private void HandleItemPlaced(string itemInstanceID, int x, int y) {
        if (string.IsNullOrEmpty(itemInstanceID)) {
            return;
        }

        if (TryFindItemUI(itemInstanceID, out _)) {
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

        if (placedItem == null || testItemPrefab == null) {
            return;
        }

        SpawnItemUIForGridItem(placedItem, x, y);
    }

    private void QueueSettlementPresentation(DungeonSettlementResult result) {
        if (result == null) {
            EnterWorkshop();
            return;
        }

        if (VisualQueue.IsHeadless) {
            ShowSettlement(result);
            return;
        }

        string transitionMessage = result.IsVictory
            ? "🎒 正在整理带出的物资..."
            : "💀 正在整理战败记录...";

        VisualQueue.Enqueue(new LogWaitCommand(transitionMessage, 0.8f));
        VisualQueue.Enqueue(new ActionCommand(() => {
            if (this != null) {
                ShowSettlement(result);
            }
        }));
    }

    private void ShowSettlement(DungeonSettlementResult result) {
        TransitionToScreen(GameScreenState.Settlement, result);
    }

    private void TransitionToScreen(GameScreenState nextScreen, object payload = null) {
        if (_currentScreen == GameScreenState.DungeonMap && nextScreen != GameScreenState.DungeonMap && _isDungeonMapInventoryOpen) {
            SetDungeonMapInventoryOpen(false, true, false);
        }

        _currentScreen = nextScreen;
        Debug.Log($"[GameFlow] 切换屏幕状态 -> {_currentScreen}");

        if (workshopPanel) workshopPanel.SetActive(nextScreen == GameScreenState.Workshop);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(nextScreen == GameScreenState.DungeonMap);
        if (combatPanel) combatPanel.SetActive(nextScreen == GameScreenState.Combat);
        if (combatLootPanel) combatLootPanel.SetActive(nextScreen == GameScreenState.CombatLoot);
        if (safeRoomPanel) safeRoomPanel.SetActive(nextScreen == GameScreenState.SafeRoom);
        if (settlementPanel) settlementPanel.SetActive(nextScreen == GameScreenState.Settlement);
        ApplyInventoryPresentationForCurrentScreen();

        switch (nextScreen) {
            case GameScreenState.Workshop:
                OnEnterWorkshopScreen();
                break;
            case GameScreenState.DungeonMap:
                OnEnterDungeonMapScreen();
                break;
            case GameScreenState.Combat:
                OnEnterCombatScreen();
                break;
            case GameScreenState.CombatLoot:
                OnEnterCombatLootScreen(payload as CombatLootPickupResult);
                break;
            case GameScreenState.SafeRoom:
                OnEnterSafeRoomScreen(payload as SafeRoomNode);
                break;
            case GameScreenState.Settlement:
                OnEnterSettlementScreen(payload as DungeonSettlementResult);
                break;
        }
    }

    private void OnEnterWorkshopScreen() {
        Debug.Log("[GameFlow] 进入局外工坊...");

        // 撤离回小镇后，恢复人偶的基本状态 (MVP 简化机制：回满血和SAN)
        var doll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (doll != null) {
            doll.Status.HP_Current = doll.Status.HP_Max;
            doll.Status.SAN_Current = doll.Status.SAN_Max;
            GameEventBus.PublishHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
            GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
        }

        var wsCtrl = workshopPanel?.GetComponent<WorkshopUIController>();
        if (wsCtrl != null) {
            wsCtrl.RefreshUI();
        }

        SyncInventoryItemUI();
    }

    private void OnEnterDungeonMapScreen() {
        Debug.Log("[GameFlow] 玩家在深渊地图中抉择路线...");
        var mapCtrl = dungeonMapPanel?.GetComponent<DungeonMapUIController>();
        if (mapCtrl != null) {
            mapCtrl.RefreshMap();
            mapCtrl.BindBackpackControls(this, _isDungeonMapInventoryOpen);
        }

        SyncInventoryItemUI();
    }

    private void OnEnterCombatScreen() {
        Debug.Log("[GameFlow] 进入战斗！");
        SyncInventoryItemUI();
    }

    private void OnEnterCombatLootScreen(CombatLootPickupResult result) {
        if (result == null) {
            Debug.LogWarning("[GameFlow] Combat loot screen requested without payload. Returning to DungeonMap.");
            EnterDungeonMap();
            return;
        }

        Debug.Log($"[GameFlow] 展示战利品拾取界面, OfferedCount={result.OfferedItems.Count}, EstimatedValue={result.TotalEstimatedValue}");
        SyncInventoryItemUI();

        EnsureCombatLootPanel();
        ConfigureCombatLootPanelInteraction();

        var lootCtrl = combatLootPanel?.GetComponent<CombatLootUIController>();
        if (lootCtrl != null) {
            lootCtrl.Present(result, testItemPrefab, () => {
                _pendingCombatLootResult = null;
                CombatNode currentCombatNode = GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode as CombatNode;
                if (currentCombatNode != null) {
                    currentCombatNode.ConfirmLootCollection();
                } else {
                    Debug.LogWarning("[GameFlow] Missing CombatNode while confirming combat loot. Falling back to generic node settlement completion.");
                    DungeonEventBus.PublishNodeSettlementCompleted();
                }
            });
        } else {
            Debug.LogWarning("[GameFlow] CombatLootPanel missing. Falling back to auto-confirm without UI interaction.");
            _pendingCombatLootResult = null;
            CombatNode currentCombatNode = GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode as CombatNode;
            if (currentCombatNode != null) {
                currentCombatNode.ConfirmLootCollection();
            } else {
                DungeonEventBus.PublishNodeSettlementCompleted();
            }
        }
    }

    private void OnEnterSafeRoomScreen(SafeRoomNode node) {
        Debug.Log("[GameFlow] 进入安全区！");
        var sfCtrl = safeRoomPanel?.GetComponent<SafeRoomUIController>();
        if (sfCtrl != null) {
            sfCtrl.Setup(node);
        }
    }

    private void OnEnterSettlementScreen(DungeonSettlementResult result) {
        if (result == null) {
            Debug.LogWarning("[GameFlow] Settlement screen requested without result payload. Returning to Workshop.");
            EnterWorkshop();
            return;
        }

        Debug.Log($"[GameFlow] 展示结算界面, Victory={result.IsVictory}, LootCount={result.LootTransferredCount}");
        var settlementCtrl = settlementPanel?.GetComponent<SettlementUIController>();
        if (settlementCtrl != null) {
            settlementCtrl.Present(result, EnterWorkshop);
        }
    }

    private void SyncInventoryItemUI() {
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        GridGenerator generator = FindObjectOfType<GridGenerator>();
        if (grid == null || generator == null || testItemPrefab == null) {
            return;
        }

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

    private void SpawnItemUIForGridItem(ItemEntity item, int x, int y) {
        if (item == null || testItemPrefab == null) {
            return;
        }

        GridGenerator generator = FindObjectOfType<GridGenerator>();
        Transform itemLayer = GetInventoryItemLayer();
        if (itemLayer == null || generator == null) {
            return;
        }

        Transform targetSlot = generator.GetSlot(x, y);
        if (targetSlot == null) {
            return;
        }

        GameObject itemGo = Instantiate(testItemPrefab, itemLayer);
        DraggableItemUI itemUI = itemGo.GetComponent<DraggableItemUI>();
        if (itemUI == null) {
            return;
        }

        itemUI.SetupData(item);
        itemUI.SnapToSlot(targetSlot, x, y);
        Debug.Log($"[GameFlow] Spawned UI for item {item.Name} at ({x},{y})");
    }

    private void SetDungeonMapInventoryOpen(bool isOpen, bool discardDetachedItems, bool refreshMapControls = true) {
        _isDungeonMapInventoryOpen = isOpen;

        if (!isOpen && discardDetachedItems) {
            DiscardDetachedBackpackItems();
        }

        ApplyInventoryPresentationForCurrentScreen();

        if (refreshMapControls) {
            RefreshDungeonMapInventoryControls();
        }

        Debug.Log(isOpen
            ? "[GameFlow] 深渊地图背包已展开。"
            : "[GameFlow] 深渊地图背包已收拢。");
    }

    private void RefreshDungeonMapInventoryControls() {
        var mapCtrl = dungeonMapPanel?.GetComponent<DungeonMapUIController>();
        if (mapCtrl != null) {
            mapCtrl.BindBackpackControls(this, _isDungeonMapInventoryOpen);
        }
    }

    private void ApplyInventoryPresentationForCurrentScreen() {
        bool shouldShowBackpack = _currentScreen == GameScreenState.Workshop
            || _currentScreen == GameScreenState.Combat
            || _currentScreen == GameScreenState.CombatLoot
            || (_currentScreen == GameScreenState.DungeonMap && _isDungeonMapInventoryOpen);

        GridGenerator generator = FindObjectOfType<GridGenerator>();
        if (generator?.gridParent != null) {
            generator.gridParent.gameObject.SetActive(shouldShowBackpack);
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
    }

    private void DiscardDetachedBackpackItems() {
        int discardedCount = 0;
        foreach (var itemUI in FindObjectsOfType<DraggableItemUI>()) {
            if (itemUI != null && itemUI.IsPendingDiscard) {
                discardedCount++;
                Destroy(itemUI.gameObject);
            }
        }

        if (discardedCount > 0) {
            Debug.Log($"[GameFlow] 丢弃了 {discardedCount} 件从局内背包移除的物品。");
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

    private void EnsureCombatLootPanel() {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) {
            Debug.LogWarning("[GameFlow] Cannot create CombatLootPanel because no Canvas was found.");
            return;
        }

        CombatLootUIController existingController = combatLootPanel != null
            ? combatLootPanel.GetComponent<CombatLootUIController>()
            : null;

        if (combatLootPanel != null && existingController != null) {
            return;
        }

        if (combatLootPanel != null && existingController == null) {
            Debug.LogWarning("[GameFlow] Existing CombatLootPanel reference has no CombatLootUIController. Rebuilding runtime fallback panel.");
            combatLootPanel.SetActive(false);
            combatLootPanel = null;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        combatLootPanel = new GameObject("CombatLootPanel_Runtime");
        combatLootPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = combatLootPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        combatLootPanel.SetActive(false);

        Image bg = combatLootPanel.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);
        bg.raycastTarget = false;

        CombatLootUIController lootCtrl = combatLootPanel.AddComponent<CombatLootUIController>();

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(combatLootPanel.transform, false);
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font = defaultFont;
        titleTxt.fontSize = 44;
        titleTxt.color = Color.white;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -60);
        titleRect.sizeDelta = new Vector2(600, 80);
        lootCtrl.titleText = titleTxt;

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(combatLootPanel.transform, false);
        Text summaryTxt = summaryObj.AddComponent<Text>();
        summaryTxt.font = defaultFont;
        summaryTxt.fontSize = 24;
        summaryTxt.color = new Color(0.95f, 0.95f, 0.95f);
        summaryTxt.alignment = TextAnchor.UpperCenter;
        summaryTxt.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0.5f, 1f);
        summaryRect.anchorMax = new Vector2(0.5f, 1f);
        summaryRect.pivot = new Vector2(0.5f, 1f);
        summaryRect.anchoredPosition = new Vector2(0, -140);
        summaryRect.sizeDelta = new Vector2(760, 120);
        lootCtrl.summaryText = summaryTxt;

        GameObject lootArea = new GameObject("LootArea");
        lootArea.transform.SetParent(combatLootPanel.transform, false);
        RectTransform lootAreaRect = lootArea.AddComponent<RectTransform>();
        lootAreaRect.anchorMin = Vector2.zero;
        lootAreaRect.anchorMax = Vector2.one;
        lootAreaRect.pivot = new Vector2(0.5f, 0.5f);
        lootAreaRect.anchoredPosition = Vector2.zero;
        lootAreaRect.sizeDelta = Vector2.zero;
        lootCtrl.lootParent = lootArea.transform;

        GameObject continueObj = new GameObject("Continue_Button");
        continueObj.transform.SetParent(combatLootPanel.transform, false);
        Image continueImg = continueObj.AddComponent<Image>();
        continueImg.color = new Color(0.9f, 0.58f, 0.18f);
        Button continueBtn = continueObj.AddComponent<Button>();
        RectTransform continueRect = continueObj.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.pivot = new Vector2(0.5f, 0f);
        continueRect.anchoredPosition = new Vector2(0, 90);
        continueRect.sizeDelta = new Vector2(320, 80);
        lootCtrl.continueBtn = continueBtn;

        GameObject continueTextObj = new GameObject("Text");
        continueTextObj.transform.SetParent(continueObj.transform, false);
        Text continueTxt = continueTextObj.AddComponent<Text>();
        continueTxt.font = defaultFont;
        continueTxt.fontSize = 30;
        continueTxt.color = Color.black;
        continueTxt.text = "确认拾取并继续";
        continueTxt.alignment = TextAnchor.MiddleCenter;
        continueTxt.raycastTarget = false;
        RectTransform continueTextRect = continueTextObj.GetComponent<RectTransform>();
        continueTextRect.anchorMin = Vector2.zero;
        continueTextRect.anchorMax = Vector2.one;
        continueTextRect.sizeDelta = Vector2.zero;

        Debug.Log("[GameFlow] Runtime fallback CombatLootPanel created.");
    }

    private void ConfigureCombatLootPanelInteraction() {
        if (combatLootPanel == null) {
            return;
        }

        Image panelImage = combatLootPanel.GetComponent<Image>();
        if (panelImage != null) {
            panelImage.raycastTarget = false;
        }

        foreach (var text in combatLootPanel.GetComponentsInChildren<Text>(true)) {
            if (text != null) {
                text.raycastTarget = false;
            }
        }
    }

    void OnDestroy() {
        DungeonEventBus.OnDungeonSettled -= HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettlementPrepared -= HandleDungeonSettlementPrepared;
        DungeonEventBus.OnLayerLoaded -= EnterDungeonMap;
        DungeonEventBus.OnNodeResolutionFinished -= EnterDungeonMap;
        DungeonEventBus.OnCombatLootPrepared -= HandleCombatLootPrepared;
        DungeonEventBus.OnNodeEntered -= HandleNodeEntered;
        DungeonEventBus.OnSafeRoomEntered -= HandleSafeRoomEntered;
        GameEventBus.OnItemPlaced -= HandleItemPlaced;
    }
}
