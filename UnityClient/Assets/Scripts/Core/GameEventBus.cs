using System;

public static class GameEventBus {
    // --- 状态更新事件 (供 UI 监听) ---
    public static event Action<string, int, int> OnHPChanged;
    public static event Action<string, int, int> OnAPChanged;
    public static event Action<string, int, int> OnSANChanged;
    public static event Action<string, int> OnShieldChanged;

    // --- 战斗视觉表现事件 ---
    // 参数: (发起者ID, 目标ID, 伤害值)
    public static event Action<string, string, int> OnDamageDealt;
    // 参数: (发起者ID, 目标ID, 动作描述)
    public static event Action<string, string, string> OnAttackAction;
    // Strike / hit / shield / heal / interference with the fighter objects; see CombatBeat.
    public static event Action<CombatBeat> OnCombatBeat;
    
    // 参数: (阵营类型)
    public static event Action<FactionType> OnTurnStarted;

    // --- 背包/物品事件 ---
    public static event Action<string, int, int> OnItemPlaced;
    public static event Action<string> OnItemRemoved;
    public static event Action<string, bool> OnTargetSelectionChanged;

    // 触发器方法 (底层调用，通过 VisualQueue 排队，非阻塞后端计算)
    public static void PublishHPChanged(string id, int current, int max) {
        VisualQueue.Enqueue(new ActionCommand(() => OnHPChanged?.Invoke(id, current, max)));
    }
    
    public static void PublishAPChanged(string id, int current, int max) {
        // AP变动一般是点击武器瞬间发生，不需要漫长延时，但需要排队以防跟战斗动画冲突
        VisualQueue.Enqueue(new ActionCommand(() => OnAPChanged?.Invoke(id, current, max)));
    }
    
    public static void PublishSANChanged(string id, int current, int max) {
        VisualQueue.Enqueue(new ActionCommand(() => OnSANChanged?.Invoke(id, current, max)));
    }
    
    public static void PublishShieldChanged(string id, int current) {
        VisualQueue.Enqueue(new ActionCommand(() => OnShieldChanged?.Invoke(id, current)));
    }
    
    // The pause belongs to the Hit beat that TakeDamage publishes right after this.
    public static void PublishDamageDealt(string attacker, string target, int damage) {
        VisualQueue.Enqueue(new ActionCommand(() => OnDamageDealt?.Invoke(attacker, target, damage)));
    }

    // The attacker's strike: the screen plays it (lunge) and the queue holds briefly before the hit lands.
    public static void PublishAttackAction(FighterEntity attacker, FighterEntity target, string weaponName, string weaponVisualID = null) {
        PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Strike, Actor = attacker, Target = target, Label = weaponName, SourceVisualID = weaponVisualID });
        string attackerName = attacker?.Name;
        string targetName = target?.Name;
        VisualQueue.Enqueue(new ActionCommand(() => OnAttackAction?.Invoke(attackerName, targetName, weaponName)));
    }

    // An enemy tampers with an item in the backpack (corrodes a weapon, stuffs in junk) instead of dealing damage.
    public static void PublishInterference(FighterEntity attacker, string itemName, string actionID, string verb) {
        PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = attacker, Label = $"{verb}：{itemName}" });
        string attackerName = attacker?.Name;
        VisualQueue.Enqueue(new ActionCommand(() => OnAttackAction?.Invoke(attackerName, itemName, actionID)));
    }

    public static void PublishCombatBeat(CombatBeat beat) {
        if (beat == null) {
            return;
        }

        VisualQueue.Enqueue(new ActionCommand(() => OnCombatBeat?.Invoke(beat)));
        VisualQueue.Enqueue(new LogWaitCommand(null, CombatBeat.HoldSeconds(beat.Kind)));
    }
    
    public static void PublishTurnStarted(FactionType factionType) {
        // 回合切换往往需要给一个视觉缓冲
        // The banner changes first, then a short beat before the side acts.
        VisualQueue.Enqueue(new ActionCommand(() => OnTurnStarted?.Invoke(factionType)));
        VisualQueue.Enqueue(new LogWaitCommand(null, 0.3f));
    }
    
    // 瞬间执行的 UI 事件 (不进战斗队列，比如拖拽物品立刻响应)
    public static void PublishItemPlaced(string itemInstanceID, int x, int y) => OnItemPlaced?.Invoke(itemInstanceID, x, y);
    public static void PublishItemRemoved(string itemInstanceID) => OnItemRemoved?.Invoke(itemInstanceID);
    public static void PublishTargetSelectionChanged(string message, bool isActive) => OnTargetSelectionChanged?.Invoke(message, isActive);

    public static void ResetAllListeners() {
        OnHPChanged = null;
        OnAPChanged = null;
        OnSANChanged = null;
        OnShieldChanged = null;
        OnDamageDealt = null;
        OnAttackAction = null;
        OnCombatBeat = null;
        OnTurnStarted = null;
        OnItemPlaced = null;
        OnItemRemoved = null;
        OnTargetSelectionChanged = null;
    }
}
