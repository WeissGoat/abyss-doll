// One visible moment of a fight, published through the VisualQueue so the screen can play it before the next one:
// a fighter strikes, a fighter is hit, gains shield, heals, or an enemy tampers with an item. It carries the fighter
// objects (two monsters can share a name) and the numbers the screen shows; it changes nothing itself.
public enum CombatBeatKind {
    Strike,
    Hit,
    Shield,
    Heal,
    Interfere
}

public class CombatBeat {
    public CombatBeatKind Kind;
    public FighterEntity Actor;
    public FighterEntity Target;
    public int Amount;
    public int HpLoss;
    public int ShieldLoss;
    public bool Defeated;
    public string Label;

    // How long the queue holds after each kind, so enemies act one after another instead of all at once.
    public static float HoldSeconds(CombatBeatKind kind) {
        switch (kind) {
            case CombatBeatKind.Strike:
                return 0.28f;
            case CombatBeatKind.Hit:
                return 0.42f;
            case CombatBeatKind.Interfere:
                return 0.7f;
            default:
                return 0.2f;
        }
    }
}
