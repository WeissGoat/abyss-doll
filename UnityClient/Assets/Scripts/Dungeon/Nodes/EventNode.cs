using System.Collections.Generic;

// An event node. With a scene it plays it and waits for the player's choice (GDD_02 §3.3: an event is a choice with
// a price, not a roll); each choice carries its own outcome and reward. Without one it resolves its configured
// outcome at once.
public class EventNode : DungeonOutcomeNode {
    protected override string DefaultTitle => "未知事件";
    protected override string DefaultDescription => "深渊中的异常回应了这次接触。";
    protected override string RewardSourceType => "EventNode";

    public string SceneID { get; private set; }

    // The scene this event plays; a subclass can build its own (RelicNode).
    public virtual SceneConfig Scene => !string.IsNullOrEmpty(SceneID) && ConfigManager.Scenes.TryGetValue(SceneID, out SceneConfig scene) ? scene : null;

    public bool HasChoices => Scene?.Choices != null && Scene.Choices.Count > 0;

    public List<SceneChoice> Choices => HasChoices ? Scene.Choices : new List<SceneChoice>();

    public override void Init(NodePoolEntry entry) {
        base.Init(entry);
        SceneID = entry?.SceneID;
    }

    public override void OnEnterNode() {
        if (!HasChoices) {
            base.OnEnterNode();
            return;
        }

        DungeonEventBus.PublishChoiceEventEntered(this);
    }

    // An unknown ID takes the last choice, which is always the way out (walk on).
    public virtual void Choose(string choiceID) {
        SceneChoice choice = FindChoice(choiceID);
        Resolve(choice?.Outcome, choice?.RewardID, choice?.Result);
    }

    protected SceneChoice FindChoice(string choiceID) {
        List<SceneChoice> choices = Choices;
        return choices.Find(candidate => candidate.ID == choiceID) ?? (choices.Count > 0 ? choices[choices.Count - 1] : null);
    }
}
