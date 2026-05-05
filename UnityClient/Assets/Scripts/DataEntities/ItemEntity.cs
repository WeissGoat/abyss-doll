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

    public ItemGridComponent Grid;
    public ItemCombatComponent Combat;
    public List<string> Tags = new List<string>();
}

[Serializable]
public class ItemGridComponent {
    public int[][] Shape;
    public int Rotation;
    public int GridCost;
    public int[] CurrentPos = new int[2] { 0, 0 };
}

[Serializable]
public class EffectData {
    public string EffectID;
    public int Level;
    public string Target;
    public float[] Params;
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
            return string.IsNullOrEmpty(payload) ? "Passive" : $"Passive {payload}";
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

    private static string BuildPayloadSummary(ItemEntity item) {
        if (item?.Combat == null) {
            return string.Empty;
        }

        switch (item.Combat.DamageType) {
            case nameof(DamageType.Physical):
            case nameof(DamageType.Energy):
                return $"{item.Combat.BaseValue} DMG";
            case nameof(DamageType.Heal):
                return $"+{item.Combat.BaseValue} HP";
            case nameof(DamageType.RestoreSAN):
                return $"+{item.Combat.BaseValue} SAN";
            case nameof(DamageType.Shield):
                if (item.Combat.TriggerType == TriggerType.Passive.ToString()) {
                    return "+Shield/Turn";
                }
                if (item.Combat.Effects != null && item.Combat.Effects.Count > 0 && item.Combat.Effects[0].Params != null && item.Combat.Effects[0].Params.Length > 0) {
                    return $"+{Mathf.RoundToInt(item.Combat.Effects[0].Params[0])} Shield";
                }
                return "+Shield";
        }

        if (item.Combat.Effects != null && item.Combat.Effects.Count > 0) {
            EffectData firstEffect = item.Combat.Effects[0];
            if (firstEffect?.Params != null && firstEffect.Params.Length > 0) {
                if (firstEffect.EffectID == "DamageMultiplier") {
                    return $"+{Mathf.RoundToInt(firstEffect.Params[0] * 100f)}% DMG";
                }
                if (firstEffect.EffectID.Contains("Shield")) {
                    return $"+{Mathf.RoundToInt(firstEffect.Params[0])} Shield";
                }
            }
        }

        return string.Empty;
    }
}
