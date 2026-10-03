using UnityEngine;

// Regression for the invisible prologue (found 2026-10-04): the prologue controller calls EnsureBuilt() again on
// its first frame, after the first line is already shown. That second call used to reset the overlay to invisible.
public static class PrologueOverlayStaysVisibleTest {
    public static void Run() {
        Debug.Log("=== Running Prologue Overlay Stays Visible Test ===");
        GameObject canvasObj = new GameObject("PrologueOverlayStaysVisibleTest_Canvas");
        try {
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            P3DialogueOverlayController overlay = P3DialogueOverlayController.CreateUnder(canvas);
            overlay.PresentLine(new NarrativeOverlayPayload {
                SpeakerName = "主角",
                Text = "又到早上了。"
            });
            bool shownAfterPresent = overlay.IsShowing && overlay.IsInputBlocking;

            overlay.EnsureBuilt();
            bool shownAfterRebuild = overlay.IsShowing && overlay.IsInputBlocking;

            if (shownAfterPresent && shownAfterRebuild) {
                Debug.Log("Prologue Overlay Stays Visible PASSED.");
            } else {
                Debug.LogError($"Prologue Overlay Stays Visible FAILED. AfterPresent={shownAfterPresent}, AfterRebuild={shownAfterRebuild}");
            }
        } finally {
            Object.DestroyImmediate(canvasObj);
        }
    }
}
