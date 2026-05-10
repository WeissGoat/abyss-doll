using UnityEngine;

public static class EffectModifierResolver {
    public static EffectModifierResolution Resolve(EffectModifierContext context) {
        EffectModifierResolution resolution = new EffectModifierResolution {
            BaseValue = context?.BaseValue ?? 0f,
            FinalValue = context?.BaseValue ?? 0f
        };

        if (context == null) {
            return resolution;
        }

        CollectBackpackItemModifiers(context, resolution);
        CollectProstheticModifiers(context, resolution);
        ApplyModifiers(context, resolution);
        return resolution;
    }

    private static void CollectBackpackItemModifiers(EffectModifierContext context, EffectModifierResolution resolution) {
        BackpackGrid grid = context.BackpackGrid;
        if (grid?.ContainedItems == null) {
            return;
        }

        foreach (ItemEntity item in grid.ContainedItems) {
            if (item?.Combat?.Effects == null) {
                continue;
            }

            context.SourceItem = item;
            context.SourceProsthetic = null;
            CollectEffects(context, resolution, item.Combat.Effects);
        }

        context.SourceItem = null;
    }

    private static void CollectProstheticModifiers(EffectModifierContext context, EffectModifierResolution resolution) {
        DollEntity doll = context.ActiveDoll;
        if (doll?.EquippedProsthetics == null) {
            return;
        }

        foreach (string prostheticID in doll.EquippedProsthetics) {
            if (!ConfigManager.Prosthetics.TryGetValue(prostheticID, out ProstheticEntity prosthetic) || prosthetic?.Effects == null) {
                continue;
            }

            context.SourceItem = null;
            context.SourceProsthetic = prosthetic;
            CollectEffects(context, resolution, prosthetic.Effects);
        }

        context.SourceProsthetic = null;
    }

    private static void CollectEffects(EffectModifierContext context, EffectModifierResolution resolution, System.Collections.Generic.List<EffectData> effects) {
        foreach (EffectData effectData in effects) {
            EffectBase effect = EffectFactory.CreateEffect(effectData);
            effect?.CollectModifiers(context, resolution.Modifiers);
        }
    }

    private static void ApplyModifiers(EffectModifierContext context, EffectModifierResolution resolution) {
        float current = resolution.BaseValue;
        current = ApplyOperation(current, resolution, EffectModifierOperation.AddFlat);
        current = ApplyOperation(current, resolution, EffectModifierOperation.AddPercent);
        current = ApplyOperation(current, resolution, EffectModifierOperation.Multiply);
        context.CurrentValue = current;

        resolution.FinalValue = current;

        if (resolution.Modifiers.Count > 0) {
            Debug.Log($"[EffectModifierResolver] Resolved modifiers. Trigger={context.Trigger}, Resource={context.Resource}, Base={resolution.BaseValue}, Final={resolution.FinalValue}, Count={resolution.Modifiers.Count}");
        }
    }

    private static float ApplyOperation(float current, EffectModifierResolution resolution, EffectModifierOperation operation) {
        foreach (EffectModifier modifier in resolution.Modifiers) {
            if (modifier.Operation != operation) {
                continue;
            }

            switch (operation) {
                case EffectModifierOperation.AddPercent:
                    current *= 1f + modifier.Value;
                    break;
                case EffectModifierOperation.Multiply:
                    current *= modifier.Value;
                    break;
                case EffectModifierOperation.AddFlat:
                default:
                    current += modifier.Value;
                    break;
            }
        }

        return current;
    }
}
