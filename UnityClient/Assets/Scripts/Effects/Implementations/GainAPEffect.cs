using UnityEngine;

// Gives Zero AP back (never past her max): when the item is used (default), at the start of her turn (Trigger
// TurnStart), or when a neighbour is used (Trigger NeighborUse, Target = which side, Tag = which neighbours count,
// e.g. a counterweight next to a heavy weapon). Params[0]: AP.
public class GainAPEffect : EffectBase {
    private int _ap;
    private string _trigger;
    private string _tag;
    private string _target;

    public override CombatEventType ListenEvent => _trigger == ApplyStatusEffect.TurnStart ? CombatEventType.OnTurnStart : CombatEventType.None;

    public override void Init(EffectData data) {
        base.Init(data);
        _ap = Mathf.Max(0, Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f));
        _trigger = data.Trigger ?? string.Empty;
        _tag = data.Tag;
        _target = data.Target;
    }

    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        if (string.IsNullOrEmpty(_trigger) || _trigger == "Use") {
            Give(outcome.User, provider);
        }
    }

    public override void OnNeighborUsed(ItemUseOutcome outcome, ItemEntity provider) {
        Give(outcome.User, provider);
    }

    public override void ApplyToFighter(FighterEntity fighter, ItemEntity provider) {
        if (_trigger == ApplyStatusEffect.TurnStart) {
            Give(fighter, provider);
        }
    }

    public override void ApplyOnUse(ItemUseContext context, ItemEntity provider) {}

    private void Give(FighterEntity fighter, ItemEntity provider) {
        if (fighter == null || _ap <= 0) {
            return;
        }

        int before = fighter.CurrentAP;
        fighter.CurrentAP = Mathf.Min(fighter.MaxAP, fighter.CurrentAP + _ap);
        if (fighter.CurrentAP > before) {
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = fighter, Label = $"{provider?.Name} AP +{fighter.CurrentAP - before}" });
            GameEventBus.PublishAPChanged(fighter.Name, fighter.CurrentAP, fighter.MaxAP);
        }
    }

    public override string Describe(ItemEntity provider, int rotation) {
        switch (_trigger) {
            case ApplyStatusEffect.TurnStart:
                return $"每回合开始 AP +{_ap}";
            case CombatItemEffects.NeighborUseTrigger:
                return $"{EffectText.Side(_target, rotation)}的{CombatItemEffects.TagLabel(_tag)}用过之后，AP +{_ap}";
            default:
                return $"使用后 AP +{_ap}";
        }
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] < 1f) {
            report.AddError($"[{ownerID}] GainAP needs Params[0] >= 1 (AP).");
        }
    }
}
