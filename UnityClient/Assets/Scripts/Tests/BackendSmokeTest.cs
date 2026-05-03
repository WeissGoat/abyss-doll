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
