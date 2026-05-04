#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MVPEditorSetup : EditorWindow
{
    [MenuItem("Tools/魔偶深渊 一键生成 MVP 场景骨架")]
    public static void GenerateMVPScene()
    {
        DestroyIfExists("[GameRoot]");
        DestroyIfExists("GameManager");
        DestroyIfExists("MVP_Tester");
        DestroyIfExists("InventoryCanvas");
        DestroyIfExists("HUD_Manager"); 

        // 1. 创建全局总控 GameRoot
        GameObject gameRoot = new GameObject("[GameRoot]");
        gameRoot.AddComponent<GameRoot>();

        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. 创建状态机控制器 GameManager
        GameObject gameManager = new GameObject("GameManager");
        GameFlowController flowCtrl = gameManager.AddComponent<GameFlowController>();

        // 3. 创建 UGUI 画布 Canvas
        GameObject canvasGo = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGo.AddComponent<GraphicRaycaster>();

        // 4. 创建网格容器 GridContainer (始终在中间)
        GameObject gridContainer = new GameObject("GridContainer");
        gridContainer.transform.SetParent(canvasGo.transform, false);
        
        RectTransform gridRect = gridContainer.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        
        GridLayoutGroup layoutGroup = gridContainer.AddComponent<GridLayoutGroup>();
        layoutGroup.cellSize = new Vector2(100, 100);
        layoutGroup.spacing = new Vector2(5, 5);
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layoutGroup.constraintCount = 5; 

        GridGenerator generator = canvasGo.AddComponent<GridGenerator>();
        generator.gridParent = gridContainer.transform;

        GameObject inventoryItemLayer = new GameObject("InventoryItemLayer");
        inventoryItemLayer.transform.SetParent(canvasGo.transform, false);
        RectTransform inventoryLayerRect = inventoryItemLayer.AddComponent<RectTransform>();
        inventoryLayerRect.anchorMin = Vector2.zero;
        inventoryLayerRect.anchorMax = Vector2.one;
        inventoryLayerRect.sizeDelta = Vector2.zero;
        inventoryItemLayer.AddComponent<CanvasGroup>();

        // 生成并保存占位预制体 (SlotPrefab)
        GameObject slotPrefab = new GameObject("SlotPrefab");
        Image slotImg = slotPrefab.AddComponent<Image>();
        slotImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        slotPrefab.AddComponent<GridSlotUI>();

        string prefabPath = "Assets/SlotPrefab.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(slotPrefab, prefabPath);
        DestroyImmediate(slotPrefab); 

        generator.slotPrefab = savedPrefab;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // ====================================================================
        // 5. 局外工坊面板 (WorkshopPanel)
        // ====================================================================
        GameObject workshopPanel = new GameObject("WorkshopPanel");
        workshopPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform wsRect = workshopPanel.AddComponent<RectTransform>();
        wsRect.anchorMin = Vector2.zero; wsRect.anchorMax = Vector2.one;
        wsRect.sizeDelta = Vector2.zero;
        
        WorkshopUIController wsCtrl = workshopPanel.AddComponent<WorkshopUIController>();

        GameObject moneyObj = new GameObject("Money_Text");
        moneyObj.transform.SetParent(workshopPanel.transform, false);
        Text moneyTxt = moneyObj.AddComponent<Text>();
        moneyTxt.font = defaultFont; moneyTxt.fontSize = 30; moneyTxt.color = Color.yellow;
        moneyTxt.raycastTarget = false;
        RectTransform moneyRect = moneyObj.GetComponent<RectTransform>();
        moneyRect.anchorMin = new Vector2(0, 1); moneyRect.anchorMax = new Vector2(0, 1);
        moneyRect.pivot = new Vector2(0, 1); moneyRect.anchoredPosition = new Vector2(20, -20);
        moneyRect.sizeDelta = new Vector2(400, 100);
        wsCtrl.moneyText = moneyTxt;

        GameObject chassisObj = new GameObject("Chassis_Text");
        chassisObj.transform.SetParent(workshopPanel.transform, false);
        Text chassisTxt = chassisObj.AddComponent<Text>();
        chassisTxt.font = defaultFont; chassisTxt.fontSize = 30; chassisTxt.color = Color.white;
        chassisTxt.raycastTarget = false;
        RectTransform chassisRect = chassisObj.GetComponent<RectTransform>();
        chassisRect.anchorMin = new Vector2(0, 1); chassisRect.anchorMax = new Vector2(0, 1);
        chassisRect.pivot = new Vector2(0, 1); chassisRect.anchoredPosition = new Vector2(20, -120);
        chassisRect.sizeDelta = new Vector2(400, 100);
        wsCtrl.chassisInfoText = chassisTxt;

        GameObject upgBtnObj = new GameObject("Upgrade_Button");
        upgBtnObj.transform.SetParent(workshopPanel.transform, false);
        Image upgImg = upgBtnObj.AddComponent<Image>();
        upgImg.color = new Color(0.2f, 0.6f, 0.2f);
        Button upgBtn = upgBtnObj.AddComponent<Button>();
        RectTransform upgRect = upgBtnObj.GetComponent<RectTransform>();
        upgRect.anchorMin = new Vector2(0, 0); upgRect.anchorMax = new Vector2(0, 0);
        upgRect.pivot = new Vector2(0, 0); upgRect.anchoredPosition = new Vector2(150, 150);
        upgRect.sizeDelta = new Vector2(250, 80);
        wsCtrl.upgradeBtn = upgBtn;

        GameObject upgTxtObj = new GameObject("Text");
        upgTxtObj.transform.SetParent(upgBtnObj.transform, false);
        Text upgTxt = upgTxtObj.AddComponent<Text>();
        upgTxt.font = defaultFont; upgTxt.fontSize = 30; upgTxt.color = Color.white;
        upgTxt.text = "升级底盘 (-1000G)";
        upgTxt.alignment = TextAnchor.MiddleCenter;
        upgTxt.raycastTarget = false;
        RectTransform upgTxtRect = upgTxtObj.GetComponent<RectTransform>();
        upgTxtRect.anchorMin = Vector2.zero; upgTxtRect.anchorMax = Vector2.one;
        upgTxtRect.sizeDelta = Vector2.zero;

        GameObject depBtnObj = new GameObject("Depart_Button");
        depBtnObj.transform.SetParent(workshopPanel.transform, false);
        Image depImg = depBtnObj.AddComponent<Image>();
        depImg.color = new Color(0.8f, 0.4f, 0.2f);
        Button depBtn = depBtnObj.AddComponent<Button>();
        RectTransform depRect = depBtnObj.GetComponent<RectTransform>();
        depRect.anchorMin = new Vector2(1, 0); depRect.anchorMax = new Vector2(1, 0);
        depRect.pivot = new Vector2(1, 0); depRect.anchoredPosition = new Vector2(-150, 150);
        depRect.sizeDelta = new Vector2(250, 80);
        wsCtrl.departBtn = depBtn;

        GameObject depTxtObj = new GameObject("Text");
        depTxtObj.transform.SetParent(depBtnObj.transform, false);
        Text depTxt = depTxtObj.AddComponent<Text>();
        depTxt.font = defaultFont; depTxt.fontSize = 30; depTxt.color = Color.white;
        depTxt.text = "出发深渊";
        depTxt.alignment = TextAnchor.MiddleCenter;
        depTxt.raycastTarget = false;
        RectTransform depTxtRect = depTxtObj.GetComponent<RectTransform>();
        depTxtRect.anchorMin = Vector2.zero; depTxtRect.anchorMax = Vector2.one;
        depTxtRect.sizeDelta = Vector2.zero;

        // ====================================================================
        // 6. 路线图面板 (DungeonMapPanel)
        // ====================================================================
        GameObject mapPanel = new GameObject("DungeonMapPanel");
        mapPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform mapPanelRect = mapPanel.AddComponent<RectTransform>();
        mapPanelRect.anchorMin = Vector2.zero; mapPanelRect.anchorMax = Vector2.one;
        mapPanelRect.sizeDelta = Vector2.zero;
        mapPanel.SetActive(false);

        DungeonMapUIController mapCtrl = mapPanel.AddComponent<DungeonMapUIController>();
        
        GameObject mapLayout = new GameObject("MapLayout");
        mapLayout.transform.SetParent(mapPanel.transform, false);
        RectTransform mapLayoutRect = mapLayout.AddComponent<RectTransform>();
        mapLayoutRect.anchorMin = new Vector2(0.5f, 0.5f); mapLayoutRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapLayoutRect.pivot = new Vector2(0.5f, 0.5f);
        mapLayoutRect.anchoredPosition = new Vector2(0, 300);
        HorizontalLayoutGroup mapGroup = mapLayout.AddComponent<HorizontalLayoutGroup>();
        mapGroup.childControlWidth = true; mapGroup.childControlHeight = true;
        mapGroup.spacing = 20;

        mapCtrl.contentParent = mapLayout.transform;

        // Node Button Prefab
        GameObject nodeBtnPrefab = new GameObject("NodeButtonPrefab");
        Image nbImg = nodeBtnPrefab.AddComponent<Image>();
        nbImg.color = Color.gray;
        nodeBtnPrefab.AddComponent<Button>();
        RectTransform nbRect = nodeBtnPrefab.GetComponent<RectTransform>();
        nbRect.sizeDelta = new Vector2(120, 80);
        
        GameObject nbTxtObj = new GameObject("Text");
        nbTxtObj.transform.SetParent(nodeBtnPrefab.transform, false);
        Text nbTxt = nbTxtObj.AddComponent<Text>();
        nbTxt.font = defaultFont; nbTxt.fontSize = 20; nbTxt.color = Color.white;
        nbTxt.alignment = TextAnchor.MiddleCenter;
        nbTxt.raycastTarget = false;
        RectTransform nbTxtRect = nbTxtObj.GetComponent<RectTransform>();
        nbTxtRect.anchorMin = Vector2.zero; nbTxtRect.anchorMax = Vector2.one;
        nbTxtRect.sizeDelta = Vector2.zero;

        string nodePrefabPath = "Assets/NodeButtonPrefab.prefab";
        mapCtrl.nodeButtonPrefab = PrefabUtility.SaveAsPrefabAsset(nodeBtnPrefab, nodePrefabPath);
        DestroyImmediate(nodeBtnPrefab);

        // ====================================================================
        // 7. 战斗面板 (CombatPanel)
        // ====================================================================
        GameObject combatPanel = new GameObject("CombatPanel");
        combatPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform dpRect = combatPanel.AddComponent<RectTransform>();
        dpRect.anchorMin = Vector2.zero; dpRect.anchorMax = Vector2.one;
        dpRect.sizeDelta = Vector2.zero;
        combatPanel.SetActive(false);
        
        HUDController hudCtrl = combatPanel.AddComponent<HUDController>();

        GameObject hpObj = new GameObject("HP_Text");
        hpObj.transform.SetParent(combatPanel.transform, false);
        Text hpTxt = hpObj.AddComponent<Text>();
        hpTxt.font = defaultFont; hpTxt.fontSize = 30; hpTxt.color = Color.red;
        hpTxt.text = "HP: 100/100";
        hpTxt.raycastTarget = false;
        RectTransform hpRect = hpObj.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0, 1); hpRect.anchorMax = new Vector2(0, 1);
        hpRect.pivot = new Vector2(0, 1); hpRect.anchoredPosition = new Vector2(20, -20);
        hpRect.sizeDelta = new Vector2(300, 50);
        hudCtrl.hpLabel = hpTxt;

        GameObject sanObj = new GameObject("SAN_Text");
        sanObj.transform.SetParent(combatPanel.transform, false);
        Text sanTxt = sanObj.AddComponent<Text>();
        sanTxt.font = defaultFont; sanTxt.fontSize = 30; sanTxt.color = new Color(0.6f, 0.2f, 1f);
        sanTxt.text = "SAN: 50/50";
        sanTxt.raycastTarget = false;
        RectTransform sanRect = sanObj.GetComponent<RectTransform>();
        sanRect.anchorMin = new Vector2(0, 1); sanRect.anchorMax = new Vector2(0, 1);
        sanRect.pivot = new Vector2(0, 1); sanRect.anchoredPosition = new Vector2(20, -70);
        sanRect.sizeDelta = new Vector2(300, 50);
        hudCtrl.sanLabel = sanTxt;

        GameObject apObj = new GameObject("AP_Text");
        apObj.transform.SetParent(combatPanel.transform, false);
        Text apTxt = apObj.AddComponent<Text>();
        apTxt.font = defaultFont; apTxt.fontSize = 40; apTxt.color = Color.cyan;
        apTxt.text = "AP: 3/3";
        apTxt.raycastTarget = false;
        RectTransform apRect = apObj.GetComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0, 0); apRect.anchorMax = new Vector2(0, 0);
        apRect.pivot = new Vector2(0, 0); apRect.anchoredPosition = new Vector2(20, 20);
        apRect.sizeDelta = new Vector2(300, 80);
        hudCtrl.apLabel = apTxt;

        GameObject shieldObj = new GameObject("Shield_Text");
        shieldObj.transform.SetParent(combatPanel.transform, false);
        Text shieldTxt = shieldObj.AddComponent<Text>();
        shieldTxt.font = defaultFont; shieldTxt.fontSize = 28; shieldTxt.color = new Color(0.95f, 0.82f, 0.3f);
        shieldTxt.text = "Shield: 0";
        shieldTxt.raycastTarget = false;
        RectTransform shieldRect = shieldObj.GetComponent<RectTransform>();
        shieldRect.anchorMin = new Vector2(0, 1); shieldRect.anchorMax = new Vector2(0, 1);
        shieldRect.pivot = new Vector2(0, 1); shieldRect.anchoredPosition = new Vector2(20, -120);
        shieldRect.sizeDelta = new Vector2(300, 50);
        hudCtrl.shieldLabel = shieldTxt;

        GameObject targetHintObj = new GameObject("TargetHint_Text");
        targetHintObj.transform.SetParent(combatPanel.transform, false);
        Text targetHintTxt = targetHintObj.AddComponent<Text>();
        targetHintTxt.font = defaultFont; targetHintTxt.fontSize = 26; targetHintTxt.color = Color.white;
        targetHintTxt.text = "点击背包里的武器后，再点击右侧敌人进行攻击。";
        targetHintTxt.alignment = TextAnchor.MiddleCenter;
        targetHintTxt.raycastTarget = false;
        RectTransform targetHintRect = targetHintObj.GetComponent<RectTransform>();
        targetHintRect.anchorMin = new Vector2(0.5f, 1); targetHintRect.anchorMax = new Vector2(0.5f, 1);
        targetHintRect.pivot = new Vector2(0.5f, 1); targetHintRect.anchoredPosition = new Vector2(0, -36);
        targetHintRect.sizeDelta = new Vector2(760, 70);
        hudCtrl.targetHintLabel = targetHintTxt;

        GameObject endBtnObj = new GameObject("EndTurn_Button");
        endBtnObj.transform.SetParent(combatPanel.transform, false);
        Image endImg = endBtnObj.AddComponent<Image>();
        endImg.color = new Color(0.8f, 0.2f, 0.2f);
        Button endBtn = endBtnObj.AddComponent<Button>();
        RectTransform endRect = endBtnObj.GetComponent<RectTransform>();
        endRect.anchorMin = new Vector2(1, 0); endRect.anchorMax = new Vector2(1, 0);
        endRect.pivot = new Vector2(1, 0); endRect.anchoredPosition = new Vector2(-20, 20);
        endRect.sizeDelta = new Vector2(200, 80);
        hudCtrl.endTurnBtn = endBtn;

        GameObject endTxtObj = new GameObject("Text");
        endTxtObj.transform.SetParent(endBtnObj.transform, false);
        Text endTxt = endTxtObj.AddComponent<Text>();
        endTxt.font = defaultFont; endTxt.fontSize = 30; endTxt.color = Color.white;
        endTxt.text = "结束回合";
        endTxt.alignment = TextAnchor.MiddleCenter;
        endTxt.raycastTarget = false;
        RectTransform endTxtRect = endTxtObj.GetComponent<RectTransform>();
        endTxtRect.anchorMin = Vector2.zero; endTxtRect.anchorMax = Vector2.one;
        endTxtRect.sizeDelta = Vector2.zero;

        // ====================================================================
        // 8. 战利品拾取面板 (CombatLootPanel)
        // ====================================================================
        GameObject combatLootPanel = new GameObject("CombatLootPanel");
        combatLootPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform clRect = combatLootPanel.AddComponent<RectTransform>();
        clRect.anchorMin = Vector2.zero; clRect.anchorMax = Vector2.one;
        clRect.sizeDelta = Vector2.zero;
        combatLootPanel.SetActive(false);

        Image combatLootBg = combatLootPanel.AddComponent<Image>();
        combatLootBg.color = new Color(0.08f, 0.08f, 0.08f, 0.82f);
        combatLootBg.raycastTarget = false;

        CombatLootUIController combatLootCtrl = combatLootPanel.AddComponent<CombatLootUIController>();

        GameObject clTitleObj = new GameObject("Title_Text");
        clTitleObj.transform.SetParent(combatLootPanel.transform, false);
        Text clTitleTxt = clTitleObj.AddComponent<Text>();
        clTitleTxt.font = defaultFont; clTitleTxt.fontSize = 44; clTitleTxt.color = Color.white;
        clTitleTxt.alignment = TextAnchor.MiddleCenter;
        clTitleTxt.raycastTarget = false;
        RectTransform clTitleRect = clTitleObj.GetComponent<RectTransform>();
        clTitleRect.anchorMin = new Vector2(0.5f, 1); clTitleRect.anchorMax = new Vector2(0.5f, 1);
        clTitleRect.pivot = new Vector2(0.5f, 1); clTitleRect.anchoredPosition = new Vector2(0, -60);
        clTitleRect.sizeDelta = new Vector2(600, 80);
        combatLootCtrl.titleText = clTitleTxt;

        GameObject clSummaryObj = new GameObject("Summary_Text");
        clSummaryObj.transform.SetParent(combatLootPanel.transform, false);
        Text clSummaryTxt = clSummaryObj.AddComponent<Text>();
        clSummaryTxt.font = defaultFont; clSummaryTxt.fontSize = 24; clSummaryTxt.color = new Color(0.95f, 0.95f, 0.95f);
        clSummaryTxt.alignment = TextAnchor.UpperCenter;
        clSummaryTxt.raycastTarget = false;
        RectTransform clSummaryRect = clSummaryObj.GetComponent<RectTransform>();
        clSummaryRect.anchorMin = new Vector2(0.5f, 1); clSummaryRect.anchorMax = new Vector2(0.5f, 1);
        clSummaryRect.pivot = new Vector2(0.5f, 1); clSummaryRect.anchoredPosition = new Vector2(0, -140);
        clSummaryRect.sizeDelta = new Vector2(760, 120);
        combatLootCtrl.summaryText = clSummaryTxt;

        GameObject clLootArea = new GameObject("LootArea");
        clLootArea.transform.SetParent(combatLootPanel.transform, false);
        RectTransform clLootRect = clLootArea.AddComponent<RectTransform>();
        clLootRect.anchorMin = Vector2.zero; clLootRect.anchorMax = Vector2.one;
        clLootRect.pivot = new Vector2(0.5f, 0.5f); clLootRect.anchoredPosition = Vector2.zero;
        clLootRect.sizeDelta = Vector2.zero;
        combatLootCtrl.lootParent = clLootArea.transform;

        GameObject clContinueObj = new GameObject("Continue_Button");
        clContinueObj.transform.SetParent(combatLootPanel.transform, false);
        Image clContinueImg = clContinueObj.AddComponent<Image>();
        clContinueImg.color = new Color(0.9f, 0.58f, 0.18f);
        Button clContinueBtn = clContinueObj.AddComponent<Button>();
        RectTransform clContinueRect = clContinueObj.GetComponent<RectTransform>();
        clContinueRect.anchorMin = new Vector2(0.5f, 0); clContinueRect.anchorMax = new Vector2(0.5f, 0);
        clContinueRect.pivot = new Vector2(0.5f, 0); clContinueRect.anchoredPosition = new Vector2(0, 90);
        clContinueRect.sizeDelta = new Vector2(320, 80);
        combatLootCtrl.continueBtn = clContinueBtn;

        GameObject clContinueTxtObj = new GameObject("Text");
        clContinueTxtObj.transform.SetParent(clContinueObj.transform, false);
        Text clContinueTxt = clContinueTxtObj.AddComponent<Text>();
        clContinueTxt.font = defaultFont; clContinueTxt.fontSize = 30; clContinueTxt.color = Color.black;
        clContinueTxt.text = "确认拾取并继续";
        clContinueTxt.alignment = TextAnchor.MiddleCenter;
        clContinueTxt.raycastTarget = false;
        RectTransform clContinueTxtRect = clContinueTxtObj.GetComponent<RectTransform>();
        clContinueTxtRect.anchorMin = Vector2.zero; clContinueTxtRect.anchorMax = Vector2.one;
        clContinueTxtRect.sizeDelta = Vector2.zero;

        // ====================================================================
        // 9. 安全区面板 (SafeRoomPanel)
        // ====================================================================
        GameObject safeRoomPanel = new GameObject("SafeRoomPanel");
        safeRoomPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform srRect = safeRoomPanel.AddComponent<RectTransform>();
        srRect.anchorMin = Vector2.zero; srRect.anchorMax = Vector2.one;
        srRect.sizeDelta = Vector2.zero;
        safeRoomPanel.SetActive(false);

        SafeRoomUIController srCtrl = safeRoomPanel.AddComponent<SafeRoomUIController>();

        GameObject srTitleObj = new GameObject("Title_Text");
        srTitleObj.transform.SetParent(safeRoomPanel.transform, false);
        Text srTitleTxt = srTitleObj.AddComponent<Text>();
        srTitleTxt.font = defaultFont; srTitleTxt.fontSize = 50; srTitleTxt.color = Color.green;
        srTitleTxt.text = "安 全 区";
        srTitleTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform srTitleRect = srTitleObj.GetComponent<RectTransform>();
        srTitleRect.anchorMin = new Vector2(0.5f, 1); srTitleRect.anchorMax = new Vector2(0.5f, 1);
        srTitleRect.pivot = new Vector2(0.5f, 1); srTitleRect.anchoredPosition = new Vector2(0, -50);
        srTitleRect.sizeDelta = new Vector2(400, 100);

        GameObject restBtnObj = new GameObject("Rest_Button");
        restBtnObj.transform.SetParent(safeRoomPanel.transform, false);
        Image restImg = restBtnObj.AddComponent<Image>();
        restImg.color = new Color(0.2f, 0.8f, 0.2f);
        Button restBtn = restBtnObj.AddComponent<Button>();
        RectTransform restRect = restBtnObj.GetComponent<RectTransform>();
        restRect.anchorMin = new Vector2(0, 0); restRect.anchorMax = new Vector2(0, 0);
        restRect.pivot = new Vector2(0, 0); restRect.anchoredPosition = new Vector2(150, 150);
        restRect.sizeDelta = new Vector2(250, 80);
        srCtrl.restBtn = restBtn;

        GameObject restTxtObj = new GameObject("Text");
        restTxtObj.transform.SetParent(restBtnObj.transform, false);
        Text restTxt = restTxtObj.AddComponent<Text>();
        restTxt.font = defaultFont; restTxt.fontSize = 30; restTxt.color = Color.white;
        restTxt.text = "休整 (回血满)";
        restTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform restTxtRect = restTxtObj.GetComponent<RectTransform>();
        restTxtRect.anchorMin = Vector2.zero; restTxtRect.anchorMax = Vector2.one;
        restTxtRect.sizeDelta = Vector2.zero;

        GameObject evacBtnObj = new GameObject("Evacuate_Button");
        evacBtnObj.transform.SetParent(safeRoomPanel.transform, false);
        Image evacImg = evacBtnObj.AddComponent<Image>();
        evacImg.color = new Color(0.8f, 0.8f, 0.2f);
        Button evacBtn = evacBtnObj.AddComponent<Button>();
        RectTransform evacRect = evacBtnObj.GetComponent<RectTransform>();
        evacRect.anchorMin = new Vector2(1, 0); evacRect.anchorMax = new Vector2(1, 0);
        evacRect.pivot = new Vector2(1, 0); evacRect.anchoredPosition = new Vector2(-150, 150);
        evacRect.sizeDelta = new Vector2(250, 80);
        srCtrl.evacuateBtn = evacBtn;

        GameObject evacTxtObj = new GameObject("Text");
        evacTxtObj.transform.SetParent(evacBtnObj.transform, false);
        Text evacTxt = evacTxtObj.AddComponent<Text>();
        evacTxt.font = defaultFont; evacTxt.fontSize = 30; evacTxt.color = Color.black;
        evacTxt.text = "撤离回城";
        evacTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform evacTxtRect = evacTxtObj.GetComponent<RectTransform>();
        evacTxtRect.anchorMin = Vector2.zero; evacTxtRect.anchorMax = Vector2.one;
        evacTxtRect.sizeDelta = Vector2.zero;

        // ====================================================================
        // 10. 结算面板 (SettlementPanel)
        // ====================================================================
        GameObject settlementPanel = new GameObject("SettlementPanel");
        settlementPanel.transform.SetParent(canvasGo.transform, false);
        RectTransform settlementRect = settlementPanel.AddComponent<RectTransform>();
        settlementRect.anchorMin = Vector2.zero; settlementRect.anchorMax = Vector2.one;
        settlementRect.sizeDelta = Vector2.zero;
        settlementPanel.SetActive(false);

        Image settlementBg = settlementPanel.AddComponent<Image>();
        settlementBg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);

        SettlementUIController settlementCtrl = settlementPanel.AddComponent<SettlementUIController>();

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(settlementPanel.transform, false);
        Text titleTxt = titleObj.AddComponent<Text>();
        titleTxt.font = defaultFont; titleTxt.fontSize = 48; titleTxt.color = Color.white;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1); titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.pivot = new Vector2(0.5f, 1); titleRect.anchoredPosition = new Vector2(0, -80);
        titleRect.sizeDelta = new Vector2(500, 80);
        settlementCtrl.titleText = titleTxt;

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(settlementPanel.transform, false);
        Text summaryTxt = summaryObj.AddComponent<Text>();
        summaryTxt.font = defaultFont; summaryTxt.fontSize = 28; summaryTxt.color = Color.white;
        summaryTxt.alignment = TextAnchor.UpperCenter;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0.5f, 1); summaryRect.anchorMax = new Vector2(0.5f, 1);
        summaryRect.pivot = new Vector2(0.5f, 1); summaryRect.anchoredPosition = new Vector2(0, -180);
        summaryRect.sizeDelta = new Vector2(700, 140);
        settlementCtrl.summaryText = summaryTxt;

        GameObject lootObj = new GameObject("Loot_Text");
        lootObj.transform.SetParent(settlementPanel.transform, false);
        Text lootTxt = lootObj.AddComponent<Text>();
        lootTxt.font = defaultFont; lootTxt.fontSize = 24; lootTxt.color = new Color(1f, 0.92f, 0.5f);
        lootTxt.alignment = TextAnchor.UpperLeft;
        RectTransform lootRect = lootObj.GetComponent<RectTransform>();
        lootRect.anchorMin = new Vector2(0.5f, 0.5f); lootRect.anchorMax = new Vector2(0.5f, 0.5f);
        lootRect.pivot = new Vector2(0.5f, 0.5f); lootRect.anchoredPosition = new Vector2(0, -20);
        lootRect.sizeDelta = new Vector2(700, 260);
        settlementCtrl.lootText = lootTxt;

        GameObject continueBtnObj = new GameObject("Continue_Button");
        continueBtnObj.transform.SetParent(settlementPanel.transform, false);
        Image continueImg = continueBtnObj.AddComponent<Image>();
        continueImg.color = new Color(0.25f, 0.6f, 0.95f);
        Button continueBtn = continueBtnObj.AddComponent<Button>();
        RectTransform continueRect = continueBtnObj.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(0.5f, 0); continueRect.anchorMax = new Vector2(0.5f, 0);
        continueRect.pivot = new Vector2(0.5f, 0); continueRect.anchoredPosition = new Vector2(0, 90);
        continueRect.sizeDelta = new Vector2(280, 80);
        settlementCtrl.continueBtn = continueBtn;

        GameObject continueTxtObj = new GameObject("Text");
        continueTxtObj.transform.SetParent(continueBtnObj.transform, false);
        Text continueTxt = continueTxtObj.AddComponent<Text>();
        continueTxt.font = defaultFont; continueTxt.fontSize = 30; continueTxt.color = Color.white;
        continueTxt.text = "返回工坊";
        continueTxt.alignment = TextAnchor.MiddleCenter;
        continueTxt.raycastTarget = false;
        RectTransform continueTxtRect = continueTxtObj.GetComponent<RectTransform>();
        continueTxtRect.anchorMin = Vector2.zero; continueTxtRect.anchorMax = Vector2.one;
        continueTxtRect.sizeDelta = Vector2.zero;

        // 11. 顺便生成一个可拖拽物品的 Prefab 占位
        GameObject itemPrefab = new GameObject("ItemPrefab_TestSword");
        Image itemImg = itemPrefab.AddComponent<Image>();
        itemImg.color = Color.red; 
        itemPrefab.AddComponent<DraggableItemUI>();
        itemPrefab.AddComponent<CanvasGroup>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(itemPrefab.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "长刀\n2AP";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = defaultFont;
        txt.raycastTarget = false; 

        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        PrefabUtility.SaveAsPrefabAsset(itemPrefab, "Assets/ItemPrefab_TestSword.prefab");
        DestroyImmediate(itemPrefab);

        // ====================================================================
        // 12. 关联 Controller 引用
        // ====================================================================
        flowCtrl.workshopPanel = workshopPanel;
        flowCtrl.dungeonMapPanel = mapPanel;
        flowCtrl.combatPanel = combatPanel;
        flowCtrl.combatLootPanel = combatLootPanel;
        flowCtrl.safeRoomPanel = safeRoomPanel;
        flowCtrl.settlementPanel = settlementPanel;
        flowCtrl.inventoryItemLayer = inventoryItemLayer.transform;
        
        var loadedTestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ItemPrefab_TestSword.prefab");
        if (loadedTestPrefab != null) {
            flowCtrl.testItemPrefab = loadedTestPrefab;
        }

        Debug.Log("<color=green>[自动化构建] 局内地图+安全区 MVP 场景骨架生成完毕！点击 Play 开始体验闭环！</color>");
    }

    private static void DestroyIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            DestroyImmediate(go);
        }
    }
}
#endif
