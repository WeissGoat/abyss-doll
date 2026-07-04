using UnityEngine;
using UnityEngine.UI;

public class GameFlowController : MonoBehaviour {
    private enum GameScreenState {
        Workshop,
        DungeonMap,
        Combat,
        CombatLoot,
        NodeResolution,
        SafeRoom,
        Stairs,
        Settlement
    }

    public static GameFlowController Instance { get; private set; }

    public GameObject workshopPanel;
    public GameObject dungeonMapPanel;
    public GameObject combatPanel;
    public GameObject combatLootPanel;
    public GameObject dungeonNodeResultPanel;
    public GameObject safeRoomPanel;
    public GameObject settlementPanel;
    public GameObject testItemPrefab;
    public Transform inventoryItemLayer;
    [SerializeField] private bool grantDebugStartResources = false;
    [SerializeField] private int debugStartMoney = 1500;
    [SerializeField] private string debugStartItemID = "mat_core_tier1";
    private bool playPrologueOnStartup = true;
    
    private GameScreenState _currentScreen;
    private CombatLootPickupResult _pendingCombatLootResult;
    private DungeonSettlementResult _pendingSettlementResult;
    private bool _isDungeonMapInventoryOpen;
    private InventoryPresentationController _inventoryPresentation;
    private PrologueFirstDiveController _prologueFirstDiveController;
    private bool _initialized;

    void Awake() {
        BindInstance();
    }

    void OnEnable() {
        BindInstance();
    }

    void Start() {
        BindInstance();
        if (GameRoot.IsCoreReady()) {
            InitGame();
        } else {
            Invoke(nameof(InitGame), 1.0f);
        }
    }

    private void BindInstance() {
        if (Instance != this) {
            Instance = this;
        }
    }

    void InitGame() {
        if (_initialized) {
            return;
        }

        if (!GameRoot.IsCoreReady()) {
            Invoke(nameof(InitGame), 0.25f);
            return;
        }

        _initialized = true;
        Debug.Log("[GameFlow] Initializing MVP Game Loop...");

        if (grantDebugStartResources) {
            GameRoot.Core.CurrentPlayer.Money = debugStartMoney;
            var debugItem = ConfigManager.CreateItem(debugStartItemID);
            if (debugItem != null) {
                GameRoot.Core.CurrentPlayer.StashInventory.Add(debugItem);
            }
        }

        var myChassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
        if (GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid == null) {
            GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(myChassis);
        }
        FindObjectOfType<GridGenerator>().GenerateGrid(myChassis);
        EnsureInventoryPresentation();
        EnsureCombatLootPanel();
        EnsureSettlementPanel();

        DungeonEventBus.OnLayerLoaded += EnterDungeonMap;
        DungeonEventBus.OnNodeResolutionFinished += EnterDungeonMap;
        DungeonEventBus.OnDungeonNodeResolutionPrepared += HandleDungeonNodeResolutionPrepared;
        DungeonEventBus.OnCombatLootPrepared += HandleCombatLootPrepared;
        DungeonEventBus.OnDungeonSettled += HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettlementPrepared += HandleDungeonSettlementPrepared;
        // 当进入具体的 Node 时切界面
        DungeonEventBus.OnNodeEntered += HandleNodeEntered;
        DungeonEventBus.OnSafeRoomEntered += HandleSafeRoomEntered;
        DungeonEventBus.OnStairsEntered += HandleStairsEntered;

        EnterWorkshop();
        StartPrologueOpeningIfNeeded();
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

    public void EnterNodeResolution(DungeonNodeResolutionResult result) {
        TransitionToScreen(GameScreenState.NodeResolution, result);
    }

    public void EnterSettlementPreview(DungeonSettlementResult result) {
        TransitionToScreen(GameScreenState.Settlement, result);
    }
    
    public void EnterSafeRoom(SafeRoomNode node) {
        TransitionToScreen(GameScreenState.SafeRoom, node);
    }

    public void EnterStairs(StairsNode node) {
        TransitionToScreen(GameScreenState.Stairs, node);
    }

    public void DepartToDungeon() {
        Debug.Log("[GameFlow] 玩家启程，加载深渊...");
        GameRoot.Core.Dungeon.LoadLayer(1);
    }

    public void EnsurePrologueOpeningForRuntime() {
        StartPrologueOpeningIfNeeded();
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
        EnsureInventoryPresentation();
        return _inventoryPresentation != null
            ? _inventoryPresentation.CanStageDiscard
            : (_currentScreen == GameScreenState.DungeonMap && _isDungeonMapInventoryOpen)
                || _currentScreen == GameScreenState.CombatLoot;
    }

    public Transform GetInventoryItemLayer() {
        EnsureInventoryPresentation();
        inventoryItemLayer = _inventoryPresentation != null ? _inventoryPresentation.GetItemLayer() : inventoryItemLayer;
        return inventoryItemLayer;
    }

    private void EnsureInventoryPresentation() {
        if (_inventoryPresentation == null) {
            _inventoryPresentation = FindObjectOfType<InventoryPresentationController>();
        }

        if (_inventoryPresentation == null) {
            _inventoryPresentation = gameObject.AddComponent<InventoryPresentationController>();
        }

        _inventoryPresentation.ItemPresentationChanged -= HandleInventoryPresentationChanged;
        _inventoryPresentation.ItemPresentationChanged += HandleInventoryPresentationChanged;
        _inventoryPresentation.Configure(testItemPrefab, inventoryItemLayer);
        inventoryItemLayer = _inventoryPresentation.GetItemLayer();
    }

    private void StartPrologueOpeningIfNeeded() {
        Debug.Log($"[GameFlow] Prologue startup check. enabled={playPrologueOnStartup}");
        if (!playPrologueOnStartup) {
            return;
        }

        PrologueFirstDiveController controller = EnsurePrologueFirstDiveController();
        if (controller == null) {
            Debug.LogWarning("[GameFlow] Prologue startup requested, but PrologueFirstDiveController could not be created.");
            return;
        }

        PrologueFirstDiveReentryStage reentryStage = controller.ApplyReentryState();
        Debug.Log($"[GameFlow] Prologue startup controller ready. reentry={reentryStage}, object={controller.gameObject.name}");
        if (reentryStage == PrologueFirstDiveReentryStage.OpeningBlackScreen) {
            controller.PlayOpeningToNo0Found();
        }
    }

    private PrologueFirstDiveController EnsurePrologueFirstDiveController() {
        if (_prologueFirstDiveController == null) {
            _prologueFirstDiveController = GetComponent<PrologueFirstDiveController>();
        }

        if (_prologueFirstDiveController == null) {
            _prologueFirstDiveController = gameObject.AddComponent<PrologueFirstDiveController>();
            Debug.Log("[GameFlow] Runtime PrologueFirstDiveController attached to GameManager.");
        }

        if (_prologueFirstDiveController.targetCanvas == null) {
            Canvas canvas = workshopPanel != null
                ? workshopPanel.GetComponentInParent<Canvas>()
                : FindObjectOfType<Canvas>();
            _prologueFirstDiveController.targetCanvas = canvas;
        }

        if (_prologueFirstDiveController.workshopController == null) {
            _prologueFirstDiveController.workshopController = workshopPanel != null
                ? workshopPanel.GetComponent<WorkshopUIController>()
                : FindObjectOfType<WorkshopUIController>();
        }

        _prologueFirstDiveController.playOpeningOnStart = false;
        _prologueFirstDiveController.presentDepartureWithRuntimeOverlay = true;
        return _prologueFirstDiveController;
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

    private void HandleDungeonNodeResolutionPrepared(DungeonNodeResolutionResult result) {
        EnterNodeResolution(result);
    }

    private void HandleNodeEntered(NodeBase node, int cost) {
        if (node is CombatNode) {
            EnterCombat();
        }
    }

    private void HandleSafeRoomEntered(NodeBase node) {
        EnterSafeRoom(node as SafeRoomNode);
    }

    private void HandleStairsEntered(StairsNode node) {
        EnterStairs(node);
    }

    private void HandleInventoryPresentationChanged() {
        RefreshSafeRoomItemHintsIfNeeded();
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

        EnsureRuntimePanelForScreen(nextScreen);

        _currentScreen = nextScreen;
        Debug.Log($"[GameFlow] 切换屏幕状态 -> {_currentScreen}");

        if (workshopPanel) workshopPanel.SetActive(nextScreen == GameScreenState.Workshop);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(nextScreen == GameScreenState.DungeonMap);
        if (combatPanel) combatPanel.SetActive(nextScreen == GameScreenState.Combat || nextScreen == GameScreenState.CombatLoot);
        if (combatLootPanel) combatLootPanel.SetActive(nextScreen == GameScreenState.CombatLoot);
        if (dungeonNodeResultPanel) dungeonNodeResultPanel.SetActive(nextScreen == GameScreenState.NodeResolution);
        if (safeRoomPanel) safeRoomPanel.SetActive(nextScreen == GameScreenState.SafeRoom || nextScreen == GameScreenState.Stairs);
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
            case GameScreenState.NodeResolution:
                OnEnterNodeResolutionScreen(payload as DungeonNodeResolutionResult);
                break;
            case GameScreenState.SafeRoom:
                OnEnterSafeRoomScreen(payload as SafeRoomNode);
                break;
            case GameScreenState.Stairs:
                OnEnterStairsScreen(payload as StairsNode);
                break;
            case GameScreenState.Settlement:
                OnEnterSettlementScreen(payload as DungeonSettlementResult);
                break;
        }

        ApplyInventoryPresentationForCurrentScreen();
        QueueDeferredInventorySync();
    }

    private void EnsureRuntimePanelForScreen(GameScreenState nextScreen) {
        switch (nextScreen) {
            case GameScreenState.CombatLoot:
                EnsureCombatLootPanel();
                break;
            case GameScreenState.NodeResolution:
                EnsureDungeonNodeResultPanel();
                break;
            case GameScreenState.Settlement:
                EnsureSettlementPanel();
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
        QueueDeferredInventorySync();
    }

    private void OnEnterDungeonMapScreen() {
        Debug.Log("[GameFlow] 玩家在深渊地图中抉择路线...");
        var mapCtrl = dungeonMapPanel?.GetComponent<DungeonMapUIController>();
        if (mapCtrl != null) {
            mapCtrl.RefreshMap();
            mapCtrl.BindBackpackControls(this, _isDungeonMapInventoryOpen);
        }

        SyncInventoryItemUI();
        QueueDeferredInventorySync();
    }

    private void OnEnterCombatScreen() {
        Debug.Log("[GameFlow] 进入战斗！");
        SetCombatLootOverlayMode(false);
        SyncInventoryItemUI();
    }

    private void OnEnterCombatLootScreen(CombatLootPickupResult result) {
        if (result == null) {
            Debug.LogWarning("[GameFlow] Combat loot screen requested without payload. Returning to DungeonMap.");
            EnterDungeonMap();
            return;
        }

        Debug.Log($"[GameFlow] 展示战利品拾取界面, OfferedCount={result.OfferedItems.Count}, EstimatedValue={result.TotalEstimatedValue}");
        SetCombatLootOverlayMode(true);
        SyncInventoryItemUI();

        EnsureCombatLootPanel();
        if (combatLootPanel != null) {
            combatLootPanel.SetActive(true);
        }
        ConfigureCombatLootPanelInteraction();

        var lootCtrl = combatLootPanel?.GetComponent<CombatLootUIController>();
        if (lootCtrl != null) {
            lootCtrl.Present(result, testItemPrefab, () => {
                _pendingCombatLootResult = null;
                DiscardDetachedBackpackItems();
                ILootPickupNode currentLootNode = GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode as ILootPickupNode;
                if (currentLootNode != null) {
                    currentLootNode.ConfirmLootCollection();
                } else {
                    Debug.LogWarning("[GameFlow] Missing loot pickup node while confirming combat loot. Falling back to generic node settlement completion.");
                    DungeonEventBus.PublishNodeSettlementCompleted();
                }
            });
        } else {
            Debug.LogWarning("[GameFlow] CombatLootPanel missing. Falling back to auto-confirm without UI interaction.");
            _pendingCombatLootResult = null;
            DiscardDetachedBackpackItems();
            ILootPickupNode currentLootNode = GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode as ILootPickupNode;
            if (currentLootNode != null) {
                currentLootNode.ConfirmLootCollection();
            } else {
                DungeonEventBus.PublishNodeSettlementCompleted();
            }
        }
    }

    private void SetCombatLootOverlayMode(bool active) {
        HUDController hud = combatPanel != null ? combatPanel.GetComponent<HUDController>() : null;
        if (hud != null) {
            hud.SetLootOverlayMode(active);
        }
    }

    private void OnEnterNodeResolutionScreen(DungeonNodeResolutionResult result) {
        if (result == null) {
            Debug.LogWarning("[GameFlow] Node resolution screen requested without payload. Returning to DungeonMap.");
            EnterDungeonMap();
            return;
        }

        EnsureDungeonNodeResultPanel();
        if (dungeonNodeResultPanel != null) {
            dungeonNodeResultPanel.SetActive(true);
        }

        var resultCtrl = dungeonNodeResultPanel?.GetComponent<DungeonNodeResultUIController>();
        if (resultCtrl != null) {
            resultCtrl.Present(result, () => {
                DungeonEventBus.PublishNodeSettlementCompleted();
            });
        } else {
            Debug.LogWarning("[GameFlow] DungeonNodeResultPanel missing. Falling back to node settlement completion.");
            DungeonEventBus.PublishNodeSettlementCompleted();
        }
    }

    private void OnEnterSafeRoomScreen(SafeRoomNode node) {
        Debug.Log("[GameFlow] 进入安全区！");
        var sfCtrl = safeRoomPanel?.GetComponent<SafeRoomUIController>();
        if (sfCtrl != null) {
            sfCtrl.Setup(node);
        }
        SyncInventoryItemUI();
        QueueDeferredInventorySync();
    }

    private void OnEnterStairsScreen(StairsNode node) {
        Debug.Log("[GameFlow] Entered stairs room. Waiting for descend/return choice.");
        var sfCtrl = safeRoomPanel?.GetComponent<SafeRoomUIController>();
        if (sfCtrl != null) {
            sfCtrl.Setup(node);
        }
        SyncInventoryItemUI();
        QueueDeferredInventorySync();
    }

    private void OnEnterSettlementScreen(DungeonSettlementResult result) {
        if (result == null) {
            Debug.LogWarning("[GameFlow] Settlement screen requested without result payload. Returning to Workshop.");
            EnterWorkshop();
            return;
        }

        Debug.Log($"[GameFlow] 展示结算界面, Victory={result.IsVictory}, LootCount={result.LootTransferredCount}");
        EnsureSettlementPanel();
        var settlementCtrl = settlementPanel?.GetComponent<SettlementUIController>();
        if (settlementCtrl != null) {
            settlementPanel.SetActive(true);
            settlementPanel.transform.SetAsLastSibling();
            CombatOutcomeReport combatReport = TryGetDefeatCombatOutcomeForSettlement(result);
            if (combatReport != null) {
                settlementCtrl.Present(combatReport, EnterWorkshop);
            } else {
                settlementCtrl.Present(result, EnterWorkshop);
            }
        } else {
            Debug.LogWarning("[GameFlow] SettlementPanel missing. Falling back to Workshop without UI interaction.");
            EnterWorkshop();
        }
    }

    private CombatOutcomeReport TryGetDefeatCombatOutcomeForSettlement(DungeonSettlementResult result) {
        if (result == null || result.IsVictory) {
            return null;
        }

        CombatOutcomeReport report = GameRoot.Core?.Combat?.LastOutcomeReport;
        if (report == null || report.OutcomeType != CombatOutcomeType.Defeat) {
            return null;
        }

        return report;
    }

    private void SyncInventoryItemUI() {
        EnsureInventoryPresentation();
        _inventoryPresentation?.SyncNow();
    }

    private void QueueDeferredInventorySync() {
        EnsureInventoryPresentation();
        _inventoryPresentation?.QueueDeferredSync();
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
        EnsureInventoryPresentation();
        _inventoryPresentation?.SetContext(ResolveInventoryPresentationMode(), _isDungeonMapInventoryOpen);
        inventoryItemLayer = _inventoryPresentation != null ? _inventoryPresentation.GetItemLayer() : inventoryItemLayer;
    }

    private InventoryPresentationMode ResolveInventoryPresentationMode() {
        switch (_currentScreen) {
            case GameScreenState.Workshop:
                return InventoryPresentationMode.Workshop;
            case GameScreenState.DungeonMap:
                return InventoryPresentationMode.DungeonMap;
            case GameScreenState.Combat:
                return InventoryPresentationMode.Combat;
            case GameScreenState.CombatLoot:
                return InventoryPresentationMode.CombatLoot;
            case GameScreenState.SafeRoom:
                return InventoryPresentationMode.SafeRoom;
            case GameScreenState.Stairs:
                return InventoryPresentationMode.Stairs;
            default:
                return InventoryPresentationMode.Hidden;
        }
    }

    private void DiscardDetachedBackpackItems() {
        EnsureInventoryPresentation();
        _inventoryPresentation?.DiscardDetachedItems();
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
        VisualUIHelper.ApplyCoverSprite(
            bg,
            VisualAssetService.CombatBackgroundID,
            new Color(0.42f, 0.38f, 0.32f, 0.96f),
            new Color(0.012f, 0.014f, 0.013f, 0.96f));
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
        continueObj.AddComponent<Image>();
        Button continueBtn = continueObj.AddComponent<Button>();
        VisualUIHelper.ApplyButtonSkin(continueBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.9f, 0.58f, 0.18f));
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

    private void EnsureDungeonNodeResultPanel() {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) {
            Debug.LogWarning("[GameFlow] Cannot create DungeonNodeResultPanel because no Canvas was found.");
            return;
        }

        DungeonNodeResultUIController existingController = dungeonNodeResultPanel != null
            ? dungeonNodeResultPanel.GetComponent<DungeonNodeResultUIController>()
            : null;

        if (dungeonNodeResultPanel != null && existingController != null) {
            return;
        }

        if (dungeonNodeResultPanel != null && existingController == null) {
            Debug.LogWarning("[GameFlow] Existing DungeonNodeResultPanel reference has no DungeonNodeResultUIController. Rebuilding runtime fallback panel.");
            dungeonNodeResultPanel.SetActive(false);
            dungeonNodeResultPanel = null;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dungeonNodeResultPanel = new GameObject("DungeonNodeResultPanel_Runtime");
        dungeonNodeResultPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = dungeonNodeResultPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        dungeonNodeResultPanel.SetActive(false);

        Image bg = dungeonNodeResultPanel.AddComponent<Image>();
        VisualUIHelper.ApplyCoverSprite(
            bg,
            VisualAssetService.DefaultDungeonMapBackgroundID,
            new Color(0.3f, 0.28f, 0.23f, 0.96f),
            new Color(0.025f, 0.028f, 0.03f, 0.96f));
        bg.raycastTarget = false;

        DungeonNodeResultUIController resultCtrl = dungeonNodeResultPanel.AddComponent<DungeonNodeResultUIController>();

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(dungeonNodeResultPanel.transform, false);
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
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(760f, 90f);
        resultCtrl.titleText = titleTxt;

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(dungeonNodeResultPanel.transform, false);
        Text summaryTxt = summaryObj.AddComponent<Text>();
        summaryTxt.font = defaultFont;
        summaryTxt.fontSize = 28;
        summaryTxt.color = new Color(0.94f, 0.94f, 0.86f, 1f);
        summaryTxt.alignment = TextAnchor.UpperCenter;
        summaryTxt.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0.5f, 0.5f);
        summaryRect.anchorMax = new Vector2(0.5f, 0.5f);
        summaryRect.pivot = new Vector2(0.5f, 0.5f);
        summaryRect.anchoredPosition = new Vector2(0f, 40f);
        summaryRect.sizeDelta = new Vector2(780f, 360f);
        resultCtrl.summaryText = summaryTxt;

        GameObject continueObj = new GameObject("Continue_Button");
        continueObj.transform.SetParent(dungeonNodeResultPanel.transform, false);
        continueObj.AddComponent<Image>();
        Button continueBtn = continueObj.AddComponent<Button>();
        VisualUIHelper.ApplyButtonSkin(continueBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.76f, 0.5f, 0.18f));
        RectTransform continueRect = continueObj.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.pivot = new Vector2(0.5f, 0f);
        continueRect.anchoredPosition = new Vector2(0f, 120f);
        continueRect.sizeDelta = new Vector2(300f, 78f);
        resultCtrl.continueBtn = continueBtn;

        GameObject continueTextObj = new GameObject("Text");
        continueTextObj.transform.SetParent(continueObj.transform, false);
        Text continueTxt = continueTextObj.AddComponent<Text>();
        continueTxt.font = defaultFont;
        continueTxt.fontSize = 30;
        continueTxt.color = Color.black;
        continueTxt.alignment = TextAnchor.MiddleCenter;
        continueTxt.raycastTarget = false;
        RectTransform continueTextRect = continueTextObj.GetComponent<RectTransform>();
        continueTextRect.anchorMin = Vector2.zero;
        continueTextRect.anchorMax = Vector2.one;
        continueTextRect.sizeDelta = Vector2.zero;

        Debug.Log("[GameFlow] Runtime fallback DungeonNodeResultPanel created.");
    }

    private void RefreshSafeRoomItemHintsIfNeeded() {
        if (_currentScreen != GameScreenState.SafeRoom) {
            return;
        }

        var safeRoomCtrl = safeRoomPanel?.GetComponent<SafeRoomUIController>();
        if (safeRoomCtrl != null) {
            safeRoomCtrl.RefreshItemHints();
        }
    }

    private void EnsureSettlementPanel() {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) {
            Debug.LogWarning("[GameFlow] Cannot create SettlementPanel because no Canvas was found.");
            return;
        }

        SettlementUIController existingController = settlementPanel != null
            ? settlementPanel.GetComponent<SettlementUIController>()
            : null;

        if (settlementPanel != null && existingController != null) {
            return;
        }

        if (settlementPanel != null && existingController == null) {
            Debug.LogWarning("[GameFlow] Existing SettlementPanel reference has no SettlementUIController. Rebuilding runtime fallback panel.");
            settlementPanel.SetActive(false);
            settlementPanel = null;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        settlementPanel = new GameObject("SettlementPanel_Runtime");
        settlementPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = settlementPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        settlementPanel.SetActive(false);

        Image bg = settlementPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.1f, 0.92f);

        SettlementUIController settlementCtrl = settlementPanel.AddComponent<SettlementUIController>();

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(settlementPanel.transform, false);
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font = defaultFont;
        titleTxt.fontSize = 48;
        titleTxt.color = Color.white;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -80);
        titleRect.sizeDelta = new Vector2(540, 90);
        settlementCtrl.titleText = titleTxt;

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(settlementPanel.transform, false);
        Text summaryTxt = summaryObj.AddComponent<Text>();
        summaryTxt.font = defaultFont;
        summaryTxt.fontSize = 28;
        summaryTxt.color = Color.white;
        summaryTxt.alignment = TextAnchor.UpperCenter;
        summaryTxt.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0.5f, 1f);
        summaryRect.anchorMax = new Vector2(0.5f, 1f);
        summaryRect.pivot = new Vector2(0.5f, 1f);
        summaryRect.anchoredPosition = new Vector2(0, -180);
        summaryRect.sizeDelta = new Vector2(760, 150);
        settlementCtrl.summaryText = summaryTxt;

        GameObject lootObj = new GameObject("Loot_Text");
        lootObj.transform.SetParent(settlementPanel.transform, false);
        Text lootTxt = lootObj.AddComponent<Text>();
        lootTxt.font = defaultFont;
        lootTxt.fontSize = 24;
        lootTxt.color = new Color(1f, 0.92f, 0.5f);
        lootTxt.alignment = TextAnchor.UpperLeft;
        lootTxt.raycastTarget = false;
        RectTransform lootRect = lootObj.GetComponent<RectTransform>();
        lootRect.anchorMin = new Vector2(0.5f, 0.5f);
        lootRect.anchorMax = new Vector2(0.5f, 0.5f);
        lootRect.pivot = new Vector2(0.5f, 0.5f);
        lootRect.anchoredPosition = new Vector2(0, -20);
        lootRect.sizeDelta = new Vector2(760, 280);
        settlementCtrl.lootText = lootTxt;

        GameObject continueObj = new GameObject("Continue_Button");
        continueObj.transform.SetParent(settlementPanel.transform, false);
        Image continueImg = continueObj.AddComponent<Image>();
        continueImg.color = new Color(0.25f, 0.6f, 0.95f);
        Button continueBtn = continueObj.AddComponent<Button>();
        RectTransform continueRect = continueObj.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0f);
        continueRect.anchorMax = new Vector2(0.5f, 0f);
        continueRect.pivot = new Vector2(0.5f, 0f);
        continueRect.anchoredPosition = new Vector2(0, 90);
        continueRect.sizeDelta = new Vector2(280, 80);
        settlementCtrl.continueBtn = continueBtn;

        GameObject continueTextObj = new GameObject("Text");
        continueTextObj.transform.SetParent(continueObj.transform, false);
        Text continueTxt = continueTextObj.AddComponent<Text>();
        continueTxt.font = defaultFont;
        continueTxt.fontSize = 30;
        continueTxt.color = Color.white;
        continueTxt.text = "返回工坊";
        continueTxt.alignment = TextAnchor.MiddleCenter;
        continueTxt.raycastTarget = false;
        RectTransform continueTextRect = continueTextObj.GetComponent<RectTransform>();
        continueTextRect.anchorMin = Vector2.zero;
        continueTextRect.anchorMax = Vector2.one;
        continueTextRect.sizeDelta = Vector2.zero;

        Debug.Log("[GameFlow] Runtime fallback SettlementPanel created.");
    }

    void OnDestroy() {
        if (Instance == this) {
            Instance = null;
        }

        if (_inventoryPresentation != null) {
            _inventoryPresentation.ItemPresentationChanged -= HandleInventoryPresentationChanged;
        }

        DungeonEventBus.OnDungeonSettled -= HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettlementPrepared -= HandleDungeonSettlementPrepared;
        DungeonEventBus.OnLayerLoaded -= EnterDungeonMap;
        DungeonEventBus.OnNodeResolutionFinished -= EnterDungeonMap;
        DungeonEventBus.OnDungeonNodeResolutionPrepared -= HandleDungeonNodeResolutionPrepared;
        DungeonEventBus.OnCombatLootPrepared -= HandleCombatLootPrepared;
        DungeonEventBus.OnNodeEntered -= HandleNodeEntered;
        DungeonEventBus.OnSafeRoomEntered -= HandleSafeRoomEntered;
        DungeonEventBus.OnStairsEntered -= HandleStairsEntered;
    }
}
