using UnityEngine;

public class ItemUseContext {
    public DollEntity UserDoll;
    public DollFighter UserFighter;
    public FighterEntity PrimaryTarget;
    public ItemEntity SourceItem;
    public SafeRoomNode SafeRoomNode;
    public bool InCombat;
    public bool InSafeRoom;
}

// One use of a backpack item in a fight, after its damage or payload landed (CombatItemEffects.AfterUse).
public class ItemUseOutcome {
    public DollFighter User;
    public ItemEntity Item;
    public FighterEntity Target;
    public int Damage;
    public bool Killed;
}

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
    
    // 用于主动使用物品时执行效果（战斗中使用道具 / 安全区使用消耗品）
    public virtual void ApplyOnUse(ItemUseContext context, ItemEntity provider) {
        if (context?.UserFighter != null) {
            ApplyToFighter(context.UserFighter, provider);
        }
    }

    public virtual void CollectModifiers(EffectModifierContext context, System.Collections.Generic.List<EffectModifier> output) {}

    // Grid pass with the backpack at hand (GridSolver); by default the plain item-to-item Apply.
    public virtual void ApplyInGrid(ItemEntity provider, ItemEntity target, BackpackGrid grid) {
        Apply(provider, target);
    }

    // The item carrying this effect was just used in a fight (a weapon strike or an active item).
    public virtual void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {}

    // An item next to the provider (in the effect's Target direction) was just used.
    public virtual void OnNeighborUsed(ItemUseOutcome outcome, ItemEntity provider) {}

    // Zero was hit by a monster while the provider sits in her backpack.
    public virtual void OnOwnerHit(DollFighter owner, FighterEntity attacker, int shieldBefore, ItemEntity provider) {}

    // False blocks using the provider right now (e.g. its uses this fight are spent).
    public virtual bool AllowsUse(ItemEntity provider, out string reason) {
        reason = string.Empty;
        return true;
    }

    // One line for the item tooltip; empty when the use hint already says it.
    public virtual string Describe(ItemEntity provider, int rotation) {
        return string.Empty;
    }

    public virtual void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {}
    
    public virtual void Remove(ItemEntity provider, ItemEntity target) {}
}
