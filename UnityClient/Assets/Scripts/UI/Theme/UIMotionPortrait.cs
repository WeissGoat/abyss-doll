using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// A portrait that lives: when a still has a registered "<visualID>_idle" clip (a transparent, seamless loop made from
// that still with tools/美术工具/make_alpha_loop.py, in Approved, imported with Keep Alpha), a video layer over the image
// loops it, and the still is hidden once the first frame is decoded, so there is never a gap. Only screens that ask
// for it get one (the hall), not every image that shows the portrait.
// V toggles video portraits on and off everywhere (sample comparison against the still with its mesh sway).
// B cycles through alternative takes registered beside the main one ("<visualID>_idle_b", "_c", "_d"), so candidate
// loops can be compared in the game; without alternatives B does nothing, and players see the main take.
[RequireComponent(typeof(Image))]
public class UIMotionPortrait : MonoBehaviour {
    public const string IdleSuffix = "_idle";
    public static bool Enabled = true;
    public static int Take;
    private static readonly string[] TakeSuffixes = { "", "_b", "_c", "_d" };
    private static int _toggledFrame = -1;
    private static int _takeFrame = -1;

    private VideoPlayer _player;
    private RawImage _screen;
    private RenderTexture _texture;
    private VideoClip _clip;
    private VideoClip[] _takes;
    private Image _still;
    private bool _frameShown;

    public VideoClip Clip => _clip;
    public int TakeCount => _takes != null ? _takes.Length : 0;
    public int MipCount => _texture != null ? _texture.mipmapCount : 0;
    public bool FrameShown => _frameShown && _screen != null && _screen.isActiveAndEnabled && _screen.color.a > 0f;
    public long Frame => _player != null ? _player.frame : -1;

    // Called after the screen has put the portrait's sprite on the image; a portrait without a clip stays a still.
    public static UIMotionPortrait Attach(Image image, string visualID) {
        if (image == null) {
            return null;
        }

        var takes = new List<VideoClip>();
        if (!string.IsNullOrEmpty(visualID) && VisualAssetService.TryGetVideo(visualID + IdleSuffix, out VideoClip clip)) {
            takes.Add(clip);
            for (int i = 1; i < TakeSuffixes.Length; i++) {
                if (VisualAssetService.TryGetVideo(visualID + IdleSuffix + TakeSuffixes[i], out VideoClip take)) {
                    takes.Add(take);
                }
            }
        }

        UIMotionPortrait motion = image.GetComponent<UIMotionPortrait>();
        if (takes.Count == 0) {
            if (motion != null) {
                motion.Stop();
            }

            return null;
        }

        if (motion == null) {
            motion = image.gameObject.AddComponent<UIMotionPortrait>();
        }

        motion._takes = takes.ToArray();
        motion.Play(motion.CurrentTake);
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

        // Mipmapped like the portrait stills: the hall shows the clip at under half size, and without mips thin strands
        // break into dots that move from frame to frame.
        _texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32) {
            name = clip.name, useMipMap = true, autoGenerateMips = true, filterMode = FilterMode.Trilinear
        };
        _screen.texture = _texture;
        _player.targetTexture = _texture;
        _player.clip = clip;
        _player.sendFrameReadyEvents = true;
        if (Enabled && isActiveAndEnabled) {
            _player.Play();
        }
    }

    private VideoClip CurrentTake => _takes[Take % _takes.Length];

    public void Stop() {
        if (_player != null) {
            _player.Stop();
        }

        _clip = null;
        _takes = null;
        _frameShown = false;
        ShowStill(true);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.V) && _toggledFrame != Time.frameCount) {
            _toggledFrame = Time.frameCount;
            Enabled = !Enabled;
        }

        if (Input.GetKeyDown(KeyCode.B) && _takeFrame != Time.frameCount && _takes != null && _takes.Length > 1) {
            _takeFrame = Time.frameCount;
            Take = (Take + 1) % _takes.Length;
            PlayerToast.Show($"待机视频 {(char)('A' + Take)}：{CurrentTake.name}");
        }

        if (_player == null || _clip == null) {
            return;
        }

        if (_takes != null && CurrentTake != _clip) {
            Play(CurrentTake);
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
