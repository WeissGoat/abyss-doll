using System.Collections;
using UnityEngine;

// The presentation clips, as far as the Editor can show them (an unfocused Editor never decodes video frames, so
// playback itself is checked in the build):
// - the hall's Zero is her drawn portrait with its paper puppet, not a video (her new portrait's thin lines came out
//   soft and shimmering in every idle clip, 2026-10-25), so it stays crisp and answers a touch;
// - a boss with an entrance clip opens the fight with it, and it clears on its own, leaving a playable fight.
public static class PresentationClipsPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        yield return new WaitForSecondsRealtime(0.5f);

        UIPortraitPuppet puppet = null;
        foreach (UIPortraitPuppet candidate in Object.FindObjectsOfType<UIPortraitPuppet>()) {
            if (UIPortraitPuppet.On(candidate) != null) {
                puppet = candidate;
            }
        }

        if (puppet == null) {
            Debug.LogError("[PresentationClips] Zero in the hall has no working puppet.\n" + P3DevTools.Describe());
        } else if (puppet.GetComponent<UIMotionPortrait>()?.Clip != null) {
            Debug.LogError("[PresentationClips] Zero in the hall plays a video over her puppet portrait.");
        }

        string fight = P3DevTools.Fight("boss_gatekeeper_mk1");
        yield return PlayScenarioSupport.Until(() => CombatIntroCutscene.Current != null, 3f,
            "the gatekeeper fight did not open with its entrance clip\n" + fight);
        yield return PlayScenarioSupport.Until(() => CombatIntroCutscene.Current == null, 10f,
            "the entrance clip never cleared");

        CombatSystem combat = GameRoot.Core.Combat;
        if (combat.CurrentState != CombatState.PlayerTurn) {
            Debug.LogError($"[PresentationClips] After the entrance clip the fight is in {combat.CurrentState}, not the player turn.");
        }

        Debug.Log($"[PresentationClips] hall puppet {(puppet != null ? puppet.name : "none")}, entrance clip played and cleared.");
    }
}
