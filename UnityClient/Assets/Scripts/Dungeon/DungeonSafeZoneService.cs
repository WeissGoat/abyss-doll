using UnityEngine;

public static class DungeonSafeZoneService {
    public static DungeonSafeZoneRecoveryResult RestoreActiveDollToFull(string sourceID, string reason) {
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null || doll.Status == null) {
            string failureReason = "Active doll is missing.";
            Debug.LogWarning($"[DungeonSafeZone] Recovery skipped. Source={sourceID ?? "Unknown"}, Reason={reason ?? "Unknown"}, Failure={failureReason}");
            return DungeonSafeZoneRecoveryResult.Failed(sourceID, reason, failureReason);
        }

        DollStatusComponent status = doll.Status;
        int beforeHP = status.HP_Current;
        int beforeSAN = status.SAN_Current;
        int maxHP = Mathf.Max(0, status.HP_Max);
        int maxSAN = Mathf.Max(0, status.SAN_Max);

        status.HP_Current = maxHP;
        status.SAN_Current = maxSAN;

        GameEventBus.PublishHPChanged(doll.Name, status.HP_Current, status.HP_Max);
        GameEventBus.PublishSANChanged(doll.Name, status.SAN_Current, status.SAN_Max);

        Debug.Log($"[DungeonSafeZone] Restored active doll. Source={sourceID ?? "Unknown"}, Reason={reason ?? "Unknown"}, HP={beforeHP}->{status.HP_Current}, SAN={beforeSAN}->{status.SAN_Current}");
        return DungeonSafeZoneRecoveryResult.Succeeded(sourceID, reason, beforeHP, status.HP_Current, beforeSAN, status.SAN_Current);
    }
}

public class DungeonSafeZoneRecoveryResult {
    public bool Success;
    public string SourceID;
    public string Reason;
    public string FailureReason;
    public int HPBefore;
    public int HPAfter;
    public int SANBefore;
    public int SANAfter;

    public int HPRestored => HPAfter - HPBefore;
    public int SANRestored => SANAfter - SANBefore;

    public static DungeonSafeZoneRecoveryResult Succeeded(string sourceID, string reason, int hpBefore, int hpAfter, int sanBefore, int sanAfter) {
        return new DungeonSafeZoneRecoveryResult {
            Success = true,
            SourceID = sourceID,
            Reason = reason,
            HPBefore = hpBefore,
            HPAfter = hpAfter,
            SANBefore = sanBefore,
            SANAfter = sanAfter
        };
    }

    public static DungeonSafeZoneRecoveryResult Failed(string sourceID, string reason, string failureReason) {
        return new DungeonSafeZoneRecoveryResult {
            Success = false,
            SourceID = sourceID,
            Reason = reason,
            FailureReason = failureReason
        };
    }
}
