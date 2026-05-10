using System.Collections.Generic;

public class EffectModifierContext {
    public EffectTriggerType Trigger;
    public EffectResourceType Resource;
    public DollEntity ActiveDoll;
    public BackpackGrid BackpackGrid;
    public NodeBase TargetNode;
    public float BaseValue;
    public float CurrentValue;
    public ItemEntity SourceItem;
    public ProstheticEntity SourceProsthetic;

    public string SourceName {
        get {
            if (SourceItem != null) {
                return SourceItem.Name;
            }

            if (SourceProsthetic != null) {
                return SourceProsthetic.Name;
            }

            return string.Empty;
        }
    }
}

public class EffectModifier {
    public string EffectID;
    public string SourceName;
    public EffectModifierOperation Operation;
    public float Value;
}

public class EffectModifierResolution {
    public float BaseValue;
    public float FinalValue;
    public readonly List<EffectModifier> Modifiers = new List<EffectModifier>();
}
