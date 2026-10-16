using System.Collections;
using UnityEngine;

// Zero's portrait in the workshop sways: the sprite helper finds her rig, the hair bends a visible few pixels, and
// the head, hands and feet carry no weight (they must not wobble).
public static class PortraitSwayPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        yield return new WaitForSecondsRealtime(0.5f);

        UIPortraitSway sway = null;
        foreach (UIPortraitSway candidate in Object.FindObjectsOfType<UIPortraitSway>()) {
            if (candidate.isActiveAndEnabled && candidate.HasRig) {
                sway = candidate;
            }
        }

        if (sway == null) {
            Debug.LogError("[PortraitSway] No portrait with a sway rig is on the workshop screen.\n" + P3DevTools.Describe());
            yield break;
        }

        float most = 0f;
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < UIPortraitSway.Period * 0.6f) {
            most = Mathf.Max(most, sway.LastMaxBend);
            yield return null;
        }

        float width = ((RectTransform)sway.transform).rect.width;
        if (most < width * UIPortraitSway.HairAmount * 0.5f) {
            Debug.LogError($"[PortraitSway] The hair barely moved ({most:0.0} px on a {width:0} px portrait).");
        }

        // (u, v) from the bottom-left of doll_proto_0_stand.
        Check(sway, 0.5f, 0.9f, false, "head");
        Check(sway, 0.45f, 0.55f, false, "hands");
        Check(sway, 0.45f, 0.05f, false, "feet");
        Check(sway, 0.3f, 0.5f, true, "left hair");
        Check(sway, 0.74f, 0.5f, true, "right hair");
        Debug.Log($"[PortraitSway] {sway.name}: largest bend {most:0.0} px on a {width:0} px portrait.");
    }

    private static void Check(UIPortraitSway sway, float u, float v, bool shouldMove, string part) {
        sway.SampleWeights(u, v, out float hair, out float skirt);
        bool moves = hair > 0.3f || skirt > 0.3f;
        if (moves != shouldMove) {
            Debug.LogError($"[PortraitSway] {part} at ({u}, {v}) has hair {hair:0.00} / skirt {skirt:0.00}; it should {(shouldMove ? "" : "not ")}move.");
        }
    }
}
