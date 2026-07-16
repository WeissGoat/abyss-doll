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
    public Image comicBackdropImage;
    public Image[] comicPanelFrameImages;
    public Image[] comicPanelImages;
    public Text comicPageLabelText;
    public Image ritualActionPanelImage;
    public Text ritualActionPromptText;
    public Image dialoguePanelImage;
    public Text speakerText;
    public Text bodyText;
    public Button continueButton;
    public Button actionButton;
    public Button autoButton;
    public Button logButton;
    public Image logPanelImage;
    public Text logBodyText;

    private Text _continueButtonText;
    private Text _actionButtonText;
    private Text _autoButtonText;
    private Text _logButtonText;
    private Button _dialoguePanelButton;
    private string _currentActionID = string.Empty;
    private string _lastVisualID = string.Empty;
    private readonly System.Collections.Generic.List<string> _lineLog = new System.Collections.Generic.List<string>();
    private bool _isAutoEnabled;
    private bool _hasBuiltOnce;
    private bool _comicPageVisible;
    private bool _comicPageDollFocusVisible;
    private bool _comicPageLargeFocusVisible;

    public bool IsShowing {
        get { return rootGroup != null && rootGroup.alpha > 0.001f; }
    }

    public bool IsInputBlocking {
        get { return rootGroup != null && rootGroup.blocksRaycasts; }
    }

    public bool HasActionVisible {
        get { return actionButton != null && actionButton.gameObject.activeSelf; }
    }

    public bool HasStandaloneActionVisible {
        get {
            return HasActionVisible
                && dialoguePanelImage != null
                && actionButton.transform.parent != dialoguePanelImage.transform;
        }
    }

    public bool HasContinueVisible {
        get { return continueButton != null && continueButton.gameObject.activeSelf; }
    }

    public bool IsAutoEnabled {
        get { return _isAutoEnabled; }
    }

    public bool HasAutoLogControlsVisible {
        get {
            return autoButton != null
                && logButton != null
                && autoButton.gameObject.activeSelf
                && logButton.gameObject.activeSelf;
        }
    }

    public string CurrentActionID {
        get { return _currentActionID; }
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

    public bool HasLegacyStagePropVisible {
        get { return HasActiveNamedChild(transform, "StageProp_Image", propImage != null ? propImage.transform : null); }
    }

    public bool HasAnyStagePropVisible {
        get { return IsStagePropVisible || IsActive(propImage) || HasLegacyStagePropVisible; }
    }

    public bool IsLargeStageFocusVisible {
        get {
            return _comicPageLargeFocusVisible
                || IsActive(propCardImage)
                && propCardImage.rectTransform.sizeDelta.x >= 760f
                && propCardImage.rectTransform.sizeDelta.y >= 420f;
        }
    }

    public bool HasLargeVisualFocus {
        get { return IsLargeStageFocusVisible; }
    }

    public bool IsStageDollVisible {
        get { return _comicPageDollFocusVisible || IsActive(dollImage); }
    }

    public bool HasComicPageVisible {
        get { return _comicPageVisible && comicBackdropImage != null && comicBackdropImage.gameObject.activeInHierarchy; }
    }

    public int VisibleComicPanelCount {
        get {
            if (!HasComicPageVisible || comicPanelFrameImages == null) {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < comicPanelFrameImages.Length; i++) {
                if (comicPanelFrameImages[i] != null && comicPanelFrameImages[i].gameObject.activeInHierarchy) {
                    count++;
                }
            }

            return count;
        }
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
        Canvas overlayCanvas = EnsureComponent<Canvas>(gameObject);
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 5000;
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        EnsureComponent<GraphicRaycaster>(gameObject);

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
        VisualUIHelper.ApplySolidColor(blackoutImage, new Color(0f, 0f, 0f, 1f), true);

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

        propImage = EnsureImage("StageProp_Image", transform);
        RectTransform propRect = propImage.rectTransform;
        propRect.anchorMin = new Vector2(0.5f, 0.5f);
        propRect.anchorMax = new Vector2(0.5f, 0.5f);
        propRect.pivot = new Vector2(0.5f, 0.5f);
        propRect.anchoredPosition = Vector2.zero;
        propRect.sizeDelta = new Vector2(260f, 260f);
        propImage.raycastTarget = false;
        HideLegacyStagePropImages();

        dollImage = EnsureImage("StageDoll_Image", transform);
        RectTransform dollRect = dollImage.rectTransform;
        dollRect.anchorMin = new Vector2(0.72f, 0.2f);
        dollRect.anchorMax = new Vector2(0.72f, 0.2f);
        dollRect.pivot = new Vector2(0.5f, 0f);
        dollRect.anchoredPosition = Vector2.zero;
        dollRect.sizeDelta = new Vector2(430f, 700f);
        dollImage.raycastTarget = false;

        BuildComicPageLayer(font);

        dialoguePanelImage = EnsureImage("DialoguePanel_Image", transform);
        RectTransform panelRect = dialoguePanelImage.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 46f);
        panelRect.sizeDelta = new Vector2(1320f, 230f);
        VisualUIHelper.ApplySlicedSprite(dialoguePanelImage, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.05f, 0.06f, 0.08f, 0.94f), false);
        _dialoguePanelButton = EnsureComponent<Button>(dialoguePanelImage.gameObject);
        _dialoguePanelButton.targetGraphic = dialoguePanelImage;
        _dialoguePanelButton.transition = Selectable.Transition.None;

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

        ritualActionPanelImage = EnsureImage("StandaloneActionPanel_Image", transform);
        RectTransform ritualPanelRect = ritualActionPanelImage.rectTransform;
        ritualPanelRect.anchorMin = new Vector2(0.5f, 0.24f);
        ritualPanelRect.anchorMax = new Vector2(0.5f, 0.24f);
        ritualPanelRect.pivot = new Vector2(0.5f, 0.5f);
        ritualPanelRect.anchoredPosition = Vector2.zero;
        ritualPanelRect.sizeDelta = new Vector2(520f, 132f);
        VisualUIHelper.ApplySolidColor(ritualActionPanelImage, new Color(0.018f, 0.07f, 0.086f, 0.88f), false);
        ritualActionPanelImage.gameObject.SetActive(false);
        ritualActionPanelImage.raycastTarget = false;

        ritualActionPromptText = EnsureText("StandaloneActionPrompt_Text", ritualActionPanelImage.transform, font, 22, new Color(0.82f, 0.96f, 1f, 1f), TextAnchor.MiddleCenter);
        Stretch(ritualActionPromptText.rectTransform);
        ritualActionPromptText.text = "\u8f7b\u89e6\u6838\u5fc3\u4ed3\uff0c\u53ea\u505a\u8fd9\u4e00\u6b21\u786e\u8ba4\u3002";

        actionButton = EnsureButton("Action_Button", transform, font, out _actionButtonText);
        RectTransform actionRect = actionButton.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.5f, 0.2f);
        actionRect.anchorMax = new Vector2(0.5f, 0.2f);
        actionRect.pivot = new Vector2(0.5f, 0.5f);
        actionRect.anchoredPosition = Vector2.zero;
        actionRect.sizeDelta = new Vector2(360f, 64f);
        Transform legacyAction = dialoguePanelImage.transform.Find("Action_Button");
        if (legacyAction != null) {
            legacyAction.gameObject.SetActive(false);
        }

        autoButton = EnsureButton("Auto_Button", dialoguePanelImage.transform, font, out _autoButtonText);
        RectTransform autoRect = autoButton.GetComponent<RectTransform>();
        autoRect.anchorMin = new Vector2(1f, 1f);
        autoRect.anchorMax = new Vector2(1f, 1f);
        autoRect.pivot = new Vector2(1f, 1f);
        autoRect.anchoredPosition = new Vector2(-162f, -22f);
        autoRect.sizeDelta = new Vector2(92f, 38f);

        logButton = EnsureButton("Log_Button", dialoguePanelImage.transform, font, out _logButtonText);
        RectTransform logRect = logButton.GetComponent<RectTransform>();
        logRect.anchorMin = new Vector2(1f, 1f);
        logRect.anchorMax = new Vector2(1f, 1f);
        logRect.pivot = new Vector2(1f, 1f);
        logRect.anchoredPosition = new Vector2(-58f, -22f);
        logRect.sizeDelta = new Vector2(86f, 38f);

        logPanelImage = EnsureImage("LogPanel_Image", transform);
        RectTransform logPanelRect = logPanelImage.rectTransform;
        logPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        logPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        logPanelRect.pivot = new Vector2(0.5f, 0.5f);
        logPanelRect.anchoredPosition = new Vector2(0f, 64f);
        logPanelRect.sizeDelta = new Vector2(760f, 440f);
        VisualUIHelper.ApplySlicedSprite(logPanelImage, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.025f, 0.032f, 0.044f, 0.96f), true);

        logBodyText = EnsureText("LogBody_Text", logPanelImage.transform, font, 22, new Color(0.9f, 0.96f, 1f, 1f), TextAnchor.UpperLeft);
        RectTransform logBodyRect = logBodyText.rectTransform;
        logBodyRect.anchorMin = Vector2.zero;
        logBodyRect.anchorMax = Vector2.one;
        logBodyRect.offsetMin = new Vector2(34f, 28f);
        logBodyRect.offsetMax = new Vector2(-34f, -28f);
        logBodyText.verticalOverflow = VerticalWrapMode.Truncate;

        _dialoguePanelButton.onClick.RemoveListener(OnContinueClicked);
        _dialoguePanelButton.onClick.AddListener(OnContinueClicked);
        continueButton.onClick.RemoveListener(OnContinueClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        actionButton.onClick.RemoveListener(OnActionClicked);
        actionButton.onClick.AddListener(OnActionClicked);
        autoButton.onClick.RemoveListener(OnAutoClicked);
        autoButton.onClick.AddListener(OnAutoClicked);
        logButton.onClick.RemoveListener(OnLogClicked);
        logButton.onClick.AddListener(OnLogClicked);
        if (!_hasBuiltOnce) {
            _hasBuiltOnce = true;
            Hide();
        }
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
        Canvas overlayCanvas = GetComponent<Canvas>();
        if (overlayCanvas != null) {
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 5000;
        }

        transform.SetAsLastSibling();
        payload = payload ?? new NarrativeOverlayPayload();
        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = IsBlockingMode(payload.BlockingMode);

        string payloadVisualID = payload.VisualRequest?.VisualID ?? string.Empty;
        blackoutImage.gameObject.SetActive(payload.UseBlackout && payloadVisualID != "cg_t0_01a_black_wake");
        SetStageObjectsActive(payload.VisualRequest != null);
        dialoguePanelImage.gameObject.SetActive(true);
        speakerText.text = payload.SpeakerName ?? string.Empty;
        bodyText.text = payload.Text ?? string.Empty;
        AppendLineLog(payload.SpeakerName, payload.Text);

        ApplyVisualRequest(payload.VisualRequest, payload.ActionID);
        ApplyDialogueLayout(payload);
        ConfigureContinue(payload.ContinueLabel);
        ConfigureAction(payload.ActionID, payload.ActionLabel);
        ConfigureUtilityControls(ShouldShowUtilityControls(payload));
    }

    public void ApplyVisualRequest(NarrativeVisualRequest request) {
        ApplyVisualRequest(request, string.Empty);
    }

    private void ApplyVisualRequest(NarrativeVisualRequest request, string actionID) {
        _lastVisualID = request?.VisualID ?? string.Empty;
        if (visualImage == null || request == null) {
            SetStageObjectsActive(false);
            return;
        }

        ComicPageDefinition comicPage = ComicPageDefinition.Resolve(request, actionID);
        if (comicPage != null) {
            ApplyComicPage(comicPage);
            return;
        }

        PrologueStageComposition composition = PrologueStageComposition.Resolve(request);
        if (composition != null) {
            ApplyStageComposition(composition);
            return;
        }

        ClearStagePropLayer();

        string visualID = ResolveDisplayVisualID(request.VisualID, request.FallbackVisualID);
        if (request.UseCover) {
            VisualUIHelper.ApplyCoverSprite(visualImage, visualID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.82f));
        } else {
            VisualUIHelper.ApplySimpleSprite(visualImage, visualID, Color.white, new Color(0.08f, 0.08f, 0.1f, 0.82f), false);
        }

        visualImage.raycastTarget = false;
        stageVeilImage.gameObject.SetActive(false);
        ClearStagePropLayer();
        dollImage.gameObject.SetActive(false);
        SetComicPageActive(false);
    }

    public void Hide() {
        if (rootGroup == null) {
            return;
        }

        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;
        _currentActionID = string.Empty;
        if (logPanelImage != null) {
            logPanelImage.gameObject.SetActive(false);
        }
        if (ritualActionPanelImage != null) {
            ritualActionPanelImage.gameObject.SetActive(false);
        }
        SetStageObjectsActive(false);
    }

    private void ApplyStageComposition(PrologueStageComposition composition) {
        SetComicPageActive(false);
        if (composition.BlackoutOnly) {
            VisualUIHelper.ApplySolidColor(visualImage, Color.black, false);
            stageVeilImage.gameObject.SetActive(false);
            ClearStagePropLayer();
            dollImage.gameObject.SetActive(false);
            return;
        }

        visualImage.gameObject.SetActive(true);
        VisualUIHelper.ApplyCoverSprite(visualImage, composition.BackgroundVisualID, Color.white, new Color(0.012f, 0.032f, 0.042f, 0.9f));
        stageVeilImage.gameObject.SetActive(true);
        VisualUIHelper.ApplySolidColor(stageVeilImage, composition.VeilColor, false);

        bool hasProp = !string.IsNullOrEmpty(composition.PropVisualID);
        propCardImage.gameObject.SetActive(hasProp);
        propImage.gameObject.SetActive(hasProp);
        if (!hasProp) {
            ClearStagePropLayer();
        }
        if (hasProp) {
            RectTransform propCardRect = propCardImage.rectTransform;
            propCardRect.anchorMin = composition.PropAnchor;
            propCardRect.anchorMax = composition.PropAnchor;
            propCardRect.pivot = new Vector2(0.5f, 0.5f);
            propCardRect.anchoredPosition = composition.PropOffset;
            propCardRect.sizeDelta = composition.PropCardSize;
            VisualUIHelper.ApplySolidColor(propCardImage, composition.FocusPanelColor, false);
            propCardImage.transform.SetSiblingIndex(Mathf.Max(visualImage.transform.GetSiblingIndex() + 2, propCardImage.transform.GetSiblingIndex()));
            propImage.transform.SetSiblingIndex(propCardImage.transform.GetSiblingIndex() + 1);
            RectTransform propRect = propImage.rectTransform;
            propRect.anchorMin = composition.PropAnchor;
            propRect.anchorMax = composition.PropAnchor;
            propRect.pivot = new Vector2(0.5f, 0.5f);
            propRect.anchoredPosition = composition.PropOffset + composition.PropImageOffset;
            VisualUIHelper.ApplyContainSprite(propImage, composition.PropVisualID, composition.PropImageSize, Color.white, new Color(0.14f, 0.38f, 0.44f, 0.9f), false);
        }

        bool hasDoll = !string.IsNullOrEmpty(composition.DollVisualID);
        dollImage.gameObject.SetActive(hasDoll);
        if (hasDoll) {
            RectTransform dollRect = dollImage.rectTransform;
            dollRect.anchorMin = composition.DollAnchor;
            dollRect.anchorMax = composition.DollAnchor;
            dollRect.anchoredPosition = composition.DollOffset;
            dollRect.sizeDelta = composition.DollSize;
            VisualUIHelper.ApplyContainSprite(dollImage, composition.DollVisualID, composition.DollSize, Color.white, new Color(0.14f, 0.38f, 0.44f, 0.9f), false);
        }
    }

    private void BuildComicPageLayer(Font font) {
        comicBackdropImage = EnsureImage("ComicPageBackdrop_Image", transform);
        Stretch(comicBackdropImage.rectTransform);
        VisualUIHelper.ApplySolidColor(comicBackdropImage, new Color(0.004f, 0.01f, 0.014f, 1f), false);
        comicBackdropImage.raycastTarget = false;
        comicBackdropImage.gameObject.SetActive(false);

        if (comicPanelFrameImages == null || comicPanelFrameImages.Length != 4) {
            comicPanelFrameImages = new Image[4];
        }

        if (comicPanelImages == null || comicPanelImages.Length != 4) {
            comicPanelImages = new Image[4];
        }

        for (int i = 0; i < 4; i++) {
            Image frame = EnsureImage($"ComicPanel{i + 1}_Frame", transform);
            VisualUIHelper.ApplySolidColor(frame, new Color(0.002f, 0.004f, 0.006f, 1f), false);
            frame.raycastTarget = false;
            frame.gameObject.SetActive(false);
            comicPanelFrameImages[i] = frame;

            Image panel = EnsureImage($"ComicPanel{i + 1}_Image", frame.transform);
            Stretch(panel.rectTransform);
            panel.raycastTarget = false;
            panel.gameObject.SetActive(false);
            comicPanelImages[i] = panel;
        }

        comicPageLabelText = EnsureText("ComicPageLabel_Text", transform, font, 18, new Color(0.72f, 0.86f, 0.9f, 0.72f), TextAnchor.MiddleLeft);
        RectTransform labelRect = comicPageLabelText.rectTransform;
        labelRect.anchorMin = new Vector2(0.08f, 0.92f);
        labelRect.anchorMax = new Vector2(0.08f, 0.92f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.sizeDelta = new Vector2(520f, 32f);
        comicPageLabelText.gameObject.SetActive(false);
    }

    private void ApplyComicPage(ComicPageDefinition page) {
        SetComicPageActive(true);
        if (visualImage != null) {
            visualImage.gameObject.SetActive(false);
        }
        if (stageVeilImage != null) {
            stageVeilImage.gameObject.SetActive(false);
        }
        ClearStagePropLayer();
        if (dollImage != null) {
            dollImage.gameObject.SetActive(false);
        }

        VisualUIHelper.ApplySolidColor(comicBackdropImage, page.BackdropColor, false);
        comicBackdropImage.transform.SetAsFirstSibling();
        _comicPageDollFocusVisible = page.HasDollFocus;
        _comicPageLargeFocusVisible = page.HasLargeFocus;

        if (comicPageLabelText != null) {
            comicPageLabelText.text = page.Label ?? string.Empty;
            comicPageLabelText.gameObject.SetActive(false);
        }

        for (int i = 0; i < comicPanelFrameImages.Length; i++) {
            ComicPanelDefinition panel = page.Panels != null && i < page.Panels.Length ? page.Panels[i] : null;
            Image frame = comicPanelFrameImages[i];
            Image image = comicPanelImages[i];
            bool visible = panel != null;
            if (frame != null) {
                frame.gameObject.SetActive(visible);
            }
            if (image != null) {
                image.gameObject.SetActive(visible);
            }
            if (!visible) {
                continue;
            }

            SetRect(frame.rectTransform, panel.Anchor, panel.Anchor, new Vector2(0.5f, 0.5f), panel.Position, panel.Size);
            VisualUIHelper.ApplySolidColor(frame, panel.FrameColor, false);
            frame.transform.SetAsLastSibling();
            string visualID = ResolveDisplayVisualID(panel.VisualID, panel.FallbackVisualID);
            SetOffsets(image.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));
            VisualUIHelper.ApplyCoverSprite(image, visualID, panel.RegisteredColor, panel.MissingColor);
        }

        if (dialoguePanelImage != null) {
            dialoguePanelImage.transform.SetAsLastSibling();
        }
    }

    private void ApplyDialogueLayout(NarrativeOverlayPayload payload) {
        ResetDialogueLayout();

        string visualID = payload?.VisualRequest?.VisualID ?? string.Empty;
        if (payload != null && payload.UseBlackout && IsBlackoutVisual(visualID)) {
            ApplyBlackoutSubtitleLayout();
            return;
        }

        if (IsCinematicStageVisual(visualID) && string.IsNullOrEmpty(payload?.ActionID)) {
            ApplyCinematicSubtitleLayout();
            return;
        }

        if (visualID == "ui_status_card_prologue" || string.Equals(payload?.ActionID, "wipe_core", StringComparison.Ordinal)) {
            ApplyStatusCardLayout();
            return;
        }

        if (!string.IsNullOrEmpty(payload?.ActionID)) {
            ApplyActionFocusLayout(payload.ActionID);
        }
    }

    private void ResetDialogueLayout() {
        SetRect(
            dialoguePanelImage.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 34f),
            new Vector2(1180f, 210f));
        VisualUIHelper.ApplySolidColor(dialoguePanelImage, new Color(0.006f, 0.022f, 0.032f, 0.90f), true);

        speakerText.gameObject.SetActive(true);
        speakerText.alignment = TextAnchor.MiddleLeft;
        speakerText.fontSize = 25;
        speakerText.color = new Color(0.78f, 0.96f, 1f, 1f);
        SetOffsets(speakerText.rectTransform, new Vector2(58f, -58f), new Vector2(-260f, -16f));

        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.fontSize = 29;
        bodyText.color = new Color(0.92f, 0.98f, 1f, 0.98f);
        bodyText.resizeTextForBestFit = true;
        bodyText.resizeTextMinSize = 21;
        bodyText.resizeTextMaxSize = 29;
        SetOffsets(bodyText.rectTransform, new Vector2(62f, 72f), new Vector2(-120f, -54f));

        SetRect(
            continueButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-54f, 24f),
            new Vector2(64f, 44f));
        SetRect(
            autoButton.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-148f, -22f),
            new Vector2(82f, 26f));
        SetRect(
            logButton.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-56f, -22f),
            new Vector2(60f, 26f));
        SetRect(
            actionButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.2f),
            new Vector2(0.5f, 0.2f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(360f, 64f));
        SetRect(
            ritualActionPanelImage.rectTransform,
            new Vector2(0.5f, 0.24f),
            new Vector2(0.5f, 0.24f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(520f, 132f));
    }

    private void ApplyBlackoutSubtitleLayout() {
        SetRect(
            dialoguePanelImage.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 112f),
            new Vector2(820f, 108f));
        VisualUIHelper.ApplySolidColor(dialoguePanelImage, new Color(0.002f, 0.006f, 0.012f, 0.22f), false);

        speakerText.gameObject.SetActive(true);
        speakerText.alignment = TextAnchor.MiddleCenter;
        speakerText.fontSize = 22;
        SetOffsets(speakerText.rectTransform, new Vector2(0f, -40f), new Vector2(0f, -8f));

        bodyText.alignment = TextAnchor.MiddleCenter;
        bodyText.fontSize = 30;
        bodyText.color = new Color(0.9f, 0.96f, 1f, 1f);
        bodyText.resizeTextMinSize = 22;
        bodyText.resizeTextMaxSize = 30;
        SetOffsets(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, -42f));
    }

    private void ApplyCinematicSubtitleLayout() {
        SetRect(
            dialoguePanelImage.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 34f),
            new Vector2(1180f, 210f));
        VisualUIHelper.ApplySolidColor(dialoguePanelImage, new Color(0.006f, 0.022f, 0.032f, 0.90f), true);

        speakerText.gameObject.SetActive(true);
        speakerText.alignment = TextAnchor.MiddleLeft;
        speakerText.fontSize = 25;
        speakerText.color = new Color(0.78f, 0.96f, 1f, 1f);
        SetOffsets(speakerText.rectTransform, new Vector2(58f, -58f), new Vector2(-260f, -16f));

        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.fontSize = 29;
        bodyText.color = new Color(0.9f, 0.98f, 1f, 0.98f);
        bodyText.resizeTextMinSize = 21;
        bodyText.resizeTextMaxSize = 29;
        SetOffsets(bodyText.rectTransform, new Vector2(62f, 72f), new Vector2(-120f, -54f));

        SetRect(
            continueButton.GetComponent<RectTransform>(),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0.5f),
            new Vector2(-54f, 24f),
            new Vector2(64f, 44f));
        SetRect(
            autoButton.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(-148f, -22f),
            new Vector2(82f, 26f));
        SetRect(
            logButton.GetComponent<RectTransform>(),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(-56f, -22f),
            new Vector2(60f, 26f));
    }

    private void ApplyActionFocusLayout(string actionID) {
        bool isStartDoll = string.Equals(actionID, PrologueDollWakeNarrativeFlow.StartDollActionID, StringComparison.Ordinal);
        SetRect(
            dialoguePanelImage.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            isStartDoll ? new Vector2(0f, 30f) : new Vector2(0f, 36f),
            isStartDoll ? new Vector2(1120f, 126f) : new Vector2(920f, 104f));
        VisualUIHelper.ApplySolidColor(dialoguePanelImage, new Color(0.006f, 0.022f, 0.032f, isStartDoll ? 0.20f : 0.18f), true);
        speakerText.gameObject.SetActive(false);
        bodyText.alignment = TextAnchor.MiddleCenter;
        bodyText.fontSize = isStartDoll ? 26 : 23;
        bodyText.color = new Color(0.88f, 0.98f, 1f, 0.96f);
        SetOffsets(bodyText.rectTransform, new Vector2(46f, 20f), isStartDoll ? new Vector2(-46f, -22f) : new Vector2(-26f, -12f));
        SetRect(
            actionButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, isStartDoll ? 0.49f : 0.31f),
            new Vector2(0.5f, isStartDoll ? 0.49f : 0.31f),
            new Vector2(0.5f, 0.5f),
            isStartDoll ? new Vector2(0f, -34f) : Vector2.zero,
            isStartDoll ? new Vector2(350f, 68f) : new Vector2(340f, 64f));
        SetRect(
            ritualActionPanelImage.rectTransform,
            new Vector2(0.5f, isStartDoll ? 0.49f : 0.31f),
            new Vector2(0.5f, isStartDoll ? 0.49f : 0.31f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            isStartDoll ? new Vector2(540f, 142f) : new Vector2(470f, 112f));
        VisualUIHelper.ApplySolidColor(ritualActionPanelImage, new Color(0.006f, 0.055f, 0.066f, isStartDoll ? 0.48f : 0.56f), false);
        if (ritualActionPromptText != null) {
            if (isStartDoll) {
                ritualActionPromptText.gameObject.SetActive(false);
            } else {
                ritualActionPromptText.gameObject.SetActive(true);
                Stretch(ritualActionPromptText.rectTransform);
                ritualActionPromptText.fontSize = 22;
            }
        }
        ritualActionPanelImage.transform.SetAsLastSibling();
        actionButton.transform.SetAsLastSibling();
    }

    private void ApplyStatusCardLayout() {
        SetRect(
            dialoguePanelImage.rectTransform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 48f),
            new Vector2(1080f, 170f));
        VisualUIHelper.ApplySolidColor(dialoguePanelImage, new Color(0.004f, 0.02f, 0.03f, 0.76f), true);

        speakerText.gameObject.SetActive(true);
        speakerText.alignment = TextAnchor.MiddleLeft;
        speakerText.fontSize = 23;
        speakerText.color = new Color(0.78f, 0.95f, 1f, 1f);
        SetOffsets(speakerText.rectTransform, new Vector2(44f, -54f), new Vector2(-220f, -16f));

        bodyText.alignment = TextAnchor.UpperLeft;
        bodyText.fontSize = 25;
        bodyText.color = new Color(0.9f, 0.98f, 1f, 0.96f);
        bodyText.resizeTextMinSize = 18;
        bodyText.resizeTextMaxSize = 25;
        SetOffsets(bodyText.rectTransform, new Vector2(48f, 74f), new Vector2(-48f, -60f));

        SetRect(
            actionButton.GetComponent<RectTransform>(),
            new Vector2(0.5f, 0.22f),
            new Vector2(0.5f, 0.22f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(390f, 64f));
        SetRect(
            ritualActionPanelImage.rectTransform,
            new Vector2(0.5f, 0.23f),
            new Vector2(0.5f, 0.23f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(520f, 118f));
    }

    private void SetStageObjectsActive(bool active) {
        if (!active) {
            SetComicPageActive(false);
        }

        if (visualImage != null) {
            visualImage.gameObject.SetActive(active);
        }

        if (stageVeilImage != null) {
            stageVeilImage.gameObject.SetActive(active);
        }

        if (propCardImage != null) {
            propCardImage.gameObject.SetActive(false);
        }
        ClearStagePropLayer();

        if (dollImage != null) {
            dollImage.gameObject.SetActive(false);
        }
    }

    private void HideLegacyStagePropImages() {
        HideNamedChildExcept(transform, "StageProp_Image", propImage != null ? propImage.transform : null);
    }

    private void ClearStagePropLayer() {
        if (propCardImage != null) {
            propCardImage.gameObject.SetActive(false);
            propCardImage.sprite = null;
            VisualUIHelper.ApplySolidColor(propCardImage, Color.clear, false);
        }

        if (propImage != null) {
            propImage.gameObject.SetActive(false);
            propImage.sprite = null;
            VisualUIHelper.ApplySolidColor(propImage, Color.clear, false);
        }

        HideLegacyStagePropImages();
    }

    private static void HideNamedChildExcept(Transform root, string childName, Transform except) {
        if (root == null) {
            return;
        }

        for (int i = 0; i < root.childCount; i++) {
            Transform child = root.GetChild(i);
            if (child != except && child.name == childName) {
                child.gameObject.SetActive(false);
            }

            HideNamedChildExcept(child, childName, except);
        }
    }

    private static bool HasActiveNamedChild(Transform root, string childName, Transform except) {
        if (root == null) {
            return false;
        }

        for (int i = 0; i < root.childCount; i++) {
            Transform child = root.GetChild(i);
            if (child != except && child.name == childName && child.gameObject.activeInHierarchy) {
                return true;
            }

            if (HasActiveNamedChild(child, childName, except)) {
                return true;
            }
        }

        return false;
    }

    private void SetComicPageActive(bool active) {
        _comicPageVisible = active;
        _comicPageDollFocusVisible = false;
        _comicPageLargeFocusVisible = false;

        if (comicBackdropImage != null) {
            comicBackdropImage.gameObject.SetActive(active);
        }

        if (comicPageLabelText != null) {
            comicPageLabelText.gameObject.SetActive(false);
        }

        if (comicPanelFrameImages != null) {
            for (int i = 0; i < comicPanelFrameImages.Length; i++) {
                if (comicPanelFrameImages[i] != null) {
                    comicPanelFrameImages[i].gameObject.SetActive(false);
                }
            }
        }

        if (comicPanelImages != null) {
            for (int i = 0; i < comicPanelImages.Length; i++) {
                if (comicPanelImages[i] != null) {
                    comicPanelImages[i].gameObject.SetActive(false);
                }
            }
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
            _continueButtonText.text = visible ? "\u25b6" : string.Empty;
        }
    }

    private void ConfigureAction(string actionID, string label) {
        _currentActionID = actionID ?? string.Empty;
        bool visible = !string.IsNullOrEmpty(_currentActionID) && !string.IsNullOrEmpty(label);
        actionButton.gameObject.SetActive(visible);
        if (ritualActionPanelImage != null) {
            ritualActionPanelImage.gameObject.SetActive(visible);
        }
        if (!visible && ritualActionPromptText != null) {
            ritualActionPromptText.gameObject.SetActive(false);
        }
        if (visible) {
            SetAutoEnabled(false);
            Image actionImage = actionButton.GetComponent<Image>();
            if (actionImage != null) {
                bool isStartDoll = actionID == PrologueDollWakeNarrativeFlow.StartDollActionID;
                if (isStartDoll) {
                    VisualUIHelper.ApplySolidColor(actionImage, new Color(0.01f, 0.22f, 0.27f, 0.96f), true);
                } else {
                    VisualUIHelper.ApplyButtonSkin(actionButton, VisualAssetService.UIButtonPrimaryID, new Color(0.024f, 0.18f, 0.22f, 0.98f));
                    actionImage.color = new Color(0.024f, 0.18f, 0.22f, 0.98f);
                }
            }
            if (ritualActionPromptText != null) {
                bool isStartDoll = actionID == PrologueDollWakeNarrativeFlow.StartDollActionID;
                ritualActionPromptText.gameObject.SetActive(true);
                ritualActionPromptText.text = isStartDoll
                    ? "\u786e\u8ba4\u6838\u5fc3\u4ed3\u7a33\u5b9a\uff0c\u7136\u540e\u542f\u52a8\u5979\u3002"
                    : "\u53ea\u505a\u8fd9\u4e00\u6b21\u786e\u8ba4\u3002";
            }
            if (ritualActionPanelImage != null) {
                ritualActionPanelImage.transform.SetAsLastSibling();
            }
            actionButton.transform.SetAsLastSibling();
        }
        if (_actionButtonText != null) {
            _actionButtonText.text = visible ? label : string.Empty;
            _actionButtonText.fontSize = actionID == PrologueDollWakeNarrativeFlow.StartDollActionID ? 27 : 24;
            _actionButtonText.color = new Color(0.82f, 0.98f, 1f, 1f);
        }
    }

    private void OnContinueClicked() {
        if (HasContinueVisible && !HasActionVisible) {
            ContinueRequested?.Invoke();
        }
    }

    private void OnActionClicked() {
        if (!string.IsNullOrEmpty(_currentActionID)) {
            SetAutoEnabled(false);
            ActionRequested?.Invoke(_currentActionID);
        }
    }

    private void OnAutoClicked() {
        SetAutoEnabled(!_isAutoEnabled);
    }

    private void OnLogClicked() {
        if (logPanelImage == null) {
            return;
        }

        bool visible = !logPanelImage.gameObject.activeSelf;
        logPanelImage.gameObject.SetActive(visible);
        if (visible && logBodyText != null) {
            logBodyText.text = _lineLog.Count == 0 ? "\u8fd8\u6ca1\u6709\u5df2\u64ad\u653e\u5bf9\u767d\u3002" : string.Join("\n\n", _lineLog.ToArray());
        }
    }

    private bool ShouldShowUtilityControls(NarrativeOverlayPayload payload) {
        if (payload == null || payload.UseBlackout || HasComicPageVisible || !string.IsNullOrEmpty(payload.ActionID)) {
            return false;
        }

        string visualID = payload.VisualRequest?.VisualID ?? string.Empty;
        return visualID != "ui_status_card_prologue";
    }

    private void ConfigureUtilityControls(bool visible = true) {
        if (autoButton != null) {
            autoButton.gameObject.SetActive(visible);
        }

        if (logButton != null) {
            logButton.gameObject.SetActive(visible);
        }

        if (_autoButtonText != null) {
            _autoButtonText.text = _isAutoEnabled ? "自动·开" : "自动";
            _autoButtonText.fontSize = 14;
            _autoButtonText.color = _isAutoEnabled
                ? new Color(0.76f, 0.98f, 1f, 0.96f)
                : new Color(0.70f, 0.82f, 0.86f, 0.66f);
        }

        if (_logButtonText != null) {
            _logButtonText.text = "记录";
            _logButtonText.fontSize = 15;
            _logButtonText.color = new Color(0.70f, 0.82f, 0.86f, 0.66f);
        }
    }

    private void SetAutoEnabled(bool enabled) {
        _isAutoEnabled = enabled;
        ConfigureUtilityControls(autoButton != null && autoButton.gameObject.activeSelf);
    }

    private void AppendLineLog(string speakerName, string text) {
        if (string.IsNullOrWhiteSpace(text)) {
            return;
        }

        string speaker = string.IsNullOrWhiteSpace(speakerName) ? "\u65c1\u767d" : speakerName;
        string line = speaker + "\uff1a" + text;
        if (_lineLog.Count == 0 || _lineLog[_lineLog.Count - 1] != line) {
            _lineLog.Add(line);
        }

        if (_lineLog.Count > 40) {
            _lineLog.RemoveAt(0);
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
        VisualUIHelper.ApplySolidColor(image, new Color(0.08f, 0.28f, 0.34f, 0.94f), true);
        Outline outline = EnsureComponent<Outline>(obj);
        outline.effectColor = new Color(0.72f, 0.96f, 1f, 0.26f);
        outline.effectDistance = new Vector2(1f, -1f);

        Transform textTransform = obj.transform.Find("Text");
        GameObject textObj = textTransform != null ? textTransform.gameObject : new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        label = EnsureComponent<Text>(textObj);
        label.font = font;
        label.fontSize = 24;
        label.color = new Color(0.9f, 0.99f, 1f, 1f);
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

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta) {
        if (rect == null) {
            return;
        }

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetOffsets(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax) {
        if (rect == null) {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static bool IsBlackoutVisual(string visualID) {
        return visualID == "cg_t0_01a_depart_black";
    }

    private static bool IsCinematicStageVisual(string visualID) {
        return visualID == "cg_t0_01a_black_wake"
            || visualID == "cg_t0_01a_debt_notice"
            || visualID == "cg_t0_01a_repair_note"
            || visualID == "cg_t0_01a_core_shard"
            || visualID == "cg_t0_01a_find_no0";
    }

    private static bool IsComicPageVisual(string visualID) {
        return visualID == "cg_t0_01a_debt_notice"
            || visualID == "cg_t0_01a_repair_note"
            || visualID == "cg_t0_01a_core_shard"
            || visualID == "cg_t0_01a_find_no0"
            || visualID == "vfx_no0_core_start";
    }

    private static bool IsActive(Image image) {
        return image != null && image.gameObject.activeInHierarchy;
    }

    private static string SpriteName(Image image) {
        return image != null && image.sprite != null ? image.sprite.name : string.Empty;
    }

    private sealed class ComicPanelDefinition {
        public string VisualID;
        public string FallbackVisualID;
        public Vector2 Anchor;
        public Vector2 Position;
        public Vector2 Size;
        public Color FrameColor = new Color(0.72f, 0.94f, 0.98f, 0.14f);
        public Color RegisteredColor = Color.white;
        public Color MissingColor = new Color(0.08f, 0.11f, 0.13f, 0.96f);

        public ComicPanelDefinition(string visualID, string fallbackVisualID, Vector2 anchor, Vector2 position, Vector2 size, Color missingColor) {
            VisualID = visualID;
            FallbackVisualID = fallbackVisualID;
            Anchor = anchor;
            Position = position;
            Size = size;
            MissingColor = missingColor;
        }
    }

    private sealed class ComicPageDefinition {
        public string Label;
        public bool HasDollFocus;
        public bool HasLargeFocus = true;
        public Color BackdropColor = new Color(0.012f, 0.022f, 0.03f, 0.96f);
        public ComicPanelDefinition[] Panels;

        public static ComicPageDefinition Resolve(NarrativeVisualRequest request, string actionID) {
            string visualID = request?.VisualID ?? string.Empty;
            if (string.IsNullOrEmpty(visualID)) {
                return null;
            }

            switch (visualID) {
                case "cg_t0_01a_debt_notice":
                    return WorkshopEstablishPage();
                case "cg_t0_01a_repair_note":
                    return DebtPressurePage();
                case "cg_t0_01a_core_shard":
                    return RepairCluePage();
                case "cg_t0_01a_find_no0":
                    return FindNo0Page();
                case "vfx_no0_core_start":
                    return StartNo0Page();
                default:
                    return null;
            }
        }

        private static ComicPageDefinition WorkshopEstablishPage() {
            return new ComicPageDefinition {
                Label = "\u7b2c 1 \u65e5 / \u5de5\u574a",
                BackdropColor = new Color(0.002f, 0.01f, 0.016f, 1f),
                Panels = new[] {
                    Panel("cg_t0_01a_p02_panel01_workshop_wide", "bg_workshop_home_room", new Vector2(0.5f, 0.88f), new Vector2(0f, 0f), new Vector2(1120f, 300f), new Color(0.06f, 0.12f, 0.14f, 0.98f)),
                    Panel("cg_t0_01a_p02_panel02_debt_notice_door", VisualAssetService.UIIconDebtRentID, new Vector2(0.28f, 0.43f), new Vector2(0f, 0f), new Vector2(500f, 285f), new Color(0.78f, 0.86f, 0.82f, 0.26f)),
                    Panel("cg_t0_01a_p02_panel03_no0_hand_cloth", "doll_proto_0_stand", new Vector2(0.72f, 0.43f), new Vector2(0f, 0f), new Vector2(500f, 285f), new Color(0.06f, 0.1f, 0.12f, 0.98f))
                }
            };
        }

        private static ComicPageDefinition DebtPressurePage() {
            return new ComicPageDefinition {
                Label = "\u50ac\u7f34",
                BackdropColor = new Color(0.002f, 0.01f, 0.016f, 1f),
                Panels = new[] {
                    Panel("cg_t0_01a_p03_panel01_debt_notice_close", VisualAssetService.UIIconDebtRentID, new Vector2(0.35f, 0.58f), new Vector2(0f, 0f), new Vector2(660f, 545f), new Color(0.82f, 0.88f, 0.84f, 0.24f)),
                    Panel("cg_t0_01a_p03_panel02_collateral_shadow", "bg_workshop_studio", new Vector2(0.76f, 0.72f), new Vector2(0f, 0f), new Vector2(430f, 245f), new Color(0.035f, 0.06f, 0.07f, 0.98f)),
                    Panel("cg_t0_01a_p03_panel03_broken_parts", "item_mat_core_tier1_icon", new Vector2(0.76f, 0.36f), new Vector2(0f, 0f), new Vector2(430f, 245f), new Color(0.035f, 0.06f, 0.07f, 0.98f))
                }
            };
        }

        private static ComicPageDefinition RepairCluePage() {
            return new ComicPageDefinition {
                Label = "\u6700\u540e\u4e00\u6b21\u529e\u6cd5",
                BackdropColor = new Color(0.002f, 0.012f, 0.02f, 1f),
                Panels = new[] {
                    Panel("cg_t0_01a_p04_panel01_repair_note_close", VisualAssetService.UIIconWearRepairID, new Vector2(0.34f, 0.58f), new Vector2(0f, 0f), new Vector2(640f, 545f), new Color(0.75f, 0.88f, 0.88f, 0.2f)),
                    Panel("cg_t0_01a_p04_panel02_shallow_gate_glow", "bg_layer_select", new Vector2(0.76f, 0.72f), new Vector2(0f, 0f), new Vector2(440f, 245f), new Color(0.04f, 0.1f, 0.13f, 0.98f)),
                    Panel("cg_t0_01a_p04_panel03_core_shard_box", "item_mat_core_tier1_icon", new Vector2(0.76f, 0.36f), new Vector2(0f, 0f), new Vector2(440f, 245f), new Color(0.03f, 0.09f, 0.13f, 0.98f))
                }
            };
        }

        private static ComicPageDefinition FindNo0Page() {
            return new ComicPageDefinition {
                Label = "\u96f6\u53f7",
                HasDollFocus = true,
                HasLargeFocus = true,
                BackdropColor = new Color(0.002f, 0.012f, 0.02f, 1f),
                Panels = new[] {
                    Panel("cg_t0_01a_p05_panel01_broken_doll_parts", "doll_proto_0_stand", new Vector2(0.24f, 0.6f), new Vector2(0f, 0f), new Vector2(380f, 480f), new Color(0.05f, 0.09f, 0.1f, 0.98f)),
                    Panel("cg_t0_01a_p05_panel02_no0_half_reveal", "doll_proto_0_stand", new Vector2(0.55f, 0.56f), new Vector2(0f, 0f), new Vector2(560f, 575f), new Color(0.05f, 0.09f, 0.1f, 0.98f)),
                    Panel("cg_t0_01a_p05_panel03_no0_core_dim", "ui_core_glow", new Vector2(0.84f, 0.56f), new Vector2(0f, 0f), new Vector2(310f, 420f), new Color(0.05f, 0.09f, 0.1f, 0.98f))
                }
            };
        }

        private static ComicPageDefinition StartNo0Page() {
            return new ComicPageDefinition {
                Label = "\u542f\u52a8",
                HasDollFocus = true,
                HasLargeFocus = true,
                BackdropColor = new Color(0.002f, 0.014f, 0.024f, 1f),
                Panels = new[] {
                    Panel("cg_t0_01a_p06_panel01_no0_close", "doll_proto_0_stand", new Vector2(0.24f, 0.56f), new Vector2(0f, 0f), new Vector2(360f, 510f), new Color(0.04f, 0.08f, 0.1f, 0.98f)),
                    Panel("cg_t0_01a_p06_panel02_core_insert", "doll_proto_0_stand", new Vector2(0.56f, 0.56f), new Vector2(0f, 0f), new Vector2(520f, 510f), new Color(0.04f, 0.08f, 0.1f, 0.98f)),
                    Panel("cg_t0_01a_p06_panel03_core_wake", "ui_core_glow", new Vector2(0.84f, 0.56f), new Vector2(0f, 0f), new Vector2(320f, 510f), new Color(0.04f, 0.08f, 0.1f, 0.98f))
                }
            };
        }

        private static ComicPanelDefinition Panel(string visualID, string fallbackVisualID, Vector2 anchor, Vector2 position, Vector2 size, Color missingColor) {
            return new ComicPanelDefinition(visualID, fallbackVisualID, anchor, position, size, missingColor);
        }
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
        public Vector2 PropImageOffset = Vector2.zero;
        public Vector2 DollAnchor = new Vector2(0.7f, 0.17f);
        public Vector2 DollOffset = Vector2.zero;
        public Vector2 DollSize = new Vector2(430f, 700f);
        public Color FocusPanelColor = new Color(0.02f, 0.034f, 0.044f, 0.55f);

        public static PrologueStageComposition Resolve(NarrativeVisualRequest request) {
            string visualID = request?.VisualID ?? string.Empty;
            if (string.IsNullOrEmpty(visualID)) {
                return null;
            }

            switch (visualID) {
                case "cg_t0_01a_black_wake":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p01_wake_pov",
                        VeilColor = new Color(0.002f, 0.01f, 0.018f, 0.10f),
                        FocusPanelColor = new Color(0.02f, 0.08f, 0.10f, 0.08f)
                    };
                case "cg_t0_01a_depart_black":
                    return new PrologueStageComposition {
                        BlackoutOnly = true
                    };
                case "cg_t0_01a_debt_notice":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p02_panel01_workshop_wide",
                        VeilColor = new Color(0.006f, 0.024f, 0.034f, 0.18f)
                    };
                case "cg_t0_01a_repair_note":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p03_panel01_debt_notice_close",
                        VeilColor = new Color(0.012f, 0.028f, 0.036f, 0.12f)
                    };
                case "cg_t0_01a_core_shard":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p04_panel03_core_shard_box",
                        VeilColor = new Color(0.006f, 0.026f, 0.04f, 0.12f)
                    };
                case "cg_t0_01a_find_no0":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p05_panel03_no0_core_dim",
                        VeilColor = new Color(0.002f, 0.014f, 0.022f, 0.10f)
                    };
                case "vfx_no0_core_start":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p06_panel03_core_wake",
                        VeilColor = new Color(0.004f, 0.02f, 0.03f, 0.12f),
                        FocusPanelColor = new Color(0.24f, 0.86f, 1f, 0.1f)
                    };
                case "stand_no0_weak_sitting":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p06_panel01_no0_close",
                        VeilColor = new Color(0.004f, 0.02f, 0.03f, 0.18f),
                        PropAnchor = new Vector2(0.5f, 0.54f),
                        PropCardSize = new Vector2(620f, 380f),
                        PropImageSize = new Vector2(210f, 210f),
                        PropImageOffset = new Vector2(0f, 76f),
                        FocusPanelColor = new Color(0.24f, 0.86f, 1f, 0.1f)
                    };
                case "ui_status_card_prologue":
                    return new PrologueStageComposition {
                        BackgroundVisualID = "cg_t0_01a_p06_panel01_no0_close",
                        VeilColor = new Color(0.004f, 0.02f, 0.03f, 0.24f)
                    };
                default:
                    return null;
            }
        }
    }
}
