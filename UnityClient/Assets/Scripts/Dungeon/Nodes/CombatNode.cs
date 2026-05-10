using System.Collections.Generic;
using UnityEngine;

public class CombatNode : NodeBase {
    public List<string> MonsterIDs { get; set; } = new List<string>();
    private CombatLootPickupResult _pendingLootResult;
    
    public override void Init(NodePoolEntry entry) {
        base.Init(entry);
        if (entry != null && entry.MonsterIDs != null) {
            MonsterIDs.AddRange(entry.MonsterIDs);
        }
    }
    
    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered Combat Node {NodeID}. Encountering {MonsterIDs.Count} enemies.");
        GameRoot.Core.Combat.StartCombat(MonsterIDs);
    }

    public void ResolveAfterVictory() {
        Debug.Log($"[CombatNode] Resolving victory settlement for node {NodeID}.");

        CombatLootPickupResult lootResult = PrepareLootPickupResult();
        if (lootResult != null && lootResult.OfferedItems.Count > 0) {
            _pendingLootResult = lootResult;
            Debug.Log($"[CombatNode] Prepared combat loot pickup. OfferedCount={lootResult.OfferedItems.Count}, EstimatedValue={lootResult.TotalEstimatedValue}");
            DungeonEventBus.PublishCombatLootPrepared(lootResult);
            return;
        }

        CompleteSettlement();
    }

    public void ConfirmLootCollection() {
        if (_pendingLootResult != null) {
            BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
            int acceptedCount = 0;
            int discardedCount = 0;
            CombatLootCollectionResult collectionResult = new CombatLootCollectionResult {
                NodeID = NodeID
            };

            foreach (var item in _pendingLootResult.OfferedItems) {
                if (item == null) {
                    continue;
                }

                if (grid != null && grid.ContainedItems.Contains(item)) {
                    acceptedCount++;
                    collectionResult.AcceptedItems.Add(item);
                } else {
                    discardedCount++;
                    collectionResult.DiscardedItems.Add(item);
                }
            }

            Debug.Log($"[CombatNode] Combat loot confirmed. Accepted={acceptedCount}, Discarded={discardedCount}");
            DungeonEventBus.PublishCombatLootCollected(collectionResult);
            _pendingLootResult = null;
        }

        CompleteSettlement();
    }

    private void CompleteSettlement() {
        Debug.Log($"[CombatNode] Settlement completed for node {NodeID}.");
        DungeonEventBus.PublishNodeSettlementCompleted();
    }

    private CombatLootPickupResult PrepareLootPickupResult() {
        if ((MonsterIDs == null || MonsterIDs.Count == 0) && string.IsNullOrEmpty(RewardID)) {
            Debug.Log("[CombatNode] No MonsterIDs or node RewardID configured. No loot generated.");
            return null;
        }

        CombatLootPickupResult result = new CombatLootPickupResult {
            NodeID = NodeID
        };

        RewardSystem rewardSystem = new RewardSystem();

        if (MonsterIDs != null) {
            foreach (string monsterID in MonsterIDs) {
                AppendMonsterReward(result, rewardSystem, monsterID);
            }
        }

        if (!string.IsNullOrEmpty(RewardID)) {
            RewardRollResult nodeReward = rewardSystem.Roll(RewardID, BuildRewardContext("Node", NodeID));
            AppendRewardResult(result, nodeReward, NodeID, false);
        }

        return result;
    }

    private void AppendMonsterReward(CombatLootPickupResult result, RewardSystem rewardSystem, string monsterID) {
        if (string.IsNullOrEmpty(monsterID) || !ConfigManager.Monsters.TryGetValue(monsterID, out var monster)) {
            Debug.LogWarning($"[CombatNode] Cannot roll loot. Monster config missing: {monsterID}");
            return;
        }

        if (!string.IsNullOrEmpty(monster.RewardID)) {
            if (ConfigManager.Rewards.ContainsKey(monster.RewardID)) {
                RewardRollResult rewardResult = rewardSystem.Roll(monster.RewardID, BuildRewardContext("Monster", monsterID));
                AppendRewardResult(result, rewardResult, monsterID, true);
                return;
            }

            Debug.LogWarning($"[CombatNode] Monster [{monster.MonsterID}] RewardID [{monster.RewardID}] missing. Falling back to legacy LootPool.");
        }

        ItemEntity droppedItem = RollLootForMonster(monsterID);
        if (droppedItem == null) {
            return;
        }

        result.SourceMonsterIDs.Add(monsterID);
        result.OfferedItems.Add(droppedItem);
        result.TotalEstimatedValue += droppedItem.BaseValue;
    }

    private void AppendRewardResult(CombatLootPickupResult pickupResult, RewardRollResult rewardResult, string sourceID, bool sourceIsMonster) {
        if (pickupResult == null || rewardResult == null) {
            return;
        }

        foreach (ItemEntity item in rewardResult.GeneratedItems) {
            if (item == null) {
                continue;
            }

            if (sourceIsMonster) {
                pickupResult.SourceMonsterIDs.Add(sourceID);
            }

            pickupResult.OfferedItems.Add(item);
            pickupResult.TotalEstimatedValue += item.BaseValue;
        }

        if (rewardResult.Money > 0) {
            Debug.LogWarning($"[CombatNode] Reward [{rewardResult.RootRewardID}] generated money={rewardResult.Money}, but combat loot pickup currently only supports item placement.");
        }
    }

    private RewardContext BuildRewardContext(string sourceType, string sourceID) {
        return new RewardContext {
            SourceType = sourceType,
            SourceID = sourceID,
            LayerID = GameRoot.Core?.Dungeon?.CurrentLayer?.LayerID ?? 0,
            NodeID = NodeID,
            Player = GameRoot.Core?.CurrentPlayer,
            ActiveDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll
        };
    }

    private ItemEntity RollLootForMonster(string monsterID) {
        if (string.IsNullOrEmpty(monsterID) || !ConfigManager.Monsters.TryGetValue(monsterID, out var monster)) {
            Debug.LogWarning($"[CombatNode] Cannot roll loot. Monster config missing: {monsterID}");
            return null;
        }

        if (monster.LootPool == null || monster.LootPool.Count == 0) {
            Debug.Log($"[CombatNode] Monster [{monster.Name}] has no loot pool configured.");
            return null;
        }

        int totalWeight = 0;
        foreach (var entry in monster.LootPool) {
            if (entry != null && entry.Weight > 0 && !string.IsNullOrEmpty(entry.ItemID)) {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0) {
            Debug.LogWarning($"[CombatNode] Monster [{monster.Name}] loot pool has no valid weighted entries.");
            return null;
        }

        int roll = Random.Range(0, totalWeight);
        int cursor = 0;
        foreach (var entry in monster.LootPool) {
            if (entry == null || entry.Weight <= 0 || string.IsNullOrEmpty(entry.ItemID)) {
                continue;
            }

            cursor += entry.Weight;
            if (roll < cursor) {
                ItemEntity item = ConfigManager.CreateItem(entry.ItemID);
                if (item == null) {
                    Debug.LogError($"[CombatNode] Loot config points to missing item: {entry.ItemID}");
                }
                return item;
            }
        }

        return null;
    }
}
