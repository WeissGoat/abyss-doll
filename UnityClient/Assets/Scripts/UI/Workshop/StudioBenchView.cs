using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum StudioTab {
    Maintenance = 0,
    Prosthetics = 1,
    Chassis = 2
}

// The workbench in the studio room (改装与维护): three tabs, 维护 / 义体 / 底盘. Rules live in MaintenanceService,
// ProstheticCraftingService and ChassisUpgradeService; this shows their state and forwards the clicks.
public class StudioBenchView : MonoBehaviour {
    public const string RootName = "StudioBench_Board";
    public static readonly string[] TabLabels = { "维护", "义体", "底盘" };

    private readonly Button[] _tabs = new Button[3];
    private Transform _content;
    private MaintenancePanel _maintenance;
    private GameObject _listRoot;
    private Text _messageText;
    private readonly List<GameObject> _rows = new List<GameObject>();
    private System.Action _onChanged;

    public StudioTab CurrentTab { get; private set; }

    public static StudioBenchView Create(Transform parent, System.Action onChanged) {
        Image board = UITheme.CreatePanel(parent, RootName, UITheme.Panel);
        UITheme.ApplyBoardSkin(board, VisualAssetService.UIPanelMainID);
        UITheme.Place(board.rectTransform, new Vector2(1f, 0.5f), new Vector2(-48f, -40f), new Vector2(900f, 800f));
        board.rectTransform.pivot = new Vector2(1f, 0.5f);
        board.rectTransform.anchoredPosition = new Vector2(-48f, -40f);
        StudioBenchView view = board.gameObject.AddComponent<StudioBenchView>();
        view._onChanged = onChanged;
        view.Build();
        return view;
    }

    // Rows fill the list's width, stacked from the top.
    public static void PlaceRow(RectTransform row, int index, float height, float gap) {
        row.anchorMin = new Vector2(0f, 1f);
        row.anchorMax = new Vector2(1f, 1f);
        row.pivot = new Vector2(0.5f, 1f);
        row.offsetMin = new Vector2(0f, -(index * (height + gap)) - height);
        row.offsetMax = new Vector2(0f, -(index * (height + gap)));
    }

    public static void PlaceTopStrip(RectTransform rect, float top, float height) {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -top - height);
        rect.offsetMax = new Vector2(0f, -top);
    }

    public void ShowTab(StudioTab tab) {
        CurrentTab = tab;
        for (int i = 0; i < _tabs.Length; i++) {
            bool active = i == (int)tab;
            Image image = (Image)_tabs[i].targetGraphic;
            UITheme.ApplyButtonSkin(image, active ? UITheme.Accent : UITheme.PanelRaised);
            _tabs[i].GetComponentInChildren<Text>().color = active ? UITheme.OnAccent : UITheme.TextPrimary;
        }

        _messageText.text = string.Empty;
        Refresh();
    }

    public void Refresh() {
        bool maintenance = CurrentTab == StudioTab.Maintenance;
        if (maintenance) {
            _maintenance.Show(_onChanged);
        } else {
            _maintenance.gameObject.SetActive(false);
        }

        _listRoot.SetActive(!maintenance);
        _messageText.gameObject.SetActive(!maintenance);
        ClearRows();
        if (CurrentTab == StudioTab.Prosthetics) {
            BuildProstheticRows();
        } else if (CurrentTab == StudioTab.Chassis) {
            BuildChassisRows();
        }
    }

    private void Build() {
        Text title = UITheme.CreateText(transform, "Title_Text", "工作台", UITheme.HeadingSize, UITheme.Accent);
        UITheme.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -26f), new Vector2(200f, 44f));
        title.rectTransform.pivot = new Vector2(0f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(36f, -26f);

        for (int i = 0; i < _tabs.Length; i++) {
            StudioTab tab = (StudioTab)i;
            Button button = UITheme.CreateButton(transform, "StudioTab_" + tab, TabLabels[i], UITheme.PanelRaised, UITheme.TextPrimary);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(140f, 50f);
            rect.anchoredPosition = new Vector2(-36f - (2 - i) * 150f, -24f);
            button.onClick.AddListener(() => ShowTab(tab));
            _tabs[i] = button;
        }

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(transform, false);
        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(36f, 30f);
        contentRect.offsetMax = new Vector2(-36f, -96f);
        _content = content.transform;

        _maintenance = MaintenancePanel.Create(_content);

        _listRoot = new GameObject("Bench_List", typeof(RectTransform));
        _listRoot.transform.SetParent(_content, false);
        RectTransform listRect = (RectTransform)_listRoot.transform;
        listRect.anchorMin = Vector2.zero;
        listRect.anchorMax = Vector2.one;
        listRect.offsetMin = new Vector2(0f, 48f);
        listRect.offsetMax = Vector2.zero;

        _messageText = UITheme.CreateText(_content, "BenchMessage_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary);
        RectTransform messageRect = _messageText.rectTransform;
        messageRect.anchorMin = Vector2.zero;
        messageRect.anchorMax = new Vector2(1f, 0f);
        messageRect.pivot = new Vector2(0.5f, 0f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = new Vector2(0f, 40f);

        ShowTab(StudioTab.Maintenance);
    }

    private void ClearRows() {
        foreach (GameObject row in _rows) {
            if (row != null) {
                row.SetActive(false);
                if (Application.isPlaying) {
                    Destroy(row);
                } else {
                    DestroyImmediate(row);
                }
            }
        }
        _rows.Clear();
    }

    private void BuildProstheticRows() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        int index = 0;
        foreach (CraftingRecipeConfig recipe in ConfigManager.CraftingRecipes.Values) {
            if (recipe == null || string.IsNullOrEmpty(recipe.TargetProstheticID)
                || !ConfigManager.Prosthetics.TryGetValue(recipe.TargetProstheticID, out ProstheticEntity prosthetic)) {
                continue;
            }

            bool equipped = doll?.EquippedProsthetics?.Contains(prosthetic.ProstheticID) == true;
            bool canCraft = ProstheticCraftingService.CanCraftAndEquip(player, doll, recipe.RecipeID, out string reason);
            string state = equipped ? "已装上" : canCraft ? DescribeCost(recipe.Cost) : $"{DescribeCost(recipe.Cost)}    · {reason}";
            string iconID = VisualAssetService.ResolveProstheticIconID(prosthetic);
            string recipeID = recipe.RecipeID;
            _rows.Add(CreateRow("Prosthetic_" + prosthetic.ProstheticID, index++, iconID,
                $"{prosthetic.Name}（{SlotName(prosthetic.SlotType)}）    {DescribeProstheticEffects(prosthetic)}", state,
                equipped ? "已装上" : "制作并装上", !equipped && canCraft, () => CraftProsthetic(recipeID)));
        }

        if (index == 0) {
            _messageText.text = "还没有能做的义体图纸。";
        }
    }

    private void BuildChassisRows() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        ChassisComponent chassis = doll?.Chassis;
        if (chassis == null) {
            return;
        }

        _rows.Add(CreateRow("Chassis_Current", 0, VisualAssetService.UIIconChassisUpgradeID,
            $"现在的底盘：{chassis.Level} 级，背包 {chassis.GridWidth}×{chassis.GridHeight} 格", "背包格子越多，能带的东西越多。",
            null, false, null));

        string nextID = chassis.UpgradeCost?.NextChassisID;
        if (string.IsNullOrEmpty(nextID) || !ConfigManager.Chassis.TryGetValue(nextID, out ChassisComponent next)) {
            _rows.Add(CreateRow("Chassis_Next", 1, VisualAssetService.UIIconLockedID, "已经是现在能做到的最好底盘。", string.Empty, null, false, null));
            return;
        }

        bool canUpgrade = ChassisUpgradeService.CanUpgrade(player, doll, out string reason);
        _rows.Add(CreateRow("Chassis_Next", 1, VisualAssetService.UIIconChassisUpgradeID,
            $"升级到 {next.Level} 级：背包 {next.GridWidth}×{next.GridHeight} 格",
            canUpgrade ? DescribeCost(chassis.UpgradeCost) : $"{DescribeCost(chassis.UpgradeCost)}    · {reason}",
            "升级底盘", canUpgrade, UpgradeChassis));
    }

    private GameObject CreateRow(string name, int index, string iconID, string line1, string line2, string action, bool interactable, System.Action onClick) {
        Image row = UITheme.CreatePanel(_listRoot.transform, name, UITheme.PanelRaised);
        PlaceRow(row.rectTransform, index, 104f, 10f);

        Image icon = UITheme.CreatePanel(row.transform, "Icon_Image", Color.white);
        UITheme.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(54f, 0f), new Vector2(72f, 72f));
        VisualUIHelper.ApplyContainSprite(icon, iconID, new Vector2(72f, 72f), Color.white, UITheme.Border, false);

        float right = action != null ? -190f : -16f;
        Text label = UITheme.CreateText(row.transform, "Label_Text", line1, UITheme.CaptionSize, interactable || action == null ? UITheme.TextPrimary : UITheme.TextMuted);
        label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(104f, 0f);
        label.rectTransform.offsetMax = new Vector2(right, -8f);
        Text detail = UITheme.CreateText(row.transform, "Detail_Text", line2, UITheme.CaptionSize - 2, UITheme.TextSecondary);
        detail.rectTransform.anchorMin = Vector2.zero;
        detail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        detail.rectTransform.offsetMin = new Vector2(104f, 8f);
        detail.rectTransform.offsetMax = new Vector2(right, 0f);

        if (action != null) {
            Button button = UITheme.CreateButton(row.transform, "Action_Button", action, interactable ? UITheme.Accent : UITheme.Border, interactable ? UITheme.OnAccent : UITheme.TextMuted, UITheme.CaptionSize);
            button.interactable = interactable;
            UITheme.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(160f, 54f));
            if (onClick != null) {
                button.onClick.AddListener(() => onClick());
            }
        }

        return row.gameObject;
    }

    private void CraftProsthetic(string recipeID) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        ProstheticCraftingResult result = ProstheticCraftingService.CraftAndEquip(player, player?.ActiveDoll, recipeID);
        string name = result.ProstheticID != null && ConfigManager.Prosthetics.TryGetValue(result.ProstheticID, out ProstheticEntity prosthetic) ? prosthetic.Name : result.ProstheticID;
        ShowResult(result.Success, result.Success ? $"做好了：{name} 已经装上。" : result.Reason);
    }

    private void UpgradeChassis() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        ChassisUpgradeResult result = ChassisUpgradeService.Upgrade(player, doll);
        if (result.Success && result.RuntimeGridRebuilt && doll?.Chassis != null) {
            FindObjectOfType<GridGenerator>()?.GenerateGrid(doll.Chassis);
        }

        ShowResult(result.Success, result.Success ? $"底盘升级好了：背包 {result.PreviousWidth}×{result.PreviousHeight} → {result.NewWidth}×{result.NewHeight}。" : result.Reason);
    }

    private void ShowResult(bool success, string message) {
        Refresh();
        _messageText.text = message;
        _messageText.color = success ? UITheme.Positive : UITheme.Warning;
        if (success) {
            GameAudio.PlaySfx("coin");
        }

        _onChanged?.Invoke();
    }

    public static string DescribeCost(CraftingCost cost) {
        List<string> parts = new List<string>();
        if (cost != null && cost.Money > 0) {
            parts.Add($"{cost.Money}G");
        }

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        foreach (CraftingRequirement requirement in cost?.RequiredItems ?? new List<CraftingRequirement>()) {
            if (requirement == null) {
                continue;
            }

            string name = ConfigManager.Items.TryGetValue(requirement.ConfigID, out ItemEntity item) ? item.Name : requirement.ConfigID;
            int owned = WorkshopCostService.CountOwnedItems(player, requirement.ConfigID);
            parts.Add($"{name} {owned}/{requirement.Count}");
        }

        return parts.Count == 0 ? "免费" : string.Join(" + ", parts);
    }

    private static string SlotName(string slot) {
        switch (slot) {
            case "Arm": return "手臂";
            case "Core": return "核心";
            case "Leg": return "腿部";
            case "Eye": return "眼部";
            default: return slot;
        }
    }

    public static string DescribeProstheticEffects(ProstheticEntity prosthetic) {
        StringBuilder text = new StringBuilder();
        foreach (EffectData effect in prosthetic?.Effects ?? new List<EffectData>()) {
            if (effect == null) {
                continue;
            }

            float value = effect.Params != null && effect.Params.Length > 0 ? effect.Params[0] : 0f;
            string part;
            switch (effect.EffectID) {
                case "DamageMultiplier":
                    part = $"{(effect.Target == "Melee" ? "近战" : string.Empty)}伤害 +{value * 100f:0}%";
                    break;
                case "RestoreSANOnCombatEnd":
                    part = $"每场战斗后 SAN +{value:0}";
                    break;
                case "AddRuntimeShieldOnTurnStart":
                    part = $"每回合开始护盾 +{value:0}";
                    break;
                default:
                    continue;
            }

            if (text.Length > 0) {
                text.Append("，");
            }
            text.Append(part);
        }

        return text.ToString();
    }
}
