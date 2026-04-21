using UnityEngine;

public abstract class EffectBase {
    public string EffectID { get; protected set; }
    public int Level { get; protected set; }
    
    // 声明该 Effect 监听的战斗阶段，默认为 None（即网格连结类计算，不参与回合流程）
    public virtual CombatEventType ListenEvent => CombatEventType.None;
    
    public virtual void Init(EffectData data) {
        EffectID = data.EffectID;
        Level = data.Level;
    }
    
    // 用于网格物品间的连结作用 (GridSolver)
    public virtual void Apply(ItemEntity provider, ItemEntity target) {}
    
    // 用于向战斗实体施加效果 (如回合初加盾、回血)
    public virtual void ApplyToFighter(FighterEntity fighter, ItemEntity provider) {}
    
    public virtual void Remove(ItemEntity provider, ItemEntity target) {}
}
