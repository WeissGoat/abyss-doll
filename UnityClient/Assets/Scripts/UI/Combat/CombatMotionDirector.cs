using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays CombatBeats on the combat stage: idle breathing, a lunge when a fighter strikes, a flash, shake and floating
// number when one is hit, a coloured pulse for shield and healing, and a small screen shake for heavy hits on Zero.
// On top of that, pose-to-pose acting: a fighter with a pose set (CombatPose) snaps to its attack, guard, hurt, item
// or kneel pose for the beat; Zero's core flares and the weapon's icon flies out of her open palm, a slash or burst
// marks the hit, a hit freezes
// the stage for a few frames (hitstop) and pushes it in a little, and Zero holding on at 1 HP gets a quick cut-in.
// The HUD rebuilds enemy slots whenever HP changes, so motion is kept per fighter and applied every frame to whatever
// image is registered for it now, as an offset on top of the layout the HUD set (the HUD can move things freely).
public class CombatMotionDirector : MonoBehaviour {
    public const float LungeDistance = 54f;
    public const float LungeSeconds = 0.26f;
    public const float ShakeSeconds = 0.32f;
    public const float FlashSeconds = 0.24f;
    public const float PopupSeconds = 0.95f;
    public const float HitstopSeconds = 0.07f;
    public const float HeavyHitstopSeconds = 0.12f;
    public const float PunchSeconds = 0.3f;
    public const float PunchScale = 0.035f;
    private const float BreathHz = 0.42f;
    private const float HitFlashStrength = 0.55f;
    private const float WindupSeconds = 0.1f;
    private const float AttackPoseSeconds = 0.5f;
    private const float ReactPoseSeconds = 0.45f;
    private const float ActPoseSeconds = 0.6f;
    private const float KneelPoseSeconds = 1.5f;
    private const float SwingSeconds = 0.36f;
    private const float LaunchDistance = 320f;
    private const float LaunchArc = 70f;
    private const float SlashSeconds = 0.28f;
    private const float CutInSeconds = 1.3f;
    private const float SlashAfterStrikeWindow = 1.2f;

    private static readonly Color HitFlash = new Color(1f, 0.96f, 0.92f, 1f);
    private static readonly Color ShieldFlash = new Color(0.62f, 0.8f, 1f, 1f);
    private static readonly Color HealFlash = new Color(0.66f, 0.95f, 0.6f, 1f);
    private static readonly Color DamageText = new Color(1f, 0.42f, 0.32f, 1f);
    private static readonly Color ShieldText = new Color(0.62f, 0.8f, 1f, 1f);
    private static readonly Color SlashColor = new Color(1f, 0.97f, 0.86f, 1f);
    private static readonly Color BurstOnZero = new Color(1f, 0.55f, 0.3f, 1f);
    private static readonly Color CoreLight = new Color(0.66f, 0.88f, 1f, 1f);
    // Where her open palm and the core under her other hand are on her attack pose (doll_zero_combat_attack, facing
    // right), as (u, v) of the image from the bottom-left.
    private static readonly Vector2 PalmPoint = new Vector2(0.92f, 0.71f);
    private static readonly Vector2 CorePoint = new Vector2(0.515f, 0.59f);

    private class Actor {
        public FighterEntity Fighter;
        public Image Image;
        public RectTransform Rect;
        public Material Material;
        public bool FacesRight;
        public bool BreathesItself;
        public float Phase;
        public Vector2 AppliedOffset;
        public Vector2 AppliedScale = Vector2.one;
        public float LungeStart = float.NegativeInfinity;
        public float LungeSign = 1f;
        public float ShakeStart = float.NegativeInfinity;
        public float ShakeStrength;
        public float FlashStart = float.NegativeInfinity;
        public Color FlashColor = Color.white;
        public float FlashStrength = 0.85f;
        public string PoseSetID;
        public Sprite BaseSprite;
        public string WindupUntilPose;
        public float WindupUntil = float.NegativeInfinity;
        public string Pose;
        public float PoseUntil = float.NegativeInfinity;
        public string ShownPose = CombatPose.Idle;
    }

    private class Popup {
        public Text Label;
        public Vector2 Origin;
        public float Start;
    }

    private enum VfxKind {
        Swing,
        Slash,
        Burst,
        CutIn
    }

    private class Vfx {
        public VfxKind Kind;
        public RectTransform Rect;
        public Graphic Graphic;
        public CanvasGroup Group;
        public RectTransform Lines;
        public RectTransform Figure;
        public Graphic Trail;
        public Vector2 Origin;
        public Vector2 Destination;
        public float Angle;
        public float Sign;
        public float Start;
        public float Duration;
    }

    private readonly Dictionary<FighterEntity, Actor> _actors = new Dictionary<FighterEntity, Actor>();
    private readonly List<Popup> _popups = new List<Popup>();
    private readonly List<Vfx> _vfx = new List<Vfx>();
    private RectTransform _popupLayer;
    private RectTransform _shakeRoot;
    private Vector2 _shakeApplied;
    private float _punchApplied = 1f;
    private float _screenShakeStart = float.NegativeInfinity;
    private float _screenShakeStrength;
    private float _punchStart = float.NegativeInfinity;
    private float _hitstopUntil = float.NegativeInfinity;
    private float _clock;
    private float _lastDollStrike = float.NegativeInfinity;
    private Material _flashTemplate;

    private static Sprite _slashSprite;
    private static Sprite _burstSprite;
    private static Sprite _linesSprite;
    private static Sprite _trailSprite;

    // Seen by the Play Mode scenario: the last beats played and the largest lunge offset applied since the reset.
    public readonly List<CombatBeat> PlayedBeats = new List<CombatBeat>();
    public readonly List<float> PlayedBeatTimes = new List<float>();
    public float MaxLungeOffset { get; private set; }
    public int PopupsSpawned { get; private set; }
    public int VfxSpawned { get; private set; }
    public int HitstopsPlayed { get; private set; }
    public readonly List<string> PosesShown = new List<string>();

    // Stage time: stops during hitstop, so lunges, shakes and numbers freeze together on the impact frame.
    public float StageTime => _clock;

    public static CombatMotionDirector Ensure(GameObject host) {
        CombatMotionDirector director = host.GetComponent<CombatMotionDirector>();
        return director != null ? director : host.AddComponent<CombatMotionDirector>();
    }

    public static string PoseVisualID(string poseSetID, string pose) {
        return string.IsNullOrEmpty(poseSetID) ? null : $"{poseSetID}_{pose}";
    }

    public static bool HasPose(string poseSetID, string pose) {
        string id = PoseVisualID(poseSetID, pose);
        return id != null && VisualAssetService.TryGetSprite(id, out _);
    }

    public void SetLayers(RectTransform popupLayer, RectTransform shakeRoot) {
        _popupLayer = popupLayer;
        if (_shakeRoot != shakeRoot) {
            ClearScreenShake();
            _shakeRoot = shakeRoot;
        }
    }

    // Called by the HUD each time it (re)creates a fighter's image. facesRight: the fighter lunges to the right.
    // poseSetID names the fighter's pose sprites; the image's current sprite is its idle pose.
    public void Register(FighterEntity fighter, Image image, bool facesRight, string poseSetID = null) {
        if (fighter == null || image == null) {
            return;
        }

        if (!_actors.TryGetValue(fighter, out Actor actor)) {
            actor = new Actor { Fighter = fighter, Phase = Random.value * 10f };
            _actors[fighter] = actor;
        }

        if (actor.Image != image) {
            ReleaseMaterial(actor);
            actor.Image = image;
            actor.Rect = image.rectTransform;
            actor.AppliedOffset = Vector2.zero;
            actor.AppliedScale = Vector2.one;
            actor.Material = CreateFlashMaterial();
            if (actor.Material != null) {
                image.material = actor.Material;
            }

            actor.ShownPose = CombatPose.Idle;
        }

        // Zero's image lives through the fight and is registered again on every HP change, maybe mid-pose.
        if (actor.ShownPose == CombatPose.Idle || actor.BaseSprite == null) {
            actor.BaseSprite = image.sprite;
        }

        actor.PoseSetID = poseSetID;
        actor.FacesRight = facesRight;
        UIStillMotion still = image.GetComponent<UIStillMotion>();
        actor.BreathesItself = still != null && still.enabled;
    }

    public void ResetRecording() {
        PlayedBeats.Clear();
        PlayedBeatTimes.Clear();
        PosesShown.Clear();
        MaxLungeOffset = 0f;
        PopupsSpawned = 0;
        VfxSpawned = 0;
        HitstopsPlayed = 0;
    }

    private void OnEnable() {
        GameEventBus.OnCombatBeat -= HandleBeat;
        GameEventBus.OnCombatBeat += HandleBeat;
    }

    private void OnDisable() {
        GameEventBus.OnCombatBeat -= HandleBeat;
        foreach (Actor actor in _actors.Values) {
            RestoreLayout(actor);
            ReleaseMaterial(actor);
        }

        _actors.Clear();
        foreach (Popup popup in _popups) {
            if (popup.Label != null) {
                Destroy(popup.Label.gameObject);
            }
        }

        _popups.Clear();
        foreach (Vfx vfx in _vfx) {
            if (vfx.Rect != null) {
                Destroy(vfx.Rect.gameObject);
            }

            if (vfx.Trail != null) {
                Destroy(vfx.Trail.gameObject);
            }
        }

        _vfx.Clear();
        ClearScreenShake();
    }

    private void HandleBeat(CombatBeat beat) {
        if (beat == null) {
            return;
        }

        PlayedBeats.Add(beat);
        PlayedBeatTimes.Add(Time.time);
        float now = _clock;
        switch (beat.Kind) {
            case CombatBeatKind.Strike:
            case CombatBeatKind.Interfere:
                if (TryGetActor(beat.Actor, out Actor striker)) {
                    bool selfUse = beat.Target != null && beat.Target == beat.Actor;
                    striker.LungeStart = now;
                    striker.LungeSign = selfUse ? 0f : (striker.FacesRight ? 1f : -1f);
                    if (beat.Kind == CombatBeatKind.Interfere) {
                        SpawnPopup(striker, beat.Label, UITheme.AccentOnDark, 22);
                    }

                    if (beat.Pose == CombatPose.Kneel) {
                        SetPose(striker, CombatPose.Kneel, now + KneelPoseSeconds);
                        SpawnCutIn(striker, beat.Label, now);
                        _hitstopUntil = Time.unscaledTime + HeavyHitstopSeconds * 2f;
                    } else if (beat.Kind == CombatBeatKind.Strike && selfUse) {
                        SetPose(striker, CombatPose.Guard, now + ActPoseSeconds);
                    } else if (beat.Kind == CombatBeatKind.Strike && beat.Pose == CombatPose.Punch && HasPose(striker.PoseSetID, CombatPose.Punch)) {
                        // Bare hands: a plain punch, no core flare and nothing thrown.
                        SetPose(striker, CombatPose.Punch, now + AttackPoseSeconds);
                    } else if (beat.Kind == CombatBeatKind.Strike) {
                        if (HasPose(striker.PoseSetID, CombatPose.Windup)) {
                            striker.WindupUntil = now + WindupSeconds;
                            striker.WindupUntilPose = CombatPose.Windup;
                            SetPose(striker, CombatPose.Attack, now + WindupSeconds + AttackPoseSeconds);
                        } else {
                            SetPose(striker, CombatPose.Attack, now + AttackPoseSeconds);
                        }

                        if (striker.Fighter is DollFighter) {
                            _lastDollStrike = now;
                            TryGetActor(beat.Target, out Actor aimed);
                            SpawnSwing(striker, aimed, beat.SourceVisualID, now);
                        }
                    }
                }
                break;
            case CombatBeatKind.Hit:
                if (TryGetActor(beat.Target, out Actor target)) {
                    bool hpHit = beat.HpLoss > 0;
                    // Softer than the pulses: the hitstop holds the flash at its peak, and a full-strength one
                    // turns the fighter into a flat cream silhouette.
                    Flash(target, hpHit ? HitFlash : ShieldFlash, now, HitFlashStrength);
                    float ratio = beat.Target.RuntimeMaxHP > 0 ? beat.HpLoss / (float)beat.Target.RuntimeMaxHP : 0f;
                    target.ShakeStart = now;
                    target.ShakeStrength = Mathf.Lerp(6f, 18f, Mathf.Clamp01(ratio * 4f));
                    if (beat.ShieldLoss > 0) {
                        SpawnPopup(target, $"护盾 -{beat.ShieldLoss}", ShieldText, 24);
                    }

                    if (hpHit || beat.ShieldLoss <= 0) {
                        SpawnPopup(target, $"-{beat.HpLoss}", DamageText, beat.Defeated ? 46 : 38);
                    }

                    if (target.Pose != CombatPose.Kneel || now >= target.PoseUntil) {
                        SetPose(target, hpHit ? CombatPose.Hurt : CombatPose.Guard, now + ReactPoseSeconds);
                    }

                    bool fromDoll = !(beat.Target is DollFighter) && now - _lastDollStrike < SlashAfterStrikeWindow;
                    if (fromDoll) {
                        SpawnSlash(target, now);
                    } else if (hpHit) {
                        SpawnBurst(target, BurstOnZero, now);
                    }

                    if (hpHit) {
                        bool heavy = beat.Defeated || ratio >= 0.2f;
                        _hitstopUntil = Time.unscaledTime + (heavy ? HeavyHitstopSeconds : HitstopSeconds);
                        _punchStart = now;
                        HitstopsPlayed++;
                    }

                    if (beat.Target is DollFighter && (ratio >= 0.12f || beat.Defeated)) {
                        _screenShakeStart = now;
                        _screenShakeStrength = beat.Defeated ? 14f : 8f;
                    }
                }
                break;
            case CombatBeatKind.Shield:
                if (TryGetActor(beat.Target, out Actor shielded)) {
                    Flash(shielded, ShieldFlash, now);
                    SpawnPopup(shielded, $"+{beat.Amount} 护盾", ShieldText, 26);
                    SetPose(shielded, CombatPose.Guard, now + ActPoseSeconds);
                }
                break;
            case CombatBeatKind.Heal:
                if (TryGetActor(beat.Target, out Actor healed)) {
                    Flash(healed, HealFlash, now);
                    SpawnPopup(healed, $"+{beat.Amount}", UITheme.PositiveOnDark, 32);
                    SetPose(healed, CombatPose.Item, now + ActPoseSeconds);
                }
                break;
        }
    }

    private void LateUpdate() {
        if (Time.unscaledTime >= _hitstopUntil) {
            _clock += Time.deltaTime;
        }

        float now = _clock;
        List<FighterEntity> gone = null;
        foreach (KeyValuePair<FighterEntity, Actor> pair in _actors) {
            Actor actor = pair.Value;
            if (actor.Rect == null) {
                ReleaseMaterial(actor);
                (gone = gone ?? new List<FighterEntity>()).Add(pair.Key);
                continue;
            }

            Apply(actor, now);
        }

        if (gone != null) {
            foreach (FighterEntity fighter in gone) {
                _actors.Remove(fighter);
            }
        }

        UpdatePopups(now);
        UpdateVfx(now);
        UpdateScreenShake(now);
    }

    private void Apply(Actor actor, float now) {
        bool alive = actor.Fighter.RuntimeHP > 0;
        ApplyPose(actor, now);
        float breath = alive && !actor.BreathesItself ? Mathf.Sin((now + actor.Phase) * BreathHz * Mathf.PI * 2f) : 0f;
        Vector2 scale = new Vector2(1f - 0.006f * breath, 1f + 0.016f * breath);

        float lunge = LungeCurve((now - actor.LungeStart) / LungeSeconds);
        Vector2 offset = new Vector2(actor.LungeSign * LungeDistance * lunge, actor.LungeSign == 0f ? 10f * lunge : 0f);
        if (lunge > 0f) {
            scale *= 1f + 0.05f * lunge;
            MaxLungeOffset = Mathf.Max(MaxLungeOffset, Mathf.Abs(offset.x) + Mathf.Abs(offset.y));
        }

        float shakeT = (now - actor.ShakeStart) / ShakeSeconds;
        if (shakeT >= 0f && shakeT < 1f) {
            offset.x += Mathf.Sin(shakeT * 34f) * actor.ShakeStrength * (1f - shakeT);
        }

        RectTransform rect = actor.Rect;
        rect.anchoredPosition = rect.anchoredPosition - actor.AppliedOffset + offset;
        actor.AppliedOffset = offset;
        Vector3 local = rect.localScale;
        rect.localScale = new Vector3(local.x / actor.AppliedScale.x * scale.x, local.y / actor.AppliedScale.y * scale.y, local.z);
        actor.AppliedScale = scale;

        if (actor.Material != null) {
            float flash = 1f - (now - actor.FlashStart) / FlashSeconds;
            actor.Material.SetColor("_FlashColor", actor.FlashColor);
            actor.Material.SetFloat("_FlashAmount", Mathf.Clamp01(flash) * actor.FlashStrength);
        }
    }

    private static void SetPose(Actor actor, string pose, float until) {
        actor.Pose = pose;
        actor.PoseUntil = until;
    }

    // The pose wanted now: a short wind-up before the attack, the beat's pose until it runs out, then idle (a
    // defeated Zero stays down on one knee).
    private void ApplyPose(Actor actor, float now) {
        string wanted = CombatPose.Idle;
        if (now < actor.WindupUntil) {
            wanted = actor.WindupUntilPose;
        } else if (now < actor.PoseUntil) {
            wanted = actor.Pose;
        } else if (actor.Fighter is DollFighter && actor.Fighter.RuntimeHP <= 0) {
            wanted = CombatPose.Kneel;
        }

        Sprite sprite = actor.BaseSprite;
        if (wanted != CombatPose.Idle && VisualAssetService.TryGetSprite(PoseVisualID(actor.PoseSetID, wanted), out Sprite posed)) {
            sprite = posed;
        } else if (wanted != CombatPose.Idle) {
            wanted = CombatPose.Idle;
        }

        if (wanted != actor.ShownPose && wanted != CombatPose.Idle) {
            PosesShown.Add(wanted);
        }

        actor.ShownPose = wanted;
        if (sprite != null && actor.Image.sprite != sprite) {
            actor.Image.sprite = sprite;
        }
    }

    // 0 → 1 fast (wind-up and strike), then back to 0.
    public static float LungeCurve(float t) {
        if (t < 0f || t >= 1f) {
            return 0f;
        }

        return t < 0.35f ? Mathf.SmoothStep(0f, 1f, t / 0.35f) : Mathf.SmoothStep(1f, 0f, (t - 0.35f) / 0.65f);
    }

    private void Flash(Actor actor, Color color, float now, float strength = 0.85f) {
        actor.FlashColor = color;
        actor.FlashStart = now;
        actor.FlashStrength = strength;
    }

    private Vector2 StagePoint(Actor actor, float u, float v) {
        Rect r = actor.Rect.rect;
        Vector3 world = actor.Rect.TransformPoint(new Vector3(Mathf.Lerp(r.xMin, r.xMax, u), Mathf.Lerp(r.yMin, r.yMax, v), 0f));
        return _popupLayer.InverseTransformPoint(world);
    }

    private void SpawnPopup(Actor actor, string text, Color color, int size) {
        if (_popupLayer == null || actor.Rect == null || string.IsNullOrEmpty(text)) {
            return;
        }

        Text label = UITheme.CreateText(_popupLayer, "CombatPopup_Text", text, size, color, TextAnchor.MiddleCenter);
        label.raycastTarget = false;
        label.fontStyle = FontStyle.Bold;
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.06f, 0.04f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 60f);
        Vector2 origin = StagePoint(actor, 0.5f, 0.9f);
        int stacked = _popups.FindAll(p => p.Start > _clock - 0.25f).Count;
        origin.y -= stacked * 36f;
        rect.anchoredPosition = origin;
        _popups.Add(new Popup { Label = label, Origin = origin, Start = _clock });
        PopupsSpawned++;
    }

    private void UpdatePopups(float now) {
        for (int i = _popups.Count - 1; i >= 0; i--) {
            Popup popup = _popups[i];
            float t = (now - popup.Start) / PopupSeconds;
            if (popup.Label == null || t >= 1f) {
                if (popup.Label != null) {
                    Destroy(popup.Label.gameObject);
                }

                _popups.RemoveAt(i);
                continue;
            }

            float pop = t < 0.12f ? Mathf.Lerp(0.6f, 1.15f, t / 0.12f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((t - 0.12f) / 0.2f));
            popup.Label.rectTransform.localScale = Vector3.one * pop;
            popup.Label.rectTransform.anchoredPosition = popup.Origin + new Vector2(0f, 70f * Mathf.Sqrt(t));
            Color color = popup.Label.color;
            color.a = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            popup.Label.color = color;
        }
    }

    // She does not swing the weapon: the core in her chest flares under her hand and the weapon's icon flies out of
    // her open palm in a low arc onto the enemy, with a pale-blue streak behind it.
    private void SpawnSwing(Actor actor, Actor target, string iconID, float now) {
        if (_popupLayer == null) {
            return;
        }

        float sign = actor.FacesRight ? 1f : -1f;
        SpawnBurst(actor, CoreLight, now, Mirror(actor, CorePoint), 130f);
        if (string.IsNullOrEmpty(iconID) || !VisualAssetService.TryGetSprite(iconID, out Sprite icon)) {
            return;
        }

        Image trail = CreateVfxImage("CombatLaunchTrail_Image", TrailSprite(), new Vector2(40f, 64f), new Vector2(1f, 0.5f));
        trail.preserveAspect = false;
        trail.color = CoreLight;
        Image image = CreateVfxImage("CombatSwing_Image", icon, new Vector2(150f, 150f), new Vector2(0.5f, 0.5f));
        Vector2 origin = StagePoint(actor, Mirror(actor, PalmPoint).x, PalmPoint.y);
        Vector2 destination = target != null && target.Rect != null && target != actor
            ? StagePoint(target, 0.5f, 0.5f)
            : origin + new Vector2(sign * LaunchDistance, 0f);
        _vfx.Add(new Vfx { Kind = VfxKind.Swing, Rect = image.rectTransform, Graphic = image, Trail = trail, Origin = origin, Destination = destination, Sign = sign, Start = now, Duration = SwingSeconds });
        VfxSpawned++;
    }

    private static Vector2 LaunchPosition(Vfx vfx, float t) {
        float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
        return Vector2.Lerp(vfx.Origin, vfx.Destination, ease) + new Vector2(0f, LaunchArc * Mathf.Sin(Mathf.PI * ease));
    }

    private static Vector2 Mirror(Actor actor, Vector2 uv) {
        return actor.FacesRight ? uv : new Vector2(1f - uv.x, uv.y);
    }

    private void SpawnSlash(Actor target, float now) {
        if (_popupLayer == null) {
            return;
        }

        Image image = CreateVfxImage("CombatSlash_Image", SlashSprite(), new Vector2(300f, 300f), new Vector2(0.5f, 0.5f));
        image.color = SlashColor;
        Vector2 origin = StagePoint(target, 0.5f, 0.5f);
        _vfx.Add(new Vfx { Kind = VfxKind.Slash, Rect = image.rectTransform, Graphic = image, Origin = origin, Angle = Random.Range(-28f, 8f), Start = now, Duration = SlashSeconds });
        SpawnBurst(target, SlashColor, now);
        VfxSpawned++;
    }

    private void SpawnBurst(Actor target, Color color, float now) {
        SpawnBurst(target, color, now, new Vector2(0.5f, 0.52f), 220f);
    }

    private void SpawnBurst(Actor target, Color color, float now, Vector2 uv, float size) {
        if (_popupLayer == null) {
            return;
        }

        Image image = CreateVfxImage("CombatBurst_Image", BurstSprite(), new Vector2(size, size), new Vector2(0.5f, 0.5f));
        image.color = color;
        Vector2 origin = StagePoint(target, uv.x, uv.y);
        _vfx.Add(new Vfx { Kind = VfxKind.Burst, Rect = image.rectTransform, Graphic = image, Origin = origin, Angle = Random.Range(0f, 45f), Start = now, Duration = SlashSeconds });
        VfxSpawned++;
    }

    // A band across the screen: speed lines, her kneeling pose sliding in, and the line in big type.
    private void SpawnCutIn(Actor actor, string text, float now) {
        if (_popupLayer == null) {
            return;
        }

        GameObject root = new GameObject("CombatCutIn", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(_popupLayer, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 300f);
        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        Image band = CreateChildImage(rect, "Band", null, new Color(0.08f, 0.05f, 0.04f, 0.82f));
        Stretch(band.rectTransform);
        Image lines = CreateChildImage(rect, "Lines", LinesSprite(), new Color(1f, 0.93f, 0.8f, 0.5f));
        lines.rectTransform.anchorMin = new Vector2(0f, 0f);
        lines.rectTransform.anchorMax = new Vector2(1f, 1f);
        lines.rectTransform.sizeDelta = new Vector2(800f, 0f);

        Sprite kneel = null;
        string kneelID = PoseVisualID(actor.PoseSetID, CombatPose.Kneel);
        RectTransform figure = null;
        if (kneelID != null && VisualAssetService.TryGetSprite(kneelID, out kneel)) {
            Image figureImage = CreateChildImage(rect, "Figure", kneel, Color.white);
            figureImage.preserveAspect = true;
            figure = figureImage.rectTransform;
            figure.anchorMin = figure.anchorMax = new Vector2(0.3f, 0.5f);
            figure.pivot = new Vector2(0.5f, 0.5f);
            figure.sizeDelta = new Vector2(300f, 450f);
        }

        Text label = UITheme.CreateText(rect, "Line", text, 54, new Color(1f, 0.95f, 0.86f, 1f), TextAnchor.MiddleLeft);
        label.raycastTarget = false;
        label.fontStyle = FontStyle.Bold;
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.08f, 0.04f, 0.95f);
        outline.effectDistance = new Vector2(3f, -3f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.45f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.sizeDelta = new Vector2(900f, 90f);

        _vfx.Add(new Vfx { Kind = VfxKind.CutIn, Rect = rect, Group = group, Lines = lines.rectTransform, Figure = figure, Start = now, Duration = CutInSeconds });
        VfxSpawned++;
    }

    private void UpdateVfx(float now) {
        for (int i = _vfx.Count - 1; i >= 0; i--) {
            Vfx vfx = _vfx[i];
            float t = (now - vfx.Start) / vfx.Duration;
            if (vfx.Rect == null || t >= 1f) {
                if (vfx.Rect != null) {
                    Destroy(vfx.Rect.gameObject);
                }

                if (vfx.Trail != null) {
                    Destroy(vfx.Trail.gameObject);
                }

                _vfx.RemoveAt(i);
                continue;
            }

            float fade = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
            switch (vfx.Kind) {
                case VfxKind.Swing: {
                    // Out of her palm: it grows from small and flies in a low arc onto the enemy with a little spin;
                    // the streak points back along its path, at most a few icon lengths long.
                    Vector2 position = LaunchPosition(vfx, t);
                    Vector2 heading = position - LaunchPosition(vfx, t - 0.05f);
                    vfx.Rect.anchoredPosition = position;
                    vfx.Rect.localScale = Vector3.one * Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(t * 3f));
                    vfx.Rect.localRotation = Quaternion.Euler(0f, 0f, -vfx.Sign * 60f * t);
                    SetAlpha(vfx.Graphic, fade);
                    if (vfx.Trail != null) {
                        RectTransform trail = vfx.Trail.rectTransform;
                        trail.anchoredPosition = position;
                        trail.sizeDelta = new Vector2(Mathf.Clamp(Vector2.Distance(position, vfx.Origin) + 30f, 40f, 300f), 64f);
                        trail.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                        SetAlpha(vfx.Trail, 0.85f * fade);
                    }
                    break;
                }
                case VfxKind.Slash: {
                    float grow = Mathf.Lerp(0.55f, 1.2f, Mathf.Sqrt(t));
                    vfx.Rect.anchoredPosition = vfx.Origin;
                    vfx.Rect.localScale = new Vector3(grow, grow * 0.8f, 1f);
                    vfx.Rect.localRotation = Quaternion.Euler(0f, 0f, vfx.Angle);
                    SetAlpha(vfx.Graphic, fade);
                    break;
                }
                case VfxKind.Burst: {
                    float grow = Mathf.Lerp(0.3f, 1.1f, Mathf.Sqrt(t));
                    vfx.Rect.anchoredPosition = vfx.Origin;
                    vfx.Rect.localScale = Vector3.one * grow;
                    vfx.Rect.localRotation = Quaternion.Euler(0f, 0f, vfx.Angle + 30f * t);
                    SetAlpha(vfx.Graphic, (1f - t) * 0.9f);
                    break;
                }
                case VfxKind.CutIn: {
                    float open = Mathf.Clamp01(t / 0.12f);
                    float close = t > 0.82f ? 1f - (t - 0.82f) / 0.18f : 1f;
                    vfx.Rect.localScale = new Vector3(1f, Mathf.SmoothStep(0f, 1f, open) * Mathf.SmoothStep(0f, 1f, close), 1f);
                    vfx.Group.alpha = close;
                    if (vfx.Lines != null) {
                        vfx.Lines.anchoredPosition = new Vector2(-400f * t, 0f);
                    }

                    if (vfx.Figure != null) {
                        float slide = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.25f), 3f);
                        vfx.Figure.anchoredPosition = new Vector2(Mathf.Lerp(-500f, 0f, slide) - 20f * t, -20f);
                    }
                    break;
                }
            }
        }
    }

    private Image CreateVfxImage(string name, Sprite sprite, Vector2 size, Vector2 pivot) {
        Image image = CreateChildImage(_popupLayer, name, sprite, Color.white);
        image.preserveAspect = true;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.sizeDelta = size;
        return image;
    }

    private static Image CreateChildImage(Transform parent, string name, Sprite sprite, Color color) {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void SetAlpha(Graphic graphic, float alpha) {
        if (graphic == null) {
            return;
        }

        Color color = graphic.color;
        color.a = Mathf.Clamp01(alpha);
        graphic.color = color;
    }

    private void UpdateScreenShake(float now) {
        if (_shakeRoot == null) {
            return;
        }

        float t = (now - _screenShakeStart) / ShakeSeconds;
        Vector2 offset = t >= 0f && t < 1f
            ? new Vector2(Mathf.Sin(t * 41f), Mathf.Cos(t * 29f)) * _screenShakeStrength * (1f - t)
            : Vector2.zero;
        _shakeRoot.anchoredPosition = _shakeRoot.anchoredPosition - _shakeApplied + offset;
        _shakeApplied = offset;

        float p = (now - _punchStart) / PunchSeconds;
        float punch = p >= 0f && p < 1f ? 1f + PunchScale * (p < 0.2f ? p / 0.2f : 1f - (p - 0.2f) / 0.8f) : 1f;
        Vector3 scale = _shakeRoot.localScale;
        _shakeRoot.localScale = new Vector3(scale.x / _punchApplied * punch, scale.y / _punchApplied * punch, scale.z);
        _punchApplied = punch;
    }

    private void ClearScreenShake() {
        if (_shakeRoot != null) {
            _shakeRoot.anchoredPosition -= _shakeApplied;
            Vector3 scale = _shakeRoot.localScale;
            _shakeRoot.localScale = new Vector3(scale.x / _punchApplied, scale.y / _punchApplied, scale.z);
        }

        _shakeApplied = Vector2.zero;
        _punchApplied = 1f;
        _screenShakeStart = float.NegativeInfinity;
        _punchStart = float.NegativeInfinity;
    }

    private bool TryGetActor(FighterEntity fighter, out Actor actor) {
        actor = null;
        return fighter != null && _actors.TryGetValue(fighter, out actor) && actor.Rect != null;
    }

    private static void RestoreLayout(Actor actor) {
        if (actor.Rect == null) {
            return;
        }

        actor.Rect.anchoredPosition -= actor.AppliedOffset;
        Vector3 local = actor.Rect.localScale;
        actor.Rect.localScale = new Vector3(local.x / actor.AppliedScale.x, local.y / actor.AppliedScale.y, local.z);
        actor.AppliedOffset = Vector2.zero;
        actor.AppliedScale = Vector2.one;
        if (actor.Image != null) {
            if (actor.BaseSprite != null) {
                actor.Image.sprite = actor.BaseSprite;
            }

            if (actor.Image.material == actor.Material) {
                actor.Image.material = null;
            }
        }
    }

    private Material CreateFlashMaterial() {
        if (_flashTemplate == null) {
            Shader shader = Resources.Load<Shader>("Shaders/P3UIFlash");
            if (shader == null) {
                return null;
            }

            _flashTemplate = new Material(shader);
        }

        return new Material(_flashTemplate);
    }

    private static void ReleaseMaterial(Actor actor) {
        if (actor.Material != null) {
            Destroy(actor.Material);
            actor.Material = null;
        }
    }

    // Effect sprites are drawn once in code: a crescent slash, a spiky impact burst and horizontal speed lines.
    private static Sprite SlashSprite() {
        if (_slashSprite == null) {
            _slashSprite = DrawSprite("P3SlashVfx", 256, 256, (x, y) => {
                float outer = Vector2.Distance(new Vector2(x, y), new Vector2(128f, 100f)) / 120f;
                float inner = Vector2.Distance(new Vector2(x, y), new Vector2(150f, 70f)) / 118f;
                float edge = Mathf.Clamp01((1f - outer) * 14f) * Mathf.Clamp01((inner - 1f) * 9f);
                float taper = Mathf.Clamp01(1f - Mathf.Abs(x - 120f) / 120f);
                return edge * Mathf.Pow(taper, 0.6f);
            });
        }

        return _slashSprite;
    }

    private static Sprite BurstSprite() {
        if (_burstSprite == null) {
            _burstSprite = DrawSprite("P3BurstVfx", 256, 256, (x, y) => {
                Vector2 d = new Vector2(x - 128f, y - 128f);
                float r = d.magnitude / 128f;
                float angle = Mathf.Atan2(d.y, d.x);
                float spikes = 0.42f + 0.5f * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4f)), 6f);
                float body = Mathf.Clamp01((spikes - r) * 8f);
                float core = Mathf.Clamp01(1f - r * 3.2f);
                return Mathf.Clamp01(body * 0.75f + core);
            });
        }

        return _burstSprite;
    }

    // A soft streak, brightest at its right end and fading out to the left (the end that trails).
    private static Sprite TrailSprite() {
        if (_trailSprite == null) {
            _trailSprite = DrawSprite("P3TrailVfx", 256, 64, (x, y) => {
                float across = Mathf.Abs(y - 32f) / 32f;
                float along = x / 256f;
                return Mathf.Pow(along, 1.6f) * Mathf.Clamp01(1f - across * across * (1.4f - 0.8f * along));
            });
        }

        return _trailSprite;
    }

    private static Sprite LinesSprite() {
        if (_linesSprite == null) {
            System.Random random = new System.Random(7);
            float[] rows = new float[64];
            for (int i = 0; i < rows.Length; i++) {
                rows[i] = random.NextDouble() < 0.35 ? (float)random.NextDouble() : 0f;
            }

            _linesSprite = DrawSprite("P3SpeedLinesVfx", 256, 64, (x, y) => {
                float row = rows[Mathf.Clamp(Mathf.FloorToInt(y), 0, rows.Length - 1)];
                return row * Mathf.Clamp01(Mathf.Sin(x / 256f * Mathf.PI * (2f + row * 3f)) * 1.5f);
            });
        }

        return _linesSprite;
    }

    private static Sprite DrawSprite(string name, int width, int height, System.Func<float, float, float> alpha) {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = name, wrapMode = TextureWrapMode.Clamp };
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f)) * 255f);
                pixels[y * width + x] = new Color32(255, 255, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
}
