#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MVPEditorSetup : EditorWindow
{
    [MenuItem("Tools/魔偶深渊 一键生成 MVP 场景骨架")]
    public static void GenerateMVPScene()
    {
        // 幂等性处理
        DestroyIfExists("[GameRoot]");
        DestroyIfExists("MVP_Tester");
        DestroyIfExists("InventoryCanvas");
        // 极其重要：必须删掉之前旧版在根目录生成的那个带有 UI Toolkit 的 HUD_Manager 幽灵！
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

        // 2. 创建测试启动器 MVPTester
        GameObject tester = new GameObject("MVP_Tester");
        tester.AddComponent<MVPTester>();

        // 3. 创建 UGUI 画布 Canvas
        GameObject canvasGo = new GameObject("InventoryCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGo.AddComponent<GraphicRaycaster>();

        // 4. 创建网格容器 GridContainer
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

        // 5. 生成并保存占位预制体 (SlotPrefab)
        GameObject slotPrefab = new GameObject("SlotPrefab");
        Image slotImg = slotPrefab.AddComponent<Image>();
        slotImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        slotPrefab.AddComponent<GridSlotUI>();

        string prefabPath = "Assets/SlotPrefab.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(slotPrefab, prefabPath);
        DestroyImmediate(slotPrefab); 

        generator.slotPrefab = savedPrefab;

        // ====================================================================
        // 【核心重构】 6. 彻底废弃 UI Toolkit，改用纯正的 UGUI 生成 HUD 状态栏
        // 这样所有的 UI 都在同一个物理层级和 EventSystem 调度下，绝对不会互相拦截！
        // ====================================================================
        
        GameObject hudManager = new GameObject("HUD_Manager");
        hudManager.transform.SetParent(canvasGo.transform, false);
        
        RectTransform hudRect = hudManager.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero; hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;
        
        HUDController hudCtrl = hudManager.AddComponent<HUDController>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 6.1 生成 HP 文本
        GameObject hpObj = new GameObject("HP_Text");
        hpObj.transform.SetParent(hudManager.transform, false);
        Text hpTxt = hpObj.AddComponent<Text>();
        hpTxt.font = defaultFont; hpTxt.fontSize = 30; hpTxt.color = Color.red;
        hpTxt.text = "HP: 100/100";
        hpTxt.raycastTarget = false; // 文本不挡射线
        RectTransform hpRect = hpObj.GetComponent<RectTransform>();
        hpRect.anchorMin = new Vector2(0, 1); hpRect.anchorMax = new Vector2(0, 1);
        hpRect.pivot = new Vector2(0, 1); hpRect.anchoredPosition = new Vector2(20, -20);
        hpRect.sizeDelta = new Vector2(300, 50);
        hudCtrl.hpLabel = hpTxt;

        // 6.2 生成 SAN 文本
        GameObject sanObj = new GameObject("SAN_Text");
        sanObj.transform.SetParent(hudManager.transform, false);
        Text sanTxt = sanObj.AddComponent<Text>();
        sanTxt.font = defaultFont; sanTxt.fontSize = 30; sanTxt.color = new Color(0.6f, 0.2f, 1f);
        sanTxt.text = "SAN: 50/50";
        sanTxt.raycastTarget = false;
        RectTransform sanRect = sanObj.GetComponent<RectTransform>();
        sanRect.anchorMin = new Vector2(0, 1); sanRect.anchorMax = new Vector2(0, 1);
        sanRect.pivot = new Vector2(0, 1); sanRect.anchoredPosition = new Vector2(20, -70);
        sanRect.sizeDelta = new Vector2(300, 50);
        hudCtrl.sanLabel = sanTxt;

        // 6.3 生成 AP 文本
        GameObject apObj = new GameObject("AP_Text");
        apObj.transform.SetParent(hudManager.transform, false);
        Text apTxt = apObj.AddComponent<Text>();
        apTxt.font = defaultFont; apTxt.fontSize = 40; apTxt.color = Color.cyan;
        apTxt.text = "AP: 3/3";
        apTxt.raycastTarget = false;
        RectTransform apRect = apObj.GetComponent<RectTransform>();
        apRect.anchorMin = new Vector2(0, 0); apRect.anchorMax = new Vector2(0, 0);
        apRect.pivot = new Vector2(0, 0); apRect.anchoredPosition = new Vector2(20, 20);
        apRect.sizeDelta = new Vector2(300, 80);
        hudCtrl.apLabel = apTxt;

        // 6.4 生成 结束回合 按钮
        GameObject btnObj = new GameObject("EndTurn_Button");
        btnObj.transform.SetParent(hudManager.transform, false);
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.8f, 0.2f, 0.2f);
        Button endBtn = btnObj.AddComponent<Button>();
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 0); btnRect.anchorMax = new Vector2(1, 0);
        btnRect.pivot = new Vector2(1, 0); btnRect.anchoredPosition = new Vector2(-20, 20);
        btnRect.sizeDelta = new Vector2(200, 80);
        hudCtrl.endTurnBtn = endBtn;

        GameObject btnTxtObj = new GameObject("Text");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        Text btnTxt = btnTxtObj.AddComponent<Text>();
        btnTxt.font = defaultFont; btnTxt.fontSize = 30; btnTxt.color = Color.white;
        btnTxt.text = "结束回合";
        btnTxt.alignment = TextAnchor.MiddleCenter;
        btnTxt.raycastTarget = false;
        RectTransform btnTxtRect = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero; btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;


        // 7. 顺便生成一个可拖拽物品的 Prefab 占位
        GameObject itemPrefab = new GameObject("ItemPrefab_TestSword");
        Image itemImg = itemPrefab.AddComponent<Image>();
        itemImg.color = Color.red; // 红色代表武器
        itemPrefab.AddComponent<DraggableItemUI>();
        itemPrefab.AddComponent<CanvasGroup>();

        // 给测试武器加上文字
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(itemPrefab.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.text = "长刀\n2AP";
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = defaultFont;
        txt.raycastTarget = false; // 极其重要：文字不能阻挡鼠标射线

        RectTransform txtRect = textObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        PrefabUtility.SaveAsPrefabAsset(itemPrefab, "Assets/ItemPrefab_TestSword.prefab");
        DestroyImmediate(itemPrefab);

        // 为了测试胶水代码的连通性，自动帮 MVPTester 挂载目标 Prefab
        var loadedTestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ItemPrefab_TestSword.prefab");
        if (loadedTestPrefab != null) {
            tester.GetComponent<MVPTester>().testItemPrefab = loadedTestPrefab;
        }

        Debug.Log("<color=green>[自动化构建] UGUI 版 MVP 场景骨架生成完毕！已彻底移除 UI Toolkit 的事件拦截！</color>");
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
