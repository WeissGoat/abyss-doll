using System.Linq;
using UnityEngine;

public static class DollInteractionServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Doll Interaction Service Smoke Test ===");

        TestTouchDailyLimit();
        TestRepeatedTouchRegionLimit();
        TestLowSanSensitiveTouchStress();
        TestAcceptedGiftConsumesItem();
        TestRejectedGiftDoesNotConsumeItem();
        TestTalkLimitAndSafeZonePermission();

        Debug.Log("=== Doll Interaction Service Smoke Test Finished ===");
    }

    private static void TestTouchDailyLimit() {
        CoreBackend core = CreateCore();
        DollEntity doll = PrepareDoll(core);

        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Shoulder);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Back);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hair);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Head);
        DollInteractionResult sixth = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Shoulder);

        bool passed = doll.Bond.AffectionLevel == DollInteractionService.TouchEffectiveDailyLimit
            && sixth.Success
            && sixth.LimitReached
            && sixth.BondDelta == 0
            && core.CurrentPlayer.DollInteractionState.GetOrCreateDailyState(core.CurrentPlayer.CurrentDay).EffectiveTouchCount == DollInteractionService.TouchEffectiveDailyLimit;

        if (passed) {
            Debug.Log("Doll Interaction Touch Daily Limit PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Touch Daily Limit FAILED. Bond={doll.Bond.AffectionLevel}, SixthSuccess={sixth.Success}, Limit={sixth.LimitReached}, SixthDelta={sixth.BondDelta}, Text={sixth.CombinedText}");
        }
    }

    private static void TestRepeatedTouchRegionLimit() {
        CoreBackend core = CreateCore();
        DollEntity doll = PrepareDoll(core);

        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);
        DollInteractionResult fourth = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);

        bool passed = doll.Bond.AffectionLevel == 3
            && fourth.Success
            && fourth.LimitReached
            && fourth.BondDelta == 0
            && fourth.OutcomeType == DollInteractionOutcomeType.Limited;

        if (passed) {
            Debug.Log("Doll Interaction Repeated Touch Region Limit PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Repeated Touch Region Limit FAILED. Bond={doll.Bond.AffectionLevel}, FourthSuccess={fourth.Success}, Limit={fourth.LimitReached}, Delta={fourth.BondDelta}, Outcome={fourth.OutcomeType}, Text={fourth.CombinedText}");
        }
    }

    private static void TestLowSanSensitiveTouchStress() {
        CoreBackend core = CreateCore();
        DollEntity doll = PrepareDoll(core);
        doll.Bond.AffectionLevel = 2;
        doll.Status.SAN_Current = 5;
        doll.Status.SAN_Max = 100;

        DollInteractionResult result = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Face);

        bool passed = result.Success
            && result.StressReaction
            && result.OutcomeType == DollInteractionOutcomeType.Stress
            && result.BondDelta == -1
            && result.SANDelta == -1
            && doll.Bond.AffectionLevel == 1
            && doll.Status.SAN_Current == 4;

        if (passed) {
            Debug.Log("Doll Interaction Low SAN Sensitive Touch PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Low SAN Sensitive Touch FAILED. Success={result.Success}, Stress={result.StressReaction}, BondDelta={result.BondDelta}, SanDelta={result.SANDelta}, Bond={doll.Bond.AffectionLevel}, SAN={doll.Status.SAN_Current}, Text={result.CombinedText}");
        }
    }

    private static void TestAcceptedGiftConsumesItem() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = PrepareDoll(core);
        player.StashInventory.Clear();
        ItemEntity gift = ConfigManager.CreateItem("loot_gear_scrap");
        player.StashInventory.Add(gift);

        DollInteractionResult result = DollInteractionService.GiveGift(player, gift);

        bool passed = result.Success
            && result.Accepted
            && result.ItemConsumed
            && result.BondDelta > 0
            && doll.Bond.AffectionLevel == result.BondAfter
            && !player.StashInventory.Contains(gift)
            && gift.ContainerType == ItemContainerType.Consumed
            && gift.OwnerScope == ItemOwnerScope.Consumed;

        if (passed) {
            Debug.Log("Doll Interaction Accepted Gift Consumes Item PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Accepted Gift Consumes Item FAILED. Success={result.Success}, Accepted={result.Accepted}, Consumed={result.ItemConsumed}, BondDelta={result.BondDelta}, StashContains={player.StashInventory.Contains(gift)}, Container={gift.ContainerType}, Text={result.CombinedText}");
        }
    }

    private static void TestRejectedGiftDoesNotConsumeItem() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = PrepareDoll(core);
        player.StashInventory.Clear();
        doll.Bond.AffectionLevel = 3;
        ItemEntity cursedGift = new ItemEntity {
            InstanceID = "test_cursed_gift",
            ConfigID = "test_cursed_gift",
            Name = "Test Cursed Gift",
            ItemType = nameof(ItemType.Loot),
            Rarity = nameof(ItemRarity.Cursed),
            BaseValue = 100,
            Tags = { "Cursed" }
        };
        player.StashInventory.Add(cursedGift);

        DollInteractionResult result = DollInteractionService.GiveGift(player, cursedGift);

        bool passed = result.Success
            && !result.Accepted
            && !result.ItemConsumed
            && result.OutcomeType == DollInteractionOutcomeType.Rejected
            && result.BondDelta < 0
            && player.StashInventory.Contains(cursedGift)
            && cursedGift.ContainerType == ItemContainerType.Unknown;

        if (passed) {
            Debug.Log("Doll Interaction Rejected Gift Does Not Consume Item PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Rejected Gift Does Not Consume Item FAILED. Success={result.Success}, Accepted={result.Accepted}, Consumed={result.ItemConsumed}, Outcome={result.OutcomeType}, BondDelta={result.BondDelta}, StashContains={player.StashInventory.Contains(cursedGift)}, Container={cursedGift.ContainerType}, Text={result.CombinedText}");
        }
    }

    private static void TestTalkLimitAndSafeZonePermission() {
        CoreBackend core = CreateCore();
        DollEntity doll = PrepareDoll(core);

        DollInteractionResult safeZoneTalk = DollInteractionService.ExecuteTalk(core.CurrentPlayer, DollInteractionScene.SafeZone, "AfterDungeon");
        DollInteractionService.ExecuteTalk(core.CurrentPlayer, DollInteractionScene.Workshop);
        DollInteractionService.ExecuteTalk(core.CurrentPlayer, DollInteractionScene.Workshop);
        DollInteractionResult fourth = DollInteractionService.ExecuteTalk(core.CurrentPlayer, DollInteractionScene.Workshop);
        DollInteractionResult businessTalk = DollInteractionService.ExecuteTalk(core.CurrentPlayer, DollInteractionScene.Business);

        bool passed = safeZoneTalk.Success
            && safeZoneTalk.BondDelta == 2
            && doll.Bond.AffectionLevel == 4
            && fourth.Success
            && fourth.LimitReached
            && fourth.BondDelta == 0
            && !businessTalk.Success
            && businessTalk.OutcomeType == DollInteractionOutcomeType.Failed;

        if (passed) {
            Debug.Log("Doll Interaction Talk Limit And Scene Permission PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Talk Limit And Scene Permission FAILED. SafeSuccess={safeZoneTalk.Success}, SafeDelta={safeZoneTalk.BondDelta}, Bond={doll.Bond.AffectionLevel}, FourthLimit={fourth.LimitReached}, FourthDelta={fourth.BondDelta}, BusinessSuccess={businessTalk.Success}, BusinessReason={businessTalk.Reason}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        return core;
    }

    private static DollEntity PrepareDoll(CoreBackend core) {
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Bond.AffectionLevel = 0;
        doll.Bond.HiddenTrust = 0;
        doll.Status.HP_Current = Mathf.Max(1, doll.Status.HP_Max);
        doll.Status.SAN_Max = Mathf.Max(100, doll.Status.SAN_Max);
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        core.CurrentPlayer.DollInteractionState = new DollInteractionRuntimeState();
        return doll;
    }
}
