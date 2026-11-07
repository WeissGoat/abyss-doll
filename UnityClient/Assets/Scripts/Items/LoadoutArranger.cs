using System.Collections.Generic;
using System.Linq;

// Arranges what is already in the backpack so neighbours help each other (docs/specs/2026-10-17-content-expansion.md):
// one item at a time, every free spot and rotation is tried and the best kept, a few passes over. Better means more
// total weapon damage after adjacency (DamageMultiplier, PerAdjacentTag) plus a bonus for each neighbour relation
// that only pays off in a fight (a charge or an AP refund from a tagged neighbour). Used by the autoplay bot so the
// simulation can tell archetypes apart; pure rules, no UI.
public static class LoadoutArranger {
    public const float RelationValue = 6f;
    private const float Epsilon = 0.01f;

    // Returns how many items moved.
    public static int Arrange(DollEntity doll, int passes = 3) {
        BackpackGrid grid = doll?.RuntimeGrid as BackpackGrid;
        if (grid == null || grid.ContainedItems.Count < 2) {
            return 0;
        }

        int moves = WireChains(doll, grid);
        float score = Score(doll, grid);
        for (int pass = 0; pass < passes; pass++) {
            bool improved = false;
            foreach (ItemEntity item in grid.ContainedItems.ToList()) {
                if (item?.Grid?.CurrentPos == null || item.Grid.CurrentPos.Length < 2) {
                    continue;
                }

                int ox = item.Grid.CurrentPos[0];
                int oy = item.Grid.CurrentPos[1];
                int orot = item.Grid.Rotation;
                float best = score;
                int bx = ox, by = oy, brot = orot;
                grid.RemoveItem(item);
                foreach (int rotation in Rotations(item)) {
                    for (int y = 0; y < grid.Height; y++) {
                        for (int x = 0; x < grid.Width; x++) {
                            if ((x == ox && y == oy && rotation == orot) || !grid.CanPlaceItem(item, x, y, rotation)) {
                                continue;
                            }

                            grid.PlaceItem(item, x, y, rotation);
                            float trial = Score(doll, grid);
                            grid.RemoveItem(item);
                            if (trial > best + Epsilon) {
                                best = trial;
                                bx = x;
                                by = y;
                                brot = rotation;
                            }
                        }
                    }
                }

                grid.PlaceItem(item, bx, by, brot);
                if (bx != ox || by != oy || brot != orot) {
                    moves++;
                    improved = true;
                    score = best;
                }
            }

            if (!improved) {
                break;
            }
        }

        GridSolver.RecalculateAllEffects(doll);
        return moves;
    }

    // Trigger chains need their parts laid out port to port, which moving one item at a time rarely finds once the
    // backpack is full. So: take everything out, lay the chain parts first (each at its best spot given the ones
    // already down, largest first after the starts), then put the rest back wherever it fits. If anything no longer
    // fits, or the result scores lower, the old layout comes back. Returns how many items moved.
    private static int WireChains(DollEntity doll, BackpackGrid grid) {
        List<ItemEntity> chainParts = grid.ContainedItems.Where(TriggerChain.HasPorts).ToList();
        if (!chainParts.Exists(item => TriggerChain.HasOutPort(item) && item.Combat?.TriggerType == TriggerType.Manual.ToString()) || chainParts.Count < 2) {
            return 0;
        }

        Dictionary<ItemEntity, int[]> before = grid.ContainedItems.ToDictionary(item => item, item => new[] { item.Grid.CurrentPos[0], item.Grid.CurrentPos[1], item.Grid.Rotation });
        float scoreBefore = Score(doll, grid);
        List<ItemEntity> rest = grid.ContainedItems.Where(item => !TriggerChain.HasPorts(item)).ToList();
        foreach (ItemEntity item in grid.ContainedItems.ToList()) {
            grid.RemoveItem(item);
        }

        List<ItemEntity> starts = chainParts.Where(item => TriggerChain.HasOutPort(item) && item.Combat?.TriggerType == TriggerType.Manual.ToString()).ToList();
        List<ItemEntity> parts = chainParts.Except(starts).OrderByDescending(item => item.Grid.Shape.Length).ToList();
        bool fits = LayChain(doll, grid, starts, parts, rest.OrderByDescending(item => item.Grid?.Shape?.Length ?? 0).ToList());

        if (!fits || Score(doll, grid) < scoreBefore - Epsilon) {
            foreach (ItemEntity item in grid.ContainedItems.ToList()) {
                grid.RemoveItem(item);
            }

            foreach (KeyValuePair<ItemEntity, int[]> pair in before) {
                grid.PlaceItem(pair.Key, pair.Value[0], pair.Value[1], pair.Value[2]);
            }

            return 0;
        }

        return before.Count(pair => pair.Key.Grid.CurrentPos[0] != pair.Value[0] || pair.Key.Grid.CurrentPos[1] != pair.Value[1] || pair.Key.Grid.Rotation != pair.Value[2]);
    }

    // Where the first start goes decides what can hang off it, so every spot of it is tried with the rest laid
    // greedily behind, and the best whole chain is kept. Results are remembered per set of items and chassis.
    private static readonly Dictionary<string, List<int[]>> ChainLayouts = new Dictionary<string, List<int[]>>();

    private static bool LayChain(DollEntity doll, BackpackGrid grid, List<ItemEntity> starts, List<ItemEntity> parts, List<ItemEntity> rest) {
        List<ItemEntity> chain = starts.Concat(parts).ToList();
        List<ItemEntity> all = chain.Concat(rest).ToList();
        string key = $"{grid.Width}x{grid.Height}:{doll.Chassis?.ChassisID}:" + string.Join(",", all.Select(item => item.ConfigID));
        if (ChainLayouts.TryGetValue(key, out List<int[]> known)) {
            bool placed = true;
            for (int i = 0; i < all.Count; i++) {
                placed &= grid.PlaceItem(all[i], known[i][0], known[i][1], known[i][2]);
            }

            if (placed) {
                return true;
            }

            foreach (ItemEntity item in all) {
                grid.RemoveItem(item);
            }
        }

        float best = float.MinValue;
        List<int[]> bestLayout = null;
        ItemEntity first = starts[0];
        foreach (int rotation in Rotations(first)) {
            for (int y = 0; y < grid.Height; y++) {
                for (int x = 0; x < grid.Width; x++) {
                    if (!grid.PlaceItem(first, x, y, rotation)) {
                        continue;
                    }

                    bool fits = true;
                    foreach (ItemEntity item in chain.Skip(1)) {
                        fits &= PlaceBest(doll, grid, item);
                    }

                    foreach (ItemEntity item in rest) {
                        fits &= fits && (grid.TryPlaceFirstAvailable(item, out _, out _) || TryAnyRotation(grid, item));
                    }

                    float score = fits ? Score(doll, grid) : float.MinValue;
                    if (score > best + Epsilon) {
                        best = score;
                        bestLayout = all.Select(item => new[] { item.Grid.CurrentPos[0], item.Grid.CurrentPos[1], item.Grid.Rotation }).ToList();
                    }

                    foreach (ItemEntity item in all) {
                        grid.RemoveItem(item);
                    }
                }
            }
        }

        if (bestLayout == null) {
            return false;
        }

        ChainLayouts[key] = bestLayout;
        bool done = true;
        for (int i = 0; i < all.Count; i++) {
            done &= grid.PlaceItem(all[i], bestLayout[i][0], bestLayout[i][1], bestLayout[i][2]);
        }

        return done;
    }

    // The spot and rotation that score best with what is already down; the first free one when nothing helps.
    private static bool PlaceBest(DollEntity doll, BackpackGrid grid, ItemEntity item) {
        float best = float.MinValue;
        int bx = -1, by = -1, brot = 0;
        foreach (int rotation in Rotations(item)) {
            for (int y = 0; y < grid.Height; y++) {
                for (int x = 0; x < grid.Width; x++) {
                    if (!grid.CanPlaceItem(item, x, y, rotation)) {
                        continue;
                    }

                    grid.PlaceItem(item, x, y, rotation);
                    float trial = Score(doll, grid);
                    grid.RemoveItem(item);
                    if (trial > best + Epsilon) {
                        best = trial;
                        bx = x;
                        by = y;
                        brot = rotation;
                    }
                }
            }
        }

        return bx >= 0 && grid.PlaceItem(item, bx, by, brot);
    }

    private static bool TryAnyRotation(BackpackGrid grid, ItemEntity item) {
        foreach (int rotation in Rotations(item)) {
            for (int y = 0; y < grid.Height; y++) {
                for (int x = 0; x < grid.Width; x++) {
                    if (grid.PlaceItem(item, x, y, rotation)) {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static float Score(DollEntity doll, BackpackGrid grid) {
        GridSolver.RecalculateAllEffects(doll);
        float score = 0f;
        foreach (ItemEntity item in grid.ContainedItems) {
            if (item?.Combat == null) {
                continue;
            }

            if (item.ItemType == nameof(ItemType.Weapon)) {
                score += item.Combat.RuntimeDamage;
            }

            foreach (EffectData effect in item.Combat.Effects ?? new List<EffectData>()) {
                score += RelationValue * Relations(item, effect, grid);
            }

            score += ChainValue(item, grid);
        }

        return score;
    }

    // A trigger-chain start is worth what its line fires (GDD_01 §5.1A): each working module a relation, plus half the
    // damage and the shields it sets off for free. Linking one more module is what moves the arranger.
    private static float ChainValue(ItemEntity start, BackpackGrid grid) {
        if (!TriggerChain.HasOutPort(start) || start.Combat.TriggerType != TriggerType.Manual.ToString()) {
            return 0f;
        }

        float value = 0f;
        foreach (ChainStep step in TriggerChain.Plan(start, grid, false)) {
            if (!TriggerChain.DoesSomething(step.Item)) {
                value += 1f;
                continue;
            }

            value += RelationValue;
            if (TriggerChain.IsStrike(step.Item)) {
                value += 0.5f * step.Item.Combat.RuntimeDamage * (1f + step.Bonus);
            } else if (step.Item.Combat.DamageType == nameof(DamageType.Shield)) {
                value += step.Item.Combat.Effects.Where(effect => effect?.EffectID == "AddRuntimeShield" && effect.Params != null && effect.Params.Length > 0).Sum(effect => effect.Params[0]);
            }
        }

        return value;
    }

    // Neighbours an effect will use in a fight: the weapons a charge reaches, the tagged items whose use refunds AP.
    private static int Relations(ItemEntity item, EffectData effect, BackpackGrid grid) {
        if (effect == null) {
            return 0;
        }

        bool charge = effect.EffectID == "ChargeNeighbor";
        bool answersUse = effect.Trigger == CombatItemEffects.NeighborUseTrigger;
        if (!charge && !answersUse) {
            return 0;
        }

        return GridSolver.Neighbors(item, effect.Target, grid)
            .Count(neighbour => CombatItemEffects.HasTag(neighbour, effect.Tag) && neighbour.Combat != null && (!charge || neighbour.ItemType == nameof(ItemType.Weapon)));
    }

    private static IEnumerable<int> Rotations(ItemEntity item) {
        int steps = BackpackGrid.GetAllowedRotationSteps(item);
        for (int step = 0; step < steps; step++) {
            int rotation = step * BackpackGrid.RotationStep;
            if (BackpackGrid.IsRotationAllowed(item, rotation)) {
                yield return rotation;
            }
        }
    }
}
