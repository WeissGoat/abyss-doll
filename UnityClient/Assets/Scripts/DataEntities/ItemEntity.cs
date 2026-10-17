using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemEntity {
    public string InstanceID;
    public string ConfigID;
    public string Name;
    public string ItemType;
    public string Rarity;
    public int BaseValue;
    public string IconID;
    public string WorldVisualID;
    public string UseVFXID;
    public string UseSFXID;
    public string RarityFrameID;
    // One line of flavour for the hover card (config Description). The card reads it from the config template, so items
    // in saves made before it existed show it too.
    public string Description;

    public ItemGridComponent Grid;
    public ItemCombatComponent Combat;
    public List<string> Tags = new List<string>();
    public ItemOwnerScope OwnerScope = ItemOwnerScope.Unknown;
    public ItemContainerType ContainerType = ItemContainerType.Unknown;
    public float Durability = 1f;
    public int CorruptionLevel;
    public List<string> DynamicTags = new List<string>();
    public bool IsBinding;
    public ItemEconomyComponent Economy;
}

[Serializable]
public class ItemEconomyComponent {
    // Market tags: rumors and showcase adjacency match on these.
    public List<string> ValueTags = new List<string>();
    public string ContrabandRule;
}

[Serializable]
public class ItemGridComponent {
    public int[][] Shape;
    public bool CanRotate;
    public int RotationSteps = 1;
    public int Rotation;
    public int GridCost;
    public int[] CurrentPos = new int[2] { 0, 0 };
}

[Serializable]
public class EffectData {
    public string EffectID;
    public int Level;
    public string Target;
    public string Trigger;
    public string Resource;
    public string Operation;
    public float[] Params;
    // ApplyStatus: Poison / Vulnerable / Weak.
    public string Status;
    // PerAdjacentTag, ChargeNeighbor, GainAP on a neighbour's use: only items carrying this tag count.
    public string Tag;
}

[Serializable]
public class ItemCombatComponent {
    public string TriggerType;
    public int APCost;
    public string DamageType;
    public int BaseValue;
    public List<EffectData> Effects = new List<EffectData>();

    [NonSerialized]
    public float RuntimeDamage;
}

public static class ItemPresentationRules {
    public static string BuildCompactSummary(ItemEntity item) {
        if (item == null) {
            return string.Empty;
        }

        if (item.Combat != null && item.Combat.TriggerType == TriggerType.Manual.ToString()) {
            string payload = BuildPayloadSummary(item);
            if (!string.IsNullOrEmpty(payload) && item.Combat.APCost > 0) {
                return $"{item.Combat.APCost}AP {payload}";
            }

            if (item.Combat.APCost > 0) {
                return $"{item.Combat.APCost}AP";
            }
        }

        if (item.Combat != null && item.Combat.TriggerType == TriggerType.Passive.ToString()) {
            string payload = BuildPayloadSummary(item);
            return string.IsNullOrEmpty(payload) ? "被动" : $"被动 {payload}";
        }

        if (item.BaseValue > 0) {
            return $"{item.BaseValue}G";
        }

        return string.Empty;
    }

    public static string BuildUseHint(ItemEntity item) {
        if (item == null) {
            return string.Empty;
        }

        if (item.Combat == null) {
            return item.BaseValue > 0 ? $"可带出卖出，价值 {item.BaseValue}G。" : "当前没有可用的主动效果。";
        }

        string payload = BuildPayloadSummary(item);
        if (item.Combat.TriggerType == TriggerType.Manual.ToString()) {
            if (item.ItemType == nameof(ItemType.Consumable)) {
                return string.IsNullOrEmpty(payload)
                    ? "点击后会立即使用并消耗。"
                    : $"点击后立即使用并消耗。效果：{payload}。";
            }

            if (item.ItemType == nameof(ItemType.Weapon)
                && (item.Combat.DamageType == nameof(DamageType.Physical) || item.Combat.DamageType == nameof(DamageType.Energy))) {
                return string.IsNullOrEmpty(payload)
                    ? $"点击后选择敌方目标，消耗 {item.Combat.APCost} AP。"
                    : $"点击后选择敌方目标，消耗 {item.Combat.APCost} AP。效果：{payload}。";
            }

            return string.IsNullOrEmpty(payload)
                ? $"手动使用，消耗 {item.Combat.APCost} AP。"
                : $"手动使用，消耗 {item.Combat.APCost} AP。效果：{payload}。";
        }

        if (item.Combat.TriggerType == TriggerType.Passive.ToString()) {
            return string.IsNullOrEmpty(payload)
                ? "放在背包中即可生效。"
                : $"放在背包中即可生效。效果：{payload}。";
        }

        return string.Empty;
    }

    // The hover card for an item anywhere on screen: what it is, what it does, how big it is and what it is worth.
    // Items have no written description yet, so every line is read from the item data.
    public static ItemTooltipText BuildTooltip(ItemEntity item) {
        if (item == null) {
            return new ItemTooltipText { Title = string.Empty, Body = string.Empty };
        }

        List<string> lines = new List<string> { $"{RarityLabel(item.Rarity)} · {TypeLabel(item)}" };
        string useHint = BuildUseHint(item);
        if (!string.IsNullOrEmpty(useHint)) {
            lines.Add(useHint);
        }

        if (item.Combat?.Effects != null) {
            foreach (EffectData effect in item.Combat.Effects) {
                string effectLine = DescribeEffect(effect, item, item.Grid?.Rotation ?? 0);
                if (!string.IsNullOrEmpty(effectLine)) {
                    lines.Add(effectLine);
                }
            }
        }

        if (item.Grid?.Shape != null && BackpackGrid.TryGetRotatedBounds(item, item.Grid.Rotation, out int columns, out int rows)) {
            lines.Add(BackpackGrid.CanRotateItem(item)
                ? $"占 {columns}×{rows} 格，可旋转（右键或 R）"
                : $"占 {columns}×{rows} 格，不能旋转");
        }

        if (item.Durability < 0.999f) {
            lines.Add($"耐久 {Mathf.RoundToInt(item.Durability * 100f)}%");
        }

        if (item.CorruptionLevel > 0) {
            lines.Add($"污染 {item.CorruptionLevel}");
        }

        if (item.BaseValue > 0) {
            lines.Add($"价值 {item.BaseValue}G");
        }

        return new ItemTooltipText { Title = item.Name, Flavor = ResolveDescription(item), Body = string.Join("\n", lines) };
    }

    public static string ResolveDescription(ItemEntity item) {
        if (item == null) {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(item.ConfigID) && ConfigManager.Items != null
            && ConfigManager.Items.TryGetValue(item.ConfigID, out ItemEntity template) && !string.IsNullOrEmpty(template?.Description)) {
            return template.Description;
        }

        return item.Description ?? string.Empty;
    }

    public static string TypeLabel(ItemEntity item) {
        switch (item?.ItemType) {
            case nameof(ItemType.Weapon): return "武器";
            case nameof(ItemType.Armor): return "防具";
            case nameof(ItemType.Anchor): return "护符";
            case nameof(ItemType.Consumable): return "消耗品";
            case nameof(ItemType.Loot): return "战利品";
            case nameof(ItemType.QuestItem): return "委托物品";
            default: return "材料";
        }
    }

    private static string RarityLabel(string rarity) {
        switch (rarity) {
            case nameof(ItemRarity.Uncommon): return "优良";
            case nameof(ItemRarity.Rare): return "稀有";
            case nameof(ItemRarity.Epic): return "史诗";
            case nameof(ItemRarity.Cursed): return "诅咒";
            default: return "普通";
        }
    }

    // Effects the use hint does not already cover: adjacency boosts, per-turn shields and dive cost changes.
    private static string DescribeEffect(EffectData effect, ItemEntity item, int rotation) {
        if (effect?.Params == null || effect.Params.Length == 0) {
            return string.Empty;
        }

        float value = effect.Params[0];
        switch (effect.EffectID) {
            case "DamageMultiplier":
                return $"{DirectionLabel(effect.Target, rotation)}伤害 +{Mathf.RoundToInt(value * 100f)}%";
            case "AddRuntimeShieldOnTurnStart":
                return $"每回合开始获得 {Mathf.RoundToInt(value)} 护盾";
            case "ModifyResourceCost":
                if (effect.Trigger != nameof(EffectTriggerType.OnDungeonMoveCost)) {
                    return string.Empty;
                }

                string resource = string.IsNullOrEmpty(effect.Resource) ? "消耗" : effect.Resource;
                string amount = effect.Operation == nameof(EffectModifierOperation.AddPercent)
                    ? $"{(value >= 0f ? "+" : string.Empty)}{Mathf.RoundToInt(value * 100f)}%"
                    : effect.Operation == nameof(EffectModifierOperation.Multiply)
                        ? $"×{value:0.##}"
                        : $"{(value >= 0f ? "+" : string.Empty)}{Mathf.RoundToInt(value)}";
                return $"放在背包里时，地图上每走一步 {resource} 消耗 {amount}";
            default:
                // Newer effects describe themselves (EffectBase.Describe).
                return EffectFactory.CreateEffect(effect)?.Describe(item, rotation) ?? string.Empty;
        }
    }

    // Directions turn with the item, so they are named as they read at the item's current rotation.
    private static string DirectionLabel(string target, int rotation) {
        if (!System.Enum.TryParse(target, out TargetDirection direction)) {
            return "自身";
        }

        switch (BackpackGrid.RotateDirection(direction, rotation)) {
            case TargetDirection.Right: return "紧贴右侧的物品";
            case TargetDirection.Left: return "紧贴左侧的物品";
            case TargetDirection.Up: return "紧贴上方的物品";
            case TargetDirection.Down: return "紧贴下方的物品";
            case TargetDirection.AllAdjacent: return "四周紧贴的物品";
            case TargetDirection.Global: return "背包里所有物品";
            default: return "自身";
        }
    }

    private static string BuildPayloadSummary(ItemEntity item) {
        if (item?.Combat == null) {
            return string.Empty;
        }

        switch (item.Combat.DamageType) {
            case nameof(DamageType.Physical):
            case nameof(DamageType.Energy):
                // RuntimeDamage carries adjacency bonuses while the item sits in the backpack; show the boost.
                int baseDamage = Mathf.RoundToInt(item.Combat.BaseValue);
                int runtimeDamage = (int)item.Combat.RuntimeDamage;
                return runtimeDamage > 0 && runtimeDamage != baseDamage
                    ? $"伤害{runtimeDamage}({(runtimeDamage > baseDamage ? "+" : string.Empty)}{runtimeDamage - baseDamage})"
                    : $"伤害{baseDamage}";
            case nameof(DamageType.Heal):
                return $"+{item.Combat.BaseValue} HP";
            case nameof(DamageType.RestoreSAN):
                return $"+{item.Combat.BaseValue} SAN";
            case nameof(DamageType.Shield):
                if (item.Combat.TriggerType == TriggerType.Passive.ToString()) {
                    return "每回合护盾";
                }
                if (item.Combat.Effects != null && item.Combat.Effects.Count > 0 && item.Combat.Effects[0].Params != null && item.Combat.Effects[0].Params.Length > 0) {
                    return $"护盾+{Mathf.RoundToInt(item.Combat.Effects[0].Params[0])}";
                }
                return "护盾";
        }

        if (item.Combat.Effects != null && item.Combat.Effects.Count > 0) {
            EffectData firstEffect = item.Combat.Effects[0];
            if (firstEffect?.Params != null && firstEffect.Params.Length > 0) {
                if (firstEffect.EffectID == "DamageMultiplier") {
                    return $"伤害+{Mathf.RoundToInt(firstEffect.Params[0] * 100f)}%";
                }
                if (firstEffect.EffectID.Contains("Shield")) {
                    return $"护盾+{Mathf.RoundToInt(firstEffect.Params[0])}";
                }
                if (firstEffect.EffectID == "ModifyResourceCost") {
                    return BuildResourceCostModifierSummary(firstEffect);
                }
            }
        }

        return string.Empty;
    }

    private static string BuildResourceCostModifierSummary(EffectData effect) {
        if (effect == null || effect.Params == null || effect.Params.Length == 0) {
            return string.Empty;
        }

        string resource = string.IsNullOrEmpty(effect.Resource) ? "Cost" : effect.Resource;
        float value = effect.Params[0];
        string sign = value >= 0f ? "+" : string.Empty;
        string suffix = effect.Trigger == nameof(EffectTriggerType.OnDungeonMoveCost) ? "/Node" : string.Empty;

        switch (effect.Operation) {
            case nameof(EffectModifierOperation.AddPercent):
                return $"{resource} {sign}{Mathf.RoundToInt(value * 100f)}%{suffix}";
            case nameof(EffectModifierOperation.Multiply):
                return $"{resource} x{value:0.##}{suffix}";
            case nameof(EffectModifierOperation.AddFlat):
            default:
                return $"{resource} {sign}{Mathf.RoundToInt(value)}{suffix}";
        }
    }
}

public struct ItemTooltipText {
    public string Title;
    public string Flavor;
    public string Body;
}
