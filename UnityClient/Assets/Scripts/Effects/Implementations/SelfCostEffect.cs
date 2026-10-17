using UnityEngine;

// Using the item costs Zero something (the price of a cursed weapon's damage): Resource HP (never below 1, through the
// shield), SAN, Corruption, or Grime (a smudge to wipe at home). Params[0]: the cost.
public class SelfCostEffect : EffectBase {
    private string _resource;
    private int _amount;

    public override void Init(EffectData data) {
        base.Init(data);
        _resource = data.Resource ?? string.Empty;
        _amount = Mathf.Max(0, Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f));
    }

    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        DollFighter user = outcome.User;
        DollEntity doll = user?.DataRef;
        if (doll == null || _amount <= 0) {
            return;
        }

        switch (_resource) {
            case "HP":
                int loss = Mathf.Min(_amount, user.RuntimeHP - 1);
                if (loss <= 0) {
                    return;
                }

                user.RuntimeHP -= loss;
                doll.Status.HP_Current = user.RuntimeHP;
                GameEventBus.PublishHPChanged(user.Name, user.RuntimeHP, user.RuntimeMaxHP);
                break;
            case "SAN":
                doll.Status.SAN_Current = Mathf.Max(0, doll.Status.SAN_Current - _amount);
                GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
                break;
            case "Corruption":
                doll.Status.Corruption = Mathf.Min(100f, doll.Status.Corruption + _amount);
                break;
            case "Grime":
                doll.Status.Grime = Mathf.Min(CareService.MaxGrime, doll.Status.Grime + _amount);
                break;
            default:
                return;
        }

        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = user, Label = $"{provider?.Name} 代价：{EffectText.Resource(_resource)} -{_amount}" });
    }

    public override string Describe(ItemEntity provider, int rotation) {
        switch (_resource) {
            case "Corruption":
                return $"代价：每次使用侵蚀 +{_amount}（回家要净化）";
            case "Grime":
                return $"代价：每次使用零号身上多 {_amount} 块污渍";
            default:
                return $"代价：每次使用 {EffectText.Resource(_resource)} -{_amount}";
        }
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Resource != "HP" && data.Resource != "SAN" && data.Resource != "Corruption" && data.Resource != "Grime") {
            report.AddError($"[{ownerID}] SelfCost needs Resource HP / SAN / Corruption / Grime, got [{data.Resource}].");
        }

        if (data.Params == null || data.Params.Length == 0 || data.Params[0] < 1f) {
            report.AddError($"[{ownerID}] SelfCost needs Params[0] >= 1.");
        }
    }
}
