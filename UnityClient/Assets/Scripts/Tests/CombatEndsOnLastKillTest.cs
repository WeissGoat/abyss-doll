using System.Collections.Generic;
using UnityEngine;

// Regression (found 2026-10-04): combat has to end as soon as the player's attack defeats the last enemy,
// without the player pressing "end turn" again.
public static class CombatEndsOnLastKillTest {
    public static void Run() {
        Debug.Log("=== Running Combat Ends On Last Kill Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CombatNode combatNode = new CombatNode {
            NodeID = "test_combat_last_kill"
        };
        combatNode.MonsterIDs.Add("mob_scavenger_bug");
        combatNode.NextNodes.Add(new SafeRoomNode { NodeID = "test_after_last_kill" });
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = combatNode,
            CurrentNode = combatNode
        };

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        GridSolver.RecalculateAllEffects(doll);
        ItemEntity weapon = null;
        foreach (ItemEntity item in ((BackpackGrid)doll.RuntimeGrid).ContainedItems) {
            if (item.ItemType == nameof(ItemType.Weapon)) {
                weapon = item;
            }
        }

        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
        FighterEntity enemy = core.Combat.EnemyFaction.Fighters[0];
        enemy.RuntimeHP = 1;

        string reason = string.Empty;
        bool attacked = weapon != null
            && ItemUseService.TryUseItem(weapon, out reason)
            && ItemUseService.TryConfirmPendingTarget(enemy, out reason);

        if (attacked && enemy.RuntimeHP <= 0 && core.Combat.CurrentState == CombatState.End) {
            Debug.Log("Combat Ends On Last Kill PASSED.");
        } else {
            Debug.LogError($"Combat Ends On Last Kill FAILED. Weapon={weapon?.Name ?? "none"}, Attacked={attacked}, Reason={reason}, EnemyHP={enemy.RuntimeHP}, State={core.Combat.CurrentState}");
        }

        Debug.Log("=== Combat Ends On Last Kill Test Finished ===");
    }
}
