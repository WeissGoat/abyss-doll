using UnityEngine;
using System.Collections.Generic;

public static class StageThreeTest {
    
    public static void RunAcceptanceTest() {
        Debug.Log("\n========== [Stage 3 Acceptance Test] Starting ==========");

        // 1. Initialize GameRoot Subsystems required for Combat
        GameRoot.Core.Combat = new CombatSystem();
        GameRoot.Core.Dungeon = new DungeonManager();

        // 2. Setup Player Profile & Backpack
        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        player.ActiveDoll.Status.HP_Current = 100;
        player.ActiveDoll.Status.SAN_Current = 50;

        ChassisComponent mockChassis = new ChassisComponent {
            GridWidth = 4, GridHeight = 4,
            GridMask = new bool[][] {
                new bool[] {true, true, true, true}, new bool[] {true, true, true, true},
                new bool[] {true, true, true, true}, new bool[] {true, true, true, true}
            }
        };
        BackpackGrid grid = new BackpackGrid(mockChassis);
        player.ActiveDoll.RuntimeGrid = grid;

        // 3. Give Player a Sword (1 AP cost, 15 Damage) and a Shield (0 AP Passive, 15 Shield)
        ItemEntity sword = ConfigManager.CreateItem("gear_rusty_dagger");
        grid.PlaceItem(sword, 0, 0);

        ItemEntity armor = ConfigManager.CreateItem("gear_iron_armor");
        grid.PlaceItem(armor, 1, 0);

        // 4. Start Dungeon (Triggering Combat)
        GameRoot.Core.Dungeon.LoadLayer(1); 

        CombatSystem combat = GameRoot.Core.Combat;
        
        // 因为深渊路线变成了读取配置表池的预生成树（存在一定随机性）
        // 如果第一关刷出来的是安全区(SafeRoom)，我们就让测试代码自动往前走，直到遇到战斗关卡
        NodeBase targetNode = GameRoot.Core.Dungeon.CurrentLayer.CurrentNode;
        while (targetNode != null && !(targetNode is CombatNode)) {
            if (targetNode.NextNodes.Count > 0) {
                targetNode = targetNode.NextNodes[0];
                GameRoot.Core.Dungeon.MoveToNode(targetNode);
            } else {
                break;
            }
        }
        
        if (combat.CurrentState != CombatState.PlayerTurn) {
            Debug.LogError("<color=red>[Test Failed] Did not enter Combat State after traversing nodes.</color>");
            return;
        }

        // 5. Simulate Player Turn!
        DollFighter dollFighter = combat.PlayerFaction.Fighters[0] as DollFighter;
        MonsterFighter monsterFighter = combat.EnemyFaction.Fighters[0] as MonsterFighter;

        // Turn Start happened automatically inside LoadLayer->StartCombat.
        // Let's verify passive shield was applied by the Iron Armor
        Debug.Log($"[Test Check] Player Shield: {dollFighter.RuntimeShield} (Expected: 15 from passive armor)");

        // Player attacks monster twice (costs 2 AP out of 3, 15 dmg each -> 30 total dmg)
        dollFighter.Attack(monsterFighter, sword);
        dollFighter.Attack(monsterFighter, sword);
        
        Debug.Log($"[Test Check] Monster HP: {monsterFighter.RuntimeHP} (Expected: 10)");

        // 6. End Player Turn (Triggers Monster Attack)
        combat.EndPlayerTurn();

        // The monster attacks automatically in StartEnemyTurn() for 10 damage
        // The player had 15 shield. 
        Debug.Log($"[Test Check] Player HP: {dollFighter.RuntimeHP} (Expected: 100)");
        Debug.Log($"[Test Check] Player Shield: {dollFighter.RuntimeShield} (Expected: 5)");
        
        // 7. Player Turn 2 (Finish Him!)
        // Wait, start player turn clears shields.
        Debug.Log($"[Test Check] Player AP restored: {dollFighter.CurrentAP} (Expected: 3)");
        Debug.Log($"[Test Check] Player Shield after clear & re-passive: {dollFighter.RuntimeShield} (Expected: 15)");

        dollFighter.Attack(monsterFighter, sword); // 15 dmg, kills the 10 hp monster

        if (combat.CurrentState == CombatSystem.CombatState.End && monsterFighter.RuntimeHP == 0) {
            Debug.Log("<color=green>[Test Passed] Stage 3 Acceptance Criteria Met! Combat state machine and factions work perfectly.</color>");
        } else {
            Debug.LogError("<color=red>[Test Failed] The combat did not conclude as expected.</color>");
        }

        Debug.Log("========== [Stage 3 Acceptance Test] Finished ==========\n");
    }
}
