using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public enum DollInteractionType {
    Touch = 0,
    Talk = 1,
    Gift = 2
}

public enum DollInteractionScene {
    Workshop = 0,
    SafeZone = 1,
    DollRoom = 2,
    Preparation = 3,
    Night = 4,
    Business = 5
}

public enum DollTouchRegion {
    None = 0,
    Head = 1,
    Face = 2,
    Shoulder = 3,
    Hand = 4,
    Back = 5,
    Hair = 6
}

public enum DollInteractionOutcomeType {
    None = 0,
    Accepted = 1,
    Limited = 2,
    Rejected = 3,
    Stress = 4,
    Failed = 5
}

public class DollInteractionResult {
    public bool Success;
    public string Reason;
    public DollInteractionType InteractionType;
    public DollInteractionScene Scene;
    public DollInteractionOutcomeType OutcomeType;
    public string DollID;
    public string DollName;
    public int Day;
    public int BondBefore;
    public int BondAfter;
    public int BondDelta;
    public int SANBefore;
    public int SANAfter;
    public int SANDelta;
    public bool LimitReached;
    public bool StressReaction;
    public bool Accepted;
    public bool ItemConsumed;
    public ItemEntity GiftItem;
    public string GiftItemID;
    public string FeedbackText;
    public string CombinedText;
}

public static class DollInteractionService {
    public const int TouchEffectiveDailyLimit = 5;
    public const int TouchRepeatRegionLimit = 3;
    public const int TalkEffectiveDailyLimit = 3;
    public const int MaxBondLevel = 10;

    public static DollInteractionResult ExecuteTouch(
        PlayerProfile player,
        DollTouchRegion region,
        DollInteractionScene scene = DollInteractionScene.Workshop) {
        DollInteractionResult result = CreateResult(DollInteractionType.Touch, scene, player);
        if (!TryResolveContext(player, out DollEntity doll, out DollDailyInteractionState dailyState, out string reason)) {
            return Fail(result, reason);
        }

        if (region == DollTouchRegion.None) {
            return Fail(result, "Touch region is missing.");
        }

        if (!CanUseInteractionInScene(DollInteractionType.Touch, scene)) {
            return Fail(result, $"Touch is not allowed in scene [{scene}].");
        }

        CaptureBefore(result, doll);
        dailyState.TotalTouchCount++;
        UpdateTouchRepeatState(dailyState, region);

        bool isBroken = doll.Status != null && doll.Status.SAN_Current <= 0;
        bool stressTouch = IsLowSanSensitiveTouch(doll, region);
        bool repeatLimited = dailyState.RepeatedTouchRegionCount > TouchRepeatRegionLimit;
        bool dailyLimited = dailyState.EffectiveTouchCount >= TouchEffectiveDailyLimit;

        if (isBroken) {
            result.Success = true;
            result.Accepted = true;
            result.OutcomeType = DollInteractionOutcomeType.Stress;
            result.StressReaction = true;
            result.FeedbackText = "Doll is broken. Comfort touch is acknowledged but cannot generate Bond.";
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        if (stressTouch) {
            ApplyBondDelta(doll, -1);
            ApplySanDelta(doll, -1);
            result.Success = true;
            result.Accepted = true;
            result.OutcomeType = DollInteractionOutcomeType.Stress;
            result.StressReaction = true;
            result.FeedbackText = $"Touching [{region}] at low SAN caused a stress reaction.";
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        if (repeatLimited || dailyLimited) {
            result.Success = true;
            result.Accepted = true;
            result.OutcomeType = DollInteractionOutcomeType.Limited;
            result.LimitReached = true;
            result.FeedbackText = repeatLimited
                ? $"Repeated touching [{region}] is now limited for today."
                : "Daily effective touch limit reached.";
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        ApplyBondDelta(doll, 1);
        dailyState.EffectiveTouchCount++;
        result.Success = true;
        result.Accepted = true;
        result.OutcomeType = DollInteractionOutcomeType.Accepted;
        result.FeedbackText = $"Touch [{region}] accepted.";
        FinalizeResult(result, doll, dailyState);
        return result;
    }

    public static DollInteractionResult ExecuteTalk(
        PlayerProfile player,
        DollInteractionScene scene = DollInteractionScene.Workshop,
        string contextTag = "") {
        DollInteractionResult result = CreateResult(DollInteractionType.Talk, scene, player);
        if (!TryResolveContext(player, out DollEntity doll, out DollDailyInteractionState dailyState, out string reason)) {
            return Fail(result, reason);
        }

        if (!CanUseInteractionInScene(DollInteractionType.Talk, scene)) {
            return Fail(result, $"Talk is not allowed in scene [{scene}].");
        }

        CaptureBefore(result, doll);
        dailyState.TotalTalkCount++;

        if (doll.Status != null && doll.Status.SAN_Current <= 0) {
            result.Success = false;
            result.Accepted = false;
            result.OutcomeType = DollInteractionOutcomeType.Rejected;
            result.Reason = "Doll is broken and cannot talk.";
            result.FeedbackText = result.Reason;
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        if (dailyState.EffectiveTalkCount >= TalkEffectiveDailyLimit) {
            result.Success = true;
            result.Accepted = true;
            result.OutcomeType = DollInteractionOutcomeType.Limited;
            result.LimitReached = true;
            result.FeedbackText = "Daily talk pool is exhausted. Fallback small talk has no numeric reward.";
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        int bondDelta = string.Equals(contextTag, "AfterDungeon", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        ApplyBondDelta(doll, bondDelta);
        dailyState.EffectiveTalkCount++;
        result.Success = true;
        result.Accepted = true;
        result.OutcomeType = DollInteractionOutcomeType.Accepted;
        result.FeedbackText = string.IsNullOrEmpty(contextTag)
            ? "Daily talk accepted."
            : $"Context talk [{contextTag}] accepted.";
        FinalizeResult(result, doll, dailyState);
        return result;
    }

    public static DollInteractionResult GiveGift(
        PlayerProfile player,
        ItemEntity giftItem,
        DollInteractionScene scene = DollInteractionScene.Workshop) {
        DollInteractionResult result = CreateResult(DollInteractionType.Gift, scene, player);
        result.GiftItem = giftItem;
        result.GiftItemID = giftItem?.ConfigID;

        if (!TryResolveContext(player, out DollEntity doll, out DollDailyInteractionState dailyState, out string reason)) {
            return Fail(result, reason);
        }

        if (!CanUseInteractionInScene(DollInteractionType.Gift, scene)) {
            return Fail(result, $"Gift is not allowed in scene [{scene}].");
        }

        if (giftItem == null) {
            return Fail(result, "Gift item is missing.");
        }

        if (!IsOwnedByPlayer(player, giftItem)) {
            return Fail(result, $"Gift item [{giftItem.Name}] is not owned by the player.");
        }

        CaptureBefore(result, doll);
        dailyState.TotalGiftCount++;

        if (doll.Status != null && doll.Status.SAN_Current <= 0) {
            result.Success = false;
            result.Accepted = false;
            result.OutcomeType = DollInteractionOutcomeType.Rejected;
            result.Reason = "Doll is broken and cannot accept gifts.";
            result.FeedbackText = result.Reason;
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        int rawBondDelta = CalculateGiftBondDelta(giftItem);
        if (ShouldRejectGift(giftItem)) {
            ApplyBondDelta(doll, Mathf.Min(-1, rawBondDelta));
            dailyState.RejectedGiftCount++;
            result.Success = true;
            result.Accepted = false;
            result.OutcomeType = DollInteractionOutcomeType.Rejected;
            result.FeedbackText = $"Gift [{giftItem.Name}] was rejected and returned.";
            FinalizeResult(result, doll, dailyState);
            return result;
        }

        int diminishedDelta = ApplyGiftDiminish(rawBondDelta, dailyState.AcceptedGiftCount);
        if (!TryConsumeGift(player, giftItem, out string consumeReason)) {
            return Fail(result, consumeReason);
        }

        ApplyBondDelta(doll, diminishedDelta);
        dailyState.AcceptedGiftCount++;
        result.Success = true;
        result.Accepted = true;
        result.ItemConsumed = true;
        result.OutcomeType = DollInteractionOutcomeType.Accepted;
        result.FeedbackText = $"Gift [{giftItem.Name}] accepted.";
        FinalizeResult(result, doll, dailyState);
        return result;
    }

    private static DollInteractionResult CreateResult(DollInteractionType type, DollInteractionScene scene, PlayerProfile player) {
        return new DollInteractionResult {
            InteractionType = type,
            Scene = scene,
            Day = Mathf.Max(1, player?.CurrentDay ?? 1)
        };
    }

    private static bool TryResolveContext(
        PlayerProfile player,
        out DollEntity doll,
        out DollDailyInteractionState dailyState,
        out string reason) {
        doll = null;
        dailyState = null;
        reason = string.Empty;

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        if (player.ActiveDoll == null) {
            reason = "Active doll is missing.";
            return false;
        }

        if (player.DollInteractionState == null) {
            player.DollInteractionState = new DollInteractionRuntimeState();
        }
        dailyState = player.DollInteractionState.GetOrCreateDailyState(player.CurrentDay);
        doll = player.ActiveDoll;
        return true;
    }

    private static bool CanUseInteractionInScene(DollInteractionType type, DollInteractionScene scene) {
        if (scene == DollInteractionScene.Workshop) {
            return true;
        }

        return type == DollInteractionType.Talk && scene == DollInteractionScene.SafeZone;
    }

    private static void CaptureBefore(DollInteractionResult result, DollEntity doll) {
        if (result == null || doll == null) {
            return;
        }

        result.DollID = doll.DollID;
        result.DollName = string.IsNullOrEmpty(doll.Name) ? doll.DollID : doll.Name;
        result.BondBefore = doll.Bond != null ? Mathf.Clamp(doll.Bond.AffectionLevel, 0, MaxBondLevel) : 0;
        result.SANBefore = doll.Status != null ? Mathf.Max(0, doll.Status.SAN_Current) : 0;
    }

    private static DollInteractionResult Fail(DollInteractionResult result, string reason) {
        result.Success = false;
        result.Accepted = false;
        result.OutcomeType = DollInteractionOutcomeType.Failed;
        result.Reason = reason;
        result.FeedbackText = reason;
        result.CombinedText = BuildCombinedText(result);
        return result;
    }

    private static void FinalizeResult(DollInteractionResult result, DollEntity doll, DollDailyInteractionState dailyState) {
        if (result == null) {
            return;
        }

        if (doll != null) {
            result.BondAfter = doll.Bond != null ? Mathf.Clamp(doll.Bond.AffectionLevel, 0, MaxBondLevel) : result.BondBefore;
            result.SANAfter = doll.Status != null ? Mathf.Max(0, doll.Status.SAN_Current) : result.SANBefore;
        } else {
            result.BondAfter = result.BondBefore;
            result.SANAfter = result.SANBefore;
        }

        result.BondDelta = result.BondAfter - result.BondBefore;
        result.SANDelta = result.SANAfter - result.SANBefore;
        result.CombinedText = BuildCombinedText(result);

        if (dailyState != null) {
            if (dailyState.LogLines == null) {
                dailyState.LogLines = new List<string>();
            }
            dailyState.LogLines.Add(result.CombinedText);
        }
    }

    private static string BuildCombinedText(DollInteractionResult result) {
        if (result == null) {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"[{result.InteractionType}] {result.OutcomeType}");
        if (!string.IsNullOrEmpty(result.DollName)) {
            builder.Append($" / {result.DollName}");
        }

        if (!string.IsNullOrEmpty(result.GiftItemID)) {
            builder.Append($" / Gift={result.GiftItemID}");
        }

        builder.Append($" / Bond {result.BondBefore}->{result.BondAfter}");
        builder.Append($" / SAN {result.SANBefore}->{result.SANAfter}");
        if (!string.IsNullOrEmpty(result.FeedbackText)) {
            builder.Append($" / {result.FeedbackText}");
        }

        return builder.ToString();
    }

    private static void UpdateTouchRepeatState(DollDailyInteractionState dailyState, DollTouchRegion region) {
        string regionText = region.ToString();
        if (string.Equals(dailyState.LastTouchRegion, regionText, StringComparison.OrdinalIgnoreCase)) {
            dailyState.RepeatedTouchRegionCount++;
        } else {
            dailyState.LastTouchRegion = regionText;
            dailyState.RepeatedTouchRegionCount = 1;
        }
    }

    private static bool IsLowSanSensitiveTouch(DollEntity doll, DollTouchRegion region) {
        if (doll?.Status == null || doll.Status.SAN_Max <= 0) {
            return false;
        }

        float ratio = (float)Mathf.Max(0, doll.Status.SAN_Current) / doll.Status.SAN_Max;
        bool sensitiveRegion = region == DollTouchRegion.Face || region == DollTouchRegion.Head;
        return sensitiveRegion && ratio < 0.1f;
    }

    private static void ApplyBondDelta(DollEntity doll, int delta) {
        if (doll == null) {
            return;
        }

        if (doll.Bond == null) {
            doll.Bond = new DollBondComponent();
        }
        doll.Bond.AffectionLevel = Mathf.Clamp(doll.Bond.AffectionLevel + delta, 0, MaxBondLevel);
    }

    private static void ApplySanDelta(DollEntity doll, int delta) {
        if (doll?.Status == null || delta == 0) {
            return;
        }

        int max = Mathf.Max(0, doll.Status.SAN_Max);
        doll.Status.SAN_Current = Mathf.Clamp(doll.Status.SAN_Current + delta, 0, max);
        GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
    }

    private static int CalculateGiftBondDelta(ItemEntity item) {
        if (item == null) {
            return 0;
        }

        if (ShouldRejectGift(item)) {
            return -2;
        }

        int delta = 1;
        if (HasTag(item, "Mechanical")) {
            delta += 2;
        }

        if (HasTag(item, "Trophy") || string.Equals(item.ItemType, nameof(ItemType.Loot), StringComparison.OrdinalIgnoreCase)) {
            delta += 1;
        }

        if (HasTag(item, "Medical") || string.Equals(item.ItemType, nameof(ItemType.Consumable), StringComparison.OrdinalIgnoreCase)) {
            delta += 1;
        }

        if (item.BaseValue >= 800) {
            delta += 1;
        }

        if (item.BaseValue >= 1200) {
            delta += 1;
        }

        return Mathf.Clamp(delta, 1, 5);
    }

    private static int ApplyGiftDiminish(int rawDelta, int acceptedGiftCountBefore) {
        if (acceptedGiftCountBefore <= 0) {
            return Mathf.Max(1, rawDelta);
        }

        float multiplier = acceptedGiftCountBefore == 1 ? 0.3f : 0.1f;
        return Mathf.Max(0, Mathf.RoundToInt(rawDelta * multiplier));
    }

    private static bool ShouldRejectGift(ItemEntity item) {
        if (item == null) {
            return true;
        }

        if (string.Equals(item.Rarity, nameof(ItemRarity.Cursed), StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        if (HasTag(item, "Cursed")) {
            return true;
        }

        return HasTag(item, "Toxic") && !HasTag(item, "Mechanical");
    }

    private static bool HasTag(ItemEntity item, string tag) {
        if (item == null || string.IsNullOrEmpty(tag)) {
            return false;
        }

        bool staticMatch = item.Tags != null && item.Tags.Any(itemTag => string.Equals(itemTag, tag, StringComparison.OrdinalIgnoreCase));
        bool dynamicMatch = item.DynamicTags != null && item.DynamicTags.Any(itemTag => string.Equals(itemTag, tag, StringComparison.OrdinalIgnoreCase));
        return staticMatch || dynamicMatch;
    }

    private static bool IsOwnedByPlayer(PlayerProfile player, ItemEntity item) {
        if (player == null || item == null) {
            return false;
        }

        if (player.StashInventory != null && player.StashInventory.Contains(item)) {
            return true;
        }

        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        return grid != null && grid.ContainedItems.Contains(item);
    }

    private static bool TryConsumeGift(PlayerProfile player, ItemEntity item, out string reason) {
        reason = string.Empty;
        if (player == null || item == null) {
            reason = "Player or gift item is missing.";
            return false;
        }

        bool removedFromBackpack = false;
        bool removed = player.StashInventory != null && player.StashInventory.Remove(item);
        if (!removed) {
            BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
            if (grid == null || !grid.ContainedItems.Contains(item)) {
                reason = $"Gift item [{item.Name}] is not owned by the player.";
                return false;
            }

            grid.RemoveItem(item);
            removed = true;
            removedFromBackpack = true;
            if (!string.IsNullOrEmpty(item.InstanceID)) {
                GameEventBus.PublishItemRemoved(item.InstanceID);
            }
        }

        if (removed) {
            MarkConsumed(item);
            if (removedFromBackpack && player.ActiveDoll != null) {
                GridSolver.RecalculateAllEffects(player.ActiveDoll);
            }
        }

        return removed;
    }

    private static void MarkConsumed(ItemEntity item) {
        if (item == null) {
            return;
        }

        item.OwnerScope = ItemOwnerScope.Consumed;
        item.ContainerType = ItemContainerType.Consumed;
        if (item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length >= 2) {
            item.Grid.CurrentPos[0] = -1;
            item.Grid.CurrentPos[1] = -1;
        }
    }
}
