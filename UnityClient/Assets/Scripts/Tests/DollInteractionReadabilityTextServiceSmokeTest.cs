using System.Linq;
using UnityEngine;

public static class DollInteractionReadabilityTextServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Doll Interaction Readability Text Service Smoke Test ===");

        TestAcceptedTouchSnapshot();
        TestLimitedTouchSnapshot();
        TestLowSanStressSnapshot();
        TestAcceptedGiftSnapshot();
        TestRejectedGiftSnapshot();
        TestDailyStateSnapshot();

        Debug.Log("=== Doll Interaction Readability Text Service Smoke Test Finished ===");
    }

    private static void TestAcceptedTouchSnapshot() {
        CoreBackend core = CreateCore();
        PrepareDoll(core);

        DollInteractionResult result = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);
        DollDailyInteractionState dailyState = GetDailyState(core.CurrentPlayer);
        DollInteractionReadabilitySnapshot snapshot = DollInteractionReadabilityTextService.BuildResultSnapshot(result, dailyState);

        bool passed = snapshot.Success
            && snapshot.TypeText == "触摸"
            && snapshot.OutcomeText == "已接受"
            && snapshot.BondDeltaText.Contains("+1")
            && snapshot.SANDeltaText.Contains("无变化")
            && snapshot.DailyCountText.Contains("有效 1/5")
            && snapshot.CombinedText.Contains("人偶交互");

        if (passed) {
            Debug.Log("Doll Interaction Readability Accepted Touch Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Readability Accepted Touch Snapshot FAILED. Success={snapshot.Success}, Type={snapshot.TypeText}, Outcome={snapshot.OutcomeText}, Bond={snapshot.BondDeltaText}, Daily={snapshot.DailyCountText}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestLimitedTouchSnapshot() {
        CoreBackend core = CreateCore();
        PrepareDoll(core);

        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hand);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Shoulder);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Back);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Hair);
        DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Head);
        DollInteractionResult limited = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Shoulder);

        DollInteractionReadabilitySnapshot snapshot = DollInteractionReadabilityTextService.BuildResultSnapshot(limited, GetDailyState(core.CurrentPlayer));

        bool passed = snapshot.Success
            && snapshot.LimitReached
            && snapshot.OutcomeText == "已达上限"
            && snapshot.BondDeltaText.Contains("无变化")
            && snapshot.WarningLines.Any(line => line.Contains("上限"))
            && snapshot.CombinedText.Contains("提示");

        if (passed) {
            Debug.Log("Doll Interaction Readability Limited Touch Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Readability Limited Touch Snapshot FAILED. Limit={snapshot.LimitReached}, Outcome={snapshot.OutcomeText}, Bond={snapshot.BondDeltaText}, Warnings={snapshot.WarningLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestLowSanStressSnapshot() {
        CoreBackend core = CreateCore();
        DollEntity doll = PrepareDoll(core);
        doll.Bond.AffectionLevel = 2;
        doll.Status.SAN_Current = 5;
        doll.Status.SAN_Max = 100;

        DollInteractionResult result = DollInteractionService.ExecuteTouch(core.CurrentPlayer, DollTouchRegion.Face);
        DollInteractionReadabilitySnapshot snapshot = DollInteractionReadabilityTextService.BuildResultSnapshot(result, GetDailyState(core.CurrentPlayer));

        bool passed = snapshot.Success
            && snapshot.StressReaction
            && snapshot.OutcomeText == "压力反馈"
            && snapshot.BondDeltaText.Contains("-1")
            && snapshot.SANDeltaText.Contains("-1")
            && snapshot.WarningLines.Any(line => line.Contains("压力反馈"));

        if (passed) {
            Debug.Log("Doll Interaction Readability Low SAN Stress Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Readability Low SAN Stress Snapshot FAILED. Stress={snapshot.StressReaction}, Outcome={snapshot.OutcomeText}, Bond={snapshot.BondDeltaText}, SAN={snapshot.SANDeltaText}, Warnings={snapshot.WarningLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestAcceptedGiftSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        PrepareDoll(core);
        player.StashInventory.Clear();
        ItemEntity gift = ConfigManager.CreateItem("loot_gear_scrap");
        player.StashInventory.Add(gift);

        DollInteractionResult result = DollInteractionService.GiveGift(player, gift);
        DollInteractionReadabilitySnapshot snapshot = DollInteractionReadabilityTextService.BuildResultSnapshot(result, GetDailyState(player));

        bool passed = snapshot.Success
            && snapshot.Accepted
            && snapshot.ItemConsumed
            && snapshot.TypeText == "赠礼"
            && snapshot.GiftText.Contains("loot_gear_scrap")
            && snapshot.ConsumptionText == "礼物已消耗"
            && snapshot.BondDeltaText.Contains("+")
            && snapshot.DailyCountText.Contains("接受 1");

        if (passed) {
            Debug.Log("Doll Interaction Readability Accepted Gift Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Readability Accepted Gift Snapshot FAILED. Success={snapshot.Success}, Accepted={snapshot.Accepted}, Consumed={snapshot.ItemConsumed}, Gift={snapshot.GiftText}, Consumption={snapshot.ConsumptionText}, Daily={snapshot.DailyCountText}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestRejectedGiftSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = PrepareDoll(core);
        player.StashInventory.Clear();
        doll.Bond.AffectionLevel = 3;
        ItemEntity cursedGift = new ItemEntity {
            InstanceID = "test_readability_cursed_gift",
            ConfigID = "test_readability_cursed_gift",
            Name = "测试诅咒礼物",
            ItemType = nameof(ItemType.Loot),
            Rarity = nameof(ItemRarity.Cursed),
            BaseValue = 100,
            Tags = { "Cursed" }
        };
        player.StashInventory.Add(cursedGift);

        DollInteractionResult result = DollInteractionService.GiveGift(player, cursedGift);
        DollInteractionReadabilitySnapshot snapshot = DollInteractionReadabilityTextService.BuildResultSnapshot(result, GetDailyState(player));

        bool passed = snapshot.Success
            && !snapshot.Accepted
            && !snapshot.ItemConsumed
            && snapshot.OutcomeText == "已拒绝"
            && snapshot.ConsumptionText == "礼物未消耗"
            && snapshot.BondDeltaText.Contains("-")
            && snapshot.DailyCountText.Contains("拒绝 1");

        if (passed) {
            Debug.Log("Doll Interaction Readability Rejected Gift Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Readability Rejected Gift Snapshot FAILED. Success={snapshot.Success}, Accepted={snapshot.Accepted}, Consumed={snapshot.ItemConsumed}, Outcome={snapshot.OutcomeText}, Consumption={snapshot.ConsumptionText}, Daily={snapshot.DailyCountText}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestDailyStateSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        PrepareDoll(core);
        player.StashInventory.Clear();
        ItemEntity gift = ConfigManager.CreateItem("loot_gear_scrap");
        player.StashInventory.Add(gift);

        DollInteractionService.ExecuteTouch(player, DollTouchRegion.Hand);
        DollInteractionService.ExecuteTalk(player, DollInteractionScene.Workshop);
        DollInteractionService.GiveGift(player, gift);

        DollInteractionDailyReadabilitySnapshot snapshot = DollInteractionReadabilityTextService.BuildDailySnapshot(player, recentLogLimit: 2);

        bool passed = snapshot.Success
            && snapshot.Day == player.CurrentDay
            && snapshot.TotalTouchCount == 1
            && snapshot.EffectiveTouchCount == 1
            && snapshot.TotalTalkCount == 1
            && snapshot.EffectiveTalkCount == 1
            && snapshot.TotalGiftCount == 1
            && snapshot.AcceptedGiftCount == 1
            && snapshot.RecentLogLines.Count == 2
            && snapshot.CombinedText.Contains("最近记录");

        if (passed) {
            Debug.Log("Doll Interaction Readability Daily State Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Interaction Readability Daily State Snapshot FAILED. Success={snapshot.Success}, Touch={snapshot.TotalTouchCount}/{snapshot.EffectiveTouchCount}, Talk={snapshot.TotalTalkCount}/{snapshot.EffectiveTalkCount}, Gift={snapshot.TotalGiftCount}/{snapshot.AcceptedGiftCount}, Logs={snapshot.RecentLogLines.Count}, Text={snapshot.CombinedText}");
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

    private static DollDailyInteractionState GetDailyState(PlayerProfile player) {
        return player.DollInteractionState.GetOrCreateDailyState(player.CurrentDay);
    }
}
