using UnityEngine;
using UnityEngine.UI;

public static class PrologueFirstDiveLayerConfirmSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Prologue First Dive Layer Confirm Smoke Test ===");

        ConfigManager.LoadAllConfigs();
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        GameObject canvasObj = new GameObject("PrologueFirstDiveConfirmCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController workshop = workshopObj.AddComponent<WorkshopUIController>();
        workshop.moneyText = CreateTestText(workshopObj.transform, "Money_Text");
        workshop.chassisInfoText = CreateTestText(workshopObj.transform, "Chassis_Text");
        workshop.departBtn = CreateTestButton(workshopObj.transform, "Depart_Button");
        workshop.upgradeBtn = CreateTestButton(workshopObj.transform, "Upgrade_Button");
        workshop.EnterPrologueHalfOpen();

        bool departRequested = false;
        workshop.OpenFirstDiveLayerConfirmPanel(() => departRequested = true);
        DungeonStartLayerUIController layerController = workshop.dungeonStartLayerPanel.GetComponentInChildren<DungeonStartLayerUIController>(true);

        bool firstDiveMode = layerController != null
            && layerController.IsFirstDiveMode
            && layerController.SelectedLayerID == 1;
        bool onePermitCardOnly = layerController != null
            && layerController.HasFirstDivePermitCard
            && CountPermitCards(layerController) == 1
            && CountLayerRows(layerController) == 0
            && layerController.PermitTitleText.Contains("第一层")
            && layerController.PermitSummaryText.Contains("许可");
        bool buttonLabels = ButtonText(layerController?.confirmBtn) == "出发"
            && ButtonText(layerController?.closeBtn) == "再看她一眼";
        bool canDepart = layerController?.confirmBtn != null && layerController.confirmBtn.interactable;
        layerController?.confirmBtn?.onClick.Invoke();
        bool departIsRequestOnly = departRequested
            && core.Dungeon.CurrentLayer == null;

        bool returnedToWorkshop = false;
        layerController?.closeBtn?.onClick.Invoke();
        returnedToWorkshop = !workshop.IsDungeonStartLayerPanelOpen
            && workshop.IsPrologueHalfOpen
            && workshop.IsPrologueShallowGateAvailable;

        bool blockedDepartRequested = false;
        core.CurrentPlayer.ActiveDoll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        workshop.OpenFirstDiveLayerConfirmPanel(() => blockedDepartRequested = true);
        layerController = workshop.dungeonStartLayerPanel.GetComponentInChildren<DungeonStartLayerUIController>(true);
        string blockedText = CollectText(workshop.dungeonStartLayerPanel);
        bool blockedState = layerController != null
            && layerController.IsFirstDiveMode
            && layerController.SelectedLayerID == 1
            && layerController.confirmBtn != null
            && !layerController.confirmBtn.interactable
            && blockedText.Contains("磨损太高")
            && !blockedText.Contains("ExtremeWear")
            && !blockedText.Contains("Wear is too high")
            && !blockedText.Contains("DiveReadinessIssueCode");
        layerController?.confirmBtn?.onClick.Invoke();
        bool blockedDoesNotDepart = !blockedDepartRequested
            && core.Dungeon.CurrentLayer == null;

        core.CurrentPlayer.ActiveDoll.Status.WearAndTear = 0f;
        PrologueFirstDiveController narrativeController = workshopObj.AddComponent<PrologueFirstDiveController>();
        narrativeController.workshopController = workshop;
        bool narrativeDepartRequested = false;
        int narrativeRunStarted = 0;
        narrativeController.FirstDiveDepartRequested += () => narrativeDepartRequested = true;
        DungeonEventBus.OnDungeonRunStarted += layerID => {
            if (layerID == 1) {
                narrativeRunStarted++;
            }
        };
        narrativeController.RequestLayerConfirm(new NarrativeLayerConfirmCommandRequest {
            Context = "layer_confirm",
            SourceNode = "T0_01A_Layer1Confirm",
            LayerID = 1,
            FirstDiveOnly = true
        });
        layerController = workshop.dungeonStartLayerPanel.GetComponentInChildren<DungeonStartLayerUIController>(true);
        bool narrativeOpensFirstDive = narrativeController.FirstDiveLayerConfirmOpened
            && narrativeController.LastLayerConfirmLayerID == 1
            && layerController != null
            && layerController.IsFirstDiveMode
            && layerController.SelectedLayerID == 1;
        layerController?.confirmBtn?.onClick.Invoke();
        bool narrativeDepartStartsFirstDive = narrativeDepartRequested
            && narrativeController.LastDepartureResult != null
            && narrativeController.LastDepartureResult.Success
            && narrativeController.FirstDiveStartRunSucceeded
            && narrativeRunStarted == 1
            && core.Dungeon.CurrentLayer != null
            && core.Dungeon.CurrentLayer.LayerID == 1;

        if (firstDiveMode
            && onePermitCardOnly
            && buttonLabels
            && canDepart
            && departIsRequestOnly
            && returnedToWorkshop
            && blockedState
            && blockedDoesNotDepart
            && narrativeOpensFirstDive
            && narrativeDepartStartsFirstDive) {
            Debug.Log("Prologue First Dive Layer Confirm Smoke PASSED.");
        } else {
            Debug.LogError(
                "Prologue First Dive Layer Confirm Smoke FAILED. "
                + $"FirstDive={firstDiveMode}, PermitCard={onePermitCardOnly}, Buttons={buttonLabels}, CanDepart={canDepart}, "
                + $"DepartRequestOnly={departIsRequestOnly}, Returned={returnedToWorkshop}, Blocked={blockedState}, "
                + $"BlockedNoDepart={blockedDoesNotDepart}, NarrativeOpen={narrativeOpensFirstDive}, NarrativeDepartStart={narrativeDepartStartsFirstDive}, "
                + $"Rows={CountLayerRows(layerController)}, Confirm={ButtonText(layerController?.confirmBtn)}, Close={ButtonText(layerController?.closeBtn)}, "
                + $"Text={blockedText}");
        }

        Object.DestroyImmediate(canvasObj);
        Debug.Log("=== Prologue First Dive Layer Confirm Smoke Test Finished ===");
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

    private static int CountLayerRows(DungeonStartLayerUIController controller) {
        if (controller?.listParent == null) {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < controller.listParent.childCount; i++) {
            Transform child = controller.listParent.GetChild(i);
            if (child != null && child.name.StartsWith("DungeonStartLayer_")) {
                count++;
            }
        }

        return count;
    }

    private static int CountPermitCards(DungeonStartLayerUIController controller) {
        if (controller?.listParent == null) {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < controller.listParent.childCount; i++) {
            Transform child = controller.listParent.GetChild(i);
            if (child != null && child.name == "FirstDivePermitCard") {
                count++;
            }
        }

        return count;
    }

    private static string ButtonText(Button button) {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        return text != null ? text.text : string.Empty;
    }

    private static string CollectText(GameObject root) {
        if (root == null) {
            return string.Empty;
        }

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
