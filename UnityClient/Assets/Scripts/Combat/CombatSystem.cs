using System.Collections.Generic;
using UnityEngine;

public enum CombatState { PlayerTurn, EnemyTurn, End }

public class CombatSystem {
    public CombatFaction PlayerFaction;
    public CombatFaction EnemyFaction;

    public CombatState CurrentState;
    public CombatOutcomeReport LastOutcomeReport { get; private set; }
    public MonsterCombatModifierSystem MonsterRuntimeModifiers => _monsterRuntimeModifiers;

    private readonly MonsterActionRunner _monsterActionRunner = new MonsterActionRunner();
    private readonly MonsterActionRuntimeState _monsterActionState = new MonsterActionRuntimeState();
    private readonly MonsterCombatModifierSystem _monsterRuntimeModifiers = new MonsterCombatModifierSystem();

    public void StartCombat(List<string> monsterIDs) {
        Debug.Log("\n[CombatSystem] Initiating Combat!");
        ItemUseService.ClearPendingTargetSelection();
        LastOutcomeReport = null;
        _monsterActionState.Reset();
        _monsterRuntimeModifiers.Clear();

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
        if (IsPlayerDefeated()) {
            HandleDefeat();
            return;
        }

        _monsterActionState.AdvanceCooldownsForNewIntentRound();
        LockMonsterIntentsForPlayerTurn();
        CombatEventBus.Publish(CombatEventType.OnTurnStart, PlayerFaction);

        if (IsPlayerDefeated()) {
            HandleDefeat();
        }
    }

    public void EndPlayerTurn() {
        if (CurrentState != CombatState.PlayerTurn) {
            return;
        }

        ItemUseService.ClearPendingTargetSelection();
        Debug.Log("[CombatSystem] Player ends turn.");
        CombatEventBus.Publish(CombatEventType.OnTurnEnd, PlayerFaction);
        _monsterRuntimeModifiers.AdvancePlayerTurnEnd();

        if (IsPlayerDefeated()) {
            HandleDefeat();
            return;
        }

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
            if (enemy != null && enemy.RuntimeHP > 0 && !IsPlayerDefeated()) {
                MonsterActionContext context = BuildMonsterActionContext(enemy);
                _monsterActionRunner.ExecuteTurn(enemy, context);
            }
        }

        CombatEventBus.Publish(CombatEventType.OnTurnEnd, EnemyFaction);

        if (!IsPlayerDefeated()) {
            StartPlayerTurn();
        } else {
            HandleDefeat();
        }
    }

    private void HandleVictory() {
        CurrentState = CombatState.End;
        ItemUseService.ClearPendingTargetSelection();
        Debug.Log("<color=green>[CombatSystem] Victory! All enemies defeated.</color>");
        CombatEventBus.Publish(CombatEventType.OnCombatEnd, PlayerFaction);

        foreach (var fighter in PlayerFaction.Fighters) {
            if (fighter is DollFighter dollFighter) {
                dollFighter.SyncDataBack();
            }
        }

        PrepareCombatOutcomeReport(CombatOutcomeType.Victory);

        PlayerFaction.Cleanup();
        EnemyFaction.Cleanup();
        CleanupMonsterActionRuntime();

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
        Debug.Log("<color=red>[CombatSystem] Defeat! Player combat failure condition reached.</color>");

        PrepareCombatOutcomeReport(CombatOutcomeType.Defeat);

        PlayerFaction.Cleanup();
        EnemyFaction.Cleanup();
        CleanupMonsterActionRuntime();

        DungeonEventBus.PublishDungeonDefeated();
    }

    private void PrepareCombatOutcomeReport(CombatOutcomeType outcomeType) {
        LastOutcomeReport = outcomeType == CombatOutcomeType.Victory
            ? CombatOutcomeReportService.BuildVictory(this)
            : CombatOutcomeReportService.BuildDefeat(this);

        CombatEventBus.PublishCombatOutcomePrepared(LastOutcomeReport);
        Debug.Log($"[CombatSystem] Outcome prepared. Type={LastOutcomeReport.OutcomeType}, Reason={LastOutcomeReport.DefeatReason}, Summary={LastOutcomeReport.Summary}");
    }

    private bool IsPlayerDefeated() {
        return CombatDefeatConditionService.Evaluate(this).IsDefeated;
    }

    private MonsterActionContext BuildMonsterActionContext(MonsterFighter actor) {
        return new MonsterActionContext {
            Combat = this,
            Actor = actor,
            PlayerFaction = PlayerFaction,
            EnemyFaction = EnemyFaction,
            ActiveDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll,
            RuntimeState = _monsterActionState,
            RuntimeModifiers = _monsterRuntimeModifiers
        };
    }

    public MonsterActionContext CreateMonsterActionContextForPreview(MonsterFighter actor) {
        return BuildMonsterActionContext(actor);
    }

    private void LockMonsterIntentsForPlayerTurn() {
        if (EnemyFaction?.Fighters == null || PlayerFaction == null || IsPlayerDefeated()) {
            _monsterActionState.ClearAllLockedIntents();
            return;
        }

        _monsterActionState.ClearAllLockedIntents();
        foreach (FighterEntity fighter in EnemyFaction.Fighters) {
            MonsterFighter enemy = fighter as MonsterFighter;
            if (enemy == null || enemy.RuntimeHP <= 0) {
                continue;
            }

            MonsterActionContext context = BuildMonsterActionContext(enemy);
            _monsterActionRunner.LockIntent(enemy, context);
        }
    }

    private void CleanupMonsterActionRuntime() {
        _monsterActionState.Reset();
        _monsterRuntimeModifiers.Clear();

        DollEntity activeDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (activeDoll != null) {
            GridSolver.RecalculateAllEffects(activeDoll);
        }
    }
}
