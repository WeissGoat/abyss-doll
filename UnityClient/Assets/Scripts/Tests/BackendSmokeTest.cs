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
        
        Debug.Log("=== Backend Smoke Test Finished ===");
    }
}