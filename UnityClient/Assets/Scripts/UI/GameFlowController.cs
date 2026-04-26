using UnityEngine;

public class GameFlowController : MonoBehaviour {
    private enum GameScreenState {
        Workshop,
        DungeonMap,
        Combat,
        SafeRoom,
        Settlement
    }

    public static GameFlowController Instance { get; private set; }

    public GameObject workshopPanel;
    public GameObject dungeonMapPanel;
    public GameObject combatPanel;
    public GameObject safeRoomPanel;
    public GameObject settlementPanel;
    public GameObject testItemPrefab;
    
    private GameScreenState _currentScreen;
    private DungeonSettlementResult _pendingSettlementResult;

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
        GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(myChassis);
        FindObjectOfType<GridGenerator>().GenerateGrid(myChassis);

        if (testItemPrefab != null) {
            GameObject itemGo = Instantiate(testItemPrefab, FindObjectOfType<Canvas>().transform);
            ItemEntity swordData = ConfigManager.CreateItem("gear_tactical_blade");
            
            RectTransform rect = itemGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 300);
            rect.anchoredPosition = new Vector2(-400, 0);

            itemGo.GetComponent<DraggableItemUI>().SetupData(swordData);
        }

        DungeonEventBus.OnLayerLoaded += EnterDungeonMap;
        DungeonEventBus.OnCombatNodeCleared += EnterDungeonMap;
        DungeonEventBus.OnDungeonSettled += HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettlementPrepared += HandleDungeonSettlementPrepared;

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
    
    public void EnterSafeRoom(SafeRoomNode node) {
        TransitionToScreen(GameScreenState.SafeRoom, node);
    }

    public void DepartToDungeon() {
        Debug.Log("[GameFlow] 玩家启程，加载深渊...");
        GameRoot.Core.Dungeon.LoadLayer(1);
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

    private void HandleNodeEntered(NodeBase node, int cost) {
        if (node is CombatNode) {
            EnterCombat();
        }
    }

    private void HandleSafeRoomEntered(NodeBase node) {
        EnterSafeRoom(node as SafeRoomNode);
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
        _currentScreen = nextScreen;
        Debug.Log($"[GameFlow] 切换屏幕状态 -> {_currentScreen}");

        if (workshopPanel) workshopPanel.SetActive(nextScreen == GameScreenState.Workshop);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(nextScreen == GameScreenState.DungeonMap);
        if (combatPanel) combatPanel.SetActive(nextScreen == GameScreenState.Combat);
        if (safeRoomPanel) safeRoomPanel.SetActive(nextScreen == GameScreenState.SafeRoom);
        if (settlementPanel) settlementPanel.SetActive(nextScreen == GameScreenState.Settlement);

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
    }

    private void OnEnterDungeonMapScreen() {
        Debug.Log("[GameFlow] 玩家在深渊地图中抉择路线...");
        var mapCtrl = dungeonMapPanel?.GetComponent<DungeonMapUIController>();
        if (mapCtrl != null) {
            mapCtrl.RefreshMap();
        }
    }

    private void OnEnterCombatScreen() {
        Debug.Log("[GameFlow] 进入战斗！");
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

    void OnDestroy() {
        DungeonEventBus.OnDungeonSettled -= HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettlementPrepared -= HandleDungeonSettlementPrepared;
        DungeonEventBus.OnLayerLoaded -= EnterDungeonMap;
        DungeonEventBus.OnCombatNodeCleared -= EnterDungeonMap;
        DungeonEventBus.OnNodeEntered -= HandleNodeEntered;
        DungeonEventBus.OnSafeRoomEntered -= HandleSafeRoomEntered;
    }
}
