using System;
using System.Collections.Generic;
using System.Text;

public enum DiveReadinessIssueSeverity {
    Info,
    Warning,
    Blocker
}

public enum DiveReadinessIssueCode {
    MissingPlayer,
    InvalidLayer,
    LayerConfigMissing,
    LayerLocked,
    MissingActiveDoll,
    ExtremeWear,
    HeavyWearWarning,
    ExtremeCorruption,
    HighCorruptionWarning,
    MissingChassis,
    InvalidChassisID,
    InvalidChassisDimensions,
    InvalidChassisMask,
    MissingRuntimeGrid,
    RuntimeGridMismatch,
    InvalidProstheticReference,
    InvalidProstheticSlot,
    DuplicateProstheticSlot,
    AutoUnequippedProsthetic
}

public class DiveReadinessIssue {
    public DiveReadinessIssueCode Code;
    public DiveReadinessIssueSeverity Severity;
    public string Message;
    public string ActionHint;

    public DiveReadinessIssue(DiveReadinessIssueCode code, DiveReadinessIssueSeverity severity, string message, string actionHint = "") {
        Code = code;
        Severity = severity;
        Message = message;
        ActionHint = actionHint;
    }
}

public class DiveReadinessResult {
    public int LayerID;
    public bool ProstheticsChanged;
    public List<DiveReadinessIssue> Issues = new List<DiveReadinessIssue>();
    public List<string> RemovedProstheticIDs = new List<string>();

    public bool CanDive {
        get { return !HasBlockers(); }
    }

    public bool HasBlockers() {
        foreach (DiveReadinessIssue issue in Issues) {
            if (issue != null && issue.Severity == DiveReadinessIssueSeverity.Blocker) {
                return true;
            }
        }

        return false;
    }

    public void AddIssue(DiveReadinessIssueCode code, DiveReadinessIssueSeverity severity, string message, string actionHint = "") {
        Issues.Add(new DiveReadinessIssue(code, severity, message, actionHint));
    }

    public string BuildSummary() {
        if (Issues.Count == 0) {
            return "Ready to dive.";
        }

        StringBuilder builder = new StringBuilder();
        AppendIssues(builder, DiveReadinessIssueSeverity.Blocker);
        if (builder.Length == 0) {
            AppendIssues(builder, DiveReadinessIssueSeverity.Warning);
        }
        if (builder.Length == 0) {
            AppendIssues(builder, DiveReadinessIssueSeverity.Info);
        }

        return builder.Length > 0 ? builder.ToString() : "Ready to dive.";
    }

    private void AppendIssues(StringBuilder builder, DiveReadinessIssueSeverity severity) {
        foreach (DiveReadinessIssue issue in Issues) {
            if (issue == null || issue.Severity != severity) {
                continue;
            }

            if (builder.Length > 0) {
                builder.Append(" ");
            }

            builder.Append(issue.Message);
            if (!string.IsNullOrEmpty(issue.ActionHint)) {
                builder.Append(" ");
                builder.Append(issue.ActionHint);
            }
        }
    }
}

public static class DiveReadinessService {
    public const float ExtremeWearThreshold = 90f;
    public const float HeavyWearWarningThreshold = 60f;
    public const float ExtremeCorruptionThreshold = 90f;
    public const float HighCorruptionWarningThreshold = 70f;

    public static DiveReadinessResult Evaluate(PlayerProfile player, int layerID, bool autoUnequipIllegalProsthetics = false) {
        DiveReadinessResult result = new DiveReadinessResult {
            LayerID = layerID
        };

        ValidateLayer(player, layerID, result);
        DollEntity doll = player != null ? player.ActiveDoll : null;
        ValidateDoll(doll, result, autoUnequipIllegalProsthetics && !result.HasBlockers());
        return result;
    }

    private static void ValidateLayer(PlayerProfile player, int layerID, DiveReadinessResult result) {
        if (player == null) {
            result.AddIssue(
                DiveReadinessIssueCode.MissingPlayer,
                DiveReadinessIssueSeverity.Blocker,
                "Player profile is missing.",
                "Initialize player profile before entering the abyss.");
            return;
        }

        if (layerID < 1) {
            result.AddIssue(
                DiveReadinessIssueCode.InvalidLayer,
                DiveReadinessIssueSeverity.Blocker,
                "Layer ID must be >= 1.",
                "Choose a valid abyss layer.");
            return;
        }

        if (!ConfigManager.Dungeons.ContainsKey(layerID)) {
            result.AddIssue(
                DiveReadinessIssueCode.LayerConfigMissing,
                DiveReadinessIssueSeverity.Blocker,
                $"Layer {layerID} config does not exist.",
                "Check Dungeons config.");
            return;
        }

        if (layerID > player.HighestUnlockedDungeonLayer) {
            result.AddIssue(
                DiveReadinessIssueCode.LayerLocked,
                DiveReadinessIssueSeverity.Blocker,
                $"Layer {layerID} is locked. HighestUnlocked={player.HighestUnlockedDungeonLayer}.",
                "Clear the previous layer first.");
        }
    }

    private static void ValidateDoll(DollEntity doll, DiveReadinessResult result, bool autoUnequipIllegalProsthetics) {
        if (doll == null) {
            result.AddIssue(
                DiveReadinessIssueCode.MissingActiveDoll,
                DiveReadinessIssueSeverity.Blocker,
                "Active doll is missing.",
                "Select or initialize a doll before diving.");
            return;
        }

        ValidateStatus(doll, result);
        ValidateChassis(doll, result);
        ValidateProsthetics(doll, result, autoUnequipIllegalProsthetics && !result.HasBlockers());
    }

    private static void ValidateStatus(DollEntity doll, DiveReadinessResult result) {
        if (doll.Status.WearAndTear >= ExtremeWearThreshold) {
            result.AddIssue(
                DiveReadinessIssueCode.ExtremeWear,
                DiveReadinessIssueSeverity.Blocker,
                $"Wear is too high ({doll.Status.WearAndTear:0}/{ExtremeWearThreshold:0}).",
                "Perform maintenance before diving.");
        } else if (doll.Status.WearAndTear >= HeavyWearWarningThreshold) {
            result.AddIssue(
                DiveReadinessIssueCode.HeavyWearWarning,
                DiveReadinessIssueSeverity.Warning,
                $"Wear is high ({doll.Status.WearAndTear:0}).",
                "Diving is allowed, but maintenance is recommended.");
        }

        if (doll.Status.Corruption >= ExtremeCorruptionThreshold) {
            result.AddIssue(
                DiveReadinessIssueCode.ExtremeCorruption,
                DiveReadinessIssueSeverity.Blocker,
                $"Corruption is too high ({doll.Status.Corruption:0}/{ExtremeCorruptionThreshold:0}).",
                "Cleanse corruption or rest before diving.");
        } else if (doll.Status.Corruption >= HighCorruptionWarningThreshold) {
            result.AddIssue(
                DiveReadinessIssueCode.HighCorruptionWarning,
                DiveReadinessIssueSeverity.Warning,
                $"Corruption is high ({doll.Status.Corruption:0}).",
                "Diving is allowed, but crisis risk should be shown.");
        }
    }

    private static void ValidateChassis(DollEntity doll, DiveReadinessResult result) {
        ChassisComponent chassis = doll.Chassis;
        if (chassis == null) {
            result.AddIssue(
                DiveReadinessIssueCode.MissingChassis,
                DiveReadinessIssueSeverity.Blocker,
                "Doll has no chassis.",
                "Install a chassis before diving.");
            return;
        }

        if (string.IsNullOrWhiteSpace(chassis.ChassisID)) {
            result.AddIssue(
                DiveReadinessIssueCode.InvalidChassisID,
                DiveReadinessIssueSeverity.Blocker,
                "Chassis ID is empty.",
                "Check chassis configuration.");
        }

        if (chassis.GridWidth <= 0 || chassis.GridHeight <= 0) {
            result.AddIssue(
                DiveReadinessIssueCode.InvalidChassisDimensions,
                DiveReadinessIssueSeverity.Blocker,
                $"Chassis grid size is invalid ({chassis.GridWidth}x{chassis.GridHeight}).",
                "Fix chassis grid dimensions.");
        }

        if (!IsChassisMaskValid(chassis)) {
            result.AddIssue(
                DiveReadinessIssueCode.InvalidChassisMask,
                DiveReadinessIssueSeverity.Blocker,
                "Chassis grid mask does not cover configured dimensions.",
                "Fix chassis GridMask.");
        }

        BackpackGrid runtimeGrid = doll.RuntimeGrid as BackpackGrid;
        if (runtimeGrid == null) {
            result.AddIssue(
                DiveReadinessIssueCode.MissingRuntimeGrid,
                DiveReadinessIssueSeverity.Blocker,
                "Runtime backpack grid is missing.",
                "Rebuild the doll backpack grid.");
            return;
        }

        if (runtimeGrid.Width != chassis.GridWidth || runtimeGrid.Height != chassis.GridHeight) {
            result.AddIssue(
                DiveReadinessIssueCode.RuntimeGridMismatch,
                DiveReadinessIssueSeverity.Blocker,
                $"Runtime backpack grid ({runtimeGrid.Width}x{runtimeGrid.Height}) does not match chassis ({chassis.GridWidth}x{chassis.GridHeight}).",
                "Rebuild the backpack grid from the current chassis.");
        }
    }

    private static bool IsChassisMaskValid(ChassisComponent chassis) {
        if (chassis == null || chassis.GridWidth <= 0 || chassis.GridHeight <= 0) {
            return false;
        }

        if (chassis.GridMask == null) {
            return true;
        }

        if (chassis.GridMask.Length < chassis.GridWidth) {
            return false;
        }

        for (int x = 0; x < chassis.GridWidth; x++) {
            if (chassis.GridMask[x] == null || chassis.GridMask[x].Length < chassis.GridHeight) {
                return false;
            }
        }

        return true;
    }

    private static void ValidateProsthetics(DollEntity doll, DiveReadinessResult result, bool autoUnequipIllegalProsthetics) {
        if (doll.EquippedProsthetics == null || doll.EquippedProsthetics.Count == 0) {
            return;
        }

        HashSet<string> occupiedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<int> removeIndices = new List<int>();

        for (int i = 0; i < doll.EquippedProsthetics.Count; i++) {
            string prostheticID = doll.EquippedProsthetics[i];
            if (string.IsNullOrWhiteSpace(prostheticID)) {
                AddAutoFixableProstheticIssue(result, DiveReadinessIssueCode.InvalidProstheticReference, "Equipped prosthetic ID is empty.", "Remove the invalid prosthetic reference.");
                removeIndices.Add(i);
                continue;
            }

            if (!ConfigManager.Prosthetics.TryGetValue(prostheticID, out ProstheticEntity prosthetic)) {
                AddAutoFixableProstheticIssue(result, DiveReadinessIssueCode.InvalidProstheticReference, $"Equipped prosthetic [{prostheticID}] is missing from config.", "Remove or restore the prosthetic config.");
                removeIndices.Add(i);
                continue;
            }

            if (string.IsNullOrWhiteSpace(prosthetic.SlotType)) {
                AddAutoFixableProstheticIssue(result, DiveReadinessIssueCode.InvalidProstheticSlot, $"Equipped prosthetic [{prostheticID}] has no slot.", "Fix Prosthetics config or unequip it.");
                removeIndices.Add(i);
                continue;
            }

            if (!occupiedSlots.Add(prosthetic.SlotType)) {
                AddAutoFixableProstheticIssue(result, DiveReadinessIssueCode.DuplicateProstheticSlot, $"Multiple prosthetics use slot [{prosthetic.SlotType}].", "Only one prosthetic can occupy a slot.");
                removeIndices.Add(i);
            }
        }

        if (autoUnequipIllegalProsthetics && removeIndices.Count > 0) {
            RemoveInvalidProsthetics(doll, result, removeIndices);
        }
    }

    private static void AddAutoFixableProstheticIssue(DiveReadinessResult result, DiveReadinessIssueCode code, string message, string actionHint) {
        result.AddIssue(code, DiveReadinessIssueSeverity.Warning, message, actionHint);
    }

    private static void RemoveInvalidProsthetics(DollEntity doll, DiveReadinessResult result, List<int> removeIndices) {
        for (int i = removeIndices.Count - 1; i >= 0; i--) {
            int index = removeIndices[i];
            if (index < 0 || index >= doll.EquippedProsthetics.Count) {
                continue;
            }

            string removedID = doll.EquippedProsthetics[index];
            doll.EquippedProsthetics.RemoveAt(index);
            result.ProstheticsChanged = true;
            result.RemovedProstheticIDs.Add(removedID);
            result.AddIssue(
                DiveReadinessIssueCode.AutoUnequippedProsthetic,
                DiveReadinessIssueSeverity.Info,
                $"Auto-unequipped illegal prosthetic reference [{removedID}].");
        }

        GridSolver.RecalculateAllEffects(doll);
    }
}
