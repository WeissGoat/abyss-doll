using System;

public class CoreBackend {
    public PlayerProfile CurrentPlayer;
    // public WorkshopSystem Workshop; // To be implemented in later stages
    // public CombatSystem Combat;     // To be implemented in later stages
    
    public void InitAllSystems() {
        // 1. Load all configurations from JSON
        ConfigManager.LoadAllConfigs();
        
        // 2. Initialize Player Profile
        CurrentPlayer = new PlayerProfile();
        CurrentPlayer.UID = Guid.NewGuid().ToString();
        CurrentPlayer.Money = 0;
        
        // As a test, let's give the player the prototype doll
        if (ConfigManager.Dolls.TryGetValue("doll_proto_0", out var templateDoll)) {
            // Deep copy the template (in a real scenario, use a proper cloner)
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(templateDoll);
            CurrentPlayer.ActiveDoll = Newtonsoft.Json.JsonConvert.DeserializeObject<DollEntity>(json);
            CurrentPlayer.ActiveDollID = CurrentPlayer.ActiveDoll.DollID;
        }
        
        // Initialize other systems here in the future...
    }
    
    public void Tick(float deltaTime) {
        // Dispatch engine tick to systems that need it
        // Combat?.Tick(deltaTime);
    }
}
