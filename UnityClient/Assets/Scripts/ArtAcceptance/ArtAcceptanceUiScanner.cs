using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArtAcceptanceRunner 的 UI 层级扫描与风险评估功能。
/// 包含 UI 层级快照构建、元素属性采集、风险标记、必需元素检查等。
/// </summary>
public partial class ArtAcceptanceRunner {

    private ArtAcceptanceUiCaptureSnapshot BuildUiCaptureSnapshot(string screenTag) {
        ArtAcceptanceUiCaptureSnapshot capture = new ArtAcceptanceUiCaptureSnapshot {
            ScreenTag = screenTag
        };

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases) {
            if (canvas == null || !canvas.gameObject.activeInHierarchy) {
                continue;
            }

            ArtAcceptanceCanvasSnapshot canvasSnapshot = new ArtAcceptanceCanvasSnapshot {
                Name = canvas.name,
                Path = BuildTransformPath(canvas.transform),
                RenderMode = canvas.renderMode.ToString(),
                SortingOrder = canvas.sortingOrder
            };

            RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(false);
            foreach (RectTransform rect in rects) {
                if (rect == null) {
                    continue;
                }

                if (!IsElementVisibleForAcceptance(rect)) {
                    continue;
                }

                ArtAcceptanceUiElementSnapshot element = BuildUiElementSnapshot(canvas.transform, rect);
                canvasSnapshot.Elements.Add(element);
            }

            capture.Canvases.Add(canvasSnapshot);
        }

        return capture;
    }

    private ArtAcceptanceUiElementSnapshot BuildUiElementSnapshot(Transform canvasTransform, RectTransform rect) {
        ArtAcceptanceUiElementSnapshot element = new ArtAcceptanceUiElementSnapshot {
            Name = rect.name,
            Path = BuildRelativePath(canvasTransform, rect.transform),
            Active = rect.gameObject.activeInHierarchy,
            AnchoredPosition = FormatVector2(rect.anchoredPosition),
            SizeDelta = FormatVector2(rect.sizeDelta),
            AnchorMin = FormatVector2(rect.anchorMin),
            AnchorMax = FormatVector2(rect.anchorMax),
            Pivot = FormatVector2(rect.pivot)
        };

        Component[] components = rect.GetComponents<Component>();
        foreach (Component component in components) {
            element.ComponentTypes.Add(component != null ? component.GetType().Name : "MissingScript");
        }

        Image image = rect.GetComponent<Image>();
        if (image != null) {
            element.Image = new ArtAcceptanceImageSnapshot {
                Found = true,
                Enabled = image.enabled,
                SpriteName = image.sprite != null ? image.sprite.name : string.Empty,
                Type = image.type.ToString(),
                RaycastTarget = image.raycastTarget,
                PreserveAspect = image.preserveAspect
            };
        }

        Button button = rect.GetComponent<Button>();
        if (button != null) {
            element.Button = new ArtAcceptanceButtonSnapshot {
                Found = true,
                Interactable = button.interactable,
                HasTargetGraphic = button.targetGraphic != null
            };
        }

        Text text = rect.GetComponent<Text>();
        if (text != null) {
            element.Text = new ArtAcceptanceTextSnapshot {
                Found = true,
                Enabled = text.enabled,
                TextLength = string.IsNullOrEmpty(text.text) ? 0 : text.text.Length,
                RaycastTarget = text.raycastTarget,
                FontSize = text.fontSize
            };
        }

        CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
        if (canvasGroup != null) {
            element.CanvasGroup = new ArtAcceptanceCanvasGroupSnapshot {
                Found = true,
                Alpha = canvasGroup.alpha,
                Interactable = canvasGroup.interactable,
                BlocksRaycasts = canvasGroup.blocksRaycasts
            };
        }

        element.Risks = EvaluateElementRisks(rect, element);
        return element;
    }

    private List<string> EvaluateElementRisks(RectTransform rect, ArtAcceptanceUiElementSnapshot element) {
        List<string> risks = new List<string>();

        if (!IsElementVisibleForAcceptance(rect)) {
            return risks;
        }

        Image imageComponent = rect.GetComponent<Image>();
        bool imageVisible = element.Image != null &&
            element.Image.Found &&
            imageComponent != null &&
            IsGraphicVisibleForAcceptance(imageComponent);

        if (imageVisible) {
            if (string.IsNullOrEmpty(element.Image.SpriteName)) {
                risks.Add("ImageEnabledButSpriteEmpty");
            }

            if (element.Image.SpriteName == VisualAssetService.MissingSpriteVisualID ||
                element.Image.SpriteName.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0) {
                risks.Add("MissingSpriteVisible");
            }

            if (element.Image.RaycastTarget &&
                IsElementRaycastRelevant(rect) &&
                rect.sizeDelta.x >= 500f &&
                rect.sizeDelta.y >= 300f &&
                !element.Button.Found) {
                risks.Add("LargeNonButtonImageBlocksRaycasts");
            }

            if (RequiresSlicedSprite(element.Image.SpriteName) && element.Image.Type != Image.Type.Sliced.ToString()) {
                risks.Add("ExpectedSlicedImageButTypeIsNotSliced");
            }
        }

        if (element.Button != null && element.Button.Found) {
            if (!element.Button.HasTargetGraphic) {
                risks.Add("ButtonMissingTargetGraphic");
            }

            if (element.Image == null || !element.Image.Found || string.IsNullOrEmpty(element.Image.SpriteName)) {
                risks.Add("ButtonMissingImageSprite");
            }
        }

        GridSlotUI slot = rect.GetComponent<GridSlotUI>();
        if (slot != null && (Mathf.Abs(rect.sizeDelta.x - InventoryDisplaySpec.CellSize) > 0.1f || Mathf.Abs(rect.sizeDelta.y - InventoryDisplaySpec.CellSize) > 0.1f)) {
            risks.Add($"InventorySlotSizeMismatch:expected={InventoryDisplaySpec.CellSize}x{InventoryDisplaySpec.CellSize},actual={FormatVector2(rect.sizeDelta)}");
        }

        return risks;
    }

    private bool IsElementVisibleForAcceptance(RectTransform rect) {
        if (rect == null || !rect.gameObject.activeInHierarchy) {
            return false;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.enabled) {
            return false;
        }

        CanvasGroup[] groups = rect.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup group in groups) {
            if (group != null && group.alpha <= VisibleAlphaThreshold) {
                return false;
            }
        }

        return true;
    }

    private bool IsGraphicVisibleForAcceptance(Graphic graphic) {
        if (graphic == null || !graphic.enabled) {
            return false;
        }

        if (graphic.color.a <= VisibleAlphaThreshold) {
            return false;
        }

        return graphic.canvasRenderer == null || graphic.canvasRenderer.GetAlpha() > VisibleAlphaThreshold;
    }

    private bool IsElementRaycastRelevant(RectTransform rect) {
        CanvasGroup[] groups = rect.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup group in groups) {
            if (group != null && !group.blocksRaycasts) {
                return false;
            }
        }

        return true;
    }

    private bool RequiresSlicedSprite(string spriteName) {
        if (string.IsNullOrEmpty(spriteName)) {
            return false;
        }

        return spriteName.StartsWith("ui_button_", StringComparison.OrdinalIgnoreCase)
            || spriteName.StartsWith("ui_panel_", StringComparison.OrdinalIgnoreCase)
            || spriteName.StartsWith("ui_list_row_", StringComparison.OrdinalIgnoreCase)
            || spriteName == VisualAssetService.UIPanelMainID
            || spriteName == VisualAssetService.UILootPickupPanelID
            || spriteName == VisualAssetService.UILootDropZoneID
            || spriteName == VisualAssetService.UISettlementVictoryPanelID
            || spriteName == VisualAssetService.UISettlementDefeatPanelID
            || spriteName == VisualAssetService.UICombatEnemyCardID
            || spriteName == VisualAssetService.UICombatEnemyCardSelectedID
            || spriteName == VisualAssetService.UICombatStatusBarHpID
            || spriteName == VisualAssetService.UICombatStatusBarShieldID
            || spriteName == VisualAssetService.UICombatTurnBannerID;
    }

    private void ApplyRequiredUiChecksToCapture(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture) {
        if (uiCapture == null || capture == null) {
            return;
        }

        switch (uiCapture.ScreenTag) {
            case "maintenance_panel":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "MaintenancePanel_Runtime", "MaintenanceCard_Image", "MaintenanceIcon_Image");
                RequireVisibleElement(uiCapture, capture, "WearRepairIcon_Image");
                RequireVisibleElement(uiCapture, capture, "CorruptionPurifyIcon_Image");
                RequireVisibleElement(uiCapture, capture, "DivePermitIcon_Image");
                RequireVisibleElement(uiCapture, capture, "WarningIcon_Image");
                RequireVisibleElement(uiCapture, capture, "FullRepair_Button");
                RequireVisibleElement(uiCapture, capture, "Postpone_Button");
                break;
            case "daily_bill_report":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "DailyBillReportPanel_Runtime", "BillCard_Image", "BillIcon_Image");
                RequireVisibleElement(uiCapture, capture, "IncomeIcon_Image");
                RequireVisibleElement(uiCapture, capture, "ExpenseIcon_Image");
                RequireVisibleElement(uiCapture, capture, "DebtRentIcon_Image");
                RequireVisibleElement(uiCapture, capture, "MoneyIcon_Image");
                RequireVisibleElement(uiCapture, capture, "WarningIcon_Image");
                RequireVisibleElement(uiCapture, capture, "DeferPayment_Button");
                break;
            case "shop_staging":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "ShopStagingPanel_Runtime", "StagingCard_Image", "ShopChannelIcon_Image");
                RequireVisibleElement(uiCapture, capture, "BlackMarketLaneIcon_Image");
                RequireVisibleElement(uiCapture, capture, "OrderLaneIcon_Image");
                RequireVisibleElement(uiCapture, capture, "BlackMarket_Button");
                break;
            case "order_board":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "OrderBoardPanel_Runtime", "OrderBoardCard_Image", "OrderIcon_Image");
                RequireVisibleElement(uiCapture, capture, "FactionIcon_Image");
                RequireVisibleElement(uiCapture, capture, "DeadlineIcon_Image");
                RequireVisibleElement(uiCapture, capture, "Abandon_Button");
                break;
            case "rumor_board":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "RumorBoardPanel_Runtime", "RumorBoardCard_Image", "RumorIcon_Image");
                RequireVisibleElement(uiCapture, capture, "PriceUpIcon_Image");
                RequireVisibleElement(uiCapture, capture, "PriceDownIcon_Image");
                RequireVisibleElement(uiCapture, capture, "WarningIcon_Image");
                break;
            case "business_settlement":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "BusinessSettlementPanel_Runtime", "BusinessSettlementCard_Image", "BusinessSettlementIcon_Image");
                RequireVisibleElement(uiCapture, capture, "CustomerIcon_Image");
                RequireVisibleElement(uiCapture, capture, "SaleSparkIcon_Image");
                RequireVisibleElement(uiCapture, capture, "ContinueToBill_Button");
                RequireVisibleElement(uiCapture, capture, "ReviewRisk_Button");
                break;
            case "chassis_upgrade_panel":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "ChassisUpgradePanel_Runtime", "ChassisUpgradeCard_Image", "ChassisUpgradeIcon_Image");
                RequireVisibleElement(uiCapture, capture, "BlueprintIcon_Image");
                RequireVisibleElement(uiCapture, capture, "MaterialNeedIcon_Image");
                RequireVisibleElement(uiCapture, capture, "Upgrade_Button");
                RequireVisibleElement(uiCapture, capture, "Blueprint_Button");
                break;
            case "doll_interaction":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "DollInteractionPanel_Runtime", "DollInteractionCard_Image", "TouchIcon_Image");
                RequireVisibleElement(uiCapture, capture, "TalkIcon_Image");
                RequireVisibleElement(uiCapture, capture, "GiftIcon_Image");
                RequireVisibleElement(uiCapture, capture, "MementoIcon_Image");
                RequireVisibleElement(uiCapture, capture, "Touch_Button");
                break;
            case "doll_room":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "DollRoomPanel_Runtime", "DollRoomCard_Image", "DiaryIcon_Image");
                RequireVisibleElement(uiCapture, capture, "DollRoomBackground_Image");
                RequireVisibleElement(uiCapture, capture, "MementoIcon_Image");
                RequireVisibleElement(uiCapture, capture, "MementoSlotIcon_Image");
                RequireVisibleElement(uiCapture, capture, "Observe_Button");
                break;
            case "faction_shop":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "FactionShopPanel_Runtime", "FactionShopCard_Image", "FactionIcon_Image");
                RequireVisibleElement(uiCapture, capture, "ReputationIcon_Image");
                RequireVisibleElement(uiCapture, capture, "TrustIcon_Image");
                RequireVisibleElement(uiCapture, capture, "BlackMarketIcon_Image");
                RequireVisibleElement(uiCapture, capture, "Buy_Button");
                break;
            case "scenario_event":
                RequireWorkshopFormalV1Panel(uiCapture, capture, "ScenarioEventPanel_Runtime", "ScenarioEventCard_Image", "EventIcon_Image");
                RequireVisibleElement(uiCapture, capture, "LoreIcon_Image");
                RequireVisibleElement(uiCapture, capture, "SkipIcon_Image");
                RequireVisibleElement(uiCapture, capture, "ConfirmChoice_Button");
                RequireVisibleElement(uiCapture, capture, "Skip_Button");
                break;
            case "combat_hud":
                RequireActiveController(capture, nameof(HUDController));
                RequireVisibleElement(uiCapture, capture, "CombatBackground_Image");
                RequireActiveElement(uiCapture, capture, "PlayerStageRoot");
                RequireVisibleElement(uiCapture, capture, "PlayerShadow_Image");
                RequireVisibleElement(uiCapture, capture, "PlayerDoll_Image");
                RequireActiveElement(uiCapture, capture, "EnemyStageRoot");
                RequireActiveElement(uiCapture, capture, "EnemySlot_0");
                RequireVisibleElement(uiCapture, capture, "EnemySprite_Image");
                RequireVisibleElement(uiCapture, capture, "EnemyShadow_Image");
                RequireVisibleElement(uiCapture, capture, "EnemyTargetRing_Image");
                RequireVisibleElement(uiCapture, capture, "EnemyFootHpBar");
                RequireVisibleElement(uiCapture, capture, "TargetHintPanel");
                RequireVisibleElement(uiCapture, capture, "ActionStrip");
                RequireVisibleElement(uiCapture, capture, "PlayerStatusCluster");
                RequireVisibleElement(uiCapture, capture, "InventoryChassisPanel");
                RequireVisibleTextCount(uiCapture, capture, 6);
                break;
            case "inventory_loot":
                RequireActiveController(capture, nameof(CombatLootUIController));
                RequireVisibleElement(uiCapture, capture, "CombatLootPanel_Runtime");
                RequireVisibleElement(uiCapture, capture, "PickupPanel");
                RequireVisibleElement(uiCapture, capture, "LootDropZone");
                RequireVisibleElement(uiCapture, capture, "Continue_Button");
                RequireVisibleTextCount(uiCapture, capture, 2);
                break;
            case "settlement":
                RequireActiveController(capture, nameof(SettlementUIController));
                RequireVisibleElement(uiCapture, capture, "SettlementPanel_Runtime");
                RequireVisibleElement(uiCapture, capture, "Title_Text");
                RequireVisibleElement(uiCapture, capture, "Summary_Text");
                RequireVisibleElement(uiCapture, capture, "Loot_Text");
                RequireVisibleElement(uiCapture, capture, "Continue_Button");
                RequireVisibleTextCount(uiCapture, capture, 4);
                break;
        }
    }

    private void RequireActiveController(ArtAcceptanceCaptureRecord capture, string controllerName) {
        if (capture == null || string.IsNullOrEmpty(controllerName)) {
            return;
        }

        if (capture.ActiveControllers == null || !capture.ActiveControllers.Contains(controllerName)) {
            AddCaptureError(capture, $"Required active controller missing: {controllerName}.");
        }
    }

    private void RequireWorkshopFormalV1Panel(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture, string rootName, string cardName, string primaryIconName) {
        RequireActiveController(capture, nameof(WorkshopFormalV1PanelController));
        RequireVisibleElement(uiCapture, capture, rootName);
        RequireVisibleElement(uiCapture, capture, cardName);
        RequireVisibleElement(uiCapture, capture, "HeaderPanel");
        RequireVisibleElement(uiCapture, capture, primaryIconName);
        RequireVisibleElement(uiCapture, capture, "TitleDivider_Image");
        RequireVisibleElement(uiCapture, capture, "Close_Button");
        RequireVisibleTextCount(uiCapture, capture, 6);
    }

    private void RequireVisibleElement(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture, string elementName) {
        if (uiCapture == null || string.IsNullOrEmpty(elementName)) {
            return;
        }

        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                if (element.Name == elementName && IsSnapshotElementVisible(element)) {
                    return;
                }
            }
        }

        AddCaptureError(capture, $"Required visible UI element missing: {elementName}.");
    }

    private void RequireActiveElement(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture, string elementName) {
        if (uiCapture == null || string.IsNullOrEmpty(elementName)) {
            return;
        }

        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                if (element.Name == elementName && element.Active) {
                    return;
                }
            }
        }

        AddCaptureError(capture, $"Required active UI element missing: {elementName}.");
    }

    private void RequireVisibleTextCount(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture, int minCount) {
        if (uiCapture == null) {
            return;
        }

        int count = 0;
        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                if (element.Text != null &&
                    element.Text.Found &&
                    element.Text.Enabled &&
                    element.Text.TextLength > 0 &&
                    IsSnapshotElementVisible(element)) {
                    count++;
                }
            }
        }

        if (count < minCount) {
            AddCaptureError(capture, $"Visible text count below requirement: actual={count}, required={minCount}.");
        }
    }

    private bool IsSnapshotElementVisible(ArtAcceptanceUiElementSnapshot element) {
        if (element == null || !element.Active) {
            return false;
        }

        return element.CanvasGroup == null || !element.CanvasGroup.Found || element.CanvasGroup.Alpha > VisibleAlphaThreshold;
    }

    private void ApplyUiRisksToCapture(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture) {
        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                foreach (string risk in element.Risks) {
                    string message = $"{uiCapture.ScreenTag}:{element.Path}:{risk}";
                    if (!capture.Warnings.Contains(message)) {
                        capture.Warnings.Add(message);
                    }

                    if (risk == "MissingSpriteVisible") {
                        AddWarning(message);
                    }
                }
            }
        }
    }
}
