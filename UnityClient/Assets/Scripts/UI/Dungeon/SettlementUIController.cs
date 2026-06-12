using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class SettlementUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Text lootText;
    public Button continueBtn;
    public Image backgroundImage;
    public Image backdropImage;
    public Image settlementPanelImage;
    public Image titleDividerImage;

    private Image _pickedColumnImage;
    private Image _broughtColumnImage;
    private Image _lostColumnImage;
    private Text _pickedColumnText;
    private Text _broughtColumnText;
    private Text _lostColumnText;

    public void Present(CombatOutcomeReport report, Action onContinue) {
        if (report == null) {
            return;
        }

        bool isVictory = report.OutcomeType == CombatOutcomeType.Victory;
        ApplySettlementSkin(isVictory);

        if (titleText != null) {
            titleText.text = string.IsNullOrEmpty(report.Title)
                ? (isVictory ? "战斗胜利" : "战斗失败")
                : report.Title;
        }

        if (summaryText != null) {
            summaryText.text = BuildCombatOutcomeSummary(report);
        }

        if (lootText != null) {
            lootText.text = BuildCombatOutcomeDetails(report);
        }

        SetSettlementColumnsVisible(false);

        if (continueBtn != null) {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => onContinue?.Invoke());
        }
    }

    public void Present(DungeonSettlementResult result, Action onContinue) {
        if (result == null) {
            return;
        }

        ApplySettlementSkin(result.IsVictory);

        if (titleText != null) {
            titleText.text = result.IsVictory ? "撤离结算" : "战败结算";
        }

        if (summaryText != null) {
            summaryText.text = result.IsVictory
                ? $"最终带出 {result.LootTransferredCount} 件局内物资\n带出估值: {result.LootEstimatedValue}G\n本次拾取 {result.PickedUpCount} / 带出 {result.BroughtOutCount} / 损失 {result.LostCount}\n当前仓库库存: {result.StashCountAfterSettlement}"
                : $"本次深入失败，背包内物资已丢失。\n本次拾取 {result.PickedUpCount} / 带出 {result.BroughtOutCount} / 损失 {result.LostCount}\n当前仓库库存: {result.StashCountAfterSettlement}";
        }

        PopulateSettlementColumns(result);
        ConfigureLootTextForSettlementColumns(result);

        if (continueBtn != null) {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => onContinue?.Invoke());
        }
    }

    private void ApplySettlementSkin(bool isVictory) {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        Image rootImage = GetComponent<Image>();
        if (rootImage != null) {
            rootImage.color = Color.clear;
            rootImage.raycastTarget = false;
        }

        backdropImage = EnsureSettlementBackdrop(backdropImage, "SettlementBackdrop_Image");
        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "SettlementBackground_Image");
        VisualUIHelper.ApplyCoverSprite(
            backgroundImage,
            VisualAssetService.ResolveSettlementBackgroundID(isVictory),
            Color.white,
            new Color(0.05f, 0.055f, 0.065f, 0.96f));

        if (backgroundImage != null) {
            int backgroundIndex = Mathf.Min(1, backgroundImage.transform.parent.childCount - 1);
            backgroundImage.transform.SetSiblingIndex(backgroundIndex);
        }

        settlementPanelImage = EnsureSkinImage(
            settlementPanelImage,
            "SettlementCard_Image",
            transform,
            Vector2.zero,
            new Vector2(1240f, 760f),
            VisualAssetService.ResolveSettlementPanelID(isVictory));

        Transform contentParent = settlementPanelImage != null ? settlementPanelImage.transform : transform;
        titleDividerImage = EnsureTitleDividerImage(
            titleDividerImage,
            "TitleDivider_Image",
            contentParent,
            new Vector2(0f, 176f),
            new Vector2(820f, 32f));

        MoveText(titleText, contentParent, new Vector2(0f, 214f), new Vector2(840f, 50f), 34, TextAnchor.MiddleCenter, Color.white);
        MoveText(summaryText, contentParent, new Vector2(0f, 118f), new Vector2(920f, 78f), 19, TextAnchor.UpperCenter, new Color(0.92f, 0.91f, 0.84f, 1f));
        MoveText(lootText, contentParent, new Vector2(0f, -72f), new Vector2(900f, 300f), 19, TextAnchor.UpperLeft, new Color(1f, 0.92f, 0.58f, 1f));
        EnsureSettlementColumns(contentParent);
        MoveButton(continueBtn, contentParent, new Vector2(0f, -328f), new Vector2(320f, 68f));
        VisualUIHelper.ApplyButtonSkin(continueBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.85f, 0.48f, 0.18f));

        if (settlementPanelImage != null) {
            settlementPanelImage.transform.SetAsLastSibling();
        }
    }

    private Image EnsureSettlementBackdrop(Image current, string objectName) {
        Image image = VisualUIHelper.EnsurePanelBackground(transform, current, objectName);
        if (image == null) {
            return null;
        }

        VisualUIHelper.ApplySolidColor(image, new Color(0.015f, 0.016f, 0.014f, 1f));
        image.transform.SetAsFirstSibling();
        return image;
    }

    private Image EnsureSkinImage(Image current, string objectName, Transform parent, Vector2 position, Vector2 size, string visualID) {
        if (parent == null) {
            return current;
        }

        Image image = current;
        if (image == null) {
            Transform existing = parent.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            image = obj.AddComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, new Color(0.08f, 0.075f, 0.065f, 0.94f), false);
        return image;
    }

    private Image EnsureTitleDividerImage(Image current, string objectName, Transform parent, Vector2 position, Vector2 size) {
        Image image = EnsureSkinImage(current, objectName, parent, position, size, VisualAssetService.UITitleDividerID);
        VisualUIHelper.ApplySimpleSprite(
            image,
            VisualAssetService.UITitleDividerID,
            Color.white,
            new Color(0.72f, 0.58f, 0.32f, 0.9f),
            false,
            false);
        return image;
    }

    private void MoveText(Text text, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, Color color) {
        if (text == null || parent == null) {
            return;
        }

        text.transform.SetParent(parent, false);
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, fontSize - 6);
        text.resizeTextMaxSize = fontSize;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void EnsureSettlementColumns(Transform parent) {
        _pickedColumnImage = EnsureSettlementColumn(
            _pickedColumnImage,
            "PickedLootColumn",
            parent,
            new Vector2(-380f, -128f),
            out _pickedColumnText);
        _broughtColumnImage = EnsureSettlementColumn(
            _broughtColumnImage,
            "BroughtLootColumn",
            parent,
            new Vector2(0f, -128f),
            out _broughtColumnText);
        _lostColumnImage = EnsureSettlementColumn(
            _lostColumnImage,
            "LostLootColumn",
            parent,
            new Vector2(380f, -128f),
            out _lostColumnText);
    }

    private Image EnsureSettlementColumn(Image current, string objectName, Transform parent, Vector2 position, out Text text) {
        Image image = EnsureSkinImage(
            current,
            objectName,
            parent,
            position,
            new Vector2(340f, 260f),
            VisualAssetService.UIPanelInfoID);
        if (image != null) {
            image.color = new Color(1f, 1f, 1f, 0.72f);
        }

        Transform textTransform = image != null ? image.transform.Find("ColumnText") : null;
        text = textTransform != null ? textTransform.GetComponent<Text>() : null;
        if (text == null && image != null) {
            GameObject textObj = new GameObject("ColumnText");
            textObj.transform.SetParent(image.transform, false);
            text = textObj.AddComponent<Text>();
        }

        if (text != null) {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 19;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 13;
            text.resizeTextMaxSize = 19;
            text.color = new Color(0.96f, 0.91f, 0.78f, 1f);
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 20f);
            textRect.offsetMax = new Vector2(-24f, -20f);
        }

        return image;
    }

    private void PopulateSettlementColumns(DungeonSettlementResult result) {
        Transform parent = settlementPanelImage != null ? settlementPanelImage.transform : transform;
        EnsureSettlementColumns(parent);

        if (_pickedColumnText != null) {
            _pickedColumnText.text = BuildSettlementColumn("本次拾取", result.PickedUpNames, result.PickedUpEstimatedValue, result.PickedUpCount, "本次没有拾取战利品。");
        }

        if (_broughtColumnText != null) {
            _broughtColumnText.text = BuildSettlementColumn("最终带出", result.BroughtOutNames, result.BroughtOutEstimatedValue, result.BroughtOutCount, result.IsVictory ? "本次没有带出战利品。" : "战败时未能带出战利品。");
        }

        if (_lostColumnText != null) {
            _lostColumnText.text = BuildSettlementColumn("本次损失", result.LostNames, result.LostEstimatedValue, result.LostCount, "本次没有损失已拾取战利品。");
        }

        SetSettlementColumnsVisible(true);
    }

    private void SetSettlementColumnsVisible(bool visible) {
        SetActiveIfPresent(_pickedColumnImage, visible);
        SetActiveIfPresent(_broughtColumnImage, visible);
        SetActiveIfPresent(_lostColumnImage, visible);
        if (lootText != null) {
            lootText.gameObject.SetActive(true);
        }
    }

    private void ConfigureLootTextForSettlementColumns(DungeonSettlementResult result) {
        if (lootText == null) {
            return;
        }

        Transform parent = settlementPanelImage != null ? settlementPanelImage.transform : transform;
        MoveText(
            lootText,
            parent,
            new Vector2(0f, 34f),
            new Vector2(880f, 34f),
            18,
            TextAnchor.MiddleCenter,
            new Color(1f, 0.92f, 0.58f, 1f));
        lootText.text = result != null
            ? $"拾取 {result.PickedUpCount} 件 / 带出 {result.BroughtOutCount} 件 / 损失 {result.LostCount} 件"
            : "收益 / 带出 / 损失";
        lootText.gameObject.SetActive(true);
    }

    private void SetActiveIfPresent(Image image, bool visible) {
        if (image != null) {
            image.gameObject.SetActive(visible);
        }
    }

    private string BuildSettlementColumn(string title, System.Collections.Generic.List<string> names, int estimatedValue, int count, string emptyText) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(title);
        builder.Append(count);
        builder.Append(" 件 / ");
        builder.Append(estimatedValue);
        builder.AppendLine("G");
        builder.AppendLine();

        if (names != null && names.Count > 0) {
            int maxRows = Mathf.Min(names.Count, 8);
            for (int i = 0; i < maxRows; i++) {
                builder.Append("- ");
                builder.AppendLine(names[i]);
            }

            if (names.Count > maxRows) {
                builder.Append("+ ");
                builder.Append(names.Count - maxRows);
                builder.AppendLine(" more");
            }

            return builder.ToString().TrimEnd();
        }

        builder.Append("- ");
        builder.Append(emptyText);
        return builder.ToString();
    }

    private void MoveButton(Button button, Transform parent, Vector2 position, Vector2 size) {
        if (button == null || parent == null) {
            return;
        }

        button.transform.SetParent(parent, false);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private string BuildSettlementDetails(DungeonSettlementResult result) {
        if (result == null) {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        AppendSection(builder, "本次拾取", result.PickedUpNames, result.PickedUpEstimatedValue, "本次没有拾取任何战利品。");
        builder.AppendLine();
        AppendSection(builder, "最终带出", result.BroughtOutNames, result.BroughtOutEstimatedValue, result.IsVictory ? "本次没有带出任何战利品。" : "战败时未能带出任何战利品。");
        builder.AppendLine();
        AppendSection(builder, "本次损失", result.LostNames, result.LostEstimatedValue, "本次没有损失任何已拾取战利品。");
        return builder.ToString().TrimEnd();
    }

    private string BuildCombatOutcomeSummary(CombatOutcomeReport report) {
        if (report == null) {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        if (!string.IsNullOrEmpty(report.Summary)) {
            builder.AppendLine(report.Summary);
        }

        if (!string.IsNullOrEmpty(report.ActiveDollName)) {
            builder.Append("魔偶: ");
            builder.Append(report.ActiveDollName);
            builder.Append(" | HP ");
            builder.Append(report.ActiveDollHP);
            builder.Append("/");
            builder.Append(report.ActiveDollMaxHP);
            builder.Append(" | SAN ");
            builder.Append(report.ActiveDollSAN);
            builder.Append("/");
            builder.AppendLine(report.ActiveDollMaxSAN.ToString());
        }

        builder.Append("敌人剩余: ");
        builder.Append(report.EnemyAliveCount);
        builder.Append("/");
        builder.Append(report.EnemyTotalCount);
        builder.Append(" | 玩家存活: ");
        builder.Append(report.PlayerAliveCount);

        return builder.ToString().TrimEnd();
    }

    private string BuildCombatOutcomeDetails(CombatOutcomeReport report) {
        if (report == null) {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("战斗时间线:");

        if (report.TimelineEvents == null || report.TimelineEvents.Count == 0) {
            builder.AppendLine("- 暂无战斗记录。");
            return builder.ToString().TrimEnd();
        }

        int startIndex = Mathf.Max(0, report.TimelineEvents.Count - 6);
        for (int i = startIndex; i < report.TimelineEvents.Count; i++) {
            CombatTimelineEvent entry = report.TimelineEvents[i];
            if (entry == null) {
                continue;
            }

            builder.Append("- ");
            if (!string.IsNullOrEmpty(entry.Title)) {
                builder.Append(entry.Title);
            } else {
                builder.Append(entry.EventType);
            }

            if (!string.IsNullOrEmpty(entry.Detail)) {
                builder.Append(": ");
                builder.Append(entry.Detail);
            }

            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private void AppendSection(StringBuilder builder, string title, System.Collections.Generic.List<string> names, int estimatedValue, string emptyText) {
        builder.Append(title);
        builder.Append(" (");
        builder.Append(estimatedValue);
        builder.AppendLine("G):");

        if (names != null && names.Count > 0) {
            for (int i = 0; i < names.Count; i++) {
                builder.Append("- ");
                builder.AppendLine(names[i]);
            }
            return;
        }

        builder.Append("- ");
        builder.AppendLine(emptyText);
    }
}

public class CombatLootUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Transform lootParent;
    public Button continueBtn;
    public Image pickupPanelImage;
    public Image lootDropZoneImage;
    public Image itemDetailPanelImage;

    private static readonly Vector2[] BaseSpawnOffsets = {
        new Vector2(-620f, 190f),
        new Vector2(620f, 190f),
        new Vector2(-620f, 30f),
        new Vector2(620f, 30f),
        new Vector2(-620f, -130f),
        new Vector2(620f, -130f),
        new Vector2(-300f, 300f),
        new Vector2(0f, 318f),
        new Vector2(300f, 300f)
    };

    public void Present(CombatLootPickupResult result, GameObject itemPrefab, Action onContinue) {
        if (result == null) {
            return;
        }

        ClearUnclaimedLootVisuals();
        PrepareOverlayForPickup();
        EnsureLootPickupSkin();

        if (titleText != null) {
            titleText.text = "战利品拾取";
            titleText.raycastTarget = false;
        }

        if (summaryText != null) {
            summaryText.text =
                $"本场掉落 {result.OfferedItems.Count} 件物资\n" +
                $"估值合计: {result.TotalEstimatedValue}G\n" +
                "将战利品拖入背包后继续，未拾取的物品会被丢弃。";
            summaryText.raycastTarget = false;
        }

        if (lootParent != null && itemPrefab != null) {
            for (int i = 0; i < result.OfferedItems.Count; i++) {
                ItemEntity item = result.OfferedItems[i];
                if (item == null) {
                    continue;
                }

                GameObject itemGo = Instantiate(itemPrefab, lootParent);
                DraggableItemUI itemUI = itemGo.GetComponent<DraggableItemUI>();
                if (itemUI != null) {
                    itemUI.SetupData(item);
                }

                PositionLootItem(itemGo, i);
            }
        }

        if (continueBtn != null) {
            VisualUIHelper.ApplyButtonSkin(continueBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.9f, 0.58f, 0.18f));
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => {
                ClearUnclaimedLootVisuals();
                onContinue?.Invoke();
            });
        }
    }

    private void PrepareOverlayForPickup() {
        Image panelImage = GetComponent<Image>();
        if (panelImage != null) {
            VisualUIHelper.ApplySolidColor(panelImage, new Color(0.006f, 0.008f, 0.012f, 0.46f), true);
        }

        if (lootParent == null) {
            return;
        }

        LayoutGroup layoutGroup = lootParent.GetComponent<LayoutGroup>();
        if (layoutGroup != null) {
            layoutGroup.enabled = false;
        }

        RectTransform lootRect = lootParent as RectTransform;
        if (lootRect != null) {
            lootRect.anchorMin = new Vector2(0.5f, 0.5f);
            lootRect.anchorMax = new Vector2(0.5f, 0.5f);
            lootRect.pivot = new Vector2(0.5f, 0.5f);
            lootRect.anchoredPosition = Vector2.zero;
            lootRect.sizeDelta = new Vector2(1480f, 720f);
        }
    }

    private void PositionLootItem(GameObject itemGo, int index) {
        if (itemGo == null) {
            return;
        }

        RectTransform itemRect = itemGo.GetComponent<RectTransform>();
        if (itemRect == null) {
            return;
        }

        Vector2 spawnOffset = ClampLootSpawnOffset(GetSpawnOffset(index), itemRect);
        itemRect.anchorMin = new Vector2(0.5f, 0.5f);
        itemRect.anchorMax = new Vector2(0.5f, 0.5f);
        itemRect.anchoredPosition = spawnOffset;
        itemRect.localRotation = Quaternion.identity;
        itemRect.localScale = Vector3.one;
        itemGo.transform.SetAsLastSibling();
    }

    private Vector2 GetSpawnOffset(int index) {
        if (index < BaseSpawnOffsets.Length) {
            return BaseSpawnOffsets[index];
        }

        int overflowIndex = index - BaseSpawnOffsets.Length;
        int side = overflowIndex % 2 == 0 ? -1 : 1;
        int row = overflowIndex / 2;
        float y = 250f - row * 120f;
        return new Vector2(side * 735f, y);
    }

    private Vector2 ClampLootSpawnOffset(Vector2 offset, RectTransform itemRect) {
        RectTransform parentRect = lootParent as RectTransform;
        if (parentRect == null || itemRect == null) {
            return offset;
        }

        Vector2 itemSize = itemRect.sizeDelta;
        float halfWidth = Mathf.Max(0f, parentRect.sizeDelta.x * 0.5f - Mathf.Max(70f, itemSize.x * 0.5f));
        float halfHeight = Mathf.Max(0f, parentRect.sizeDelta.y * 0.5f - Mathf.Max(70f, itemSize.y * 0.5f));
        return new Vector2(
            Mathf.Clamp(offset.x, -halfWidth, halfWidth),
            Mathf.Clamp(offset.y, -halfHeight, halfHeight));
    }

    private void EnsureLootPickupSkin() {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        pickupPanelImage = EnsureSkinImage(
            pickupPanelImage,
            "PickupPanel",
            transform,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(1120f, 760f),
            VisualAssetService.UILootPickupPanelID);
        if (pickupPanelImage != null) {
            pickupPanelImage.color = new Color(1f, 1f, 1f, 0.2f);
        }

        Transform panelTransform = pickupPanelImage != null ? pickupPanelImage.transform : transform;
        lootDropZoneImage = EnsureSkinImage(
            lootDropZoneImage,
            "LootDropZone",
            panelTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -28f),
            new Vector2(720f, 500f),
            VisualAssetService.UILootDropZoneID);
        if (lootDropZoneImage != null) {
            lootDropZoneImage.color = new Color(1f, 1f, 1f, 0.18f);
        }

        itemDetailPanelImage = EnsureSkinImage(
            itemDetailPanelImage,
            "ItemDetailPanel",
            panelTransform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 266f),
            new Vector2(740f, 118f),
            VisualAssetService.UIPanelInfoID);
        if (itemDetailPanelImage != null) {
            itemDetailPanelImage.color = new Color(1f, 1f, 1f, 0.52f);
        }

        MoveText(titleText, panelTransform, new Vector2(0f, 342f), new Vector2(740f, 58f), 34, TextAnchor.MiddleCenter);
        MoveText(summaryText, itemDetailPanelImage != null ? itemDetailPanelImage.transform : panelTransform, Vector2.zero, new Vector2(680f, 96f), 18, TextAnchor.MiddleCenter);

        if (lootParent != null) {
            lootParent.SetParent(panelTransform, false);
            lootParent.SetAsLastSibling();
        }

        if (continueBtn != null) {
            continueBtn.transform.SetParent(panelTransform, false);
            RectTransform btnRect = continueBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 0.5f);
            btnRect.anchorMax = new Vector2(0.5f, 0.5f);
            btnRect.pivot = new Vector2(0.5f, 0.5f);
            btnRect.anchoredPosition = new Vector2(0f, -342f);
            btnRect.sizeDelta = new Vector2(320f, 62f);
        }

        if (pickupPanelImage != null) {
            pickupPanelImage.transform.SetAsFirstSibling();
        }
        if (lootDropZoneImage != null) {
            lootDropZoneImage.transform.SetAsFirstSibling();
        }
    }

    private Image EnsureSkinImage(Image current, string objectName, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string visualID) {
        if (parent == null) {
            return current;
        }

        Image image = current;
        if (image == null) {
            Transform existing = parent.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            image = obj.AddComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, new Color(0.08f, 0.075f, 0.065f, 0.92f), false);
        return image;
    }

    private void MoveText(Text text, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment) {
        if (text == null || parent == null) {
            return;
        }

        text.transform.SetParent(parent, false);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void ClearUnclaimedLootVisuals() {
        if (lootParent == null) {
            return;
        }

        for (int i = lootParent.childCount - 1; i >= 0; i--) {
            DestroyRuntimeObject(lootParent.GetChild(i).gameObject);
        }
    }

    private void DestroyRuntimeObject(GameObject target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(target);
        } else {
            DestroyImmediate(target);
        }
    }
}
