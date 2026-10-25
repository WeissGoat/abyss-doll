using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Live2D-lite for one still portrait: the image's quad is split into a grid and its vertices bend sideways by a
// weight map, so long hair sways (a wave running down to the tips) while the head, hands and feet stay put. Pixels
// stretch instead of tearing, and the RectTransform never moves. The skirt does not sway by itself: standing still,
// her dress only moves with her (UIPortraitPuppet leans her whole body).
// The weight map is a registered sprite "<visualID>_rig" in Approved (R = hair; built by
// tools/美术工具/split_hair_layer.py for a portrait whose hair is a layer of its own, else build_portrait_rig.py,
// whose G is no longer used; imported Read/Write, uncompressed, not sRGB). Portraits without one stay still.
[RequireComponent(typeof(Graphic))]
public class UIPortraitSway : BaseMeshEffect {
    public const string RigSuffix = "_rig";
    // Fine enough for a head turning on its neck (UIPortraitPuppet bends the same grid), and for the hair beside her
    // arms to sway without a cell dragging the arm's edge along (16 px cells on a 1024 px portrait).
    public const int Columns = 64;
    public const int Rows = 96;
    // Peak sideways bend as a share of the image width (about 12 px on a 1024 px portrait, 7 px in the workshop). Half
    // what it was before her hair also followed her head: together they swing about as wide.
    public const float HairAmount = 0.012f;
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

        UIVertex corner = default;
        vh.PopulateUIVertex(ref corner, 0);
        Vector3 bottomLeft = corner.position;
        vh.PopulateUIVertex(ref corner, 3);
        float width = Vector3.Distance(bottomLeft, corner.position);
        float maxBend = 0f;
        BuildGrid(vh, (uv, rest) => {
            float bend = BendAt(uv.x, uv.y, width);
            maxBend = Mathf.Max(maxBend, Mathf.Abs(bend));
            return rest + new Vector3(bend, 0f, 0f);
        });
        LastMaxBend = maxBend;
    }

    // Replaces a simple image quad with a Columns x Rows grid; move gets each vertex's (u, v) from the bottom-left and
    // its place on the flat quad, and returns where to draw it.
    public static void BuildGrid(VertexHelper vh, System.Func<Vector2, Vector3, Vector3> move) {
        // Simple image quad: 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right.
        UIVertex[] corners = new UIVertex[4];
        for (int i = 0; i < 4; i++) {
            vh.PopulateUIVertex(ref corners[i], i);
        }

        vh.Clear();
        for (int row = 0; row <= Rows; row++) {
            float v = row / (float)Rows;
            for (int column = 0; column <= Columns; column++) {
                float u = column / (float)Columns;
                UIVertex vertex = Lerp(corners, u, v);
                vertex.position = move(new Vector2(u, v), vertex.position);
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
    }

    // How far the point (u, v) of the portrait is bent sideways right now, for a portrait drawn width wide.
    public float BendAt(float u, float v, float width) {
        if (_weights == null) {
            return 0f;
        }

        float wave = (Time.unscaledTime + _phase) / Period * Mathf.PI * 2f;
        float fromTop = 1f - v;
        return width * HairAmount * (1f + Boost) * HairWeight(u, v) * Mathf.Sin(wave - 3f * fromTop);
    }

    // The hair weight at (u, v) of the portrait, v up; the rig texture is stored v up too (Unity's GetPixels order).
    public float HairWeight(float u, float v) {
        if (_weights == null) {
            return 0f;
        }

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
        return Mathf.Lerp(Mathf.Lerp(a.r, b.r, tx), Mathf.Lerp(c.r, d.r, tx), ty) / 255f;
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
