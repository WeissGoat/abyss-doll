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
        RunRotationPermissionRules();
        RunRotatedDirectionEffectRules();
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
        }, 20, true, 4);

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
        }, 1, true, 4);
        lockedProbe.Grid.Rotation = 90;
        bool lockedRejected = InventoryInteractionService.PreviewPlacement(lockedProbe, 1, 1, context, out BackpackPlacementResult lockedPreview, out _)
            && !lockedPreview.CanPlace
            && lockedPreview.Failure == BackpackPlacementFailure.LockedCell;

        ItemEntity collisionProbe = CreateTestItem("collision_probe", "Collision Probe", new int[][] {
            new int[] { 0, 0 }
        }, 1, false, 1);
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

    private static void RunRotationPermissionRules() {
        ChassisComponent chassis = new ChassisComponent {
            GridWidth = 3,
            GridHeight = 3,
            GridMask = new bool[][] {
                new bool[] { true, true, true },
                new bool[] { true, true, true },
                new bool[] { true, true, true }
            }
        };

        DollEntity doll = new DollEntity {
            Name = "Inventory Rotation Permission Test Doll",
            Chassis = chassis,
            RuntimeGrid = new BackpackGrid(chassis),
            EquippedProsthetics = new List<string>()
        };

        GameRoot.Core.CurrentPlayer.ActiveDoll = doll;
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionRotationPermissionRules");

        ItemEntity fixedItem = CreateTestItem("fixed_item", "Fixed Item", new int[][] {
            new int[] { 0, 0 },
            new int[] { 0, 1 }
        }, 1, false, 1);

        bool fixedRotationRejected = !InventoryInteractionService.RequestRotateHeldItem(fixedItem, 90, context, out int rejectedRotation, out string fixedRotateReason)
            && rejectedRotation == 0
            && fixedItem.Grid.Rotation == 0
            && fixedRotateReason.Contains("不可旋转");

        bool fixedPlacementRejectsRotatedAngle = InventoryInteractionService.PreviewPlacement(
                fixedItem,
                0,
                0,
                90,
                context,
                out BackpackPlacementResult fixedPreview,
                out _)
            && !fixedPreview.CanPlace
            && fixedPreview.Failure == BackpackPlacementFailure.RotationNotAllowed;

        ItemEntity twoStepItem = CreateTestItem("two_step_item", "Two Step Item", new int[][] {
            new int[] { 0, 0 },
            new int[] { 0, 1 }
        }, 1, true, 2);

        bool twoStepFirstRotation = InventoryInteractionService.RequestRotateHeldItem(twoStepItem, 90, context, out int firstRotation, out string firstRotateReason)
            && firstRotation == 90
            && twoStepItem.Grid.Rotation == 90;
        bool twoStepWrapsToZero = InventoryInteractionService.RequestRotateHeldItem(twoStepItem, 90, context, out int wrappedRotation, out string wrappedRotateReason)
            && wrappedRotation == 0
            && twoStepItem.Grid.Rotation == 0;
        bool twoStepRejects180Placement = InventoryInteractionService.PreviewPlacement(
                twoStepItem,
                0,
                0,
                180,
                context,
                out BackpackPlacementResult twoStepPreview,
                out _)
            && !twoStepPreview.CanPlace
            && twoStepPreview.Failure == BackpackPlacementFailure.RotationNotAllowed;

        if (fixedRotationRejected
            && fixedPlacementRejectsRotatedAngle
            && twoStepFirstRotation
            && twoStepWrapsToZero
            && twoStepRejects180Placement) {
            Debug.Log("Inventory Interaction Rotation Permission Rules PASSED.");
        } else {
            Debug.LogError($"Inventory Interaction Rotation Permission Rules FAILED. FixedRotate={fixedRotationRejected} ({fixedRotateReason}), FixedPlacement={fixedPlacementRejectsRotatedAngle}, First={twoStepFirstRotation} ({firstRotateReason}), Wrap={twoStepWrapsToZero} ({wrappedRotateReason}), Reject180={twoStepRejects180Placement}");
        }
    }

    private static void RunRotatedDirectionEffectRules() {
        ChassisComponent chassis = new ChassisComponent {
            GridWidth = 3,
            GridHeight = 3,
            GridMask = new bool[][] {
                new bool[] { true, true, true },
                new bool[] { true, true, true },
                new bool[] { true, true, true }
            }
        };

        DollEntity doll = new DollEntity {
            Name = "Inventory Rotated Direction Test Doll",
            Chassis = chassis,
            RuntimeGrid = new BackpackGrid(chassis),
            EquippedProsthetics = new List<string>()
        };

        GameRoot.Core.CurrentPlayer.ActiveDoll = doll;
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("InventoryInteractionRotatedDirectionRules");
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        ItemEntity target = CreateTestItem("direction_target", "Direction Target", new int[][] {
            new int[] { 0, 0 }
        }, 10, false, 1);

        ItemEntity rotatedProvider = CreateAmplifier("rotated_provider", TargetDirection.Right.ToString(), 0.5f);
        rotatedProvider.Grid.CanRotate = true;
        rotatedProvider.Grid.RotationSteps = 4;
        rotatedProvider.Grid.Rotation = 90;

        bool targetPlaced = InventoryInteractionService.RequestPlace(target, 2, 1, context, out string targetPlaceReason);
        bool providerPlaced = InventoryInteractionService.RequestPlace(rotatedProvider, 2, 0, context, out string providerPlaceReason);
        bool rotatedDirectionAppliedDown = targetPlaced
            && providerPlaced
            && grid.GetItemAt(2, 1) == target
            && Mathf.Approximately(target.Combat.RuntimeDamage, 15f);

        if (rotatedDirectionAppliedDown) {
            Debug.Log("Inventory Interaction Rotated Direction Rules PASSED.");
        } else {
            Debug.LogError($"Inventory Interaction Rotated Direction Rules FAILED. TargetPlaced={targetPlaced} ({targetPlaceReason}), ProviderPlaced={providerPlaced} ({providerPlaceReason}), Damage={target.Combat.RuntimeDamage}");
        }
    }

    private static ItemEntity CreateTestItem(string instanceID, string name, int[][] shape, int baseDamage, bool canRotate, int rotationSteps) {
        return new ItemEntity {
            InstanceID = instanceID,
            ConfigID = instanceID,
            Name = name,
            ItemType = nameof(ItemType.Weapon),
            Grid = new ItemGridComponent {
                Shape = shape,
                CanRotate = canRotate,
                RotationSteps = rotationSteps,
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
        ItemEntity item = CreateTestItem(instanceID, "Direction Amplifier", new int[][] { new int[] { 0, 0 } }, 0, false, 1);
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
