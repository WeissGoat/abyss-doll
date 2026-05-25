using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DollInteractionReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public string Header;
    public DollInteractionType InteractionType;
    public DollInteractionScene Scene;
    public DollInteractionOutcomeType OutcomeType;
    public string DollID;
    public string DollName;
    public int Day;
    public bool Accepted;
    public bool ItemConsumed;
    public bool LimitReached;
    public bool StressReaction;
    public string GiftItemID;
    public string GiftItemName;
    public string TypeText;
    public string SceneText;
    public string OutcomeText;
    public string BondDeltaText;
    public string SANDeltaText;
    public string GiftText;
    public string ConsumptionText;
    public string DailyCountText;
    public string FeedbackText;
    public string CombinedText;
    public List<string> WarningLines = new List<string>();
}

public class DollInteractionDailyReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public string Header;
    public int Day;
    public int TotalTouchCount;
    public int EffectiveTouchCount;
    public int TotalTalkCount;
    public int EffectiveTalkCount;
    public int TotalGiftCount;
    public int AcceptedGiftCount;
    public int RejectedGiftCount;
    public string TouchCountText;
    public string TalkCountText;
    public string GiftCountText;
    public string RepeatTouchText;
    public string CombinedText;
    public List<string> RecentLogLines = new List<string>();
}

public static class DollInteractionReadabilityTextService {
    public static DollInteractionReadabilitySnapshot BuildResultSnapshot(
        DollInteractionResult result,
        DollDailyInteractionState dailyState = null) {
        if (result == null) {
            return BuildResultFailureSnapshot("Doll interaction result is missing.");
        }

        DollInteractionReadabilitySnapshot snapshot = new DollInteractionReadabilitySnapshot {
            Success = result.Success,
            Reason = result.Reason,
            Header = "人偶交互",
            InteractionType = result.InteractionType,
            Scene = result.Scene,
            OutcomeType = result.OutcomeType,
            DollID = result.DollID,
            DollName = string.IsNullOrEmpty(result.DollName) ? result.DollID : result.DollName,
            Day = Mathf.Max(1, result.Day),
            Accepted = result.Accepted,
            ItemConsumed = result.ItemConsumed,
            LimitReached = result.LimitReached,
            StressReaction = result.StressReaction,
            GiftItemID = result.GiftItemID,
            GiftItemName = ResolveGiftName(result),
            TypeText = FormatInteractionType(result.InteractionType),
            SceneText = FormatScene(result.Scene),
            OutcomeText = FormatOutcome(result),
            BondDeltaText = BuildDeltaText("Bond", result.BondBefore, result.BondAfter, result.BondDelta),
            SANDeltaText = BuildDeltaText("SAN", result.SANBefore, result.SANAfter, result.SANDelta),
            GiftText = BuildGiftText(result),
            ConsumptionText = BuildConsumptionText(result),
            DailyCountText = BuildDailyCountText(dailyState, result.InteractionType),
            FeedbackText = string.IsNullOrEmpty(result.FeedbackText) ? result.Reason : result.FeedbackText
        };

        AppendResultWarnings(snapshot);
        snapshot.CombinedText = BuildResultCombinedText(snapshot);
        return snapshot;
    }

    public static DollInteractionDailyReadabilitySnapshot BuildDailySnapshot(
        PlayerProfile player,
        int day = 0,
        int recentLogLimit = 5) {
        if (player == null) {
            return BuildDailyFailureSnapshot("Player profile is missing.");
        }

        int targetDay = day > 0 ? day : Mathf.Max(1, player.CurrentDay);
        DollDailyInteractionState state = FindDailyState(player.DollInteractionState, targetDay);
        return BuildDailySnapshot(state, targetDay, recentLogLimit);
    }

    public static DollInteractionDailyReadabilitySnapshot BuildDailySnapshot(
        DollDailyInteractionState state,
        int day = 0,
        int recentLogLimit = 5) {
        int targetDay = day > 0 ? day : Mathf.Max(1, state?.Day ?? 1);
        DollInteractionDailyReadabilitySnapshot snapshot = new DollInteractionDailyReadabilitySnapshot {
            Success = true,
            Header = "今日人偶交互",
            Day = targetDay
        };

        if (state == null) {
            snapshot.TouchCountText = $"第 {targetDay} 天触摸 0 次 / 有效 0/{DollInteractionService.TouchEffectiveDailyLimit}";
            snapshot.TalkCountText = $"对话 0 次 / 有效 0/{DollInteractionService.TalkEffectiveDailyLimit}";
            snapshot.GiftCountText = "赠礼 0 次 / 接受 0 / 拒绝 0";
            snapshot.RepeatTouchText = "连续触摸: 无";
            snapshot.CombinedText = BuildDailyCombinedText(snapshot);
            return snapshot;
        }

        snapshot.TotalTouchCount = Mathf.Max(0, state.TotalTouchCount);
        snapshot.EffectiveTouchCount = Mathf.Max(0, state.EffectiveTouchCount);
        snapshot.TotalTalkCount = Mathf.Max(0, state.TotalTalkCount);
        snapshot.EffectiveTalkCount = Mathf.Max(0, state.EffectiveTalkCount);
        snapshot.TotalGiftCount = Mathf.Max(0, state.TotalGiftCount);
        snapshot.AcceptedGiftCount = Mathf.Max(0, state.AcceptedGiftCount);
        snapshot.RejectedGiftCount = Mathf.Max(0, state.RejectedGiftCount);
        snapshot.TouchCountText = $"第 {targetDay} 天触摸 {snapshot.TotalTouchCount} 次 / 有效 {snapshot.EffectiveTouchCount}/{DollInteractionService.TouchEffectiveDailyLimit}";
        snapshot.TalkCountText = $"对话 {snapshot.TotalTalkCount} 次 / 有效 {snapshot.EffectiveTalkCount}/{DollInteractionService.TalkEffectiveDailyLimit}";
        snapshot.GiftCountText = $"赠礼 {snapshot.TotalGiftCount} 次 / 接受 {snapshot.AcceptedGiftCount} / 拒绝 {snapshot.RejectedGiftCount}";
        snapshot.RepeatTouchText = BuildRepeatTouchText(state);
        AppendRecentLogs(snapshot, state, recentLogLimit);
        snapshot.CombinedText = BuildDailyCombinedText(snapshot);
        return snapshot;
    }

    private static DollInteractionReadabilitySnapshot BuildResultFailureSnapshot(string reason) {
        DollInteractionReadabilitySnapshot snapshot = new DollInteractionReadabilitySnapshot {
            Success = false,
            Reason = reason,
            Header = "人偶交互",
            TypeText = "交互不可用",
            SceneText = "场景未知",
            OutcomeText = "失败",
            BondDeltaText = "Bond 无变化",
            SANDeltaText = "SAN 无变化",
            DailyCountText = "今日计数不可用",
            FeedbackText = reason
        };
        snapshot.CombinedText = BuildResultCombinedText(snapshot);
        return snapshot;
    }

    private static DollInteractionDailyReadabilitySnapshot BuildDailyFailureSnapshot(string reason) {
        DollInteractionDailyReadabilitySnapshot snapshot = new DollInteractionDailyReadabilitySnapshot {
            Success = false,
            Reason = reason,
            Header = "今日人偶交互",
            TouchCountText = "交互计数不可用",
            TalkCountText = reason,
            GiftCountText = reason,
            RepeatTouchText = "连续触摸: 未知"
        };
        snapshot.CombinedText = BuildDailyCombinedText(snapshot);
        return snapshot;
    }

    private static DollDailyInteractionState FindDailyState(DollInteractionRuntimeState runtimeState, int day) {
        if (runtimeState?.DailyStates == null) {
            return null;
        }

        int safeDay = Mathf.Max(1, day);
        foreach (DollDailyInteractionState state in runtimeState.DailyStates) {
            if (state != null && state.Day == safeDay) {
                return state;
            }
        }

        return null;
    }

    private static string ResolveGiftName(DollInteractionResult result) {
        if (result?.GiftItem == null) {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(result.GiftItem.Name)) {
            return result.GiftItem.Name;
        }

        return result.GiftItem.ConfigID;
    }

    private static string BuildGiftText(DollInteractionResult result) {
        if (result == null || result.InteractionType != DollInteractionType.Gift) {
            return string.Empty;
        }

        string itemName = ResolveGiftName(result);
        string itemID = string.IsNullOrEmpty(result.GiftItemID) ? result.GiftItem?.ConfigID : result.GiftItemID;
        if (string.IsNullOrEmpty(itemName) && string.IsNullOrEmpty(itemID)) {
            return "礼物: 未知物品";
        }

        if (string.IsNullOrEmpty(itemID)) {
            return $"礼物: {itemName}";
        }

        if (string.IsNullOrEmpty(itemName) || string.Equals(itemName, itemID, StringComparison.OrdinalIgnoreCase)) {
            return $"礼物: {itemID}";
        }

        return $"礼物: {itemName} ({itemID})";
    }

    private static string BuildConsumptionText(DollInteractionResult result) {
        if (result == null || result.InteractionType != DollInteractionType.Gift) {
            return string.Empty;
        }

        if (result.ItemConsumed) {
            return "礼物已消耗";
        }

        if (!result.Accepted || result.OutcomeType == DollInteractionOutcomeType.Rejected) {
            return "礼物未消耗";
        }

        return "礼物消耗状态未知";
    }

    private static string BuildDailyCountText(DollDailyInteractionState state, DollInteractionType type) {
        if (state == null) {
            return "今日计数不可用";
        }

        switch (type) {
            case DollInteractionType.Touch:
                return $"今日触摸 {Mathf.Max(0, state.TotalTouchCount)} 次 / 有效 {Mathf.Max(0, state.EffectiveTouchCount)}/{DollInteractionService.TouchEffectiveDailyLimit} / {BuildRepeatTouchText(state)}";
            case DollInteractionType.Talk:
                return $"今日对话 {Mathf.Max(0, state.TotalTalkCount)} 次 / 有效 {Mathf.Max(0, state.EffectiveTalkCount)}/{DollInteractionService.TalkEffectiveDailyLimit}";
            case DollInteractionType.Gift:
                return $"今日赠礼 {Mathf.Max(0, state.TotalGiftCount)} 次 / 接受 {Mathf.Max(0, state.AcceptedGiftCount)} / 拒绝 {Mathf.Max(0, state.RejectedGiftCount)}";
            default:
                return "今日计数不可用";
        }
    }

    private static string BuildRepeatTouchText(DollDailyInteractionState state) {
        if (state == null || string.IsNullOrEmpty(state.LastTouchRegion) || state.RepeatedTouchRegionCount <= 0) {
            return "连续触摸: 无";
        }

        return $"连续触摸: {state.LastTouchRegion} x{Mathf.Max(0, state.RepeatedTouchRegionCount)}";
    }

    private static void AppendRecentLogs(DollInteractionDailyReadabilitySnapshot snapshot, DollDailyInteractionState state, int recentLogLimit) {
        if (snapshot == null || state?.LogLines == null || state.LogLines.Count == 0) {
            return;
        }

        int safeLimit = recentLogLimit <= 0 ? state.LogLines.Count : Mathf.Min(recentLogLimit, state.LogLines.Count);
        int startIndex = Mathf.Max(0, state.LogLines.Count - safeLimit);
        for (int i = startIndex; i < state.LogLines.Count; i++) {
            if (!string.IsNullOrEmpty(state.LogLines[i])) {
                snapshot.RecentLogLines.Add(state.LogLines[i]);
            }
        }
    }

    private static void AppendResultWarnings(DollInteractionReadabilitySnapshot snapshot) {
        if (snapshot == null) {
            return;
        }

        if (snapshot.LimitReached) {
            snapshot.WarningLines.Add("本日收益已到上限，本次不再产生数值收益。");
        }

        if (snapshot.StressReaction) {
            snapshot.WarningLines.Add("触发压力反馈，后续应优先处理 SAN 或维护状态。");
        }

        if (!snapshot.Success && !string.IsNullOrEmpty(snapshot.Reason)) {
            snapshot.WarningLines.Add(snapshot.Reason);
        }
    }

    private static string BuildResultCombinedText(DollInteractionReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header ?? "人偶交互");
        if (!string.IsNullOrEmpty(snapshot.DollName) || !string.IsNullOrEmpty(snapshot.DollID)) {
            builder.AppendLine($"{snapshot.DollName} ({snapshot.DollID})");
        }

        builder.AppendLine($"{snapshot.TypeText} / {snapshot.OutcomeText} / {snapshot.SceneText}");
        AppendLineIfNotEmpty(builder, snapshot.BondDeltaText);
        AppendLineIfNotEmpty(builder, snapshot.SANDeltaText);
        AppendLineIfNotEmpty(builder, snapshot.GiftText);
        AppendLineIfNotEmpty(builder, snapshot.ConsumptionText);
        AppendLineIfNotEmpty(builder, snapshot.DailyCountText);
        AppendLineIfNotEmpty(builder, snapshot.FeedbackText);

        if (snapshot.WarningLines.Count > 0) {
            builder.AppendLine("提示:");
            foreach (string line in snapshot.WarningLines) {
                if (!string.IsNullOrEmpty(line)) {
                    builder.AppendLine($"- {line}");
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildDailyCombinedText(DollInteractionDailyReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header ?? "今日人偶交互");
        if (snapshot.Day > 0) {
            builder.AppendLine($"第 {snapshot.Day} 天");
        }

        AppendLineIfNotEmpty(builder, snapshot.TouchCountText);
        AppendLineIfNotEmpty(builder, snapshot.TalkCountText);
        AppendLineIfNotEmpty(builder, snapshot.GiftCountText);
        AppendLineIfNotEmpty(builder, snapshot.RepeatTouchText);

        if (snapshot.RecentLogLines.Count > 0) {
            builder.AppendLine("最近记录:");
            foreach (string line in snapshot.RecentLogLines) {
                if (!string.IsNullOrEmpty(line)) {
                    builder.AppendLine($"- {line}");
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendLineIfNotEmpty(StringBuilder builder, string text) {
        if (!string.IsNullOrEmpty(text)) {
            builder.AppendLine(text);
        }
    }

    private static string BuildDeltaText(string label, int before, int after, int delta) {
        if (delta == 0) {
            return $"{label} 无变化 ({before}->{after})";
        }

        string sign = delta > 0 ? "+" : string.Empty;
        return $"{label} {sign}{delta} ({before}->{after})";
    }

    private static string FormatInteractionType(DollInteractionType type) {
        switch (type) {
            case DollInteractionType.Touch:
                return "触摸";
            case DollInteractionType.Talk:
                return "对话";
            case DollInteractionType.Gift:
                return "赠礼";
            default:
                return type.ToString();
        }
    }

    private static string FormatScene(DollInteractionScene scene) {
        switch (scene) {
            case DollInteractionScene.Workshop:
                return "工坊";
            case DollInteractionScene.SafeZone:
                return "安全区";
            case DollInteractionScene.DollRoom:
                return "人偶房间";
            case DollInteractionScene.Preparation:
                return "整备";
            case DollInteractionScene.Night:
                return "夜晚";
            case DollInteractionScene.Business:
                return "经营";
            default:
                return scene.ToString();
        }
    }

    private static string FormatOutcome(DollInteractionResult result) {
        if (result == null) {
            return "失败";
        }

        switch (result.OutcomeType) {
            case DollInteractionOutcomeType.Accepted:
                return result.Accepted ? "已接受" : "已处理";
            case DollInteractionOutcomeType.Limited:
                return "已达上限";
            case DollInteractionOutcomeType.Rejected:
                return "已拒绝";
            case DollInteractionOutcomeType.Stress:
                return "压力反馈";
            case DollInteractionOutcomeType.Failed:
                return "失败";
            default:
                return result.Success ? "已处理" : "失败";
        }
    }
}
