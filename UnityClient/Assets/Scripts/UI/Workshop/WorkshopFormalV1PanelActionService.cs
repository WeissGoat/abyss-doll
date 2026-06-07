using System;
using System.Linq;
using UnityEngine;

public sealed class WorkshopFormalV1PanelActionResult {
    public string ScreenID;
    public string ButtonName;
    public bool Handled;
    public bool Success;
    public string Reason;
    public string FeedbackText;
    public bool RefreshPanel = true;
    public bool ClosePanel;
    public string NextScreenID;
}

public static class WorkshopFormalV1PanelActionService {
    public static WorkshopFormalV1PanelActionResult Execute(string screenID, string buttonName, CoreBackend core) {
        WorkshopFormalV1PanelActionResult result = NewResult(screenID, buttonName);
        if (core == null || core.CurrentPlayer == null) {
            return Fail(result, "Core backend or player profile is missing.");
        }

        switch (screenID) {
            case "maintenance_panel":
                return ExecuteMaintenanceAction(result, core, buttonName);
            case "daily_bill_report":
                return ExecuteDailyBillAction(result, buttonName);
            case "business_settlement":
                return ExecuteBusinessSettlementAction(result, buttonName);
            case "chassis_upgrade_panel":
                return ExecuteChassisUpgradeAction(result, core, buttonName);
            case "doll_interaction":
                return ExecuteDollInteractionAction(result, core, buttonName);
            default:
                return Unsupported(result, "This panel is currently read-only in the player flow.");
        }
    }

    private static WorkshopFormalV1PanelActionResult ExecuteMaintenanceAction(
        WorkshopFormalV1PanelActionResult result,
        CoreBackend core,
        string buttonName) {
        if (buttonName == "Postpone_Button") {
            result.Handled = true;
            result.Success = true;
            result.ClosePanel = true;
            result.RefreshPanel = false;
            result.FeedbackText = "Maintenance postponed.";
            return result;
        }

        if (buttonName != "FullRepair_Button" && buttonName != "UseRepairKit_Button") {
            return Unsupported(result, "Maintenance action is not bound to a backend operation yet.");
        }

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        string maintenanceID = ResolveBestMaintenanceID(player, doll, buttonName, out string reason);
        if (string.IsNullOrEmpty(maintenanceID)) {
            return Fail(result, reason);
        }

        MaintenanceApplicationResult applied = core.Workshop.ApplyMaintenance(maintenanceID, doll);
        result.Handled = true;
        result.Success = applied.Success;
        result.Reason = applied.Reason;
        result.FeedbackText = applied.Success
            ? $"Applied maintenance [{maintenanceID}]. Money -{applied.MoneySpent}, effects {applied.AppliedEffects.Count}."
            : $"Maintenance failed: {applied.Reason}";
        return result;
    }

    private static WorkshopFormalV1PanelActionResult ExecuteDailyBillAction(
        WorkshopFormalV1PanelActionResult result,
        string buttonName) {
        switch (buttonName) {
            case "Continue_Button":
                result.Handled = true;
                result.Success = true;
                result.ClosePanel = true;
                result.RefreshPanel = false;
                result.FeedbackText = "Daily bill reviewed.";
                return result;
            case "ReviewSell_Button":
                return Navigate(result, "shop_staging", "Opening shop staging.");
            case "DeferPayment_Button":
                return Unsupported(result, "Payment deferral requires the monthly rent decision UI before it can mutate state.");
            default:
                return Unsupported(result, "Daily bill action is not bound to a backend operation yet.");
        }
    }

    private static WorkshopFormalV1PanelActionResult ExecuteBusinessSettlementAction(
        WorkshopFormalV1PanelActionResult result,
        string buttonName) {
        switch (buttonName) {
            case "ContinueToBill_Button":
                return Navigate(result, "daily_bill_report", "Opening daily bill.");
            case "ReviewRisk_Button":
                return Navigate(result, "shop_staging", "Opening shop risk review.");
            default:
                return Unsupported(result, "Business settlement action is not bound to a backend operation yet.");
        }
    }

    private static WorkshopFormalV1PanelActionResult ExecuteChassisUpgradeAction(
        WorkshopFormalV1PanelActionResult result,
        CoreBackend core,
        string buttonName) {
        if (buttonName != "Upgrade_Button") {
            return Unsupported(result, "Blueprint selection is not player-bound yet.");
        }

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        ChassisUpgradeResult upgradeResult = ChassisUpgradeService.Upgrade(core.CurrentPlayer, doll);

        result.Handled = true;
        result.Success = upgradeResult.Success;
        result.Reason = upgradeResult.Reason;
        result.FeedbackText = upgradeResult.FeedbackText;
        return result;
    }

    private static WorkshopFormalV1PanelActionResult ExecuteDollInteractionAction(
        WorkshopFormalV1PanelActionResult result,
        CoreBackend core,
        string buttonName) {
        DollInteractionResult interactionResult;
        switch (buttonName) {
            case "Touch_Button":
                interactionResult = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Head, DollInteractionScene.Workshop);
                break;
            case "Talk_Button":
                interactionResult = DollInteractionService.ExecuteTalk(core.CurrentPlayer, DollInteractionScene.Workshop);
                break;
            default:
                return Unsupported(result, "Doll interaction action is not bound to a backend operation yet.");
        }

        result.Handled = true;
        result.Success = interactionResult.Success;
        result.Reason = interactionResult.Reason;
        result.FeedbackText = interactionResult.CombinedText;
        return result;
    }

    private static string ResolveBestMaintenanceID(PlayerProfile player, DollEntity doll, string buttonName, out string reason) {
        reason = string.Empty;
        if (player == null || doll == null) {
            reason = "Player or doll is missing.";
            return string.Empty;
        }

        var candidates = ConfigManager.MaintenanceConfigs.Values
            .Where(config => config != null && MaintenanceCanImproveDoll(config, doll))
            .OrderByDescending(config => ScoreMaintenance(config, doll, buttonName))
            .ThenBy(config => config.MaintenanceID)
            .ToList();

        if (candidates.Count == 0) {
            reason = "No maintenance is needed for current doll state.";
            return string.Empty;
        }

        foreach (MaintenanceConfig candidate in candidates) {
            if (MaintenanceService.CanApply(player, doll, candidate.MaintenanceID, out _)) {
                return candidate.MaintenanceID;
            }
        }

        string firstReason;
        MaintenanceService.CanApply(player, doll, candidates[0].MaintenanceID, out firstReason);
        reason = string.IsNullOrEmpty(firstReason)
            ? "No affordable maintenance option is available."
            : firstReason;
        return string.Empty;
    }

    private static bool MaintenanceCanImproveDoll(MaintenanceConfig config, DollEntity doll) {
        if (config?.Effects == null || doll?.Status == null) {
            return false;
        }

        return config.Effects.Any(effect => EffectCanImproveDoll(effect, doll));
    }

    private static bool EffectCanImproveDoll(MaintenanceEffectConfig effect, DollEntity doll) {
        if (effect == null || doll?.Status == null) {
            return false;
        }

        MaintenanceTargetState target;
        if (!Enum.TryParse(effect.TargetState, true, out target)) {
            return false;
        }

        switch (target) {
            case MaintenanceTargetState.Wear:
                return doll.Status.WearAndTear > 0f;
            case MaintenanceTargetState.Corruption:
                return doll.Status.Corruption > 0f;
            case MaintenanceTargetState.HP:
                return doll.Status.HP_Current < doll.Status.HP_Max;
            case MaintenanceTargetState.SAN:
                return doll.Status.SAN_Current < doll.Status.SAN_Max;
            default:
                return false;
        }
    }

    private static int ScoreMaintenance(MaintenanceConfig config, DollEntity doll, string buttonName) {
        int score = 0;
        foreach (MaintenanceEffectConfig effect in config.Effects) {
            MaintenanceTargetState target;
            if (!Enum.TryParse(effect.TargetState, true, out target)) {
                continue;
            }

            if (buttonName == "UseRepairKit_Button" && (target == MaintenanceTargetState.Wear || target == MaintenanceTargetState.HP)) {
                score += 10;
            }

            if (target == MaintenanceTargetState.Wear && doll.Status.WearAndTear > 0f) {
                score += Mathf.RoundToInt(Mathf.Min(doll.Status.WearAndTear, effect.Amount));
            } else if (target == MaintenanceTargetState.Corruption && doll.Status.Corruption > 0f) {
                score += Mathf.RoundToInt(Mathf.Min(doll.Status.Corruption, effect.Amount));
            } else if (target == MaintenanceTargetState.HP && doll.Status.HP_Current < doll.Status.HP_Max) {
                score += Mathf.Min(doll.Status.HP_Max - doll.Status.HP_Current, Mathf.RoundToInt(effect.Amount));
            } else if (target == MaintenanceTargetState.SAN && doll.Status.SAN_Current < doll.Status.SAN_Max) {
                score += Mathf.Min(doll.Status.SAN_Max - doll.Status.SAN_Current, Mathf.RoundToInt(effect.Amount));
            }
        }

        return score;
    }

    private static WorkshopFormalV1PanelActionResult Navigate(
        WorkshopFormalV1PanelActionResult result,
        string nextScreenID,
        string feedbackText) {
        result.Handled = true;
        result.Success = true;
        result.NextScreenID = nextScreenID;
        result.FeedbackText = feedbackText;
        return result;
    }

    private static WorkshopFormalV1PanelActionResult Unsupported(WorkshopFormalV1PanelActionResult result, string reason) {
        result.Handled = false;
        result.Success = false;
        result.Reason = reason;
        result.FeedbackText = reason;
        return result;
    }

    private static WorkshopFormalV1PanelActionResult Fail(WorkshopFormalV1PanelActionResult result, string reason) {
        result.Handled = true;
        result.Success = false;
        result.Reason = reason;
        result.FeedbackText = reason;
        return result;
    }

    private static WorkshopFormalV1PanelActionResult NewResult(string screenID, string buttonName) {
        return new WorkshopFormalV1PanelActionResult {
            ScreenID = screenID,
            ButtonName = buttonName
        };
    }
}
