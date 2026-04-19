using UnityEngine;

public abstract class EffectBase {
    public string EffectID { get; protected set; }
    public int Level { get; protected set; }
    
    public virtual void Init(EffectData data) {
        EffectID = data.EffectID;
        Level = data.Level;
    }
    
    // provider can be null if the effect comes from a global source (like a prosthetic)
    public abstract void Apply(ItemEntity provider, ItemEntity target);
    
    public abstract void Remove(ItemEntity provider, ItemEntity target);
}
