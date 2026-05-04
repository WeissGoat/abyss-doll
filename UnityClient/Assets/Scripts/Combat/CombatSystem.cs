using System.Collections.Generic;
using UnityEngine;

public enum CombatState { PlayerTurn, EnemyTurn, End }

public class CombatSystem {
    public CombatFaction PlayerFaction;
    public CombatFaction EnemyFaction;

    public CombatState CurrentState;

    public void StartCombat(List<string> monsterIDs) {
        Debug.Log("\n[CombatSystem] Initiating Combat!");
        ItemUseService.ClearPendingTargetSelection();

        PlayerFaction = new CombatFaction { Type = FactionType.Player };
        PlayerFaction.Fighters.Add(new DollFighter(GameRoot.Core.CurrentPlayer.ActiveDoll, PlayerFaction));

        EnemyFaction = new CombatFaction { Type = FactionType.Enemy };
        foreach (var id in monsterIDs) {
            var template = ConfigManager.Monsters.ContainsKey(id) ? ConfigManager.Monsters[id] : null;
            if (template != null) {
                EnemyFaction.Fighters.Add(new MonsterFighter(template, EnemyFaction));
            } else {
                Debug.LogError($"[CombatSystem] Unknown Monster ID: {id}");
            }
        }

        Debug.Log($"[CombatSystem] Combat Started! Player vs {EnemyFaction.Fighters.Count} Monsters.");
        StartPlayerTurn();
    }

    public void StartPlayerTurn() {
        CurrentState = CombatState.PlayerTurn;
        CombatEventBus.Publish(CombatEventType.OnTurnStart, PlayerFaction);
    }

    public void EndPlayerTurn() {
        if (CurrentState != CombatState.PlayerTurn) {
            return;
        }

        ItemUseService.ClearPendingTargetSelection();
        Debug.Log("[CombatSystem] Player ends turn.");
        CombatEventBus.Publish(CombatEventType.OnTurnEnd, PlayerFaction);

        if (EnemyFaction.IsWipedOut()) {
            HandleVictory();
            return;
        }

        StartEnemyTurn();
    }

    public void StartEnemyTurn() {
        CurrentState = CombatState.EnemyTurn;
        ItemUseService.ClearPendingTargetSelection();
        CombatEventBus.Publish(CombatEventType.OnTurnStart, EnemyFaction);

        foreach (var fighter in EnemyFaction.Fighters) {
            MonsterFighter enemy = fighter as MonsterFighter;
            if (enemy != null && enemy.RuntimeHP > 0 && !PlayerFaction.IsWipedOut()) {
                FighterEntity target = PlayerFaction.Fighters.Find(candidate => candidate.RuntimeHP > 0);
                if (target != null) {
                    for (int i = 0; i < enemy.DataRef.AttacksPerTurn; i++) {
                        enemy.Attack(target);
                    }
                }
            }
        }

        CombatEventBus.Publish(CombatEventType.OnTurnEnd, EnemyFaction);

        if (!PlayerFaction.IsWipedOut()) {
            StartPlayerTurn();
        } else {
            HandleDefeat();
        }
    }

    private void HandleVictory() {
        CurrentState = CombatState.End;
        ItemUseService.ClearPendingTargetSelection();
        Debug.Log("<color=green>[CombatSystem] Victory! All enemies defeated.</color>");

        foreach (var fighter in PlayerFaction.Fighters) {
            if (fighter is DollFighter dollFighter) {
                dollFighter.SyncDataBack();
            }
        }

        PlayerFaction.Cleanup();
        EnemyFaction.Cleanup();

        CombatNode currentCombatNode = GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode as CombatNode;
        if (currentCombatNode != null) {
            currentCombatNode.ResolveAfterVictory();
        } else {
            Debug.LogWarning("[CombatSystem] Victory reached outside of a CombatNode context. Falling back to generic node settlement completion.");
            DungeonEventBus.PublishNodeSettlementCompleted();
        }
    }

    private void HandleDefeat() {
        CurrentState = CombatState.End;
        ItemUseService.ClearPendingTargetSelection();
        Debug.Log("<color=red>[CombatSystem] Defeat! All player entities wiped out.</color>");

        PlayerFaction.Cleanup();
        EnemyFaction.Cleanup();

        DungeonEventBus.PublishDungeonDefeated();
    }
}

public static class ItemUseService {
    private static ItemEntity _pendingTargetItem;
    private static DollFighter _pendingUserFighter;

    public static bool HasPendingEnemyTargetSelection => _pendingTargetItem != null && _pendingUserFighter != null;
    public static string PendingTargetItemName => _pendingTargetItem?.Name ?? string.Empty;

    public static bool TryUseItem(ItemEntity item, out string failureReason) {
        failureReason = string.Empty;

        if (item == null) {
            failureReason = "物品数据不存在。";
            return false;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(item)) {
            failureReason = $"物品 [{item.Name}] 不在当前背包中。";
            return false;
        }

        if (!RequiresEnemyTargetSelection(item) && HasPendingEnemyTargetSelection) {
            ClearPendingTargetSelection();
        }

        if (TryUseItemInCombat(item, out failureReason)) {
            return true;
        }

        if (TryUseItemInSafeRoom(item, out failureReason)) {
            return true;
        }

        if (string.IsNullOrEmpty(failureReason)) {
            failureReason = $"当前场景无法使用 [{item.Name}]。";
        }

        return false;
    }

    public static bool TryConfirmPendingTarget(FighterEntity target, out string failureReason) {
        failureReason = string.Empty;

        if (!HasPendingEnemyTargetSelection) {
            failureReason = "当前没有等待选择目标的物品。";
            return false;
        }

        if (target == null || target.RuntimeHP <= 0) {
            failureReason = "目标已失效，请重新选择。";
            ClearPendingTargetSelection();
            return false;
        }

        if (_pendingTargetItem?.Combat == null || _pendingUserFighter == null) {
            failureReason = "待使用物品或使用者已失效。";
            ClearPendingTargetSelection();
            return false;
        }

        if (_pendingUserFighter.CurrentAP < _pendingTargetItem.Combat.APCost) {
            failureReason = $"AP 不足，无法使用 [{_pendingTargetItem.Name}]。";
            ClearPendingTargetSelection();
            return false;
        }

        _pendingUserFighter.Attack(target, _pendingTargetItem);
        ClearPendingTargetSelection();
        return true;
    }

    public static void ClearPendingTargetSelection() {
        _pendingTargetItem = null;
        _pendingUserFighter = null;
        GameEventBus.PublishTargetSelectionChanged("点击武器后可选择一个敌方目标。", false);
    }

    private static bool TryUseItemInCombat(ItemEntity item, out string failureReason) {
        failureReason = string.Empty;
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat == null || combat.PlayerFaction == null || combat.EnemyFaction == null || combat.CurrentState == CombatState.End) {
            return false;
        }

        if (combat.CurrentState != CombatState.PlayerTurn) {
            failureReason = "当前不是可用道具的玩家回合。";
            return false;
        }

        if (combat.PlayerFaction.Fighters.Count == 0) {
            failureReason = "玩家战斗实体不存在。";
            return false;
        }

        DollFighter playerFighter = combat.PlayerFaction.Fighters[0] as DollFighter;
        if (playerFighter == null || item.Combat == null || item.Combat.TriggerType != TriggerType.Manual.ToString()) {
            failureReason = $"[{item?.Name ?? "未知物品"}] 当前没有可用的主动使用逻辑。";
            return false;
        }

        if (RequiresEnemyTargetSelection(item)) {
            if (combat.EnemyFaction.Fighters.Find(fighter => fighter.RuntimeHP > 0) == null) {
                failureReason = "当前没有有效攻击目标。";
                return false;
            }

            if (playerFighter.CurrentAP < item.Combat.APCost) {
                failureReason = $"AP 不足，无法使用 [{item.Name}]。";
                return false;
            }

            _pendingTargetItem = item;
            _pendingUserFighter = playerFighter;
            GameEventBus.PublishTargetSelectionChanged($"[{item.Name}] 已就绪，请点击一个敌方目标。", true);
            return true;
        }

        if (!CanUsePayload(item, playerFighter.DataRef, out failureReason)) {
            return false;
        }

        if (!playerFighter.TrySpendAP(item.Combat.APCost, item.Name)) {
            failureReason = $"AP 不足，无法使用 [{item.Name}]。";
            return false;
        }

        ItemUseContext context = new ItemUseContext {
            InCombat = true,
            UserDoll = playerFighter.DataRef,
            UserFighter = playerFighter,
            SourceItem = item
        };

        ApplyDirectItemPayload(item, context);
        ApplyConfiguredEffects(item, context);
        ConsumeItemAfterUse(item);
        GameEventBus.PublishTargetSelectionChanged($"{item.Name} 已使用。", false);
        return true;
    }

    private static bool TryUseItemInSafeRoom(ItemEntity item, out string failureReason) {
        failureReason = string.Empty;

        SafeRoomNode safeRoomNode = GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode as SafeRoomNode;
        if (safeRoomNode == null) {
            return false;
        }

        if (item.ItemType != nameof(ItemType.Consumable) || item.Combat == null || item.Combat.TriggerType != TriggerType.Manual.ToString()) {
            failureReason = $"[{item.Name}] 不能在安全屋中直接使用。";
            return false;
        }

        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (!CanUsePayload(item, doll, out failureReason)) {
            return false;
        }

        ItemUseContext context = new ItemUseContext {
            InSafeRoom = true,
            SafeRoomNode = safeRoomNode,
            UserDoll = doll,
            SourceItem = item
        };

        ApplyDirectItemPayload(item, context);
        ApplyConfiguredEffects(item, context);
        ConsumeItemAfterUse(item);
        GameEventBus.PublishTargetSelectionChanged($"{item.Name} 已使用。", false);
        return true;
    }

    private static bool RequiresEnemyTargetSelection(ItemEntity item) {
        if (item?.Combat == null) {
            return false;
        }

        return item.Combat.TriggerType == TriggerType.Manual.ToString()
            && item.ItemType == nameof(ItemType.Weapon)
            && (item.Combat.DamageType == DamageType.Physical.ToString() || item.Combat.DamageType == DamageType.Energy.ToString());
    }

    private static void ApplyDirectItemPayload(ItemEntity item, ItemUseContext context) {
        if (item?.Combat == null || context?.UserDoll == null) {
            return;
        }

        int magnitude = item.Combat.BaseValue;
        switch (item.Combat.DamageType) {
            case nameof(DamageType.Heal):
                if (context.UserFighter != null) {
                    context.UserFighter.Heal(magnitude);
                    context.UserDoll.Status.HP_Current = context.UserFighter.RuntimeHP;
                } else {
                    context.UserDoll.Status.HP_Current = Mathf.Min(context.UserDoll.Status.HP_Current + magnitude, context.UserDoll.Status.HP_Max);
                    GameEventBus.PublishHPChanged(context.UserDoll.Name, context.UserDoll.Status.HP_Current, context.UserDoll.Status.HP_Max);
                    Debug.Log($"[ItemUse] {context.UserDoll.Name} 在安全区使用 {item.Name}，恢复 {magnitude} HP。");
                }
                break;
            case nameof(DamageType.RestoreSAN):
                context.UserDoll.Status.SAN_Current = Mathf.Min(context.UserDoll.Status.SAN_Current + magnitude, context.UserDoll.Status.SAN_Max);
                GameEventBus.PublishSANChanged(context.UserDoll.Name, context.UserDoll.Status.SAN_Current, context.UserDoll.Status.SAN_Max);
                Debug.Log($"[ItemUse] {context.UserDoll.Name} 使用 {item.Name}，恢复 {magnitude} SAN。");
                break;
            case nameof(DamageType.Shield):
                if (context.UserFighter != null) {
                    context.UserFighter.AddShield(magnitude);
                }
                break;
        }
    }

    private static void ApplyConfiguredEffects(ItemEntity item, ItemUseContext context) {
        if (item?.Combat?.Effects == null) {
            return;
        }

        foreach (var effectData in item.Combat.Effects) {
            EffectBase effect = EffectFactory.CreateEffect(effectData);
            if (effect != null) {
                effect.ApplyOnUse(context, item);
            }
        }
    }

    private static void ConsumeItemAfterUse(ItemEntity item) {
        if (item == null || item.ItemType != nameof(ItemType.Consumable)) {
            return;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(item)) {
            return;
        }

        grid.RemoveItem(item);
        GridSolver.RecalculateAllEffects(GameRoot.Core.CurrentPlayer.ActiveDoll);
        GameEventBus.PublishItemRemoved(item.InstanceID);
        Debug.Log($"[ItemUse] Consumed item [{item.Name}] and removed it from backpack.");
    }

    private static bool CanUsePayload(ItemEntity item, DollEntity doll, out string failureReason) {
        failureReason = string.Empty;
        if (item?.Combat == null || doll == null) {
            return true;
        }

        switch (item.Combat.DamageType) {
            case nameof(DamageType.Heal):
                if (doll.Status.HP_Current >= doll.Status.HP_Max) {
                    failureReason = $"[{item.Name}] 无法使用，当前 HP 已满。";
                    return false;
                }
                break;
            case nameof(DamageType.RestoreSAN):
                if (doll.Status.SAN_Current >= doll.Status.SAN_Max) {
                    failureReason = $"[{item.Name}] 无法使用，当前 SAN 已满。";
                    return false;
                }
                break;
        }

        return true;
    }
}
