using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared steps for the Play Mode scenarios (run by Assets/Tests/PlayMode/P3PlayTests). The game boots itself
// (PlayModeRuntimeEntrySceneBootstrap loads SampleScene), so a scenario waits for it and walks on with P3DevTools.
public static class PlayScenarioSupport {
    public const string MainScene = "SampleScene";

    // To the workshop from wherever the previous scenario left the game: title, prologue, a dive or a fight.
    public static IEnumerator BootToWorkshop() {
        if (SceneManager.GetActiveScene().name != MainScene) {
            SceneManager.LoadScene(MainScene);
        }

        yield return Until(() => GameRoot.Core?.CurrentPlayer != null && GameFlowController.Instance != null, 30f, "the game did not boot");
        yield return new WaitForSecondsRealtime(0.5f);
        if (TitleScreen.IsShowing) {
            P3DevTools.Click(TitleScreen.NewGameLabel);
            yield return new WaitForSecondsRealtime(1f);
        }

        if (UnityEngine.Object.FindObjectOfType<PrologueFirstDiveController>() != null) {
            P3DevTools.SkipPrologue();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        P3DevTools.GoTo("workshop");
        yield return null;
    }

    // Waits (real time) until the condition holds; logs an error and stops the scenario's wait on timeout.
    public static IEnumerator Until(Func<bool> condition, float timeoutSeconds, string failure) {
        float start = Time.realtimeSinceStartup;
        while (!condition()) {
            if (Time.realtimeSinceStartup - start > timeoutSeconds) {
                Debug.LogError($"[PlayScenario] Timed out after {timeoutSeconds}s: {failure}\n{P3DevTools.Describe()}");
                yield break;
            }

            yield return null;
        }
    }
}
