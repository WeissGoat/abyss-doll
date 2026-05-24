using System.Collections.Generic;
using UnityEngine;

public class MaintenanceEffectApplication {
    public MaintenanceTargetState TargetState;
    public float Before;
    public float After;
    public float Delta;
}

public class MaintenanceApplicationResult {
    public bool Success;
    public string Reason;
    public string MaintenanceID;
    public int MoneySpent;
    public List<ItemEntity> ConsumedItems = new List<ItemEntity>();
    public List<MaintenanceEffectApplication> AppliedEffects = new List<MaintenanceEffectApplication>();
}

public static class MaintenanceService {
    public static bool CanApply(PlayerProfile player, DollEntity doll, string maintenanceID, out string reason) {
        reason = string.Empty;
        if (!TryResolveContext(player, doll, maintenanceID, out MaintenanceConfig config, out reason)) {
            return false;
        }

        if (!ValidateEffects(config, maintenanceID, out reason)) {
            return false;
        }

        if (!WorkshopCostService.CanAfford(config.Cost, player, out reason)) {
            return false;
        }

        return true;
    }

    public static MaintenanceApplicationResult Apply(PlayerProfile player, DollEntity doll, string maintenanceID) {
        MaintenanceApplicationResult result = new MaintenanceApplicationResult {
            MaintenanceID = maintenanceID
        };

        if (!CanApply(player, doll, maintenanceID, out string reason)) {
            result.Success = false;
            result.Reason = reason;
            return result;
        }

        MaintenanceConfig config = ConfigManager.MaintenanceConfigs[maintenanceID];
        if (!WorkshopCostService.TryPayCost(config.Cost, player, $"Maintenance:{maintenanceID}", out WorkshopCostPaymentResult paymentResult)) {
            result.Success = false;
            result.Reason = paymentResult.Reason;
            return result;
        }

        result.MoneySpent = paymentResult.MoneySpent;
        result.ConsumedItems.AddRange(paymentResult.ConsumedItems);

        foreach (MaintenanceEffectConfig effect in config.Effects) {
            if (!TryApplyEffect(doll, effect, out MaintenanceEffectApplication application, out string effectReason)) {
                Debug.LogWarning($"[MaintenanceService] Skipped effect on [{maintenanceID}]: {effectReason}");
                continue;
            }

            result.AppliedEffects.Add(application);
        }

        result.Success = true;
        result.Reason = string.Empty;
        Debug.Log($"[MaintenanceService] Applied [{maintenanceID}]. Effects={result.AppliedEffects.Count}, Money={result.MoneySpent}, Items={result.ConsumedItems.Count}");
        return result;
    }

    private static bool TryResolveContext(PlayerProfile player, DollEntity doll, string maintenanceID, out MaintenanceConfig config, out string reason) {
        config = null;
        reason = string.Empty;

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        if (doll == null) {
            reason = "Doll is missing.";
            return false;
        }

        if (string.IsNullOrEmpty(maintenanceID)) {
            reason = "Maintenance ID is empty.";
            return false;
        }

        if (!ConfigManager.MaintenanceConfigs.TryGetValue(maintenanceID, out config)) {
            reason = $"Maintenance config [{maintenanceID}] is missing.";
            return false;
        }

        return true;
    }

    private static bool ValidateEffects(MaintenanceConfig config, string maintenanceID, out string reason) {
        reason = string.Empty;
        if (config?.Effects == null || config.Effects.Count == 0) {
            reason = $"Maintenance [{maintenanceID}] has no effects.";
            return false;
        }

        foreach (MaintenanceEffectConfig effect in config.Effects) {
            if (effect == null) {
                reason = $"Maintenance [{maintenanceID}] has a null effect.";
                return false;
            }

            if (!TryParseTarget(effect.TargetState, out _)) {
                reason = $"Maintenance [{maintenanceID}] has unknown TargetState [{effect.TargetState}].";
                return false;
            }

            if (effect.Amount <= 0f) {
                reason = $"Maintenance [{maintenanceID}] effect amount must be positive. Amount={effect.Amount}.";
                return false;
            }
        }

        return true;
    }

    private static bool TryApplyEffect(DollEntity doll, MaintenanceEffectConfig effect, out MaintenanceEffectApplication application, out string reason) {
        application = null;
        reason = string.Empty;

        if (doll == null) {
            reason = "Doll is missing.";
            return false;
        }

        if (effect == null) {
            reason = "Effect config is null.";
            return false;
        }

        if (!TryParseTarget(effect.TargetState, out MaintenanceTargetState targetState)) {
            reason = $"Unknown TargetState [{effect.TargetState}].";
            return false;
        }

        if (effect.Amount <= 0f) {
            reason = $"Effect amount must be positive. Amount={effect.Amount}.";
            return false;
        }

        float before = GetStateValue(doll, targetState);
        float after = ResolveAfterValue(doll, targetState, effect.Amount);
        SetStateValue(doll, targetState, after);

        application = new MaintenanceEffectApplication {
            TargetState = targetState,
            Before = before,
            After = after,
            Delta = after - before
        };
        return true;
    }

    private static bool TryParseTarget(string targetState, out MaintenanceTargetState parsed) {
        return System.Enum.TryParse(targetState, true, out parsed);
    }

    private static float GetStateValue(DollEntity doll, MaintenanceTargetState targetState) {
        switch (targetState) {
            case MaintenanceTargetState.Wear:
                return doll.Status.WearAndTear;
            case MaintenanceTargetState.Corruption:
                return doll.Status.Corruption;
            case MaintenanceTargetState.HP:
                return doll.Status.HP_Current;
            case MaintenanceTargetState.SAN:
                return doll.Status.SAN_Current;
            default:
                return 0f;
        }
    }

    private static float ResolveAfterValue(DollEntity doll, MaintenanceTargetState targetState, float amount) {
        switch (targetState) {
            case MaintenanceTargetState.Wear:
                return Mathf.Max(0f, doll.Status.WearAndTear - amount);
            case MaintenanceTargetState.Corruption:
                return Mathf.Max(0f, doll.Status.Corruption - amount);
            case MaintenanceTargetState.HP:
                return Mathf.Min(doll.Status.HP_Max, doll.Status.HP_Current + Mathf.RoundToInt(amount));
            case MaintenanceTargetState.SAN:
                return Mathf.Min(doll.Status.SAN_Max, doll.Status.SAN_Current + Mathf.RoundToInt(amount));
            default:
                return 0f;
        }
    }

    private static void SetStateValue(DollEntity doll, MaintenanceTargetState targetState, float value) {
        switch (targetState) {
            case MaintenanceTargetState.Wear:
                doll.Status.WearAndTear = value;
                break;
            case MaintenanceTargetState.Corruption:
                doll.Status.Corruption = value;
                break;
            case MaintenanceTargetState.HP:
                doll.Status.HP_Current = Mathf.RoundToInt(value);
                GameEventBus.PublishHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
                break;
            case MaintenanceTargetState.SAN:
                doll.Status.SAN_Current = Mathf.RoundToInt(value);
                GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
                break;
        }
    }
}
