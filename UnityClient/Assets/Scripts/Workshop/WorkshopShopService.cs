using System.Collections.Generic;
using UnityEngine;

public class WorkshopShopOffer {
    public string ConfigID;
    public string Name;
    public string ItemType;
    public int Price;
    public bool CanAfford;
}

public class WorkshopShopPurchaseResult {
    public bool Success;
    public string Reason;
    public ItemEntity Item;
    public int Price;
    public int MoneyAfter;
}

// The workshop supply shop (零时耗, rule card 01): basic weapons and consumables that can always be bought, so losing
// every weapon is a setback, not a dead save. Stock and markup come from the economy config (ShopStock,
// ShopPriceMultiplier). The price is the current sell value times the markup, so buying and reselling never pays.
// Bought items go to the stash; the pre-dive loadout brings them along.
public static class WorkshopShopService {
    public static List<WorkshopShopOffer> GetOffers(PlayerProfile player, string economyConfigID = null) {
        List<WorkshopShopOffer> offers = new List<WorkshopShopOffer>();
        EconomyConfig config = ResolveConfig(economyConfigID);
        if (player == null || config?.ShopStock == null) {
            return offers;
        }

        foreach (string configID in config.ShopStock) {
            if (string.IsNullOrEmpty(configID) || !ConfigManager.Items.TryGetValue(configID, out ItemEntity template)) {
                continue;
            }

            int price = CalculatePrice(player, template, config);
            offers.Add(new WorkshopShopOffer {
                ConfigID = configID,
                Name = template.Name,
                ItemType = template.ItemType,
                Price = price,
                CanAfford = player.Money >= price
            });
        }

        return offers;
    }

    public static WorkshopShopPurchaseResult TryBuy(PlayerProfile player, string configID, string economyConfigID = null) {
        WorkshopShopPurchaseResult result = new WorkshopShopPurchaseResult();
        EconomyConfig config = ResolveConfig(economyConfigID);
        if (player == null || config == null) {
            result.Reason = "经济配置缺失，无法购买。";
            return result;
        }

        if (config.ShopStock == null || !config.ShopStock.Contains(configID) || !ConfigManager.Items.TryGetValue(configID, out ItemEntity template)) {
            result.Reason = "补给铺没有这件商品。";
            return result;
        }

        int price = CalculatePrice(player, template, config);
        if (player.Money < price) {
            result.Reason = $"钱不够：{template.Name}要 {price}G，你有 {player.Money}G。";
            result.Price = price;
            result.MoneyAfter = player.Money;
            return result;
        }

        ItemEntity item = ConfigManager.CreateItem(configID);
        if (item == null) {
            result.Reason = $"无法创建物品 [{configID}]。";
            return result;
        }

        player.Money -= price;
        ItemLifecycleService.MarkGroundInventoryItem(player, item, "WorkshopShop");
        result.Success = true;
        result.Item = item;
        result.Price = price;
        result.MoneyAfter = player.Money;
        result.Reason = $"买下了{item.Name}（-{price}G），已放进仓库。";
        Debug.Log($"[WorkshopShop] Bought {configID} for {price}G. Money now {player.Money}.");
        return result;
    }

    public static int CalculatePrice(PlayerProfile player, ItemEntity template, EconomyConfig config) {
        if (template == null) {
            return 0;
        }

        int sellValue = Mathf.Max(
            TownEconomyService.CalculateItemSellValue(player, template, EconomySellChannel.DumpBox).FinalValue,
            TownEconomyService.CalculateItemSellValue(player, template, EconomySellChannel.Showcase).FinalValue);
        int baseValue = Mathf.Max(sellValue, template.BaseValue);
        float markup = config != null ? Mathf.Max(1f, config.ShopPriceMultiplier) : 1.2f;
        // The small epsilon keeps float noise (100 * 1.2 = 120.000004) from rounding up to 121.
        return Mathf.Max(1, Mathf.CeilToInt(baseValue * markup - 0.001f));
    }

    private static EconomyConfig ResolveConfig(string economyConfigID) {
        string id = string.IsNullOrEmpty(economyConfigID) ? TownEconomyService.DefaultEconomyConfigID : economyConfigID;
        return ConfigManager.EconomyConfigs.TryGetValue(id, out EconomyConfig config) ? config : null;
    }
}
