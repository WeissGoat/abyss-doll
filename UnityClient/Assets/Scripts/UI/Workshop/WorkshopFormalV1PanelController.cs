using UnityEngine;
using UnityEngine.UI;

public class WorkshopFormalV1PanelController : MonoBehaviour {
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
        CurrentScreenID = string.Empty;
        if (_rootPanel == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(_rootPanel);
        } else {
            DestroyImmediate(_rootPanel);
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
        VisualUIHelper.ApplySolidColor(backdrop, new Color(0.012f, 0.014f, 0.013f, 1f), true);

        Image background = CreateImage(spec.BackgroundName, _rootPanel.transform);
        Stretch(background.rectTransform);
        VisualUIHelper.ApplyCoverSprite(
            background,
            string.IsNullOrEmpty(spec.BackgroundVisualID) ? VisualAssetService.WorkshopBackgroundID : spec.BackgroundVisualID,
            Color.white,
            new Color(0.08f, 0.07f, 0.055f, 0.94f));

        Image card = CreateImage(spec.CardName, _rootPanel.transform);
        ConfigureCenterRect(card.rectTransform, Vector2.zero, new Vector2(1360f, 900f));
        VisualUIHelper.ApplySlicedSprite(card, VisualAssetService.UIPanelMainID, Color.white, new Color(0.08f, 0.075f, 0.065f, 0.98f), false);

        Image header = CreateImage("HeaderPanel", card.transform);
        ConfigureTopLeftRect(header.rectTransform, 60f, 40f, 1240f, 110f);
        VisualUIHelper.ApplySlicedSprite(header, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.08f, 0.085f, 0.08f, 0.92f), false);

        CreateIcon(spec.PrimaryIconName, header.transform, new Vector2(32f, 23f), spec.PrimaryIconVisualID);
        CreateText("Title_Text", header.transform, spec.Title, new Vector2(112f, 24f), new Vector2(560f, 60f), 38, spec.AccentColor, TextAnchor.MiddleLeft);
        CreateTitleDivider(header.transform, new Vector2(680f, 41f), new Vector2(500f, 28f));

        float iconX = 700f;
        for (int i = 0; i < spec.HeaderIcons.Length; i++) {
            IconSpec icon = spec.HeaderIcons[i];
            CreateIcon(icon.Name, header.transform, new Vector2(iconX + i * 78f, 25f), icon.VisualID);
        }

        WorkshopFormalV1PanelBinding binding = WorkshopFormalV1PanelBindingService.Build(spec.ScreenID, GameRoot.Core?.CurrentPlayer);
        BuildContentPanels(card.transform, spec, binding);
        BuildRows(card.transform, spec, binding);
        BuildButtons(card.transform, spec);

        CanvasGroup group = _rootPanel.GetComponent<CanvasGroup>();
        if (group == null) {
            group = _rootPanel.AddComponent<CanvasGroup>();
        }
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;

        _rootPanel.transform.SetAsLastSibling();
    }

    private void BuildContentPanels(Transform card, PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        for (int i = 0; i < spec.InfoPanels.Length; i++) {
            NamedRect panel = spec.InfoPanels[i];
            Image image = CreateImage(panel.Name, card);
            ConfigureTopLeftRect(image.rectTransform, panel.X, panel.Y, panel.Width, panel.Height);
            string visualID = panel.UseMainPanel ? VisualAssetService.UIPanelMainID : VisualAssetService.UIPanelInfoID;
            VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, new Color(0.07f, 0.075f, 0.07f, 0.9f), false);
            string panelText = WorkshopFormalV1PanelBindingService.ResolveText(binding.PanelTexts, panel.Name, panel.Label);
            CreateText(
                $"{panel.Name}_Text",
                image.transform,
                panelText,
                new Vector2(18f, 14f),
                new Vector2(Mathf.Max(120f, panel.Width - 36f), Mathf.Max(40f, panel.Height - 28f)),
                22,
                new Color(0.86f, 0.86f, 0.8f, 1f),
                TextAnchor.UpperLeft);
        }
    }

    private void BuildRows(Transform card, PanelSpec spec, WorkshopFormalV1PanelBinding binding) {
        for (int i = 0; i < spec.Rows.Length; i++) {
            RowSpec row = spec.Rows[i];
            Image rowImage = CreateImage(row.Name, card);
            ConfigureTopLeftRect(rowImage.rectTransform, row.X, row.Y, row.Width, row.Height);
            VisualUIHelper.ApplySlicedSprite(rowImage, row.VisualID, Color.white, new Color(0.1f, 0.105f, 0.095f, 0.94f), false);
            string rowText = WorkshopFormalV1PanelBindingService.ResolveText(binding.RowTexts, row.Name, row.Label);
            CreateText(
                $"{row.Name}_Text",
                rowImage.transform,
                rowText,
                new Vector2(16f, 8f),
                new Vector2(Mathf.Max(100f, row.Width - 32f), Mathf.Max(30f, row.Height - 16f)),
                20,
                Color.white,
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
                20,
                new Color(0.94f, 0.82f, 0.5f, 1f),
                TextAnchor.MiddleLeft);
        }
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
        VisualUIHelper.ApplyContainSprite(icon, visualID, VisualDisplaySpecs.UIIcon, Color.white, new Color(0.82f, 0.42f, 0.18f, 1f), false);
    }

    private void CreateTitleDivider(Transform parent, Vector2 topLeft, Vector2 size) {
        Image divider = CreateImage("TitleDivider_Image", parent);
        ConfigureTopLeftRect(divider.rectTransform, topLeft.x, topLeft.y, size.x, size.y);
        VisualUIHelper.ApplySimpleSprite(divider, VisualAssetService.UITitleDividerID, Color.white, new Color(0.7f, 0.56f, 0.32f, 1f), false, false);
    }

    private Text CreateText(string name, Transform parent, string content, Vector2 topLeft, Vector2 size, int fontSize, Color color, TextAnchor alignment) {
        GameObject obj = CreateRectObject(name, parent);
        Text text = obj.AddComponent<Text>();
        text.font = _defaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
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
        VisualUIHelper.ApplyButtonSkin(button, visualID, new Color(0.34f, 0.3f, 0.24f, 1f));
        CreateText("Text", obj.transform, label, Vector2.zero, new Vector2(width, height), 24, Color.white, TextAnchor.MiddleCenter);
        return button;
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
                    CardName = "MaintenanceCard_Image",
                    Title = "Maintenance",
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
                    CardName = "ChassisUpgradeCard_Image",
                    Title = "Chassis Upgrade",
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
