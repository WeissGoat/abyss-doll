using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public abstract class DungeonOutcomeNode : NodeBase, ILootPickupNode {
    private readonly List<DungeonNodeOutcomeConfig> _outcomeEffects = new List<DungeonNodeOutcomeConfig>();
    private CombatLootPickupResult _pendingLootResult;
    private string _title;
    private string _description;

    protected virtual string DefaultTitle => "深渊节点";
    protected virtual string DefaultDescription => "节点事件已经结算。";
    protected virtual string RewardSourceType => "DungeonNode";

    public override void Init(NodePoolEntry entry) {
        base.Init(entry);

        _title = !string.IsNullOrEmpty(entry?.Title) ? entry.Title : DefaultTitle;
        _description = !string.IsNullOrEmpty(entry?.Description) ? entry.Description : DefaultDescription;

        _outcomeEffects.Clear();
        if (entry?.OutcomeEffects != null) {
            _outcomeEffects.AddRange(entry.OutcomeEffects);
        }
    }

    public override void OnEnterNode() {
        Resolve(_outcomeEffects, RewardID, null);
    }

    // Applies the outcome, then offers the reward's loot (plus any items given) or shows the result card. lead replaces
    // the node's description at the top of the card (an event choice says what she did).
    protected void Resolve(IEnumerable<DungeonNodeOutcomeConfig> effects, string rewardID, string lead, List<ItemEntity> givenItems = null) {
        if (!string.IsNullOrEmpty(lead)) {
            _description = lead;
        }

        List<string> outcomeLines = new List<string>();
        ApplyConfiguredOutcomes(effects, outcomeLines);

        CombatLootPickupResult lootResult = PrepareLootPickupResult(rewardID, outcomeLines, givenItems);
        if (lootResult != null && lootResult.OfferedItems.Count > 0) {
            _pendingLootResult = lootResult;
            DungeonEventBus.PublishCombatLootPrepared(lootResult);
            return;
        }

        PublishOutcomeResult(outcomeLines);
    }

    public void ConfirmLootCollection() {
        if (_pendingLootResult != null) {
            BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
            CombatLootCollectionResult collectionResult = new CombatLootCollectionResult {
                NodeID = NodeID
            };

            foreach (ItemEntity item in _pendingLootResult.OfferedItems) {
                if (item == null) {
                    continue;
                }

                if (grid != null && grid.ContainedItems.Contains(item)) {
                    ItemLifecycleService.MarkBackpackItem(item, ItemOwnerScope.Run);
                    collectionResult.AcceptedItems.Add(item);
                } else {
                    ItemLifecycleService.MarkInboxItemLost(item, "DungeonNodeLootConfirm", out _);
                    collectionResult.DiscardedItems.Add(item);
                }
            }

            DungeonEventBus.PublishCombatLootCollected(collectionResult);
            _pendingLootResult = null;
        }

        DungeonEventBus.PublishNodeSettlementCompleted();
    }

    private void ApplyConfiguredOutcomes(IEnumerable<DungeonNodeOutcomeConfig> effects, List<string> outcomeLines) {
        if (effects == null) {
            return;
        }

        foreach (DungeonNodeOutcomeConfig effect in effects) {
            if (effect == null) {
                continue;
            }

            if (!TryParseOutcomeType(effect.Type, out DungeonNodeOutcomeType outcomeType)) {
                outcomeLines.Add($"未识别节点效果: {effect.Type}");
                continue;
            }

            switch (outcomeType) {
                case DungeonNodeOutcomeType.ModifyResource:
                    ApplyResourceModification(effect, outcomeLines);
                    break;
            }
        }
    }

    private bool TryParseOutcomeType(string rawType, out DungeonNodeOutcomeType outcomeType) {
        string type = string.IsNullOrEmpty(rawType) ? nameof(DungeonNodeOutcomeType.ModifyResource) : rawType;
        return Enum.TryParse(type, true, out outcomeType);
    }

    private void ApplyResourceModification(DungeonNodeOutcomeConfig effect, List<string> outcomeLines) {
        if (!Enum.TryParse(effect.Resource, true, out EffectResourceType resource)) {
            outcomeLines.Add($"未识别资源: {effect.Resource}");
            return;
        }

        switch (resource) {
            case EffectResourceType.HP:
                ModifyDollHP(effect.Amount, outcomeLines);
                break;
            case EffectResourceType.SAN:
                ModifyDollSAN(effect.Amount, outcomeLines);
                break;
            case EffectResourceType.Money:
                ModifyPlayerMoney(effect.Amount, outcomeLines);
                break;
            default:
                outcomeLines.Add($"节点资源暂不支持: {resource}");
                break;
        }
    }

    private void ModifyDollHP(int amount, List<string> outcomeLines) {
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null) {
            outcomeLines.Add("HP变化失败：当前没有可用魔偶。");
            return;
        }

        int before = doll.Status.HP_Current;
        doll.Status.HP_Current = Mathf.Clamp(doll.Status.HP_Current + amount, 0, doll.Status.HP_Max);
        GameEventBus.PublishHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
        outcomeLines.Add(BuildDeltaLine("HP", before, doll.Status.HP_Current));
    }

    private void ModifyDollSAN(int amount, List<string> outcomeLines) {
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null) {
            outcomeLines.Add("SAN变化失败：当前没有可用魔偶。");
            return;
        }

        int before = doll.Status.SAN_Current;
        doll.Status.SAN_Current = Mathf.Clamp(doll.Status.SAN_Current + amount, 0, doll.Status.SAN_Max);
        GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
        outcomeLines.Add(BuildDeltaLine("SAN", before, doll.Status.SAN_Current));
    }

    private void ModifyPlayerMoney(int amount, List<string> outcomeLines) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            outcomeLines.Add("金币变化失败：当前没有玩家档案。");
            return;
        }

        int before = player.Money;
        player.Money = Mathf.Max(0, player.Money + amount);
        outcomeLines.Add(BuildDeltaLine("金币", before, player.Money));
    }

    private CombatLootPickupResult PrepareLootPickupResult(string rewardID, List<string> outcomeLines, List<ItemEntity> givenItems) {
        List<ItemEntity> items = new List<ItemEntity>(givenItems ?? new List<ItemEntity>());
        string source = "Given";
        if (!string.IsNullOrEmpty(rewardID)) {
            RewardRollResult rewardResult = new RewardSystem().Roll(rewardID, BuildRewardContext());
            if (rewardResult != null) {
                if (rewardResult.Money > 0) {
                    ModifyPlayerMoney(rewardResult.Money, outcomeLines);
                }

                items.AddRange(rewardResult.GeneratedItems ?? new List<ItemEntity>());
                source = rewardResult.RootRewardID;
            }
        }

        if (items.Count == 0) {
            return null;
        }

        CombatLootPickupResult pickupResult = new CombatLootPickupResult {
            NodeID = NodeID
        };

        foreach (ItemEntity item in items) {
            if (item == null) {
                continue;
            }

            ItemLifecycleService.MarkGeneratedToInbox(item, $"Reward:{source}");
            pickupResult.OfferedItems.Add(item);
            pickupResult.TotalEstimatedValue += item.BaseValue;
        }

        return pickupResult;
    }

    private RewardContext BuildRewardContext() {
        return new RewardContext {
            SourceType = RewardSourceType,
            SourceID = NodeID,
            LayerID = GameRoot.Core?.Dungeon?.CurrentLayer?.LayerID ?? 0,
            NodeID = NodeID,
            Player = GameRoot.Core?.CurrentPlayer,
            ActiveDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll
        };
    }

    private void PublishOutcomeResult(List<string> outcomeLines) {
        DungeonEventBus.PublishDungeonNodeResolutionPrepared(new DungeonNodeResolutionResult {
            NodeID = NodeID,
            Title = _title,
            Summary = BuildSummary(outcomeLines),
            PrimaryActionLabel = "继续探索"
        });
    }

    private string BuildSummary(List<string> outcomeLines) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(_description);

        if (outcomeLines != null && outcomeLines.Count > 0) {
            builder.AppendLine();
            foreach (string line in outcomeLines) {
                if (!string.IsNullOrEmpty(line)) {
                    builder.Append("- ");
                    builder.AppendLine(line);
                }
            }
        }

        return builder.ToString().TrimEnd();
    }

    private string BuildDeltaLine(string label, int before, int after) {
        int delta = after - before;
        string sign = delta >= 0 ? "+" : string.Empty;
        return $"{label}: {before} -> {after} ({sign}{delta})";
    }
}
