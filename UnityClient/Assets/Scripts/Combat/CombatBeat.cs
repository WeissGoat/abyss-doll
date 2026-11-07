// One visible moment of a fight, published through the VisualQueue so the screen can play it before the next one:
// a fighter strikes, a fighter is hit, gains shield, heals, or an enemy tampers with an item. It carries the fighter
// objects (two monsters can share a name) and the numbers the screen shows; it changes nothing itself.
public enum CombatBeatKind {
    Strike,
    Hit,
    Shield,
    Heal,
    Interfere,
    // A trigger chain passes from one module to the next (SourceItemID -> TargetItemID).
    ChainPulse
}

// Pose names a fighter can show. Each is an optional sprite "<pose set>_<pose>" in the VisualAssetRegistry
// (Zero's set is "doll_zero_combat", a monster's is its CombatVisualID); a missing pose keeps the base image.
public static class CombatPose {
    public const string Idle = "idle";
    public const string Windup = "windup";
    public const string Attack = "attack";
    // Bare hands: Zero throws a punch instead of pushing the weapon out of her palm.
    public const string Punch = "punch";
    public const string Guard = "guard";
    public const string Hurt = "hurt";
    public const string Item = "item";
    public const string Kneel = "kneel";
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
    // The item the strike was made with (its icon flies across the stage), and a pose the moment asks for, e.g.
    // CombatPose.Kneel when Zero holds on at 1 HP; empty means the screen picks the pose from the beat kind.
    public string SourceVisualID;
    // The backpack item behind the beat (it lights up where it lies), the next module of a chain, and whether the item
    // shoots (a bolt leaves Zero) rather than cuts (a slash leaves Zero).
    public string SourceItemID;
    public string TargetItemID;
    public bool Ranged;
    public string Pose;
    // How long the queue holds after this beat; 0 takes the kind's default (a monster's strike holds until its move
    // lands, CombatAttackMotion.ImpactSeconds).
    public float Hold;

    // How long the queue holds after each kind, so enemies act one after another instead of all at once.
    public static float HoldSeconds(CombatBeatKind kind) {
        switch (kind) {
            case CombatBeatKind.Strike:
                return 0.28f;
            case CombatBeatKind.Hit:
                return 0.42f;
            case CombatBeatKind.Interfere:
                return 0.7f;
            case CombatBeatKind.ChainPulse:
                return 0.14f;
            default:
                return 0.2f;
        }
    }
}
