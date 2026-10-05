using System.Collections.Generic;
using UnityEngine;

// T0 B segment (docs/specs/2026-10-05-t0-first-loop.md): the first dive is a fixed three-step route on the shallow
// edge instead of the random layer 1 map — the voice, one scavenger bug, the old mine cart where the player picks the
// one thing to bring back. The run's facts (what she heard, what came back, how she was before) feed the C segment.
public static class PrologueShallowRun {
    public const string MapProfileID = "t0_shallow_edge";
    public const string CoreDustID = "mat_core_dust_stable";
    public const string ScrapID = "loot_gear_scrap";
    public const string MemoryNoiseID = "quest_memory_noise";
    public const int ListenSanCost = 5;

    public static PrologueRunState Current { get; private set; }

    public static bool IsPrologueLayer(DungeonLayer layer) {
        return layer != null && layer.MapProfileID == MapProfileID;
    }

    public static PrologueRunState Begin(PlayerProfile player) {
        DollEntity doll = player?.ActiveDoll;
        Current = new PrologueRunState {
            HPBefore = doll?.Status.HP_Current ?? 0,
            SANBefore = doll?.Status.SAN_Current ?? 0,
            WearBefore = doll?.Status.WearAndTear ?? 0f,
            CorruptionBefore = doll?.Status.Corruption ?? 0f
        };
        return Current;
    }

    public static void Reset() {
        Current = null;
    }

    public static DungeonLayer BuildLayer() {
        DungeonConfig config = ConfigManager.Dungeons != null && ConfigManager.Dungeons.ContainsKey(1) ? ConfigManager.Dungeons[1] : null;
        DungeonLayer layer = new DungeonLayer {
            LayerID = 1,
            MapBackgroundID = config?.MapBackgroundID,
            MapProfileID = MapProfileID
        };

        // Two scavenger bugs: a fight of two or three turns that leaves a mark on her, as the first dive should.
        NodeBase voice = new PrologueVoiceNode { NodeID = "t0_voice", NodeIconID = "node_eventnode_icon", RiskLevel = "Low", RiskHint = "下面有一段声音" };
        CombatNode bug = new CombatNode { NodeID = "t0_scavenger", NodeIconID = "node_combat_icon", RiskLevel = "Low", RiskHint = "两只拾荒虫", NoLoot = true };
        bug.MonsterIDs.Add("mob_scavenger_bug");
        bug.MonsterIDs.Add("mob_scavenger_bug");
        NodeBase cart = new PrologueBringBackNode { NodeID = "t0_mine_cart", NodeIconID = "node_treasurenode_icon", RiskLevel = "Low", RiskHint = "旧矿车" };

        NodeBase[] route = { voice, bug, cart };
        for (int i = 0; i < route.Length; i++) {
            route[i].MapRow = i;
            route[i].MapColumn = 0;
            route[i].RouteTheme = "SafeRoute";
            if (i + 1 < route.Length) {
                route[i].NextNodes.Add(route[i + 1]);
            }

            layer.NodeRows.Add(new List<NodeBase> { route[i] });
        }

        layer.RootNode = voice;
        layer.EntryNodes.Add(voice);
        return layer;
    }

    // The bring-back choice: the memory shard only shows when she listened to the voice.
    public static List<string> BringBackOptions() {
        List<string> options = new List<string> { CoreDustID, ScrapID };
        if (Current != null && Current.ListenedToVoice) {
            options.Add(MemoryNoiseID);
        }

        return options;
    }

    public static void ListenToVoice(PlayerProfile player) {
        if (Current == null || Current.ListenedToVoice) {
            return;
        }

        Current.ListenedToVoice = true;
        DollEntity doll = player?.ActiveDoll;
        if (doll != null) {
            doll.Status.SAN_Current = Mathf.Max(0, doll.Status.SAN_Current - ListenSanCost);
        }
    }

    // The chosen item goes straight to the workshop stash; the run then ends like an evacuation.
    public static ItemEntity BringBack(PlayerProfile player, string configID) {
        if (player == null || !BringBackOptions().Contains(configID)) {
            return null;
        }

        ItemEntity item = ConfigManager.CreateItem(configID);
        if (item == null) {
            return null;
        }

        ItemLifecycleService.MarkGroundInventoryItem(player, item, "PrologueBringBack");
        if (Current != null) {
            Current.BroughtBackID = configID;
            Current.BroughtBackName = item.Name;
        }

        return item;
    }
}

public sealed class PrologueRunState {
    public bool ListenedToVoice;
    public string BroughtBackID;
    public string BroughtBackName;
    public int HPBefore;
    public int SANBefore;
    public float WearBefore;
    public float CorruptionBefore;
    public bool Defeated;
    public bool ReturnHomeDone;
}
