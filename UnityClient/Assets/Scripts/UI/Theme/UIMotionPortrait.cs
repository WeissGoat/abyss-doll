using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// A portrait that lives: when a still has a registered "<visualID>_idle" clip (a transparent, seamless loop made from
// that still with tools/美术工具/make_alpha_loop.py, in Approved, imported with Keep Alpha), a video layer over the image
// loops it, and the still is hidden once the first frame is decoded, so there is never a gap. Only screens that ask
// for it get one (the hall and the title), not every image that shows the portrait.
// V toggles video portraits on and off everywhere (sample comparison against the still with its mesh sway).
[RequireComponent(typeof(Image))]
public class UIMotionPortrait : MonoBehaviour {
    public const string IdleSuffix = "_idle";
    public static bool Enabled = true;
    private static int _toggledFrame = -1;

    private VideoPlayer _player;
    private RawImage _screen;
    private RenderTexture _texture;
    private VideoClip _clip;
    private Image _still;
    private bool _frameShown;

    public VideoClip Clip => _clip;
    public bool FrameShown => _frameShown && _screen != null && _screen.isActiveAndEnabled && _screen.color.a > 0f;
    public long Frame => _player != null ? _player.frame : -1;

    // Called after the screen has put the portrait's sprite on the image; a portrait without a clip stays a still.
    public static UIMotionPortrait Attach(Image image, string visualID) {
        if (image == null) {
            return null;
        }

        VideoClip clip = null;
        if (!string.IsNullOrEmpty(visualID)) {
            VisualAssetService.TryGetVideo(visualID + IdleSuffix, out clip);
        }

        UIMotionPortrait motion = image.GetComponent<UIMotionPortrait>();
        if (clip == null) {
            if (motion != null) {
                motion.Stop();
            }

            return null;
        }

        if (motion == null) {
            motion = image.gameObject.AddComponent<UIMotionPortrait>();
        }

        motion.Play(clip);
        return motion;
    }

    public void Play(VideoClip clip) {
        _still = GetComponent<Image>();
        if (clip == _clip && _player != null) {
            return;
        }

        EnsureLayer();
        _clip = clip;
        _frameShown = false;
        ShowStill(true);
        if (_texture != null) {
            _texture.Release();
            Destroy(_texture);
        }

        _texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32) { name = clip.name };
        _screen.texture = _texture;
        _player.targetTexture = _texture;
        _player.clip = clip;
        _player.sendFrameReadyEvents = true;
        if (Enabled && isActiveAndEnabled) {
            _player.Play();
        }
    }

    public void Stop() {
        if (_player != null) {
            _player.Stop();
        }

        _clip = null;
        _frameShown = false;
        ShowStill(true);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.V) && _toggledFrame != Time.frameCount) {
            _toggledFrame = Time.frameCount;
            Enabled = !Enabled;
        }

        if (_player == null || _clip == null) {
            return;
        }

        if (!Enabled && _player.isPlaying) {
            _player.Pause();
            ShowStill(true);
        } else if (Enabled && !_player.isPlaying) {
            _player.Play();
        } else if (Enabled && _frameShown && _screen.color.a == 0f) {
            ShowStill(false);
        }
    }

    // The video sits exactly over the still (same rect, same aspect), so hiding one and showing the other swaps them.
    private void ShowStill(bool still) {
        if (_still != null) {
            _still.canvasRenderer.SetAlpha(still ? 1f : 0f);
        }

        if (_screen != null) {
            _screen.color = still ? Color.clear : Color.white;
        }
    }

    private void OnEnable() {
        if (_player != null && _clip != null && Enabled && !_player.isPlaying) {
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

        GameObject layer = new GameObject("MotionPortrait_Video", typeof(RectTransform));
        layer.transform.SetParent(transform, false);
        RectTransform rect = (RectTransform)layer.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();
        _screen = layer.AddComponent<RawImage>();
        _screen.raycastTarget = false;
        _screen.color = Color.clear;

        GameObject playerObject = new GameObject($"MotionPortrait_Player ({name})");
        _player = playerObject.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = true;
        _player.waitForFirstFrame = true;
        _player.skipOnDrop = true;
        _player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.audioOutputMode = VideoAudioOutputMode.None;
        _player.frameReady += OnFrameReady;
    }

    private void OnFrameReady(VideoPlayer source, long frame) {
        _frameShown = true;
        source.sendFrameReadyEvents = false;
        if (Enabled) {
            ShowStill(false);
        }
    }

    private void OnDestroy() {
        if (_player != null) {
            _player.frameReady -= OnFrameReady;
            Destroy(_player.gameObject);
        }

        if (_texture != null) {
            _texture.Release();
            Destroy(_texture);
        }
    }
}
