using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonStartLayerUIController : MonoBehaviour {
    private const string FirstDivePermitTitle = "\u9996\u6b21\u4e0b\u6f5c\u8bb8\u53ef";
    private const string FirstDiveGateVisualID = "cg_t0_01a_p04_panel02_shallow_gate_glow";

    public Text titleText;
    public Text summaryText;
    public Transform listParent;
    public Button confirmBtn;
    public Button closeBtn;
    public Image titleDividerImage;
    public Image backgroundImage;

    private readonly List<GameObject> _rows = new List<GameObject>();
    private int _selectedLayerID = 1;
    private Action _onClose;
    private Action _onFirstDiveDepart;
    private string _lastStartFailureReason;
    private bool _firstDiveMode;
    private bool _hasFirstDivePermitCard;
    private string _permitTitleText = string.Empty;
    private string _permitSummaryText = string.Empty;

    public string PermitTitleText {
        get {
            if (!_firstDiveMode) {
                return titleText != null ? titleText.text : string.Empty;
            }

            return string.IsNullOrEmpty(_permitTitleText) ? FirstDivePermitTitle : _permitTitleText;
        }
    }

    public string PermitSummaryText {
        get { return _firstDiveMode ? _permitSummaryText : (summaryText != null ? summaryText.text : string.Empty); }
    }

    public bool IsFirstDiveMode {
        get { return _firstDiveMode; }
    }

    public bool HasFirstDivePermitCard {
        get { return _hasFirstDivePermitCard; }
    }

    public int SelectedLayerID {
        get { return _selectedLayerID; }
    }

    public bool HasFirstDiveDepartCallback {
        get { return _onFirstDiveDepart != null; }
    }

    public void ResetFirstDivePermitState() {
        _firstDiveMode = false;
        _hasFirstDivePermitCard = false;
        _onFirstDiveDepart = null;
        _permitTitleText = string.Empty;
        _permitSummaryText = string.Empty;
        _lastStartFailureReason = string.Empty;
    }

    public void Present(Action onClose) {
        ResetFirstDivePermitState();
        _onClose = onClose;
        RefreshBackground(false);
        SetTopCopyVisible(true);

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null || GameRoot.Core.Dungeon == null) {
            return;
        }

        NormalizeSelection(player);
        RefreshTexts(player);
        RefreshLayerRows(player);
        BindButtons();
    }

    public void PresentFirstDive(Action onClose, Action onDepartRequested = null) {
        _firstDiveMode = true;
        _onClose = onClose;
        _onFirstDiveDepart = onDepartRequested;
        _lastStartFailureReason = string.Empty;
        RefreshBackground(true);
        SetTopCopyVisible(false);

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            return;
        }

        _selectedLayerID = 1;
        RefreshTexts(player);
        RefreshLayerRows(player);
        BindButtons();
        ApplyFirstDiveRuntimeLayout();
    }

    private void NormalizeSelection(PlayerProfile player) {
        if (_firstDiveMode) {
            _selectedLayerID = 1;
            return;
        }

        int preferredLayer = player.LastSelectedDungeonStartLayer;
        if (!GameRoot.Core.Dungeon.CanStartAtLayer(preferredLayer)) {
            preferredLayer = FindDeepestStartableLayer(player);
        }

        _selectedLayerID = Mathf.Max(1, preferredLayer);
    }

    private int FindDeepestStartableLayer(PlayerProfile player) {
        int fallbackLayer = 1;
        int deepestLayer = 0;
        foreach (var kvp in ConfigManager.Dungeons) {
            int layerID = kvp.Key;
            if (layerID <= player.HighestUnlockedDungeonLayer && layerID > fallbackLayer) {
                fallbackLayer = layerID;
            }

            if (GameRoot.Core.Dungeon.CanStartAtLayer(layerID) && layerID > deepestLayer) {
                deepestLayer = layerID;
            }
        }

        return deepestLayer > 0 ? deepestLayer : fallbackLayer;
    }

    private void RefreshTexts(PlayerProfile player) {
        if (_firstDiveMode) {
            RefreshFirstDiveTexts(player);
            return;
        }

        if (titleText != null) {
            titleText.text = "选择深渊入口";
        }

        if (summaryText != null) {
            DiveReadinessResult readiness = BuildReadiness(player, _selectedLayerID);
            string selectedState = readiness != null && readiness.CanDive ? "可下潜" : "无法下潜";
            string failureLine = string.IsNullOrEmpty(_lastStartFailureReason)
                ? string.Empty
                : $"\n上次出发失败：{_lastStartFailureReason}";
            summaryText.text =
                $"已解锁至第 {player.HighestUnlockedDungeonLayer} 层\n" +
                $"当前选择：第 {_selectedLayerID} 层 - {selectedState}\n" +
                $"{BuildReadinessSummary(readiness)}{failureLine}\n" +
                "从小镇出发会开启新一轮探索，并重置本轮战利品账本。";
        }
    }

    private void RefreshFirstDiveTexts(PlayerProfile player) {
        DiveReadinessResult readiness = BuildReadiness(player, 1);
        ApplyFormalFirstDivePermitCopy(readiness);

        if (titleText != null) {
            titleText.text = string.Empty;
        }

        if (summaryText != null) {
            summaryText.text = string.Empty;
        }
    }

    private void RefreshLayerRows(PlayerProfile player) {
        ClearRows();
        _hasFirstDivePermitCard = false;
        if (listParent == null) {
            return;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_firstDiveMode) {
            CreateFirstDivePermitCard(player, defaultFont);
            return;
        }

        List<int> layerIDs = new List<int>(ConfigManager.Dungeons.Keys);
        layerIDs.Sort();

        foreach (int layerID in layerIDs) {
            if (_firstDiveMode && layerID != 1) {
                continue;
            }

            if (!ConfigManager.Dungeons.TryGetValue(layerID, out DungeonConfig config)) {
                continue;
            }

            CreateLayerRow(layerID, config, player, defaultFont);
        }
    }

    private void CreateFirstDivePermitCard(PlayerProfile player, Font font) {
        _hasFirstDivePermitCard = true;
        GameObject card = new GameObject("FirstDivePermitCard");
        card.transform.SetParent(listParent, false);
        _rows.Add(card);

        RectTransform cardRect = card.AddComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(1100f, 548f);
        LayoutElement layoutElement = card.AddComponent<LayoutElement>();
        layoutElement.minWidth = 1100f;
        layoutElement.preferredWidth = 1100f;
        layoutElement.minHeight = 548f;
        layoutElement.preferredHeight = 548f;

        Image cardBg = card.AddComponent<Image>();
        VisualUIHelper.ApplySolidColor(cardBg, new Color(0.004f, 0.020f, 0.030f, 0.32f), true);

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.64f, 0.96f, 1f, 0.18f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);

        Image innerWash = CreateImage("PermitInnerWash_Image", card.transform);
        LayoutElement washLayout = innerWash.gameObject.AddComponent<LayoutElement>();
        washLayout.ignoreLayout = true;
        RectTransform washRect = innerWash.rectTransform;
        washRect.anchorMin = new Vector2(0f, 0f);
        washRect.anchorMax = new Vector2(1f, 1f);
        washRect.offsetMin = new Vector2(20f, 20f);
        washRect.offsetMax = new Vector2(-20f, -20f);
        VisualUIHelper.ApplySolidColor(innerWash, new Color(0.08f, 0.42f, 0.5f, 0.09f), false);

        Image gate = CreateImage("PermitGate_Image", card.transform);
        LayoutElement gateLayout = gate.gameObject.AddComponent<LayoutElement>();
        gateLayout.ignoreLayout = true;
        RectTransform gateRect = gate.rectTransform;
        gateRect.anchorMin = new Vector2(0f, 0f);
        gateRect.anchorMax = new Vector2(0f, 1f);
        gateRect.pivot = new Vector2(0f, 0.5f);
        gateRect.anchoredPosition = new Vector2(30f, 0f);
        gateRect.sizeDelta = new Vector2(520f, -44f);
        VisualUIHelper.ApplyCoverSprite(gate, FirstDiveGateVisualID, Color.white, new Color(0.02f, 0.07f, 0.09f, 0.92f));
        gate.color = new Color(0.92f, 1f, 1f, 0.96f);
        gate.raycastTarget = false;

        Image gateVeil = CreateImage("PermitGateVeil_Image", card.transform);
        LayoutElement gateVeilLayout = gateVeil.gameObject.AddComponent<LayoutElement>();
        gateVeilLayout.ignoreLayout = true;
        RectTransform gateVeilRect = gateVeil.rectTransform;
        gateVeilRect.anchorMin = gateRect.anchorMin;
        gateVeilRect.anchorMax = gateRect.anchorMax;
        gateVeilRect.pivot = gateRect.pivot;
        gateVeilRect.anchoredPosition = gateRect.anchoredPosition;
        gateVeilRect.sizeDelta = gateRect.sizeDelta;
        VisualUIHelper.ApplySolidColor(gateVeil, new Color(0.006f, 0.02f, 0.028f, 0.08f), false);

        Image seal = CreateImage("PermitSeal_Image", card.transform);
        LayoutElement sealLayout = seal.gameObject.AddComponent<LayoutElement>();
        sealLayout.ignoreLayout = true;
        RectTransform sealRect = seal.rectTransform;
        sealRect.anchorMin = new Vector2(0f, 1f);
        sealRect.anchorMax = new Vector2(0f, 1f);
        sealRect.pivot = new Vector2(0f, 1f);
        sealRect.anchoredPosition = new Vector2(186f, -48f);
        sealRect.sizeDelta = new Vector2(136f, 136f);
        VisualUIHelper.ApplyContainSprite(seal, VisualAssetService.UIIconDivePermitID, new Vector2(136f, 136f), new Color(0.72f, 0.98f, 1f, 0.98f), new Color(0.06f, 0.44f, 0.54f, 0.86f), false);

        Text sealText = CreatePermitText("PermitSealText_Text", card.transform, font, 18, new Color(0.78f, 0.96f, 1f, 0.92f));
        LayoutElement sealTextLayout = sealText.gameObject.AddComponent<LayoutElement>();
        sealTextLayout.ignoreLayout = true;
        sealText.alignment = TextAnchor.UpperCenter;
        sealText.text = "\u4ec5\u6b64\n\u4e00\u6b21\n\u653e\u884c";
        RectTransform sealTextRect = sealText.rectTransform;
        sealTextRect.anchorMin = new Vector2(0f, 1f);
        sealTextRect.anchorMax = new Vector2(0f, 1f);
        sealTextRect.pivot = new Vector2(0f, 1f);
        sealTextRect.anchoredPosition = new Vector2(160f, -170f);
        sealTextRect.sizeDelta = new Vector2(140f, 82f);

        Text sideNote = CreatePermitText("PermitSideNote_Text", card.transform, font, 16, new Color(0.66f, 0.84f, 0.88f, 0.78f));
        LayoutElement sideNoteLayout = sideNote.gameObject.AddComponent<LayoutElement>();
        sideNoteLayout.ignoreLayout = true;
        sideNote.alignment = TextAnchor.UpperCenter;
        sideNote.text = "\u65e7\u77ff\u4e95\u6d45\u7f1d\n\u53ea\u653e\u884c\u8fd9\u4e00\u6b21";
        RectTransform sideNoteRect = sideNote.rectTransform;
        sideNoteRect.anchorMin = new Vector2(0f, 0f);
        sideNoteRect.anchorMax = new Vector2(0f, 0f);
        sideNoteRect.pivot = new Vector2(0f, 0f);
        sideNoteRect.anchoredPosition = new Vector2(124f, 34f);
        sideNoteRect.sizeDelta = new Vector2(240f, 78f);

        Image copyPanel = CreateImage("PermitCopyPanel_Image", card.transform);
        LayoutElement copyPanelLayout = copyPanel.gameObject.AddComponent<LayoutElement>();
        copyPanelLayout.ignoreLayout = true;
        RectTransform copyPanelRect = copyPanel.rectTransform;
        copyPanelRect.anchorMin = new Vector2(0f, 1f);
        copyPanelRect.anchorMax = new Vector2(0f, 1f);
        copyPanelRect.pivot = new Vector2(0f, 1f);
        copyPanelRect.anchoredPosition = new Vector2(580f, -48f);
        copyPanelRect.sizeDelta = new Vector2(480f, 360f);
        VisualUIHelper.ApplySolidColor(copyPanel, new Color(0.012f, 0.040f, 0.050f, 0.28f), false);

        Text header = CreatePermitText("PermitTitle_Text", copyPanel.transform, font, 38, new Color(0.84f, 0.98f, 1f, 1f));
        header.text = _permitTitleText;
        RectTransform headerRect = header.rectTransform;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0f, 1f);
        headerRect.anchoredPosition = new Vector2(22f, -16f);
        headerRect.sizeDelta = new Vector2(-44f, 64f);

        Text subHeader = CreatePermitText("PermitSubTitle_Text", copyPanel.transform, font, 20, new Color(0.64f, 0.86f, 0.9f, 0.82f));
        subHeader.text = "\u7b2c\u4e00\u5c42\uff1a\u65e7\u77ff\u4e95\u6d45\u7f1d";
        RectTransform subHeaderRect = subHeader.rectTransform;
        subHeaderRect.anchorMin = new Vector2(0f, 1f);
        subHeaderRect.anchorMax = new Vector2(1f, 1f);
        subHeaderRect.pivot = new Vector2(0f, 1f);
        subHeaderRect.anchoredPosition = new Vector2(24f, -74f);
        subHeaderRect.sizeDelta = new Vector2(-48f, 34f);

        DiveReadinessResult readiness = BuildReadiness(player, 1);
        Text body = CreatePermitText("PermitBody_Text", copyPanel.transform, font, 24, new Color(0.76f, 0.92f, 0.95f, 0.98f));
        body.text = _permitSummaryText;
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.offsetMin = new Vector2(24f, 28f);
        bodyRect.offsetMax = new Vector2(-24f, -116f);

        Text permitNo = CreatePermitText("PermitSerial_Text", card.transform, font, 16, new Color(0.64f, 0.84f, 0.88f, 0.82f));
        LayoutElement permitNoLayout = permitNo.gameObject.AddComponent<LayoutElement>();
        permitNoLayout.ignoreLayout = true;
        permitNo.alignment = TextAnchor.UpperRight;
        permitNo.text = "T0-01 / \u6d45\u5c42\u4e34\u65f6\u8bb8\u53ef";
        RectTransform permitNoRect = permitNo.rectTransform;
        permitNoRect.anchorMin = new Vector2(1f, 0f);
        permitNoRect.anchorMax = new Vector2(1f, 0f);
        permitNoRect.pivot = new Vector2(1f, 0f);
        permitNoRect.anchoredPosition = new Vector2(-46f, 38f);
        permitNoRect.sizeDelta = new Vector2(360f, 32f);
    }

    private Text CreatePermitText(string objectName, Transform parent, Font font, int fontSize, Color color) {
        Text text = CreateText(objectName, parent, font, fontSize, color);
        text.alignment = TextAnchor.UpperLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(16, fontSize - 6);
        text.resizeTextMaxSize = fontSize;
        return text;
    }

    private void CreateLayerRow(int layerID, DungeonConfig config, PlayerProfile player, Font font) {
        GameObject row = new GameObject($"DungeonStartLayer_{layerID}");
        row.transform.SetParent(listParent, false);
        _rows.Add(row);

        Image rowBg = row.AddComponent<Image>();
        DiveReadinessResult readiness = BuildReadiness(player, layerID);
        bool canStart = readiness != null && readiness.CanDive;
        bool selected = layerID == _selectedLayerID;
        Color rowTint = selected
            ? new Color(0.92f, 0.64f, 0.22f, 0.95f)
            : canStart ? new Color(0.18f, 0.27f, 0.32f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.75f);
        VisualUIHelper.ApplySlicedSprite(
            rowBg,
            selected ? VisualAssetService.UIListRowSelectedID : VisualAssetService.UIListRowNormalID,
            rowTint,
            rowTint,
            true);

        Button rowButton = row.AddComponent<Button>();
        rowButton.interactable = canStart;
        rowButton.onClick.AddListener(() => {
            _selectedLayerID = layerID;
            RefreshLayerRows(player);
            RefreshConfirmButton();
        });

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(800f, 86f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(24, 24, 10, 10);
        layout.spacing = 16f;

        if (!canStart) {
            Image lockedIcon = CreateImage("LockedIcon_Image", row.transform);
            VisualUIHelper.ApplyContainSprite(
                lockedIcon,
                VisualAssetService.UIIconLockedID,
                new Vector2(44f, 44f),
                Color.white,
                new Color(0.65f, 0.65f, 0.65f, 1f));
        }

        Text layerText = CreateText("Layer_Text", row.transform, font, 24, canStart ? Color.white : new Color(0.68f, 0.68f, 0.68f));
        layerText.text = _firstDiveMode ? "第一层  旧矿井浅缝" : $"第 {layerID} 层  {config.Name}";
        layerText.rectTransform.sizeDelta = new Vector2(330f, 66f);

        Text stateText = CreateText("State_Text", row.transform, font, 20, canStart ? new Color(0.78f, 1f, 0.82f) : new Color(1f, 0.68f, 0.58f));
        stateText.text = _firstDiveMode ? BuildFirstDiveRowStateText(readiness) : BuildLayerStateText(readiness, selected);
        stateText.alignment = TextAnchor.MiddleRight;
        stateText.horizontalOverflow = HorizontalWrapMode.Wrap;
        stateText.verticalOverflow = VerticalWrapMode.Truncate;
        stateText.resizeTextForBestFit = true;
        stateText.resizeTextMinSize = 14;
        stateText.resizeTextMaxSize = 20;
        stateText.rectTransform.sizeDelta = new Vector2(canStart ? 360f : 330f, 66f);
    }

    private Image CreateImage(string objectName, Transform parent) {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private DiveReadinessResult BuildReadiness(PlayerProfile player, int layerID) {
        return DiveReadinessService.Evaluate(player, layerID, false);
    }

    private string BuildLayerStateText(DiveReadinessResult readiness, bool selected) {
        string summary = BuildCompactReadinessSummary(readiness);
        if (readiness == null) {
            return "状态未知";
        }

        if (readiness.CanDive) {
            string prefix = selected ? "已选择" : "可出发";
            return IsReadySummary(summary) ? prefix : $"{prefix}\n{summary}";
        }

        return $"无法出发\n{summary}";
    }

    private string BuildFirstDiveReadinessLine(DiveReadinessResult readiness) {
        if (readiness == null) {
            return "许可：还没有读到她的状态。";
        }

        DiveReadinessIssue blocker = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Blocker);
        if (blocker != null) {
            return $"许可：{MapFirstDiveIssue(blocker)}";
        }

        DiveReadinessIssue warning = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Warning);
        if (warning != null) {
            return $"许可：可以出发。{MapFirstDiveIssue(warning)}";
        }

        return "许可：入口稳定，零号还能撑一次。";
    }

    private void ApplyFormalFirstDivePermitCopy(DiveReadinessResult readiness) {
        _permitTitleText = FirstDivePermitTitle;
        _permitSummaryText = BuildFormalFirstDivePermitBody(readiness);
    }

    private string BuildFormalFirstDivePermitBody(DiveReadinessResult readiness) {
        string state = readiness != null && readiness.CanDive
            ? "\u96f6\u53f7\uff1a\u77ed\u65f6\u7a33\u5b9a\uff0c\u53ef\u79bb\u5f00\u5de5\u574a"
            : "\u96f6\u53f7\uff1a\u72b6\u6001\u672a\u8fbe\u5230\u51fa\u53d1\u6761\u4ef6";
        string permit = "\u8bb8\u53ef\uff1a\u5165\u53e3\u7a33\u5b9a\uff0c\u53ea\u653e\u884c\u6d45\u5c42\u4e00\u6b21";

        DiveReadinessIssue blocker = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Blocker);
        if (blocker != null) {
            permit = "\u8bb8\u53ef\uff1a" + MapFirstDiveIssue(blocker);
        } else {
            DiveReadinessIssue warning = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Warning);
            if (warning != null) {
                permit = "\u8bb8\u53ef\uff1a\u53ef\u4ee5\u51fa\u53d1\u3002" + MapFirstDiveIssue(warning);
            }
        }

        return state + "\n"
            + permit + "\n"
            + "\u53ef\u80fd\u5e26\u56de\uff1a\u7a33\u5b9a\u6838\u5fc3\u788e\u5c51 / \u53ef\u552e\u5e9f\u6599 / \u8bb0\u5fc6\u566a\u58f0\n"
            + "\u98ce\u9669\uff1a\u89c1\u5230\u5f02\u5e38\u5c31\u64a4\u56de\uff0c\u4e0d\u6df1\u5165\u3002";
    }

    private void ApplyFirstDiveRuntimeLayout() {
        if (!_firstDiveMode || listParent == null) {
            return;
        }

        RectTransform listRect = listParent as RectTransform;
        if (listRect != null) {
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.pivot = new Vector2(0.5f, 0.5f);
            listRect.anchoredPosition = new Vector2(0f, 38f);
            listRect.sizeDelta = new Vector2(1100f, 560f);
        }

        RectTransform shellRect = listParent.parent as RectTransform;
        if (shellRect != null) {
            shellRect.sizeDelta = new Vector2(1220f, 700f);
        }

        Image shellImage = listParent.parent != null ? listParent.parent.GetComponent<Image>() : null;
        if (shellImage != null) {
            VisualUIHelper.ApplySolidColor(shellImage, new Color(0.004f, 0.014f, 0.02f, 0.16f), false);
        }

        PlaceFirstDiveButton(closeBtn, new Vector2(-216f, 36f), new Vector2(206f, 56f));
        PlaceFirstDiveButton(confirmBtn, new Vector2(222f, 36f), new Vector2(250f, 62f));
    }

    private static void PlaceFirstDiveButton(Button button, Vector2 anchoredPosition, Vector2 size) {
        if (button == null) {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        button.transform.SetAsLastSibling();
    }

    private void RefreshBackground(bool firstDiveMode) {
        if (backgroundImage == null) {
            return;
        }

        string visualID = firstDiveMode ? FirstDiveGateVisualID : VisualAssetService.ResolveLayerSelectBackgroundID();
        Color missingColor = firstDiveMode
            ? new Color(0.008f, 0.03f, 0.04f, 1f)
            : new Color(0.025f, 0.035f, 0.04f, 0.94f);
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, Color.white, missingColor);
        backgroundImage.color = firstDiveMode ? new Color(0.82f, 0.96f, 1f, 1f) : Color.white;
    }

    private string BuildFirstDiveRowStateText(DiveReadinessResult readiness) {
        if (readiness == null) {
            return "暂缓\n状态未读取";
        }

        DiveReadinessIssue blocker = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Blocker);
        if (blocker != null) {
            return $"暂缓\n{MapFirstDiveIssue(blocker)}";
        }

        DiveReadinessIssue warning = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Warning);
        if (warning != null) {
            return $"可出发\n{MapFirstDiveIssue(warning)}";
        }

        return "可出发\n入口稳定";
    }

    private string MapFirstDiveIssue(DiveReadinessIssue issue) {
        if (issue == null) {
            return "状态有异常，先整理工坊。";
        }

        switch (issue.Code) {
            case DiveReadinessIssueCode.LayerLocked:
                return "浅层入口还没稳定。";
            case DiveReadinessIssueCode.LayerConfigMissing:
                return "浅层入口记录缺失。";
            case DiveReadinessIssueCode.MissingPlayer:
                return "工坊记录还没恢复。";
            case DiveReadinessIssueCode.InvalidLayer:
                return "入口坐标不稳定。";
            case DiveReadinessIssueCode.MissingActiveDoll:
                return "零号还没有准备好。";
            case DiveReadinessIssueCode.ExtremeWear:
                return "磨损太高，先做维护。";
            case DiveReadinessIssueCode.ExtremeCorruption:
                return "侵蚀太高，先做净化。";
            case DiveReadinessIssueCode.MissingChassis:
            case DiveReadinessIssueCode.InvalidChassisID:
            case DiveReadinessIssueCode.InvalidChassisDimensions:
            case DiveReadinessIssueCode.InvalidChassisMask:
                return "底盘未安装，不能下潜。";
            case DiveReadinessIssueCode.MissingRuntimeGrid:
            case DiveReadinessIssueCode.RuntimeGridMismatch:
                return "背包底盘还没整理好。";
            case DiveReadinessIssueCode.InvalidProstheticReference:
            case DiveReadinessIssueCode.InvalidProstheticSlot:
            case DiveReadinessIssueCode.DuplicateProstheticSlot:
            case DiveReadinessIssueCode.AutoUnequippedProsthetic:
                return "义体连接异常，先卸下或调整。";
            case DiveReadinessIssueCode.HeavyWearWarning:
                return "磨损偏高，回来后要维护。";
            case DiveReadinessIssueCode.HighCorruptionWarning:
                return "侵蚀偏高，风险会上升。";
            default:
                return "状态有异常，先整理工坊。";
        }
    }

    private string BuildCompactReadinessSummary(DiveReadinessResult readiness) {
        if (readiness == null) {
            return "状态未知";
        }

        DiveReadinessIssue issue = FindFirstIssue(readiness, DiveReadinessIssueSeverity.Blocker)
            ?? FindFirstIssue(readiness, DiveReadinessIssueSeverity.Warning)
            ?? FindFirstIssue(readiness, DiveReadinessIssueSeverity.Info);
        if (issue == null) {
            return "Ready to dive.";
        }

        switch (issue.Code) {
            case DiveReadinessIssueCode.LayerLocked:
                return "未解锁：先通过上一层";
            case DiveReadinessIssueCode.LayerConfigMissing:
                return "配置缺失：检查层配置";
            case DiveReadinessIssueCode.MissingActiveDoll:
                return "无人偶：先选择出战人偶";
            case DiveReadinessIssueCode.ExtremeWear:
                return "磨损过高：先维护";
            case DiveReadinessIssueCode.ExtremeCorruption:
                return "侵蚀过高：先净化";
            case DiveReadinessIssueCode.MissingChassis:
                return "无底盘：先安装底盘";
            case DiveReadinessIssueCode.MissingRuntimeGrid:
            case DiveReadinessIssueCode.RuntimeGridMismatch:
                return "背包异常：先整理底盘";
            case DiveReadinessIssueCode.InvalidProstheticReference:
            case DiveReadinessIssueCode.InvalidProstheticSlot:
            case DiveReadinessIssueCode.DuplicateProstheticSlot:
                return "义体异常：先调整装备";
            case DiveReadinessIssueCode.HeavyWearWarning:
                return "磨损偏高：建议维护";
            case DiveReadinessIssueCode.HighCorruptionWarning:
                return "侵蚀偏高：风险上升";
            case DiveReadinessIssueCode.AutoUnequippedProsthetic:
                return "已自动卸下异常义体";
            default:
                return string.IsNullOrEmpty(issue.Message) ? issue.Code.ToString() : issue.Message;
        }
    }

    private DiveReadinessIssue FindFirstIssue(DiveReadinessResult readiness, DiveReadinessIssueSeverity severity) {
        if (readiness?.Issues == null) {
            return null;
        }

        foreach (DiveReadinessIssue issue in readiness.Issues) {
            if (issue != null && issue.Severity == severity) {
                return issue;
            }
        }

        return null;
    }

    private string BuildReadinessSummary(DiveReadinessResult readiness) {
        if (readiness == null) {
            return "Readiness unavailable.";
        }

        string summary = readiness.BuildSummary();
        return string.IsNullOrWhiteSpace(summary) ? "Ready to dive." : summary;
    }

    private bool IsReadySummary(string summary) {
        return string.Equals(summary, "Ready to dive.", StringComparison.Ordinal);
    }

    private void SetTopCopyVisible(bool visible) {
        if (titleText != null) {
            titleText.gameObject.SetActive(visible);
        }

        if (summaryText != null) {
            summaryText.gameObject.SetActive(visible);
        }

        if (titleDividerImage != null) {
            titleDividerImage.gameObject.SetActive(visible);
        }
    }

    private Text CreateText(string objectName, Transform parent, Font font, int fontSize, Color color) {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private void BindButtons() {
        RefreshConfirmButton();

        if (confirmBtn != null) {
            confirmBtn.onClick.RemoveAllListeners();
            if (_firstDiveMode) {
                confirmBtn.onClick.AddListener(ConfirmFirstDive);
            } else {
                confirmBtn.onClick.AddListener(ConfirmStart);
            }

            SetButtonLabel(confirmBtn, _firstDiveMode ? "出发" : "开始下潜", 26);
        }

        if (closeBtn != null) {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => _onClose?.Invoke());
            SetButtonLabel(closeBtn, _firstDiveMode ? "再看她一眼" : "返回", 26);
        }

        if (_firstDiveMode) {
            ApplyFirstDiveButtonSkin(confirmBtn, new Color(0.04f, 0.52f, 0.62f, 0.97f));
            ApplyFirstDiveButtonSkin(closeBtn, new Color(0.06f, 0.18f, 0.24f, 0.9f));
        }
    }

    private void ApplyFirstDiveButtonCopyIfNeeded() {
        if (!_firstDiveMode) {
            return;
        }

        SetButtonLabel(confirmBtn, "\u51fa\u53d1", 26);
        SetButtonLabel(closeBtn, "\u518d\u770b\u5979\u4e00\u773c", 26);
    }

    private void RefreshConfirmButton() {
        if (confirmBtn != null) {
            if (_firstDiveMode) {
                DiveReadinessResult readiness = BuildReadiness(GameRoot.Core?.CurrentPlayer, 1);
                confirmBtn.interactable = readiness != null && readiness.CanDive;
                return;
            }

            confirmBtn.interactable = GameRoot.Core?.Dungeon != null && GameRoot.Core.Dungeon.CanStartAtLayer(_selectedLayerID);
        }
    }

    private void ApplyFirstDiveButtonSkin(Button button, Color color) {
        if (button == null) {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null) {
            image = button.gameObject.AddComponent<Image>();
        }

        VisualUIHelper.ApplySolidColor(image, color, true);
        button.targetGraphic = image;
        Outline outline = button.GetComponent<Outline>();
        if (outline == null) {
            outline = button.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.8f, 1f, 1f, 0.28f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private void SetButtonLabel(Button button, string label, int fontSize) {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (text == null) {
            return;
        }

        if (_firstDiveMode && button == confirmBtn) {
            label = "\u51fa\u53d1";
        } else if (_firstDiveMode && button == closeBtn) {
            label = "\u518d\u770b\u5979\u4e00\u773c";
        }

        text.text = label;
        text.fontSize = fontSize;
    }

    private void ConfirmFirstDive() {
        DiveReadinessResult readiness = BuildReadiness(GameRoot.Core?.CurrentPlayer, 1);
        if (readiness == null || !readiness.CanDive) {
            PlayerProfile player = GameRoot.Core?.CurrentPlayer;
            if (player != null) {
                RefreshTexts(player);
                RefreshLayerRows(player);
            }

            RefreshConfirmButton();
            return;
        }

        _selectedLayerID = 1;
        if (_onFirstDiveDepart != null) {
            Debug.Log("[DungeonStartLayerUIController] First dive confirm requested departure callback.");
            _onFirstDiveDepart.Invoke();
            return;
        }

        PrologueFirstDiveController prologue = FindObjectOfType<PrologueFirstDiveController>(true);
        if (prologue != null) {
            Debug.LogWarning("[DungeonStartLayerUIController] First dive callback was missing; recovered via PrologueFirstDiveController.");
            prologue.RequestFirstDiveDeparture();
            return;
        }

        Debug.LogError("[DungeonStartLayerUIController] First dive confirm failed: departure callback and PrologueFirstDiveController are missing.");
    }

    private void ConfirmStart() {
        if (GameRoot.Core?.Dungeon == null) {
            return;
        }

        bool started = GameRoot.Core.Dungeon.StartRunAtLayer(_selectedLayerID);
        if (started) {
            _lastStartFailureReason = string.Empty;
            gameObject.SetActive(false);
            return;
        }

        _lastStartFailureReason = BuildReadinessSummary(BuildReadiness(GameRoot.Core.CurrentPlayer, _selectedLayerID));
        Present(_onClose);
    }

    private void ClearRows() {
        for (int i = _rows.Count - 1; i >= 0; i--) {
            if (_rows[i] != null) {
                _rows[i].SetActive(false);
            }
            DestroyRuntimeObject(_rows[i]);
        }

        _rows.Clear();

        if (listParent == null) {
            return;
        }

        for (int i = listParent.childCount - 1; i >= 0; i--) {
            Transform child = listParent.GetChild(i);
            if (child != null) {
                child.gameObject.SetActive(false);
                DestroyRuntimeObject(child.gameObject);
            }
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
