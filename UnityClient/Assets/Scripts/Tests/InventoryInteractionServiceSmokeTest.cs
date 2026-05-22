using UnityEngine;
using System.Collections.Generic;

public static class InventoryInteractionServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Inventory Interaction Service Smoke Test ===");

        EffectFactory.Initialize();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity item = grid?.ContainedItems.Count > 0 ? grid.ContainedItems[0] : null;
        if (grid == null || item == null) {
            Debug.LogError("Inventory Interaction Bootstrap FAILED: missing grid or item.");
            return;
        }

        int originalX = item.Grid.CurrentPos[0];
        int originalY = item.Grid.CurrentPos[1];
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionSmoke");

        bool pickedUp = InventoryInteractionService.RequestPickUp(item, context, out InventoryItemPlacement placement, out string pickUpReason);
        bool removedFromGrid = !grid.ContainedItems.Contains(item);
        bool restored = InventoryInteractionService.RequestRestore(item, placement, context, out string restoreReason);
        bool restoredToOriginalCell = grid.GetItemAt(originalX, originalY) == item;

        InventoryInteractionContext noDiscardContext = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionSmokeNoDiscard");
        bool rejectDiscard = !InventoryInteractionService.RequestStageDiscard(item, noDiscardContext, out string rejectDiscardReason)
            && rejectDiscardReason.Contains("不允许");

        InventoryInteractionContext allowDiscardContext = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionSmokeAllowDiscard", true);
        bool allowDiscard = InventoryInteractionService.RequestStageDiscard(item, allowDiscardContext, out string allowDiscardReason);

        if (pickedUp && removedFromGrid && restored && restoredToOriginalCell && rejectDiscard && allowDiscard) {
            Debug.Log("Inventory Interaction Service PASSED.");
        } else {
            Debug.LogError($"Inventory Interaction Service FAILED. PickedUp={pickedUp} ({pickUpReason}), Removed={removedFromGrid}, Restored={restored} ({restoreReason}), OriginalCell={restoredToOriginalCell}, RejectDiscard={rejectDiscard}, AllowDiscard={allowDiscard} ({allowDiscardReason})");
        }

        RunRotationPlacementRules();
        Debug.Log("=== Inventory Interaction Service Smoke Test Finished ===");
    }

    private static void RunRotationPlacementRules() {
        ChassisComponent chassis = new ChassisComponent {
            GridWidth = 3,
            GridHeight = 3,
            GridMask = new bool[][] {
                new bool[] { true, true, true },
                new bool[] { true, false, true },
                new bool[] { true, true, true }
            }
        };

        DollEntity doll = new DollEntity {
            Name = "Inventory Interaction Test Doll",
            Chassis = chassis,
            RuntimeGrid = new BackpackGrid(chassis),
            EquippedProsthetics = new List<string>()
        };

        GameRoot.Core.CurrentPlayer.ActiveDoll = doll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionRotationRules");

        ItemEntity blade = CreateTestItem("blade", "Rotating Blade", new int[][] {
            new int[] { 0, 0 },
            new int[] { 0, 1 }
        }, 20);

        bool rotationMutated = InventoryInteractionService.RequestRotateHeldItem(blade, 90, context, out int newRotation, out string rotateReason)
            && newRotation == 90
            && blade.Grid.Rotation == 90;

        bool rotatedCanFit = InventoryInteractionService.PreviewPlacement(blade, 0, 0, context, out BackpackPlacementResult rotatedPreview, out string rotatedPreviewReason)
            && rotatedPreview.CanPlace
            && rotatedPreview.OccupiedCells.Count == 2
            && HasCell(rotatedPreview.OccupiedCells, 0, 0)
            && HasCell(rotatedPreview.OccupiedCells, 1, 0);

        bool rotatedPlaced = InventoryInteractionService.RequestPlace(blade, 0, 0, context, out string placeReason)
            && blade.Grid.Rotation == 90
            && grid.GetItemAt(0, 0) == blade
            && grid.GetItemAt(1, 0) == blade;

        ItemEntity lockedProbe = CreateTestItem("locked_probe", "Locked Probe", new int[][] {
            new int[] { 0, 0 },
            new int[] { 0, 1 }
        }, 1);
        lockedProbe.Grid.Rotation = 90;
        bool lockedRejected = InventoryInteractionService.PreviewPlacement(lockedProbe, 1, 1, context, out BackpackPlacementResult lockedPreview, out _)
            && !lockedPreview.CanPlace
            && lockedPreview.Failure == BackpackPlacementFailure.LockedCell;

        ItemEntity collisionProbe = CreateTestItem("collision_probe", "Collision Probe", new int[][] {
            new int[] { 0, 0 }
        }, 1);
        bool occupiedRejected = InventoryInteractionService.PreviewPlacement(collisionProbe, 0, 0, context, out BackpackPlacementResult occupiedPreview, out _)
            && !occupiedPreview.CanPlace
            && occupiedPreview.Failure == BackpackPlacementFailure.OccupiedCell;

        bool pickedUp = InventoryInteractionService.RequestPickUp(blade, context, out InventoryItemPlacement placement, out string pickUpReason);
        bool removedFromGrid = pickedUp && !grid.ContainedItems.Contains(blade);
        bool rotatedWhileHeld = InventoryInteractionService.RequestRotateHeldItem(blade, 90, context, out int heldRotation, out string heldRotateReason)
            && heldRotation == 180;
        bool failedPlace = !InventoryInteractionService.RequestPlace(blade, 1, 1, context, out string failedPlaceReason);
        bool restored = InventoryInteractionService.RequestRestore(blade, placement, context, out string restoreReason);
        bool restoredPlacement = restored
            && blade.Grid.Rotation == 90
            && grid.GetItemAt(0, 0) == blade
            && grid.GetItemAt(1, 0) == blade;

        ItemEntity amplifier = CreateAmplifier("amp", TargetDirection.Left.ToString(), 0.5f);
        bool amplifierPlaced = InventoryInteractionService.RequestPlace(amplifier, 2, 0, context, out string amplifierPlaceReason);
        bool directionEffectRefreshed = amplifierPlaced && Mathf.Approximately(blade.Combat.RuntimeDamage, 30f);

        if (rotationMutated
            && rotatedCanFit
            && rotatedPlaced
            && lockedRejected
            && occupiedRejected
            && removedFromGrid
            && rotatedWhileHeld
            && failedPlace
            && restoredPlacement
            && directionEffectRefreshed) {
            Debug.Log("Inventory Interaction Rotation Rules PASSED.");
        } else {
            Debug.LogError($"Inventory Interaction Rotation Rules FAILED. Rotate={rotationMutated} ({rotateReason}), Preview={rotatedCanFit} ({rotatedPreviewReason}), Place={rotatedPlaced} ({placeReason}), Locked={lockedRejected}, Occupied={occupiedRejected}, PickUp={pickedUp} ({pickUpReason}), Removed={removedFromGrid}, HeldRotate={rotatedWhileHeld} ({heldRotateReason}), FailedPlace={failedPlace} ({failedPlaceReason}), Restored={restoredPlacement} ({restoreReason}), Effect={directionEffectRefreshed} ({amplifierPlaceReason}), Damage={blade.Combat.RuntimeDamage}");
        }
    }

    private static ItemEntity CreateTestItem(string instanceID, string name, int[][] shape, int baseDamage) {
        return new ItemEntity {
            InstanceID = instanceID,
            ConfigID = instanceID,
            Name = name,
            ItemType = nameof(ItemType.Weapon),
            Grid = new ItemGridComponent {
                Shape = shape,
                Rotation = 0
            },
            Combat = new ItemCombatComponent {
                TriggerType = nameof(TriggerType.Manual),
                DamageType = nameof(DamageType.Physical),
                BaseValue = baseDamage,
                RuntimeDamage = baseDamage
            }
        };
    }

    private static ItemEntity CreateAmplifier(string instanceID, string target, float multiplier) {
        ItemEntity item = CreateTestItem(instanceID, "Direction Amplifier", new int[][] { new int[] { 0, 0 } }, 0);
        item.Combat.TriggerType = nameof(TriggerType.Passive);
        item.Combat.DamageType = nameof(DamageType.None);
        item.Combat.Effects = new List<EffectData> {
            new EffectData {
                EffectID = "DamageMultiplier",
                Target = target,
                Params = new float[] { multiplier }
            }
        };
        return item;
    }

    private static bool HasCell(List<int[]> cells, int x, int y) {
        foreach (int[] cell in cells) {
            if (cell != null && cell.Length >= 2 && cell[0] == x && cell[1] == y) {
                return true;
            }
        }

        return false;
    }
}
