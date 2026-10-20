using System.Collections;
using System.IO;
using UnityEngine;

// Records the game's own frames (not the desktop) as numbered JPEGs, for turning a moment of play into a short
// video with ffmpeg: P3DevTools.Record(folder, seconds, fps), then
//   ffmpeg -framerate <fps> -i <folder>/frame_%05d.jpg -pix_fmt yuv420p out.mp4
// Frames are taken at the end of the frame on real time, so hitstop and pauses are recorded as the player sees them.
public class P3FrameRecorder : MonoBehaviour {
    private static P3FrameRecorder _instance;

    public static bool Recording => _instance != null && _instance._running;

    private bool _running;

    public static string Begin(string folder, float seconds, int fps) {
        if (Recording) {
            return "Already recording.";
        }

        if (_instance == null) {
            GameObject host = new GameObject("P3FrameRecorder");
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<P3FrameRecorder>();
        }

        Directory.CreateDirectory(folder);
        foreach (string old in Directory.GetFiles(folder, "frame_*.jpg")) {
            File.Delete(old);
        }

        _instance.StartCoroutine(_instance.Run(folder, Mathf.Clamp(seconds, 0.5f, 60f), Mathf.Clamp(fps, 5, 60)));
        return $"Recording {seconds:0.#}s at {fps} fps into {folder}.";
    }

    private IEnumerator Run(string folder, float seconds, int fps) {
        _running = true;
        float start = Time.realtimeSinceStartup;
        float next = start;
        int index = 0;
        while (Time.realtimeSinceStartup - start < seconds) {
            yield return new WaitForEndOfFrame();
            float now = Time.realtimeSinceStartup;
            if (now < next) {
                continue;
            }

            // Late frames are written more than once, so the clip keeps real-time length.
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
            byte[] jpg = frame.EncodeToJPG(88);
            Destroy(frame);
            while (next <= now) {
                File.WriteAllBytes(Path.Combine(folder, $"frame_{index:00000}.jpg"), jpg);
                index++;
                next += 1f / fps;
            }
        }

        _running = false;
    }
}
