using System.Collections;
using UnityEngine;

// The prologue's "start Zero" comic page puts the close-up panel's motion clip on screen: the video shows its first
// frame, the next line on the same page keeps it, and the next page stops it. Whether it then plays through is checked
// in the packaged build (P3BuildSmoke): video in the Editor's test runner stays on frame 0 when the Editor is not
// focused.
public static class MotionCGPlayScenario {
    private const string PageVisualID = "vfx_no0_core_start";
    private const string PanelVisualID = "cg_t0_01a_p06_panel01_no0_close";

    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        if (!VisualAssetService.TryGetVideo(PanelVisualID + UIMotionCG.MotionSuffix, out UnityEngine.Video.VideoClip clip)) {
            Debug.LogError($"[MotionCG] {PanelVisualID}{UIMotionCG.MotionSuffix} is not registered.");
            yield break;
        }

        // The prologue makes its overlay on demand; outside the prologue the scenario makes one the same way.
        P3DialogueOverlayController overlay = Object.FindObjectOfType<P3DialogueOverlayController>(true);
        bool madeOverlay = false;
        if (overlay == null) {
            foreach (Canvas canvas in Object.FindObjectsOfType<Canvas>()) {
                if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                    overlay = P3DialogueOverlayController.CreateUnder(canvas);
                    madeOverlay = true;
                    break;
                }
            }
        }

        if (overlay == null) {
            Debug.LogError("[MotionCG] No dialogue overlay and no screen canvas to make one under.");
            yield break;
        }

        overlay.PresentLine(Line(PageVisualID, "第一句"));
        UIMotionCG motion = null;
        yield return PlayScenarioSupport.Until(() => (motion = FindPlaying(clip)) != null && motion.FrameShown, 5f, "the close-up panel never showed a video frame");
        if (motion == null || !motion.FrameShown) {
            yield break;
        }

        overlay.PresentLine(Line(PageVisualID, "第二句"));
        yield return new WaitForSecondsRealtime(0.3f);
        if (FindPlaying(clip) != motion || !motion.FrameShown) {
            Debug.LogError("[MotionCG] The next line on the same page replaced or stopped the clip.");
        }

        overlay.PresentLine(Line("cg_t0_01a_find_no0", "下一页"));
        yield return null;
        if (motion != null && motion.FrameShown) {
            Debug.LogError("[MotionCG] The next page still shows the close-up's video.");
        }

        Debug.Log($"[MotionCG] {clip.name} showed on the comic page, survived the next line and stopped on the next page.");
        overlay.Hide();
        if (madeOverlay) {
            Object.Destroy(overlay.gameObject);
        }
    }

    private static NarrativeOverlayPayload Line(string visualID, string text) {
        return new NarrativeOverlayPayload {
            SpeakerName = "零号",
            Text = text,
            VisualRequest = new NarrativeVisualRequest { VisualID = visualID }
        };
    }

    private static UIMotionCG FindPlaying(UnityEngine.Video.VideoClip clip) {
        foreach (UIMotionCG candidate in Object.FindObjectsOfType<UIMotionCG>()) {
            if (candidate.Clip == clip) {
                return candidate;
            }
        }

        return null;
    }
}
