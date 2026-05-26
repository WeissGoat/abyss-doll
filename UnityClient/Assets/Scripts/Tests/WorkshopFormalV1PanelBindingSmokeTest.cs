using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public static class WorkshopFormalV1PanelBindingSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Workshop Formal V1 Panel Binding Smoke Test ===");

            TestMaintenancePanelUsesReadOnlyGrowthSnapshot();
            TestDailyBillPanelUsesReadOnlyEconomySnapshot();
            TestWorkshopMainFlowCanOpenFormalV1Panels();
            TestMaintenancePanelButtonAppliesBackendService();
            TestDollInteractionPanelButtonsApplyBackendService();

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

    private static void TestWorkshopMainFlowCanOpenFormalV1Panels() {
        CreateCore();
        GameObject canvasObj = CreateCanvas();
        WorkshopUIController workshop = CreateWorkshopUI(canvasObj.transform);

        workshop.SendMessage("Start");
        bool buttonsCreated = workshop.openMaintenancePanelBtn != null
            && workshop.openDollInteractionPanelBtn != null
            && workshop.openOrderBoardPanelBtn != null;

        workshop.openMaintenancePanelBtn.onClick.Invoke();
        WorkshopFormalV1PanelController formalController = workshop.GetComponent<WorkshopFormalV1PanelController>();
        bool maintenanceOpened = formalController != null && formalController.CurrentScreenID == "maintenance_panel";

        workshop.openDollInteractionPanelBtn.onClick.Invoke();
        bool dollInteractionOpened = formalController != null && formalController.CurrentScreenID == "doll_interaction";

        bool passed = buttonsCreated && maintenanceOpened && dollInteractionOpened;
        if (passed) {
            Debug.Log("Workshop Formal V1 Main Flow Entry PASSED.");
        } else {
            Debug.LogError($"Workshop Formal V1 Main Flow Entry FAILED. ButtonsCreated={buttonsCreated}, MaintenanceOpened={maintenanceOpened}, DollInteractionOpened={dollInteractionOpened}, Screen={formalController?.CurrentScreenID}");
        }

        Object.DestroyImmediate(canvasObj);
    }

    private static void TestMaintenancePanelButtonAppliesBackendService() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        player.Money = 250;
        player.StashInventory.Clear();
        ItemEntity scrap = ConfigManager.CreateItem("loot_gear_scrap");
        player.StashInventory.Add(scrap);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        doll.Status.HP_Current = Mathf.Max(1, doll.Status.HP_Max - 10);

        GameObject canvasObj = CreateCanvas();
        WorkshopFormalV1PanelController controller = CreateController(canvasObj.transform);
        controller.Show("maintenance_panel");

        Button repairButton = FindButton(canvasObj, "FullRepair_Button");
        repairButton?.onClick.Invoke();

        bool passed = repairButton != null
            && controller.CurrentScreenID == "maintenance_panel"
            && player.Money == 200
            && !player.StashInventory.Contains(scrap)
            && Mathf.Approximately(doll.Status.WearAndTear, DiveReadinessService.ExtremeWearThreshold - 40f)
            && doll.Status.HP_Current == doll.Status.HP_Max
            && CollectText(canvasObj).Contains("Applied maintenance");

        if (passed) {
            Debug.Log("Workshop Formal V1 Maintenance Button Backend Action PASSED.");
        } else {
            Debug.LogError($"Workshop Formal V1 Maintenance Button Backend Action FAILED. Button={repairButton != null}, Screen={controller.CurrentScreenID}, Money={player.Money}, StashContains={player.StashInventory.Contains(scrap)}, Wear={doll.Status.WearAndTear}, HP={doll.Status.HP_Current}/{doll.Status.HP_Max}, Text={CollectText(canvasObj)}");
        }

        controller.Hide();
        Object.DestroyImmediate(canvasObj);
    }

    private static void TestDollInteractionPanelButtonsApplyBackendService() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        doll.Bond.AffectionLevel = 0;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        player.DollInteractionState = new DollInteractionRuntimeState();

        GameObject canvasObj = CreateCanvas();
        WorkshopFormalV1PanelController controller = CreateController(canvasObj.transform);
        controller.Show("doll_interaction");

        Button touchButton = FindButton(canvasObj, "Touch_Button");
        touchButton?.onClick.Invoke();
        int bondAfterTouch = doll.Bond.AffectionLevel;

        Button talkButton = FindButton(canvasObj, "Talk_Button");
        talkButton?.onClick.Invoke();

        var dailyState = player.DollInteractionState.GetOrCreateDailyState(player.CurrentDay);
        bool passed = touchButton != null
            && talkButton != null
            && controller.CurrentScreenID == "doll_interaction"
            && bondAfterTouch == 1
            && doll.Bond.AffectionLevel == 2
            && dailyState.TotalTouchCount == 1
            && dailyState.TotalTalkCount == 1
            && CollectText(canvasObj).Contains("Daily talk accepted");

        if (passed) {
            Debug.Log("Workshop Formal V1 Doll Interaction Button Backend Action PASSED.");
        } else {
            Debug.LogError($"Workshop Formal V1 Doll Interaction Button Backend Action FAILED. TouchButton={touchButton != null}, TalkButton={talkButton != null}, Screen={controller.CurrentScreenID}, BondAfterTouch={bondAfterTouch}, Bond={doll.Bond.AffectionLevel}, TouchCount={dailyState.TotalTouchCount}, TalkCount={dailyState.TotalTalkCount}, Text={CollectText(canvasObj)}");
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

    private static WorkshopUIController CreateWorkshopUI(Transform parent) {
        GameObject workshopObj = new GameObject("WorkshopFormalV1MainFlowTestController");
        workshopObj.transform.SetParent(parent, false);
        workshopObj.AddComponent<RectTransform>();
        return workshopObj.AddComponent<WorkshopUIController>();
    }

    private static Button FindButton(GameObject root, string buttonName) {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        return buttons.FirstOrDefault(button => button != null && button.name == buttonName);
    }

    private static string CollectText(GameObject root) {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        return string.Join("\n", texts.Select(text => text != null ? text.text : string.Empty));
    }
}
