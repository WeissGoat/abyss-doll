using System;
using System.Collections.Generic;

[Serializable]
public class ProstheticEntity {
    public string ProstheticID;
    public string Name;
    public string Level;
    public string SlotType;
    
    public EffectData PassiveEffect; // If it's a single effect in some configs
    public List<EffectData> PassiveEffects = new List<EffectData>(); // If multiple
}
