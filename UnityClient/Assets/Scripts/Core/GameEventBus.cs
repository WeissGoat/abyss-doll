using System;

public static class GameEventBus {
    // --- 状态更新事件 ---
    // 参数: (实体ID, 当前值, 最大值)
    public static event Action<string, int, int> OnHPChanged;
    public static event Action<string, int, int> OnAPChanged;
    public static event Action<string, int, int> OnSANChanged;
    public static event Action<string, int> OnShieldChanged;

    // --- 战斗流程事件 ---
    // 参数: (发起者ID, 目标ID, 伤害值)
    public static event Action<string, string, int> OnDamageDealt;
    
    // 参数: (阵营类型)
    public static event Action<FactionType> OnTurnStarted;

    // --- 背包/物品事件 ---
    // 参数: (物品实例ID, X坐标, Y坐标)
    public static event Action<string, int, int> OnItemPlaced;
    public static event Action<string> OnItemRemoved;

    // 触发器方法
    public static void PublishHPChanged(string id, int current, int max) => OnHPChanged?.Invoke(id, current, max);
    public static void PublishAPChanged(string id, int current, int max) => OnAPChanged?.Invoke(id, current, max);
    public static void PublishSANChanged(string id, int current, int max) => OnSANChanged?.Invoke(id, current, max);
    public static void PublishShieldChanged(string id, int current) => OnShieldChanged?.Invoke(id, current);
    
    public static void PublishDamageDealt(string attacker, string target, int damage) => OnDamageDealt?.Invoke(attacker, target, damage);
    public static void PublishTurnStarted(FactionType factionType) => OnTurnStarted?.Invoke(factionType);
    
    public static void PublishItemPlaced(string itemInstanceID, int x, int y) => OnItemPlaced?.Invoke(itemInstanceID, x, y);
    public static void PublishItemRemoved(string itemInstanceID) => OnItemRemoved?.Invoke(itemInstanceID);
}
