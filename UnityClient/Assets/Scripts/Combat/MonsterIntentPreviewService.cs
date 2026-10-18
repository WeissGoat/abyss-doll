using System;
using System.Collections.Generic;
using UnityEngine;

public enum MonsterIntentType {
    Attack,
    WeakenWeapon,
    AddJunk,
    Defend,
    Cleanse,
    SelfDestruct,
    Disrupt,
    Unknown
}

public class MonsterIntentReport {
    public bool Success;
    public string Reason;
    public List<MonsterIntentCard> Monsters = new List<MonsterIntentCard>();
}

public class MonsterIntentCard {
    public string MonsterID;
    public string MonsterName;
    public string RuntimeID;
    public int RuntimeHP;
    public int RuntimeMaxHP;
    public int RuntimeShield;
    public bool IsAlive;
    public MonsterActionIntentPreview SelectedIntent;
    public List<MonsterActionIntentPreview> ActionIntents = new List<MonsterActionIntentPreview>();
}

public class MonsterActionIntentPreview {
    public string ActionID;
    public string ActionType;
    public string Target;
    public MonsterIntentType IntentType = MonsterIntentType.Unknown;
    public int Weight;
    public bool CanExecute;
    public string BlockReason;
    public string Title;
    // Title with its numbers, for the enemy card: "攻击 12", "攻击 6×2", "削弱武器 25%", "塞入齿轮废料".
    public string ShortText;
    public string Description;
    public int DamagePerHit;
    public int RepeatCount;
    public int EstimatedTotalDamage;
    public float WeaponDamageMultiplier = 1f;
    public int DurationPlayerTurns;
    public string CursedItemID;
    public string CursedItemName;
    public int CursedItemGridCost;
    public List<string> OverrideTags = new List<string>();
    public int ShieldAmount;
    public bool AffectsAllAllies;
    public List<string> CleansedStatuses = new List<string>();
    public int HealAmount;
    public int ShuffleCount;
}

public static class MonsterIntentPreviewService {
    public static MonsterIntentReport BuildReport(CombatSystem combat) {
        MonsterIntentReport report = new MonsterIntentReport();
        if (combat == null) {
            report.Success = false;
            report.Reason = "CombatSystem is missing.";
            return report;
        }

        if (combat.EnemyFaction?.Fighters == null) {
            report.Success = false;
            report.Reason = "Enemy faction is missing.";
            return report;
        }

        foreach (FighterEntity fighter in combat.EnemyFaction.Fighters) {
            if (fighter is MonsterFighter monster) {
                MonsterActionContext context = combat.CreateMonsterActionContextForPreview(monster);
                report.Monsters.Add(BuildMonsterCard(monster, context));
            }
        }

        report.Success = true;
        return report;
    }

    public static MonsterIntentCard BuildMonsterCard(MonsterFighter monster, MonsterActionContext context) {
        MonsterIntentCard card = new MonsterIntentCard();
        if (monster == null) {
            return card;
        }

        card.MonsterID = monster.DataRef?.MonsterID ?? string.Empty;
        card.MonsterName = monster.Name;
        card.RuntimeID = monster.RuntimeID;
        card.RuntimeHP = monster.RuntimeHP;
        card.RuntimeMaxHP = monster.RuntimeMaxHP;
        card.RuntimeShield = monster.RuntimeShield;
        card.IsAlive = monster.RuntimeHP > 0;

        MonsterAIConfig ai = monster.DataRef?.AI;
        if (!card.IsAlive || ai?.Actions == null) {
            return card;
        }

        foreach (MonsterActionConfig action in ai.Actions) {
            if (action == null) {
                continue;
            }

            card.ActionIntents.Add(BuildActionPreview(monster, context, action));
        }

        card.SelectedIntent = SelectPreviewIntent(monster, context, ai, card.ActionIntents);
        return card;
    }

    public static MonsterActionIntentPreview BuildActionPreview(MonsterFighter monster, MonsterActionContext context, MonsterActionConfig action) {
        MonsterActionIntentPreview preview = new MonsterActionIntentPreview {
            ActionID = action?.ActionID ?? string.Empty,
            ActionType = action?.ActionType ?? string.Empty,
            Target = action?.Target ?? string.Empty,
            Weight = action?.Weight ?? 0
        };

        if (monster == null || context == null || action == null) {
            preview.CanExecute = false;
            preview.BlockReason = "Intent context is incomplete.";
            preview.Title = "未知意图";
            preview.Description = preview.BlockReason;
            return preview;
        }

        if (!MonsterActionConfigParser.TryParseActionType(action.ActionType, out MonsterActionType actionType)) {
            preview.CanExecute = false;
            preview.BlockReason = $"Unknown action type [{action.ActionType}].";
            preview.Title = "未知意图";
            preview.Description = preview.BlockReason;
            return preview;
        }

        FillTypedPreview(preview, actionType, action);
        // The hit as it will land: 迟缓 on the monster, 破甲 on Zero (CombatStatusService).
        FighterEntity zero = context.PlayerFaction?.Fighters != null && context.PlayerFaction.Fighters.Count > 0 ? context.PlayerFaction.Fighters[0] : null;
        if ((preview.IntentType == MonsterIntentType.Attack || preview.IntentType == MonsterIntentType.SelfDestruct) && preview.DamagePerHit > 0) {
            preview.DamagePerHit = CombatStatusService.ModifyDamage(monster, zero, preview.DamagePerHit);
            preview.EstimatedTotalDamage = preview.DamagePerHit * preview.RepeatCount;
        }

        preview.CanExecute = EvaluateCanExecute(monster, context, action, out string blockReason);
        preview.BlockReason = blockReason;
        FillReadableText(preview, actionType);
        return preview;
    }

    private static MonsterActionIntentPreview SelectPreviewIntent(MonsterFighter monster, MonsterActionContext context, MonsterAIConfig ai, List<MonsterActionIntentPreview> previews) {
        if (previews == null || previews.Count == 0) {
            return null;
        }

        if (context?.RuntimeState != null && context.RuntimeState.TryGetLockedIntent(monster, ai, out MonsterActionConfig lockedAction)) {
            MonsterActionIntentPreview lockedPreview = previews.Find(preview =>
                preview != null &&
                string.Equals(preview.ActionID, lockedAction.ActionID, StringComparison.OrdinalIgnoreCase));

            if (lockedPreview != null) {
                return lockedPreview;
            }
        }

        MonsterActionIntentPreview selected = null;
        foreach (MonsterActionIntentPreview preview in previews) {
            if (preview == null || !preview.CanExecute || preview.Weight <= 0) {
                continue;
            }

            if (selected == null
                || preview.Weight > selected.Weight
                || (preview.Weight == selected.Weight && string.CompareOrdinal(preview.ActionID, selected.ActionID) < 0)) {
                selected = preview;
            }
        }

        return selected;
    }

    private static bool EvaluateCanExecute(MonsterFighter monster, MonsterActionContext context, MonsterActionConfig action, out string blockReason) {
        blockReason = string.Empty;

        if (action.Weight <= 0) {
            blockReason = "Action weight is zero.";
            return false;
        }

        if (context.RuntimeState != null && !context.RuntimeState.IsActionAvailable(monster, action)) {
            blockReason = "Action is on cooldown or has reached its use limit.";
            return false;
        }

        if (!EvaluateConditionForPreview(action.Condition, context, action)) {
            blockReason = $"Condition [{action.Condition}] is not satisfied.";
            return false;
        }

        if (!MonsterActionConfigParser.TryParseActionType(action.ActionType, out MonsterActionType actionType)) {
            blockReason = $"Action type [{action.ActionType}] is unknown.";
            return false;
        }

        if (!MonsterActionFactory.IsActionRegistered(action.ActionType)) {
            blockReason = $"Action type [{action.ActionType}] is not implemented.";
            return false;
        }

        if (!HasLegalTargetForPreview(actionType, context, action)) {
            blockReason = "No legal target or placement is available.";
            return false;
        }

        return true;
    }

    private static bool EvaluateConditionForPreview(string condition, MonsterActionContext context, MonsterActionConfig actionConfig) {
        if (!MonsterActionConfigParser.TryParseCondition(condition, out MonsterActionConditionType conditionType)) {
            return false;
        }

        switch (conditionType) {
            case MonsterActionConditionType.Always:
                return true;
            case MonsterActionConditionType.PlayerHasWeapon:
                return HasPlayerWeapon(context);
            case MonsterActionConditionType.PlayerGridHasSpace:
                return CanPlaceConfiguredItem(context, actionConfig);
            case MonsterActionConditionType.FromRound:
                return MonsterActionConditionEvaluator.IsFromRound(context, actionConfig);
            default:
                return false;
        }
    }

    private static bool HasLegalTargetForPreview(MonsterActionType actionType, MonsterActionContext context, MonsterActionConfig actionConfig) {
        switch (actionType) {
            case MonsterActionType.DamageTarget:
            case MonsterActionType.SelfDestruct:
                return HasAlivePlayerTarget(context);
            case MonsterActionType.ReduceWeaponDamage:
                return HasPlayerWeapon(context);
            case MonsterActionType.AddCursedItem:
                return CanPlaceConfiguredItem(context, actionConfig);
            case MonsterActionType.GainShield:
            case MonsterActionType.Cleanse:
            case MonsterActionType.ShuffleItems:
                MonsterActionBase action = MonsterActionFactory.Create(actionConfig);
                return action != null && action.CanExecute(context);
            default:
                return false;
        }
    }

    private static bool HasAlivePlayerTarget(MonsterActionContext context) {
        if (context?.PlayerFaction?.Fighters == null) {
            return false;
        }

        foreach (FighterEntity fighter in context.PlayerFaction.Fighters) {
            if (fighter != null && fighter.RuntimeHP > 0) {
                return true;
            }
        }

        return false;
    }

    private static bool HasPlayerWeapon(MonsterActionContext context) {
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        if (grid?.ContainedItems == null) {
            return false;
        }

        foreach (ItemEntity item in grid.ContainedItems) {
            if (MonsterTargetSelector.IsWeaponItem(item)) {
                return true;
            }
        }

        return false;
    }

    private static bool CanPlaceConfiguredItem(MonsterActionContext context, MonsterActionConfig actionConfig) {
        MonsterActionParamReader reader = new MonsterActionParamReader(actionConfig);
        string itemID = reader.GetString("ItemID", string.Empty);
        if (string.IsNullOrEmpty(itemID) || !ConfigManager.Items.ContainsKey(itemID)) {
            return false;
        }

        ItemEntity item = ConfigManager.CreateItem(itemID);
        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        return item != null && grid != null && grid.TryFindFirstAvailable(item, out _, out _);
    }

    private static void FillTypedPreview(MonsterActionIntentPreview preview, MonsterActionType actionType, MonsterActionConfig action) {
        MonsterActionParamReader reader = new MonsterActionParamReader(action);
        switch (actionType) {
            case MonsterActionType.DamageTarget:
                preview.IntentType = MonsterIntentType.Attack;
                preview.DamagePerHit = Mathf.Max(0, reader.GetInt("Damage", 0));
                preview.RepeatCount = Mathf.Max(1, reader.GetInt("RepeatCount", 1));
                preview.EstimatedTotalDamage = preview.DamagePerHit * preview.RepeatCount;
                break;
            case MonsterActionType.ReduceWeaponDamage:
                preview.IntentType = MonsterIntentType.WeakenWeapon;
                preview.WeaponDamageMultiplier = reader.GetFloat("Multiplier", 1f);
                preview.DurationPlayerTurns = reader.GetInt("DurationPlayerTurns", 1);
                break;
            case MonsterActionType.AddCursedItem:
                preview.IntentType = MonsterIntentType.AddJunk;
                preview.CursedItemID = reader.GetString("ItemID", string.Empty);
                preview.OverrideTags = reader.GetStringList("OverrideTags");
                FillCursedItemPreview(preview);
                break;
            case MonsterActionType.GainShield:
                preview.IntentType = MonsterIntentType.Defend;
                preview.ShieldAmount = Mathf.Max(0, reader.GetInt("Amount", 0));
                preview.AffectsAllAllies = IsAllAllies(action, actionType);
                break;
            case MonsterActionType.Cleanse:
                preview.IntentType = MonsterIntentType.Cleanse;
                preview.AffectsAllAllies = IsAllAllies(action, actionType);
                preview.HealAmount = Mathf.Max(0, reader.GetInt("Heal", 0));
                foreach (CombatStatusType status in CleanseAction.ReadStatuses(reader)) {
                    preview.CleansedStatuses.Add(CombatStatusService.Label(status));
                }
                break;
            case MonsterActionType.SelfDestruct:
                preview.IntentType = MonsterIntentType.SelfDestruct;
                preview.DamagePerHit = Mathf.Max(0, reader.GetInt("Damage", 0));
                preview.RepeatCount = 1;
                preview.EstimatedTotalDamage = preview.DamagePerHit;
                break;
            case MonsterActionType.ShuffleItems:
                preview.IntentType = MonsterIntentType.Disrupt;
                preview.ShuffleCount = Mathf.Max(1, reader.GetInt("Count", 2));
                break;
            default:
                preview.IntentType = MonsterIntentType.Unknown;
                break;
        }
    }

    private static bool IsAllAllies(MonsterActionConfig action, MonsterActionType actionType) {
        MonsterActionConfigParser.TryParseTarget(action.Target, MonsterActionConfigParser.GetDefaultTarget(actionType), out MonsterTargetType target);
        return target == MonsterTargetType.AllAllies;
    }

    private static void FillCursedItemPreview(MonsterActionIntentPreview preview) {
        if (string.IsNullOrEmpty(preview.CursedItemID) || !ConfigManager.Items.ContainsKey(preview.CursedItemID)) {
            return;
        }

        ItemEntity item = ConfigManager.Items[preview.CursedItemID];
        preview.CursedItemName = item.Name;
        preview.CursedItemGridCost = item.Grid?.GridCost ?? 0;
    }

    private static void FillReadableText(MonsterActionIntentPreview preview, MonsterActionType actionType) {
        switch (actionType) {
            case MonsterActionType.DamageTarget:
                preview.Title = "攻击";
                preview.ShortText = preview.RepeatCount > 1
                    ? $"攻击 {preview.DamagePerHit}×{preview.RepeatCount}"
                    : $"攻击 {preview.EstimatedTotalDamage}";
                preview.Description = preview.RepeatCount > 1
                    ? $"造成 {preview.DamagePerHit} x {preview.RepeatCount} 伤害，总计约 {preview.EstimatedTotalDamage}。"
                    : $"造成约 {preview.EstimatedTotalDamage} 伤害。";
                break;
            case MonsterActionType.ReduceWeaponDamage:
                int reducedPercent = Mathf.RoundToInt((1f - preview.WeaponDamageMultiplier) * 100f);
                preview.Title = "腐蚀武器";
                preview.ShortText = $"削弱武器 {reducedPercent}%";
                preview.Description = $"随机削弱一件武器 {reducedPercent}%，持续 {preview.DurationPlayerTurns} 个玩家回合。";
                break;
            case MonsterActionType.AddCursedItem:
                string itemName = string.IsNullOrEmpty(preview.CursedItemName) ? preview.CursedItemID : preview.CursedItemName;
                preview.Title = "塞入污染物";
                preview.ShortText = $"塞入{itemName}（{preview.CursedItemGridCost}格）";
                preview.Description = $"尝试向背包塞入 {itemName}，占格 {preview.CursedItemGridCost}。";
                break;
            case MonsterActionType.GainShield:
                string shieldWho = preview.AffectsAllAllies ? "所有敌人" : "自己";
                preview.Title = "防御";
                preview.ShortText = preview.AffectsAllAllies ? $"全体护盾 {preview.ShieldAmount}" : $"护盾 {preview.ShieldAmount}";
                preview.Description = $"给{shieldWho}加 {preview.ShieldAmount} 点护盾，挡到它下个回合开始。";
                break;
            case MonsterActionType.Cleanse:
                string cleansed = string.Join("、", preview.CleansedStatuses);
                string cleanseWho = preview.AffectsAllAllies ? "所有敌人" : "自己";
                preview.Title = "净化";
                preview.ShortText = preview.HealAmount > 0 ? $"净化{cleansed} +{preview.HealAmount} HP" : $"净化{cleansed}";
                preview.Description = preview.HealAmount > 0
                    ? $"清掉{cleanseWho}身上的{cleansed}，并各回复 {preview.HealAmount} HP。"
                    : $"清掉{cleanseWho}身上的{cleansed}。";
                break;
            case MonsterActionType.SelfDestruct:
                preview.Title = "自爆";
                preview.ShortText = $"自爆 {preview.EstimatedTotalDamage}（无视护盾）";
                preview.Description = $"引爆自己，造成约 {preview.EstimatedTotalDamage} 伤害，护盾挡不住。在它行动前打倒它就不会爆。";
                break;
            case MonsterActionType.ShuffleItems:
                preview.Title = "震荡";
                preview.ShortText = $"震乱背包 {preview.ShuffleCount} 件";
                preview.Description = $"把背包里随机 {preview.ShuffleCount} 件物品挪到别处，挨着的加成可能断开。";
                break;
            default:
                preview.Title = "未知意图";
                preview.ShortText = preview.Title;
                preview.Description = string.IsNullOrEmpty(preview.BlockReason) ? "该行动尚未定义可读描述。" : preview.BlockReason;
                break;
        }
    }
}
