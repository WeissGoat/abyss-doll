using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// A short entrance clip before a fight: when an enemy's combat visual has a registered "<CombatVisualID>_intro" video
// (a boss waking up, in Approved), it plays full screen once, over the combat screen, then fades out. A click skips it.
// It plays once per fight (the HUD can be switched off and on during one), and gives up on its own if the video
// never produces a frame or runs past its length, so a stuck decoder never blocks the fight.
public class CombatIntroCutscene : MonoBehaviour {
    public const string IntroSuffix = "_intro";
    private const float FadeSeconds = 0.45f;
    private const float FirstFrameTimeout = 2f;

    private static object _lastFight;

    private VideoPlayer _player;
    private AudioSource _audio;
    private RawImage _screen;
    private CanvasGroup _group;
    private RenderTexture _texture;
    private float _started;
    private float _length;
    private float _fadeStart = -1f;
    private bool _frameShown;

    public static CombatIntroCutscene Current { get; private set; }
    public bool FrameShown => _frameShown;

    // fight: any object that identifies this fight (the first enemy); enemyVisualID: its combat visual.
    public static CombatIntroCutscene TryPlay(Transform parent, object fight, string enemyVisualID) {
        if (parent == null || fight == null || fight == _lastFight || string.IsNullOrEmpty(enemyVisualID)) {
            return null;
        }

        if (!VisualAssetService.TryGetVideo(enemyVisualID + IntroSuffix, out VideoClip clip)) {
            return null;
        }

        _lastFight = fight;
        GameObject root = new GameObject("CombatIntroCutscene", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        CombatIntroCutscene cutscene = root.AddComponent<CombatIntroCutscene>();
        cutscene.Build(clip);
        return cutscene;
    }

    private void Build(VideoClip clip) {
        Current = this;
        RectTransform rect = (RectTransform)transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Canvas canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 950;
        _group = GetComponent<CanvasGroup>();

        Image backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        backdrop.transform.SetParent(transform, false);
        backdrop.color = Color.black;
        Stretch(backdrop.rectTransform);
        Button skip = backdrop.gameObject.AddComponent<Button>();
        skip.transition = Selectable.Transition.None;
        skip.onClick.AddListener(Finish);

        _screen = new GameObject("Video", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        _screen.transform.SetParent(transform, false);
        _screen.raycastTarget = false;
        _screen.color = Color.clear;
        Stretch(_screen.rectTransform);
        AspectRatioFitter fitter = _screen.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = clip.height > 0 ? clip.width / (float)clip.height : 16f / 9f;

        _texture = new RenderTexture((int)clip.width, (int)clip.height, 0) { name = clip.name };
        _screen.texture = _texture;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.volume = GameSettingsService.SfxVolume;
        _player = gameObject.AddComponent<VideoPlayer>();
        _player.playOnAwake = false;
        _player.isLooping = false;
        _player.waitForFirstFrame = true;
        _player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        _player.renderMode = VideoRenderMode.RenderTexture;
        _player.targetTexture = _texture;
        _player.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _player.SetTargetAudioSource(0, _audio);
        _player.clip = clip;
        _player.sendFrameReadyEvents = true;
        _player.frameReady += OnFrameReady;
        _player.loopPointReached += _ => Finish();
        _length = (float)clip.length;
        _started = Time.unscaledTime;
        _player.Play();
    }

    private void OnFrameReady(VideoPlayer source, long frame) {
        _frameShown = true;
        _screen.color = Color.white;
        source.sendFrameReadyEvents = false;
    }

    private void Update() {
        float elapsed = Time.unscaledTime - _started;
        if (_fadeStart < 0f && ((!_frameShown && elapsed > FirstFrameTimeout) || elapsed > _length + 1f)) {
            Finish();
        }

        if (_fadeStart >= 0f) {
            float t = (Time.unscaledTime - _fadeStart) / FadeSeconds;
            _group.alpha = 1f - Mathf.Clamp01(t);
            if (t >= 1f) {
                Destroy(gameObject);
            }
        }
    }

    public void Finish() {
        if (_fadeStart >= 0f) {
            return;
        }

        _fadeStart = Time.unscaledTime;
        _group.blocksRaycasts = false;
        if (_player != null) {
            _player.Pause();
        }
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy() {
        if (Current == this) {
            Current = null;
        }

        if (_player != null) {
            _player.frameReady -= OnFrameReady;
        }

        if (_texture != null) {
            _texture.Release();
            Destroy(_texture);
        }
    }
}
