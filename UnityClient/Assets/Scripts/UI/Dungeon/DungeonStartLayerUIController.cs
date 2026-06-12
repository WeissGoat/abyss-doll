using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonStartLayerUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Transform listParent;
    public Button confirmBtn;
    public Button closeBtn;
    public Image titleDividerImage;

    private readonly List<GameObject> _rows = new List<GameObject>();
    private int _selectedLayerID = 1;
    private Action _onClose;
    private string _lastStartFailureReason;

    public void Present(Action onClose) {
        _onClose = onClose;

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null || GameRoot.Core.Dungeon == null) {
            return;
        }

        NormalizeSelection(player);
        RefreshTexts(player);
        RefreshLayerRows(player);
        BindButtons();
    }

    private void NormalizeSelection(PlayerProfile player) {
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

    private void RefreshLayerRows(PlayerProfile player) {
        ClearRows();
        if (listParent == null) {
            return;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        List<int> layerIDs = new List<int>(ConfigManager.Dungeons.Keys);
        layerIDs.Sort();

        foreach (int layerID in layerIDs) {
            if (!ConfigManager.Dungeons.TryGetValue(layerID, out DungeonConfig config)) {
                continue;
            }

            CreateLayerRow(layerID, config, player, defaultFont);
        }
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
        layerText.text = $"第 {layerID} 层  {config.Name}";
        layerText.rectTransform.sizeDelta = new Vector2(330f, 66f);

        Text stateText = CreateText("State_Text", row.transform, font, 20, canStart ? new Color(0.78f, 1f, 0.82f) : new Color(1f, 0.68f, 0.58f));
        stateText.text = BuildLayerStateText(readiness, selected);
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
            confirmBtn.onClick.AddListener(ConfirmStart);
        }

        if (closeBtn != null) {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => _onClose?.Invoke());
        }
    }

    private void RefreshConfirmButton() {
        if (confirmBtn != null) {
            confirmBtn.interactable = GameRoot.Core?.Dungeon != null && GameRoot.Core.Dungeon.CanStartAtLayer(_selectedLayerID);
        }
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
            DestroyRuntimeObject(_rows[i]);
        }

        _rows.Clear();
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
