using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 设置 (M3 shell): volume, fullscreen or window, resolution. Values live in GameSettingsService; this forwards clicks.
public class SettingsPanel : MonoBehaviour {
    public const string PanelName = "SettingsPanel_Runtime";
    public const string ApplyLabel = "应用显示设置";

    private static SettingsPanel _active;
    public static bool IsShowing => _active != null && _active.gameObject.activeSelf;

    private Text _volumeText;
    private Text _modeText;
    private Text _sizeText;
    private List<Vector2Int> _sizes;
    private int _sizeIndex;
    private bool _fullscreen;

    public static void Show(Transform parent) {
        if (_active == null) {
            GameObject root = UITheme.CreateModal(parent, PanelName, new Vector2(760f, 520f), 900);
            _active = root.AddComponent<SettingsPanel>();
            _active.Build(root.transform.Find("Dialog"));
        } else if (_active.transform.parent != parent) {
            _active.transform.SetParent(parent, false);
        }

        _active.Open();
    }

    public static void Hide() {
        if (_active != null) {
            _active.gameObject.SetActive(false);
        }
    }

    private void OnDestroy() {
        if (_active == this) {
            _active = null;
        }
    }

    private void Build(Transform dialog) {
        Text title = UITheme.CreateText(dialog, "Title_Text", "设置", UITheme.TitleSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 50f));

        _volumeText = AddRow(dialog, "音量", -130f, () => ChangeVolume(-GameSettingsService.VolumeStep), () => ChangeVolume(GameSettingsService.VolumeStep));
        Text note = UITheme.CreateText(dialog, "VolumeNote_Text", "游戏暂时还没有声音，M4 接入音频。", UITheme.CaptionSize - 2, UITheme.TextMuted, TextAnchor.MiddleCenter);
        UITheme.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(600f, 26f));
        _modeText = AddRow(dialog, "显示", -230f, ToggleMode, ToggleMode);
        _sizeText = AddRow(dialog, "分辨率", -310f, () => ChangeSize(-1), () => ChangeSize(1));

        Button apply = UITheme.CreateButton(dialog, "ApplyDisplay_Button", ApplyLabel, UITheme.Accent, UITheme.Background);
        UITheme.Place((RectTransform)apply.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 32f), new Vector2(260f, 60f));
        apply.onClick.AddListener(ApplyDisplay);
        Button close = UITheme.CreateButton(dialog, "CloseSettings_Button", "关闭", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(close.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(150f, 32f), new Vector2(260f, 60f));
        close.onClick.AddListener(Hide);
    }

    private Text AddRow(Transform dialog, string label, float y, UnityEngine.Events.UnityAction onLeft, UnityEngine.Events.UnityAction onRight) {
        Text name = UITheme.CreateText(dialog, label + "_Label", label, UITheme.BodySize, UITheme.TextSecondary, TextAnchor.MiddleLeft);
        UITheme.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(-230f, y), new Vector2(180f, 44f));
        Button left = UITheme.CreateButton(dialog, label + "_Left", "◀", UITheme.PanelRaised, UITheme.Accent);
        UITheme.Place((RectTransform)left.transform, new Vector2(0.5f, 1f), new Vector2(-60f, y), new Vector2(56f, 44f));
        left.onClick.AddListener(onLeft);
        Text value = UITheme.CreateText(dialog, label + "_Value", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(90f, y), new Vector2(220f, 44f));
        Button right = UITheme.CreateButton(dialog, label + "_Right", "▶", UITheme.PanelRaised, UITheme.Accent);
        UITheme.Place((RectTransform)right.transform, new Vector2(0.5f, 1f), new Vector2(240f, y), new Vector2(56f, 44f));
        right.onClick.AddListener(onRight);
        return value;
    }

    private void Open() {
        _sizes = GameSettingsService.AvailableResolutions();
        _sizeIndex = Mathf.Max(0, _sizes.IndexOf(GameSettingsService.CurrentSize));
        _fullscreen = GameSettingsService.IsFullscreen;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    private void Refresh() {
        _volumeText.text = $"{GameSettingsService.VolumePercent}%";
        _modeText.text = _fullscreen ? "全屏" : "窗口";
        _sizeText.text = _sizes.Count > 0 ? GameSettingsService.DescribeSize(_sizes[_sizeIndex]) : "-";
    }

    private void ChangeVolume(int delta) {
        GameSettingsService.SetVolumePercent(GameSettingsService.VolumePercent + delta);
        Refresh();
    }

    private void ToggleMode() {
        _fullscreen = !_fullscreen;
        Refresh();
    }

    private void ChangeSize(int delta) {
        if (_sizes.Count == 0) {
            return;
        }

        _sizeIndex = (_sizeIndex + delta + _sizes.Count) % _sizes.Count;
        Refresh();
    }

    private void ApplyDisplay() {
        if (_sizes.Count > 0) {
            GameSettingsService.ApplyDisplay(_sizes[_sizeIndex], _fullscreen);
            PlayerToast.Show($"显示：{(_fullscreen ? "全屏" : "窗口")} {GameSettingsService.DescribeSize(_sizes[_sizeIndex])}", PlayerToastKind.Info);
        }
    }
}
