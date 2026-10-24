using System;
using System.Collections.Generic;
using UnityEngine;

// The end of the Demo, first beat (DemoEndingService): at the stairs after the deepest boss, its comic page with the
// lines of the scene demo_end_oracle, one at a time over the dialogue overlay. Shows state and forwards 继续 only.
public class DemoEndingComic : MonoBehaviour {
    private const string OverlayName = "P3DialogueOverlay_Runtime";

    private P3DialogueOverlayController _overlay;
    private List<SceneLine> _lines;
    private int _index;
    private Action _onDone;

    public static DemoEndingComic Current { get; private set; }
    public static bool IsPlaying => Current != null && Current._overlay != null && Current._overlay.IsShowing;

    // Plays the page once if it is due; false (and onDone not called) when it is not.
    public static bool PlayIfDue(Action onDone) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (!DemoEndingService.ComicDue(player) || ConfigManager.Scenes == null
            || !ConfigManager.Scenes.TryGetValue(DemoEndingService.ComicSceneID, out SceneConfig scene) || scene.Lines == null || scene.Lines.Count == 0) {
            return false;
        }

        Canvas canvas = MainCanvas.Find();
        if (canvas == null) {
            return false;
        }

        Transform existing = canvas.transform.Find(OverlayName);
        P3DialogueOverlayController overlay = existing != null ? existing.GetComponent<P3DialogueOverlayController>() : P3DialogueOverlayController.CreateUnder(canvas);
        if (overlay == null) {
            return false;
        }

        DemoEndingService.MarkComicShown(player);
        DemoEndingComic comic = overlay.GetComponent<DemoEndingComic>();
        if (comic == null) {
            comic = overlay.gameObject.AddComponent<DemoEndingComic>();
        }

        comic.Begin(overlay, scene.Lines, onDone);
        return true;
    }

    // DevTools: the next line, or the end of the page.
    public void Advance() {
        _index++;
        if (_index < _lines.Count) {
            Present();
            return;
        }

        _overlay.ContinueRequested -= Advance;
        _overlay.Hide();
        if (Current == this) {
            Current = null;
        }

        Action done = _onDone;
        _onDone = null;
        done?.Invoke();
    }

    private void Begin(P3DialogueOverlayController overlay, List<SceneLine> lines, Action onDone) {
        _overlay = overlay;
        _lines = lines;
        _index = 0;
        _onDone = onDone;
        Current = this;
        _overlay.ContinueRequested -= Advance;
        _overlay.ContinueRequested += Advance;
        Present();
    }

    private void Present() {
        SceneLine line = _lines[_index];
        _overlay.PresentLine(new NarrativeOverlayPayload {
            SpeakerName = line.Speaker,
            Text = line.Text,
            VisualRequest = new NarrativeVisualRequest { VisualID = DemoEndingService.ComicVisualID }
        });
    }

    private void OnDestroy() {
        if (_overlay != null) {
            _overlay.ContinueRequested -= Advance;
        }

        if (Current == this) {
            Current = null;
        }
    }
}
