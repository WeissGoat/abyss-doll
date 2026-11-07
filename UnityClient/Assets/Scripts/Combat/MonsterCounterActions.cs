using System;
using System.Collections.Generic;
using UnityEngine;

// Monster actions that answer the item archetypes (docs/specs/2026-10-17-content-expansion.md, 怪物配合):
// 加盾 holds off small hits for a turn, 净化 washes poison off, 自爆 goes through Zero's shield, 震荡 breaks up
// neighbour chains in the backpack. Armour, the counter to throwing knives, is a monster field (MonsterEntity.Armor).

// Params: Amount. Target Self or AllAllies. The shield lasts until the monster's next turn begins.
public class GainShieldAction : MonsterActionBase {
    public GainShieldAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        return Params.GetInt("Amount", 0) > 0 && MonsterTargetSelector.SelectAllies(context, Config.Target).Count > 0;
    }

    public override bool Execute(MonsterActionContext context) {
        int amount = Params.GetInt("Amount", 0);
        List<MonsterFighter> allies = MonsterTargetSelector.SelectAllies(context, Config.Target);
        if (amount <= 0 || allies.Count == 0) {
            return false;
        }

        foreach (MonsterFighter ally in allies) {
            ally.AddShield(amount);
        }

        Debug.Log($"[MonsterActionAI] {context.Actor.Name} raised {amount} shield on {allies.Count} monster(s).");
        return true;
    }
}

// Params: Statuses (default ["Poison"]), Heal (optional). Target Self or AllAllies.
public class CleanseAction : MonsterActionBase {
    public CleanseAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        return MonsterTargetSelector.SelectAllies(context, Config.Target).Count > 0;
    }

    public override bool Execute(MonsterActionContext context) {
        List<MonsterFighter> allies = MonsterTargetSelector.SelectAllies(context, Config.Target);
        if (allies.Count == 0) {
            return false;
        }

        List<CombatStatusType> statuses = ReadStatuses(Params);
        int heal = Mathf.Max(0, Params.GetInt("Heal", 0));
        foreach (MonsterFighter ally in allies) {
            int removed = 0;
            foreach (CombatStatusType status in statuses) {
                removed += CombatStatusService.Clear(ally, status);
            }

            if (removed > 0) {
                GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = ally, Label = $"净化 -{removed} 层" });
            }

            ally.Heal(heal);
        }

        Debug.Log($"[MonsterActionAI] {context.Actor.Name} cleansed {allies.Count} monster(s).");
        return true;
    }

    public static List<CombatStatusType> ReadStatuses(MonsterActionParamReader reader) {
        List<CombatStatusType> statuses = new List<CombatStatusType>();
        foreach (string name in reader.GetStringList("Statuses")) {
            if (Enum.TryParse(name, true, out CombatStatusType status) && !statuses.Contains(status)) {
                statuses.Add(status);
            }
        }

        if (statuses.Count == 0) {
            statuses.Add(CombatStatusType.Poison);
        }

        return statuses;
    }
}

// Params: Damage. The blast skips Zero's shield (and so her thorns), then the monster is gone. Pair it with
// Condition FromRound so the player sees it coming and has a turn to put the monster down first.
public class SelfDestructAction : MonsterActionBase {
    public SelfDestructAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        return Params.GetInt("Damage", 0) > 0 && MonsterTargetSelector.SelectPlayerTarget(context, Config.Target) != null;
    }

    public override bool Execute(MonsterActionContext context) {
        int damage = Params.GetInt("Damage", 0);
        FighterEntity target = MonsterTargetSelector.SelectPlayerTarget(context, Config.Target);
        if (damage <= 0 || target == null) {
            return false;
        }

        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = context.Actor, Label = "自爆！" });
        context.Actor.DealActionDamage(target, damage, Config.ActionID, ignoreShield: true);
        context.Actor.TakeDamage(context.Actor.RuntimeHP, ignoreShield: true);
        Debug.Log($"[MonsterActionAI] {context.Actor.Name} blew itself up on [{target.Name}] for {damage}.");
        return true;
    }
}

// Params: Count (default 2). Moves that many random backpack items to other free spots; with no free spot, swaps
// two items. Nothing leaves the backpack. Neighbour bonuses are recalculated afterwards.
public class ShuffleItemsAction : MonsterActionBase {
    private static readonly int[] Rotations = { 0, 90, 180, 270 };

    public ShuffleItemsAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        return grid?.ContainedItems != null && grid.ContainedItems.Count > 0;
    }

    public override bool Execute(MonsterActionContext context) {
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        if (grid?.ContainedItems == null || grid.ContainedItems.Count == 0) {
            return false;
        }

        int count = Mathf.Max(1, Params.GetInt("Count", 2));
        List<ItemEntity> moved = Shuffle(grid, count);
        if (moved.Count == 0) {
            Debug.Log($"[MonsterActionAI] {context.Actor.Name} shook the backpack but nothing could move.");
            return true;
        }

        if (context.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(context.ActiveDoll);
        }

        List<string> names = new List<string>();
        foreach (ItemEntity item in moved) {
            names.Add(item.Name);
            GameEventBus.PublishItemPlaced(item.InstanceID, item.Grid.CurrentPos[0], item.Grid.CurrentPos[1]);
        }

        string joined = string.Join("、", names);
        GameEventBus.PublishInterference(context.Actor, joined, Config.ActionID, "震荡");
        context.Combat?.Timeline?.RecordItemInterference(
            FactionType.Enemy,
            context.Actor.Name,
            joined,
            Config.ActionID,
            "震荡背包",
            $"{context.Actor.Name} 震乱了背包：{joined} 换了位置。",
            moved[0]);
        Debug.Log($"[MonsterActionAI] {context.Actor.Name} shuffled {joined}.");
        return true;
    }

    // Pure grid work, shared with the tests.
    public static List<ItemEntity> Shuffle(BackpackGrid grid, int count) {
        List<ItemEntity> moved = new List<ItemEntity>();
        List<ItemEntity> pool = new List<ItemEntity>(grid.ContainedItems);
        for (int i = 0; i < count && pool.Count > 0; i++) {
            ItemEntity item = pool[GameRandom.Range(0, pool.Count)];
            pool.Remove(item);
            if (TryRelocate(grid, item)) {
                AddOnce(moved, item);
            } else if (TrySwap(grid, item, pool, out ItemEntity other)) {
                AddOnce(moved, item);
                AddOnce(moved, other);
                pool.Remove(other);
            }
        }

        return moved;
    }

    private static void AddOnce(List<ItemEntity> list, ItemEntity item) {
        if (!list.Contains(item)) {
            list.Add(item);
        }
    }

    private static bool TryRelocate(BackpackGrid grid, ItemEntity item) {
        int originX = item.Grid.CurrentPos[0];
        int originY = item.Grid.CurrentPos[1];
        int originRotation = item.Grid.Rotation;
        grid.RemoveItem(item);

        List<int[]> spots = new List<int[]>();
        foreach (int rotation in Rotations) {
            if (!BackpackGrid.IsRotationAllowed(item, rotation)) {
                continue;
            }

            for (int x = 0; x < grid.Width; x++) {
                for (int y = 0; y < grid.Height; y++) {
                    if ((x != originX || y != originY) && grid.CanPlaceItem(item, x, y, rotation)) {
                        spots.Add(new[] { x, y, rotation });
                    }
                }
            }
        }

        if (spots.Count == 0) {
            grid.PlaceItem(item, originX, originY, originRotation);
            return false;
        }

        int[] spot = spots[GameRandom.Range(0, spots.Count)];
        return grid.PlaceItem(item, spot[0], spot[1], spot[2]) || grid.PlaceItem(item, originX, originY, originRotation);
    }

    private static bool TrySwap(BackpackGrid grid, ItemEntity item, List<ItemEntity> others, out ItemEntity other) {
        List<ItemEntity> candidates = new List<ItemEntity>(others);
        while (candidates.Count > 0) {
            other = candidates[GameRandom.Range(0, candidates.Count)];
            candidates.Remove(other);
            if (SwapPlaces(grid, item, other)) {
                return true;
            }
        }

        other = null;
        return false;
    }

    private static bool SwapPlaces(BackpackGrid grid, ItemEntity a, ItemEntity b) {
        int ax = a.Grid.CurrentPos[0], ay = a.Grid.CurrentPos[1], ar = a.Grid.Rotation;
        int bx = b.Grid.CurrentPos[0], by = b.Grid.CurrentPos[1], br = b.Grid.Rotation;
        grid.RemoveItem(a);
        grid.RemoveItem(b);
        foreach (int rotationA in Rotations) {
            if (!BackpackGrid.IsRotationAllowed(a, rotationA) || !grid.PlaceItem(a, bx, by, rotationA)) {
                continue;
            }

            foreach (int rotationB in Rotations) {
                if (BackpackGrid.IsRotationAllowed(b, rotationB) && grid.PlaceItem(b, ax, ay, rotationB)) {
                    return true;
                }
            }

            grid.RemoveItem(a);
        }

        grid.PlaceItem(a, ax, ay, ar);
        grid.PlaceItem(b, bx, by, br);
        return false;
    }
}

// 结晶 (GDD_01 §6.2 格子封锁型). Params: Count (cells), Turns (Zero's turns until it melts). Seals cells under items
// that do something in a fight first, then empty cells: a sealed item can neither be used nor pass a trigger chain.
public class LockCellsAction : MonsterActionBase {
    public LockCellsAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        return grid != null && Candidates(grid, true).Count + Candidates(grid, false).Count > 0;
    }

    public override bool Execute(MonsterActionContext context) {
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        if (grid == null) {
            return false;
        }

        List<int[]> sealedCells = Lock(grid, Mathf.Max(1, Params.GetInt("Count", 2)), Mathf.Max(1, Params.GetInt("Turns", 2)));
        List<string> names = new List<string>();
        foreach (int[] cell in sealedCells) {
            ItemEntity item = grid.GetItemAt(cell[0], cell[1]);
            if (item != null && !names.Contains(item.Name)) {
                names.Add(item.Name);
            }
        }

        string what = names.Count > 0 ? string.Join("、", names) : $"{sealedCells.Count} 个空格";
        GameEventBus.PublishInterference(context.Actor, what, Config.ActionID, "结晶封住");
        context.Combat?.Timeline?.RecordItemInterference(FactionType.Enemy, context.Actor.Name, what, Config.ActionID, "结晶封格",
            $"{context.Actor.Name} 让背包里长出结晶，封住了 {what}。");
        return true;
    }

    // Pure grid work, shared with the tests.
    public static List<int[]> Lock(BackpackGrid grid, int count, int turns) {
        List<int[]> picked = new List<int[]>();
        foreach (bool underItems in new[] { true, false }) {
            List<int[]> pool = Candidates(grid, underItems);
            while (picked.Count < count && pool.Count > 0) {
                int[] cell = pool[GameRandom.Range(0, pool.Count)];
                pool.Remove(cell);
                grid.SetCrystal(cell[0], cell[1], turns);
                picked.Add(cell);
            }
        }

        return picked;
    }

    // Cells not yet crystal: under an item that does something in a fight (one cell per item), or empty and usable.
    private static List<int[]> Candidates(BackpackGrid grid, bool underItems) {
        List<int[]> cells = new List<int[]>();
        HashSet<ItemEntity> taken = new HashSet<ItemEntity>();
        for (int x = 0; x < grid.Width; x++) {
            for (int y = 0; y < grid.Height; y++) {
                if (grid.IsCrystal(x, y)) {
                    continue;
                }

                ItemEntity item = grid.GetItemAt(x, y);
                if (underItems && item?.Combat != null && item.ItemType != nameof(ItemType.Loot) && taken.Add(item)) {
                    cells.Add(new[] { x, y });
                } else if (!underItems && item == null && grid.CanPlaceItem(OneCell, x, y, 0)) {
                    cells.Add(new[] { x, y });
                }
            }
        }

        return cells;
    }

    private static readonly ItemEntity OneCell = new ItemEntity { InstanceID = "crystal_probe", Name = "结晶", Grid = new ItemGridComponent { Shape = new[] { new[] { 0, 0 } } } };
}

// 孢子污染 (GDD_01 §6.2 侵蚀型). Params: SanPerUse. Fouls one item that does something in a fight: every use of it
// costs Zero that much SAN, and at each of her turn starts the spores creep into a neighbour (CombatItemEffects).
// It lasts the fight; the solvent spray washes it off.
public class SporePolluteAction : MonsterActionBase {
    public SporePolluteAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        return Pick(MonsterTargetSelector.GetPlayerGrid(context), false) != null;
    }

    public override bool Execute(MonsterActionContext context) {
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        ItemEntity item = Pick(grid, true);
        if (item == null || context.Combat == null) {
            return false;
        }

        context.Combat.Items.Pollute(item, Mathf.Max(1, Params.GetInt("SanPerUse", 2)));
        GameEventBus.PublishInterference(context.Actor, item.Name, Config.ActionID, "孢子污染");
        context.Combat.Timeline?.RecordItemInterference(FactionType.Enemy, context.Actor.Name, item.Name, Config.ActionID, "孢子污染",
            $"{context.Actor.Name} 往 {item.Name} 上喷了孢子：每用一次扣 SAN，还会往旁边蔓延。", item);
        return true;
    }

    // A clean item Zero uses in a fight (by hand first); random when `roll`, the first otherwise (for the preview).
    public static ItemEntity Pick(BackpackGrid grid, bool roll) {
        if (grid == null) {
            return null;
        }

        List<ItemEntity> manual = new List<ItemEntity>();
        List<ItemEntity> other = new List<ItemEntity>();
        foreach (ItemEntity item in grid.ContainedItems) {
            if (item?.Combat == null || item.ItemType == nameof(ItemType.Loot) || CombatItemEffects.Pollution(item) > 0) {
                continue;
            }

            (item.Combat.TriggerType == TriggerType.Manual.ToString() ? manual : other).Add(item);
        }

        List<ItemEntity> pool = manual.Count > 0 ? manual : other;
        if (pool.Count == 0) {
            return null;
        }

        return roll ? pool[GameRandom.Range(0, pool.Count)] : pool[0];
    }
}
