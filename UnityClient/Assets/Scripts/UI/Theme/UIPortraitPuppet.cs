using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Zero as a paper puppet (docs/specs/2026-10-20-presentation-ceiling.md, puppet sample): on the grid UIPortraitSway
// lays over her portrait, her head moves on its neck. It tilts slowly by itself and turns a little toward the pointer
// (she cannot see; she hears you), dips under a hand that pats her and leans with a stroke, then springs back with her
// hair swinging. Held by the hand, she bows her head toward it and reaches out: a drawn arm ("<visualID>_hold", the
// portrait's canvas with only the changed arm opaque) fades in over her own, or without one her clasped hands rise a
// little. A blush shows
// while she is touched. Marks on her (UIPuppetPin) ride along. P switches it off and on, to compare with the plain
// sway. Her mouth is left alone: at room size it is 2-4 px, and swapped or flapping mouths read as a flicker.
// The weights are a registered sprite "<visualID>_puppet" (R = head, G = face, B = hands; build_puppet_rig.py,
// imported like the sway rig). It bends the sway's grid, so a portrait without a sway rig does not move.
[RequireComponent(typeof(Graphic))]
public class UIPortraitPuppet : BaseMeshEffect {
    public const string RigSuffix = "_puppet";
    public const string HoldSuffix = "_hold";
    // Where her head turns and the middle of her cheeks, as (u, v) of doll_proto_0_stand from the bottom-left
    // (build_puppet_rig.py prints the neck).
    public static readonly Vector2 Neck = new Vector2(0.503f, 0.801f);
    private static readonly Vector2 Cheeks = new Vector2(0.511f, 0.855f);
    // Lengths are shares of the portrait width (0.016 = 16 px on the 1024 px portrait, 9 px in the studio), angles
    // degrees. Past about 10 degrees or 0.02 the cape under her chin starts to stretch visibly.
    public const float PatDip = 0.016f;
    public const float LookShift = 0.012f;
    public const float LookTilt = 2.5f;
    public const float IdleTilt = 1.2f;
    public const float IdlePeriod = 7.3f;
    public const float MaxStrokeTilt = 9f;
    public const float StrokeTiltPerPixel = 0.06f;
    // Past about 0.04 her forearms visibly shorten and the dress under the hands smears.
    public const float HandLift = 0.03f;
    // Held by the hand she bows: a dip and a tilt toward her hands (on her right, screen left).
    public const float HoldBow = 0.011f;
    public const float HoldTilt = 5f;
    // Her reaching arm fades in fast when you take her hand and out a little slower when you let go.
    private const float HoldPoseIn = 0.12f;
    private const float HoldPoseOut = 0.25f;
    // A point on the reaching arm (u, v from the bottom-left): the drawn arm moves with her there (breath, sway).
    private static readonly Vector2 HoldAnchor = new Vector2(0.4f, 0.62f);
    // Head springs: a little bouncy, so a pat lets go with an overshoot.
    private const float Stiffness = 90f;
    private const float Damping = 11f;

    public static bool Enabled = true;
    private static int _toggledFrame = -1;
    private static readonly Dictionary<Texture2D, Color32[]> PixelCache = new Dictionary<Texture2D, Color32[]>();
    private static Sprite _blushSprite;

    private Texture2D _rig;
    private Color32[] _weights;
    private UIPortraitSway _sway;
    private Spring _angle;
    private Spring _dip;
    private Spring _look;
    private float _strokeTilt;
    private float _leanTilt;
    private float _leanUntil;
    private float _hairBoost;
    private float _blush;
    private Spring _hand;
    private bool _holding;
    private float _phase;
    private bool _pressed;
    private bool _hasGrid;
    private Vector2 _uvMin;
    private Vector2 _uvMax;
    private Vector2 _posMin;
    private Vector2 _posMax;
    private Image _blushImage;
    private Sprite _holdSprite;
    private Image _holdImage;
    private float _holdShown;

    public bool HasRig => _weights != null;
    public float HeadAngle => _angle.Value;
    public float HeadDip => _dip.Value;
    public bool Pressed => _pressed;
    // On screen and drawn (not hidden under the living clip).
    public bool Showing => isActiveAndEnabled && HasRig && Visible();
    public float HandRise => _hand.Value;
    public bool HasHoldPose => _holdSprite != null;
    // How far the drawn reaching arm is faded in (0 to 1).
    public float HoldPoseShown => _holdShown;
    // The furthest a vertex of her head moved in the last mesh, in the portrait's local units.
    public float LastHeadShift { get; private set; }

    // Called by VisualUIHelper whenever an image gets a sprite, after the sway.
    public static void Sync(Image image, string visualID) {
        if (image == null) {
            return;
        }

        Texture2D rig = null;
        if (!string.IsNullOrEmpty(visualID) && VisualAssetService.TryGetSprite(visualID + RigSuffix, out Sprite rigSprite)) {
            rig = rigSprite.texture;
        }

        UIPortraitPuppet puppet = image.GetComponent<UIPortraitPuppet>();
        if (rig == null || !rig.isReadable) {
            if (puppet != null) {
                puppet.enabled = false;
            }

            return;
        }

        if (puppet == null) {
            puppet = image.gameObject.AddComponent<UIPortraitPuppet>();
        }

        puppet.SetRig(rig);
        VisualAssetService.TryGetSprite(visualID + HoldSuffix, out Sprite hold);
        puppet._holdSprite = hold;
        puppet.enabled = true;
    }

    // The working puppet on a portrait, or null.
    public static UIPortraitPuppet On(Component portrait) {
        UIPortraitPuppet puppet = portrait != null ? portrait.GetComponent<UIPortraitPuppet>() : null;
        return puppet != null && puppet.isActiveAndEnabled && puppet.HasRig ? puppet : null;
    }

    public void SetRig(Texture2D rig) {
        _rig = rig;
        if (!PixelCache.TryGetValue(rig, out _weights)) {
            _weights = rig.GetPixels32();
            PixelCache[rig] = _weights;
        }
    }

    // A hand on her head (down) and off it.
    public void Press(bool down) {
        _pressed = down && Enabled;
        if (!down) {
            _hairBoost = Mathf.Max(_hairBoost, 0.9f);
        }
    }

    // Your hand holding hers (down) and letting go.
    public void HoldHand(bool down) {
        _holding = down && Enabled;
    }

    // The hand moving across her head, dx screen pixels: she leans with it.
    public void Stroke(float dx) {
        if (!Enabled) {
            return;
        }

        _strokeTilt = Mathf.Clamp(_strokeTilt - dx * StrokeTiltPerPixel, -MaxStrokeTilt, MaxStrokeTilt);
        _hairBoost = Mathf.Max(_hairBoost, 0.4f);
    }

    public void React(ZeroReaction reaction) {
        if (!Enabled) {
            return;
        }

        switch (reaction) {
            case ZeroReaction.Hop:
                _dip.Velocity -= 0.12f;
                _hairBoost = 1f;
                break;
            case ZeroReaction.Flinch:
                _angle.Velocity += (Time.frameCount & 1) == 0 ? -160f : 160f;
                _dip.Velocity += 0.1f;
                _hairBoost = 1f;
                break;
            case ZeroReaction.Lean:
                // Toward the middle of the screen, where you are.
                Vector3 screen = RectTransformUtility.WorldToScreenPoint(CanvasCamera(), transform.position);
                _leanTilt = screen.x < Screen.width * 0.5f ? -6f : 6f;
                _leanUntil = Time.unscaledTime + 1.6f;
                _blush = 1f;
                break;
        }
    }

    // A small nod as she starts to speak.
    public void Talk() {
        if (!Enabled) {
            return;
        }

        _dip.Velocity += 0.05f;
    }

    // A pat played without a hand (P3DevTools.Pat and the Play Mode check): press, two strokes across, let go.
    public IEnumerator PlayPat(float seconds = 1.8f) {
        Press(true);
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) {
            Stroke(Mathf.Sin(t / seconds * Mathf.PI * 4f) * 900f * Time.unscaledDeltaTime);
            yield return null;
        }

        Press(false);
    }

    // Where a mark anchored at spot (a fraction of the portrait's rect) is carried to right now, as an offset from its
    // anchor, and how far it turns with her head.
    public bool TryFollow(Vector2 spot, out Vector2 offset, out float angle) {
        offset = Vector2.zero;
        angle = 0f;
        Rect rect = ((RectTransform)transform).rect;
        Rect quad = RestQuad(rect);
        if (!_hasGrid || quad.width <= 0f || quad.height <= 0f) {
            return false;
        }

        Vector2 rest = rect.min + Vector2.Scale(spot, rect.size);
        Vector2 uv = new Vector2((rest.x - quad.xMin) / quad.width, (rest.y - quad.yMin) / quad.height);
        Vector2 moved = Carry(uv, out angle);
        offset = moved - rest;
        return true;
    }

    protected override void OnEnable() {
        base.OnEnable();
        // Not UnityEngine.Random: drawing from it would shift the game's own rolls (loot, fights).
        _phase = (GetInstanceID() & 1023) / 1023f * IdlePeriod;
    }

    protected override void OnDisable() {
        if (_sway != null) {
            _sway.Boost = 0f;
        }

        ShowOverlays(false);
        _hasGrid = false;
        base.OnDisable();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.P) && _toggledFrame != Time.frameCount) {
            _toggledFrame = Time.frameCount;
            Enabled = !Enabled;
        }

        if (_sway == null) {
            _sway = GetComponent<UIPortraitSway>();
        }

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float now = Time.unscaledTime;
        bool live = Enabled && Visible();
        if (!live) {
            _pressed = false;
            _holding = false;
        }

        bool holding = live && _holding;
        float look = live ? (holding ? -0.5f : LookTarget()) : 0f;
        _strokeTilt = Mathf.MoveTowards(_strokeTilt, 0f, dt * (_pressed ? 6f : 14f));
        float angleTarget = live
            ? IdleTilt * Mathf.Sin((now + _phase) / IdlePeriod * Mathf.PI * 2f) - LookTilt * look + _strokeTilt + (now < _leanUntil ? _leanTilt : 0f)
                + (holding ? HoldTilt : 0f)
            : 0f;
        float dipTarget = live && _pressed ? PatDip : holding ? HoldBow : 0f;
        _angle.Step(angleTarget, dt, Stiffness, Damping);
        _dip.Step(dipTarget, dt, Stiffness, Damping);
        _look.Step(look * LookShift, dt, 30f, 11f);
        _hairBoost = Mathf.MoveTowards(_hairBoost, 0f, dt * 0.8f);
        _hand.Step(holding && _holdSprite == null ? HandLift : 0f, dt, 60f, 10f);
        bool reaching = holding && _holdSprite != null;
        _holdShown = Mathf.MoveTowards(_holdShown, reaching ? 1f : 0f, dt / (reaching ? HoldPoseIn : HoldPoseOut));
        bool touched = live && (_pressed || _holding);
        _blush = Mathf.MoveTowards(_blush, touched ? 1f : 0f, dt * (touched ? 1.5f : 0.35f));
        if (_sway != null) {
            _sway.Boost = _hairBoost;
        }

        UpdateOverlays(live);
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh) {
        int count = vh.currentVertCount;
        if (!IsActive() || _weights == null || count <= 4) {
            _hasGrid = false;
            return;
        }

        // The grid's bottom-left and top-right vertices (smallest and largest uv) fix where the portrait is drawn;
        // the sway never bends the corners.
        UIVertex vertex = default;
        float low = float.MaxValue;
        float high = float.MinValue;
        for (int i = 0; i < count; i++) {
            vh.PopulateUIVertex(ref vertex, i);
            float sum = vertex.uv0.x + vertex.uv0.y;
            if (sum < low) {
                low = sum;
                _uvMin = vertex.uv0;
                _posMin = vertex.position;
            }

            if (sum > high) {
                high = sum;
                _uvMax = vertex.uv0;
                _posMax = vertex.position;
            }
        }

        _hasGrid = _uvMax.x > _uvMin.x && _uvMax.y > _uvMin.y;
        if (!_hasGrid) {
            return;
        }

        float most = 0f;
        for (int i = 0; i < count; i++) {
            vh.PopulateUIVertex(ref vertex, i);
            Vector2 uv = new Vector2((vertex.uv0.x - _uvMin.x) / (_uvMax.x - _uvMin.x), (vertex.uv0.y - _uvMin.y) / (_uvMax.y - _uvMin.y));
            Vector2 before = vertex.position;
            Vector2 after = MoveParts(before, uv, out _);
            vertex.position = new Vector3(after.x, after.y, vertex.position.z);
            vh.SetUIVertex(vertex, i);
            most = Mathf.Max(most, (after - before).magnitude);
        }

        LastHeadShift = most;
    }

    // A point of the portrait (uv from the bottom-left) with everything that moves it: the breath (already in the
    // grid corners), the sway and the head.
    private Vector2 Carry(Vector2 uv, out float angle) {
        Vector2 p = Point(uv);
        if (_sway != null && _sway.isActiveAndEnabled) {
            p.x += _sway.BendAt(uv.x, uv.y, _posMax.x - _posMin.x);
        }

        return MoveParts(p, uv, out angle);
    }

    private Vector2 MoveParts(Vector2 p, Vector2 uv, out float angle) {
        Sample(uv, out float head, out float face, out float hands);
        angle = _angle.Value * head;
        float width = _posMax.x - _posMin.x;
        p.y += _hand.Value * width * hands;
        if (head <= 0.001f) {
            return p;
        }

        Vector2 neck = Point(Neck);
        float a = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(a);
        float sin = Mathf.Sin(a);
        float dx = p.x - neck.x;
        float dy = p.y - neck.y;
        // A dip also squashes the head a touch toward the neck, as if she ducks under the hand.
        float squash = 1f - 1.5f * _dip.Value * head;
        Vector2 q = new Vector2(neck.x + cos * dx - sin * dy, neck.y + (sin * dx + cos * dy) * squash);
        q.x += _look.Value * width * (0.3f * head + 0.7f * face);
        q.y -= _dip.Value * width * head;
        return q;
    }

    private Vector2 Point(Vector2 uv) {
        return new Vector2(Mathf.Lerp(_posMin.x, _posMax.x, uv.x), Mathf.Lerp(_posMin.y, _posMax.y, uv.y));
    }

    // Where the sprite sits in the rect before any motion (Image's preserve-aspect fit, placed by the pivot).
    private Rect RestQuad(Rect rect) {
        Image image = graphic as Image;
        if (image == null || image.sprite == null || !image.preserveAspect) {
            return rect;
        }

        float spriteRatio = image.sprite.rect.width / image.sprite.rect.height;
        float rectRatio = rect.width / rect.height;
        Vector2 pivot = ((RectTransform)transform).pivot;
        if (rectRatio > spriteRatio) {
            float width = rect.height * spriteRatio;
            rect.x += (rect.width - width) * pivot.x;
            rect.width = width;
        } else {
            float height = rect.width / spriteRatio;
            rect.y += (rect.height - height) * pivot.y;
            rect.height = height;
        }

        return rect;
    }

    private void Sample(Vector2 uv, out float head, out float face, out float hands) {
        int w = _rig.width;
        int h = _rig.height;
        float x = Mathf.Clamp(uv.x * (w - 1), 0f, w - 1);
        float y = Mathf.Clamp(uv.y * (h - 1), 0f, h - 1);
        int x0 = (int)x;
        int y0 = (int)y;
        int x1 = Mathf.Min(x0 + 1, w - 1);
        int y1 = Mathf.Min(y0 + 1, h - 1);
        float tx = x - x0;
        float ty = y - y0;
        Color32 a = _weights[y0 * w + x0];
        Color32 b = _weights[y0 * w + x1];
        Color32 c = _weights[y1 * w + x0];
        Color32 d = _weights[y1 * w + x1];
        head = Mathf.Lerp(Mathf.Lerp(a.r, b.r, tx), Mathf.Lerp(c.r, d.r, tx), ty) / 255f;
        face = Mathf.Lerp(Mathf.Lerp(a.g, b.g, tx), Mathf.Lerp(c.g, d.g, tx), ty) / 255f;
        hands = Mathf.Lerp(Mathf.Lerp(a.b, b.b, tx), Mathf.Lerp(c.b, d.b, tx), ty) / 255f;
    }

    // -1 (pointer far to her left) to 1 (far to her right); 0 when it is nowhere near her.
    private float LookTarget() {
        if (!_hasGrid || !Application.isFocused) {
            return 0f;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, Input.mousePosition, CanvasCamera(), out Vector2 local)) {
            return 0f;
        }

        Vector2 neck = Point(Neck);
        float width = _posMax.x - _posMin.x;
        Vector2 d = local - neck;
        if (Mathf.Abs(d.x) > width * 2f || Mathf.Abs(d.y) > width * 1.6f) {
            return 0f;
        }

        return Mathf.Clamp(d.x / (width * 0.8f), -1f, 1f);
    }

    private Camera CanvasCamera() {
        Canvas canvas = graphic.canvas;
        return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    // The still can be hidden under the living clip (UIMotionPortrait); then nothing of the puppet shows.
    private bool Visible() {
        return graphic.color.a > 0.01f && graphic.canvasRenderer.GetAlpha() > 0.01f;
    }

    private void UpdateOverlays(bool live) {
        if (!live || !_hasGrid) {
            ShowOverlays(false);
            return;
        }

        EnsureOverlays();
        float width = _posMax.x - _posMin.x;
        Place(_blushImage, Cheeks, new Vector2(width * 0.11f, width * 0.03f));
        _blushImage.color = new Color(1f, 0.42f, 0.48f, 0.45f * Mathf.SmoothStep(0f, 1f, _blush));
        _blushImage.enabled = _blush > 0.01f;

        // The drawn arm covers the whole portrait (it is the portrait's canvas), carried by the point on the arm.
        _holdImage.enabled = _holdSprite != null && _holdShown > 0.01f;
        if (_holdImage.enabled) {
            Rect rect = ((RectTransform)transform).rect;
            Vector2 size = _posMax - _posMin;
            Vector2 centre = Carry(HoldAnchor, out _) + Vector2.Scale(new Vector2(0.5f, 0.5f) - HoldAnchor, size);
            RectTransform t = _holdImage.rectTransform;
            _holdImage.sprite = _holdSprite;
            t.anchoredPosition = centre - rect.min;
            t.sizeDelta = size;
            t.localRotation = Quaternion.identity;
            _holdImage.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, _holdShown));
        }
    }

    private void Place(Image overlay, Vector2 uv, Vector2 size) {
        Rect rect = ((RectTransform)transform).rect;
        Vector2 at = Carry(uv, out float angle);
        RectTransform t = overlay.rectTransform;
        t.anchoredPosition = at - rect.min;
        t.sizeDelta = size;
        t.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void EnsureOverlays() {
        if (_holdImage == null) {
            _holdImage = CreateOverlay("PuppetHold", null);
        }

        if (_blushImage == null) {
            _blushImage = CreateOverlay("PuppetBlush", BlushSprite());
        }
    }

    private void ShowOverlays(bool show) {
        if (_blushImage != null) {
            _blushImage.enabled = show;
        }

        if (_holdImage != null) {
            _holdImage.enabled = show;
        }
    }

    private Image CreateOverlay(string name, Sprite sprite) {
        Transform existing = transform.Find(name);
        Image overlay = existing != null ? existing.GetComponent<Image>() : null;
        if (overlay == null) {
            GameObject host = new GameObject(name, typeof(RectTransform), typeof(Image));
            host.transform.SetParent(transform, false);
            overlay = host.GetComponent<Image>();
        }

        overlay.sprite = sprite;
        overlay.raycastTarget = false;
        RectTransform t = overlay.rectTransform;
        t.anchorMin = t.anchorMax = Vector2.zero;
        t.pivot = new Vector2(0.5f, 0.5f);
        return overlay;
    }

    // Two soft pink patches, one under each eye.
    private static Sprite BlushSprite() {
        if (_blushSprite == null) {
            _blushSprite = DrawSprite(128, 32, (x, y) => {
                float left = Ellipse(x - 0.22f, y - 0.5f, 0.2f, 0.42f);
                float right = Ellipse(x - 0.78f, y - 0.5f, 0.2f, 0.42f);
                return Mathf.Max(left, right);
            });
        }

        return _blushSprite;
    }

    private static float Ellipse(float dx, float dy, float rx, float ry) {
        float d = Mathf.Sqrt(dx * dx / (rx * rx) + dy * dy / (ry * ry));
        return 1f - Mathf.SmoothStep(0f, 1f, d);
    }

    private static Sprite DrawSprite(int width, int height, System.Func<float, float, float> alpha) {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha((x + 0.5f) / width, (y + 0.5f) / height));
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f));
    }

    private struct Spring {
        public float Value;
        public float Velocity;

        public void Step(float target, float dt, float stiffness, float damping) {
            Velocity += (stiffness * (target - Value) - damping * Velocity) * dt;
            Value += Velocity * dt;
        }
    }
}

// Something drawn on a puppet portrait (a smudge, a crack, the haze), anchored at its spot on her: it rides along
// when she moves, so it stays on the same fold of her cape.
public class UIPuppetPin : MonoBehaviour {
    private UIPortraitPuppet _puppet;

    public static void Attach(Graphic mark) {
        if (mark != null && mark.GetComponent<UIPuppetPin>() == null) {
            mark.gameObject.AddComponent<UIPuppetPin>();
        }
    }

    private void LateUpdate() {
        RectTransform rect = (RectTransform)transform;
        if (_puppet == null && transform.parent != null) {
            _puppet = transform.parent.GetComponent<UIPortraitPuppet>();
        }

        if (UIPortraitPuppet.Enabled && _puppet != null && _puppet.isActiveAndEnabled && _puppet.TryFollow(rect.anchorMin, out Vector2 offset, out float angle)) {
            rect.anchoredPosition = offset;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        } else {
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
        }
    }
}
