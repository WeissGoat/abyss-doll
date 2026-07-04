using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Live2DAcceptanceRunner : MonoBehaviour {
    public const string TriggerCommand = "RUN_LIVE2D_ACCEPTANCE";
    public const string OutputRootRelative = "UnityClient/Logs/Live2DAcceptance/latest";

    private const string SchemaVersion = "1.0";
    private const string ProjectName = "P3";
    private const int ReferenceWidth = 1920;
    private const int ReferenceHeight = 1080;

    private static Live2DAcceptanceRunner activeRunner;

    private readonly Live2DAcceptanceReport report = new Live2DAcceptanceReport();
    private readonly List<Live2DAcceptanceCaptureStep> captureSteps = new List<Live2DAcceptanceCaptureStep> {
        new Live2DAcceptanceCaptureStep("idle_0s", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Normal, false, false, false, 1f, 0f),
        new Live2DAcceptanceCaptureStep("idle_1s", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Blink, false, false, false, 1f, 0f),
        new Live2DAcceptanceCaptureStep("expression_2s", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Relaxed, false, false, false, 1f, 0f),
        new Live2DAcceptanceCaptureStep("low_san_idle_3s", DollLive2DMotionIDs.LowSanIdle, DollLive2DExpressionIDs.LowSan, true, false, false, 0.45f, 0.85f),
        new Live2DAcceptanceCaptureStep("repair_react", DollLive2DMotionIDs.RepairReact, DollLive2DExpressionIDs.Relaxed, false, true, false, 1f, 0f),
        new Live2DAcceptanceCaptureStep("hit_react", DollLive2DMotionIDs.HitReact, DollLive2DExpressionIDs.Hurt, false, false, true, 0.3f, 0.7f),
        new Live2DAcceptanceCaptureStep("fallback", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Normal, false, false, false, 1f, 0f, true)
    };

    private string runID;
    private string outputRoot;
    private string screenshotsRoot;
    private bool autoExitPlayMode;
    private GameObject stageRoot;
    private Camera captureCamera;
    private Canvas canvas;
    private DollLive2DUGUIBridge bridge;
    private IDollLive2DPresenter presenter;
    private DollLive2DVisualLoadResult visualResult;
    private string presenterMode = "none";
    private Text titleText;
    private Text detailText;
    private Image statusVeil;

    public static void BeginAutomatedRun(bool autoExitPlayMode) {
        if (activeRunner != null && activeRunner.report.IsRunning) {
            Debug.LogWarning("[Live2DAcceptance] A run is already active.");
            return;
        }

        GameObject runnerObject = new GameObject("Live2DAcceptanceRunner_Auto");
        DontDestroyOnLoad(runnerObject);
        Live2DAcceptanceRunner runner = runnerObject.AddComponent<Live2DAcceptanceRunner>();
        runner.autoExitPlayMode = autoExitPlayMode;
        activeRunner = runner;
        runner.StartCoroutine(runner.RunAcceptanceFlow());
    }

    private IEnumerator RunAcceptanceFlow() {
        bool prepared = TryPrepareRun();
        if (!prepared) {
            CompleteRun();
            yield break;
        }

        Debug.Log($"[Live2DAcceptance] Run started. Output={outputRoot}");

        BuildStage();
        ResolveVisualAndPresenter();

        for (int i = 0; i < captureSteps.Count; i++) {
            yield return CaptureStep(i + 1, captureSteps[i]);
        }

        FinalizeReport();
        WriteOutputs();
        Debug.Log($"[Live2DAcceptance] Run finished. Status={report.Status}. Report={Path.Combine(outputRoot, "report.json")}");

        CompleteRun();
    }

    private bool TryPrepareRun() {
        try {
            PrepareOutputDirectories();
            InitializeReport();
            return true;
        } catch (Exception ex) {
            Debug.LogError($"[Live2DAcceptance] Failed to prepare run: {ex}");
            return false;
        }
    }

    private void PrepareOutputDirectories() {
        runID = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        outputRoot = Path.Combine(logsRoot, "Live2DAcceptance", "latest");
        screenshotsRoot = Path.Combine(outputRoot, "screenshots");

        if (Directory.Exists(screenshotsRoot)) {
            Directory.Delete(screenshotsRoot, true);
        }

        Directory.CreateDirectory(screenshotsRoot);
    }

    private void InitializeReport() {
        report.SchemaVersion = SchemaVersion;
        report.RunID = runID;
        report.Project = ProjectName;
        report.Mode = "live2d_independent";
        report.Status = "RUNNING";
        report.IsRunning = true;
        report.StartedAt = DateTime.Now.ToString("o");
        report.OutputRoot = OutputRootRelative;
        report.Boundaries = new Live2DAcceptanceBoundarySummary {
            FormalV2ArtAcceptanceTouched = false,
            ScreenLayoutsTouched = false,
            ApprovedAssetsTouched = false,
            PackageManifestTouched = false,
            Notes = "Independent Live2D acceptance runner only; it does not change FormalV2 ArtAcceptance pass/fail conditions."
        };
    }

    private void BuildStage() {
        stageRoot = new GameObject("Live2DAcceptanceStage");
        DontDestroyOnLoad(stageRoot);

        GameObject cameraObject = new GameObject("Live2DAcceptanceCamera");
        cameraObject.transform.SetParent(stageRoot.transform, false);
        captureCamera = cameraObject.AddComponent<Camera>();
        captureCamera.clearFlags = CameraClearFlags.SolidColor;
        captureCamera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        captureCamera.orthographic = true;
        captureCamera.orthographicSize = ReferenceHeight * 0.5f;
        captureCamera.nearClipPlane = -10f;
        captureCamera.farClipPlane = 10f;
        captureCamera.transform.position = new Vector3(0f, 0f, -5f);

        GameObject canvasObject = new GameObject("Live2DAcceptanceCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(stageRoot.transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = captureCamera;
        canvas.planeDistance = 1f;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        AddPanel(canvasObject.transform, "Background", new Color(0.08f, 0.09f, 0.12f, 1f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddPanel(canvasObject.transform, "DollPlate", new Color(0.16f, 0.18f, 0.22f, 0.92f), new Vector2(0.33f, 0.08f), new Vector2(0.67f, 0.94f), Vector2.zero, Vector2.zero);
        AddPanel(canvasObject.transform, "DollGlow", new Color(0.23f, 0.44f, 0.62f, 0.18f), new Vector2(0.38f, 0.16f), new Vector2(0.62f, 0.82f), Vector2.zero, Vector2.zero);

        GameObject bridgeObject = new GameObject("DollLive2D_AcceptanceBridge", typeof(RectTransform));
        bridgeObject.transform.SetParent(canvasObject.transform, false);
        RectTransform bridgeRect = bridgeObject.GetComponent<RectTransform>();
        bridgeRect.anchorMin = new Vector2(0.38f, 0.15f);
        bridgeRect.anchorMax = new Vector2(0.62f, 0.85f);
        bridgeRect.offsetMin = Vector2.zero;
        bridgeRect.offsetMax = Vector2.zero;
        bridge = bridgeObject.AddComponent<DollLive2DUGUIBridge>();
        bridge.EnsureDisplayObjects();

        statusVeil = AddPanel(canvasObject.transform, "StateVeil", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        titleText = AddText(canvasObject.transform, "Title", "Live2D Acceptance", 34, TextAnchor.MiddleLeft, new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.94f));
        detailText = AddText(canvasObject.transform, "Details", string.Empty, 24, TextAnchor.UpperLeft, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.19f));
    }

    private void ResolveVisualAndPresenter() {
        bool runtimePackageAvailable = IsRuntimePackageAvailable();
        visualResult = DollLive2DVisualResolver.ResolveDollProto0(runtimePackageAvailable);
        bridge.Configure(visualResult);

        GameObject dynamicInstance = null;
        if (visualResult != null && visualResult.UseDynamicPrefab) {
            dynamicInstance = Instantiate(visualResult.Prefab, stageRoot.transform);
            dynamicInstance.name = $"{visualResult.DynamicVisualID}_AcceptanceInstance";
            dynamicInstance.transform.localPosition = Vector3.zero;
            Camera modelCamera = CreateModelCamera(dynamicInstance);
            bridge.AttachRenderCamera(modelCamera, 768, 1024, 16);
            presenter = FindPresenter(dynamicInstance);
            presenterMode = presenter != null ? "dynamic_presenter" : "dynamic_prefab_no_presenter";
        }

        if (presenter == null) {
            FallbackDollLive2DPresenter fallbackPresenter = bridge.gameObject.AddComponent<FallbackDollLive2DPresenter>();
            fallbackPresenter.Initialize(visualResult, bridge.FallbackImage);
            presenter = fallbackPresenter;
            if (presenterMode == "none") {
                presenterMode = "fallback_presenter";
            }
        }

        report.Visual = new Live2DAcceptanceVisualSummary {
            DollID = visualResult != null ? visualResult.DollID : DollLive2DVisualResolver.DefaultDollID,
            DynamicVisualID = visualResult != null ? visualResult.DynamicVisualID : DollLive2DVisualResolver.DefaultDynamicVisualID,
            FallbackSpriteID = visualResult != null ? visualResult.FallbackSpriteID : DollLive2DVisualResolver.DefaultFallbackSpriteID,
            VisualMode = visualResult != null ? visualResult.Mode.ToString() : DollLive2DVisualMode.MissingFallback.ToString(),
            FallbackReason = visualResult != null ? visualResult.FallbackReason : "visual_result_missing",
            RuntimePackageAvailable = runtimePackageAvailable,
            DynamicPrefabFound = visualResult != null && visualResult.UseDynamicPrefab,
            FallbackSpriteFound = visualResult != null && visualResult.FallbackSprite != null,
            PresenterMode = presenterMode
        };

        if (!runtimePackageAvailable) {
            AddWarning("Live2D/Spine runtime package is not loaded; dynamic motion cannot be proven.");
        }
        if (visualResult == null || !visualResult.UseDynamicPrefab) {
            AddWarning("Dynamic Live2D prefab is missing; runner will capture static fallback only.");
        }
        if (presenterMode != "dynamic_presenter") {
            AddWarning("No dynamic IDollLive2DPresenter is available; motion/expression calls are acceptance intents only.");
        }
    }

    private IEnumerator CaptureStep(int index, Live2DAcceptanceCaptureStep step) {
        Live2DAcceptanceCaptureRecord record = new Live2DAcceptanceCaptureRecord {
            Index = index,
            StepID = step.StepID,
            MotionID = step.MotionID,
            ExpressionID = step.ExpressionID,
            File = $"screenshots/{step.StepID}.png",
            CapturedAt = DateTime.Now.ToString("o"),
            Resolution = $"{ReferenceWidth}x{ReferenceHeight}",
            DynamicFrameProven = visualResult != null && visualResult.UseDynamicPrefab && presenterMode == "dynamic_presenter" && !step.ForceFallback,
            FallbackVisible = visualResult != null && visualResult.FallbackSprite != null && (!recordDynamicMode() || step.ForceFallback)
        };

        if (step.ForceFallback) {
            bridge.ShowFallback(visualResult != null ? visualResult.FallbackSprite : null);
        } else if (visualResult != null && visualResult.UseDynamicPrefab && bridge.CurrentRenderTexture != null) {
            bridge.ShowDynamic(bridge.CurrentRenderTexture);
        } else {
            bridge.ShowFallback(visualResult != null ? visualResult.FallbackSprite : null);
        }

        DollLive2DPresenterDriver.Apply(presenter, new DollLive2DPresentationIntent {
            UseLowSanIdle = step.UseLowSanIdle,
            TriggerRepairReact = step.TriggerRepairReact,
            TriggerHitReact = step.TriggerHitReact,
            ExpressionID = step.ExpressionID,
            CoreGlowNormalized = step.CoreGlowNormalized,
            SanStressNormalized = step.SanStressNormalized
        });

        UpdateStageLabels(step, record);
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();

        string absolutePath = Path.Combine(outputRoot, record.File.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        RenderScreenshot(absolutePath, record);

        if (File.Exists(absolutePath)) {
            record.Status = record.DynamicFrameProven ? "captured" : "fallback_captured";
            if (!record.DynamicFrameProven && !step.ForceFallback) {
                record.Warnings.Add($"Dynamic frame not proven for {step.StepID}; visual mode={report.Visual.VisualMode}, reason={report.Visual.FallbackReason}.");
            }
        } else {
            record.Status = "failed";
            record.Errors.Add($"Screenshot file was not created: {absolutePath}");
        }

        report.Captures.Add(record);
    }

    private bool recordDynamicMode() {
        return visualResult != null && visualResult.UseDynamicPrefab && bridge != null && bridge.CurrentRenderTexture != null;
    }

    private void UpdateStageLabels(Live2DAcceptanceCaptureStep step, Live2DAcceptanceCaptureRecord record) {
        if (titleText != null) {
            titleText.text = $"Live2D Acceptance / {step.StepID}";
        }

        if (detailText != null) {
            detailText.text =
                $"DollID: {report.Visual.DollID}    DynamicVisualID: {report.Visual.DynamicVisualID}\n" +
                $"Motion: {step.MotionID}    Expression: {step.ExpressionID}    Presenter: {presenterMode}\n" +
                $"VisualMode: {report.Visual.VisualMode}    FallbackReason: {report.Visual.FallbackReason}";
        }

        if (statusVeil != null) {
            float stress = Mathf.Clamp01(step.SanStressNormalized);
            statusVeil.color = new Color(0.22f, 0.06f, 0.12f, stress * 0.32f);
        }
    }

    private void RenderScreenshot(string absolutePath, Live2DAcceptanceCaptureRecord record) {
        if (captureCamera == null) {
            record.Errors.Add("Capture camera is missing.");
            return;
        }

        RenderTexture renderTexture = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = captureCamera.targetTexture;
        float previousAspect = captureCamera.aspect;
        Texture2D screenshot = null;

        try {
            if (bridge != null && bridge.RenderCamera != null && bridge.RenderCamera.targetTexture != null) {
                bridge.RenderCamera.Render();
            }

            renderTexture = new RenderTexture(ReferenceWidth, ReferenceHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            captureCamera.targetTexture = renderTexture;
            captureCamera.aspect = (float)ReferenceWidth / ReferenceHeight;
            captureCamera.Render();

            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(ReferenceWidth, ReferenceHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, ReferenceWidth, ReferenceHeight), 0, 0);
            screenshot.Apply();

            if (IsScreenshotVisuallyBlank(screenshot)) {
                record.Errors.Add("Screenshot appears visually blank or near-solid color.");
            }

            File.WriteAllBytes(absolutePath, screenshot.EncodeToPNG());
        } catch (Exception ex) {
            record.Errors.Add($"Failed to write screenshot: {ex.Message}");
            Debug.LogError($"[Live2DAcceptance] Failed to write screenshot {absolutePath}\n{ex}");
        } finally {
            captureCamera.targetTexture = previousTarget;
            captureCamera.aspect = previousAspect;
            RenderTexture.active = previousActive;

            if (renderTexture != null) {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (screenshot != null) {
                Destroy(screenshot);
            }
        }
    }

    private Camera CreateModelCamera(GameObject dynamicInstance) {
        GameObject cameraObject = new GameObject("Live2DModelCamera");
        cameraObject.transform.SetParent(stageRoot.transform, false);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 50f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        if (dynamicInstance != null) {
            dynamicInstance.transform.position = new Vector3(0f, 0f, 0f);
        }

        return camera;
    }

    private IDollLive2DPresenter FindPresenter(GameObject root) {
        if (root == null) {
            return null;
        }

        MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours) {
            if (behaviour is IDollLive2DPresenter candidate) {
                return candidate;
            }
        }

        return null;
    }

    private bool IsRuntimePackageAvailable() {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (Assembly assembly in assemblies) {
            string assemblyName = assembly.GetName().Name;
            if (assemblyName.IndexOf("Live2D", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assemblyName.IndexOf("Cubism", StringComparison.OrdinalIgnoreCase) >= 0 ||
                assemblyName.IndexOf("Spine", StringComparison.OrdinalIgnoreCase) >= 0) {
                return true;
            }
        }

        return Type.GetType("Live2D.Cubism.Core.CubismModel, Assembly-CSharp") != null ||
               Type.GetType("Spine.Unity.SkeletonAnimation, Assembly-CSharp") != null;
    }

    private void FinalizeReport() {
        report.FinishedAt = DateTime.Now.ToString("o");
        report.IsRunning = false;

        bool hasScreenshot = false;
        foreach (Live2DAcceptanceCaptureRecord capture in report.Captures) {
            if (capture.Status == "captured" || capture.Status == "fallback_captured") {
                hasScreenshot = true;
            }

            foreach (string warning in capture.Warnings) {
                AddWarning(warning);
            }

            foreach (string error in capture.Errors) {
                AddError(error);
            }
        }

        if (!hasScreenshot) {
            AddError("No Live2D acceptance screenshots were captured.");
        }

        if (report.Errors.Count > 0) {
            report.Status = "FAILED";
        } else if (report.Visual.DynamicPrefabFound && report.Visual.PresenterMode == "dynamic_presenter") {
            report.Status = "PASSED";
        } else if (report.Visual.FallbackSpriteFound) {
            report.Status = "FALLBACK_ONLY";
        } else {
            report.Status = "WARNING";
        }
    }

    private void WriteOutputs() {
        Directory.CreateDirectory(outputRoot);
        File.WriteAllText(Path.Combine(outputRoot, "report.json"), JsonUtility.ToJson(report, true));
        File.WriteAllText(Path.Combine(outputRoot, "notes.txt"), BuildNotesText());
    }

    private string BuildNotesText() {
        return "Live2DAcceptance latest output is overwritten on each run.\n" +
               "Trigger: UnityClient/Logs/.live2d_acceptance_trigger = RUN_LIVE2D_ACCEPTANCE\n" +
               $"RunID: {runID}\n" +
               $"Status: {report.Status}\n" +
               "This runner is independent from FormalV2 ArtAcceptance.\n";
    }

    private void CompleteRun() {
        activeRunner = null;

        if (stageRoot != null) {
            Destroy(stageRoot);
        }

        Destroy(gameObject);

#if UNITY_EDITOR
        if (autoExitPlayMode) {
            EditorApplication.isPlaying = false;
            if (Application.isBatchMode) {
                EditorApplication.Exit(0);
            }
        }
#endif
    }

    private void AddWarning(string message) {
        if (!string.IsNullOrEmpty(message) && !report.Warnings.Contains(message)) {
            report.Warnings.Add(message);
        }
    }

    private void AddError(string message) {
        if (!string.IsNullOrEmpty(message) && !report.Errors.Contains(message)) {
            report.Errors.Add(message);
        }
    }

    private bool IsScreenshotVisuallyBlank(Texture2D screenshot) {
        if (screenshot == null) {
            return true;
        }

        int width = screenshot.width;
        int height = screenshot.height;
        int stepX = Mathf.Max(1, width / 32);
        int stepY = Mathf.Max(1, height / 18);
        Color32 first = screenshot.GetPixel(0, 0);
        int sampled = 0;
        int different = 0;

        for (int y = 0; y < height; y += stepY) {
            for (int x = 0; x < width; x += stepX) {
                sampled++;
                Color32 current = screenshot.GetPixel(x, y);
                int delta = Mathf.Abs(current.r - first.r) + Mathf.Abs(current.g - first.g) + Mathf.Abs(current.b - first.b);
                if (delta > 8) {
                    different++;
                }
            }
        }

        return sampled == 0 || different < Mathf.Max(4, sampled / 100);
    }

    private Image AddPanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = panel.AddComponent<Image>();
        image.sprite = GetSolidSprite();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text AddText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax) {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label = textObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.text = text;
        label.fontSize = fontSize;
        label.color = new Color(0.88f, 0.91f, 0.95f, 1f);
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private Sprite GetSolidSprite() {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);
        texture.name = "Live2DAcceptanceSolidTexture";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sprite.name = "Live2DAcceptanceSolidSprite";
        return sprite;
    }

    private sealed class Live2DAcceptanceCaptureStep {
        public Live2DAcceptanceCaptureStep(
            string stepID,
            string motionID,
            string expressionID,
            bool useLowSanIdle,
            bool triggerRepairReact,
            bool triggerHitReact,
            float coreGlowNormalized,
            float sanStressNormalized,
            bool forceFallback = false) {
            StepID = stepID;
            MotionID = motionID;
            ExpressionID = expressionID;
            UseLowSanIdle = useLowSanIdle;
            TriggerRepairReact = triggerRepairReact;
            TriggerHitReact = triggerHitReact;
            CoreGlowNormalized = coreGlowNormalized;
            SanStressNormalized = sanStressNormalized;
            ForceFallback = forceFallback;
        }

        public string StepID { get; }
        public string MotionID { get; }
        public string ExpressionID { get; }
        public bool UseLowSanIdle { get; }
        public bool TriggerRepairReact { get; }
        public bool TriggerHitReact { get; }
        public float CoreGlowNormalized { get; }
        public float SanStressNormalized { get; }
        public bool ForceFallback { get; }
    }
}
