using System.Collections.Generic;
using UnityEngine;

public interface IRewardRandom {
    int Range(int minInclusive, int maxExclusive);
}

public class UnityRewardRandom : IRewardRandom {
    public int Range(int minInclusive, int maxExclusive) {
        return Random.Range(minInclusive, maxExclusive);
    }
}

public class RewardContext {
    public string SourceType;
    public string SourceID;
    public int LayerID;
    public string NodeID;
    public PlayerProfile Player;
    public DollEntity ActiveDoll;
}

public class RewardGrant {
    public string Type;
    public string ItemID;
    public string RewardID;
    public int Money;
    public int Count;
    public string SourceRewardID;
    public string SourcePoolID;
}

public class RewardRollResult {
    public string RootRewardID;
    public List<RewardGrant> Grants = new List<RewardGrant>();
    public List<ItemEntity> GeneratedItems = new List<ItemEntity>();
    public int Money;
    public List<string> Logs = new List<string>();
}

public class RewardSystem {
    private const int MaxRewardRefDepth = 8;
    private readonly IRewardRandom _random;

    public RewardSystem(IRewardRandom random = null) {
        _random = random ?? new UnityRewardRandom();
    }

    public RewardRollResult Roll(string rewardID, RewardContext context) {
        RewardRollResult result = new RewardRollResult {
            RootRewardID = rewardID
        };

        RollInto(rewardID, context, result, 0, new HashSet<string>());
        return result;
    }

    private void RollInto(string rewardID, RewardContext context, RewardRollResult result, int depth, HashSet<string> stack) {
        if (string.IsNullOrEmpty(rewardID)) {
            result.Logs.Add("[RewardSystem] Empty RewardID skipped.");
            return;
        }

        if (depth > MaxRewardRefDepth) {
            Debug.LogError($"[RewardSystem] RewardRef depth exceeded while rolling {rewardID}.");
            return;
        }

        if (stack.Contains(rewardID)) {
            Debug.LogError($"[RewardSystem] RewardRef cycle detected at {rewardID}.");
            return;
        }

        if (!ConfigManager.Rewards.TryGetValue(rewardID, out RewardConfig config) || config == null) {
            Debug.LogWarning($"[RewardSystem] Reward config missing: {rewardID}");
            return;
        }

        stack.Add(rewardID);
        RollGuaranteed(config, context, result, depth, stack);
        RollWeightedPools(config, context, result, depth, stack);
        stack.Remove(rewardID);
    }

    private void RollGuaranteed(RewardConfig config, RewardContext context, RewardRollResult result, int depth, HashSet<string> stack) {
        if (config.Guaranteed == null) {
            return;
        }

        foreach (RewardEntry entry in config.Guaranteed) {
            ApplyEntry(config.RewardID, "Guaranteed", entry, context, result, depth, stack);
        }
    }

    private void RollWeightedPools(RewardConfig config, RewardContext context, RewardRollResult result, int depth, HashSet<string> stack) {
        if (config.WeightedPools == null) {
            return;
        }

        foreach (RewardPool pool in config.WeightedPools) {
            if (pool == null || pool.Entries == null || pool.Entries.Count == 0) {
                Debug.LogWarning($"[RewardSystem] Reward [{config.RewardID}] has an empty weighted pool.");
                continue;
            }

            List<RewardEntry> availableEntries = new List<RewardEntry>(pool.Entries);
            int rollCount = Mathf.Max(0, pool.RollCount);
            for (int i = 0; i < rollCount; i++) {
                RewardEntry selected = PickWeightedEntry(config.RewardID, pool, availableEntries);
                if (selected == null) {
                    break;
                }

                ApplyEntry(config.RewardID, pool.PoolID, selected, context, result, depth, stack);
                if (!pool.AllowDuplicate) {
                    availableEntries.Remove(selected);
                }
            }
        }
    }

    private RewardEntry PickWeightedEntry(string rewardID, RewardPool pool, List<RewardEntry> entries) {
        int totalWeight = 0;
        foreach (RewardEntry entry in entries) {
            if (entry != null && entry.Weight > 0) {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0) {
            Debug.LogWarning($"[RewardSystem] Reward [{rewardID}] pool [{pool?.PoolID ?? "unknown"}] has no positive weights.");
            return null;
        }

        int roll = _random.Range(0, totalWeight);
        int cursor = 0;
        foreach (RewardEntry entry in entries) {
            if (entry == null || entry.Weight <= 0) {
                continue;
            }

            cursor += entry.Weight;
            if (roll < cursor) {
                return entry;
            }
        }

        return null;
    }

    private void ApplyEntry(string sourceRewardID, string sourcePoolID, RewardEntry entry, RewardContext context, RewardRollResult result, int depth, HashSet<string> stack) {
        if (entry == null) {
            return;
        }

        string type = string.IsNullOrEmpty(entry.Type) ? "Item" : entry.Type;
        switch (type) {
            case "Nothing":
                result.Logs.Add($"[RewardSystem] Nothing rolled from {sourceRewardID}/{sourcePoolID}.");
                break;
            case "Item":
                GrantItems(sourceRewardID, sourcePoolID, entry, result);
                break;
            case "Money":
                GrantMoney(sourceRewardID, sourcePoolID, entry, result);
                break;
            case "RewardRef":
                RollInto(entry.RewardID, context, result, depth + 1, stack);
                break;
            default:
                Debug.LogWarning($"[RewardSystem] Unsupported reward entry type [{type}] in {sourceRewardID}.");
                break;
        }
    }

    private void GrantItems(string sourceRewardID, string sourcePoolID, RewardEntry entry, RewardRollResult result) {
        if (string.IsNullOrEmpty(entry.ItemID)) {
            Debug.LogError($"[RewardSystem] Item reward in [{sourceRewardID}] is missing ItemID.");
            return;
        }

        int count = ResolveCount(entry);
        for (int i = 0; i < count; i++) {
            ItemEntity item = ConfigManager.CreateItem(entry.ItemID);
            if (item == null) {
                Debug.LogError($"[RewardSystem] Item reward points to missing item: {entry.ItemID}");
                continue;
            }

            result.GeneratedItems.Add(item);
        }

        result.Grants.Add(new RewardGrant {
            Type = "Item",
            ItemID = entry.ItemID,
            Count = count,
            SourceRewardID = sourceRewardID,
            SourcePoolID = sourcePoolID
        });
    }

    private void GrantMoney(string sourceRewardID, string sourcePoolID, RewardEntry entry, RewardRollResult result) {
        int money = entry.Money > 0 ? entry.Money : ResolveCount(entry);
        result.Money += money;
        result.Grants.Add(new RewardGrant {
            Type = "Money",
            Money = money,
            Count = 1,
            SourceRewardID = sourceRewardID,
            SourcePoolID = sourcePoolID
        });
    }

    private int ResolveCount(RewardEntry entry) {
        if (entry.MinCount > 0 && entry.MaxCount >= entry.MinCount) {
            return _random.Range(entry.MinCount, entry.MaxCount + 1);
        }

        return Mathf.Max(1, entry.Count);
    }
}

