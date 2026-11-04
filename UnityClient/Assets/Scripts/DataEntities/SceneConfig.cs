using System.Collections.Generic;

// A short scene (配置表(JSON)/Scenes): title, lines shown one at a time, then optional choices. Choice IDs are read by
// the code that plays the scene (PrologueShallowRun / PrologueReturnHome).
public class SceneConfig {
    public string SceneID;
    public string Title;
    public List<SceneLine> Lines = new List<SceneLine>();
    public List<SceneChoice> Choices = new List<SceneChoice>();
}

public class SceneLine {
    public string Speaker;
    public string Text;
    // Zero's face on her large portrait while she says it (ZeroFace); empty is neutral. Only for her lines.
    public string Face;
}

public class SceneChoice {
    public string ID;
    public string Label;
    public string Hint;
    // In a dungeon event (NodePoolEntry.SceneID): what choosing it does, as a node outcome, the reward it rolls and
    // the line the result card opens with.
    public List<DungeonNodeOutcomeConfig> Outcome = new List<DungeonNodeOutcomeConfig>();
    public string RewardID;
    public string Result;
}
