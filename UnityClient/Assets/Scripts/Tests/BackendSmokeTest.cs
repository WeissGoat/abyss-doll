using UnityEngine;
using System.Collections.Generic;

public static class BackendSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Backend Smoke Test ===");
        
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        
        if (core.CurrentPlayer == null) {
            Debug.LogError("CurrentPlayer is null! Bootstrap failed.");
            return;
        }

        if (core.CurrentPlayer.ActiveDoll == null) {
            Debug.LogError("ActiveDoll is null! JSON loading failed.");
            return;
        }
        
        var doll = core.CurrentPlayer.ActiveDoll;
        Debug.Log($"Active Doll: {doll.Name} (HP: {doll.Status.HP_Current}/{doll.Status.HP_Max})");
        
        // Assert HP is 100 as we fixed earlier
        if (doll.Status.HP_Current != 100) {
            Debug.LogError($"HP Assert Failed: Expected 100, but was {doll.Status.HP_Current}");
        } else {
            Debug.Log("HP Assertion PASSED.");
        }
        
        if (doll.Status.SAN_Current != 50) {
            Debug.LogError($"SAN Assert Failed: Expected 50, but was {doll.Status.SAN_Current}");
        } else {
            Debug.Log("SAN Assertion PASSED.");
        }

        BackpackGrid runtimeGrid = doll.RuntimeGrid as BackpackGrid;
        if (runtimeGrid == null) {
            Debug.LogError("RuntimeGrid Assert FAILED: Active doll runtime grid was not initialized.");
        } else {
            Debug.Log("RuntimeGrid Initialization PASSED.");
        }

        if (runtimeGrid != null && doll.InitialItems != null && doll.InitialItems.Count > 0) {
            bool loadoutMatches = runtimeGrid.ContainedItems.Count == doll.InitialItems.Count;
            foreach (var initialItem in doll.InitialItems) {
                ItemEntity placedItem = runtimeGrid.GetItemAt(initialItem.X, initialItem.Y);
                if (placedItem == null || placedItem.ConfigID != initialItem.ItemConfigID) {
                    loadoutMatches = false;
                    Debug.LogError($"Initial Loadout Assert FAILED at ({initialItem.X},{initialItem.Y}). Expected {initialItem.ItemConfigID}, got {placedItem?.ConfigID ?? "null"}");
                }
            }

            if (loadoutMatches) {
                Debug.Log("Initial Loadout Placement PASSED.");
            }
        }
        
        Debug.Log("=== Backend Smoke Test Finished ===");
    }
}

public static class ItemUseSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Item Use Smoke Test v2 ===");

        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        TestCombatConsumableUse();
        TestSafeRoomConsumableUse();
        TestConsumableGuardrails();
        TestWeaponTargetSelection();

        Debug.Log("=== Item Use Smoke Test Finished ===");
    }

    private static void TestCombatConsumableUse() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity repairKit = grid?.ContainedItems.Find(item => item.ConfigID == "con_repair_kit");

        if (grid == null || repairKit == null) {
            Debug.LogError("Combat Consumable Bootstrap FAILED: missing runtime grid or repair kit.");
            return;
        }

        doll.Status.HP_Current = 40;
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });

        DollFighter playerFighter = core.Combat.PlayerFaction.Fighters[0] as DollFighter;
        int beforeCount = grid.ContainedItems.Count;

        bool used = ItemUseService.TryUseItem(repairKit, out string failureReason);
        if (!used) {
            Debug.LogError($"Combat Consumable Use FAILED: {failureReason}");
            return;
        }

        if (playerFighter != null &&
            playerFighter.RuntimeHP == 70 &&
            doll.Status.HP_Current == 70 &&
            playerFighter.CurrentAP == 2 &&
            grid.ContainedItems.Count == beforeCount - 1) {
            Debug.Log("Combat Consumable Use PASSED.");
        } else {
            Debug.LogError($"Combat Consumable Use FAILED. HP={playerFighter?.RuntimeHP}, DollHP={doll.Status.HP_Current}, AP={playerFighter?.CurrentAP}, Backpack={grid.ContainedItems.Count}");
        }
    }

    private static void TestSafeRoomConsumableUse() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity sedative = grid?.ContainedItems.Find(item => item.ConfigID == "con_cheap_sedative");

        if (grid == null || sedative == null) {
            Debug.LogError("SafeRoom Consumable Bootstrap FAILED: missing runtime grid or sedative.");
            return;
        }

        doll.Status.SAN_Current = 10;
        SafeRoomNode safeRoomNode = new SafeRoomNode { NodeID = "item_use_safe_room" };
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = safeRoomNode,
            CurrentNode = safeRoomNode
        };

        int beforeCount = grid.ContainedItems.Count;
        bool used = ItemUseService.TryUseItem(sedative, out string failureReason);
        if (!used) {
            Debug.LogError($"SafeRoom Consumable Use FAILED: {failureReason}");
            return;
        }

        if (doll.Status.SAN_Current == 30 && grid.ContainedItems.Count == beforeCount - 1) {
            Debug.Log("SafeRoom Consumable Use PASSED.");
        } else {
            Debug.LogError($"SafeRoom Consumable Use FAILED. SAN={doll.Status.SAN_Current}, Backpack={grid.ContainedItems.Count}");
        }
    }

    private static void TestConsumableGuardrails() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity repairKit = grid?.ContainedItems.Find(item => item.ConfigID == "con_repair_kit");
        ItemEntity sedative = grid?.ContainedItems.Find(item => item.ConfigID == "con_cheap_sedative");

        SafeRoomNode safeRoomNode = new SafeRoomNode { NodeID = "item_use_guardrail_safe_room" };
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = safeRoomNode,
            CurrentNode = safeRoomNode
        };

        string healFailure;
        bool healRejected = !ItemUseService.TryUseItem(repairKit, out healFailure) && healFailure.Contains("HP");

        string sanFailure;
        bool sanRejected = !ItemUseService.TryUseItem(sedative, out sanFailure) && sanFailure.Contains("SAN");

        if (healRejected && sanRejected) {
            Debug.Log("Consumable Guardrail PASSED.");
        } else {
            Debug.LogError($"Consumable Guardrail FAILED. HealRejected={healRejected}, SanRejected={sanRejected}, HealFailure={healFailure}, SanFailure={sanFailure}");
        }
    }

    private static void TestWeaponTargetSelection() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity weapon = grid?.ContainedItems.Find(item => item.ConfigID == "gear_tactical_blade");

        if (grid == null || weapon == null) {
            Debug.LogError("Weapon Target Selection Bootstrap FAILED: missing runtime grid or weapon.");
            return;
        }

        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug", "mob_scavenger_bug" });
        FighterEntity firstEnemy = core.Combat.EnemyFaction.Fighters[0];
        FighterEntity secondEnemy = core.Combat.EnemyFaction.Fighters[1];

        bool queued = ItemUseService.TryUseItem(weapon, out string queueFailure);
        bool confirmed = ItemUseService.TryConfirmPendingTarget(secondEnemy, out string confirmFailure);

        if (!queued || !confirmed) {
            Debug.LogError($"Weapon Target Selection FAILED. Queued={queued}, Confirmed={confirmed}, QueueFailure={queueFailure}, ConfirmFailure={confirmFailure}");
            return;
        }

        if (firstEnemy.RuntimeHP == firstEnemy.RuntimeMaxHP &&
            secondEnemy.RuntimeHP < secondEnemy.RuntimeMaxHP &&
            !ItemUseService.HasPendingEnemyTargetSelection) {
            Debug.Log("Weapon Target Selection PASSED.");
        } else {
            Debug.LogError($"Weapon Target Selection FAILED. FirstEnemyHP={firstEnemy.RuntimeHP}, SecondEnemyHP={secondEnemy.RuntimeHP}, Pending={ItemUseService.HasPendingEnemyTargetSelection}");
        }
    }
}
