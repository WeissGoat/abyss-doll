using System;

public static class DungeonEventBus {
    // 派发节点进入事件：参数为 (进入的节点实例, 需要扣除的SAN值过路费)
    public static event Action<NodeBase, int> OnNodeEntered;
    
    public static void PublishNodeEntered(NodeBase node, int sanCost) {
        OnNodeEntered?.Invoke(node, sanCost);
    }
}
