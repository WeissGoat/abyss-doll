using System;
using UnityEngine;

// Stacks a status on enemies (CombatStatusService): on the struck target when the item is used, on every enemy when
// Target is AllEnemies, or on every enemy at the start of Zero's turn when Trigger is TurnStart (a passive item).
// Status: Poison / Vulnerable / Weak. Params[0]: stacks.
public class ApplyStatusEffect : EffectBase {
    public const string AllEnemies = "AllEnemies";
    public const string TurnStart = "TurnStart";

    private CombatStatusType _status;
    private bool _validStatus;
    private int _stacks;
    private bool _allEnemies;
    private bool _onTurnStart;

    public override CombatEventType ListenEvent => _onTurnStart ? CombatEventType.OnTurnStart : CombatEventType.None;

    public override void Init(EffectData data) {
        base.Init(data);
        _validStatus = Enum.TryParse(data.Status, out _status);
        _stacks = Mathf.Max(0, Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f));
        _allEnemies = data.Target == AllEnemies;
        _onTurnStart = data.Trigger == TurnStart;
    }

    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        if (_onTurnStart || !_validStatus) {
            return;
        }

        if (_allEnemies) {
            foreach (FighterEntity enemy in CombatItemEffects.Opponents(outcome.User)) {
                CombatStatusService.Apply(enemy, _status, _stacks, provider?.Name);
            }
        } else if (outcome.Target != null && outcome.Target != outcome.User) {
            CombatStatusService.Apply(outcome.Target, _status, _stacks, provider?.Name);
        }
    }

    // Turn start (passive): every enemy gets the stacks.
    public override void ApplyToFighter(FighterEntity fighter, ItemEntity provider) {
        if (!_onTurnStart || !_validStatus) {
            return;
        }

        foreach (FighterEntity enemy in CombatItemEffects.Opponents(fighter)) {
            CombatStatusService.Apply(enemy, _status, _stacks, provider?.Name);
        }
    }

    public override void ApplyOnUse(ItemUseContext context, ItemEntity provider) {}

    public override string Describe(ItemEntity provider, int rotation) {
        if (!_validStatus) {
            return string.Empty;
        }

        string who = _onTurnStart ? "每回合开始，所有敌人" : _allEnemies ? "使用时，所有敌人" : "命中的敌人";
        return $"{who}叠 {_stacks} 层{CombatStatusService.Label(_status)}（{CombatStatusService.Explain(_status)}）";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (!Enum.TryParse(data.Status, out CombatStatusType _)) {
            report.AddError($"[{ownerID}] ApplyStatus needs Status Poison / Vulnerable / Weak, got [{data.Status}].");
        }

        if (data.Params == null || data.Params.Length == 0 || data.Params[0] < 1f) {
            report.AddError($"[{ownerID}] ApplyStatus needs Params[0] >= 1 (stacks).");
        }
    }
}
