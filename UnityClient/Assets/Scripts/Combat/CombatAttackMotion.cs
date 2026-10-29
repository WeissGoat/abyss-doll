using UnityEngine;

// How a monster moves when it strikes (its config's AttackMotion): the default short lunge, or a bigger move that
// fits its body. Each has a wind-up (the monster shows its windup pose and draws back), the move itself, and the way
// back. The queue holds the strike until the impact, so the hit lands when the monster arrives (or its shot does).
// Offsets are in stage pixels towards the target (+x) and up (+y); scale is (width, height) on top of the image.
public static class CombatAttackMotion {
    public const string Lunge = "Lunge";
    // Draws back, dashes far across the stage at the target, holds there, walks back.
    public const string Charge = "Charge";
    // Crouches, jumps at the target in an arc, lands squashed, slides back.
    public const string Leap = "Leap";
    // Leans back, fires (recoil), a shot flies to the target.
    public const string Shoot = "Shoot";
    // Draws far back and swings through a long step.
    public const string Swing = "Swing";

    public const float ShotSeconds = 0.22f;
    private const float DefaultHold = 0.28f;

    public static string Resolve(FighterEntity fighter) {
        string style = fighter is MonsterFighter monster ? monster.DataRef?.AttackMotion : null;
        return Normalize(style);
    }

    public static string Normalize(string style) {
        switch (style) {
            case Charge:
            case Leap:
            case Shoot:
            case Swing:
                return style;
            default:
                return Lunge;
        }
    }

    // How long the windup pose shows before the move.
    public static float WindupSeconds(string style) {
        switch (style) {
            case Charge:
                return 0.3f;
            case Leap:
                return 0.26f;
            case Shoot:
                return 0.34f;
            case Swing:
                return 0.28f;
            default:
                return 0.1f;
        }
    }

    // From the strike to the impact: how long the queue holds before the hit beat.
    public static float ImpactSeconds(string style) {
        switch (style) {
            case Charge:
                return 0.44f;
            case Leap:
                return 0.54f;
            case Shoot:
                return WindupSeconds(Shoot) + ShotSeconds;
            case Swing:
                return 0.42f;
            default:
                return DefaultHold;
        }
    }

    // The whole move, back in place at the end.
    public static float TotalSeconds(string style) {
        switch (style) {
            case Charge:
                return 0.92f;
            case Leap:
                return 1.0f;
            case Shoot:
                return 0.8f;
            case Swing:
                return 0.8f;
            default:
                return 0.26f;
        }
    }

    // Where the striker is t seconds into its move (zero outside it). Lunge is the director's own curve.
    public static Vector2 Offset(string style, float t, out Vector2 scale) {
        scale = Vector2.one;
        if (t < 0f || t >= TotalSeconds(style)) {
            return Vector2.zero;
        }

        float w = WindupSeconds(style);
        switch (style) {
            case Charge: {
                const float back = 36f;
                const float reach = 300f;
                const float dash = 0.14f;
                const float hold = 0.12f;
                if (t < w) {
                    return new Vector2(-back * Mathf.SmoothStep(0f, 1f, t / w), 0f);
                }

                if (t < w + dash) {
                    float p = (t - w) / dash;
                    scale = new Vector2(1.06f, 0.96f);
                    return new Vector2(Mathf.Lerp(-back, reach, p * p), 0f);
                }

                if (t < w + dash + hold) {
                    return new Vector2(reach, 0f);
                }

                float r = (t - w - dash - hold) / (TotalSeconds(style) - w - dash - hold);
                return new Vector2(Mathf.Lerp(reach, 0f, Mathf.SmoothStep(0f, 1f, r)), 0f);
            }
            case Leap: {
                const float reach = 220f;
                const float height = 120f;
                const float air = 0.28f;
                const float land = 0.12f;
                if (t < w) {
                    float c = Mathf.SmoothStep(0f, 1f, t / w);
                    scale = new Vector2(1f + 0.07f * c, 1f - 0.11f * c);
                    return new Vector2(-14f * c, 0f);
                }

                if (t < w + air) {
                    float p = (t - w) / air;
                    scale = new Vector2(0.95f, 1.07f);
                    return new Vector2(Mathf.Lerp(-14f, reach, p), height * 4f * p * (1f - p));
                }

                if (t < w + air + land) {
                    float s = 1f - (t - w - air) / land;
                    scale = new Vector2(1f + 0.08f * s, 1f - 0.12f * s);
                    return new Vector2(reach, 0f);
                }

                float r = (t - w - air - land) / (TotalSeconds(style) - w - air - land);
                return new Vector2(Mathf.Lerp(reach, 0f, Mathf.SmoothStep(0f, 1f, r)), 0f);
            }
            case Shoot: {
                const float lean = 16f;
                const float kick = 42f;
                if (t < w) {
                    return new Vector2(-lean * Mathf.SmoothStep(0f, 1f, t / w), 0f);
                }

                float k = (t - w) / (TotalSeconds(style) - w);
                float recoil = k < 0.12f ? k / 0.12f : Mathf.SmoothStep(1f, 0f, (k - 0.12f) / 0.88f);
                scale = Vector2.one * (1f + 0.04f * recoil);
                return new Vector2(-(lean * (1f - k) + kick * recoil), 0f);
            }
            case Swing: {
                const float back = 44f;
                const float reach = 140f;
                const float step = 0.12f;
                const float hold = 0.1f;
                if (t < w) {
                    return new Vector2(-back * Mathf.SmoothStep(0f, 1f, t / w), 0f);
                }

                if (t < w + step) {
                    float p = (t - w) / step;
                    scale = Vector2.one * 1.06f;
                    return new Vector2(Mathf.Lerp(-back, reach, Mathf.SmoothStep(0f, 1f, p)), 0f);
                }

                if (t < w + step + hold) {
                    return new Vector2(reach, 0f);
                }

                float r = (t - w - step - hold) / (TotalSeconds(style) - w - step - hold);
                return new Vector2(Mathf.Lerp(reach, 0f, Mathf.SmoothStep(0f, 1f, r)), 0f);
            }
            default:
                return Vector2.zero;
        }
    }
}
