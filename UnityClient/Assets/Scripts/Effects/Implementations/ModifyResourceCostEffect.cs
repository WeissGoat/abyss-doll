using System;
using System.Collections.Generic;

public class ModifyResourceCostEffect : EffectBase {
    private EffectTriggerType _trigger;
    private EffectResourceType _resource;
    private EffectModifierOperation _operation;
    private float _value;

    public override void Init(EffectData data) {
        base.Init(data);
        Enum.TryParse(data.Trigger, true, out _trigger);
        Enum.TryParse(data.Resource, true, out _resource);
        Enum.TryParse(data.Operation, true, out _operation);
        _value = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
    }

    public override void CollectModifiers(EffectModifierContext context, List<EffectModifier> output) {
        if (context == null || output == null || context.Trigger != _trigger || context.Resource != _resource) {
            return;
        }

        output.Add(new EffectModifier {
            EffectID = EffectID,
            SourceName = context.SourceName,
            Operation = _operation,
            Value = _value
        });
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (!Enum.TryParse(data.Trigger, true, out EffectTriggerType _)) {
            report.AddError($"[{ownerID}] ModifyResourceCost requires valid Trigger. Got [{data.Trigger}].");
        }

        if (!Enum.TryParse(data.Resource, true, out EffectResourceType resource) || resource == EffectResourceType.None) {
            report.AddError($"[{ownerID}] ModifyResourceCost requires valid Resource. Got [{data.Resource}].");
        }

        if (!Enum.TryParse(data.Operation, true, out EffectModifierOperation _)) {
            report.AddError($"[{ownerID}] ModifyResourceCost requires valid Operation. Got [{data.Operation}].");
        }

        if (data.Params == null || data.Params.Length == 0) {
            report.AddError($"[{ownerID}] ModifyResourceCost requires Params[0] value.");
        }
    }
}
