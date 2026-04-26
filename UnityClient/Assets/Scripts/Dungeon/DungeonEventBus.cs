using System;

public static class DungeonEventBus {
    // 深渊层地图加载完毕
    public static event Action OnLayerLoaded;
    
    // 派发节点进入事件：参数为 (进入的节点实例, 需要扣除的SAN值过路费)
    public static event Action<NodeBase, int> OnNodeEntered;
    
    // 进入安全区节点
    public static event Action<NodeBase> OnSafeRoomEntered;
    
    // 战斗节点胜利，可继续前进
    public static event Action OnCombatNodeCleared;
    
    // 玩家主动从安全区撤离 (或打完Boss自动撤离)
    public static event Action OnDungeonEvacuated;
    
    // 玩家在战斗中阵亡 (失败结算)
    public static event Action OnDungeonDefeated;
    
    // 深渊最终结算：参数为 (是否为胜利撤离)
    public static event Action<bool> OnDungeonSettled;
    public static event Action<DungeonSettlementResult> OnDungeonSettlementPrepared;
    
    // 领域事件必须同步发生，不能绑在表现队列上等待动画结束
    public static void PublishLayerLoaded() {
        OnLayerLoaded?.Invoke();
    }
    
    public static void PublishNodeEntered(NodeBase node, int sanCost) {
        OnNodeEntered?.Invoke(node, sanCost);
    }
    
    public static void PublishSafeRoomEntered(NodeBase node) {
        OnSafeRoomEntered?.Invoke(node);
    }
    
    public static void PublishCombatNodeCleared() {
        OnCombatNodeCleared?.Invoke();
    }
    
    public static void PublishDungeonEvacuated() {
        OnDungeonEvacuated?.Invoke();
    }
    
    public static void PublishDungeonDefeated() {
        OnDungeonDefeated?.Invoke();
    }
    
    public static void PublishDungeonSettled(bool isVictory) {
        OnDungeonSettled?.Invoke(isVictory);
    }

    public static void PublishDungeonSettlementPrepared(DungeonSettlementResult result) {
        OnDungeonSettlementPrepared?.Invoke(result);
    }

    public static void ResetAllListeners() {
        OnLayerLoaded = null;
        OnNodeEntered = null;
        OnSafeRoomEntered = null;
        OnCombatNodeCleared = null;
        OnDungeonEvacuated = null;
        OnDungeonDefeated = null;
        OnDungeonSettled = null;
        OnDungeonSettlementPrepared = null;
    }
}
