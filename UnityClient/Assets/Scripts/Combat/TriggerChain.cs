using System;
using System.Collections.Generic;
using UnityEngine;

// A port where it sits in the backpack right now: the cell, the edge it faces (after the item's rotation), in or out.
public struct PlacedPort {
    public ItemEntity Item;
    public int X;
    public int Y;
    public TargetDirection Side;
    public bool IsOut;

    // The cell on the other side of this port's edge.
    public int FacingX => X + (Side == TargetDirection.Right ? 1 : Side == TargetDirection.Left ? -1 : 0);
    public int FacingY => Y + (Side == TargetDirection.Down ? 1 : Side == TargetDirection.Up ? -1 : 0);
}

// One out port feeding one in port across a cell edge.
public struct ChainLink {
    public ItemEntity From;
    public ItemEntity To;
    public PlacedPort Out;
    public PlacedPort In;
}

// A module a use would set off, in firing order: how far from the start, which module fed it, and the output bonus it
// gets from amplifiers between it and the start.
public class ChainStep {
    public ItemEntity Item;
    public ItemEntity Parent;
    public int Depth;
    public float Bonus;
}

// What one fight remembers about trigger chains: Zero's overload and whether she jammed.
public class ChainState {
    public int Overload;
    public int Capacity = 3;
    public int Cooling = 2;
    // Set when a use overflowed; the next turn of Zero's starts jammed.
    public bool JamNextTurn;
    // This turn: links are dead and she has one AP less.
    public bool Jammed;

    public const int JamAPPenalty = 1;

    public void Reset(ChassisComponent chassis) {
        Overload = 0;
        JamNextTurn = false;
        Jammed = false;
        Capacity = Mathf.Max(0, chassis?.OverloadCapacity ?? 3);
        Cooling = Mathf.Max(0, chassis?.OverloadCooling ?? 2);
    }

    public void Add(int load) {
        Overload = Mathf.Max(0, Overload + load);
    }

    // Zero's turn starts: a pending jam takes hold (overload clears), otherwise the overload cools.
    public void OnTurnStart() {
        Jammed = JamNextTurn;
        JamNextTurn = false;
        Overload = Jammed ? 0 : Mathf.Max(0, Overload - Cooling);
    }

    // After a whole use: over capacity means a jam next turn.
    public bool CheckOverflow() {
        if (Overload <= Capacity || JamNextTurn) {
            return false;
        }

        JamNextTurn = true;
        return true;
    }
}

// Trigger chains (GDD_01 §5.1A, docs/specs/2026-11-07-trigger-chains.md). Pure grid rules: where the ports are, which
// items link, what a use would set off; and the fight side: firing the modules down the line after a use, adding their
// load to the overload, jamming Zero when it overflows.
public static class TriggerChain {
    public const int MaxModules = 12;

    public static ChainState State => GameRoot.Core?.Combat?.Chain;

    public static bool HasPorts(ItemEntity item) {
        return item?.Grid?.Ports != null && item.Grid.Ports.Count > 0;
    }

    public static bool HasOutPort(ItemEntity item) {
        return HasPorts(item) && item.Grid.Ports.Exists(port => port != null && port.IsOut);
    }

    // The item's ports where they sit now; empty when it is not placed.
    public static List<PlacedPort> Ports(ItemEntity item, BackpackGrid grid) {
        List<PlacedPort> placed = new List<PlacedPort>();
        if (!HasPorts(item) || item.Grid.CurrentPos == null || grid == null || !grid.ContainedItems.Contains(item)) {
            return placed;
        }

        int rotation = item.Grid.Rotation;
        List<int[]> cells = BackpackGrid.GetNormalizedShapeCells(item, rotation);
        foreach (ItemPort port in item.Grid.Ports) {
            int index = ShapeIndex(item, port?.Cell);
            if (index < 0 || index >= cells.Count || !Enum.TryParse(port.Side, true, out TargetDirection side)) {
                continue;
            }

            placed.Add(new PlacedPort {
                Item = item,
                X = item.Grid.CurrentPos[0] + cells[index][0],
                Y = item.Grid.CurrentPos[1] + cells[index][1],
                Side = BackpackGrid.RotateDirection(side, rotation),
                IsOut = port.IsOut
            });
        }

        return placed;
    }

    // The same port, for any rotation and origin (the drag preview and the tests).
    public static List<PlacedPort> PortsAt(ItemEntity item, int originX, int originY, int rotation) {
        List<PlacedPort> placed = new List<PlacedPort>();
        if (!HasPorts(item)) {
            return placed;
        }

        List<int[]> cells = BackpackGrid.GetNormalizedShapeCells(item, rotation);
        foreach (ItemPort port in item.Grid.Ports) {
            int index = ShapeIndex(item, port?.Cell);
            if (index < 0 || index >= cells.Count || !Enum.TryParse(port.Side, true, out TargetDirection side)) {
                continue;
            }

            placed.Add(new PlacedPort { Item = item, X = originX + cells[index][0], Y = originY + cells[index][1], Side = BackpackGrid.RotateDirection(side, rotation), IsOut = port.IsOut });
        }

        return placed;
    }

    private static int ShapeIndex(ItemEntity item, int[] cell) {
        if (cell == null || cell.Length < 2 || item?.Grid?.Shape == null) {
            return -1;
        }

        int index = 0;
        foreach (int[] point in item.Grid.Shape) {
            if (point == null || point.Length < 2) {
                continue;
            }

            if (point[0] == cell[0] && point[1] == cell[1]) {
                return index;
            }

            index++;
        }

        return -1;
    }

    private static TargetDirection Opposite(TargetDirection side) {
        switch (side) {
            case TargetDirection.Right: return TargetDirection.Left;
            case TargetDirection.Left: return TargetDirection.Right;
            case TargetDirection.Up: return TargetDirection.Down;
            default: return TargetDirection.Up;
        }
    }

    // The links out of one item: each out port whose facing cell holds another item's in port on the shared edge.
    public static List<ChainLink> LinksFrom(ItemEntity item, BackpackGrid grid) {
        List<ChainLink> links = new List<ChainLink>();
        foreach (PlacedPort port in Ports(item, grid)) {
            if (!port.IsOut) {
                continue;
            }

            ItemEntity other = grid.GetItemAt(port.FacingX, port.FacingY);
            if (other == null || other == item) {
                continue;
            }

            foreach (PlacedPort entry in Ports(other, grid)) {
                if (!entry.IsOut && entry.X == port.FacingX && entry.Y == port.FacingY && entry.Side == Opposite(port.Side)) {
                    links.Add(new ChainLink { From = item, To = other, Out = port, In = entry });
                    break;
                }
            }
        }

        return links;
    }

    // Every link in the backpack (the combat screen draws them).
    public static List<ChainLink> AllLinks(BackpackGrid grid) {
        List<ChainLink> links = new List<ChainLink>();
        foreach (ItemEntity item in grid?.ContainedItems ?? new List<ItemEntity>()) {
            links.AddRange(LinksFrom(item, grid));
        }

        return links;
    }

    // What using the start would set off, in firing order (nearest first), each module once; amplifiers raise what
    // comes after them. Modules that cannot fire now (spent, sealed) are left in with their branch cut off when
    // `stopAtBlocked` is set, as firing does.
    public static List<ChainStep> Plan(ItemEntity start, BackpackGrid grid, bool stopAtBlocked = true) {
        List<ChainStep> steps = new List<ChainStep>();
        if (start == null || grid == null || !HasOutPort(start)) {
            return steps;
        }

        HashSet<ItemEntity> seen = new HashSet<ItemEntity> { start };
        Queue<ChainStep> queue = new Queue<ChainStep>();
        foreach (ChainLink link in LinksFrom(start, grid)) {
            if (seen.Add(link.To)) {
                queue.Enqueue(new ChainStep { Item = link.To, Parent = start, Depth = 1, Bonus = 0f });
            }
        }

        while (queue.Count > 0 && steps.Count < MaxModules) {
            ChainStep step = queue.Dequeue();
            steps.Add(step);
            if (stopAtBlocked && !CanFire(step.Item, out _)) {
                continue;
            }

            float bonus = step.Bonus + AmplifyBonus(step.Item);
            foreach (ChainLink link in LinksFrom(step.Item, grid)) {
                if (seen.Add(link.To)) {
                    queue.Enqueue(new ChainStep { Item = link.To, Parent = step.Item, Depth = step.Depth + 1, Bonus = bonus });
                }
            }
        }

        return steps;
    }

    // The overload the start's chain would add (modules that cannot fire add nothing).
    public static int PlannedLoad(ItemEntity start, BackpackGrid grid) {
        int load = 0;
        foreach (ChainStep step in Plan(start, grid)) {
            if (CanFire(step.Item, out _)) {
                load += step.Item.Combat?.ChainLoad ?? 0;
            }
        }

        return load;
    }

    public static bool ChainHasStrike(ItemEntity start, BackpackGrid grid) {
        return Plan(start, grid).Exists(step => IsStrike(step.Item) && CanFire(step.Item, out _));
    }

    // A module that does something when fired (a strike, a shield, an effect, or load to add or take away); a plain
    // conduit only passes the chain on.
    public static bool DoesSomething(ItemEntity item) {
        return item?.Combat != null
            && (IsStrike(item) || item.Combat.DamageType == nameof(DamageType.Shield) || item.Combat.ChainLoad != 0 || (item.Combat.Effects != null && item.Combat.Effects.Count > 0));
    }

    // The modules a use would fire now that do something (what the preview and the start's check count).
    public static int Workers(ItemEntity start, BackpackGrid grid) {
        return Plan(start, grid).FindAll(step => DoesSomething(step.Item) && CanFire(step.Item, out _)).Count;
    }

    public static bool IsStrike(ItemEntity item) {
        return item?.Combat != null && (item.Combat.DamageType == nameof(DamageType.Physical) || item.Combat.DamageType == nameof(DamageType.Energy));
    }

    // A module fires when it has combat stats, is no consumable and its own effects allow a use now.
    public static bool CanFire(ItemEntity item, out string reason) {
        reason = string.Empty;
        if (item?.Combat == null || item.ItemType == nameof(ItemType.Consumable)) {
            reason = "消耗品不能接进链路";
            return false;
        }

        return CombatItemEffects.CanUse(item, out reason);
    }

    private static float AmplifyBonus(ItemEntity item) {
        float bonus = 0f;
        foreach (EffectData data in item?.Combat?.Effects ?? new List<EffectData>()) {
            if (data?.EffectID == "ChainAmplify" && data.Params != null && data.Params.Length > 0) {
                bonus += data.Params[0];
            }
        }

        return bonus;
    }

    // Fight side: after the start's own use, fire what it links to. Called once per manual use (CombatItemEffects).
    public static void Fire(ItemUseOutcome start) {
        ChainState state = State;
        BackpackGrid grid = start?.User?.DataRef?.RuntimeGrid as BackpackGrid;
        if (state == null || grid == null || start.Item == null || !HasOutPort(start.Item)) {
            return;
        }

        if (state.Jammed) {
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = start.User, Label = "卡壳：链路不通" });
            return;
        }

        List<ChainStep> plan = Plan(start.Item, grid);
        if (plan.Count == 0) {
            return;
        }

        int fired = 0;
        int load = 0;
        foreach (ChainStep step in plan) {
            if (!CanFire(step.Item, out _)) {
                continue;
            }

            if (FireModule(start, step)) {
                if (DoesSomething(step.Item)) {
                    fired++;
                }

                load += step.Item.Combat.ChainLoad;
            }

            if (GameRoot.Core?.Combat?.EnemyFaction?.IsWipedOut() == true) {
                break;
            }
        }

        // The whole chain's load at once, so a heat sink early in the line still takes off what comes after it.
        state.Add(load);
        if (fired > 0) {
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = start.User, Label = $"链路带动 {fired} 件 · 过载 {state.Overload}/{state.Capacity}" });
            GameEventBus.PublishOverloadChanged(state.Overload, state.Capacity, state.Jammed);
        }

    }

    // After a whole use by hand (its own heat and its chain): over capacity means a jam next turn.
    public static void CheckOverflow(FighterEntity user) {
        ChainState state = State;
        if (state != null && state.CheckOverflow()) {
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = user, Label = "过载！下回合卡壳" });
            GameEventBus.PublishOverloadChanged(state.Overload, state.Capacity, state.Jammed);
        }
    }

    // Overload a use adds: its chain's load plus heat the start itself gives off (HeatOnUse), and the heat of the
    // modules its chain fires.
    public static int UseLoad(ItemEntity item, BackpackGrid grid) {
        int load = SelfHeat(item);
        foreach (ChainStep step in Plan(item, grid)) {
            if (CanFire(step.Item, out _)) {
                load += (step.Item.Combat?.ChainLoad ?? 0) + SelfHeat(step.Item);
            }
        }

        return HasOutPort(item) && State?.Jammed != true ? load : SelfHeat(item);
    }

    public static int SelfHeat(ItemEntity item) {
        int heat = 0;
        foreach (EffectData data in item?.Combat?.Effects ?? new List<EffectData>()) {
            if (data?.EffectID == "HeatOnUse" && data.Params != null && data.Params.Length > 0) {
                heat += Mathf.RoundToInt(data.Params[0]);
            }
        }

        return heat;
    }

    private static bool FireModule(ItemUseOutcome start, ChainStep step) {
        DollFighter user = start.User;
        ItemEntity item = step.Item;
        // The spark travels along the link before the module fires.
        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.ChainPulse, Actor = user, SourceItemID = step.Parent?.InstanceID, TargetItemID = item.InstanceID });
        float scale = 1f + step.Bonus;
        ItemUseOutcome outcome = new ItemUseOutcome { User = user, Item = item, FromChain = true };
        if (IsStrike(item)) {
            FighterEntity target = start.Target != null && start.Target.RuntimeHP > 0 ? start.Target : FirstEnemy(user);
            if (target == null) {
                return false;
            }

            user.Strike(target, item, outcome, scale);
        } else {
            if (item.Combat.DamageType == nameof(DamageType.Shield)) {
                GameEventBus.PublishAttackAction(user, user, item.Name, item.IconID, null, item);
            }

            CombatItemEffects.OutputScale = scale;
            try {
                foreach (EffectData data in item.Combat.Effects ?? new List<EffectData>()) {
                    EffectBase effect = EffectFactory.CreateEffect(data);
                    if (effect != null && effect.ListenEvent == CombatEventType.None) {
                        effect.ApplyOnUse(new ItemUseContext { InCombat = true, UserDoll = user.DataRef, UserFighter = user, SourceItem = item }, item);
                    }
                }
            } finally {
                CombatItemEffects.OutputScale = 1f;
            }
        }

        CombatItemEffects.AfterUse(outcome);
        return true;
    }

    private static FighterEntity FirstEnemy(FighterEntity user) {
        List<FighterEntity> enemies = CombatItemEffects.Opponents(user);
        return enemies.Count > 0 ? enemies[0] : null;
    }

    // One line for the tooltip of a start: what it sets off now and the overload it adds.
    public static string Preview(ItemEntity start, BackpackGrid grid) {
        if (!HasOutPort(start) || grid == null) {
            return string.Empty;
        }

        List<ChainStep> plan = Plan(start, grid);
        int count = Workers(start, grid);
        if (count == 0) {
            return "出口还没接上任何模块";
        }

        ChainState state = State;
        bool inFight = GameRoot.Core?.Combat != null && GameRoot.Core.Combat.CurrentState != CombatState.End && state != null;
        int load = UseLoad(start, grid);
        string loadText = load >= 0 ? $"+{load}" : load.ToString();
        if (!inFight) {
            return $"带动 {count} 件，过载 {loadText}";
        }

        if (state.Jammed) {
            return "卡壳中：这回合链路不通";
        }

        int after = Mathf.Max(0, state.Overload + load);
        string line = $"带动 {count} 件，过载 {loadText}（现在 {state.Overload} / 容量 {state.Capacity}）";
        return after > state.Capacity ? $"<color=#E0503C>{line}，会卡壳</color>" : line;
    }
}
