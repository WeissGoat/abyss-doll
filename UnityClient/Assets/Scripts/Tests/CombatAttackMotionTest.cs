using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

// A monster's strike moves (CombatAttackMotion): each bigger move has reached the target side by the time the hit
// lands, is back in place at the end, and every monster config names a move the game knows.
public static class CombatAttackMotionTest {
    public static void Run() {
        Debug.Log("=== Running Combat Attack Motion Test ===");

        TestMovesLandAndReturn();
        TestConfigsNameKnownMoves();

        Debug.Log("=== Combat Attack Motion Test Finished ===");
    }

    private static void TestMovesLandAndReturn() {
        string[] styles = { CombatAttackMotion.Charge, CombatAttackMotion.Leap, CombatAttackMotion.Swing, CombatAttackMotion.Shoot };
        foreach (string style in styles) {
            float impact = CombatAttackMotion.ImpactSeconds(style);
            float total = CombatAttackMotion.TotalSeconds(style);
            Vector2 atImpact = CombatAttackMotion.Offset(style, impact - 0.001f, out _);
            Vector2 atEnd = CombatAttackMotion.Offset(style, total - 0.001f, out _);
            Vector2 drawnBack = CombatAttackMotion.Offset(style, CombatAttackMotion.WindupSeconds(style) - 0.001f, out _);
            // A shooter stays put and lets its shot travel; the others must have come forward.
            bool landed = style == CombatAttackMotion.Shoot ? impact > CombatAttackMotion.WindupSeconds(style) : atImpact.x > 100f;
            bool back = atEnd.magnitude < 2f && CombatAttackMotion.Offset(style, total + 0.01f, out _) == Vector2.zero;
            bool anticipates = drawnBack.x < 0f;
            bool holds = impact < total && CombatAttackMotion.WindupSeconds(style) < impact;
            if (landed && back && anticipates && holds) {
                Debug.Log($"Combat Attack Motion {style} PASSED.");
            } else {
                Debug.LogError($"Combat Attack Motion {style} FAILED. Landed={landed} ({atImpact}), Back={back} ({atEnd}), Anticipates={anticipates}, Holds={holds}");
            }
        }

        bool lungeDefault = CombatAttackMotion.Normalize(null) == CombatAttackMotion.Lunge && CombatAttackMotion.Normalize("Dance") == CombatAttackMotion.Lunge;
        if (lungeDefault) {
            Debug.Log("Combat Attack Motion Default PASSED.");
        } else {
            Debug.LogError("Combat Attack Motion Default FAILED: an unset or unknown move should be the plain lunge.");
        }
    }

    private static void TestConfigsNameKnownMoves() {
        string folder = Path.Combine(Application.streamingAssetsPath, "Configs", "Monsters");
        int unknown = 0;
        int named = 0;
        foreach (string file in Directory.GetFiles(folder, "*.json")) {
            string motion = (string)JObject.Parse(File.ReadAllText(file))["AttackMotion"];
            if (string.IsNullOrEmpty(motion)) {
                continue;
            }

            named++;
            if (CombatAttackMotion.Normalize(motion) != motion) {
                unknown++;
                Debug.LogError($"Combat Attack Motion Config FAILED: {Path.GetFileName(file)} names '{motion}'.");
            }
        }

        if (unknown == 0 && named > 0) {
            Debug.Log($"Combat Attack Motion Config PASSED ({named} monsters).");
        } else if (named == 0) {
            Debug.LogError("Combat Attack Motion Config FAILED: no monster config names an AttackMotion.");
        }
    }
}
