using UnityEngine;
using UnityEngine.UI;

public static class PrologueHalfOpenNarrativeFlowSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Prologue Half Open Narrative Flow Smoke Test ===");

        ConfigManager.LoadAllConfigs();
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        GameObject canvasObj = new GameObject("PrologueHalfOpenCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController controller = workshopObj.AddComponent<WorkshopUIController>();
        controller.moneyText = CreateTestText(workshopObj.transform, "Money_Text");
        controller.chassisInfoText = CreateTestText(workshopObj.transform, "Chassis_Text");
        controller.departBtn = CreateTestButton(workshopObj.transform, "Depart_Button");
        controller.upgradeBtn = CreateTestButton(workshopObj.transform, "Upgrade_Button");

        controller.RefreshUI();

        PrologueHalfOpenNarrativeFlow flow = new PrologueHalfOpenNarrativeFlow(ConfigManager.Narrative, controller);
        PrologueHalfOpenFlowResult result = flow.PlayCoreWipedToLayerConfirmRequest();

        bool nodesPlayed = result.Nodes.Count == 2
            && result.Nodes[0].NodeID == PrologueHalfOpenNarrativeFlow.FirstDiveUnlockNodeID
            && result.Nodes[1].NodeID == PrologueHalfOpenNarrativeFlow.Layer1ConfirmNodeID;

        bool halfOpenUi = controller.IsPrologueHalfOpen
            && controller.IsPrologueShallowGateAvailable
            && controller.departBtn != null
            && controller.departBtn.gameObject.activeSelf
            && ButtonText(controller.departBtn) == "浅层入口";

        bool hiddenSystems = Hidden(controller.openSellPanelBtn)
            && Hidden(controller.openProstheticPanelBtn)
            && Hidden(controller.openMaintenancePanelBtn)
            && Hidden(controller.openOrderBoardPanelBtn)
            && Hidden(controller.openRumorBoardPanelBtn)
            && Hidden(controller.openFactionShopPanelBtn)
            && Hidden(controller.upgradeBtn);

        bool noGenericLayerPanel = !controller.IsDungeonStartLayerPanelOpen;
        bool actionPublished = result.WorkshopActionIDs.Count == 1
            && result.WorkshopActionIDs[0] == WorkshopUIController.PrologueOpenLayer1ConfirmActionID;
        bool layerConfirmRequested = result.LayerConfirmRequests == 1
            && result.State.GetFlag("Layer1ConfirmOpened");
        string text = CollectText(canvasObj);
        bool playerReadablePressureHint = text.Contains("压力稳定")
            && text.Contains("零号还能撑一次")
            && text.Contains("维护")
            && text.Contains("稍后");

        if (result.Success
            && nodesPlayed
            && halfOpenUi
            && hiddenSystems
            && noGenericLayerPanel
            && actionPublished
            && layerConfirmRequested
            && playerReadablePressureHint) {
            Debug.Log("Prologue Half Open Narrative Flow Smoke PASSED.");
        } else {
            Debug.LogError(
                "Prologue Half Open Narrative Flow Smoke FAILED. "
                + $"Success={result.Success}, Nodes={nodesPlayed}, HalfOpenUI={halfOpenUi}, HiddenSystems={hiddenSystems}, "
                + $"NoGenericPanel={noGenericLayerPanel}, Action={actionPublished}, LayerConfirm={layerConfirmRequested}, "
                + $"Hint={playerReadablePressureHint}, ActionsSeen={string.Join("|", result.WorkshopActionIDs)}, "
                + $"Unlocks={string.Join("|", result.UnlockGateIDs)}, Text={text}, Errors={string.Join("|", result.Errors)}");
        }

        Object.DestroyImmediate(canvasObj);
        Debug.Log("=== Prologue Half Open Narrative Flow Smoke Test Finished ===");
    }

    private static Text CreateTestText(Transform parent, string objectName) {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        return obj.AddComponent<Text>();
    }

    private static Button CreateTestButton(Transform parent, string objectName) {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>();
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text label = textObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        return obj.AddComponent<Button>();
    }

    private static bool Hidden(Button button) {
        return button == null || !button.gameObject.activeSelf;
    }

    private static string ButtonText(Button button) {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        return text != null ? text.text : string.Empty;
    }

    private static string CollectText(GameObject root) {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        string combined = string.Empty;
        for (int i = 0; i < texts.Length; i++) {
            if (texts[i] == null) {
                continue;
            }

            if (!string.IsNullOrEmpty(combined)) {
                combined += "\n";
            }

            combined += texts[i].text;
        }

        return combined;
    }
}
