using UnityEngine;

// T0 B segment steps that are a short scene with a choice rather than an outcome table. Entering one asks the UI to
// play it (DungeonEventBus.OnPrologueBeatEntered); the player's choice comes back through the methods below.
public abstract class PrologueBeatNode : NodeBase {
    public abstract string DisplayLabel { get; }

    public override void OnEnterNode() {
        Debug.Log($"[Dungeon] Entered prologue beat {NodeID}.");
        DungeonEventBus.PublishPrologueBeatEntered(this);
    }
}

// 下面的声音: follow it, or let her listen first (SAN cost; the memory shard becomes a bring-back option).
public class PrologueVoiceNode : PrologueBeatNode {
    public override string DisplayLabel => "声音";

    public void Choose(bool listen) {
        if (listen) {
            PrologueShallowRun.ListenToVoice(GameRoot.Core?.CurrentPlayer);
        }

        DungeonEventBus.PublishNodeSettlementCompleted();
    }
}

// 旧矿车: one thing to bring back, then home.
public class PrologueBringBackNode : PrologueBeatNode {
    public override string DisplayLabel => "旧矿车";

    // Home either way; an unknown choice just brings nothing back.
    public bool Choose(string configID) {
        bool brought = PrologueShallowRun.BringBack(GameRoot.Core?.CurrentPlayer, configID) != null;
        DungeonEventBus.PublishDungeonEvacuated();
        return brought;
    }
}
