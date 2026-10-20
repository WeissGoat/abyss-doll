using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The two kinds of clips added for the presentation samples, as far as the Editor can show them (an unfocused Editor
// never decodes video frames, so playback itself is checked in the build):
// - the hall's Zero gets her looping idle clip, and while no frame has arrived the still stays visible;
// - a boss with an entrance clip opens the fight with it, and it clears on its own, leaving a playable fight.
public static class PresentationClipsPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        yield return new WaitForSecondsRealtime(0.5f);

        UIMotionPortrait portrait = null;
        foreach (UIMotionPortrait candidate in Object.FindObjectsOfType<UIMotionPortrait>()) {
            if (candidate.isActiveAndEnabled && candidate.Clip != null) {
                portrait = candidate;
            }
        }

        if (portrait == null) {
            Debug.LogError("[PresentationClips] Zero in the hall has no idle clip.\n" + P3DevTools.Describe());
        } else if (!portrait.FrameShown && portrait.GetComponent<Image>().canvasRenderer.GetAlpha() < 0.99f) {
            Debug.LogError("[PresentationClips] The hall portrait is hidden although no video frame is showing.");
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

        Debug.Log($"[PresentationClips] idle clip {(portrait != null ? portrait.Clip.name : "none")}, entrance clip played and cleared.");
    }
}
