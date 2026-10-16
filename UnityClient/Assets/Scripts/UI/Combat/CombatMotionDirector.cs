using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays CombatBeats on the combat stage: idle breathing, a lunge when a fighter strikes, a flash, shake and floating
// number when one is hit, a coloured pulse for shield and healing, and a small screen shake for heavy hits on Zero.
// The HUD rebuilds enemy slots whenever HP changes, so motion is kept per fighter and applied every frame to whatever
// image is registered for it now, as an offset on top of the layout the HUD set (the HUD can move things freely).
public class CombatMotionDirector : MonoBehaviour {
    public const float LungeDistance = 54f;
    public const float LungeSeconds = 0.26f;
    public const float ShakeSeconds = 0.32f;
    public const float FlashSeconds = 0.24f;
    public const float PopupSeconds = 0.95f;
    private const float BreathHz = 0.42f;

    private static readonly Color HitFlash = new Color(1f, 0.96f, 0.92f, 1f);
    private static readonly Color ShieldFlash = new Color(0.62f, 0.8f, 1f, 1f);
    private static readonly Color HealFlash = new Color(0.66f, 0.95f, 0.6f, 1f);
    private static readonly Color DamageText = new Color(1f, 0.42f, 0.32f, 1f);
    private static readonly Color ShieldText = new Color(0.62f, 0.8f, 1f, 1f);

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
    }

    private class Popup {
        public Text Label;
        public Vector2 Origin;
        public float Start;
    }

    private readonly Dictionary<FighterEntity, Actor> _actors = new Dictionary<FighterEntity, Actor>();
    private readonly List<Popup> _popups = new List<Popup>();
    private RectTransform _popupLayer;
    private RectTransform _shakeRoot;
    private Vector2 _shakeApplied;
    private float _screenShakeStart = float.NegativeInfinity;
    private float _screenShakeStrength;
    private Material _flashTemplate;

    // Seen by the Play Mode scenario: the last beats played and the largest lunge offset applied since the reset.
    public readonly List<CombatBeat> PlayedBeats = new List<CombatBeat>();
    public readonly List<float> PlayedBeatTimes = new List<float>();
    public float MaxLungeOffset { get; private set; }
    public int PopupsSpawned { get; private set; }

    public static CombatMotionDirector Ensure(GameObject host) {
        CombatMotionDirector director = host.GetComponent<CombatMotionDirector>();
        return director != null ? director : host.AddComponent<CombatMotionDirector>();
    }

    public void SetLayers(RectTransform popupLayer, RectTransform shakeRoot) {
        _popupLayer = popupLayer;
        if (_shakeRoot != shakeRoot) {
            ClearScreenShake();
            _shakeRoot = shakeRoot;
        }
    }

    // Called by the HUD each time it (re)creates a fighter's image. facesRight: the fighter lunges to the right.
    public void Register(FighterEntity fighter, Image image, bool facesRight) {
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
        }

        actor.FacesRight = facesRight;
        UIStillMotion still = image.GetComponent<UIStillMotion>();
        actor.BreathesItself = still != null && still.enabled;
    }

    public void ResetRecording() {
        PlayedBeats.Clear();
        PlayedBeatTimes.Clear();
        MaxLungeOffset = 0f;
        PopupsSpawned = 0;
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
        ClearScreenShake();
    }

    private void HandleBeat(CombatBeat beat) {
        if (beat == null) {
            return;
        }

        PlayedBeats.Add(beat);
        PlayedBeatTimes.Add(Time.time);
        float now = Time.time;
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
                }
                break;
            case CombatBeatKind.Hit:
                if (TryGetActor(beat.Target, out Actor target)) {
                    bool hpHit = beat.HpLoss > 0;
                    Flash(target, hpHit ? HitFlash : ShieldFlash, now);
                    float ratio = beat.Target.RuntimeMaxHP > 0 ? beat.HpLoss / (float)beat.Target.RuntimeMaxHP : 0f;
                    target.ShakeStart = now;
                    target.ShakeStrength = Mathf.Lerp(6f, 18f, Mathf.Clamp01(ratio * 4f));
                    if (beat.ShieldLoss > 0) {
                        SpawnPopup(target, $"护盾 -{beat.ShieldLoss}", ShieldText, 24);
                    }

                    if (hpHit || beat.ShieldLoss <= 0) {
                        SpawnPopup(target, $"-{beat.HpLoss}", DamageText, beat.Defeated ? 46 : 38);
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
                }
                break;
            case CombatBeatKind.Heal:
                if (TryGetActor(beat.Target, out Actor healed)) {
                    Flash(healed, HealFlash, now);
                    SpawnPopup(healed, $"+{beat.Amount}", UITheme.PositiveOnDark, 32);
                }
                break;
        }
    }

    private void LateUpdate() {
        float now = Time.time;
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
        UpdateScreenShake(now);
    }

    private void Apply(Actor actor, float now) {
        bool alive = actor.Fighter.RuntimeHP > 0;
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
            actor.Material.SetFloat("_FlashAmount", Mathf.Clamp01(flash) * 0.85f);
        }
    }

    // 0 → 1 fast (wind-up and strike), then back to 0.
    public static float LungeCurve(float t) {
        if (t < 0f || t >= 1f) {
            return 0f;
        }

        return t < 0.35f ? Mathf.SmoothStep(0f, 1f, t / 0.35f) : Mathf.SmoothStep(1f, 0f, (t - 0.35f) / 0.65f);
    }

    private void Flash(Actor actor, Color color, float now) {
        actor.FlashColor = color;
        actor.FlashStart = now;
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
        Vector3 head = actor.Rect.TransformPoint(new Vector3(actor.Rect.rect.center.x, actor.Rect.rect.yMax * 0.9f, 0f));
        Vector2 origin = (Vector2)_popupLayer.InverseTransformPoint(head);
        int stacked = _popups.FindAll(p => p.Start > Time.time - 0.25f).Count;
        origin.y -= stacked * 36f;
        rect.anchoredPosition = origin;
        _popups.Add(new Popup { Label = label, Origin = origin, Start = Time.time });
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
    }

    private void ClearScreenShake() {
        if (_shakeRoot != null) {
            _shakeRoot.anchoredPosition -= _shakeApplied;
        }

        _shakeApplied = Vector2.zero;
        _screenShakeStart = float.NegativeInfinity;
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
        if (actor.Image != null && actor.Image.material == actor.Material) {
            actor.Image.material = null;
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
}
