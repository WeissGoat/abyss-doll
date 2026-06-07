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

    private static void InvokeLifecycle(MonoBehaviour target, string methodName) {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }
}
