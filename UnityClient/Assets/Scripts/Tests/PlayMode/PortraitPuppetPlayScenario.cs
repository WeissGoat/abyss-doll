using System.Collections;
using UnityEngine;

// Zero's puppet portrait in the studio: a pat on her head dips and tilts it and springs back, counts as one touch,
// and the smudges on her ride along with her; switched off (P), a pat leaves her still.
public static class PortraitPuppetPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        P3DevTools.Soil(3);
        yield return new WaitForSecondsRealtime(0.5f);

        UIPortraitPuppet puppet = null;
        foreach (UIPortraitPuppet candidate in Object.FindObjectsOfType<UIPortraitPuppet>()) {
            // StudioRoom/DollDisplay/DollImage
            if (candidate.isActiveAndEnabled && candidate.HasRig && candidate.transform.parent != null && candidate.transform.parent.parent != null
                && candidate.transform.parent.parent.name == "StudioRoom") {
                puppet = candidate;
            }
        }

        PortraitPatZone zone = puppet != null ? puppet.GetComponentInChildren<PortraitPatZone>() : null;
        UIPuppetPin pin = puppet != null ? puppet.GetComponentInChildren<UIPuppetPin>() : null;
        if (puppet == null || zone == null || pin == null) {
            Debug.LogError($"[PortraitPuppet] Studio puppet {puppet != null}, head zone {zone != null}, pinned smudge {pin != null}.\n" + P3DevTools.Describe());
            yield break;
        }

        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        int touches = player.TouchesToday;
        float dip = 0f;
        float tilt = 0f;
        float shift = 0f;
        float pinMoved = 0f;
        bool done = false;
        zone.StartCoroutine(Wrap(zone.Play(), () => done = true));
        float start = Time.realtimeSinceStartup;
        while (!done && Time.realtimeSinceStartup - start < 5f) {
            dip = Mathf.Max(dip, puppet.HeadDip);
            tilt = Mathf.Max(tilt, Mathf.Abs(puppet.HeadAngle));
            shift = Mathf.Max(shift, puppet.LastHeadShift);
            pinMoved = Mathf.Max(pinMoved, ((RectTransform)pin.transform).anchoredPosition.magnitude);
            yield return null;
        }

        if (dip < UIPortraitPuppet.PatDip * 0.6f || tilt < 3f || shift < 3f) {
            Debug.LogError($"[PortraitPuppet] A pat barely moved her head: dip {dip:0.000}, tilt {tilt:0.0} deg, largest vertex move {shift:0.0}.");
        }

        if (pinMoved < 0.5f) {
            Debug.LogError($"[PortraitPuppet] The smudge stayed put while she moved ({pinMoved:0.00}).");
        }

        if (player.TouchesToday != touches + 1) {
            Debug.LogError($"[PortraitPuppet] The pat should count as one touch ({touches} -> {player.TouchesToday}).");
        }

        yield return new WaitForSecondsRealtime(2.5f);
        if (Mathf.Abs(puppet.HeadDip) > 0.002f) {
            Debug.LogError($"[PortraitPuppet] Her head did not come back up (dip {puppet.HeadDip:0.000}).");
        }

        UIPortraitPuppet.Enabled = false;
        float offDip = 0f;
        IEnumerator pat = puppet.PlayPat(0.6f);
        while (pat.MoveNext()) {
            offDip = Mathf.Max(offDip, puppet.HeadDip);
            yield return pat.Current;
        }

        UIPortraitPuppet.Enabled = true;
        if (offDip > 0.002f) {
            Debug.LogError($"[PortraitPuppet] Switched off, a pat still dipped her head ({offDip:0.000}).");
        }

        Debug.Log($"[PortraitPuppet] Pat: dip {dip:0.000}, tilt {tilt:0.0} deg, largest vertex move {shift:0.0}, smudge moved {pinMoved:0.0}.");
    }

    private static IEnumerator Wrap(IEnumerator inner, System.Action done) {
        yield return inner;
        done();
    }
}
