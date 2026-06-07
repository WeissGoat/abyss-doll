using System.Collections.Generic;
using UnityEngine;

public class ChassisUpgradeResult {
    public bool Success;
    public string Reason;
    public string PreviousChassisID;
    public string NewChassisID;
    public int PreviousWidth;
    public int PreviousHeight;
    public int NewWidth;
    public int NewHeight;
    public int MoneySpent;
    public bool RuntimeGridRebuilt;
    public List<ItemEntity> ConsumedItems = new List<ItemEntity>();

    public string FeedbackText {
        get {
            if (!Success) {
                return string.IsNullOrEmpty(Reason) ? "Chassis upgrade failed." : Reason;
            }

            return $"Chassis upgraded: {PreviousChassisID} -> {NewChassisID}. Money -{MoneySpent}, items {ConsumedItems.Count}.";
        }
    }
}

public static class ChassisUpgradeService {
    public static bool CanUpgrade(PlayerProfile player, DollEntity doll, out string reason) {
        return TryResolveUpgrade(player, doll, out ChassisComponent currentConfig, out _, out reason)
            && WorkshopCostService.CanAfford(currentConfig.UpgradeCost, player, out reason);
    }

    public static ChassisUpgradeResult Upgrade(PlayerProfile player, DollEntity doll) {
        ChassisUpgradeResult result = NewResult(doll);

        if (!TryResolveUpgrade(player, doll, out ChassisComponent currentConfig, out ChassisComponent nextConfig, out string reason)) {
            result.Success = false;
            result.Reason = reason;
            return result;
        }

        ChassisUpgradeCost cost = currentConfig.UpgradeCost;
        if (!WorkshopCostService.TryPayCost(cost, player, $"ChassisUpgrade:{currentConfig.ChassisID}", out WorkshopCostPaymentResult paymentResult)) {
            result.Success = false;
            result.Reason = paymentResult.Reason;
            return result;
        }

        result.MoneySpent = paymentResult.MoneySpent;
        result.ConsumedItems.AddRange(paymentResult.ConsumedItems);

        doll.Chassis = CloneChassis(nextConfig);
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);

        result.Success = true;
        result.Reason = string.Empty;
        result.NewChassisID = doll.Chassis.ChassisID;
        result.NewWidth = doll.Chassis.GridWidth;
        result.NewHeight = doll.Chassis.GridHeight;
        result.RuntimeGridRebuilt = doll.RuntimeGrid is BackpackGrid;

        Debug.Log($"[ChassisUpgradeService] {result.FeedbackText}");
        return result;
    }

    private static bool TryResolveUpgrade(
        PlayerProfile player,
        DollEntity doll,
        out ChassisComponent currentConfig,
        out ChassisComponent nextConfig,
        out string reason) {
        currentConfig = null;
        nextConfig = null;
        reason = string.Empty;

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        if (doll == null) {
            reason = "Doll is missing.";
            return false;
        }

        if (doll.Chassis == null || string.IsNullOrEmpty(doll.Chassis.ChassisID)) {
            reason = "Current chassis is missing.";
            return false;
        }

        if (!ConfigManager.Chassis.TryGetValue(doll.Chassis.ChassisID, out currentConfig)) {
            reason = $"Current chassis config [{doll.Chassis.ChassisID}] is missing.";
            return false;
        }

        if (currentConfig.UpgradeCost == null) {
            reason = $"Chassis [{currentConfig.ChassisID}] has no further upgrade.";
            return false;
        }

        if (string.IsNullOrEmpty(currentConfig.UpgradeCost.NextChassisID)) {
            reason = $"Chassis [{currentConfig.ChassisID}] upgrade has no NextChassisID.";
            return false;
        }

        if (!ConfigManager.Chassis.TryGetValue(currentConfig.UpgradeCost.NextChassisID, out nextConfig)) {
            reason = $"Next chassis config [{currentConfig.UpgradeCost.NextChassisID}] is missing.";
            return false;
        }

        return true;
    }

    private static ChassisComponent CloneChassis(ChassisComponent template) {
        string chassisJson = Newtonsoft.Json.JsonConvert.SerializeObject(template);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<ChassisComponent>(chassisJson);
    }

    private static ChassisUpgradeResult NewResult(DollEntity doll) {
        ChassisComponent chassis = doll?.Chassis;
        return new ChassisUpgradeResult {
            PreviousChassisID = chassis?.ChassisID ?? string.Empty,
            NewChassisID = chassis?.ChassisID ?? string.Empty,
            PreviousWidth = chassis?.GridWidth ?? 0,
            PreviousHeight = chassis?.GridHeight ?? 0,
            NewWidth = chassis?.GridWidth ?? 0,
            NewHeight = chassis?.GridHeight ?? 0
        };
    }
}
