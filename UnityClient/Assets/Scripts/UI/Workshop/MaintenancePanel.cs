using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// 维护 (GDD_08 ④, zero-time): Zero's HP / SAN / wear / corruption and every maintenance option from config with
// its cost, effect and why it cannot be done right now. Rules live in MaintenanceService; this shows and forwards.
public class MaintenancePanel : MonoBehaviour {
    public const string PanelName = "MaintenancePanel_Runtime";

    private Text _statusText;
    private Text _messageText;
    private Transform _listRoot;
    private Action _onChanged;
    private readonly List<GameObject> _rows = new List<GameObject>();

    public static MaintenancePanel Create(Transform parent) {
        GameObject root = UITheme.CreateModal(parent, PanelName, new Vector2(820f, 660f), 550);
        MaintenancePanel panel = root.AddComponent<MaintenancePanel>();
        panel.Build(root.transform.Find("Dialog"));
        root.SetActive(false);
        return panel;
    }

    public void Show(Action onChanged) {
        _onChanged = onChanged;
        _messageText.text = "维护不花时间。磨损或侵蚀到 90 就不能下潜。";
        _messageText.color = UITheme.TextSecondary;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    public static string DescribeStrain(DollEntity doll) {
        if (doll == null) {
            return string.Empty;
        }

        return $"磨损 {doll.Status.WearAndTear:0}   侵蚀 {doll.Status.Corruption:0}";
    }

    public static Color StrainColor(float value) {
        return value >= DiveReadinessService.ExtremeWearThreshold ? UITheme.Danger
            : value >= DiveReadinessService.HeavyWearWarningThreshold ? UITheme.Warning
            : UITheme.TextPrimary;
    }

    private void Refresh() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        if (doll == null) {
            return;
        }

        _statusText.text = $"零号  HP {doll.Status.HP_Current}/{doll.Status.HP_Max}   SAN {doll.Status.SAN_Current}/{doll.Status.SAN_Max}   "
            + $"{DescribeStrain(doll)}   资金 {player.Money}G";
        _statusText.color = StrainColor(Mathf.Max(doll.Status.WearAndTear, doll.Status.Corruption));

        foreach (GameObject row in _rows) {
            if (row != null) {
                row.SetActive(false);
                Destroy(row);
            }
        }
        _rows.Clear();

        List<MaintenanceConfig> options = ConfigManager.MaintenanceConfigs.Values
            .Where(config => config != null)
            .OrderBy(config => config.Cost?.Money ?? 0)
            .ThenBy(config => config.MaintenanceID)
            .ToList();
        for (int i = 0; i < options.Count; i++) {
            _rows.Add(CreateRow(player, doll, options[i], i));
        }
    }

    private GameObject CreateRow(PlayerProfile player, DollEntity doll, MaintenanceConfig config, int index) {
        Image row = UITheme.CreatePanel(_listRoot, "Maintenance_" + config.MaintenanceID, UITheme.PanelRaised);
        UITheme.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -index * 96f), new Vector2(760f, 88f));

        bool canApply = MaintenanceService.CanApply(player, doll, config.MaintenanceID, out string reason);
        Text label = UITheme.CreateText(row.transform, "Label_Text", $"{config.Name}    {DescribeCost(config)}\n{DescribeEffects(config)}{(canApply ? string.Empty : "    · " + reason)}", UITheme.CaptionSize, canApply ? UITheme.TextPrimary : UITheme.TextMuted);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(18f, 6f);
        label.rectTransform.offsetMax = new Vector2(-170f, -6f);

        Button apply = UITheme.CreateButton(row.transform, "Apply_Button", "维护", canApply ? UITheme.Accent : UITheme.Border, canApply ? UITheme.Background : UITheme.TextMuted);
        apply.interactable = canApply;
        UITheme.Place((RectTransform)apply.transform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(130f, 52f));
        string id = config.MaintenanceID;
        apply.onClick.AddListener(() => HandleApply(id));
        return row.gameObject;
    }

    private void HandleApply(string maintenanceID) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        MaintenanceApplicationResult result = MaintenanceService.Apply(player, player?.ActiveDoll, maintenanceID);
        if (result.Success) {
            StringBuilder text = new StringBuilder("完成：");
            foreach (MaintenanceEffectApplication effect in result.AppliedEffects) {
                text.Append($"{TargetName(effect.TargetState)} {effect.Before:0} → {effect.After:0}  ");
            }
            _messageText.text = text.ToString().TrimEnd();
            _messageText.color = UITheme.Positive;
        } else {
            _messageText.text = result.Reason;
            _messageText.color = UITheme.Warning;
        }

        Refresh();
        _onChanged?.Invoke();
    }

    private static string DescribeCost(MaintenanceConfig config) {
        List<string> parts = new List<string>();
        if (config.Cost != null && config.Cost.Money > 0) {
            parts.Add($"{config.Cost.Money}G");
        }

        if (config.Cost?.RequiredItems != null) {
            foreach (var requirement in config.Cost.RequiredItems) {
                if (requirement == null) {
                    continue;
                }

                string name = ConfigManager.Items.TryGetValue(requirement.ConfigID, out ItemEntity item) ? item.Name : requirement.ConfigID;
                parts.Add($"{name}×{requirement.Count}");
            }
        }

        return parts.Count == 0 ? "免费" : string.Join(" + ", parts);
    }

    private static string DescribeEffects(MaintenanceConfig config) {
        List<string> parts = new List<string>();
        foreach (MaintenanceEffectConfig effect in config.Effects ?? new List<MaintenanceEffectConfig>()) {
            if (effect == null || !Enum.TryParse(effect.TargetState, true, out MaintenanceTargetState target)) {
                continue;
            }

            bool lowers = target == MaintenanceTargetState.Wear || target == MaintenanceTargetState.Corruption;
            parts.Add($"{TargetName(target)} {(lowers ? "-" : "+")}{effect.Amount:0}");
        }

        return string.Join("，", parts);
    }

    private static string TargetName(MaintenanceTargetState target) {
        switch (target) {
            case MaintenanceTargetState.Wear: return "磨损";
            case MaintenanceTargetState.Corruption: return "侵蚀";
            case MaintenanceTargetState.HP: return "HP";
            case MaintenanceTargetState.SAN: return "SAN";
            default: return target.ToString();
        }
    }

    private void Build(Transform dialog) {
        Text title = UITheme.CreateText(dialog, "Title_Text", "维护零号", UITheme.HeadingSize, UITheme.Accent);
        UITheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(400f, 44f));
        _statusText = UITheme.CreateText(dialog, "Status_Text", string.Empty, UITheme.CaptionSize + 1, UITheme.TextPrimary);
        UITheme.Place(_statusText.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -68f), new Vector2(760f, 32f));

        GameObject list = new GameObject("Option_List", typeof(RectTransform));
        list.transform.SetParent(dialog, false);
        UITheme.Place((RectTransform)list.transform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(760f, 400f));
        _listRoot = list.transform;

        _messageText = UITheme.CreateText(dialog, "Message_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary);
        UITheme.Place(_messageText.rectTransform, new Vector2(0f, 0f), new Vector2(28f, 92f), new Vector2(760f, 40f));

        Button close = UITheme.CreateButton(dialog, "Close_Button", "关闭", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(close.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(220f, 58f));
        close.onClick.AddListener(() => gameObject.SetActive(false));
    }
}
