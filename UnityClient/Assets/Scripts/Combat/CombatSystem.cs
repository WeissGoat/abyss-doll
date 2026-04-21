using System.Collections.Generic;
using UnityEngine;

public enum CombatState { PlayerTurn, EnemyTurn, End }

public class CombatSystem {
    public CombatFaction PlayerFaction;
    public CombatFaction EnemyFaction;
    
    public CombatState CurrentState;

    public void StartCombat(List<string> monsterIDs) {
        Debug.Log("\n[CombatSystem] Initiating Combat!");
        
        // 1. Init Player Faction
        PlayerFaction = new CombatFaction { Type = FactionType.Player };
        PlayerFaction.Fighters.Add(new DollFighter(GameRoot.Core.CurrentPlayer.ActiveDoll, PlayerFaction));
        
        // 2. Init Enemy Faction
        EnemyFaction = new CombatFaction { Type = FactionType.Enemy };
        foreach(var id in monsterIDs) {
            var template = ConfigManager.Monsters.ContainsKey(id) ? ConfigManager.Monsters[id] : null;
            if (template != null) {
                EnemyFaction.Fighters.Add(new MonsterFighter(template, EnemyFaction));
            } else {
                Debug.LogError($"[CombatSystem] Unknown Monster ID: {id}");
            }
        }
        
        Debug.Log($"[CombatSystem] Combat Started! Player vs {EnemyFaction.Fighters.Count} Monsters.");
        StartPlayerTurn();
    }
    
    public void StartPlayerTurn() {
        CurrentState = CombatState.PlayerTurn;
        CombatEventBus.Publish(CombatEventType.OnTurnStart, PlayerFaction);
    }
    
    public void EndPlayerTurn() {
        if(CurrentState != CombatState.PlayerTurn) return;
        
        Debug.Log("[CombatSystem] Player ends turn.");
        CombatEventBus.Publish(CombatEventType.OnTurnEnd, PlayerFaction);
        
        if (EnemyFaction.IsWipedOut()) { 
            HandleVictory(); 
            return; 
        }
        
        StartEnemyTurn();
    }
    
    public void StartEnemyTurn() {
        CurrentState = CombatState.EnemyTurn;
        CombatEventBus.Publish(CombatEventType.OnTurnStart, EnemyFaction);
        
        // Sequential Monster AI
        foreach(var f in EnemyFaction.Fighters) {
            MonsterFighter enemy = f as MonsterFighter;
            if(enemy.RuntimeHP > 0 && !PlayerFaction.IsWipedOut()) {
                // Focus the first alive player entity
                var target = PlayerFaction.Fighters.Find(fighter => fighter.RuntimeHP > 0);
                if (target != null) {
                    for (int i = 0; i < enemy.DataRef.AttacksPerTurn; i++) {
                         enemy.Attack(target);
                    }
                }
            }
        }
        
        CombatEventBus.Publish(CombatEventType.OnTurnEnd, EnemyFaction);
        
        if(!PlayerFaction.IsWipedOut()) {
            StartPlayerTurn();
        } else {
            HandleDefeat();
        }
    }
    
    private void HandleVictory() {
        CurrentState = CombatState.End;
        Debug.Log("<color=green>[CombatSystem] Victory! All enemies defeated.</color>");
        
        // Sync HP back
        foreach(var f in PlayerFaction.Fighters) {
            if (f is DollFighter df) {
                df.SyncDataBack();
            }
        }
        PlayerFaction.Cleanup();
        EnemyFaction.Cleanup();
        // In reality, this would transition back to the DungeonManager or Loot UI
    }
    
    private void HandleDefeat() {
        CurrentState = CombatState.End;
        Debug.Log("<color=red>[CombatSystem] Defeat! All player entities wiped out.</color>");
        PlayerFaction.Cleanup();
        EnemyFaction.Cleanup();
        // Triggers the defeat loop (losing items, sanity penalty)
    }
}
