using UnityEngine;
using UnityEngine.UI;

public class WorkshopFormalV1PanelController : MonoBehaviour {
    private static readonly Color BackdropColor = new Color(0.004f, 0.006f, 0.008f, 1f);
    private static readonly Color BackgroundColor = new Color(0.64f, 0.58f, 0.46f, 0.44f);
    private static readonly Color BackgroundVeilColor = new Color(0.006f, 0.008f, 0.01f, 0.48f);
    private static readonly Color MainCardColor = new Color(0.2f, 0.22f, 0.2f, 0.74f);
    private static readonly Color HeaderColor = new Color(0.28f, 0.36f, 0.33f, 0.72f);
    private static readonly Color InfoPanelColor = new Color(0.16f, 0.18f, 0.17f, 0.62f);
    private static readonly Color EmphasisPanelColor = new Color(0.22f, 0.27f, 0.24f, 0.7f);
    private static readonly Color RowColor = new Color(0.22f, 0.25f, 0.22f, 0.66f);
    private static readonly Color BodyTextColor = new Color(0.9f, 0.92f, 0.88f, 0.94f);
    private static readonly Color IconColor = new Color(0.86f, 0.82f, 0.62f, 0.72f);

    private GameObject _rootPanel;
    private Font _defaultFont;
    private string _lastActionFeedback;
    private string _lastActionScreenID;

    public string CurrentScreenID { get; private set; }

    public void Show(string screenID) {
        _lastActionFeedback = string.Empty;
        _lastActionScreenID = string.Empty;
        ShowInternal(screenID);
    }

    private void ShowInternal(string screenID, bool preserveActionFeedback = false) {
        if (!preserveActionFeedback) {
            _lastActionFeedback = string.Empty;
            _lastActionScreenID = string.Empty;
        }

        PanelSpec spec = ResolveSpec(screenID);
        if (spec == null) {
            Debug.LogWarning($"[WorkshopFormalV1Panel] Unknown screenID: {screenID}");
            return;
        }

        _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Hide();
        CurrentScreenID = spec.ScreenID;
        BuildPanel(spec);
    }

    public void Hide() {
        HideInternal(false);
    }

    public void HideImmediateForAcceptance() {
        HideInternal(true);
    }

    private void HideInternal(bool immediate) {
        CurrentScreenID = string.Empty;
        if (_rootPanel == null) {
            return;
        }

        if (immediate || !Application.isPlaying) {
            DestroyImmediate(_rootPanel);
        } else {
            Destroy(_rootPanel);
        }
        _rootPanel = null;
    }

    private void BuildPanel(PanelSpec spec) {
        Transform rootParent = ResolveRootParent();
        _rootPanel = CreateRectObject(spec.RootName, rootParent);
        RectTransform rootRect = _rootPanel.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image backdrop = _rootPanel.AddComponent<Image>();
        VisualUIHelper.ApplySolidColor(backdrop, BackdropColor, true);

        Image background = CreateImage(spec.BackgroundName, _rootPanel.transform);
        Stretch(background.rectTransform);
        VisualUIHelper.ApplyCoverSprite(
            background,
            string.IsNullOrEmpty(spec.BackgroundVisualID) ? VisualAssetService.WorkshopBackgroundID : spec.BackgroundVisualID,
            BackgroundColor,
            new Color(0.08f, 0.07f, 0.055f, 0.94f));

        Image veil = CreateImage("FormalV2BackgroundVeil_Image", _rootPanel.transform);
        Stretch(veil.rectTransform);
        VisualUIHelper.ApplySolidColor(veil, BackgroundVeilColor, false);

        WorkshopFormalV1PanelBinding binding = WorkshopFormalV1PanelBindingService.Build(spec.ScreenID, GameRoot.Core?.CurrentPlayer);
        if (TryBuildFormalV2SpecialPanel(spec, binding)) {
            FinalizeRootPanel();
            return;
        }

        Image card = CreateImage(spec.CardName, _rootPanel.transform);
        ConfigureCenterRect(card.rectTransform, Vector2.zero, new Vector2(1360f, 815f));
        ApplyPanelFill(card, MainCardColor, new Color(0.2f, 0.42f, 0.38f, 0.36f), new Vector2(2f, -2f));

        BuildScreenHeader(card.transform, spec, new Vector2(70f, 38f), new Vector2(1220f, 88f));
        BuildContentPanels(card.transform, spec, binding);
        BuildRows(card.transform, spec, binding);
        BuildButtons(card.transform, spec);

        FinalizeRootPanel();
    }

    private void FinalizeRootPanel() {
        CanvasGroup group = _rootPanel.GetComponent<CanvasGroup>();
        if (group == null) {
            group = _rootPanel.AddComponent<CanvasGroup>();
        }
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        _rootPanel.transform.SetAsLastSibling();
    }

    private bool TryBuildFormalV2SpecialPanel(PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        switch (spec.ScreenID) {
            case "maintenance_panel":
                BuildMaintenancePanelV2(spec, binding);
                return true;
            case "chassis_upgrade_panel":
                BuildChassisUpgradePanelV2(spec, binding);
                return true;
            case "doll_interaction":
                BuildDollInteractionPanelV2(spec, binding);
                return true;
            case "doll_room":
                BuildDollRoomPanelV2(spec, binding);
                return true;
            default:
                return false;
        }
    }

    private void BuildContentPanels(Transform card, PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        for (int i = 0; i < spec.InfoPanels.Length; i++) {
            NamedRect panel = spec.InfoPanels[i];
            Image image = CreateImage(panel.Name, card);
            ConfigureTopLeftRect(image.rectTransform, panel.X, panel.Y, panel.Width, panel.Height);
            Color panelColor = panel.UseMainPanel ? EmphasisPanelColor : InfoPanelColor;
            ApplyPanelFill(image, panelColor, new Color(0.2f, 0.42f, 0.38f, 0.24f), new Vector2(1f, -1f));
            string panelText = WorkshopFormalV1PanelBindingService.ResolveText(binding.PanelTexts, panel.Name, panel.Label);
            CreateText(
                $"{panel.Name}_Text",
                image.transform,
                panelText,
                new Vector2(18f, 14f),
                new Vector2(Mathf.Max(120f, panel.Width - 36f), Mathf.Max(40f, panel.Height - 28f)),
                18,
                BodyTextColor,
                TextAnchor.UpperLeft);
        }
    }

    private void BuildRows(Transform card, PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        for (int i = 0; i < spec.Rows.Length; i++) {
            RowSpec row = spec.Rows[i];
            Image rowImage = CreateImage(row.Name, card);
            ConfigureTopLeftRect(rowImage.rectTransform, row.X, row.Y, row.Width, row.Height);
            ApplyPanelFill(rowImage, RowColor, new Color(0.24f, 0.52f, 0.46f, 0.2f), new Vector2(1f, -1f));
            string rowText = WorkshopFormalV1PanelBindingService.ResolveText(binding.RowTexts, row.Name, row.Label);
            CreateText(
                $"{row.Name}_Text",
                rowImage.transform,
                rowText,
                new Vector2(16f, 8f),
                new Vector2(Mathf.Max(100f, row.Width - 32f), Mathf.Max(30f, row.Height - 16f)),
                17,
                BodyTextColor,
                TextAnchor.MiddleLeft);
        }
    }

    private void BuildButtons(Transform card, PanelSpec spec) {
        for (int i = 0; i < spec.Buttons.Length; i++) {
            ButtonSpec buttonSpec = spec.Buttons[i];
            Button button = CreateButton(buttonSpec.Name, buttonSpec.Label, card, buttonSpec.X, buttonSpec.Y, buttonSpec.Width, buttonSpec.Height, buttonSpec.VisualID);
            if (buttonSpec.ClosesPanel) {
                button.onClick.AddListener(Hide);
            } else {
                string capturedScreenID = spec.ScreenID;
                string capturedButtonName = buttonSpec.Name;
                button.onClick.AddListener(() => ExecutePanelAction(capturedScreenID, capturedButtonName));
            }
        }

        if (!string.IsNullOrEmpty(_lastActionFeedback) && _lastActionScreenID == spec.ScreenID) {
            CreateText(
                "ActionFeedback_Text",
                card,
                _lastActionFeedback,
                new Vector2(60f, 820f),
                new Vector2(1240f, 46f),
                17,
                new Color(0.94f, 0.82f, 0.5f, 1f),
                TextAnchor.MiddleLeft);
        }
    }

    private void BuildScreenHeader(Transform parent, PanelSpec spec, Vector2 topLeft, Vector2 size) {
        Image header = CreateSlicedPanel("HeaderPanel", parent, topLeft.x, topLeft.y, size.x, size.y, VisualAssetService.UIPanelInfoID, HeaderColor);
        CreateIcon(spec.PrimaryIconName, header.transform, new Vector2(30f, 14f), spec.PrimaryIconVisualID);
        CreateText("Title_Text", header.transform, spec.Title, new Vector2(104f, 16f), new Vector2(Mathf.Max(320f, size.x * 0.48f), 52f), 30, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateTitleDivider(header.transform, new Vector2(size.x * 0.58f, 33f), new Vector2(size.x * 0.32f, 18f));

        float iconX = size.x * 0.63f;
        for (int i = 0; i < spec.HeaderIcons.Length; i++) {
            IconSpec icon = spec.HeaderIcons[i];
            CreateIcon(icon.Name, header.transform, new Vector2(iconX + i * 72f, 20f), icon.VisualID);
        }
    }

    private void BuildMaintenancePanelV2(PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        RectTransform stage = CreateReferenceStage(spec.CardName);
        BuildScreenHeader(stage, spec, new Vector2(130f, 42f), new Vector2(1660f, 86f));

        Image careBay = CreateSlicedPanel("CareBay", stage, 170f, 165f, 610f, 635f, VisualAssetService.UIPanelMainID, new Color(0.86f, 0.92f, 0.86f, 0.92f));
        CreateText("CareBayTitle_Text", careBay.transform, "Care Bay", new Vector2(28f, 22f), new Vector2(260f, 42f), 25, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateContainSprite("DollCare_Image", careBay.transform, "doll_proto_0_stand", new Vector2(150f, 82f), new Vector2(330f, 460f), VisualDisplaySpecs.DollStand, new Color(1f, 1f, 1f, 0.98f), new Color(0.42f, 0.32f, 0.24f, 0.92f));
        Image conditionPlate = CreateSlicedPanel("DollConditionPanel", careBay.transform, 28f, 494f, 554f, 112f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.15f, 0.13f, 0.86f));
        CreatePanelText(conditionPlate.transform, ResolvePanelText(binding, "DollConditionPanel", "Doll condition"), 18);

        Image diagnosis = CreateSlicedPanel("DiagnosisBoard", stage, 850f, 165f, 515f, 360f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.13f, 0.12f, 0.9f));
        CreateIcon("DivePermitBadge_Image", diagnosis.transform, new Vector2(24f, 24f), VisualAssetService.UIIconDivePermitID);
        CreateText("DiagnosisTitle_Text", diagnosis.transform, "Diagnosis", new Vector2(104f, 32f), new Vector2(350f, 36f), 25, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateText("DiveReadinessPanel_Text", diagnosis.transform, ResolvePanelText(binding, "DiveReadinessPanel", "Dive readiness"), new Vector2(28f, 94f), new Vector2(460f, 116f), 18, BodyTextColor, TextAnchor.UpperLeft);
        CreateText("WearCorrosionPanel_Text", diagnosis.transform, ResolvePanelText(binding, "WearCorrosionPanel", "Wear and corrosion"), new Vector2(28f, 222f), new Vector2(460f, 108f), 17, BodyTextColor, TextAnchor.UpperLeft);

        Image tray = CreateSlicedPanel("TreatmentTray", stage, 835f, 548f, 790f, 230f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.13f, 0.12f, 0.9f));
        CreateText("TreatmentTrayTitle_Text", tray.transform, "Treatment Tray", new Vector2(24f, 16f), new Vector2(290f, 34f), 23, new Color(0.94f, 0.82f, 0.58f, 1f), TextAnchor.MiddleLeft);
        CreateToolCard(tray.transform, "WearPlanCard", new Vector2(28f, 66f), VisualAssetService.UIIconWearRepairID, "Wear repair", ResolveRowText(binding, "CostRow_Template", "Repair parts"));
        CreateToolCard(tray.transform, "PurifyPlanCard", new Vector2(276f, 66f), VisualAssetService.UIIconCorruptionPurifyID, "Purify", ResolveRowText(binding, "StatusRow_Template", "Warning status"));
        CreateToolCard(tray.transform, "PermitPlanCard", new Vector2(524f, 66f), VisualAssetService.UIIconDivePermitID, "Dive check", "Permit gate");

        Image cost = CreateSlicedPanel("CostTokenRow", stage, 850f, 800f, 420f, 125f, VisualAssetService.UIPanelInfoID, new Color(0.11f, 0.13f, 0.12f, 0.88f));
        CreateIcon("CostMoneyIcon_Image", cost.transform, new Vector2(20f, 28f), VisualAssetService.UIIconMoneyID);
        CreateText("MaterialCostListPanel_Text", cost.transform, ResolvePanelText(binding, "MaterialCostListPanel", "Material cost"), new Vector2(96f, 18f), new Vector2(300f, 92f), 17, BodyTextColor, TextAnchor.UpperLeft);

        Image action = CreateSlicedPanel("MaintenanceActionPanel", stage, 1295f, 775f, 315f, 170f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.16f, 0.13f, 0.92f));
        CreateText("RepairActionPanel_Text", action.transform, ResolvePanelText(binding, "RepairActionPanel", "Repair action"), new Vector2(20f, 14f), new Vector2(275f, 48f), 17, BodyTextColor, TextAnchor.UpperLeft);
        CreateBoundButton(spec, "FullRepair_Button", action.transform, 20f, 76f, 130f, 50f, VisualAssetService.UIButtonPrimaryID);
        CreateBoundButton(spec, "UseRepairKit_Button", action.transform, 166f, 76f, 126f, 50f, VisualAssetService.UIButtonSecondaryID);
        CreateBoundButton(spec, "Postpone_Button", action.transform, 20f, 132f, 130f, 34f, VisualAssetService.UIButtonDangerID);
        CreateBoundButton(spec, "Close_Button", action.transform, 166f, 132f, 126f, 34f, VisualAssetService.UIButtonSecondaryID);

        CreateActionFeedback(stage, spec, new Vector2(850f, 945f), new Vector2(760f, 54f));
    }

    private void BuildChassisUpgradePanelV2(PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        RectTransform stage = CreateReferenceStage(spec.CardName);
        BuildScreenHeader(stage, spec, new Vector2(130f, 42f), new Vector2(1660f, 86f));

        Image table = CreateSlicedPanel("BlueprintTable", stage, 150f, 130f, 1620f, 835f, VisualAssetService.UIPanelMainID, new Color(0.88f, 0.9f, 0.82f, 0.9f));
        CreateText("BlueprintTableTitle_Text", table.transform, "Blueprint Table", new Vector2(34f, 26f), new Vector2(360f, 42f), 26, spec.AccentColor, TextAnchor.MiddleLeft);

        Image current = CreateSlicedPanel("CurrentChassisBlueprint", table.transform, 90f, 120f, 530f, 440f, VisualAssetService.UIInventoryChassisPanelID, new Color(0.12f, 0.16f, 0.18f, 0.92f));
        CreateText("CurrentTitle_Text", current.transform, "Current", new Vector2(26f, 22f), new Vector2(220f, 36f), 24, new Color(0.76f, 0.9f, 1f, 1f), TextAnchor.MiddleLeft);
        CreateContainSprite("CurrentChassis_Image", current.transform, "chassis_chassis_lv1_basic_frame", new Vector2(138f, 76f), new Vector2(260f, 250f), VisualDisplaySpecs.ChassisFrame, Color.white, new Color(0.26f, 0.34f, 0.42f, 0.92f));
        CreateText("CurrentChassisPanel_Text", current.transform, ResolvePanelText(binding, "CurrentChassisPanel", "Current chassis"), new Vector2(30f, 326f), new Vector2(470f, 90f), 15, BodyTextColor, TextAnchor.UpperLeft);

        Image arrow = CreateSlicedPanel("UpgradeArrowPlate", table.transform, 670f, 198f, 280f, 250f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.18f, 0.2f, 0.9f));
        CreateIcon("ArrowIcon_Image", arrow.transform, new Vector2(108f, 26f), VisualAssetService.UIIconChassisUpgradeID);
        CreateText("ArrowGlyph_Text", arrow.transform, ">", new Vector2(98f, 86f), new Vector2(86f, 70f), 48, spec.AccentColor, TextAnchor.MiddleCenter);
        CreateText("CapacityDeltaPanel_Text", arrow.transform, ResolvePanelText(binding, "CapacityDeltaPanel", "Capacity change"), new Vector2(24f, 164f), new Vector2(232f, 62f), 16, BodyTextColor, TextAnchor.UpperCenter);

        Image next = CreateSlicedPanel("NextChassisBlueprint", table.transform, 1000f, 120f, 530f, 440f, VisualAssetService.UIInventoryChassisPanelID, new Color(0.12f, 0.16f, 0.18f, 0.92f));
        CreateText("NextTitle_Text", next.transform, "Next", new Vector2(26f, 22f), new Vector2(220f, 36f), 24, new Color(0.96f, 0.86f, 0.54f, 1f), TextAnchor.MiddleLeft);
        CreateContainSprite("NextChassis_Image", next.transform, "chassis_chassis_lv2_expanded_frame", new Vector2(138f, 76f), new Vector2(260f, 250f), VisualDisplaySpecs.ChassisFrame, Color.white, new Color(0.38f, 0.32f, 0.22f, 0.92f));
        CreateText("NextChassisPanel_Text", next.transform, ResolvePanelText(binding, "NextChassisPanel", "Next chassis"), new Vector2(30f, 326f), new Vector2(470f, 90f), 15, BodyTextColor, TextAnchor.UpperLeft);

        Image delta = CreateSlicedPanel("ChassisDeltaStrip", table.transform, 90f, 610f, 900f, 120f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.13f, 0.14f, 0.9f));
        CreateText("ChassisDeltaStrip_Text", delta.transform, ResolveRowText(binding, "CurrentChassisRow", "Current grid") + "\n" + ResolveRowText(binding, "NextChassisRow", "Upgrade target"), new Vector2(24f, 18f), new Vector2(840f, 82f), 19, BodyTextColor, TextAnchor.UpperLeft);

        Image costs = CreateSlicedPanel("ChassisCostTokens", table.transform, 1010f, 592f, 305f, 156f, VisualAssetService.UIPanelInfoID, new Color(0.11f, 0.13f, 0.12f, 0.88f));
        CreateIcon("MaterialNeedIcon_Image", costs.transform, new Vector2(18f, 34f), VisualAssetService.UIIconMaterialNeedID);
        CreateText("MaterialNeedPanel_Text", costs.transform, ResolvePanelText(binding, "MaterialNeedPanel", ResolveRowText(binding, "MaterialNeedRow_Template", "Material needs")), new Vector2(92f, 18f), new Vector2(190f, 116f), 14, BodyTextColor, TextAnchor.UpperLeft);

        Image action = CreateSlicedPanel("ChassisActionPanel", table.transform, 1320f, 600f, 210f, 140f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.16f, 0.18f, 0.9f));
        CreateBoundButton(spec, "Upgrade_Button", action.transform, 22f, 24f, 166f, 58f, VisualAssetService.UIButtonPrimaryID);
        CreateBoundButton(spec, "Blueprint_Button", action.transform, 22f, 92f, 78f, 36f, VisualAssetService.UIButtonSecondaryID);
        CreateBoundButton(spec, "Close_Button", action.transform, 110f, 92f, 78f, 36f, VisualAssetService.UIButtonSecondaryID);

        CreateActionFeedback(stage, spec, new Vector2(240f, 980f), new Vector2(1440f, 46f));
    }

    private void BuildDollInteractionPanelV2(PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        RectTransform stage = CreateReferenceStage(spec.CardName);
        BuildScreenHeader(stage, spec, new Vector2(130f, 42f), new Vector2(1660f, 86f));

        Image condition = CreateSlicedPanel("ConditionRibbon", stage, 170f, 142f, 1580f, 116f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.12f, 0.16f, 0.88f));
        CreateIcon("ConditionMementoIcon_Image", condition.transform, new Vector2(26f, 14f), VisualAssetService.UIIconMementoID);
        CreateText("DollStagePanel_Text", condition.transform, ResolvePanelText(binding, "DollStagePanel", "Doll stage"), new Vector2(106f, 18f), new Vector2(1380f, 78f), 18, BodyTextColor, TextAnchor.MiddleLeft);

        Image tools = CreateSlicedPanel("InteractionTools", stage, 180f, 260f, 330f, 540f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.14f, 0.15f, 0.88f));
        CreateText("InteractionToolsTitle_Text", tools.transform, "Tools", new Vector2(24f, 22f), new Vector2(210f, 34f), 24, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateBoundIconButton(spec, "Touch_Button", tools.transform, 34f, 82f, VisualAssetService.UIIconTouchID, VisualAssetService.UIButtonPrimaryID);
        CreateBoundIconButton(spec, "Talk_Button", tools.transform, 34f, 178f, VisualAssetService.UIIconTalkID, VisualAssetService.UIButtonSecondaryID);
        CreateStaticIconCard("GiftModeCard", tools.transform, new Vector2(34f, 274f), VisualAssetService.UIIconGiftID, "Gift");
        CreateStaticIconCard("CareModeCard", tools.transform, new Vector2(34f, 370f), VisualAssetService.UIIconMaintenanceID, "Care");

        Image dollStage = CreateSlicedPanel("DollStage", stage, 610f, 245f, 650f, 565f, VisualAssetService.UIPanelMainID, new Color(0.9f, 0.88f, 0.82f, 0.86f));
        CreateContainSprite("DollInteractionDoll_Image", dollStage.transform, "doll_proto_0_stand", new Vector2(110f, 18f), new Vector2(430f, 520f), VisualDisplaySpecs.DollStand, Color.white, new Color(0.42f, 0.32f, 0.24f, 0.92f));

        Image tray = CreateSlicedPanel("GiftTopicTray", stage, 1320f, 260f, 420f, 540f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.13f, 0.14f, 0.88f));
        CreateText("GiftTopicTitle_Text", tray.transform, "Gift / Topic", new Vector2(24f, 22f), new Vector2(250f, 34f), 24, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateText("GiftTopicListPanel_Text", tray.transform, ResolvePanelText(binding, "GiftTopicListPanel", "Gift and topic list"), new Vector2(24f, 76f), new Vector2(372f, 180f), 18, BodyTextColor, TextAnchor.UpperLeft);
        Image talkRow = CreateSlicedPanel("TalkOptionRow_Template", tray.transform, 24f, 288f, 372f, 72f, VisualAssetService.UIPanelInfoID, new Color(0.16f, 0.18f, 0.17f, 0.9f));
        CreateText("TalkOptionRow_Text", talkRow.transform, ResolveRowText(binding, "TalkOptionRow_Template", "Talk"), new Vector2(16f, 10f), new Vector2(340f, 48f), 17, BodyTextColor, TextAnchor.MiddleLeft);
        Image giftRow = CreateSlicedPanel("GiftOptionRow_Template", tray.transform, 24f, 374f, 372f, 72f, VisualAssetService.UIPanelInfoID, new Color(0.18f, 0.17f, 0.2f, 0.9f));
        CreateText("GiftOptionRow_Text", giftRow.transform, ResolveRowText(binding, "GiftOptionRow_Template", "Gift"), new Vector2(16f, 10f), new Vector2(340f, 48f), 17, BodyTextColor, TextAnchor.MiddleLeft);

        Image feedback = CreateSlicedPanel("FeedbackDialogue", stage, 420f, 842f, 1080f, 150f, VisualAssetService.UIPanelMainID, new Color(0.14f, 0.12f, 0.14f, 0.92f));
        CreateText("FeedbackPanel_Text", feedback.transform, ResolvePanelText(binding, "FeedbackPanel", "Interaction feedback"), new Vector2(34f, 26f), new Vector2(1010f, 88f), 20, BodyTextColor, TextAnchor.UpperLeft);
        CreateBoundButton(spec, "Close_Button", stage, 1550f, 892f, 150f, 58f, VisualAssetService.UIButtonSecondaryID);

        CreateActionFeedback(stage, spec, new Vector2(420f, 1002f), new Vector2(1080f, 44f));
    }

    private void BuildDollRoomPanelV2(PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        RectTransform stage = CreateReferenceStage(spec.CardName);
        BuildScreenHeader(stage, spec, new Vector2(130f, 42f), new Vector2(1660f, 78f));

        Image window = CreateSlicedPanel("WindowStateArea", stage, 110f, 140f, 480f, 330f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.12f, 0.14f, 0.72f));
        CreateIcon("WindowWarningIcon_Image", window.transform, new Vector2(24f, 24f), VisualAssetService.UIIconWarningID);
        CreateText("ObservationPanel_Text", window.transform, ResolvePanelText(binding, "ObservationPanel", "Observation"), new Vector2(102f, 26f), new Vector2(340f, 252f), 18, BodyTextColor, TextAnchor.UpperLeft);

        Image diary = CreateSlicedPanel("DiaryDesk", stage, 170f, 535f, 480f, 310f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.12f, 0.13f, 0.76f));
        CreateIcon("DiaryDeskIcon_Image", diary.transform, new Vector2(24f, 22f), VisualAssetService.UIIconDiaryID);
        CreateText("DiaryPanel_Text", diary.transform, ResolvePanelText(binding, "DiaryPanel", "Diary"), new Vector2(102f, 28f), new Vector2(340f, 220f), 18, BodyTextColor, TextAnchor.UpperLeft);

        Image dollStage = CreateSlicedPanel("DollIdleStage", stage, 650f, 185f, 540f, 645f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.1f, 0.12f, 0.42f));
        CreateContainSprite("DollRoomDoll_Image", dollStage.transform, "doll_proto_0_stand", new Vector2(72f, 18f), new Vector2(396f, 590f), VisualDisplaySpecs.DollStand, Color.white, new Color(0.42f, 0.32f, 0.24f, 0.92f));

        Image shelf = CreateSlicedPanel("MementoShelf", stage, 1200f, 160f, 500f, 470f, VisualAssetService.UIPanelInfoID, new Color(0.11f, 0.12f, 0.1f, 0.78f));
        CreateText("MementoShelfTitle_Text", shelf.transform, "Mementos", new Vector2(26f, 20f), new Vector2(250f, 36f), 24, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateMementoSlot(shelf.transform, 34f, 82f, ResolveRowText(binding, "MementoSlotRow_0", "Memento slot"));
        CreateMementoSlot(shelf.transform, 252f, 82f, "Memory");
        CreateMementoSlot(shelf.transform, 34f, 232f, "Gift");
        CreateMementoSlot(shelf.transform, 252f, 232f, "Empty");
        CreateText("MementoDisplayPanel_Text", shelf.transform, ResolvePanelText(binding, "MementoDisplayPanel", "Memento display"), new Vector2(28f, 372f), new Vector2(440f, 70f), 16, BodyTextColor, TextAnchor.UpperLeft);

        Image whisper = CreateSlicedPanel("RoomDetailWhisper", stage, 760f, 850f, 700f, 125f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.11f, 0.12f, 0.76f));
        CreateText("RoomStagePanel_Text", whisper.transform, ResolvePanelText(binding, "RoomStagePanel", "Room stage") + "\n" + ResolveRowText(binding, "DiaryRow_Template", "Diary entry"), new Vector2(24f, 18f), new Vector2(650f, 84f), 18, BodyTextColor, TextAnchor.UpperLeft);

        Image actions = CreateSlicedPanel("RoomActionStrip", stage, 1480f, 840f, 300f, 140f, VisualAssetService.UIPanelInfoID, new Color(0.1f, 0.12f, 0.13f, 0.7f));
        CreateBoundButton(spec, "Observe_Button", actions.transform, 24f, 26f, 252f, 48f, VisualAssetService.UIButtonSecondaryID);
        CreateBoundButton(spec, "Close_Button", actions.transform, 24f, 84f, 252f, 38f, VisualAssetService.UIButtonSecondaryID);

        CreateActionFeedback(stage, spec, new Vector2(760f, 982f), new Vector2(700f, 44f));
    }

    private RectTransform CreateReferenceStage(string name) {
        Image stageImage = CreateImage(name, _rootPanel.transform);
        VisualUIHelper.ApplySolidColor(stageImage, new Color(0f, 0f, 0f, 0.01f), false);
        RectTransform stage = stageImage.rectTransform;
        ConfigureCenterRect(stage, Vector2.zero, new Vector2(1920f, 1080f));
        return stage;
    }

    private Image CreateSlicedPanel(string name, Transform parent, float x, float y, float width, float height, string visualID, Color fallbackColor) {
        Image image = CreateImage(name, parent);
        ConfigureTopLeftRect(image.rectTransform, x, y, width, height);
        VisualUIHelper.ApplySlicedSprite(image, visualID, new Color(1f, 1f, 1f, Mathf.Clamp01(fallbackColor.a)), fallbackColor, false);
        return image;
    }

    private void CreatePanelText(Transform parent, string content, int fontSize) {
        CreateText("Text", parent, content, new Vector2(18f, 14f), new Vector2(520f, 82f), fontSize, BodyTextColor, TextAnchor.UpperLeft);
    }

    private Image CreateContainSprite(string name, Transform parent, string visualID, Vector2 topLeft, Vector2 size, Vector2 displaySpec, Color registeredColor, Color missingColor) {
        Image image = CreateImage(name, parent);
        ConfigureTopLeftRect(image.rectTransform, topLeft.x, topLeft.y, size.x, size.y);
        VisualUIHelper.ApplyContainSprite(image, visualID, displaySpec, registeredColor, missingColor, false);
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private void CreateToolCard(Transform parent, string name, Vector2 topLeft, string iconVisualID, string title, string body) {
        Image card = CreateSlicedPanel(name, parent, topLeft.x, topLeft.y, 228f, 132f, VisualAssetService.UIPanelInfoID, new Color(0.15f, 0.17f, 0.15f, 0.9f));
        CreateIcon(name + "_Icon", card.transform, new Vector2(16f, 30f), iconVisualID);
        CreateText(name + "_Title", card.transform, title, new Vector2(88f, 16f), new Vector2(122f, 28f), 17, new Color(0.96f, 0.86f, 0.58f, 1f), TextAnchor.MiddleLeft);
        CreateText(name + "_Body", card.transform, body, new Vector2(88f, 48f), new Vector2(122f, 68f), 13, BodyTextColor, TextAnchor.UpperLeft);
    }

    private void CreateBoundIconButton(PanelSpec spec, string buttonName, Transform parent, float x, float y, string iconVisualID, string buttonVisualID) {
        ButtonSpec buttonSpec = FindButton(spec, buttonName);
        if (buttonSpec == null) {
            return;
        }

        Button button = CreateButton(buttonSpec.Name, buttonSpec.Label, parent, x, y, 262f, 76f, buttonVisualID);
        CreateIcon(buttonName + "_Icon", button.transform, new Vector2(18f, 6f), iconVisualID);
        if (buttonSpec.ClosesPanel) {
            button.onClick.AddListener(Hide);
        } else {
            string capturedScreenID = spec.ScreenID;
            string capturedButtonName = buttonSpec.Name;
            button.onClick.AddListener(() => ExecutePanelAction(capturedScreenID, capturedButtonName));
        }
    }

    private void CreateStaticIconCard(string name, Transform parent, Vector2 topLeft, string iconVisualID, string label) {
        Image card = CreateSlicedPanel(name, parent, topLeft.x, topLeft.y, 262f, 76f, VisualAssetService.UIPanelInfoID, new Color(0.12f, 0.14f, 0.15f, 0.62f));
        CreateIcon(name + "_Icon", card.transform, new Vector2(18f, 6f), iconVisualID);
        CreateText(name + "_Text", card.transform, label, new Vector2(96f, 12f), new Vector2(140f, 46f), 20, BodyTextColor, TextAnchor.MiddleLeft);
    }

    private void CreateMementoSlot(Transform parent, float x, float y, string label) {
        Image slot = CreateSlicedPanel("MementoSlot", parent, x, y, 190f, 118f, VisualAssetService.UIRoomMementoSlotID, new Color(0.14f, 0.13f, 0.1f, 0.8f));
        CreateIcon("MementoSlotIcon_Image", slot.transform, new Vector2(64f, 12f), VisualAssetService.UIIconMementoID);
        CreateText("MementoSlot_Text", slot.transform, label, new Vector2(14f, 78f), new Vector2(162f, 28f), 15, BodyTextColor, TextAnchor.MiddleCenter);
    }

    private void CreateBoundButton(PanelSpec spec, string buttonName, Transform parent, float x, float y, float width, float height, string visualID) {
        ButtonSpec buttonSpec = FindButton(spec, buttonName);
        if (buttonSpec == null) {
            return;
        }

        Button button = CreateButton(buttonSpec.Name, buttonSpec.Label, parent, x, y, width, height, visualID);
        if (buttonSpec.ClosesPanel) {
            button.onClick.AddListener(Hide);
        } else {
            string capturedScreenID = spec.ScreenID;
            string capturedButtonName = buttonSpec.Name;
            button.onClick.AddListener(() => ExecutePanelAction(capturedScreenID, capturedButtonName));
        }
    }

    private ButtonSpec FindButton(PanelSpec spec, string buttonName) {
        for (int i = 0; i < spec.Buttons.Length; i++) {
            if (spec.Buttons[i].Name == buttonName) {
                return spec.Buttons[i];
            }
        }

        return null;
    }

    private string ResolvePanelText(WorkshopFormalV1PanelBinding binding, string key, string fallback) {
        return WorkshopFormalV1PanelBindingService.ResolveText(binding.PanelTexts, key, fallback);
    }

    private string ResolveRowText(WorkshopFormalV1PanelBinding binding, string key, string fallback) {
        return WorkshopFormalV1PanelBindingService.ResolveText(binding.RowTexts, key, fallback);
    }

    private void CreateActionFeedback(Transform parent, PanelSpec spec, Vector2 topLeft, Vector2 size) {
        if (string.IsNullOrEmpty(_lastActionFeedback) || _lastActionScreenID != spec.ScreenID) {
            return;
        }

        CreateText(
            "ActionFeedback_Text",
            parent,
            _lastActionFeedback,
            topLeft,
            size,
            17,
            new Color(0.94f, 0.82f, 0.5f, 1f),
            TextAnchor.MiddleLeft);
    }

    private void ExecutePanelAction(string screenID, string buttonName) {
        WorkshopFormalV1PanelActionResult result = WorkshopFormalV1PanelActionService.Execute(screenID, buttonName, GameRoot.Core);
        if (result == null) {
            return;
        }

        _lastActionScreenID = string.IsNullOrEmpty(result.NextScreenID) ? screenID : result.NextScreenID;
        _lastActionFeedback = result.FeedbackText ?? string.Empty;

        if (result.Success) {
            Debug.Log($"[WorkshopFormalV1Panel] Action succeeded. Screen={screenID}, Button={buttonName}, Feedback={result.FeedbackText}");
        } else {
            Debug.LogWarning($"[WorkshopFormalV1Panel] Action failed or unsupported. Screen={screenID}, Button={buttonName}, Reason={result.Reason}");
        }

        if (result.ClosePanel) {
            Hide();
            return;
        }

        if (!string.IsNullOrEmpty(result.NextScreenID)) {
            ShowInternal(result.NextScreenID, true);
            return;
        }

        if (result.RefreshPanel) {
            ShowInternal(screenID, true);
        }
    }

    private Transform ResolveRootParent() {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null) {
            return canvas.transform;
        }

        Canvas sceneCanvas = FindObjectOfType<Canvas>();
        return sceneCanvas != null ? sceneCanvas.transform : transform;
    }

    private Image CreateImage(string name, Transform parent) {
        GameObject obj = CreateRectObject(name, parent);
        Image image = obj.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private void CreateIcon(string name, Transform parent, Vector2 topLeft, string visualID) {
        Image icon = CreateImage(name, parent);
        ConfigureTopLeftRect(icon.rectTransform, topLeft.x, topLeft.y, VisualDisplaySpecs.UIIcon.x, VisualDisplaySpecs.UIIcon.y);
        VisualUIHelper.ApplyContainSprite(icon, visualID, VisualDisplaySpecs.UIIcon, IconColor, new Color(0.82f, 0.42f, 0.18f, 1f), false);
    }

    private void CreateTitleDivider(Transform parent, Vector2 topLeft, Vector2 size) {
        Image divider = CreateImage("TitleDivider_Image", parent);
        ConfigureTopLeftRect(divider.rectTransform, topLeft.x, topLeft.y, size.x, size.y);
        VisualUIHelper.ApplySimpleSprite(divider, VisualAssetService.UITitleDividerID, new Color(0.64f, 0.78f, 0.76f, 0.28f), new Color(0.32f, 0.42f, 0.4f, 0.34f), false, false);
    }

    private void ApplyPanelFill(Image image, Color fillColor, Color outlineColor, Vector2 outlineDistance) {
        VisualUIHelper.ApplySolidColor(image, fillColor, false);
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) {
            outline = image.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = outlineColor;
        outline.effectDistance = outlineDistance;
        outline.useGraphicAlpha = true;
    }

    private Text CreateText(string name, Transform parent, string content, Vector2 topLeft, Vector2 size, int fontSize, Color color, TextAnchor alignment) {
        GameObject obj = CreateRectObject(name, parent);
        Text text = obj.AddComponent<Text>();
        text.font = _defaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(11, fontSize - 7);
        text.resizeTextMaxSize = fontSize;
        text.lineSpacing = 0.92f;
        text.raycastTarget = false;
        ConfigureTopLeftRect(text.rectTransform, topLeft.x, topLeft.y, size.x, size.y);
        return text;
    }

    private Button CreateButton(string name, string label, Transform parent, float x, float y, float width, float height, string visualID) {
        GameObject obj = CreateRectObject(name, parent);
        Image image = obj.AddComponent<Image>();
        Button button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        ConfigureTopLeftRect(obj.GetComponent<RectTransform>(), x, y, width, height);
        VisualUIHelper.ApplyButtonSkin(button, visualID, new Color(0.2f, 0.22f, 0.22f, 0.86f));
        if (image != null) {
            image.color = ResolveButtonColor(visualID);
        }
        CreateText("Text", obj.transform, label, Vector2.zero, new Vector2(width, height), 19, Color.white, TextAnchor.MiddleCenter);
        return button;
    }

    private Color ResolveButtonColor(string visualID) {
        if (visualID == VisualAssetService.UIButtonPrimaryID) {
            return new Color(0.42f, 0.58f, 0.5f, 0.82f);
        }

        if (visualID == VisualAssetService.UIButtonDangerID) {
            return new Color(0.55f, 0.28f, 0.28f, 0.78f);
        }

        return new Color(0.28f, 0.36f, 0.38f, 0.72f);
    }

    private GameObject CreateRectObject(string name, Transform parent) {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void ConfigureTopLeftRect(RectTransform rect, float x, float y, float width, float height) {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void ConfigureCenterRect(RectTransform rect, Vector2 position, Vector2 size) {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private PanelSpec ResolveSpec(string screenID) {
        switch (screenID) {
            case "maintenance_panel":
                return new PanelSpec {
                    ScreenID = "maintenance_panel",
                    RootName = "MaintenancePanel_Runtime",
                    BackgroundName = "MaintenanceBackground_Image",
                    BackgroundVisualID = "bg_workshop_studio",
                    CardName = "MaintenanceCard_Image",
                    Title = "Workshop Studio / Maintenance",
                    PrimaryIconName = "MaintenanceIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconMaintenanceID,
                    AccentColor = new Color(0.62f, 0.92f, 0.82f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("WearRepairIcon_Image", VisualAssetService.UIIconWearRepairID),
                        new IconSpec("CorruptionPurifyIcon_Image", VisualAssetService.UIIconCorruptionPurifyID),
                        new IconSpec("DivePermitIcon_Image", VisualAssetService.UIIconDivePermitID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("DollConditionPanel", 60f, 180f, 390f, 250f, "Doll condition"),
                        new NamedRect("DiveReadinessPanel", 60f, 450f, 390f, 180f, "Dive readiness"),
                        new NamedRect("MaterialCostListPanel", 490f, 180f, 390f, 450f, "Material cost"),
                        new NamedRect("WearCorrosionPanel", 920f, 180f, 380f, 270f, "Wear and corrosion"),
                        new NamedRect("RepairActionPanel", 920f, 480f, 380f, 150f, "Repair actions", true)
                    },
                    Rows = new[] {
                        new RowSpec("CostRow_Template", 520f, 290f, 330f, 58f, VisualAssetService.UIListRowNormalID, "Repair parts"),
                        new RowSpec("StatusRow_Template", 950f, 300f, 320f, 58f, VisualAssetService.UIListRowNormalID, "Warning status")
                    },
                    Buttons = new[] {
                        new ButtonSpec("FullRepair_Button", "Full Repair", 500f, 690f, 210f, 64f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("UseRepairKit_Button", "Use Kit", 730f, 690f, 190f, 64f, VisualAssetService.UIButtonSecondaryID),
                        new ButtonSpec("Postpone_Button", "Postpone", 940f, 690f, 190f, 64f, VisualAssetService.UIButtonDangerID),
                        new ButtonSpec("Close_Button", "Close", 1150f, 690f, 150f, 64f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "daily_bill_report":
                return new PanelSpec {
                    ScreenID = "daily_bill_report",
                    RootName = "DailyBillReportPanel_Runtime",
                    BackgroundName = "BillBackground_Image",
                    CardName = "BillCard_Image",
                    Title = "Daily Bill",
                    PrimaryIconName = "BillIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconBillID,
                    AccentColor = new Color(1f, 0.82f, 0.42f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("IncomeIcon_Image", VisualAssetService.UIIconIncomeID),
                        new IconSpec("ExpenseIcon_Image", VisualAssetService.UIIconExpenseID),
                        new IconSpec("DebtRentIcon_Image", VisualAssetService.UIIconDebtRentID),
                        new IconSpec("MoneyIcon_Image", VisualAssetService.UIIconMoneyID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("SummaryPanel", 60f, 180f, 390f, 220f, "Income summary"),
                        new NamedRect("PressureWarningPanel", 60f, 430f, 390f, 190f, "Pressure warning"),
                        new NamedRect("UnsoldGoodsPanel", 490f, 180f, 390f, 450f, "Unsold goods"),
                        new NamedRect("IncomeExpenseListPanel", 920f, 180f, 380f, 450f, "Income and expense"),
                        new NamedRect("ActionPanel", 490f, 660f, 810f, 120f, "Bill actions", true)
                    },
                    Rows = new[] {
                        new RowSpec("UnsoldRow_Template", 520f, 300f, 330f, 58f, VisualAssetService.UIListRowSelectedID, "Unsold gear"),
                        new RowSpec("BillRow_Template", 950f, 300f, 320f, 58f, VisualAssetService.UIListRowNormalID, "Rent due")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Continue_Button", "Continue", 520f, 690f, 200f, 64f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("ReviewSell_Button", "Review Sell", 740f, 690f, 210f, 64f, VisualAssetService.UIButtonSecondaryID),
                        new ButtonSpec("DeferPayment_Button", "Defer", 970f, 690f, 170f, 64f, VisualAssetService.UIButtonDangerID),
                        new ButtonSpec("Close_Button", "Close", 1160f, 690f, 120f, 64f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "shop_staging":
                return new PanelSpec {
                    ScreenID = "shop_staging",
                    RootName = "ShopStagingPanel_Runtime",
                    BackgroundName = "Background_Image",
                    CardName = "StagingCard_Image",
                    Title = "Shop Staging",
                    PrimaryIconName = "ShopChannelIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconShopChannelID,
                    AccentColor = new Color(0.92f, 0.78f, 0.42f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("OrderLaneIcon_Image", VisualAssetService.UIIconOrderID),
                        new IconSpec("BlackMarketLaneIcon_Image", VisualAssetService.UIIconBlackMarketID),
                        new IconSpec("MoneyIcon_Image", VisualAssetService.UIIconMoneyID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("ItemListPanel", 60f, 180f, 430f, 500f, "Items for sale"),
                        new NamedRect("ChannelPanel", 530f, 180f, 360f, 500f, "Shop channels"),
                        new NamedRect("OrderLanePanel", 930f, 180f, 370f, 220f, "Order lane"),
                        new NamedRect("BlackMarketLanePanel", 930f, 430f, 370f, 250f, "Black market"),
                        new NamedRect("ActionPanel", 60f, 710f, 1240f, 100f, "Staging actions", true)
                    },
                    Rows = new[] {
                        new RowSpec("ItemRow_Template", 90f, 310f, 370f, 58f, VisualAssetService.UIListRowNormalID, "Inventory item"),
                        new RowSpec("SelectedItemRow", 90f, 390f, 370f, 58f, VisualAssetService.UIListRowSelectedID, "Selected item")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Confirm_Button", "Confirm", 640f, 730f, 190f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Reset_Button", "Reset", 850f, 730f, 160f, 58f, VisualAssetService.UIButtonSecondaryID),
                        new ButtonSpec("BlackMarket_Button", "Black Market", 1030f, 730f, 210f, 58f, VisualAssetService.UIButtonDangerID),
                        new ButtonSpec("Close_Button", "Close", 90f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "order_board":
                return new PanelSpec {
                    ScreenID = "order_board",
                    RootName = "OrderBoardPanel_Runtime",
                    BackgroundName = "Background_Image",
                    CardName = "OrderBoardCard_Image",
                    Title = "Order Board",
                    PrimaryIconName = "OrderIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconOrderID,
                    AccentColor = new Color(0.72f, 0.9f, 1f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("FactionIcon_Image", VisualAssetService.UIIconFactionID),
                        new IconSpec("DeadlineIcon_Image", VisualAssetService.UIIconDeadlineID),
                        new IconSpec("MoneyIcon_Image", VisualAssetService.UIIconMoneyID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("OrderListPanel", 60f, 180f, 430f, 500f, "Available orders"),
                        new NamedRect("OrderDetailPanel", 530f, 180f, 420f, 500f, "Order detail"),
                        new NamedRect("RewardPreviewPanel", 990f, 180f, 310f, 260f, "Reward preview"),
                        new NamedRect("DeadlinePanel", 990f, 470f, 310f, 210f, "Deadline"),
                        new NamedRect("ActionPanel", 60f, 710f, 1240f, 100f, "Order actions", true)
                    },
                    Rows = new[] {
                        new RowSpec("OrderRow_Template", 90f, 310f, 370f, 58f, VisualAssetService.UIListRowNormalID, "Open order"),
                        new RowSpec("SelectedOrderRow_Template", 90f, 390f, 370f, 58f, VisualAssetService.UIListRowSelectedID, "Selected order")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Accept_Button", "Accept", 560f, 730f, 170f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Submit_Button", "Submit", 750f, 730f, 170f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Abandon_Button", "Abandon", 940f, 730f, 190f, 58f, VisualAssetService.UIButtonDangerID),
                        new ButtonSpec("Close_Button", "Close", 1150f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "rumor_board":
                return new PanelSpec {
                    ScreenID = "rumor_board",
                    RootName = "RumorBoardPanel_Runtime",
                    BackgroundName = "Background_Image",
                    CardName = "RumorBoardCard_Image",
                    Title = "Rumor Board",
                    PrimaryIconName = "RumorIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconRumorID,
                    AccentColor = new Color(0.85f, 0.72f, 1f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("PriceUpIcon_Image", VisualAssetService.UIIconPriceUpID),
                        new IconSpec("PriceDownIcon_Image", VisualAssetService.UIIconPriceDownID),
                        new IconSpec("MoneyIcon_Image", VisualAssetService.UIIconMoneyID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("TodayRumorListPanel", 60f, 180f, 430f, 500f, "Today's rumors"),
                        new NamedRect("PriceWavePanel", 530f, 180f, 360f, 500f, "Price waves"),
                        new NamedRect("RumorDetailPanel", 930f, 180f, 370f, 300f, "Rumor detail"),
                        new NamedRect("RecommendationPanel", 930f, 510f, 370f, 170f, "Recommended action", true),
                        new NamedRect("BottomHintPanel", 60f, 710f, 830f, 100f, "Risk notes")
                    },
                    Rows = new[] {
                        new RowSpec("RumorRow_Template", 90f, 310f, 370f, 58f, VisualAssetService.UIListRowNormalID, "Rumor"),
                        new RowSpec("SelectedRumorRow_Template", 90f, 390f, 370f, 58f, VisualAssetService.UIListRowSelectedID, "Selected rumor")
                    },
                    Buttons = new[] {
                        new ButtonSpec("PlanExpedition_Button", "Plan Expedition", 960f, 610f, 230f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Close_Button", "Close", 1210f, 610f, 110f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "business_settlement":
                return new PanelSpec {
                    ScreenID = "business_settlement",
                    RootName = "BusinessSettlementPanel_Runtime",
                    BackgroundName = "BusinessSettlementBackground_Image",
                    CardName = "BusinessSettlementCard_Image",
                    Title = "Business Settlement",
                    PrimaryIconName = "BusinessSettlementIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconBusinessSettlementID,
                    AccentColor = new Color(1f, 0.78f, 0.42f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("CustomerIcon_Image", VisualAssetService.UIIconCustomerID),
                        new IconSpec("SaleSparkIcon_Image", VisualAssetService.UIIconSaleSparkID),
                        new IconSpec("MoneyIcon_Image", VisualAssetService.UIIconMoneyID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("CustomerFlowPanel", 60f, 180f, 380f, 250f, "Customer flow"),
                        new NamedRect("SaleHighlightPanel", 60f, 460f, 380f, 220f, "Sale highlights"),
                        new NamedRect("RevenuePanel", 480f, 180f, 390f, 500f, "Revenue summary"),
                        new NamedRect("RiskSummaryPanel", 910f, 180f, 390f, 300f, "Unsold and risk summary"),
                        new NamedRect("NextBillPanel", 910f, 510f, 390f, 170f, "Proceed to daily bill", true)
                    },
                    Rows = new[] {
                        new RowSpec("CustomerRow_Template", 90f, 310f, 320f, 58f, VisualAssetService.UIListRowNormalID, "Workshop visitor"),
                        new RowSpec("SaleRow_Template", 510f, 310f, 330f, 58f, VisualAssetService.UIListRowSelectedID, "Sold item")
                    },
                    Buttons = new[] {
                        new ButtonSpec("ContinueToBill_Button", "Continue to Bill", 930f, 610f, 250f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("ReviewRisk_Button", "Review Risk", 1200f, 610f, 150f, 58f, VisualAssetService.UIButtonDangerID),
                        new ButtonSpec("Close_Button", "Close", 90f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "chassis_upgrade_panel":
                return new PanelSpec {
                    ScreenID = "chassis_upgrade_panel",
                    RootName = "ChassisUpgradePanel_Runtime",
                    BackgroundName = "ChassisUpgradeBackground_Image",
                    BackgroundVisualID = "bg_workshop_studio",
                    CardName = "ChassisUpgradeCard_Image",
                    Title = "Workshop Studio / Chassis",
                    PrimaryIconName = "ChassisUpgradeIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconChassisUpgradeID,
                    AccentColor = new Color(0.72f, 0.9f, 1f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("BlueprintIcon_Image", VisualAssetService.UIIconBlueprintID),
                        new IconSpec("MaterialNeedIcon_Image", VisualAssetService.UIIconMaterialNeedID),
                        new IconSpec("MoneyIcon_Image", VisualAssetService.UIIconMoneyID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("CurrentChassisPanel", 60f, 180f, 360f, 500f, "Current chassis"),
                        new NamedRect("NextChassisPanel", 460f, 180f, 360f, 500f, "Next chassis"),
                        new NamedRect("CapacityDeltaPanel", 860f, 180f, 440f, 180f, "Capacity change"),
                        new NamedRect("MaterialNeedPanel", 860f, 390f, 440f, 190f, "Material needs"),
                        new NamedRect("UpgradeActionPanel", 860f, 610f, 440f, 100f, "Upgrade action", true)
                    },
                    Rows = new[] {
                        new RowSpec("CurrentChassisRow", 90f, 310f, 300f, 58f, VisualAssetService.UIListRowNormalID, "Current grid"),
                        new RowSpec("NextChassisRow", 490f, 310f, 300f, 58f, VisualAssetService.UIListRowSelectedID, "Upgrade target"),
                        new RowSpec("MaterialNeedRow_Template", 890f, 500f, 380f, 58f, VisualAssetService.UIListRowNormalID, "Missing material")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Upgrade_Button", "Upgrade", 890f, 630f, 180f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Blueprint_Button", "Blueprint", 1090f, 630f, 180f, 58f, VisualAssetService.UIButtonSecondaryID),
                        new ButtonSpec("Close_Button", "Close", 90f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "doll_interaction":
                return new PanelSpec {
                    ScreenID = "doll_interaction",
                    RootName = "DollInteractionPanel_Runtime",
                    BackgroundName = "DollInteractionBackground_Image",
                    BackgroundVisualID = VisualAssetService.DollRoomAtticBackgroundID,
                    CardName = "DollInteractionCard_Image",
                    Title = "Doll Interaction",
                    PrimaryIconName = "TouchIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconTouchID,
                    AccentColor = new Color(1f, 0.74f, 0.86f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("TalkIcon_Image", VisualAssetService.UIIconTalkID),
                        new IconSpec("GiftIcon_Image", VisualAssetService.UIIconGiftID),
                        new IconSpec("MementoIcon_Image", VisualAssetService.UIIconMementoID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("DollStagePanel", 60f, 180f, 410f, 500f, "Doll stage"),
                        new NamedRect("InteractionMenuPanel", 510f, 180f, 350f, 500f, "Interaction menu"),
                        new NamedRect("GiftTopicListPanel", 900f, 180f, 400f, 280f, "Gift and topic list"),
                        new NamedRect("FeedbackPanel", 900f, 490f, 400f, 190f, "Interaction feedback", true)
                    },
                    Rows = new[] {
                        new RowSpec("TalkOptionRow_Template", 540f, 310f, 290f, 58f, VisualAssetService.UIListRowNormalID, "Talk"),
                        new RowSpec("GiftOptionRow_Template", 540f, 390f, 290f, 58f, VisualAssetService.UIListRowSelectedID, "Gift")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Touch_Button", "Touch", 930f, 610f, 150f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Talk_Button", "Talk", 1100f, 610f, 150f, 58f, VisualAssetService.UIButtonSecondaryID),
                        new ButtonSpec("Close_Button", "Close", 90f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "doll_room":
                return new PanelSpec {
                    ScreenID = "doll_room",
                    RootName = "DollRoomPanel_Runtime",
                    BackgroundName = "DollRoomBackground_Image",
                    BackgroundVisualID = VisualAssetService.DollRoomAtticBackgroundID,
                    CardName = "DollRoomCard_Image",
                    Title = "Doll Room",
                    PrimaryIconName = "DiaryIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconDiaryID,
                    AccentColor = new Color(0.86f, 0.78f, 1f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("MementoIcon_Image", VisualAssetService.UIIconMementoID),
                        new IconSpec("MementoSlotIcon_Image", VisualAssetService.UIRoomMementoSlotID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("RoomStagePanel", 60f, 180f, 520f, 500f, "Room stage"),
                        new NamedRect("MementoDisplayPanel", 620f, 180f, 310f, 500f, "Memento display"),
                        new NamedRect("DiaryPanel", 970f, 180f, 330f, 260f, "Diary"),
                        new NamedRect("ObservationPanel", 970f, 470f, 330f, 210f, "Observation", true)
                    },
                    Rows = new[] {
                        new RowSpec("MementoSlotRow_0", 650f, 310f, 250f, 58f, VisualAssetService.UIListRowNormalID, "Memento slot"),
                        new RowSpec("DiaryRow_Template", 1000f, 300f, 270f, 58f, VisualAssetService.UIListRowSelectedID, "Diary entry")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Observe_Button", "Observe", 1000f, 610f, 170f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Close_Button", "Close", 1190f, 610f, 110f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "faction_shop":
                return new PanelSpec {
                    ScreenID = "faction_shop",
                    RootName = "FactionShopPanel_Runtime",
                    BackgroundName = "FactionShopBackground_Image",
                    CardName = "FactionShopCard_Image",
                    Title = "Faction Shop",
                    PrimaryIconName = "FactionIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconFactionID,
                    AccentColor = new Color(0.74f, 0.9f, 1f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("ReputationIcon_Image", VisualAssetService.UIIconReputationID),
                        new IconSpec("TrustIcon_Image", VisualAssetService.UIIconTrustID),
                        new IconSpec("BlackMarketIcon_Image", VisualAssetService.UIIconBlackMarketID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("FactionListPanel", 60f, 180f, 350f, 500f, "Factions"),
                        new NamedRect("StandingPanel", 450f, 180f, 350f, 500f, "Reputation and trust"),
                        new NamedRect("GoodsListPanel", 840f, 180f, 280f, 500f, "Goods"),
                        new NamedRect("SelectedGoodsPanel", 1160f, 180f, 140f, 500f, "Detail", true)
                    },
                    Rows = new[] {
                        new RowSpec("FactionRow_Template", 90f, 310f, 290f, 58f, VisualAssetService.UIListRowNormalID, "Faction"),
                        new RowSpec("GoodsRow_Template", 870f, 310f, 220f, 58f, VisualAssetService.UIListRowSelectedID, "Locked goods")
                    },
                    Buttons = new[] {
                        new ButtonSpec("Buy_Button", "Buy", 900f, 610f, 150f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("BlackMarket_Button", "Black Market", 1070f, 610f, 210f, 58f, VisualAssetService.UIButtonDangerID),
                        new ButtonSpec("Close_Button", "Close", 90f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            case "scenario_event":
                return new PanelSpec {
                    ScreenID = "scenario_event",
                    RootName = "ScenarioEventPanel_Runtime",
                    BackgroundName = "ScenarioEventBackground_Image",
                    CardName = "ScenarioEventCard_Image",
                    Title = "Scenario Event",
                    PrimaryIconName = "EventIcon_Image",
                    PrimaryIconVisualID = VisualAssetService.UIIconEventID,
                    AccentColor = new Color(0.96f, 0.82f, 0.58f, 1f),
                    HeaderIcons = new[] {
                        new IconSpec("LoreIcon_Image", VisualAssetService.UIIconLoreID),
                        new IconSpec("SkipIcon_Image", VisualAssetService.UIIconSkipID),
                        new IconSpec("WarningIcon_Image", VisualAssetService.UIIconWarningID)
                    },
                    InfoPanels = new[] {
                        new NamedRect("SpeakerPanel", 60f, 180f, 360f, 180f, "Speaker"),
                        new NamedRect("EventTextPanel", 460f, 180f, 840f, 300f, "Event text"),
                        new NamedRect("ChoiceListPanel", 60f, 400f, 360f, 280f, "Choices"),
                        new NamedRect("LorePanel", 460f, 510f, 400f, 170f, "Lore"),
                        new NamedRect("ActionSummaryPanel", 900f, 510f, 400f, 170f, "Action summary", true)
                    },
                    Rows = new[] {
                        new RowSpec("ChoiceRow_Template", 90f, 500f, 300f, 58f, VisualAssetService.UIListRowNormalID, "Choice"),
                        new RowSpec("SelectedChoiceRow_Template", 90f, 580f, 300f, 58f, VisualAssetService.UIListRowSelectedID, "Selected choice")
                    },
                    Buttons = new[] {
                        new ButtonSpec("ConfirmChoice_Button", "Confirm", 930f, 610f, 170f, 58f, VisualAssetService.UIButtonPrimaryID),
                        new ButtonSpec("Skip_Button", "Skip", 1120f, 610f, 150f, 58f, VisualAssetService.UIButtonSecondaryID),
                        new ButtonSpec("Close_Button", "Close", 90f, 730f, 140f, 58f, VisualAssetService.UIButtonSecondaryID, true)
                    }
                };
            default:
                return null;
        }
    }

    private sealed class PanelSpec {
        public string ScreenID;
        public string RootName;
        public string BackgroundName;
        public string BackgroundVisualID;
        public string CardName;
        public string Title;
        public string PrimaryIconName;
        public string PrimaryIconVisualID;
        public Color AccentColor;
        public IconSpec[] HeaderIcons = new IconSpec[0];
        public NamedRect[] InfoPanels = new NamedRect[0];
        public RowSpec[] Rows = new RowSpec[0];
        public ButtonSpec[] Buttons = new ButtonSpec[0];
    }

    private sealed class IconSpec {
        public readonly string Name;
        public readonly string VisualID;

        public IconSpec(string name, string visualID) {
            Name = name;
            VisualID = visualID;
        }
    }

    private class NamedRect {
        public readonly string Name;
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;
        public readonly string Label;
        public readonly bool UseMainPanel;

        public NamedRect(string name, float x, float y, float width, float height, string label, bool useMainPanel = false) {
            Name = name;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Label = label;
            UseMainPanel = useMainPanel;
        }
    }

    private sealed class RowSpec : NamedRect {
        public readonly string VisualID;

        public RowSpec(string name, float x, float y, float width, float height, string visualID, string label)
            : base(name, x, y, width, height, label) {
            VisualID = visualID;
        }
    }

    private sealed class ButtonSpec : NamedRect {
        public readonly string VisualID;
        public readonly bool ClosesPanel;

        public ButtonSpec(string name, string label, float x, float y, float width, float height, string visualID, bool closesPanel = false)
            : base(name, x, y, width, height, label) {
            VisualID = visualID;
            ClosesPanel = closesPanel;
        }
    }
}
