using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Live2D-lite for one still portrait: the image's quad is split into a grid and its vertices bend sideways by a
// weight map, so long hair sways (a wave running down to the tips) and the skirt hem stirs, while the head, hands
// and feet stay put. Pixels stretch instead of tearing, and the RectTransform never moves.
// The weight map is a registered sprite "<visualID>_rig" in Approved (R = hair, G = skirt; built by
// tools/美术工具/build_portrait_rig.py, imported Read/Write, uncompressed, not sRGB). Portraits without one stay still.
[RequireComponent(typeof(Graphic))]
public class UIPortraitSway : BaseMeshEffect {
    public const string RigSuffix = "_rig";
    // Fine enough for a head turning on its neck (UIPortraitPuppet bends the same grid), and for the hair beside her
    // arms to sway without a cell dragging the arm's edge along (16 px cells on a 1024 px portrait).
    public const int Columns = 64;
    public const int Rows = 96;
    // Peak sideways bend as a share of the image width (about 25 px on a 1024 px portrait, 14 px in the workshop).
    // The producer found 10 px too faint; past about 30 px the cape and dress edges start to drag with the hair.
    public const float HairAmount = 0.024f;
    public const float SkirtAmount = 0.007f;
    public const float Period = 3.8f;

    private static readonly Dictionary<Texture2D, Color32[]> PixelCache = new Dictionary<Texture2D, Color32[]>();

    private Texture2D _rig;
    private Color32[] _weights;
    private float _phase;

    // Extra hair swing on top of the idle sway (1 = twice as far); UIPortraitPuppet raises it when she moves.
    public float Boost;

    public bool HasRig => _weights != null;
    public float LastMaxBend { get; private set; }

    // Called by VisualUIHelper whenever an image gets a sprite.
    public static void Sync(Image image, string visualID) {
        if (image == null) {
            return;
        }

        Texture2D rig = null;
        if (!string.IsNullOrEmpty(visualID) && VisualAssetService.TryGetSprite(visualID + RigSuffix, out Sprite rigSprite)) {
            rig = rigSprite.texture;
        }

        UIPortraitSway sway = image.GetComponent<UIPortraitSway>();
        if (rig == null || !rig.isReadable) {
            if (sway != null) {
                sway.enabled = false;
            }

            return;
        }

        if (sway == null) {
            sway = image.gameObject.AddComponent<UIPortraitSway>();
        }

        sway.SetRig(rig);
        sway.enabled = true;
    }

    public void SetRig(Texture2D rig) {
        _rig = rig;
        if (!PixelCache.TryGetValue(rig, out _weights)) {
            _weights = rig.GetPixels32();
            PixelCache[rig] = _weights;
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        _phase = Random.value * Period;
    }

    private void Update() {
        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh) {
        if (!IsActive() || _weights == null || vh.currentVertCount != 4) {
            return;
        }

        // Simple image quad: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
        UIVertex[] corners = new UIVertex[4];
        for (int i = 0; i < 4; i++) {
            vh.PopulateUIVertex(ref corners[i], i);
        }

        float width = Vector3.Distance(corners[0].position, corners[3].position);
        vh.Clear();
        float maxBend = 0f;
        for (int row = 0; row <= Rows; row++) {
            float v = row / (float)Rows;
            for (int column = 0; column <= Columns; column++) {
                float u = column / (float)Columns;
                UIVertex vertex = Lerp(corners, u, v);
                float bend = BendAt(u, v, width);
                vertex.position.x += bend;
                maxBend = Mathf.Max(maxBend, Mathf.Abs(bend));
                vh.AddVert(vertex);
            }
        }

        int stride = Columns + 1;
        for (int row = 0; row < Rows; row++) {
            for (int column = 0; column < Columns; column++) {
                int i = row * stride + column;
                vh.AddTriangle(i, i + stride, i + stride + 1);
                vh.AddTriangle(i + stride + 1, i + 1, i);
            }
        }

        LastMaxBend = maxBend;
    }

    // How far the point (u, v) of the portrait is bent sideways right now, for a portrait drawn width wide.
    public float BendAt(float u, float v, float width) {
        if (_weights == null) {
            return 0f;
        }

        float wave = (Time.unscaledTime + _phase) / Period * Mathf.PI * 2f;
        SampleWeights(u, v, out float hair, out float skirt);
        float fromTop = 1f - v;
        return width * (HairAmount * (1f + Boost) * hair * Mathf.Sin(wave - 3f * fromTop)
            + SkirtAmount * skirt * Mathf.Sin(1.3f * wave + 1f));
    }

    // Weights at (u, v) of the portrait, v up; the rig texture is stored v up too (Unity's GetPixels order).
    public void SampleWeights(float u, float v, out float hair, out float skirt) {
        int w = _rig.width;
        int h = _rig.height;
        float x = Mathf.Clamp(u * (w - 1), 0f, w - 1);
        float y = Mathf.Clamp(v * (h - 1), 0f, h - 1);
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
        hair = Mathf.Lerp(Mathf.Lerp(a.r, b.r, tx), Mathf.Lerp(c.r, d.r, tx), ty) / 255f;
        skirt = Mathf.Lerp(Mathf.Lerp(a.g, b.g, tx), Mathf.Lerp(c.g, d.g, tx), ty) / 255f;
    }

    private static UIVertex Lerp(UIVertex[] q, float u, float v) {
        UIVertex bottom = LerpVertex(q[0], q[3], u);
        UIVertex top = LerpVertex(q[1], q[2], u);
        return LerpVertex(bottom, top, v);
    }

    private static UIVertex LerpVertex(UIVertex a, UIVertex b, float t) {
        UIVertex result = a;
        result.position = Vector3.Lerp(a.position, b.position, t);
        result.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
        result.color = Color32.Lerp(a.color, b.color, t);
        return result;
    }
}
