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
    
    // 参数: (阵营类型)
    public static event Action<FactionType> OnTurnStarted;

    // --- 背包/物品事件 ---
    public static event Action<string, int, int> OnItemPlaced;
    public static event Action<string> OnItemRemoved;

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
    
    public static void PublishDamageDealt(string attacker, string target, int damage) {
        // 先播一段受击飘字的时间，再触发实际UI变动（这里为了简便，在队列里直接组合）
        VisualQueue.Enqueue(new LogWaitCommand($"💥 [{target}] 受击飘字 -{damage}!", 0.4f));
        VisualQueue.Enqueue(new ActionCommand(() => OnDamageDealt?.Invoke(attacker, target, damage)));
    }
    
    public static void PublishAttackAction(string attacker, string target, string weaponName) {
        // 模拟攻击起手动作延时
        VisualQueue.Enqueue(new LogWaitCommand($"⚔️ [{attacker}] 对 [{target}] 使用了 [{weaponName}] 攻击!", 0.6f));
        VisualQueue.Enqueue(new ActionCommand(() => OnAttackAction?.Invoke(attacker, target, weaponName)));
    }
    
    public static void PublishTurnStarted(FactionType factionType) {
        // 回合切换往往需要给一个视觉缓冲
        VisualQueue.Enqueue(new LogWaitCommand($"🔄 切换到回合: {factionType} Turn", 0.5f));
        VisualQueue.Enqueue(new ActionCommand(() => OnTurnStarted?.Invoke(factionType)));
    }
    
    // 瞬间执行的 UI 事件 (不进战斗队列，比如拖拽物品立刻响应)
    public static void PublishItemPlaced(string itemInstanceID, int x, int y) => OnItemPlaced?.Invoke(itemInstanceID, x, y);
    public static void PublishItemRemoved(string itemInstanceID) => OnItemRemoved?.Invoke(itemInstanceID);

    public static void ResetAllListeners() {
        OnHPChanged = null;
        OnAPChanged = null;
        OnSANChanged = null;
        OnShieldChanged = null;
        OnDamageDealt = null;
        OnAttackAction = null;
        OnTurnStarted = null;
        OnItemPlaced = null;
        OnItemRemoved = null;
    }
}
