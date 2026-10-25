using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Zero as a paper puppet (docs/specs/2026-10-20-presentation-ceiling.md, puppet sample): on the grid UIPortraitSway
// lays over her portrait, her whole body leans as one chain and her head turns on its neck. She tilts slowly by herself
// and leans a little toward the pointer (she cannot see; she hears you), dips under a hand that pats her and leans with
// a stroke, then springs back with her hair swinging. Held by the hand, she bows toward it and reaches out: a drawn arm
// ("<visualID>_hold", the portrait's canvas with only the changed arm opaque) fades in over her own, or without one her
// clasped hands rise a little. A blush shows while she is touched. Marks on her (UIPuppetPin) ride along. P switches it
// off and on, to compare with the plain sway. Her mouth is left alone: at room size it is 2-4 px, and swapped or
// flapping mouths read as a flicker.
// The chain: from her knees up the body turns a little more the higher it is (full from the waist), so the bend spreads
// over the skirt instead of kinking anywhere; the head turns on top of that by the head weight. Only small angles: a
// flat picture turned further shows its stretch.
// The weights are a registered sprite "<visualID>_puppet" (R = head, B = hands; build_puppet_rig.py, imported like
// the sway rig; G is unused). It bends the sway's grid, so a portrait without a sway rig does not move. Where her neck,
// cheeks, knees and waist are on this portrait comes from its PortraitProfile (配置表(JSON)/Portraits); without one
// the puppet stays off.
// A portrait cut into "<visualID>_hair" and "<visualID>_body" (split_hair_layer.py) keeps her long hanging hair behind
// her: the image shows the hair layer (swaying and following her head by the sway rig), and the body layer, drawn over
// it on the same chain, never bends with the hair, so the edge of her dress stays clean.
[RequireComponent(typeof(Graphic))]
public class UIPortraitPuppet : BaseMeshEffect {
    public const string RigSuffix = "_puppet";
    public const string HoldSuffix = "_hold";
    public const string HairSuffix = "_hair";
    public const string BodySuffix = "_body";
    // Of every tilt her body takes this share and her head the rest; a slow sway of the body of its own on top.
    private const float BodyShare = 0.3f;
    private const float IdleLean = 0.4f;
    private const float IdleLeanPeriod = 5.1f;
    // Her hanging hair is carried sideways as its roots (this share of the width above the neck) move with her head,
    // near her shoulders at once and toward the tips later (the profile's HairLag).
    private const float HairRoot = 0.107f;
    // Lengths are shares of the portrait width (0.016 = 16 px on the 1024 px portrait, 9 px in the studio), angles
    // degrees. Past about 6 degrees of tilt or 0.02 of dip the stretch across her skirt and collar starts to show.
    public const float PatDip = 0.016f;
    public const float LookTilt = 2.5f;
    public const float IdleTilt = 1.2f;
    public const float IdlePeriod = 7.3f;
    public const float MaxStrokeTilt = 6f;
    public const float StrokeTiltPerPixel = 0.06f;
    // Past about 0.04 her forearms visibly shorten and the dress under the hands smears.
    public const float HandLift = 0.03f;
    // Held by the hand she bows: a dip and a tilt toward her hands (on her right, screen left).
    public const float HoldBow = 0.011f;
    public const float HoldTilt = 5f;
    // Her reaching arm fades in fast when you take her hand and out a little slower when you let go.
    private const float HoldPoseIn = 0.12f;
    private const float HoldPoseOut = 0.25f;
    // Head springs: a little bouncy, so a pat lets go with an overshoot. The body follows slower and softer.
    private const float Stiffness = 90f;
    private const float Damping = 11f;
    private const float LeanStiffness = 40f;
    private const float LeanDamping = 9f;

    public static bool Enabled = true;
    private static int _toggledFrame = -1;
    private static readonly Dictionary<Texture2D, Color32[]> PixelCache = new Dictionary<Texture2D, Color32[]>();
    private static Sprite _blushSprite;

    private Texture2D _rig;
    private Color32[] _weights;
    private PortraitProfile _profile;
    private UIPortraitSway _sway;
    private Spring _angle;
    private Spring _lean;
    private Spring _dip;
    private Spring _hairNear;
    private Spring _hairTip;
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
    private Sprite _hairSprite;
    private Sprite _bodySprite;
    private Sprite _wholeSprite;
    private Image _bodyImage;

    public bool HasRig => _weights != null && _profile != null;
    // Her head's whole tilt: the body's lean and its own turn on the neck.
    public float HeadAngle => _angle.Value + _lean.Value;
    public float HeadDip => _dip.Value;
    public bool Pressed => _pressed;
    // On screen and drawn (not hidden under the living clip).
    public bool Showing => isActiveAndEnabled && HasRig && Visible();
    public float HandRise => _hand.Value;
    public bool HasHoldPose => _holdSprite != null;
    public bool HasLayers => _hairSprite != null && _bodySprite != null;
    // How far the drawn reaching arm is faded in (0 to 1).
    public float HoldPoseShown => _holdShown;
    // The furthest a vertex moved in the last mesh, in the portrait's local units.
    public float LastHeadShift { get; private set; }

    // This portrait's points (u, v from the bottom-left).
    private Vector2 Neck => PortraitProfile.Point(_profile.Neck);
    private Vector2 Cheeks => PortraitProfile.Point(_profile.Cheeks);
    private Vector2 HoldAnchor => PortraitProfile.Point(_profile.HoldAnchor);
    private float KneeV => _profile.KneeV;
    private float WaistV => _profile.WaistV;

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
        PortraitProfile profile = PortraitProfile.Find(visualID);
        if (rig != null && profile == null) {
            Debug.LogWarning($"[UIPortraitPuppet] {visualID} has a puppet rig but no PortraitProfile (配置表(JSON)/Portraits); it stays still.");
        }

        if (rig == null || !rig.isReadable || profile == null) {
            if (puppet != null) {
                puppet.ClearLayers();
                puppet.enabled = false;
            }

            return;
        }

        if (puppet == null) {
            puppet = image.gameObject.AddComponent<UIPortraitPuppet>();
        }

        puppet.SetRig(rig);
        puppet._profile = profile;
        VisualAssetService.TryGetSprite(visualID + HoldSuffix, out Sprite hold);
        puppet._holdSprite = hold;
        VisualAssetService.TryGetSprite(visualID + HairSuffix, out Sprite hair);
        VisualAssetService.TryGetSprite(visualID + BodySuffix, out Sprite body);
        puppet._hairSprite = hair;
        puppet._bodySprite = body;
        puppet._wholeSprite = image.sprite;
        puppet.enabled = true;
        puppet.ShowLayers(true);
    }

    // The image shows the hair layer while the body layer is drawn over it, and the whole portrait otherwise.
    private void ShowLayers(bool layered) {
        Image image = graphic as Image;
        if (image == null || !HasLayers) {
            return;
        }

        Sprite wanted = layered ? _hairSprite : _wholeSprite;
        if (wanted != null && image.sprite != wanted && (image.sprite == _hairSprite || image.sprite == _wholeSprite)) {
            image.sprite = wanted;
        }

        if (!layered && _bodyImage != null) {
            _bodyImage.enabled = false;
        }
    }

    private void ClearLayers() {
        ShowLayers(false);
        _hairSprite = null;
        _bodySprite = null;
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
                _angle.Velocity += (Time.frameCount & 1) == 0 ? -110f : 110f;
                _dip.Velocity += 0.1f;
                _hairBoost = 1f;
                break;
            case ZeroReaction.Lean:
                // Toward the middle of the screen, where you are.
                Vector3 screen = RectTransformUtility.WorldToScreenPoint(CanvasCamera(), transform.position);
                _leanTilt = screen.x < Screen.width * 0.5f ? -5f : 5f;
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
    // anchor, and how far it turns with her.
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
        ShowLayers(true);
    }

    protected override void OnDisable() {
        if (_sway != null) {
            _sway.Boost = 0f;
        }

        ShowOverlays(false);
        ShowLayers(false);
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
        float tilt = live
            ? IdleTilt * Mathf.Sin((now + _phase) / IdlePeriod * Mathf.PI * 2f) - LookTilt * look + _strokeTilt + (now < _leanUntil ? _leanTilt : 0f)
                + (holding ? HoldTilt : 0f)
            : 0f;
        float idleLean = live ? IdleLean * Mathf.Sin((now + _phase) / IdleLeanPeriod * Mathf.PI * 2f + 1f) : 0f;
        float dipTarget = live && _pressed ? PatDip : holding ? HoldBow : 0f;
        _lean.Step(BodyShare * tilt + idleLean, dt, LeanStiffness, LeanDamping);
        _angle.Step((1f - BodyShare) * tilt, dt, Stiffness, Damping);
        _dip.Step(dipTarget, dt, Stiffness, Damping);
        // The hair's roots move with her head; the hair near her shoulders follows at once, its tips later.
        float root = -Mathf.Sin(HeadAngle * Mathf.Deg2Rad) * HairRoot;
        _hairNear.Step(root, dt, 45f, 7f);
        _hairTip.Step(root, dt, 14f, 3.2f);
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
        if (_bodyImage != null && _bodyImage.enabled) {
            _bodyImage.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh) {
        int count = vh.currentVertCount;
        if (!IsActive() || !HasRig || count <= 4) {
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
            Vector2 after = MoveParts(before, uv, out _, true);
            vertex.position = new Vector3(after.x, after.y, vertex.position.z);
            vh.SetUIVertex(vertex, i);
            most = Mathf.Max(most, (after - before).magnitude);
        }

        LastHeadShift = most;
    }

    // Where the point (u, v) of the body layer is drawn right now, or false before the image has its grid.
    public bool TryBodyPoint(Vector2 uv, out Vector2 point) {
        point = default;
        if (!_hasGrid) {
            return false;
        }

        point = MoveParts(Point(uv), uv, out _, false);
        return true;
    }

    // A point of her body (uv from the bottom-left) with everything that moves it: the breath (already in the grid
    // corners), the sway (when the hair is not a layer of its own) and the chain.
    private Vector2 Carry(Vector2 uv, out float angle) {
        Vector2 p = Point(uv);
        if (!HasLayers && _sway != null && _sway.isActiveAndEnabled) {
            p.x += _sway.BendAt(uv.x, uv.y, _posMax.x - _posMin.x);
        }

        return MoveParts(p, uv, out angle, false);
    }

    // The chain at p (uv from the bottom-left): hands lifted, the body settled by the dip and leaned, the head turned on
    // the leaned neck, and with hair the hanging hair carried after the head. angle is how far the point turned.
    private Vector2 MoveParts(Vector2 p, Vector2 uv, out float angle, bool hair) {
        Sample(uv, out float head, out float hands);
        float width = _posMax.x - _posMin.x;
        p.y += _hand.Value * width * hands;
        Vector2 q = MoveBody(p, uv, out float lean);
        angle = lean;
        if (head > 0.001f) {
            Vector2 neck = MoveBody(Point(Neck), Neck, out _);
            float turn = _angle.Value * head;
            q = neck + Rotate(q - neck, turn);
            angle += turn;
        }

        if (hair && _sway != null) {
            float weight = _sway.HairWeight(uv.x, uv.y);
            float lag = Smooth(_profile.HairLag[0], _profile.HairLag[1], uv.y);
            q.x += weight * Mathf.Lerp(_hairNear.Value, _hairTip.Value, lag) * width;
        }

        return q;
    }

    // Her body from the knees up, settled by the dip (the head the whole of it) and leaned around her spine.
    private Vector2 MoveBody(Vector2 p, Vector2 uv, out float lean) {
        float width = _posMax.x - _posMin.x;
        float height = _posMax.y - _posMin.y;
        p.y -= _dip.Value * width * Smooth(KneeV, Neck.y, uv.y);
        lean = _lean.Value * Smooth(KneeV, WaistV, uv.y);
        float a = lean * Mathf.Deg2Rad;
        float spine = Mathf.Lerp(_posMin.x, _posMax.x, Neck.x);
        float dx = p.x - spine;
        // The spine at this height has been carried sideways by every turn below it (small angles: sin a = a).
        float carried = -_lean.Value * Mathf.Deg2Rad * Climb(uv.y) * height;
        return new Vector2(spine + carried + dx * Mathf.Cos(a), p.y + dx * Mathf.Sin(a));
    }

    // The integral of the lean profile from the knees up to v (in v units).
    private float Climb(float v) {
        if (v <= KneeV) {
            return 0f;
        }

        float span = WaistV - KneeV;
        if (v >= WaistV) {
            return span * 0.5f + (v - WaistV);
        }

        float t = (v - KneeV) / span;
        return span * (t * t * t - t * t * t * t * 0.5f);
    }

    private static float Smooth(float edge0, float edge1, float x) {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    private static Vector2 Rotate(Vector2 v, float degrees) {
        float a = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(a);
        float sin = Mathf.Sin(a);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
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

    private void Sample(Vector2 uv, out float head, out float hands) {
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
        UpdateBody();
        if (!live || !_hasGrid) {
            ShowOverlays(false);
            return;
        }

        EnsureOverlays();
        float width = _posMax.x - _posMin.x;
        Place(_blushImage, Cheeks, new Vector2(width * 0.11f, width * 0.03f));
        _blushImage.color = new Color(1f, 0.42f, 0.48f, 0.45f * Mathf.SmoothStep(0f, 1f, _blush));
        _blushImage.enabled = _blush > 0.01f;

        // The drawn arm covers the whole portrait (it is the portrait's canvas), carried and turned at the point on the
        // arm.
        _holdImage.enabled = _holdSprite != null && _holdShown > 0.01f;
        if (_holdImage.enabled) {
            Rect rect = ((RectTransform)transform).rect;
            RectTransform t = _holdImage.rectTransform;
            _holdImage.sprite = _holdSprite;
            t.pivot = HoldAnchor;
            t.anchoredPosition = Carry(HoldAnchor, out float angle) - rect.min;
            t.sizeDelta = _posMax - _posMin;
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            _holdImage.color = new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, _holdShown));
        }
    }

    // The body layer, over the hair: drawn whenever the portrait is (also still, with the puppet switched off), never
    // over the living clip. It fills the portrait's rect like the image itself, so its grid lands on the same points.
    private void UpdateBody() {
        bool show = HasLayers && Visible();
        if (!show) {
            if (_bodyImage != null) {
                _bodyImage.enabled = false;
            }

            return;
        }

        if (_bodyImage == null) {
            _bodyImage = CreateOverlay("PuppetBody", null);
            // Over the hair (and the clip's layer, which hides it), under the blush, the reaching arm and any marks.
            Transform clip = transform.Find("MotionPortrait_Video");
            _bodyImage.transform.SetSiblingIndex(clip != null ? clip.GetSiblingIndex() + 1 : 0);
            UIPuppetLayer layer = _bodyImage.gameObject.AddComponent<UIPuppetLayer>();
            layer.Owner = this;
        }

        RectTransform t = _bodyImage.rectTransform;
        t.anchorMin = Vector2.zero;
        t.anchorMax = Vector2.one;
        t.pivot = ((RectTransform)transform).pivot;
        t.offsetMin = Vector2.zero;
        t.offsetMax = Vector2.zero;
        t.localRotation = Quaternion.identity;
        _bodyImage.sprite = _bodySprite;
        _bodyImage.preserveAspect = graphic is Image image && image.preserveAspect;
        _bodyImage.color = graphic.color;
        _bodyImage.enabled = true;
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

// The body layer of a puppet whose hair is a layer of its own: a grid like the sway's, every vertex put where the
// puppet's chain carries that point of her body (without the hair's sway).
[RequireComponent(typeof(Graphic))]
public class UIPuppetLayer : BaseMeshEffect {
    public UIPortraitPuppet Owner;

    public override void ModifyMesh(VertexHelper vh) {
        if (!IsActive() || Owner == null || vh.currentVertCount != 4) {
            return;
        }

        UIPortraitSway.BuildGrid(vh, (uv, rest) => Owner.TryBodyPoint(uv, out Vector2 moved) ? (Vector3)moved : rest);
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
