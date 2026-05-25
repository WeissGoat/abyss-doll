using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class WorkshopFormalV1PanelBindingSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Workshop Formal V1 Panel Binding Smoke Test ===");

            TestMaintenancePanelUsesReadOnlyGrowthSnapshot();
            TestDailyBillPanelUsesReadOnlyEconomySnapshot();

            Debug.Log("=== Workshop Formal V1 Panel Binding Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[WorkshopFormalV1PanelBindingSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void TestMaintenancePanelUsesReadOnlyGrowthSnapshot() {
        CoreBackend core = CreateCore();
        GameObject canvasObj = CreateCanvas();
        WorkshopFormalV1PanelController controller = CreateController(canvasObj.transform);

        core.CurrentPlayer.ActiveDoll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        controller.Show("maintenance_panel");

        string text = CollectText(canvasObj);
        bool passed = controller.CurrentScreenID == "maintenance_panel"
            && text.Contains("人偶状态")
            && text.Contains("下潜许可")
            && text.Contains("HP")
            && text.Contains("SAN")
            && !text.Contains("Doll condition");

        if (passed) {
            Debug.Log("Workshop Formal V1 Maintenance Binding PASSED.");
        } else {
            Debug.LogError($"Workshop Formal V1 Maintenance Binding FAILED. Screen={controller.CurrentScreenID}, Text={text}");
        }

        controller.Hide();
        Object.DestroyImmediate(canvasObj);
    }

    private static void TestDailyBillPanelUsesReadOnlyEconomySnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.StashInventory.Clear();
        player.StashInventory.Add(ConfigManager.CreateItem("loot_gear_scrap"));
        TownEconomyService.RefreshWeeklyEconomy(player, 42);

        int orderCountBefore = player.ActiveOrders.Count;
        int rumorCountBefore = player.ActiveRumors.Count;
        int refreshWeekBefore = player.LastEconomyRefreshWeek;

        GameObject canvasObj = CreateCanvas();
        WorkshopFormalV1PanelController controller = CreateController(canvasObj.transform);
        controller.Show("daily_bill_report");

        string text = CollectText(canvasObj);
        bool stateUnchanged = player.ActiveOrders.Count == orderCountBefore
            && player.ActiveRumors.Count == rumorCountBefore
            && player.LastEconomyRefreshWeek == refreshWeekBefore;
        bool passed = controller.CurrentScreenID == "daily_bill_report"
            && stateUnchanged
            && text.Contains("账单摘要")
            && text.Contains("金币")
            && text.Contains("可售")
            && !text.Contains("Income summary")
            && !text.Contains("Unsold gear");

        if (passed) {
            Debug.Log("Workshop Formal V1 Daily Bill Binding PASSED.");
        } else {
            Debug.LogError($"Workshop Formal V1 Daily Bill Binding FAILED. Screen={controller.CurrentScreenID}, StateUnchanged={stateUnchanged}, Text={text}");
        }

        controller.Hide();
        Object.DestroyImmediate(canvasObj);
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        return core;
    }

    private static GameObject CreateCanvas() {
        GameObject canvasObj = new GameObject("WorkshopFormalV1BindingTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    private static WorkshopFormalV1PanelController CreateController(Transform parent) {
        GameObject controllerObj = new GameObject("WorkshopFormalV1BindingTestController");
        controllerObj.transform.SetParent(parent, false);
        controllerObj.AddComponent<RectTransform>();
        return controllerObj.AddComponent<WorkshopFormalV1PanelController>();
    }

    private static string CollectText(GameObject root) {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        return string.Join("\n", texts.Select(text => text != null ? text.text : string.Empty));
    }
}
