using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// What a use looks like in the backpack (phase 2 M2 batch 4, GDD_01 §5.1A): the item that fires lights up where it lies,
// and a trigger chain sends a spark from module to module along its links. The backpack is Zero's chassis and the
// items are modules on it, so nothing leaves the backpack; the effect itself comes off Zero on the stage
// (CombatMotionDirector). Takes no clicks and cleans itself up.
public class BackpackFx : MonoBehaviour {
    public const float FlareSeconds = 0.42f;
    public const float PulseSeconds = 0.16f;
    private static readonly Color FlareColor = new Color(1f, 0.9f, 0.55f, 0.9f);
    private static readonly Color PulseColor = new Color(0.62f, 0.95f, 1f, 1f);
    private static Sprite _glow;

    private Image _image;
    private RectTransform _rect;
    private Vector3 _from;
    private Vector3 _to;
    private float _start;
    private float _duration;
    private bool _travels;
    private Vector2 _size;

    // Tests and DevTools.
    public static int Played { get; private set; }

    // Lights up the item's tile; false when it is not on screen.
    public static bool Flare(string instanceID) {
        DraggableItemUI tile = DraggableItemUI.Find(instanceID);
        RectTransform rect = tile != null ? tile.transform as RectTransform : null;
        if (rect == null) {
            return false;
        }

        BackpackFx fx = Create(rect.parent, "BackpackFlare", FlareColor);
        Vector2 size = rect.rect.size;
        fx._size = new Vector2(Mathf.Max(80f, size.x), Mathf.Max(80f, size.y)) * 1.15f;
        fx._from = fx._to = WorldCentre(rect);
        fx._duration = FlareSeconds;
        fx.Begin();
        return true;
    }

    // A spark from one module's tile to the next; the next one lights up as it arrives.
    public static bool Pulse(string fromID, string toID) {
        RectTransform from = DraggableItemUI.Find(fromID)?.transform as RectTransform;
        RectTransform to = DraggableItemUI.Find(toID)?.transform as RectTransform;
        if (from == null || to == null) {
            return false;
        }

        BackpackFx fx = Create(to.parent, "BackpackPulse", PulseColor);
        fx._size = new Vector2(70f, 70f);
        fx._from = WorldCentre(from);
        fx._to = WorldCentre(to);
        fx._travels = true;
        fx._duration = PulseSeconds;
        fx.Begin();
        Flare(fromID);
        return true;
    }

    private static Vector3 WorldCentre(RectTransform rect) {
        return rect.TransformPoint(rect.rect.center);
    }

    private static BackpackFx Create(Transform parent, string name, Color color) {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(BackpackFx));
        obj.transform.SetParent(parent, false);
        obj.transform.SetAsLastSibling();
        BackpackFx fx = obj.GetComponent<BackpackFx>();
        fx._image = obj.GetComponent<Image>();
        fx._image.sprite = GlowSprite();
        fx._image.color = color;
        fx._image.raycastTarget = false;
        fx._rect = (RectTransform)obj.transform;
        fx._rect.anchorMin = fx._rect.anchorMax = new Vector2(0.5f, 0.5f);
        fx._rect.pivot = new Vector2(0.5f, 0.5f);
        return fx;
    }

    private void Begin() {
        _start = Time.unscaledTime;
        _rect.sizeDelta = _size;
        _rect.position = _from;
        Played++;
    }

    private void Update() {
        float t = Mathf.Clamp01((Time.unscaledTime - _start) / Mathf.Max(0.01f, _duration));
        if (_travels) {
            _rect.position = Vector3.Lerp(_from, _to, 1f - (1f - t) * (1f - t));
            _rect.sizeDelta = _size * (1.1f - 0.3f * t);
            if (t >= 1f) {
                Destroy(gameObject);
            }

            return;
        }

        // A quick swell, then a fade.
        float swell = t < 0.25f ? t / 0.25f : 1f;
        Color c = _image.color;
        c.a = FlareColor.a * (t < 0.25f ? swell : 1f - (t - 0.25f) / 0.75f);
        _image.color = c;
        _rect.sizeDelta = _size * (0.9f + 0.15f * swell);
        if (t >= 1f) {
            Destroy(gameObject);
        }
    }

    // A soft round glow, made once at runtime.
    private static Sprite GlowSprite() {
        if (_glow != null) {
            return _glow;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "BackpackFxGlow", wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        }

        texture.Apply(false, true);
        _glow = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _glow;
    }
}
