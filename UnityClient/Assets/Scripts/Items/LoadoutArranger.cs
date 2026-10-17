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

        int moves = 0;
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
        }

        return score;
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
