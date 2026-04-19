using UnityEngine;
using System.Collections.Generic;

public static class StageTwoTest {
    
    public static void RunAcceptanceTest() {
        Debug.Log("\n--- [Stage 2 Acceptance Test] Starting ---");

        // 1. Initialize Effect Factory
        EffectFactory.Initialize();

        // 2. Create a mock chassis (5x5 grid, all cells available)
        ChassisComponent mockChassis = new ChassisComponent {
            GridWidth = 5,
            GridHeight = 5,
            GridMask = new bool[][] {
                new bool[] {true, true, true, true, true},
                new bool[] {true, true, true, true, true},
                new bool[] {true, true, true, true, true},
                new bool[] {true, true, true, true, true},
                new bool[] {true, true, true, true, true}
            }
        };

        // 3. Create a mock BackpackGrid
        BackpackGrid grid = new BackpackGrid(mockChassis);

        // 4. Create a Sword (1x3)
        ItemEntity sword = new ItemEntity {
            InstanceID = "sword_01",
            Name = "Test Sword",
            Grid = new ItemGridComponent {
                Shape = new int[][] { new int[]{0,0}, new int[]{0,1}, new int[]{0,2} }, // 1x3 shape (vertical)
                Rotation = 0
            },
            Combat = new ItemCombatComponent {
                BaseValue = 100,
                RuntimeDamage = 100
            }
        };

        // 5. Create a Damage Core (1x1) that buffs the item to its Right
        ItemEntity dmgCore = new ItemEntity {
            InstanceID = "core_01",
            Name = "Damage Core (+30%)",
            Grid = new ItemGridComponent {
                Shape = new int[][] { new int[]{0,0} }, // 1x1 shape
                Rotation = 0
            },
            Combat = new ItemCombatComponent {
                Effects = new List<EffectData> {
                    new EffectData {
                        EffectID = "DamageMultiplier",
                        Level = 0,
                        Target = "Right", // Pointing right
                        Params = new float[] { 0.30f } // 30% buff
                    }
                }
            }
        };

        // 6. Place items in the grid
        // Place core at (1, 1)
        bool placedCore = grid.PlaceItem(dmgCore, 1, 1);
        // Place sword at (2, 1), which is exactly to the right of the core
        bool placedSword = grid.PlaceItem(sword, 2, 1);

        if (!placedCore || !placedSword) {
            Debug.LogError("[Test Failed] Could not place items in the grid.");
            return;
        }

        Debug.Log($"Items placed successfully. Sword Base Damage: {sword.Combat.BaseValue}");

        // 7. Create a mock Doll to hold the grid (needed by GridSolver)
        DollEntity mockDoll = new DollEntity {
            RuntimeGrid = grid,
            EquippedProsthetics = new List<string>() // Empty for this test
        };

        // 8. Run the Solver!
        GridSolver.RecalculateAllEffects(mockDoll);

        // 9. Verify the result
        Debug.Log($"[Test Result] Sword Runtime Damage after calculation: {sword.Combat.RuntimeDamage}");
        
        if (Mathf.Approximately(sword.Combat.RuntimeDamage, 130f)) {
            Debug.Log("<color=green>[Test Passed] Stage 2 Acceptance Criteria Met! Damage increased by exactly 30%.</color>");
        } else {
            Debug.LogError($"<color=red>[Test Failed] Expected 130, got {sword.Combat.RuntimeDamage}.</color>");
        }
        
        Debug.Log("--- [Stage 2 Acceptance Test] Finished ---\n");
    }
}
