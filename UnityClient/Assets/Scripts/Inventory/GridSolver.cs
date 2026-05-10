using System;
using System.Collections.Generic;
using UnityEngine;

public static class GridSolver {
    
    // Recalculates all combat stats and applies adjacency effects
    public static void RecalculateAllEffects(DollEntity doll) {
        if (doll.RuntimeGrid == null) return;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        if (grid == null) return;

        // 1. Reset Runtime Stats
        foreach (var item in grid.ContainedItems) {
            if (item.Combat != null) {
                // Reset damage to base value
                item.Combat.RuntimeDamage = item.Combat.BaseValue;
            }
        }

        // 2. Apply Adjacency Effects from Items
        foreach (var providerItem in grid.ContainedItems) {
            if (providerItem.Combat == null || providerItem.Combat.Effects == null) continue;
            
            foreach (var effectData in providerItem.Combat.Effects) {
                EffectBase effect = EffectFactory.CreateEffect(effectData);
                if (effect == null) continue;

                List<ItemEntity> targets = GetTargetItems(providerItem, effectData.Target, grid);
                
                foreach (var targetItem in targets) {
                    effect.Apply(providerItem, targetItem);
                }
            }
        }

        // 3. Apply Global Effects from Prosthetics
        foreach (string prosID in doll.EquippedProsthetics) {
            if (ConfigManager.Prosthetics.TryGetValue(prosID, out var prosConfig)) {
                if (prosConfig.Effects == null) {
                    continue;
                }

                foreach (var effectData in prosConfig.Effects) {
                    EffectBase effect = EffectFactory.CreateEffect(effectData);
                    if (effect == null) continue;

                    foreach (var targetItem in GetProstheticTargetItems(effectData.Target, grid)) {
                        effect.Apply(null, targetItem);
                    }
                }
            }
        }

        GameRoot.Core?.Combat?.MonsterRuntimeModifiers?.ApplyToGrid(grid);
    }

    private static List<ItemEntity> GetProstheticTargetItems(string targetSelector, BackpackGrid grid) {
        List<ItemEntity> targets = new List<ItemEntity>();
        if (grid == null || grid.ContainedItems == null) {
            return targets;
        }

        if (string.IsNullOrEmpty(targetSelector) || targetSelector == TargetDirection.Global.ToString()) {
            targets.AddRange(grid.ContainedItems);
            return targets;
        }

        foreach (var item in grid.ContainedItems) {
            if (item?.Tags != null && item.Tags.Exists(tag => string.Equals(tag, targetSelector, StringComparison.OrdinalIgnoreCase))) {
                targets.Add(item);
            }
        }

        return targets;
    }

    private static List<ItemEntity> GetTargetItems(ItemEntity provider, string targetDirectionStr, BackpackGrid grid) {
        List<ItemEntity> targets = new List<ItemEntity>();
        
        if (!Enum.TryParse(targetDirectionStr, out TargetDirection dir)) {
            Debug.LogWarning($"[GridSolver] Unknown target direction: {targetDirectionStr}");
            return targets;
        }

        if (dir == TargetDirection.Self) {
            targets.Add(provider);
            return targets;
        }
        
        if (dir == TargetDirection.Global) {
            targets.AddRange(grid.ContainedItems);
            return targets;
        }

        // Get the occupied cells of the provider
        List<int[]> providerCells = grid.GetOccupiedCells(provider, provider.Grid.CurrentPos[0], provider.Grid.CurrentPos[1]);
        if (providerCells.Count == 0) return targets;

        // Helper function to check if a coordinate belongs to the provider itself
        bool IsProviderCell(int x, int y) {
            return providerCells.Exists(c => c[0] == x && c[1] == y);
        }

        // Helper function to try adding a target item at a specific coordinate
        void TryAddTarget(int x, int y) {
            if (IsProviderCell(x, y)) return; // Ignore self
            ItemEntity target = grid.GetItemAt(x, y);
            if (target != null && !targets.Contains(target)) {
                targets.Add(target);
            }
        }

        // Define directional vectors for scanning (Right, Left, Up, Down)
        // Unity 2D grids commonly use: right=(1,0), left=(-1,0), up=(0,1) or (0,-1), down=(0,-1) or (0,1)
        // We'll define: right(+x), left(-x), up(-y), down(+y) 
        int[][] scanDirections = new int[0][];

        switch (dir) {
            case TargetDirection.Right:
                scanDirections = new int[][] { new int[] { 1, 0 } };
                break;
            case TargetDirection.Left:
                scanDirections = new int[][] { new int[] { -1, 0 } };
                break;
            case TargetDirection.Up:
                scanDirections = new int[][] { new int[] { 0, -1 } };
                break;
            case TargetDirection.Down:
                scanDirections = new int[][] { new int[] { 0, 1 } };
                break;
            case TargetDirection.AllAdjacent:
                scanDirections = new int[][] { 
                    new int[] { 1, 0 }, new int[] { -1, 0 }, 
                    new int[] { 0, 1 }, new int[] { 0, -1 } 
                };
                break;
        }

        // For each cell the provider occupies, check the scanning directions
        foreach (var cell in providerCells) {
            foreach (var offset in scanDirections) {
                TryAddTarget(cell[0] + offset[0], cell[1] + offset[1]);
            }
        }
        
        return targets;
    }
}
