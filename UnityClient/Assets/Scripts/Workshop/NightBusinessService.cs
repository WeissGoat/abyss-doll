using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BusinessChannel {
    DumpBox,
    Showcase
}

public class BusinessLine {
    public ItemEntity Item;
    public BusinessChannel Channel;
    public int SellValue;
    public float Multiplier = 1f;
    public int AdjacentMatches;
    public int Value;
}

public class BusinessReport {
    public int DumpBoxIncome;
    public int ShowcaseIncome;
    public List<BusinessLine> Lines = new List<BusinessLine>();

    public int Total => DumpBoxIncome + ShowcaseIncome;
}

// 今晚营业 (GDD_04 three steps, Demo cut; docs/specs/2026-10-05-night-business.md). Goods move from the stash into
// the dump box (sold at DumpBoxMultiplier of the sell value) or the small showcase grid (ShowcaseMultiplier, plus
// ShowcaseAdjacencyBonus for every neighbour sharing a market tag). Everything placed sells when the business opens;
// customers, budgets and heat come after the Demo. Arranging is free (rule card 01) and can happen any time.
public static class NightBusinessService {
    public static bool CanSell(ItemEntity item) {
        return item != null && item.BaseValue > 0 && item.ItemType != nameof(ItemType.QuestItem);
    }

    public static bool IsContraband(ItemEntity item) {
        List<string> tags = item?.Economy?.ValueTags;
        return tags != null && tags.Contains("Contraband");
    }

    // 一键倾倒: every loot item in the stash except core materials and contraband.
    public static int DumpAllLoot(PlayerProfile player) {
        if (player == null) {
            return 0;
        }

        List<ItemEntity> loot = player.StashInventory
            .Where(item => item != null
                && item.ItemType == nameof(ItemType.Loot)
                && (item.Tags == null || !item.Tags.Contains("CoreMaterial"))
                && !IsContraband(item)
                && CanSell(item))
            .ToList();
        foreach (ItemEntity item in loot) {
            TryMoveToDumpBox(player, item, out _);
        }

        return loot.Count;
    }

    public static bool TryMoveToDumpBox(PlayerProfile player, ItemEntity item, out string reason) {
        if (!CanTakeFromStash(player, item, out reason)) {
            return false;
        }

        if (IsContraband(item)) {
            reason = $"{item.Name}是违禁品，普通顾客不收；可以试试橱窗。";
            return false;
        }

        player.StashInventory.Remove(item);
        player.DumpBox.Add(item);
        item.ContainerType = ItemContainerType.ShopBin;
        reason = string.Empty;
        return true;
    }

    // Places the item at the first spot of the showcase it fits, trying every allowed rotation.
    public static bool TryMoveToShowcase(PlayerProfile player, ItemEntity item, out string reason) {
        if (!CanTakeFromStash(player, item, out reason)) {
            return false;
        }

        BackpackGrid grid = BuildShowcaseGrid(player);
        if (!TryFindSpot(grid, item, out int x, out int y, out int rotation)) {
            reason = $"橱窗放不下{item.Name}了。";
            return false;
        }

        player.StashInventory.Remove(item);
        item.Grid.CurrentPos = new[] { x, y };
        item.Grid.Rotation = rotation;
        player.Showcase.Add(item);
        item.ContainerType = ItemContainerType.ShopBin;
        reason = string.Empty;
        return true;
    }

    public static bool TryReturnToStash(PlayerProfile player, ItemEntity item) {
        if (player == null || item == null || !(player.DumpBox.Remove(item) || player.Showcase.Remove(item))) {
            return false;
        }

        ItemLifecycleService.MarkGroundInventoryItem(player, item, "NightBusinessReturn");
        return true;
    }

    public static BackpackGrid BuildShowcaseGrid(PlayerProfile player) {
        EconomyConfig config = TownEconomyService.GetConfig();
        BackpackGrid grid = new BackpackGrid(new ChassisComponent {
            ChassisID = "showcase",
            GridWidth = Mathf.Max(1, (config?.ShowcaseWidth ?? 4) + Mathf.RoundToInt(HomeUpgradeService.Value(player, "ShowcaseColumns"))),
            GridHeight = Mathf.Max(1, config?.ShowcaseHeight ?? 4)
        });
        foreach (ItemEntity item in player?.Showcase ?? new List<ItemEntity>()) {
            if (item?.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length >= 2) {
                grid.PlaceItem(item, item.Grid.CurrentPos[0], item.Grid.CurrentPos[1], item.Grid.Rotation);
            }
        }

        return grid;
    }

    public static List<BusinessLine> Preview(PlayerProfile player) {
        List<BusinessLine> lines = new List<BusinessLine>();
        if (player == null) {
            return lines;
        }

        EconomyConfig config = TownEconomyService.GetConfig();
        foreach (ItemEntity item in player.DumpBox) {
            int sellValue = TownEconomyService.CalculateItemSellValue(player, item, EconomySellChannel.DumpBox).FinalValue;
            float multiplier = config?.DumpBoxMultiplier ?? 0.8f;
            lines.Add(new BusinessLine {
                Item = item,
                Channel = BusinessChannel.DumpBox,
                SellValue = sellValue,
                Multiplier = multiplier,
                Value = Mathf.RoundToInt(sellValue * multiplier)
            });
        }

        BackpackGrid grid = BuildShowcaseGrid(player);
        foreach (ItemEntity item in player.Showcase) {
            int sellValue = TownEconomyService.CalculateItemSellValue(player, item, EconomySellChannel.Showcase).FinalValue;
            int matches = CountMatchingNeighbours(grid, item, config);
            float multiplier = (config?.ShowcaseMultiplier ?? 1.3f) * (1f + matches * (config?.ShowcaseAdjacencyBonus ?? 0.15f));
            lines.Add(new BusinessLine {
                Item = item,
                Channel = BusinessChannel.Showcase,
                SellValue = sellValue,
                Multiplier = multiplier,
                AdjacentMatches = matches,
                Value = Mathf.RoundToInt(sellValue * multiplier)
            });
        }

        return lines;
    }

    // 开始营业: sells everything in the dump box and the showcase.
    public static BusinessReport Open(PlayerProfile player) {
        BusinessReport report = new BusinessReport();
        if (player == null) {
            return report;
        }

        report.Lines = Preview(player);
        foreach (BusinessLine line in report.Lines) {
            line.Item.ContainerType = ItemContainerType.Sold;
            if (line.Channel == BusinessChannel.DumpBox) {
                report.DumpBoxIncome += line.Value;
            } else {
                report.ShowcaseIncome += line.Value;
            }
        }

        player.DumpBox.Clear();
        player.Showcase.Clear();
        Debug.Log($"[NightBusiness] Sold {report.Lines.Count} item(s): dump box {report.DumpBoxIncome}G, showcase {report.ShowcaseIncome}G.");
        return report;
    }

    public static List<string> MarketTags(ItemEntity item, EconomyConfig config) {
        List<string> source = item?.Economy?.ValueTags != null && item.Economy.ValueTags.Count > 0 ? item.Economy.ValueTags : item?.Tags;
        List<string> ignored = config?.ShowcaseIgnoredTags ?? new List<string>();
        return (source ?? new List<string>()).Where(tag => !string.IsNullOrEmpty(tag) && !ignored.Contains(tag)).ToList();
    }

    private static int CountMatchingNeighbours(BackpackGrid grid, ItemEntity item, EconomyConfig config) {
        List<string> tags = MarketTags(item, config);
        if (tags.Count == 0 || item?.Grid?.CurrentPos == null) {
            return 0;
        }

        HashSet<ItemEntity> neighbours = new HashSet<ItemEntity>();
        foreach (int[] cell in grid.GetOccupiedCells(item, item.Grid.CurrentPos[0], item.Grid.CurrentPos[1], item.Grid.Rotation)) {
            foreach (int[] step in new[] { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } }) {
                ItemEntity other = grid.GetItemAt(cell[0] + step[0], cell[1] + step[1]);
                if (other != null && other != item) {
                    neighbours.Add(other);
                }
            }
        }

        return neighbours.Count(other => MarketTags(other, config).Any(tags.Contains));
    }

    private static bool CanTakeFromStash(PlayerProfile player, ItemEntity item, out string reason) {
        if (player == null || item == null || !player.StashInventory.Contains(item)) {
            reason = "这件东西不在仓库里。";
            return false;
        }

        if (!CanSell(item)) {
            reason = $"{item.Name}不能卖。";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryFindSpot(BackpackGrid grid, ItemEntity item, out int x, out int y, out int rotation) {
        x = y = rotation = 0;
        if (item?.Grid == null) {
            return false;
        }

        int steps = BackpackGrid.GetAllowedRotationSteps(item);
        for (int step = 0; step < steps; step++) {
            int candidate = step * BackpackGrid.RotationStep;
            for (int row = 0; row < grid.Height; row++) {
                for (int column = 0; column < grid.Width; column++) {
                    if (grid.CanPlaceItem(item, column, row, candidate)) {
                        x = column;
                        y = row;
                        rotation = candidate;
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
