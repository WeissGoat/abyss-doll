using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

public class DungeonLayer {
    public int LayerID;
    public string MapBackgroundID;
    public string MapProfileID;
    public int RunSeed;
    public NodeBase RootNode;
    public NodeBase CurrentNode;
    public List<NodeBase> EntryNodes { get; private set; } = new List<NodeBase>();
    public List<List<NodeBase>> NodeRows { get; private set; } = new List<List<NodeBase>>();
    public string GenerationSummary { get; private set; }

    public void GenerateMapTree(DungeonConfig config, int runSeed = 0) {
        if (config != null) {
            LayerID = config.LayerID;
            MapBackgroundID = config.MapBackgroundID;
            MapProfileID = string.IsNullOrEmpty(config.MapProfileID) ? $"layer_{config.LayerID}_default" : config.MapProfileID;
        }

        RootNode = null;
        CurrentNode = null;
        EntryNodes.Clear();
        NodeRows.Clear();

        if (config == null) {
            GenerationSummary = "[DungeonMap] Config is null.";
            return;
        }

        RunSeed = ResolveSeed(config, runSeed);
        System.Random rng = new System.Random(RunSeed);

        int routeRowCount = ResolveRouteRowCount(config);
        int minWidth = ResolveMinWidth(config);
        int maxWidth = ResolveMaxWidth(config, minWidth);
        int minRouteCount = Mathf.Clamp(config.MinRouteCount > 0 ? config.MinRouteCount : minWidth, 1, maxWidth);

        for (int rowIndex = 0; rowIndex < routeRowCount; rowIndex++) {
            int width = ResolveRouteRowWidth(rowIndex, routeRowCount, minWidth, maxWidth, minRouteCount, rng);
            List<NodeBase> row = new List<NodeBase>();
            for (int columnIndex = 0; columnIndex < width; columnIndex++) {
                NodePoolEntry entry = PickRandomNode(config.NodePool, rng);
                NodeBase node = CreateConfiguredNode(entry, $"layer_{config.LayerID}_r{rowIndex}_c{columnIndex}", config.LayerID);
                if (node == null) {
                    continue;
                }

                ConfigureMapPosition(node, rowIndex, columnIndex, ResolveRouteTheme(columnIndex, width));
                row.Add(node);
            }

            if (row.Count > 0) {
                NodeRows.Add(row);
            }
        }

        CombatNode bossNode = NodeFactory.CreateNode("CombatNode") as CombatNode;
        if (bossNode != null) {
            bossNode.NodeID = $"layer_{config.LayerID}_boss";
            bossNode.NodeIconID = config.BossNodeIconID;
            bossNode.MonsterIDs = new List<string> { config.BossNode };
            ConfigureMapPosition(bossNode, NodeRows.Count, 0, "Boss");
            NodeRows.Add(new List<NodeBase> { bossNode });
        }

        NodeBase endNode = CreateConfiguredNode(config.EndNode, $"layer_{config.LayerID}_end", config.LayerID);
        if (endNode != null) {
            ConfigureMapPosition(endNode, NodeRows.Count, 0, "SafeZone");
            NodeRows.Add(new List<NodeBase> { endNode });
        }

        BuildNetworkEdges();
        if (NodeRows.Count > 0 && NodeRows[0].Count > 0) {
            EntryNodes.AddRange(NodeRows[0]);
            RootNode = EntryNodes[0];
        }

        GenerationSummary = BuildGenerationSummary();
        Debug.Log(GenerationSummary);
    }

    private NodeBase CreateConfiguredNode(NodePoolEntry entry, string nodeID, int layerID) {
        if (entry == null) {
            return null;
        }

        NodeBase node = NodeFactory.CreateNode(entry.NodeType);
        if (node == null) {
            return null;
        }

        node.Init(entry);
        node.NodeID = nodeID;

        if (node is StairsNode stairsNode) {
            stairsNode.LayerID = layerID;
        }

        return node;
    }

    private void ConfigureMapPosition(NodeBase node, int rowIndex, int columnIndex, string routeTheme) {
        if (node == null) {
            return;
        }

        node.MapRow = rowIndex;
        node.MapColumn = columnIndex;
        node.RouteTheme = routeTheme;
    }

    private void BuildNetworkEdges() {
        for (int rowIndex = 0; rowIndex < NodeRows.Count - 1; rowIndex++) {
            List<NodeBase> currentRow = NodeRows[rowIndex];
            List<NodeBase> nextRow = NodeRows[rowIndex + 1];
            if (currentRow == null || currentRow.Count == 0 || nextRow == null || nextRow.Count == 0) {
                continue;
            }

            if (nextRow.Count == 1) {
                foreach (NodeBase node in currentRow) {
                    AddEdge(node, nextRow[0]);
                }
                continue;
            }

            bool[] hasIncoming = new bool[nextRow.Count];
            for (int currentIndex = 0; currentIndex < currentRow.Count; currentIndex++) {
                NodeBase node = currentRow[currentIndex];
                int mappedIndex = MapColumnIndex(currentIndex, currentRow.Count, nextRow.Count);
                AddEdge(node, nextRow[mappedIndex]);
                hasIncoming[mappedIndex] = true;

                int adjacentIndex = Mathf.Clamp(mappedIndex + (currentIndex % 2 == 0 ? 1 : -1), 0, nextRow.Count - 1);
                if (adjacentIndex != mappedIndex) {
                    AddEdge(node, nextRow[adjacentIndex]);
                    hasIncoming[adjacentIndex] = true;
                }
            }

            for (int nextIndex = 0; nextIndex < nextRow.Count; nextIndex++) {
                if (hasIncoming[nextIndex]) {
                    continue;
                }

                int sourceIndex = MapColumnIndex(nextIndex, nextRow.Count, currentRow.Count);
                AddEdge(currentRow[sourceIndex], nextRow[nextIndex]);
            }
        }
    }

    private void AddEdge(NodeBase from, NodeBase to) {
        if (from == null || to == null) {
            return;
        }

        if (!from.NextNodes.Contains(to)) {
            from.NextNodes.Add(to);
        }
    }

    private int MapColumnIndex(int sourceIndex, int sourceCount, int targetCount) {
        if (targetCount <= 1 || sourceCount <= 1) {
            return 0;
        }

        float normalized = (float)sourceIndex / (float)(sourceCount - 1);
        return Mathf.Clamp(Mathf.RoundToInt(normalized * (targetCount - 1)), 0, targetCount - 1);
    }

    private NodePoolEntry PickRandomNode(List<NodePoolEntry> pool, System.Random rng) {
        if (pool == null || pool.Count == 0) {
            return null;
        }

        int totalWeight = 0;
        foreach (var p in pool) {
            totalWeight += p.Weight;
        }

        if (totalWeight <= 0) {
            return pool[0];
        }

        int roll = rng.Next(0, totalWeight);
        int current = 0;
        foreach (var p in pool) {
            current += p.Weight;
            if (roll < current) {
                return p;
            }
        }

        return pool[pool.Count - 1];
    }

    private int ResolveSeed(DungeonConfig config, int runSeed) {
        unchecked {
            int profileHash = StableHash(MapProfileID);
            int baseSeed = runSeed != 0 ? runSeed : (config.MapSeed != 0 ? config.MapSeed : config.LayerID * 73856093);
            return baseSeed ^ (config.LayerID * 19349663) ^ profileHash;
        }
    }

    private int StableHash(string value) {
        if (string.IsNullOrEmpty(value)) {
            return 0;
        }

        unchecked {
            int hash = 23;
            for (int i = 0; i < value.Length; i++) {
                hash = hash * 31 + value[i];
            }
            return hash;
        }
    }

    private int ResolveRouteRowCount(DungeonConfig config) {
        if (config.RowCount > 0) {
            return config.RowCount;
        }

        return Mathf.Max(2, config.ExpectedNodeCount - 1);
    }

    private int ResolveMinWidth(DungeonConfig config) {
        return Mathf.Max(1, config.MinWidth > 0 ? config.MinWidth : 2);
    }

    private int ResolveMaxWidth(DungeonConfig config, int minWidth) {
        return Mathf.Max(minWidth, config.MaxWidth > 0 ? config.MaxWidth : Mathf.Max(3, minWidth));
    }

    private int ResolveRouteRowWidth(int rowIndex, int rowCount, int minWidth, int maxWidth, int minRouteCount, System.Random rng) {
        if (rowCount <= 1) {
            return Mathf.Max(1, minWidth);
        }

        if (rowIndex == 0) {
            return Mathf.Clamp(minRouteCount, minWidth, maxWidth);
        }

        if (rowIndex == rowCount - 1) {
            return Mathf.Clamp(Mathf.Min(2, maxWidth), minWidth, maxWidth);
        }

        return rng.Next(minWidth, maxWidth + 1);
    }

    private string ResolveRouteTheme(int columnIndex, int width) {
        if (width <= 1) {
            return "Main";
        }

        if (columnIndex == 0) {
            return "Safe";
        }

        if (columnIndex == width - 1) {
            return "RiskReward";
        }

        return "Explore";
    }

    private string BuildGenerationSummary() {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[DungeonMap] Layer={LayerID}, Seed={RunSeed}, Profile={MapProfileID}, Rows={NodeRows.Count}");
        for (int rowIndex = 0; rowIndex < NodeRows.Count; rowIndex++) {
            List<NodeBase> row = NodeRows[rowIndex];
            builder.Append($"R{rowIndex}: ");
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++) {
                NodeBase node = row[columnIndex];
                if (columnIndex > 0) {
                    builder.Append(", ");
                }
                builder.Append($"{node.GetType().Name}({node.NodeID})");
            }
            builder.AppendLine();
        }

        builder.Append("Edges: ");
        bool firstEdge = true;
        foreach (List<NodeBase> row in NodeRows) {
            foreach (NodeBase node in row) {
                foreach (NodeBase next in node.NextNodes) {
                    if (!firstEdge) {
                        builder.Append("; ");
                    }
                    builder.Append($"{node.NodeID}->{next.NodeID}");
                    firstEdge = false;
                }
            }
        }

        return builder.ToString();
    }
}

public class DungeonManager {
    public DungeonLayer CurrentLayer;
    private readonly List<ItemEntity> _runAcceptedLoot = new List<ItemEntity>();
    private int _currentRunSeed;

    public DungeonManager() {
        DungeonEventBus.OnDungeonEvacuated += HandleEvacuate;
        DungeonEventBus.OnDungeonDefeated += HandleDefeat;
        DungeonEventBus.OnNodeSettlementCompleted += HandleNodeSettlementCompleted;
        DungeonEventBus.OnCombatLootCollected += HandleCombatLootCollected;
    }

    ~DungeonManager() {
        DungeonEventBus.OnDungeonEvacuated -= HandleEvacuate;
        DungeonEventBus.OnDungeonDefeated -= HandleDefeat;
        DungeonEventBus.OnNodeSettlementCompleted -= HandleNodeSettlementCompleted;
        DungeonEventBus.OnCombatLootCollected -= HandleCombatLootCollected;
    }

    public bool LoadLayer(int layerID) {
        return StartRunAtLayer(layerID);
    }

    public bool CanStartAtLayer(int layerID) {
        DiveReadinessResult readiness = DiveReadinessService.Evaluate(GameRoot.Core?.CurrentPlayer, layerID, false);
        return readiness.CanDive;
    }

    public bool StartRunAtLayer(int layerID) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DiveReadinessResult readiness = DiveReadinessService.Evaluate(player, layerID, true);
        if (!readiness.CanDive) {
            string reason = readiness.BuildSummary();
            Debug.LogWarning($"[DungeonManager] Cannot start dungeon at Layer {layerID}. {reason}");
            DungeonEventBus.PublishDungeonStartLayerRejected(layerID, reason);
            return false;
        }

        if (readiness.ProstheticsChanged) {
            Debug.Log($"[DungeonManager] Dive readiness auto-unequipped {readiness.RemovedProstheticIDs.Count} illegal prosthetic reference(s).");
        }

        player.LastSelectedDungeonStartLayer = layerID;
        _currentRunSeed = CreateRunSeed(layerID);
        Debug.Log($"[DungeonManager] Starting new dungeon run at Layer {layerID}.");
        ResetRunLootLedger();
        LoadLayerInternal(layerID, false);
        DungeonEventBus.PublishDungeonRunStarted(layerID);
        return true;
    }

    public bool TryUnlockNextStartLayerFromClearedLayer(int clearedLayerID) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null || clearedLayerID < 1) {
            return false;
        }

        int nextLayerID = clearedLayerID + 1;
        if (!ConfigManager.Dungeons.ContainsKey(nextLayerID)) {
            Debug.Log($"[DungeonManager] No Layer {nextLayerID} config exists. Start layer unlock skipped.");
            return false;
        }

        if (player.HighestUnlockedDungeonLayer >= nextLayerID) {
            return false;
        }

        player.HighestUnlockedDungeonLayer = nextLayerID;
        Debug.Log($"[DungeonManager] Unlocked dungeon start layer {nextLayerID}.");
        DungeonEventBus.PublishDungeonStartLayerUnlocked(nextLayerID);
        return true;
    }

    private void LoadLayerInternal(int layerID, bool resetRunLootLedger) {
        if (!ConfigManager.Dungeons.ContainsKey(layerID)) {
            Debug.LogError($"[DungeonManager] Layer {layerID} not found.");
            return;
        }

        var config = ConfigManager.Dungeons[layerID];
        Debug.Log($"[DungeonManager] Entering Layer {layerID}: {config.Name}");

        if (resetRunLootLedger) {
            ResetRunLootLedger();
        }

        CurrentLayer = new DungeonLayer();
        CurrentLayer.GenerateMapTree(config, _currentRunSeed);

        DungeonEventBus.PublishLayerLoaded();
    }

    private int CreateRunSeed(int startLayerID) {
        unchecked {
            return Environment.TickCount ^ (startLayerID * 83492791);
        }
    }

    public bool CanEnterNextLayer() {
        return CurrentLayer != null && ConfigManager.Dungeons.ContainsKey(CurrentLayer.LayerID + 1);
    }

    public bool EnterNextLayer() {
        if (CurrentLayer == null) {
            Debug.LogWarning("[DungeonManager] Cannot enter next layer because CurrentLayer is null.");
            return false;
        }

        int nextLayerID = CurrentLayer.LayerID + 1;
        if (!ConfigManager.Dungeons.ContainsKey(nextLayerID)) {
            Debug.Log("[DungeonManager] No next layer configured. Evacuating from abyss end.");
            DungeonEventBus.PublishDungeonEvacuated();
            return false;
        }

        Debug.Log($"[DungeonManager] Descending from Layer {CurrentLayer.LayerID} to Layer {nextLayerID}.");
        LoadLayerInternal(nextLayerID, false);
        return true;
    }

    public void MoveToNode(NodeBase targetNode) {
        if (!CanMoveToNode(targetNode)) {
            Debug.LogWarning($"[DungeonManager] Rejected invalid map move. Target={targetNode?.NodeID ?? "null"}, Current={CurrentLayer?.CurrentNode?.NodeID ?? "null"}");
            return;
        }

        CurrentLayer.CurrentNode = targetNode;
        targetNode.IsVisited = true;

        int cost = GetSanCostForNode(targetNode);
        DungeonEventBus.PublishNodeEntered(targetNode, cost);

        targetNode.OnEnterNode();
    }

    public bool CanMoveToNode(NodeBase targetNode) {
        if (CurrentLayer == null || targetNode == null || targetNode.IsVisited) {
            return false;
        }

        if (!LayerContainsNode(CurrentLayer, targetNode)) {
            return false;
        }

        if (CurrentLayer.CurrentNode == null) {
            return (CurrentLayer.EntryNodes != null && CurrentLayer.EntryNodes.Contains(targetNode))
                || (CurrentLayer.EntryNodes == null || CurrentLayer.EntryNodes.Count == 0) && targetNode == CurrentLayer.RootNode;
        }

        return CurrentLayer.CurrentNode.NextNodes != null && CurrentLayer.CurrentNode.NextNodes.Contains(targetNode);
    }

    private bool LayerContainsNode(DungeonLayer layer, NodeBase targetNode) {
        if (layer == null || targetNode == null) {
            return false;
        }

        if (layer.NodeRows != null && layer.NodeRows.Count > 0) {
            foreach (List<NodeBase> row in layer.NodeRows) {
                if (row != null && row.Contains(targetNode)) {
                    return true;
                }
            }
            return false;
        }

        if (targetNode == layer.RootNode || (layer.EntryNodes != null && layer.EntryNodes.Contains(targetNode))) {
            return true;
        }

        HashSet<NodeBase> visited = new HashSet<NodeBase>();
        Queue<NodeBase> queue = new Queue<NodeBase>();
        if (layer.RootNode != null) {
            queue.Enqueue(layer.RootNode);
            visited.Add(layer.RootNode);
        }

        while (queue.Count > 0) {
            NodeBase current = queue.Dequeue();
            if (current == targetNode) {
                return true;
            }

            if (current.NextNodes == null) {
                continue;
            }

            foreach (NodeBase next in current.NextNodes) {
                if (next != null && visited.Add(next)) {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    private int GetSanCostForNode(NodeBase node) {
        int baseCost = GetBaseSanCostForNode(node);
        EffectModifierResolution resolution = ResolveMovementSanCost(node, baseCost);
        int totalCost = Mathf.Max(0, Mathf.RoundToInt(resolution.FinalValue));

        if (resolution.Modifiers.Count > 0) {
            Debug.Log($"[DungeonManager] Movement SAN cost resolved. Node={node?.NodeID ?? "UnknownNode"}, Base={baseCost}, Final={totalCost}, Modifiers={resolution.Modifiers.Count}");
        }

        return totalCost;
    }

    private int GetBaseSanCostForNode(NodeBase node) {
        if (node is SafeRoomNode || node is StairsNode) {
            return 0;
        }

        return ConfigManager.Dungeons[CurrentLayer.LayerID].SANCostPerNode;
    }

    private EffectModifierResolution ResolveMovementSanCost(NodeBase node, int baseCost) {
        DollEntity activeDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        EffectModifierContext context = new EffectModifierContext {
            Trigger = EffectTriggerType.OnDungeonMoveCost,
            Resource = EffectResourceType.SAN,
            ActiveDoll = activeDoll,
            BackpackGrid = activeDoll?.RuntimeGrid as BackpackGrid,
            TargetNode = node,
            BaseValue = baseCost,
            CurrentValue = baseCost
        };

        return EffectModifierResolver.Resolve(context);
    }

    private void HandleNodeSettlementCompleted() {
        Debug.Log($"[DungeonManager] Node settlement completed: {CurrentLayer?.CurrentNode?.NodeID ?? "UnknownNode"}");
        ContinueAfterCurrentNodeResolved();
    }

    private void HandleCombatLootCollected(CombatLootCollectionResult result) {
        if (result?.AcceptedItems == null || result.AcceptedItems.Count == 0) {
            return;
        }

        _runAcceptedLoot.AddRange(result.AcceptedItems);
        Debug.Log($"[DungeonManager] Run loot ledger updated. AcceptedThisRun={_runAcceptedLoot.Count}");
    }

    private void ContinueAfterCurrentNodeResolved() {
        if (CurrentLayer?.CurrentNode == null) {
            return;
        }

        if (CurrentLayer.CurrentNode.NextNodes == null || CurrentLayer.CurrentNode.NextNodes.Count == 0) {
            Debug.Log("[DungeonManager] Reached a terminal node without next choices. Auto-evacuating as fallback.");
            DungeonEventBus.PublishDungeonEvacuated();
        } else {
            Debug.Log("[DungeonManager] Awaiting player to select the next node on the map...");
            DungeonEventBus.PublishNodeResolutionFinished();
        }
    }

    private void HandleEvacuate() {
        Debug.Log("<color=green>[DungeonManager] Handling Dungeon Evacuation (Victory/Escape).</color>");
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = true
        };

        var activeDoll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (activeDoll != null) {
            BackpackGrid grid = activeDoll.RuntimeGrid as BackpackGrid;
            if (grid != null) {
                List<ItemEntity> carriedRunLoot = CollectAcceptedLootStillInBackpack(grid);
                PopulateRunLootSummary(result, carriedRunLoot);

                foreach (var item in carriedRunLoot) {
                    if (item == null) {
                        continue;
                    }

                    result.LootTransferredCount++;
                    result.LootEstimatedValue += item.BaseValue;
                    result.LootNames.Add(item.Name);
                }

                Debug.Log(carriedRunLoot.Count > 0
                    ? $"[DungeonManager] {carriedRunLoot.Count} run loot items remain in the active backpack after evacuation."
                    : "[DungeonManager] No carried run loot to preserve after evacuation.");
            }
        }

        result.StashCountAfterSettlement = GameRoot.Core.CurrentPlayer.StashInventory.Count;
        Debug.Log($"[DungeonManager] Settlement summary prepared. LootCount={result.LootTransferredCount}, EstimatedValue={result.LootEstimatedValue}, StashCount={result.StashCountAfterSettlement}");
        DungeonEventBus.PublishDungeonSettlementPrepared(result);
        DungeonEventBus.PublishDungeonSettled(true);
        ResetRunLootLedger();
    }

    private void HandleDefeat() {
        Debug.Log("<color=red>[DungeonManager] Handling Dungeon Defeat.</color>");
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = false
        };

        var activeDoll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        if (activeDoll != null) {
            BackpackGrid grid = activeDoll.RuntimeGrid as BackpackGrid;
            if (grid != null) {
                PopulateRunLootSummary(result, null);
                List<ItemEntity> clearedItems = grid.ClearAllItems();
                PublishInventoryRemovalEvents(clearedItems);
                Debug.LogWarning($"[DungeonManager] Player defeated. All items in Backpack have been lost.");
            }
        }

        result.StashCountAfterSettlement = GameRoot.Core.CurrentPlayer.StashInventory.Count;
        Debug.Log($"[DungeonManager] Defeat settlement prepared. StashCount={result.StashCountAfterSettlement}");
        DungeonEventBus.PublishDungeonSettlementPrepared(result);
        DungeonEventBus.PublishDungeonSettled(false);
        ResetRunLootLedger();
    }

    private void PopulateRunLootSummary(DungeonSettlementResult result, IEnumerable<ItemEntity> preservedItems) {
        if (result == null) {
            return;
        }

        HashSet<string> preservedIds = new HashSet<string>();
        if (preservedItems != null) {
            foreach (var item in preservedItems) {
                if (item != null && !string.IsNullOrEmpty(item.InstanceID)) {
                    preservedIds.Add(item.InstanceID);
                }
            }
        }

        foreach (var item in _runAcceptedLoot) {
            if (item == null) {
                continue;
            }

            result.PickedUpCount++;
            result.PickedUpEstimatedValue += item.BaseValue;
            result.PickedUpNames.Add(item.Name);

            if (preservedIds.Contains(item.InstanceID)) {
                result.BroughtOutCount++;
                result.BroughtOutEstimatedValue += item.BaseValue;
                result.BroughtOutNames.Add(item.Name);
            } else {
                result.LostCount++;
                result.LostEstimatedValue += item.BaseValue;
                result.LostNames.Add(item.Name);
            }
        }
    }

    private List<ItemEntity> CollectAcceptedLootStillInBackpack(BackpackGrid grid) {
        List<ItemEntity> carriedRunLoot = new List<ItemEntity>();
        if (grid == null) {
            return carriedRunLoot;
        }

        HashSet<string> seenIds = new HashSet<string>();
        foreach (var item in _runAcceptedLoot) {
            if (item == null || string.IsNullOrEmpty(item.InstanceID)) {
                continue;
            }

            if (seenIds.Contains(item.InstanceID)) {
                continue;
            }

            if (grid.ContainedItems.Contains(item)) {
                carriedRunLoot.Add(item);
                seenIds.Add(item.InstanceID);
            }
        }

        return carriedRunLoot;
    }

    private void ResetRunLootLedger() {
        _runAcceptedLoot.Clear();
    }

    private string BuildStartLayerRejectionReason(PlayerProfile player, int layerID) {
        if (player == null) {
            return "Player profile is missing.";
        }

        if (layerID < 1) {
            return "Layer ID must be >= 1.";
        }

        if (!ConfigManager.Dungeons.ContainsKey(layerID)) {
            return "Layer config does not exist.";
        }

        if (layerID > player.HighestUnlockedDungeonLayer) {
            return $"Layer is locked. HighestUnlocked={player.HighestUnlockedDungeonLayer}.";
        }

        return "Unknown rejection reason.";
    }

    private void PublishInventoryRemovalEvents(IEnumerable<ItemEntity> removedItems) {
        if (removedItems == null) {
            return;
        }

        foreach (var item in removedItems) {
            if (item != null && !string.IsNullOrEmpty(item.InstanceID)) {
                GameEventBus.PublishItemRemoved(item.InstanceID);
            }
        }
    }
}