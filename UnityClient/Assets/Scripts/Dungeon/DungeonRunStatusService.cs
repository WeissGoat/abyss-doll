using System.Collections.Generic;

// What the map shows beside the route so the player can weigh going deeper against getting out: Zero's state, what
// the next step costs, how far the nearest exit she can already see is, and what evacuating now would bring home.
public class DungeonRunStatus {
    public int LayerID;
    public string LayerName;
    public int HP;
    public int HPMax;
    public int SAN;
    public int SANMax;
    public float Wear;
    public float Corruption;
    // SAN the cheapest and dearest of the next steps cost; both 0 when there is nowhere left to go.
    public int NextStepSanMin;
    public int NextStepSanMax;
    // Steps her SAN still covers at the cheapest next cost; -1 when that step is free.
    public int StepsSanAllows = -1;
    // The road tax in the next step's cost, e.g. "过载 +1、重伤 +2" (empty when there is none).
    public string SanTaxText;
    // Shortest walk to a safe room or stairs that is not hidden in the fog; -1 when none is in sight.
    public int StepsToExit = -1;
    public string ExitLabel;
    // Loot picked up this run that is still in her backpack, valued the way the settlement estimates it.
    public int LootCount;
    public int LootValue;
}

public static class DungeonRunStatusService {
    public static DungeonRunStatus Build(DungeonManager dungeon, PlayerProfile player) {
        DungeonRunStatus status = new DungeonRunStatus();
        DungeonLayer layer = dungeon?.CurrentLayer;
        if (layer != null) {
            status.LayerID = layer.LayerID;
            status.LayerName = ConfigManager.Dungeons != null && ConfigManager.Dungeons.TryGetValue(layer.LayerID, out DungeonConfig config)
                ? config?.Name
                : null;
        }

        DollEntity doll = player?.ActiveDoll;
        if (doll?.Status != null) {
            status.HP = doll.Status.HP_Current;
            status.HPMax = doll.Status.HP_Max;
            status.SAN = doll.Status.SAN_Current;
            status.SANMax = doll.Status.SAN_Max;
            status.Wear = doll.Status.WearAndTear;
            status.Corruption = doll.Status.Corruption;
        }

        if (layer == null) {
            return status;
        }

        bool anyNext = false;
        foreach (NodeBase next in NextChoices(layer)) {
            int cost = dungeon.PreviewSanCost(next);
            status.NextStepSanMin = anyNext ? System.Math.Min(status.NextStepSanMin, cost) : cost;
            status.NextStepSanMax = anyNext ? System.Math.Max(status.NextStepSanMax, cost) : cost;
            anyNext = true;
        }

        status.SanTaxText = DungeonSanTaxService.Describe(dungeon.PreviewSanTax());
        if (anyNext && status.NextStepSanMin > 0) {
            status.StepsSanAllows = status.SAN / status.NextStepSanMin;
        }

        status.StepsToExit = FindStepsToKnownExit(layer, out NodeBase exit);
        status.ExitLabel = exit is StairsNode ? "阶梯" : exit is SafeRoomNode ? "升降梯" : null;

        foreach (ItemEntity item in dungeon.CarriedRunLoot()) {
            status.LootCount++;
            status.LootValue += item.BaseValue;
        }

        return status;
    }

    // The nodes she can step onto from where she stands (the layer's entries before her first step).
    public static List<NodeBase> NextChoices(DungeonLayer layer) {
        List<NodeBase> choices = new List<NodeBase>();
        IEnumerable<NodeBase> source = layer?.CurrentNode != null ? layer.CurrentNode.NextNodes : layer?.EntryNodes;
        if (source == null) {
            return choices;
        }

        foreach (NodeBase node in source) {
            if (node != null && !node.IsVisited) {
                choices.Add(node);
            }
        }

        return choices;
    }

    // Breadth-first over the routes ahead, only through nodes the fog does not hide: the player can only plan with what
    // the map shows. A step is one node walked. The node she stands on does not count: once she has left a safe room
    // for the map, it no longer offers the way out.
    public static int FindStepsToKnownExit(DungeonLayer layer, out NodeBase exit) {
        exit = null;
        if (layer == null) {
            return -1;
        }

        Queue<KeyValuePair<NodeBase, int>> pending = new Queue<KeyValuePair<NodeBase, int>>();
        HashSet<NodeBase> seen = new HashSet<NodeBase>();
        foreach (NodeBase node in NextChoices(layer)) {
            pending.Enqueue(new KeyValuePair<NodeBase, int>(node, 1));
        }

        while (pending.Count > 0) {
            KeyValuePair<NodeBase, int> entry = pending.Dequeue();
            NodeBase node = entry.Key;
            if (node == null || !seen.Add(node)
                || DungeonMapVisibilityService.ResolveVisibility(layer, node) == DungeonNodeVisibilityState.Hidden) {
                continue;
            }

            if (IsExit(node)) {
                exit = node;
                return entry.Value;
            }

            if (node.NextNodes == null) {
                continue;
            }

            foreach (NodeBase next in node.NextNodes) {
                pending.Enqueue(new KeyValuePair<NodeBase, int>(next, entry.Value + 1));
            }
        }

        return -1;
    }

    private static bool IsExit(NodeBase node) {
        return node is SafeRoomNode || node is StairsNode;
    }
}
