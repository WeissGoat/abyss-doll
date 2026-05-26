using UnityEngine;

public static class RewardSystemSmokeTest {
    private class MinRewardRandom : IRewardRandom {
        public int Range(int minInclusive, int maxExclusive) {
            return minInclusive;
        }
    }

    public static void Run() {
        Debug.Log("=== Running Reward System Smoke Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        if (!ConfigManager.Rewards.ContainsKey("reward_boss_gatekeeper_mk1")) {
            Debug.LogError("Reward System Smoke Test FAILED. reward_boss_gatekeeper_mk1 not loaded.");
            return;
        }

        if (!ConfigManager.Rewards.ContainsKey("reward_monster_elite_scrap_guard")) {
            Debug.LogError("Reward System Smoke Test FAILED. reward_monster_elite_scrap_guard not loaded.");
            return;
        }

        RewardSystem rewardSystem = new RewardSystem(new MinRewardRandom());
        RewardRollResult bossResult = rewardSystem.Roll(
            "reward_boss_gatekeeper_mk1",
            BuildContext(core, "gatekeeper_mk1"));
        RewardRollResult eliteResult = rewardSystem.Roll(
            "reward_monster_elite_scrap_guard",
            BuildContext(core, "elite_scrap_guard"));

        bool bossPassed = HasGuaranteedGrant(bossResult, "mat_core_tier1")
            && HasGeneratedItem(bossResult, "mat_core_tier1")
            && bossResult.GeneratedItems.Count >= 2;
        if (bossPassed) {
            Debug.Log("Reward System Boss Guarantee PASSED.");
        } else {
            Debug.LogError($"Reward System Boss Guarantee FAILED. GeneratedItems={bossResult.GeneratedItems.Count}, HasGuaranteedCore={HasGuaranteedGrant(bossResult, "mat_core_tier1")}, HasCoreItem={HasGeneratedItem(bossResult, "mat_core_tier1")}, Money={bossResult.Money}");
        }

        bool elitePassed = !HasGuaranteedGrant(eliteResult, "mat_core_tier1")
            && !HasGeneratedItem(eliteResult, "mat_core_tier1")
            && eliteResult.GeneratedItems.Count >= 1;
        if (elitePassed) {
            Debug.Log("Reward System Elite Reward PASSED.");
        } else {
            Debug.LogError($"Reward System Elite Reward FAILED. GeneratedItems={eliteResult.GeneratedItems.Count}, HasGuaranteedCore={HasGuaranteedGrant(eliteResult, "mat_core_tier1")}, HasCoreItem={HasGeneratedItem(eliteResult, "mat_core_tier1")}, Money={eliteResult.Money}");
        }

        if (bossPassed && elitePassed) {
            Debug.Log("Reward System Smoke Test PASSED.");
        } else {
            Debug.LogError("Reward System Smoke Test FAILED.");
        }

        Debug.Log("=== Reward System Smoke Test Finished ===");
    }

    private static RewardContext BuildContext(CoreBackend core, string sourceID) {
        return new RewardContext {
            SourceType = "Monster",
            SourceID = sourceID,
            LayerID = 1,
            NodeID = "reward_system_smoke",
            Player = core.CurrentPlayer,
            ActiveDoll = core.CurrentPlayer.ActiveDoll
        };
    }

    private static bool HasGeneratedItem(RewardRollResult result, string itemID) {
        if (result?.GeneratedItems == null) {
            return false;
        }

        foreach (ItemEntity item in result.GeneratedItems) {
            if (item != null && item.ConfigID == itemID) {
                return true;
            }
        }

        return false;
    }

    private static bool HasGuaranteedGrant(RewardRollResult result, string itemID) {
        if (result?.Grants == null) {
            return false;
        }

        foreach (RewardGrant grant in result.Grants) {
            if (grant != null
                && grant.Type == "Item"
                && grant.ItemID == itemID
                && grant.SourcePoolID == "Guaranteed"
                && grant.Count > 0) {
                return true;
            }
        }

        return false;
    }
}
