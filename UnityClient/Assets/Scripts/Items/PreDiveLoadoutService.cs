using System.Collections.Generic;
using System.Linq;

public enum LoadoutStashGroup {
    Gear,
    Consumable,
    LootAndMaterial
}

public class LoadoutRestoreReport {
    public int PlacedAtSamePosition;
    public int PlacedElsewhere;
    public List<string> DidNotFit = new List<string>();

    public int PlacedCount => PlacedAtSamePosition + PlacedElsewhere;
}

// 出发前整备 (docs/specs/2026-10-04-pre-dive-loadout.md): move items between the stash and the backpack before a dive.
// The stash is PlayerProfile.StashInventory (GroundInventory); the grid and shapes are the only limits.
public static class PreDiveLoadoutService {
    private const string Source = "PreDiveLoadout";

    public static LoadoutStashGroup Classify(ItemEntity item) {
        switch (item?.ItemType) {
            case "Weapon":
            case "Armor":
            case "Anchor":
                return LoadoutStashGroup.Gear;
            case "Consumable":
                return LoadoutStashGroup.Consumable;
            default:
                return LoadoutStashGroup.LootAndMaterial;
        }
    }

    public static List<ItemEntity> GetStashItems(PlayerProfile player, LoadoutStashGroup group) {
        if (player?.StashInventory == null) {
            return new List<ItemEntity>();
        }

        return player.StashInventory.Where(item => item != null && Classify(item) == group).ToList();
    }

    // Puts a stash item at the first backpack position it fits, trying each allowed rotation.
    public static bool TryBringFromStash(PlayerProfile player, ItemEntity item, out string reason) {
        reason = string.Empty;
        BackpackGrid grid = ResolveGrid(player);
        if (grid == null || item == null) {
            reason = "背包不存在。";
            return false;
        }

        if (!TryFindFirstFit(grid, item, out int x, out int y, out int rotation)) {
            reason = $"背包放不下 [{item.Name}]。";
            return false;
        }

        bool moved = ItemLifecycleService.TryMoveGroundInventoryToBackpack(player, item, x, y, rotation, Source, out ItemLifecycleResult result);
        reason = moved ? string.Empty : result?.Reason;
        return moved;
    }

    public static bool TryReturnToStash(PlayerProfile player, ItemEntity item, out string reason) {
        bool moved = ItemLifecycleService.TryMoveBackpackItemToGroundInventory(player, item, Source, out ItemLifecycleResult result);
        reason = moved ? string.Empty : result?.Reason;
        return moved;
    }

    // Called when a dive starts: remembers where everything sat, for 「带上上次的装备」.
    public static void RememberLoadout(PlayerProfile player) {
        BackpackGrid grid = ResolveGrid(player);
        if (player == null || grid == null) {
            return;
        }

        player.LastDiveLoadout = grid.ContainedItems
            .Where(item => item?.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length >= 2)
            .Select(item => new LoadoutSlotRecord {
                InstanceID = item.InstanceID,
                X = item.Grid.CurrentPos[0],
                Y = item.Grid.CurrentPos[1],
                Rotation = item.Grid.Rotation
            })
            .ToList();
    }

    // Brings back every remembered item that is still in the stash: at its old position when free, otherwise at the
    // first position it fits.
    public static LoadoutRestoreReport RestoreLastLoadout(PlayerProfile player) {
        LoadoutRestoreReport report = new LoadoutRestoreReport();
        BackpackGrid grid = ResolveGrid(player);
        if (player?.LastDiveLoadout == null || grid == null) {
            return report;
        }

        List<(ItemEntity item, LoadoutSlotRecord slot)> pending = new List<(ItemEntity, LoadoutSlotRecord)>();
        foreach (LoadoutSlotRecord slot in player.LastDiveLoadout) {
            ItemEntity item = player.StashInventory.FirstOrDefault(candidate => candidate != null && candidate.InstanceID == slot.InstanceID);
            if (item != null) {
                pending.Add((item, slot));
            }
        }

        // Old positions first, so a fallback item cannot take a slot another item is about to return to.
        List<ItemEntity> fallback = new List<ItemEntity>();
        foreach (var (item, slot) in pending) {
            if (grid.CanPlaceItem(item, slot.X, slot.Y, slot.Rotation)
                && ItemLifecycleService.TryMoveGroundInventoryToBackpack(player, item, slot.X, slot.Y, slot.Rotation, Source, out _)) {
                report.PlacedAtSamePosition++;
            } else {
                fallback.Add(item);
            }
        }

        foreach (ItemEntity item in fallback) {
            if (TryBringFromStash(player, item, out _)) {
                report.PlacedElsewhere++;
            } else {
                report.DidNotFit.Add(item.Name);
            }
        }

        return report;
    }

    public static bool HasWeapon(PlayerProfile player) {
        BackpackGrid grid = ResolveGrid(player);
        return grid != null && grid.ContainedItems.Any(item => item?.ItemType == "Weapon");
    }

    private static bool TryFindFirstFit(BackpackGrid grid, ItemEntity item, out int x, out int y, out int rotation) {
        x = -1;
        y = -1;
        rotation = 0;
        if (item?.Grid == null) {
            return false;
        }

        int current = BackpackGrid.NormalizeRotation(item.Grid.Rotation);
        List<int> rotations = new List<int> { current };
        if (BackpackGrid.CanRotateItem(item)) {
            for (int step = BackpackGrid.RotationStep; step < BackpackGrid.FullRotation; step += BackpackGrid.RotationStep) {
                int candidate = BackpackGrid.NormalizeRotation(current + step);
                if (BackpackGrid.IsRotationAllowed(item, candidate) && !rotations.Contains(candidate)) {
                    rotations.Add(candidate);
                }
            }
        }

        foreach (int candidateRotation in rotations) {
            for (int row = 0; row < grid.Height; row++) {
                for (int column = 0; column < grid.Width; column++) {
                    if (grid.CanPlaceItem(item, column, row, candidateRotation)) {
                        x = column;
                        y = row;
                        rotation = candidateRotation;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static BackpackGrid ResolveGrid(PlayerProfile player) {
        return player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
    }
}
