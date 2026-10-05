using System.Collections.Generic;
using UnityEngine;

// Music and sound effects (M4). Lives on GameRoot. Clips are found by ID under Resources/Audio/BGM and
// Resources/Audio/SFX; a missing clip plays nothing, so cues can be wired before their sounds exist. Game rules never
// call this: BindGameEvents turns event-bus events into cues, and screens pick their music.
public class GameAudio : MonoBehaviour {
    public const string BgmFolder = "Audio/BGM/";
    public const string SfxFolder = "Audio/SFX/";
    private const float FadeSeconds = 0.8f;
    private const int SfxVoices = 6;

    private static GameAudio _instance;
    private readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
    private AudioSource[] _music;
    private AudioSource[] _effects;
    private int _activeMusic;
    private int _nextEffect;
    private string _currentBgm;

    public static string CurrentBgm => _instance != null ? _instance._currentBgm : null;

    private void Awake() {
        _instance = this;
        _music = new[] { CreateSource(true), CreateSource(true) };
        _effects = new AudioSource[SfxVoices];
        for (int i = 0; i < SfxVoices; i++) {
            _effects[i] = CreateSource(false);
        }
    }

    private AudioSource CreateSource(bool loop) {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        return source;
    }

    // Crossfades to the track; the same ID again keeps it playing.
    public static void PlayBgm(string id) {
        if (_instance == null || id == _instance._currentBgm) {
            return;
        }

        _instance._currentBgm = id;
        AudioClip clip = _instance.Load(BgmFolder + id);
        _instance._activeMusic = 1 - _instance._activeMusic;
        AudioSource next = _instance._music[_instance._activeMusic];
        next.clip = clip;
        next.volume = 0f;
        if (clip != null) {
            next.Play();
        } else {
            next.Stop();
        }
    }

    public static void PlaySfx(string id) {
        if (_instance == null) {
            return;
        }

        AudioClip clip = _instance.Load(SfxFolder + id);
        if (clip == null) {
            return;
        }

        AudioSource source = _instance._effects[_instance._nextEffect];
        _instance._nextEffect = (_instance._nextEffect + 1) % SfxVoices;
        source.volume = GameSettingsService.SfxVolume;
        source.PlayOneShot(clip);
    }

    private AudioClip Load(string path) {
        if (!_clips.TryGetValue(path, out AudioClip clip)) {
            clip = Resources.Load<AudioClip>(path);
            _clips[path] = clip;
        }

        return clip;
    }

    private void Update() {
        float step = Time.unscaledDeltaTime / FadeSeconds;
        float target = GameSettingsService.BgmVolume;
        for (int i = 0; i < _music.Length; i++) {
            AudioSource source = _music[i];
            float goal = i == _activeMusic ? target : 0f;
            source.volume = Mathf.MoveTowards(source.volume, goal, step * Mathf.Max(target, 0.01f));
            if (i != _activeMusic && source.isPlaying && source.volume <= 0f) {
                source.Stop();
            }
        }
    }

    // CoreBackend.InitAllSystems drops every listener, so the flow controller binds again after each new game.
    public static void BindGameEvents() {
        GameEventBus.OnDamageDealt -= HandleDamageDealt;
        GameEventBus.OnDamageDealt += HandleDamageDealt;
        GameEventBus.OnShieldChanged -= HandleShieldChanged;
        GameEventBus.OnShieldChanged += HandleShieldChanged;
        GameEventBus.OnItemPlaced -= HandleItemPlaced;
        GameEventBus.OnItemPlaced += HandleItemPlaced;
        GameEventBus.OnItemRemoved -= HandleItemRemoved;
        GameEventBus.OnItemRemoved += HandleItemRemoved;
        CombatEventBus.OnCombatPhase -= HandleCombatPhase;
        CombatEventBus.OnCombatPhase += HandleCombatPhase;
        DungeonEventBus.OnDungeonRunStarted -= HandleRunStarted;
        DungeonEventBus.OnDungeonRunStarted += HandleRunStarted;
        DungeonEventBus.OnLayerLoaded -= HandleLayerLoaded;
        DungeonEventBus.OnLayerLoaded += HandleLayerLoaded;
        DungeonEventBus.OnNodeEntered -= HandleNodeEntered;
        DungeonEventBus.OnNodeEntered += HandleNodeEntered;
        DungeonEventBus.OnCombatLootCollected -= HandleLootCollected;
        DungeonEventBus.OnCombatLootCollected += HandleLootCollected;
        DungeonEventBus.OnDungeonSettled -= HandleDungeonSettled;
        DungeonEventBus.OnDungeonSettled += HandleDungeonSettled;
    }

    private static void HandleDamageDealt(string attacker, string target, int damage) {
        bool playerHit = GameRoot.Core?.Combat?.PlayerFaction?.Fighters?.Exists(fighter => fighter.Name == target) ?? false;
        PlaySfx(playerHit ? "hurt" : "hit");
    }

    private static int _lastShield;

    private static void HandleShieldChanged(string owner, int shield) {
        if (shield > _lastShield) {
            PlaySfx("shield");
        }

        _lastShield = shield;
    }

    private static void HandleItemPlaced(string itemID, int x, int y) {
        PlaySfx("item_place");
    }

    private static void HandleItemRemoved(string itemID) {
        PlaySfx("item_pick");
    }

    private static void HandleCombatPhase(CombatEventType phase, CombatFaction faction) {
        if (phase == CombatEventType.OnCombatEnd) {
            PlayBgm(LayerBgm());
        }
    }

    private static void HandleRunStarted(int layerID) {
        PlayBgm(LayerBgm());
    }

    private static void HandleLayerLoaded() {
        PlayBgm(LayerBgm());
    }

    private static void HandleNodeEntered(NodeBase node, int sanCost) {
        if (node is CombatNode combat) {
            PlayBgm(combat.NodeID != null && combat.NodeID.EndsWith("_boss") ? "boss" : "battle");
        } else {
            PlaySfx("step");
        }
    }

    private static void HandleLootCollected(CombatLootCollectionResult result) {
        PlaySfx("loot");
    }

    private static void HandleDungeonSettled(bool victory) {
        PlaySfx(victory ? "extract" : "defeat");
    }

    private static string LayerBgm() {
        int layer = GameRoot.Core?.Dungeon?.CurrentLayer?.LayerID ?? 1;
        return layer >= 2 ? "abyss_l2" : "abyss_l1";
    }
}
