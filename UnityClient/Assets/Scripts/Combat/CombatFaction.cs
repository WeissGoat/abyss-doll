using System.Collections.Generic;
using UnityEngine;

public enum FactionType { Player, Enemy, Neutral }

public class CombatFaction {
    public FactionType Type;
    public List<FighterEntity> Fighters = new List<FighterEntity>();
    
    public bool IsWipedOut() {
        return Fighters.TrueForAll(f => f.RuntimeHP <= 0);
    }
    
    public void Cleanup() {
        foreach(var f in Fighters) {
            f.Cleanup();
        }
    }
}
