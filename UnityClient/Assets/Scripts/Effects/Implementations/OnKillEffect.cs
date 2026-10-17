using UnityEngine;

// When the item's strike finishes an enemy, Zero gets Params[0] of Resource: HP, SAN, Shield or AP.
public class OnKillEffect : EffectBase {
    private string _resource;
    private int _amount;

    public override void Init(EffectData data) {
        base.Init(data);
        _resource = data.Resource ?? string.Empty;
        _amount = Mathf.Max(0, Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f));
    }

    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        if (!outcome.Killed || outcome.User == null || _amount <= 0) {
            return;
        }

        DollFighter user = outcome.User;
        switch (_resource) {
            case "HP":
                user.Heal(_amount);
                break;
            case "SAN":
                DollEntity doll = user.DataRef;
                if (doll != null) {
                    doll.Status.SAN_Current = Mathf.Min(doll.Status.SAN_Max, doll.Status.SAN_Current + _amount);
                    GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
                }
                break;
            case "Shield":
                user.AddShield(_amount);
                break;
            case "AP":
                user.CurrentAP = Mathf.Min(user.MaxAP, user.CurrentAP + _amount);
                GameEventBus.PublishAPChanged(user.Name, user.CurrentAP, user.MaxAP);
                break;
            default:
                return;
        }

        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = user, Label = $"{provider?.Name} 击杀：{EffectText.Resource(_resource)} +{_amount}" });
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return $"用它击倒敌人时，{EffectText.Resource(_resource)} +{_amount}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Resource != "HP" && data.Resource != "SAN" && data.Resource != "Shield" && data.Resource != "AP") {
            report.AddError($"[{ownerID}] OnKill needs Resource HP / SAN / Shield / AP, got [{data.Resource}].");
        }

        if (data.Params == null || data.Params.Length == 0 || data.Params[0] < 1f) {
            report.AddError($"[{ownerID}] OnKill needs Params[0] >= 1.");
        }
    }
}
