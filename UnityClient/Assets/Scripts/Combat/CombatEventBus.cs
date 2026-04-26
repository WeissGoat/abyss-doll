using System;

public static class CombatEventBus {
    // 派发战斗流程节点的通用事件：参数为 (事件阶段, 当前正处于活跃状态的阵营)
    public static event Action<CombatEventType, CombatFaction> OnCombatPhase;
    
    public static void Publish(CombatEventType phase, CombatFaction activeFaction) {
        OnCombatPhase?.Invoke(phase, activeFaction);
    }

    public static void ResetAllListeners() {
        OnCombatPhase = null;
    }
}
