using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class HomeUpgradeConfig {
    public string UpgradeID;
    public string Name;
    public string Room;
    public string Description;
    public int Price;
    public string Effect;
    public float Value;
}

// Making the workshop a home (docs/specs/2026-10-17-home-loop.md, batch 5): five one-off purchases, each a small
// standing effect (cheaper maintenance, better rest, SAN at night, a wider showcase, more shelf room). Config in
// 配置表(JSON)/Home/upgrades.json.
public static class HomeUpgradeService {
    public static bool Has(PlayerProfile player, string upgradeID) {
        return player?.HomeUpgrades != null && player.HomeUpgrades.Contains(upgradeID);
    }

    // The summed value of an effect over everything the player owns (0 when none).
    public static float Value(PlayerProfile player, string effect) {
        if (player?.HomeUpgrades == null) {
            return 0f;
        }

        return ConfigManager.HomeUpgrades
            .Where(upgrade => upgrade != null && upgrade.Effect == effect && player.HomeUpgrades.Contains(upgrade.UpgradeID))
            .Sum(upgrade => upgrade.Value);
    }

    public static bool CanBuy(PlayerProfile player, HomeUpgradeConfig upgrade, out string reason) {
        reason = string.Empty;
        if (player == null || upgrade == null) {
            reason = "没有这件改造。";
            return false;
        }

        if (Has(player, upgrade.UpgradeID)) {
            reason = "已经装好了。";
            return false;
        }

        if (player.Money < upgrade.Price) {
            reason = $"钱不够：要 {upgrade.Price}G，你有 {player.Money}G。";
            return false;
        }

        return true;
    }

    public static bool Buy(PlayerProfile player, string upgradeID, out string reason) {
        HomeUpgradeConfig upgrade = ConfigManager.HomeUpgrades.FirstOrDefault(u => u != null && u.UpgradeID == upgradeID);
        if (!CanBuy(player, upgrade, out reason)) {
            return false;
        }

        player.Money -= upgrade.Price;
        player.HomeUpgrades.Add(upgrade.UpgradeID);
        return true;
    }

    // A maintenance cost after the bench lamp's discount (money only; materials stay).
    public static CraftingCost MaintenanceCost(PlayerProfile player, CraftingCost cost) {
        float discount = Mathf.Clamp01(Value(player, "MaintenanceDiscount"));
        if (cost == null || discount <= 0f || cost.Money <= 0) {
            return cost;
        }

        return new CraftingCost {
            Money = Mathf.RoundToInt(cost.Money * (1f - discount)),
            RequiredItems = cost.RequiredItems
        };
    }
}
