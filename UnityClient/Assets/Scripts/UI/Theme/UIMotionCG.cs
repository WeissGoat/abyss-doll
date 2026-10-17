using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// A CG that moves. When a still has a registered "<visualID>_motion" clip (an image-to-video clip made from that still,
// in Approved), a video layer over the image plays it once and then holds its last frame. The still stays underneath,
// so until the first frame is decoded, or without a clip, the player sees the picture as before. Showing the same CG
// again (the next dialogue line on the same page) does not restart it. The overlay switches its panels off and on for
// every line, and a VideoPlayer stops when switched off, so the player lives on its own object and only pauses while
// the panel is off.
[RequireComponent(typeof(Image))]
public class UIMotionCG : MonoBehaviour {
    public const string MotionSuffix = "_motion";

    private VideoPlayer _player;
    private AudioSource _audio;
    private RawImage _screen;
    private RenderTexture _texture;
    private VideoClip _clip;
    private bool _frameShown;
    private bool _finished;

    public VideoClip Clip => _clip;
    public bool FrameShown => _frameShown && _screen != null && _screen.isActiveAndEnabled;
    public long Frame => _player != null ? _player.frame : -1;
    public bool Finished => _finished;

    // Called whenever the overlay shows a CG on this image; a CG without a clip stops any video on it.
    public static UIMotionCG Sync(Image image, string visualID) {
        if (image == null) {
            return null;
        }

        VideoClip clip = null;
        if (!string.IsNullOrEmpty(visualID)) {
            VisualAssetService.TryGetVideo(visualID + MotionSuffix, out clip);
        }

        UIMotionCG motion = image.GetComponent<UIMotionCG>();
        if (clip == null) {
            if (motion != null) {
                motion.Stop();
            }

            return null;
        }

        if (motion == null) {
            motion = image.gameObject.AddComponent<UIMotionCG>();
        }

        motion.Play(clip);
        return motion;
    }

    public void Play(VideoClip clip) {
        if (clip == _clip && _player != null) {
            return;
        }

        EnsureLayer();
        _clip = clip;
        _frameShown = false;
        _finished = false;
        _screen.color = Color.clear;
        if (_texture != null) {
            _texture.Release();
            Destroy(_texture);
        }

        _texture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = clip.name };
        _screen.texture = _texture;
        _player.targetTexture = _texture;
        _player.clip = clip;
        _audio.volume = GameSettingsService.SfxVolume;
        _player.sendFrameReadyEvents = true;
        _player.Play();
    }

    public void Stop() {
        if (_player != null) {
            _player.Stop();
        }

        if (_screen != null) {
            _screen.color = Color.clear;
        }

        _clip = null;
        _frameShown = false;
        _finished = false;
    }

    private void OnEnable() {
        if (_player != null && _clip != null && !_finished && !_player.isPlaying) {
            _player.Play();
        }
    }

    private void OnDisable() {
        if (_player != null && _player.isPlaying) {
            _player.Pause();
        }
    }

    private void EnsureLayer() {
        if (_player != null) {
            return;
        }

        GameObject layer = new GameObject("MotionCG_Video", typeof(RectTransform));
        layer.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)layer.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        _screen = layer.AddComponent<RawImage>();
        _screen.raycastTarget = false;
        _screen.color = Color.clear;

        GameObject playerObject = new GameObject($"MotionCG_Player ({name})");
        _audio = playerObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _player = playerObject.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.waitForFirstFrame = true;
        _player.skipOnDrop = true;
        // Game time, not the audio clock: the clip keeps moving when audio is paused or the window is unfocused.
        _player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _player.SetTargetAudioSource(0, _audio);
        _player.frameReady += OnFrameReady;
        _player.loopPointReached += OnFinished;
    }

    // Not looping: it stops on its last frame, which stays in the texture.
    private void OnFinished(VideoPlayer source) {
        _finished = true;
    }

    // The video covers the still only once it has a picture, so there is never a black flash.
    private void OnFrameReady(VideoPlayer source, long frame) {
        _frameShown = true;
        _screen.color = Color.white;
        source.sendFrameReadyEvents = false;
    }

    private void OnDestroy() {
        if (_player != null) {
            _player.frameReady -= OnFrameReady;
            _player.loopPointReached -= OnFinished;
            Destroy(_player.gameObject);
        }

        if (_texture != null) {
            _texture.Release();
            Destroy(_texture);
        }
    }
}
