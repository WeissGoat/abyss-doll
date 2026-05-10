using System;
using System.Collections.Generic;

[Serializable]
public class ProstheticEntity {
    public string ProstheticID;
    public string Name;
    public string Level;
    public string SlotType;
    public List<EffectData> Effects = new List<EffectData>();
}
