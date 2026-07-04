using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class CombatHUDIntentBindingSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Combat HUD Intent Binding Smoke Test ===");

            TestHudShowsMonsterIntentPreview();
            TestCombatHudStatusLayoutSpacing();
            TestCombatHudEnemyStageHasNoCardBacking();

            Debug.Log("=== Combat HUD Intent Binding Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[CombatHUDIntentBindingSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void TestHudShowsMonsterIntentPreview() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });

        GameObject canvasObj = CreateCanvas();
        GameObject hudObj = new GameObject("CombatHUDIntentBindingTestHUD");
        hudObj.SetActive(false);
        hudObj.transform.SetParent(canvasObj.transform, false);
        hudObj.AddComponent<RectTransform>();
        HUDController hud = hudObj.AddComponent<HUDController>();
        InvokeLifecycle(hud, "OnEnable");

        string text = CollectText(canvasObj);
        bool passed = text.Contains("敌方意图")
            && text.Contains("拾荒虫")
            && text.Contains("攻击")
            && text.Contains("造成约 10 伤害");

        if (passed) {
            Debug.Log("Combat HUD Intent Binding PASSED.");
        } else {
            Debug.LogError($"Combat HUD Intent Binding FAILED. Text={text}");
        }

        InvokeLifecycle(hud, "OnDisable");
        Object.DestroyImmediate(canvasObj);
        GameRoot.Core = null;
    }

    private static void TestCombatHudStatusLayoutSpacing() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });

        GameObject canvasObj = CreateCanvas();
        GameObject hudObj = new GameObject("CombatHUDStatusLayoutTestHUD");
        hudObj.SetActive(false);
        hudObj.transform.SetParent(canvasObj.transform, false);
        hudObj.AddComponent<RectTransform>();
        HUDController hud = hudObj.AddComponent<HUDController>();
        InvokeLifecycle(hud, "OnEnable");

        RectTransform panel = FindRectRecursive(canvasObj.transform, "PlayerStatusCluster");
        RectTransform hpText = FindRectRecursive(panel, "HP_Text");
        RectTransform hpBar = FindRectRecursive(panel, "HpBar");
        RectTransform shieldText = FindRectRecursive(panel, "Shield_Text");
        RectTransform shieldBar = FindRectRecursive(panel, "ShieldBar");
        RectTransform sanText = FindRectRecursive(panel, "SAN_Text");
        RectTransform apText = FindRectRecursive(panel, "AP_Text");
        RectTransform apPips = FindRectRecursive(panel, "ApPips");

        bool allPresent = panel != null
            && hpText != null
            && hpBar != null
            && shieldText != null
            && shieldBar != null
            && sanText != null
            && apText != null
            && apPips != null;
        bool noStatusOverlap = allPresent
            && !RectsOverlap(hpText, hpBar)
            && !RectsOverlap(shieldText, shieldBar)
            && !RectsOverlap(sanText, apText)
            && !RectsOverlap(apText, apPips);
        bool panelHasBreathingRoom = panel != null && panel.sizeDelta.x >= 540f && panel.sizeDelta.y >= 200f;

        if (noStatusOverlap && panelHasBreathingRoom) {
            Debug.Log("Combat HUD Status Layout PASSED.");
        } else {
            Debug.LogError($"Combat HUD Status Layout FAILED. Present={allPresent}, NoOverlap={noStatusOverlap}, Panel={panel?.sizeDelta.ToString() ?? "null"}");
        }

        InvokeLifecycle(hud, "OnDisable");
        Object.DestroyImmediate(canvasObj);
        GameRoot.Core = null;
    }

    private static void TestCombatHudEnemyStageHasNoCardBacking() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });

        GameObject canvasObj = CreateCanvas();
        GameObject hudObj = new GameObject("CombatHUDEnemyBackingTestHUD");
        hudObj.SetActive(false);
        hudObj.transform.SetParent(canvasObj.transform, false);
        hudObj.AddComponent<RectTransform>();
        HUDController hud = hudObj.AddComponent<HUDController>();
        InvokeLifecycle(hud, "OnEnable");

        RectTransform enemyStage = FindRectRecursive(canvasObj.transform, "EnemyStageRoot");
        Image enemyStageImage = enemyStage != null ? enemyStage.GetComponent<Image>() : null;
        Image hotspotImage = FindImageRecursive(canvasObj.transform, "EnemyClickHotspot_Button");
        Image enemyShadow = FindImageRecursive(canvasObj.transform, "EnemyShadow_Image");
        bool noStageCard = enemyStageImage == null || enemyStageImage.color.a <= 0.01f;
        bool noHotspotTint = hotspotImage == null || hotspotImage.color.a <= 0.01f;
        bool softShadowOnly = enemyShadow == null || enemyShadow.color.a <= 0.24f;

        if (noStageCard && noHotspotTint && softShadowOnly) {
            Debug.Log("Combat HUD Enemy Stage Backing PASSED.");
        } else {
            Debug.LogError($"Combat HUD Enemy Stage Backing FAILED. StageAlpha={enemyStageImage?.color.a ?? -1f}, HotspotAlpha={hotspotImage?.color.a ?? -1f}, ShadowAlpha={enemyShadow?.color.a ?? -1f}");
        }

        InvokeLifecycle(hud, "OnDisable");
        Object.DestroyImmediate(canvasObj);
        GameRoot.Core = null;
    }

    private static CoreBackend CreateCoreWithEmptyGrid() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        GridSolver.RecalculateAllEffects(doll);
        return core;
    }

    private static GameObject CreateCanvas() {
        GameObject canvasObj = new GameObject("CombatHUDIntentBindingTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    private static string CollectText(GameObject root) {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        return string.Join("\n", texts.Select(text => text != null ? text.text : string.Empty));
    }

    private static RectTransform FindRectRecursive(Transform root, string objectName) {
        if (root == null || string.IsNullOrEmpty(objectName)) {
            return null;
        }

        if (root.name == objectName) {
            return root as RectTransform;
        }

        for (int i = 0; i < root.childCount; i++) {
            RectTransform match = FindRectRecursive(root.GetChild(i), objectName);
            if (match != null) {
                return match;
            }
        }

        return null;
    }

    private static Image FindImageRecursive(Transform root, string objectName) {
        RectTransform rect = FindRectRecursive(root, objectName);
        return rect != null ? rect.GetComponent<Image>() : null;
    }

    private static bool RectsOverlap(RectTransform first, RectTransform second) {
        if (first == null || second == null) {
            return true;
        }

        Rect a = ToLocalRect(first);
        Rect b = ToLocalRect(second);
        return a.xMin < b.xMax
            && a.xMax > b.xMin
            && a.yMin < b.yMax
            && a.yMax > b.yMin;
    }

    private static Rect ToLocalRect(RectTransform rect) {
        Vector2 size = rect.sizeDelta;
        Vector2 position = rect.anchoredPosition;
        float xMin = position.x - size.x * rect.pivot.x;
        float yMin = position.y - size.y * rect.pivot.y;
        return new Rect(xMin, yMin, size.x, size.y);
    }

    private static void InvokeLifecycle(MonoBehaviour target, string methodName) {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }
}
