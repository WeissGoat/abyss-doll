using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TownEconomyOverviewReport {
    public bool Success;
    public string Reason;
    public int Day;
    public int Month;
    public int MonthDay;
    public int CurrentWeek;
    public int Money;
    public int DebtLevel;
    public int DebtAmount;
    public int MonthlyGrossIncome;
    public int EstimatedMonthlyBill;
    public int EstimatedMonthlyShortfall;
    public int RentCountdown;
    public bool HasPendingMonthlyRent;
    public int PendingMonthlyBillAmount;
    public int PendingMonthlyRentDay;
    public List<TownEconomySellCandidate> SellCandidates = new List<TownEconomySellCandidate>();
    public List<TownEconomyOrderCard> Orders = new List<TownEconomyOrderCard>();
    public List<TownEconomyRumorCard> Rumors = new List<TownEconomyRumorCard>();
    public List<TownEconomyFactionSummary> Factions = new List<TownEconomyFactionSummary>();
    public List<PawnCandidate> PawnCandidates = new List<PawnCandidate>();
}

public class TownEconomySellCandidate {
    public ItemEntity Item;
    public string InstanceID;
    public string ItemID;
    public string Name;
    public string SourceContainer;
    public string ItemType;
    public int BaseValue;
    public bool IsSellable;
    public string Reason;
    public string BestChannel;
    public int BestValue;
    public List<string> Tags = new List<string>();
    public List<TownEconomySellChannelValue> ChannelValues = new List<TownEconomySellChannelValue>();
}

public class TownEconomySellChannelValue {
    public string Channel;
    public bool CanSell;
    public string Reason;
    public int FinalValue;
    public float Multiplier = 1f;
    public List<string> AppliedRumorIDs = new List<string>();
}

public class TownEconomyOrderCard {
    public OrderInstanceState Instance;
    public string InstanceID;
    public string OrderID;
    public string FactionID;
    public string FactionName;
    public string Status;
    public string DisplayName;
    public string Description;
    public string OrderType;
    public int RequiredCount;
    public int MatchedOwnedCount;
    public int MissingCount;
    public int GeneratedDay;
    public int AcceptedDay;
    public int DeadlineDay;
    public int DaysRemaining;
    public bool IsExpired;
    public bool CanAccept;
    public bool CanDeliver;
    public string BlockReason;
    public int FixedGold;
    public int GuaranteedRewardMoneyPreview;
    public int ReputationDelta;
    public int TrustDelta;
    public List<string> RequiredItemIDs = new List<string>();
    public List<string> RequiredTags = new List<string>();
    public List<string> ForbiddenTags = new List<string>();
    public List<string> MatchingItemInstanceIDs = new List<string>();
    public List<string> RewardItemPreviewIDs = new List<string>();
}

public class TownEconomyRumorCard {
    public ActiveRumorState State;
    public string RumorID;
    public string DisplayName;
    public string RumorType;
    public string Description;
    public string Channel;
    public float PriceMultiplier = 1f;
    public int StartDay;
    public int ExpireDay;
    public int DaysRemaining;
    public List<string> TargetTags = new List<string>();
    public List<string> BoostedOrderTags = new List<string>();
}

public class TownEconomyFactionSummary {
    public FactionRuntimeState State;
    public string FactionID;
    public string DisplayName;
    public bool IsBlackMarket;
    public int ReputationValue;
    public int TrustValue;
    public int FailedOrderCount;
    public int CooldownUntilDay;
    public bool IsOnCooldown;
    public int CurrentRank;
    public int NextRankThreshold;
    public int CompletedOrderCount;
    public int AvailableOrderCount;
    public int AcceptedOrderCount;
}

internal class TownEconomyOwnedItemSlot {
    public ItemEntity Item;
    public string SourceContainer;
}

public static class TownEconomyOverviewService {
    public static TownEconomyOverviewReport BuildReport(
        PlayerProfile player,
        string economyConfigID = TownEconomyService.DefaultEconomyConfigID) {
        TownEconomyOverviewReport report = new TownEconomyOverviewReport();
        if (player == null) {
            report.Success = false;
            report.Reason = "Player profile is missing.";
            return report;
        }

        EconomyConfig config;
        string resolvedConfigID = string.IsNullOrEmpty(economyConfigID)
            ? TownEconomyService.DefaultEconomyConfigID
            : economyConfigID;
        if (!ConfigManager.EconomyConfigs.TryGetValue(resolvedConfigID, out config) || config == null) {
            report.Success = false;
            report.Reason = $"Economy config [{resolvedConfigID}] is missing.";
            return report;
        }

        NormalizeEconomicState(player);
        report.Success = true;
        report.Day = player.CurrentDay;
        report.Month = player.CurrentMonth;
        report.MonthDay = player.CurrentMonthDay;
        report.CurrentWeek = CalculateCurrentWeek(player, config);
        report.Money = player.Money;
        report.DebtLevel = player.DebtLevel;
        report.DebtAmount = player.EconomyDebtAmount;
        report.MonthlyGrossIncome = player.MonthlyGrossIncome;
        report.EstimatedMonthlyBill = CalculateMonthlyBill(player, config);
        report.EstimatedMonthlyShortfall = Mathf.Max(0, report.EstimatedMonthlyBill - player.Money);
        report.RentCountdown = CalculateRentCountdown(player, config);
        report.HasPendingMonthlyRent = player.HasPendingMonthlyRent;
        report.PendingMonthlyBillAmount = player.PendingMonthlyBillAmount;
        report.PendingMonthlyRentDay = player.PendingMonthlyRentDay;
        report.SellCandidates.AddRange(BuildSellCandidates(player));
        report.Orders.AddRange(BuildOrderCards(player));
        report.Rumors.AddRange(BuildRumorCards(player));
        report.Factions.AddRange(BuildFactionSummaries(player));
        report.PawnCandidates.AddRange(TownEconomyService.BuildPawnCandidates(player, resolvedConfigID));
        return report;
    }

    private static List<TownEconomySellCandidate> BuildSellCandidates(PlayerProfile player) {
        List<TownEconomySellCandidate> candidates = new List<TownEconomySellCandidate>();
        foreach (TownEconomyOwnedItemSlot slot in EnumerateOwnedItems(player)) {
            ItemEntity item = slot.Item;
            if (item == null) {
                continue;
            }

            TownEconomySellCandidate candidate = new TownEconomySellCandidate {
                Item = item,
                InstanceID = item.InstanceID,
                ItemID = item.ConfigID,
                Name = item.Name,
                SourceContainer = slot.SourceContainer,
                ItemType = item.ItemType,
                BaseValue = Mathf.Max(0, item.BaseValue),
                IsSellable = item.BaseValue > 0,
                Reason = item.BaseValue > 0 ? "Sellable." : "Item has no sell value."
            };
            candidate.Tags.AddRange(CollectItemTags(item));

            foreach (EconomySellChannel channel in Enum.GetValues(typeof(EconomySellChannel))) {
                EconomySellLine line = TownEconomyService.CalculateItemSellValue(player, item, channel);
                TownEconomySellChannelValue value = new TownEconomySellChannelValue {
                    Channel = channel.ToString(),
                    CanSell = item.BaseValue > 0 && line.FinalValue > 0,
                    Reason = item.BaseValue > 0 ? string.Empty : "Item has no sell value.",
                    FinalValue = line.FinalValue,
                    Multiplier = line.Multiplier
                };
                value.AppliedRumorIDs.AddRange(line.AppliedRumorIDs);
                candidate.ChannelValues.Add(value);
            }

            TownEconomySellChannelValue best = candidate.ChannelValues
                .Where(value => value.CanSell)
                .OrderByDescending(value => value.FinalValue)
                .ThenBy(value => value.Channel)
                .FirstOrDefault();
            if (best != null) {
                candidate.BestChannel = best.Channel;
                candidate.BestValue = best.FinalValue;
            }

            candidates.Add(candidate);
        }

        return candidates
            .OrderByDescending(candidate => candidate.BestValue)
            .ThenBy(candidate => candidate.ItemID)
            .ThenBy(candidate => candidate.InstanceID)
            .ToList();
    }

    private static List<TownEconomyOrderCard> BuildOrderCards(PlayerProfile player) {
        List<TownEconomyOrderCard> cards = new List<TownEconomyOrderCard>();
        if (player.ActiveOrders == null) {
            return cards;
        }

        List<TownEconomyOwnedItemSlot> ownedItems = EnumerateOwnedItems(player).ToList();
        foreach (OrderInstanceState instance in player.ActiveOrders.Where(IsDisplayableOrder)) {
            if (instance == null || !ConfigManager.Orders.TryGetValue(instance.OrderID, out OrderConfig config) || config == null) {
                continue;
            }

            TownEconomyOrderCard card = BuildOrderCard(player, ownedItems, instance, config);
            cards.Add(card);
        }

        return cards
            .OrderBy(card => ResolveOrderSort(card.Status))
            .ThenBy(card => card.FactionID)
            .ThenBy(card => card.OrderID)
            .ToList();
    }

    private static TownEconomyOrderCard BuildOrderCard(
        PlayerProfile player,
        List<TownEconomyOwnedItemSlot> ownedItems,
        OrderInstanceState instance,
        OrderConfig config) {
        OrderRequirementConfig requirement = config.Requirement ?? new OrderRequirementConfig();
        TownEconomyOrderCard card = new TownEconomyOrderCard {
            Instance = instance,
            InstanceID = instance.InstanceID,
            OrderID = config.OrderID,
            FactionID = config.FactionID,
            Status = string.IsNullOrEmpty(instance.Status) ? EconomyOrderStatus.Available.ToString() : instance.Status,
            DisplayName = string.IsNullOrEmpty(config.DisplayName) ? config.OrderID : config.DisplayName,
            Description = config.Description,
            OrderType = config.OrderType,
            RequiredCount = Mathf.Max(1, requirement.RequiredCount),
            GeneratedDay = instance.GeneratedDay,
            AcceptedDay = instance.AcceptedDay,
            DeadlineDay = instance.DeadlineDay,
            FixedGold = Mathf.Max(0, config.FixedGold),
            ReputationDelta = config.ReputationDelta,
            TrustDelta = config.TrustDelta
        };

        if (ConfigManager.Factions.TryGetValue(config.FactionID, out FactionConfig faction)) {
            card.FactionName = string.IsNullOrEmpty(faction.DisplayName) ? faction.FactionID : faction.DisplayName;
        } else {
            card.FactionName = config.FactionID;
        }

        CopyList(requirement.RequiredItemIDs, card.RequiredItemIDs);
        CopyList(requirement.RequiredTags, card.RequiredTags);
        CopyList(requirement.ForbiddenTags, card.ForbiddenTags);
        AppendRewardPreview(config.RewardID, card);

        foreach (TownEconomyOwnedItemSlot slot in ownedItems) {
            if (ItemMatchesRequirement(slot.Item, requirement)) {
                card.MatchingItemInstanceIDs.Add(slot.Item.InstanceID);
            }
        }

        card.MatchedOwnedCount = Mathf.Min(card.RequiredCount, card.MatchingItemInstanceIDs.Count);
        card.MissingCount = Mathf.Max(0, card.RequiredCount - card.MatchingItemInstanceIDs.Count);
        card.IsExpired = instance.DeadlineDay > 0 && player.CurrentDay > instance.DeadlineDay;
        card.DaysRemaining = instance.DeadlineDay > 0 ? Mathf.Max(0, instance.DeadlineDay - player.CurrentDay) : 0;
        ApplyOrderActionState(player, config, card);
        return card;
    }

    private static void ApplyOrderActionState(PlayerProfile player, OrderConfig config, TownEconomyOrderCard card) {
        EconomyOrderStatus status = ParseOrderStatus(card.Status);
        if (card.IsExpired) {
            card.BlockReason = "Order is expired.";
            return;
        }

        if (status == EconomyOrderStatus.Available) {
            int activeCount = CountFactionOrders(player, config.FactionID, EconomyOrderStatus.Accepted, EconomyOrderStatus.InProgress);
            int maxActive = ConfigManager.Factions.TryGetValue(config.FactionID, out FactionConfig faction)
                ? Mathf.Max(1, faction.MaxActiveOrders)
                : 3;
            card.CanAccept = activeCount < maxActive;
            card.BlockReason = card.CanAccept ? string.Empty : "Faction active order limit reached.";
            return;
        }

        if (status == EconomyOrderStatus.Accepted || status == EconomyOrderStatus.InProgress) {
            card.CanDeliver = card.MissingCount <= 0;
            card.BlockReason = card.CanDeliver ? string.Empty : $"Missing {card.MissingCount} matching item(s).";
            return;
        }

        card.BlockReason = $"Order status [{card.Status}] is not actionable.";
    }

    private static List<TownEconomyRumorCard> BuildRumorCards(PlayerProfile player) {
        List<TownEconomyRumorCard> cards = new List<TownEconomyRumorCard>();
        if (player.ActiveRumors == null) {
            return cards;
        }

        int currentDay = Mathf.Max(1, player.CurrentDay);
        foreach (ActiveRumorState state in player.ActiveRumors) {
            if (state == null
                || state.StartDay > currentDay
                || state.ExpireDay < currentDay
                || !ConfigManager.Rumors.TryGetValue(state.RumorID, out RumorConfig config)
                || config == null) {
                continue;
            }

            TownEconomyRumorCard card = new TownEconomyRumorCard {
                State = state,
                RumorID = config.RumorID,
                DisplayName = string.IsNullOrEmpty(config.DisplayName) ? config.RumorID : config.DisplayName,
                RumorType = config.RumorType,
                Description = config.Description,
                Channel = config.Channel,
                PriceMultiplier = config.PriceMultiplier,
                StartDay = state.StartDay,
                ExpireDay = state.ExpireDay,
                DaysRemaining = Mathf.Max(0, state.ExpireDay - currentDay)
            };
            CopyList(config.TargetTags, card.TargetTags);
            CopyList(config.BoostedOrderTags, card.BoostedOrderTags);
            cards.Add(card);
        }

        return cards
            .OrderBy(card => card.ExpireDay)
            .ThenBy(card => card.RumorID)
            .ToList();
    }

    private static List<TownEconomyFactionSummary> BuildFactionSummaries(PlayerProfile player) {
        List<TownEconomyFactionSummary> summaries = new List<TownEconomyFactionSummary>();
        foreach (FactionConfig config in ConfigManager.Factions.Values.OrderBy(faction => faction.FactionID)) {
            FactionRuntimeState state = GetFactionState(player, config.FactionID);
            TownEconomyFactionSummary summary = new TownEconomyFactionSummary {
                State = state,
                FactionID = config.FactionID,
                DisplayName = string.IsNullOrEmpty(config.DisplayName) ? config.FactionID : config.DisplayName,
                IsBlackMarket = config.IsBlackMarket
            };

            if (state != null) {
                summary.ReputationValue = state.ReputationValue;
                summary.TrustValue = state.TrustValue;
                summary.FailedOrderCount = state.FailedOrderCount;
                summary.CooldownUntilDay = state.CooldownUntilDay;
                summary.IsOnCooldown = state.CooldownUntilDay > player.CurrentDay;
                summary.CompletedOrderCount = state.CompletedOrderIDs != null ? state.CompletedOrderIDs.Count : 0;
            }

            ResolveRank(config, summary);
            summary.AvailableOrderCount = CountFactionOrders(player, config.FactionID, EconomyOrderStatus.Available);
            summary.AcceptedOrderCount = CountFactionOrders(player, config.FactionID, EconomyOrderStatus.Accepted, EconomyOrderStatus.InProgress);
            summaries.Add(summary);
        }

        return summaries;
    }

    private static IEnumerable<TownEconomyOwnedItemSlot> EnumerateOwnedItems(PlayerProfile player) {
        if (player?.StashInventory != null) {
            foreach (ItemEntity item in player.StashInventory) {
                if (item != null) {
                    yield return new TownEconomyOwnedItemSlot {
                        Item = item,
                        SourceContainer = "GroundInventory"
                    };
                }
            }
        }

        BackpackGrid grid = player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null) {
            yield break;
        }

        foreach (ItemEntity item in grid.ContainedItems) {
            if (item != null) {
                yield return new TownEconomyOwnedItemSlot {
                    Item = item,
                    SourceContainer = "Backpack"
                };
            }
        }
    }

    private static bool IsDisplayableOrder(OrderInstanceState order) {
        if (order == null) {
            return false;
        }

        EconomyOrderStatus status = ParseOrderStatus(order.Status);
        return status == EconomyOrderStatus.Available
            || status == EconomyOrderStatus.Accepted
            || status == EconomyOrderStatus.InProgress;
    }

    private static bool ItemMatchesRequirement(ItemEntity item, OrderRequirementConfig requirement) {
        if (item == null || requirement == null) {
            return false;
        }

        if (requirement.RequiredItemIDs != null
            && requirement.RequiredItemIDs.Count > 0
            && !requirement.RequiredItemIDs.Any(requiredID => string.Equals(requiredID, item.ConfigID, StringComparison.OrdinalIgnoreCase))) {
            return false;
        }

        List<string> tags = CollectItemTags(item);
        if (requirement.RequiredTags != null
            && requirement.RequiredTags.Count > 0
            && !requirement.RequiredTags.All(requiredTag => tags.Any(tag => string.Equals(tag, requiredTag, StringComparison.OrdinalIgnoreCase)))) {
            return false;
        }

        if (requirement.ForbiddenTags != null
            && requirement.ForbiddenTags.Any(forbiddenTag => tags.Any(tag => string.Equals(tag, forbiddenTag, StringComparison.OrdinalIgnoreCase)))) {
            return false;
        }

        return item.Grid == null || requirement.MinGridCost <= 0 || item.Grid.GridCost >= requirement.MinGridCost;
    }

    private static List<string> CollectItemTags(ItemEntity item) {
        List<string> tags = new List<string>();
        if (item?.Tags != null) {
            tags.AddRange(item.Tags.Where(tag => !string.IsNullOrEmpty(tag)));
        }

        List<string> dynamicTags = GetDynamicTags(item);
        if (dynamicTags != null) {
            tags.AddRange(dynamicTags.Where(tag => !string.IsNullOrEmpty(tag)));
        }

        return tags.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(tag => tag).ToList();
    }

    private static List<string> GetDynamicTags(ItemEntity item) {
        System.Reflection.FieldInfo dynamicTagsField = typeof(ItemEntity).GetField("DynamicTags");
        if (dynamicTagsField == null || !typeof(List<string>).IsAssignableFrom(dynamicTagsField.FieldType)) {
            return null;
        }

        return dynamicTagsField.GetValue(item) as List<string>;
    }

    private static void AppendRewardPreview(string rewardID, TownEconomyOrderCard card) {
        if (string.IsNullOrEmpty(rewardID)
            || card == null
            || !ConfigManager.Rewards.TryGetValue(rewardID, out RewardConfig reward)
            || reward == null
            || reward.Guaranteed == null) {
            return;
        }

        foreach (RewardEntry entry in reward.Guaranteed) {
            if (entry == null) {
                continue;
            }

            string type = string.IsNullOrEmpty(entry.Type) ? "Item" : entry.Type;
            if (string.Equals(type, "Money", StringComparison.OrdinalIgnoreCase)) {
                card.GuaranteedRewardMoneyPreview += entry.Money > 0 ? entry.Money : ResolveRewardCount(entry);
            } else if (string.Equals(type, "Item", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(entry.ItemID)) {
                card.RewardItemPreviewIDs.Add(entry.ItemID);
            }
        }
    }

    private static int ResolveRewardCount(RewardEntry entry) {
        if (entry == null) {
            return 0;
        }

        if (entry.MinCount > 0) {
            return entry.MinCount;
        }

        return Mathf.Max(1, entry.Count);
    }

    private static int CountFactionOrders(PlayerProfile player, string factionID, params EconomyOrderStatus[] statuses) {
        if (player?.ActiveOrders == null || statuses == null || statuses.Length == 0) {
            return 0;
        }

        return player.ActiveOrders.Count(order =>
            order != null
            && string.Equals(order.FactionID, factionID, StringComparison.OrdinalIgnoreCase)
            && statuses.Contains(ParseOrderStatus(order.Status)));
    }

    private static FactionRuntimeState GetFactionState(PlayerProfile player, string factionID) {
        return player?.FactionStates?.FirstOrDefault(state =>
            state != null && string.Equals(state.FactionID, factionID, StringComparison.OrdinalIgnoreCase));
    }

    private static void ResolveRank(FactionConfig config, TownEconomyFactionSummary summary) {
        if (config?.ReputationRanks == null || summary == null) {
            return;
        }

        ReputationRankConfig current = null;
        ReputationRankConfig next = null;
        foreach (ReputationRankConfig rank in config.ReputationRanks.OrderBy(rank => rank.Threshold)) {
            if (rank == null) {
                continue;
            }

            if (rank.Threshold <= summary.ReputationValue) {
                current = rank;
                continue;
            }

            next = rank;
            break;
        }

        summary.CurrentRank = current != null ? current.Rank : 0;
        summary.NextRankThreshold = next != null ? next.Threshold : 0;
    }

    private static EconomyOrderStatus ParseOrderStatus(string status) {
        EconomyOrderStatus parsed;
        if (Enum.TryParse(status, true, out parsed)) {
            return parsed;
        }

        return EconomyOrderStatus.Available;
    }

    private static int ResolveOrderSort(string status) {
        switch (ParseOrderStatus(status)) {
            case EconomyOrderStatus.Accepted:
            case EconomyOrderStatus.InProgress:
                return 0;
            case EconomyOrderStatus.Available:
                return 1;
            default:
                return 2;
        }
    }

    private static void CopyList(List<string> source, List<string> target) {
        if (source == null || target == null) {
            return;
        }

        target.AddRange(source.Where(value => !string.IsNullOrEmpty(value)));
    }

    private static void NormalizeEconomicState(PlayerProfile player) {
        if (player == null) {
            return;
        }

        player.CurrentDay = Mathf.Max(1, player.CurrentDay);
        player.CurrentMonth = Mathf.Max(1, player.CurrentMonth);
        player.CurrentMonthDay = Mathf.Max(1, player.CurrentMonthDay);
        if (player.StashInventory == null) {
            player.StashInventory = new List<ItemEntity>();
        }

        if (player.FactionStates == null) {
            player.FactionStates = new List<FactionRuntimeState>();
        }

        if (player.ActiveOrders == null) {
            player.ActiveOrders = new List<OrderInstanceState>();
        }

        if (player.ActiveRumors == null) {
            player.ActiveRumors = new List<ActiveRumorState>();
        }
    }

    private static int CalculateCurrentWeek(PlayerProfile player, EconomyConfig config) {
        int weekLength = Mathf.Max(1, config?.WeekLength ?? 7);
        int currentDay = Mathf.Max(1, player?.CurrentDay ?? 1);
        return ((currentDay - 1) / weekLength) + 1;
    }

    private static int CalculateRentCountdown(PlayerProfile player, EconomyConfig config) {
        if (player == null || config == null) {
            return 0;
        }

        return Mathf.Max(0, config.MonthLength - player.CurrentMonthDay + 1);
    }

    private static int CalculateMonthlyBill(PlayerProfile player, EconomyConfig config) {
        if (player == null || config == null) {
            return 0;
        }

        int debtPrincipal = Mathf.Max(0, player.EconomyDebtAmount);
        int debtInterest = Mathf.CeilToInt(debtPrincipal * Mathf.Max(0f, config.DebtInterestRate));
        return ResolveBaseRent(config, Mathf.Max(1, player.CurrentMonth))
            + Mathf.Max(0, config.WorkshopMaintenanceBase)
            + Mathf.Max(0, config.LicenseFeeBase)
            + debtPrincipal
            + debtInterest;
    }

    private static int ResolveBaseRent(EconomyConfig config, int month) {
        if (config?.RentCurve == null || config.RentCurve.Count == 0) {
            return 0;
        }

        RentCurveStepConfig best = null;
        foreach (RentCurveStepConfig step in config.RentCurve) {
            if (step == null || step.Month <= 0 || step.BaseRent <= 0) {
                continue;
            }

            if (step.Month <= month && (best == null || step.Month > best.Month)) {
                best = step;
            }
        }

        if (best != null) {
            return best.BaseRent;
        }

        RentCurveStepConfig first = config.RentCurve
            .Where(step => step != null && step.BaseRent > 0)
            .OrderBy(step => step.Month)
            .FirstOrDefault();
        return first != null ? first.BaseRent : 0;
    }
}
