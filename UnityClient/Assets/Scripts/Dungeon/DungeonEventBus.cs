using System;

public static class DungeonEventBus {
    public static event Action OnLayerLoaded;
    public static event Action<NodeBase, int> OnNodeEntered;
    public static event Action<NodeBase> OnSafeRoomEntered;
    public static event Action<StairsNode> OnStairsEntered;

    public static event Action OnNodeSettlementCompleted;
    public static event Action OnNodeResolutionFinished;
    public static event Action<DungeonNodeResolutionResult> OnDungeonNodeResolutionPrepared;
    public static event Action<CombatLootPickupResult> OnCombatLootPrepared;
    public static event Action<CombatLootCollectionResult> OnCombatLootCollected;

    public static event Action OnDungeonEvacuated;
    public static event Action OnDungeonDefeated;
    public static event Action<bool> OnDungeonSettled;
    public static event Action<DungeonSettlementResult> OnDungeonSettlementPrepared;
    public static event Action<int> OnDungeonStartLayerUnlocked;
    public static event Action<int> OnDungeonRunStarted;
    public static event Action<int, string> OnDungeonStartLayerRejected;

    public static void PublishLayerLoaded() {
        OnLayerLoaded?.Invoke();
    }

    public static void PublishNodeEntered(NodeBase node, int sanCost) {
        OnNodeEntered?.Invoke(node, sanCost);
    }

    public static void PublishSafeRoomEntered(NodeBase node) {
        OnSafeRoomEntered?.Invoke(node);
    }

    public static void PublishStairsEntered(StairsNode node) {
        OnStairsEntered?.Invoke(node);
    }

    public static void PublishNodeSettlementCompleted() {
        OnNodeSettlementCompleted?.Invoke();
    }

    public static void PublishNodeResolutionFinished() {
        OnNodeResolutionFinished?.Invoke();
    }

    public static void PublishDungeonNodeResolutionPrepared(DungeonNodeResolutionResult result) {
        OnDungeonNodeResolutionPrepared?.Invoke(result);
    }

    public static void PublishCombatLootPrepared(CombatLootPickupResult result) {
        OnCombatLootPrepared?.Invoke(result);
    }

    public static void PublishCombatLootCollected(CombatLootCollectionResult result) {
        OnCombatLootCollected?.Invoke(result);
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

    public static void PublishDungeonStartLayerUnlocked(int layerID) {
        OnDungeonStartLayerUnlocked?.Invoke(layerID);
    }

    public static void PublishDungeonRunStarted(int startLayerID) {
        OnDungeonRunStarted?.Invoke(startLayerID);
    }

    public static void PublishDungeonStartLayerRejected(int layerID, string reason) {
        OnDungeonStartLayerRejected?.Invoke(layerID, reason);
    }

    public static void ResetAllListeners() {
        OnLayerLoaded = null;
        OnNodeEntered = null;
        OnSafeRoomEntered = null;
        OnStairsEntered = null;
        OnNodeSettlementCompleted = null;
        OnNodeResolutionFinished = null;
        OnDungeonNodeResolutionPrepared = null;
        OnCombatLootPrepared = null;
        OnCombatLootCollected = null;
        OnDungeonEvacuated = null;
        OnDungeonDefeated = null;
        OnDungeonSettled = null;
        OnDungeonSettlementPrepared = null;
        OnDungeonStartLayerUnlocked = null;
        OnDungeonRunStarted = null;
        OnDungeonStartLayerRejected = null;
    }
}
