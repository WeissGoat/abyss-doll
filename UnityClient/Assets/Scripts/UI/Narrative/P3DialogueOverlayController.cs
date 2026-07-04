using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class NarrativeVisualRequest {
    public string VisualID;
    public string FallbackVisualID;
    public string Slot;
    public bool UseCover = true;
}

public sealed class NarrativeOverlayPayload {
    public string BlockingMode = "modal";
    public string SpeakerName;
    public string Text;
    public string ContinueLabel = "继续";
    public string ActionID;
    public string ActionLabel;
    public bool UseBlackout;
    public NarrativeVisualRequest VisualRequest;
}

public sealed class P3DialogueOverlayController : MonoBehaviour {
    public event Action ContinueRequested;
    public event Action<string> ActionRequested;

    public CanvasGroup rootGroup;
    public Image blackoutImage;
    public Image visualImage;
    public Image stageVeilImage;
    public Image propCardImage;
    public Image propImage;
    public Image dollImage;
    public Image dialoguePanelImage;
    public Text speakerText;
    public Text bodyText;
    public Button continueButton;
    public Button actionButton;

    private Text _continueButtonText;
    private Text _actionButtonText;
    private string _currentActionID = string.Empty;
    private string _lastVisualID = string.Empty;

    public bool IsShowing {
        get { return rootGroup != null && rootGroup.alpha > 0.001f; }
    }

    public bool IsInputBlocking {
        get { return rootGroup != null && rootGroup.blocksRaycasts; }
    }

    public bool HasActionVisible {
        get { return actionButton != null && actionButton.gameObject.activeSelf; }
    }

    public string CurrentSpeaker {
        get { return speakerText != null ? speakerText.text : string.Empty; }
    }

    public string CurrentText {
        get { return bodyText != null ? bodyText.text : string.Empty; }
    }

    public string LastVisualID {
        get { return _lastVisualID; }
    }

    public string StageDebugSummary {
        get {
            return $"visualActive={IsActive(visualImage)}, visualSprite={StageBackgroundSpriteName}, "
                + $"veilActive={IsActive(stageVeilImage)}, propCardActive={IsActive(propCardImage)}, "
                + $"propSprite={StagePropSpriteName}, dollActive={IsStageDollVisible}, dollSprite={StageDollSpriteName}";
        }
    }

    public string StageBackgroundSpriteName {
        get { return SpriteName(visualImage); }
    }

    public string StagePropSpriteName {
        get { return SpriteName(propImage); }
    }

    public string StageDollSpriteName {
        get { return SpriteName(dollImage); }
    }

    public bool IsStagePropVisible {
        get { return IsActive(propCardImage) && IsActive(propImage); }
    }

    public bool IsStageDollVisible {
        get { return IsActive(dollImage); }
    }

    public static P3DialogueOverlayController CreateUnder(Canvas canvas) {
        if (canvas == null) {
            return null;
        }

        GameObject overlayObj = new GameObject("P3DialogueOverlay_Runtime");
        overlayObj.transform.SetParent(canvas.transform, false);
        P3DialogueOverlayController controller = overlayObj.AddComponent<P3DialogueOverlayController>();
        controller.EnsureBuilt();
        return controller;
    }

    public void EnsureBuilt() {
        RectTransform rootRect = EnsureRectTransform(gameObject);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        rootGroup = EnsureComponent<CanvasGroup>(gameObject);
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        blackoutImage = EnsureImage("Blackout_Image", transform);
        Stretch(blackoutImage.rectTransform);
        VisualUIHelper.ApplySolidColor(blackoutImage, new Color(0f, 0f, 0f, 0.92f), true);

        visualImage = EnsureImage("VisualStage_Image", transform);
        Stretch(visualImage.rectTransform);
        VisualUIHelper.ApplySolidColor(visualImage, new Color(0f, 0f, 0f, 0f), false);
        visualImage.raycastTarget = false;

        stageVeilImage = EnsureImage("StageVeil_Image", transform);
        Stretch(stageVeilImage.rectTransform);
        VisualUIHelper.ApplySolidColor(stageVeilImage, new Color(0.02f, 0.025f, 0.035f, 0.36f), false);
        stageVeilImage.raycastTarget = false;

        propCardImage = EnsureImage("StagePropCard_Image", transform);
        RectTransform propCardRect = propCardImage.rectTransform;
        propCardRect.anchorMin = new Vector2(0.16f, 0.25f);
        propCardRect.anchorMax = new Vector2(0.16f, 0.25f);
        propCardRect.pivot = new Vector2(0.5f, 0.5f);
        propCardRect.anchoredPosition = Vector2.zero;
        propCardRect.sizeDelta = new Vector2(430f, 360f);
        VisualUIHelper.ApplySlicedSprite(propCardImage, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.9f), false);
        propCardImage.raycastTarget = false;

        propImage = EnsureImage("StageProp_Image", propCardImage.transform);
        RectTransform propRect = propImage.rectTransform;
        propRect.anchorMin = new Vector2(0.5f, 0.5f);
        propRect.anchorMax = new Vector2(0.5f, 0.5f);
        propRect.pivot = new Vector2(0.5f, 0.5f);
        propRect.anchoredPosition = Vector2.zero;
        propRect.sizeDelta = new Vector2(260f, 260f);
        propImage.raycastTarget = false;

        dollImage = EnsureImage("StageDoll_Image", transform);
        RectTransform dollRect = dollImage.rectTransform;
        dollRect.anchorMin = new Vector2(0.72f, 0.2f);
        dollRect.anchorMax = new Vector2(0.72f, 0.2f);
        dollRect.pivot = new Vector2(0.5f, 0f);
        dollRect.anchoredPosition = Vector2.zero;
        dollRect.sizeDelta = new Vector2(430f, 700f);
        dollImage.raycastTarget = false;

        dialoguePanelImage = EnsureImage("DialoguePanel_Image", transform);
        RectTransform panelRect = dialoguePanelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 46f);
        panelRect.sizeDelta = new Vector2(1320f, 230f);
        VisualUIHelper.ApplySlicedSprite(dialoguePanelImage, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.05f, 0.06f, 0.08f, 0.94f), false);

        speakerText = EnsureText("Speaker_Text", dialoguePanelImage.transform, font, 28, new Color(0.86f, 0.94f, 1f, 1f), TextAnchor.MiddleLeft);
        RectTransform speakerRect = speakerText.rectTransform;
        speakerRect.anchorMin = new Vector2(0f, 1f);
        speakerRect.anchorMax = new Vector2(1f, 1f);
        speakerRect.pivot = new Vector2(0.5f, 1f);
        speakerRect.offsetMin = new Vector2(56f, -74f);
        speakerRect.offsetMax = new Vector2(-56f, -22f);

        bodyText = EnsureText("Body_Text", dialoguePanelImage.transform, font, 30, Color.white, TextAnchor.UpperLeft);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(56f, 74f);
        bodyRect.offsetMax = new Vector2(-56f, -70f);

        continueButton = EnsureButton("Continue_Button", dialoguePanelImage.transform, font, out _continueButtonText);
        RectTransform continueRect = continueButton.GetComponent<RectTransform>();
        continueRect.anchorMin = new Vector2(1f, 0f);
        continueRect.anchorMax = new Vector2(1f, 0f);
        continueRect.pivot = new Vector2(1f, 0f);
        continueRect.anchoredPosition = new Vector2(-56f, 26f);
        continueRect.sizeDelta = new Vector2(190f, 52f);

        actionButton = EnsureButton("Action_Button", dialoguePanelImage.transform, font, out _actionButtonText);
        RectTransform actionRect = actionButton.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.5f, 0f);
        actionRect.anchorMax = new Vector2(0.5f, 0f);
        actionRect.pivot = new Vector2(0.5f, 0f);
        actionRect.anchoredPosition = new Vector2(0f, 26f);
        actionRect.sizeDelta = new Vector2(340f, 60f);

        continueButton.onClick.RemoveListener(OnContinueClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        actionButton.onClick.RemoveListener(OnActionClicked);
        actionButton.onClick.AddListener(OnActionClicked);
        Hide();
    }

    public void PresentBlackoutSubtitle(string speakerName, string text, string blockingMode = "modal") {
        PresentLine(new NarrativeOverlayPayload {
            BlockingMode = blockingMode,
            SpeakerName = speakerName,
            Text = text,
            UseBlackout = true,
            ContinueLabel = string.Empty
        });
    }

    public void PresentLine(NarrativeOverlayPayload payload) {
        EnsureBuilt();
        payload = payload ?? new NarrativeOverlayPayload();
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = IsBlockingMode(payload.BlockingMode);

        blackoutImage.gameObject.SetActive(payload.UseBlackout);
        SetStageObjectsActive(payload.VisualRequest != null);
        dialoguePanelImage.gameObject.SetActive(true);
        speakerText.text = payload.SpeakerName ?? string.Empty;
        bodyText.text = payload.Text ?? string.Empty;

        ApplyVisualRequest(payload.VisualRequest);
        ConfigureContinue(payload.ContinueLabel);
        ConfigureAction(payload.ActionID, payload.ActionLabel);
    }

    public void ApplyVisualRequest(NarrativeVisualRequest request) {
        _lastVisualID = request?.VisualID ?? string.Empty;
        if (visualImage == null || request == null) {
            SetStageObjectsActive(false);
            return;
        }

        PrologueStageComposition composition = PrologueStageComposition.Resolve(request);
        if (composition != null) {
            ApplyStageComposition(composition);
            return;
        }

        string visualID = ResolveDisplayVisualID(request.VisualID, request.FallbackVisualID);
        if (request.UseCover) {
            VisualUIHelper.ApplyCoverSprite(visualImage, visualID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.82f));
        } else {
            VisualUIHelper.ApplySimpleSprite(visualImage, visualID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.82f), false);
        }

        visualImage.raycastTarget = false;
        stageVeilImage.gameObject.SetActive(false);
        propCardImage.gameObject.SetActive(false);
        dollImage.gameObject.SetActive(false);
    }

    public void Hide() {
        if (rootGroup == null) {
            return;
        }

        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;
        _currentActionID = string.Empty;
        SetStageObjectsActive(false);
    }

    private void ApplyStageComposition(PrologueStageComposition composition) {
        if (composition.BlackoutOnly) {
            VisualUIHelper.ApplySolidColor(visualImage, Color.black, false);
            stageVeilImage.gameObject.SetActive(false);
            propCardImage.gameObject.SetActive(false);
            dollImage.gameObject.SetActive(false);
            return;
        }

        visualImage.gameObject.SetActive(true);
        VisualUIHelper.ApplyCoverSprite(visualImage, composition.BackgroundVisualID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.82f));
        stageVeilImage.gameObject.SetActive(true);
        VisualUIHelper.ApplySolidColor(stageVeilImage, composition.VeilColor, false);

        bool hasProp = !string.IsNullOrEmpty(composition.PropVisualID);
        propCardImage.gameObject.SetActive(hasProp);
        if (hasProp) {
            RectTransform propCardRect = propCardImage.rectTransform;
            propCardRect.anchorMin = composition.PropAnchor;
            propCardRect.anchorMax = composition.PropAnchor;
            propCardRect.anchoredPosition = composition.PropOffset;
            propCardRect.sizeDelta = composition.PropCardSize;
            VisualUIHelper.ApplySlicedSprite(propCardImage, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.9f), false);
            VisualUIHelper.ApplyContainSprite(propImage, composition.PropVisualID, composition.PropImageSize, Color.white, new Color(0.42f, 0.32f, 0.24f, 0.92f), false);
        }

        bool hasDoll = !string.IsNullOrEmpty(composition.DollVisualID);
        dollImage.gameObject.SetActive(hasDoll);
        if (hasDoll) {
            RectTransform dollRect = dollImage.rectTransform;
            dollRect.anchorMin = composition.DollAnchor;
            dollRect.anchorMax = composition.DollAnchor;
            dollRect.anchoredPosition = composition.DollOffset;
            dollRect.sizeDelta = composition.DollSize;
            VisualUIHelper.ApplyContainSprite(dollImage, composition.DollVisualID, composition.DollSize, Color.white, new Color(0.42f, 0.32f, 0.24f, 0.92f), false);
        }
    }

    private void SetStageObjectsActive(bool active) {
        if (visualImage != null) {
            visualImage.gameObject.SetActive(active);
        }

        if (stageVeilImage != null) {
            stageVeilImage.gameObject.SetActive(active);
        }

        if (propCardImage != null) {
            propCardImage.gameObject.SetActive(false);
        }

        if (dollImage != null) {
            dollImage.gameObject.SetActive(false);
        }
    }

    private static string ResolveDisplayVisualID(string visualID, string fallbackVisualID) {
        if (!string.IsNullOrEmpty(visualID) && VisualAssetService.TryGetSprite(visualID, out _)) {
            return visualID;
        }

        if (!string.IsNullOrEmpty(fallbackVisualID) && VisualAssetService.TryGetSprite(fallbackVisualID, out _)) {
            return fallbackVisualID;
        }

        return !string.IsNullOrEmpty(visualID) ? visualID : fallbackVisualID;
    }

    private void ConfigureContinue(string label) {
        bool visible = !string.IsNullOrEmpty(label);
        continueButton.gameObject.SetActive(visible);
        if (_continueButtonText != null) {
            _continueButtonText.text = visible ? label : string.Empty;
        }
    }

    private void ConfigureAction(string actionID, string label) {
        _currentActionID = actionID ?? string.Empty;
        bool visible = !string.IsNullOrEmpty(_currentActionID) && !string.IsNullOrEmpty(label);
        actionButton.gameObject.SetActive(visible);
        if (_actionButtonText != null) {
            _actionButtonText.text = visible ? label : string.Empty;
        }
    }

    private void OnContinueClicked() {
        ContinueRequested?.Invoke();
    }

    private void OnActionClicked() {
        if (!string.IsNullOrEmpty(_currentActionID)) {
            ActionRequested?.Invoke(_currentActionID);
        }
    }

    private static bool IsBlockingMode(string blockingMode) {
        return string.Equals(blockingMode, "modal", StringComparison.Ordinal)
            || string.Equals(blockingMode, "modal_light", StringComparison.Ordinal);
    }

    private static Button EnsureButton(string objectName, Transform parent, Font font, out Text label) {
        Transform existing = parent.Find(objectName);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        EnsureRectTransform(obj);
        Image image = EnsureComponent<Image>(obj);
        Button button = EnsureComponent<Button>(obj);
        button.targetGraphic = image;
        VisualUIHelper.ApplyButtonSkin(button, VisualAssetService.UIButtonPrimaryID, new Color(0.72f, 0.54f, 0.36f, 0.96f));

        Transform textTransform = obj.transform.Find("Text");
        GameObject textObj = textTransform != null ? textTransform.gameObject : new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        label = EnsureComponent<Text>(textObj);
        label.font = font;
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        Stretch(label.rectTransform);
        return button;
    }

    private static Image EnsureImage(string objectName, Transform parent) {
        Transform existing = parent.Find(objectName);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        EnsureRectTransform(obj);
        return EnsureComponent<Image>(obj);
    }

    private static Text EnsureText(string objectName, Transform parent, Font font, int fontSize, Color color, TextAnchor alignment) {
        Transform existing = parent.Find(objectName);
        GameObject obj = existing != null ? existing.gameObject : new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Text text = EnsureComponent<Text>(obj);
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform EnsureRectTransform(GameObject obj) {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect == null) {
            rect = obj.AddComponent<RectTransform>();
        }

        return rect;
    }

    private static T EnsureComponent<T>(GameObject obj) where T : Component {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool IsActive(Image image) {
        return image != null && image.gameObject.activeInHierarchy;
    }

    private static string SpriteName(Image image) {
        return image != null && image.sprite != null ? image.sprite.name : string.Empty;
    }

    private sealed class PrologueStageComposition {
        public string BackgroundVisualID;
        public string PropVisualID;
        public string DollVisualID;
        public bool BlackoutOnly;
        public Color VeilColor = new Color(0.02f, 0.025f, 0.035f, 0.36f);
        public Vector2 PropAnchor = new Vector2(0.23f, 0.52f);
        public Vector2 PropOffset = Vector2.zero;
        public Vector2 PropCardSize = new Vector2(430f, 360f);
        public Vector2 PropImageSize = new Vector2(260f, 260f);
        public Vector2 DollAnchor = new Vector2(0.7f, 0.17f);
        public Vector2 DollOffset = Vector2.zero;
        public Vector2 DollSize = new Vector2(430f, 700f);

        public static PrologueStageComposition Resolve(NarrativeVisualRequest request) {
            string visualID = request?.VisualID ?? string.Empty;
            if (string.IsNullOrEmpty(visualID)) {
                return null;
            }

            switch (visualID) {
                case "cg_t0_01a_black_wake":
                case "cg_t0_01a_depart_black":
                    return new PrologueStageComposition {
                        BlackoutOnly = true
                    };
                case "cg_t0_01a_debt_notice":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "bg_workshop_home_room",
                        PropVisualID = "memento_debt_shadow_window",
                        VeilColor = new Color(0.02f, 0.02f, 0.03f, 0.46f),
                        PropAnchor = new Vector2(0.24f, 0.56f),
                        PropCardSize = new Vector2(430f, 360f),
                        PropImageSize = new Vector2(270f, 270f)
                    };
                case "cg_t0_01a_repair_note":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "bg_workshop_studio",
                        PropVisualID = "ui_icon_wear_repair",
                        VeilColor = new Color(0.02f, 0.03f, 0.04f, 0.4f),
                        PropAnchor = new Vector2(0.24f, 0.56f),
                        PropCardSize = new Vector2(410f, 340f),
                        PropImageSize = new Vector2(220f, 220f)
                    };
                case "cg_t0_01a_core_shard":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "bg_workshop_studio",
                        PropVisualID = "item_mat_core_tier1_icon",
                        VeilColor = new Color(0.02f, 0.025f, 0.04f, 0.38f),
                        PropAnchor = new Vector2(0.5f, 0.56f),
                        PropCardSize = new Vector2(400f, 360f),
                        PropImageSize = new Vector2(230f, 230f)
                    };
                case "cg_t0_01a_find_no0":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "bg_workshop_home_room",
                        DollVisualID = "doll_proto_0_stand",
                        VeilColor = new Color(0.02f, 0.02f, 0.035f, 0.42f),
                        DollAnchor = new Vector2(0.68f, 0.15f),
                        DollSize = new Vector2(460f, 760f)
                    };
                case "vfx_no0_core_start":
                case "stand_no0_weak_sitting":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "bg_workshop_studio",
                        PropVisualID = visualID == "vfx_no0_core_start" ? "item_mat_core_tier1_icon" : string.Empty,
                        DollVisualID = "doll_proto_0_stand",
                        VeilColor = new Color(0.02f, 0.025f, 0.04f, 0.34f),
                        PropAnchor = new Vector2(0.48f, 0.55f),
                        PropCardSize = new Vector2(300f, 280f),
                        PropImageSize = new Vector2(180f, 180f),
                        DollAnchor = new Vector2(0.72f, 0.14f),
                        DollSize = new Vector2(430f, 720f)
                    };
                case "ui_status_card_prologue":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "bg_workshop_home_room",
                        DollVisualID = "doll_proto_0_stand",
                        VeilColor = new Color(0.02f, 0.025f, 0.04f, 0.44f),
                        DollAnchor = new Vector2(0.76f, 0.13f),
                        DollSize = new Vector2(380f, 640f)
                    };
                default:
                    return null;
            }
        }
    }
}
