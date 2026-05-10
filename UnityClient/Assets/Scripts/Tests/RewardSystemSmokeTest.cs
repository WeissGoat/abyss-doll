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

        if (!ConfigManager.Rewards.ContainsKey("reward_monster_elite_scrap_guard")) {
            Debug.LogError("Reward System Smoke Test FAILED. reward_monster_elite_scrap_guard not loaded.");
            return;
        }

        RewardRollResult result = new RewardSystem(new MinRewardRandom()).Roll(
            "reward_monster_elite_scrap_guard",
            new RewardContext {
                SourceType = "Monster",
                SourceID = "elite_scrap_guard",
                LayerID = 1,
                NodeID = "reward_system_smoke",
                Player = core.CurrentPlayer,
                ActiveDoll = core.CurrentPlayer.ActiveDoll
            });

        bool hasGuaranteedCore = false;
        foreach (ItemEntity item in result.GeneratedItems) {
            if (item != null && item.ConfigID == "mat_core_tier1") {
                hasGuaranteedCore = true;
                break;
            }
        }

        if (hasGuaranteedCore && result.GeneratedItems.Count >= 2 && result.Money == 0) {
            Debug.Log("Reward System Smoke Test PASSED.");
        } else {
            Debug.LogError($"Reward System Smoke Test FAILED. GeneratedItems={result.GeneratedItems.Count}, HasCore={hasGuaranteedCore}, Money={result.Money}");
        }

        Debug.Log("=== Reward System Smoke Test Finished ===");
    }
}
