using UnityEngine;
using UnityEngine.UI;

public static class PrologueFirstDiveDepartureSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Prologue First Dive Departure Smoke Test ===");

        bool successPath = RunSuccessPath(out string successDiagnostic);
        bool failurePath = RunFailurePath(out string failureDiagnostic);
        bool reentryPath = RunReentryPath(out string reentryDiagnostic);

        if (successPath && failurePath && reentryPath) {
            Debug.Log("Prologue First Dive Departure Smoke PASSED.");
        } else {
            Debug.LogError(
                "Prologue First Dive Departure Smoke FAILED. "
                + $"SuccessPath={successPath} [{successDiagnostic}], "
                + $"FailurePath={failurePath} [{failureDiagnostic}], "
                + $"Reentry={reentryPath} [{reentryDiagnostic}]");
        }

        Debug.Log("=== Prologue First Dive Departure Smoke Test Finished ===");
    }

    private static bool RunSuccessPath(out string diagnostic) {
        TestHarness harness = CreateHarness("PrologueFirstDiveDepartureSuccess");
        int runStarted = 0;
        int rejected = 0;
        int nodeEntered = 0;
        int lootPrepared = 0;
        int lootCollected = 0;
        int settled = 0;
        bool departRequested = false;

        DungeonEventBus.OnDungeonRunStarted += layerID => {
            if (layerID == 1) {
                runStarted++;
            }
        };
        DungeonEventBus.OnDungeonStartLayerRejected += (layerID, reason) => rejected++;
        DungeonEventBus.OnNodeEntered += (node, sanCost) => nodeEntered++;
        DungeonEventBus.OnCombatLootPrepared += result => lootPrepared++;
        DungeonEventBus.OnCombatLootCollected += result => lootCollected++;
        DungeonEventBus.OnDungeonSettled += victory => settled++;
        harness.Controller.FirstDiveDepartRequested += () => departRequested = true;

        OpenConfirmAndClickDepart(harness);

        bool lineSequence = HasLineSequence(harness.Controller.LastDepartureResult);
        bool enteredLayerOne = harness.Core.Dungeon.CurrentLayer != null
            && harness.Core.Dungeon.CurrentLayer.LayerID == 1
            && harness.Core.Dungeon.CurrentLayer.EntryNodes != null
            && harness.Core.Dungeon.CurrentLayer.EntryNodes.Count > 0
            && harness.Core.Dungeon.CurrentLayer.CurrentNode == null;
        bool noDownstreamLoop = nodeEntered == 0
            && lootPrepared == 0
            && lootCollected == 0
            && settled == 0;
        bool panelClosed = !harness.Workshop.IsDungeonStartLayerPanelOpen;
        bool state = harness.Controller.NarrativeState.GetFlag("Layer1FirstDeparted")
            && harness.Controller.ResolveReentryStage() == PrologueFirstDiveReentryStage.Layer1Run;

        diagnostic =
            $"Depart={departRequested}, Lines={lineSequence}, RunStarted={runStarted}, Rejected={rejected}, "
            + $"Entered={enteredLayerOne}, NoDownstream={noDownstreamLoop}, Closed={panelClosed}, State={state}";

        UnityEngine.Object.DestroyImmediate(harness.CanvasObject);
        DungeonEventBus.ResetAllListeners();
        return departRequested
            && lineSequence
            && harness.Controller.FirstDiveStartRunSucceeded
            && runStarted == 1
            && rejected == 0
            && enteredLayerOne
            && noDownstreamLoop
            && panelClosed
            && state;
    }

    private static bool RunFailurePath(out string diagnostic) {
        TestHarness harness = CreateHarness("PrologueFirstDiveDepartureFailure");
        int runStarted = 0;
        int rejected = 0;
        bool departRequested = false;

        DungeonEventBus.OnDungeonRunStarted += layerID => runStarted++;
        DungeonEventBus.OnDungeonStartLayerRejected += (layerID, reason) => rejected++;
        harness.Controller.FirstDiveDepartRequested += () => departRequested = true;
        harness.Controller.BeforeFirstDiveStartRun += () => {
            harness.Core.CurrentPlayer.ActiveDoll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        };

        OpenConfirmAndClickDepart(harness);

        string text = CollectText(harness.Workshop.dungeonStartLayerPanel);
        bool playerReadableBlocker = text.Contains("磨损太高")
            && !text.Contains("ExtremeWear")
            && !text.Contains("Wear is too high")
            && !text.Contains("DiveReadinessIssueCode");
        bool stayedOnConfirm = harness.Workshop.IsDungeonStartLayerPanelOpen
            && harness.Core.Dungeon.CurrentLayer == null
            && !harness.Controller.FirstDiveStartRunSucceeded
            && !harness.Controller.NarrativeState.GetFlag("Layer1FirstDeparted");
        bool departurePlayed = harness.Controller.LastDepartureResult != null
            && harness.Controller.LastDepartureResult.Success
            && HasLineSequence(harness.Controller.LastDepartureResult);

        diagnostic =
            $"Depart={departRequested}, DeparturePlayed={departurePlayed}, RunStarted={runStarted}, Rejected={rejected}, "
            + $"Stayed={stayedOnConfirm}, TextOK={playerReadableBlocker}, Text={text}";

        UnityEngine.Object.DestroyImmediate(harness.CanvasObject);
        DungeonEventBus.ResetAllListeners();
        return departRequested
            && departurePlayed
            && runStarted == 0
            && rejected == 1
            && stayedOnConfirm
            && playerReadableBlocker;
    }

    private static bool RunReentryPath(out string diagnostic) {
        NarrativeStateStore newGame = new NarrativeStateStore();

        NarrativeStateStore startedNotWiped = new NarrativeStateStore();
        startedNotWiped.SetFlag("No0Started");

        NarrativeStateStore unlockedNotDeparted = new NarrativeStateStore();
        unlockedNotDeparted.SetFlag("No0Started");
        unlockedNotDeparted.SetFlag("FirstCoreWiped");
        unlockedNotDeparted.SetFlag("FirstDiveUnlocked");

        NarrativeStateStore departed = new NarrativeStateStore();
        departed.SetFlag("Layer1FirstDeparted");

        PrologueFirstDiveReentryStage newStage = PrologueFirstDiveReentryResolver.Resolve(newGame);
        PrologueFirstDiveReentryStage statusStage = PrologueFirstDiveReentryResolver.Resolve(startedNotWiped);
        PrologueFirstDiveReentryStage halfOpenStage = PrologueFirstDiveReentryResolver.Resolve(unlockedNotDeparted);
        PrologueFirstDiveReentryStage layerStage = PrologueFirstDiveReentryResolver.Resolve(departed);

        diagnostic = $"New={newStage}, Status={statusStage}, HalfOpen={halfOpenStage}, Layer={layerStage}";
        return newStage == PrologueFirstDiveReentryStage.OpeningBlackScreen
            && statusStage == PrologueFirstDiveReentryStage.StatusCard
            && halfOpenStage == PrologueFirstDiveReentryStage.HalfOpenWorkshop
            && layerStage == PrologueFirstDiveReentryStage.Layer1Run;
    }

    private static void OpenConfirmAndClickDepart(TestHarness harness) {
        harness.Controller.RequestLayerConfirm(new NarrativeLayerConfirmCommandRequest {
            Context = "layer_confirm",
            SourceNode = "T0_01A_Layer1Confirm",
            LayerID = 1,
            FirstDiveOnly = true
        });

        DungeonStartLayerUIController layerController = harness.Workshop.dungeonStartLayerPanel
            .GetComponentInChildren<DungeonStartLayerUIController>(true);
        layerController?.confirmBtn?.onClick.Invoke();
    }

    private static bool HasLineSequence(PrologueFirstDiveDepartureFlowResult result) {
        return result != null
            && result.Success
            && result.LineKeys.Count == 3
            && result.LineKeys[0] == "prologue.t0_01a.depart.line_01"
            && result.LineKeys[1] == "prologue.t0_01a.depart.line_02"
            && result.LineKeys[2] == "prologue.t0_01a.depart.line_03"
            && result.VisualSequence.Contains(PrologueFirstDiveDepartureFlow.DepartBlackVisualID);
    }

    private static TestHarness CreateHarness(string name) {
        ConfigManager.LoadAllConfigs();
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        GameObject canvasObj = new GameObject(name + "_Canvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject(name + "_WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();

        WorkshopUIController workshop = workshopObj.AddComponent<WorkshopUIController>();
        workshop.moneyText = CreateTestText(workshopObj.transform, "Money_Text");
        workshop.chassisInfoText = CreateTestText(workshopObj.transform, "Chassis_Text");
        workshop.departBtn = CreateTestButton(workshopObj.transform, "Depart_Button");
        workshop.upgradeBtn = CreateTestButton(workshopObj.transform, "Upgrade_Button");
        workshop.EnterPrologueHalfOpen();

        PrologueFirstDiveController controller = workshopObj.AddComponent<PrologueFirstDiveController>();
        controller.workshopController = workshop;

        return new TestHarness {
            Core = core,
            CanvasObject = canvasObj,
            Workshop = workshop,
            Controller = controller
        };
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

    private sealed class TestHarness {
        public CoreBackend Core;
        public GameObject CanvasObject;
        public WorkshopUIController Workshop;
        public PrologueFirstDiveController Controller;
    }
}
