using UnityEngine;

public class GameFlowController : MonoBehaviour {
    public static GameFlowController Instance { get; private set; }

    public GameObject workshopPanel;
    public GameObject dungeonMapPanel;
    public GameObject combatPanel;
    public GameObject safeRoomPanel;
    public GameObject testItemPrefab;

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
        
        // 当进入具体的 Node 时切界面
        DungeonEventBus.OnNodeEntered += (node, cost) => {
            if (node is CombatNode) {
                EnterCombat();
            }
        };
        DungeonEventBus.OnSafeRoomEntered += (node) => {
            EnterSafeRoom(node as SafeRoomNode);
        };

        EnterWorkshop();
    }

    public void EnterWorkshop() {
        Debug.Log("[GameFlow] 进入局外工坊...");
        if (workshopPanel) workshopPanel.SetActive(true);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(false);
        if (safeRoomPanel) safeRoomPanel.SetActive(false);
        
        var wsCtrl = workshopPanel?.GetComponent<WorkshopUIController>();
        if (wsCtrl != null) wsCtrl.RefreshUI();
    }

    public void EnterDungeonMap() {
        Debug.Log("[GameFlow] 玩家在深渊地图中抉择路线...");
        if (workshopPanel) workshopPanel.SetActive(false);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(true);
        if (combatPanel) combatPanel.SetActive(false);
        if (safeRoomPanel) safeRoomPanel.SetActive(false);

        var mapCtrl = dungeonMapPanel?.GetComponent<DungeonMapUIController>();
        if (mapCtrl != null) mapCtrl.RefreshMap();
    }

    public void EnterCombat() {
        Debug.Log("[GameFlow] 进入战斗！");
        if (workshopPanel) workshopPanel.SetActive(false);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(true);
        if (safeRoomPanel) safeRoomPanel.SetActive(false);
    }
    
    public void EnterSafeRoom(SafeRoomNode node) {
        Debug.Log("[GameFlow] 进入安全区！");
        if (workshopPanel) workshopPanel.SetActive(false);
        if (dungeonMapPanel) dungeonMapPanel.SetActive(false);
        if (combatPanel) combatPanel.SetActive(false);
        if (safeRoomPanel) safeRoomPanel.SetActive(true);
        
        var sfCtrl = safeRoomPanel?.GetComponent<SafeRoomUIController>();
        if (sfCtrl != null) sfCtrl.Setup(node);
    }

    public void DepartToDungeon() {
        Debug.Log("[GameFlow] 玩家启程，加载深渊...");
        GameRoot.Core.Dungeon.LoadLayer(1);
    }

    private void HandleDungeonSettled(bool isVictory) {
        EnterWorkshop();
    }

    void OnDestroy() {
        DungeonEventBus.OnDungeonSettled -= HandleDungeonSettled;
        DungeonEventBus.OnLayerLoaded -= EnterDungeonMap;
        DungeonEventBus.OnCombatNodeCleared -= EnterDungeonMap;
    }
}