using UnityEngine;
using UnityEngine.UI;

// Quiet life for still art, done in the mesh so the RectTransform (and whatever is laid out against it) stays exactly
// where the screen put it. Breath: Zero's portraits rise and settle from the feet, a couple of percent, every few
// seconds. PushIn: a full-screen CG slowly creeps closer while it is shown. Unscaled time, so dialogue pauses do not
// freeze it.
[RequireComponent(typeof(Graphic))]
public class UIStillMotion : BaseMeshEffect {
    public enum Mode { Breath, PushIn }

    public const string PortraitPrefix = "doll_";
    public const float BreathAmount = 0.014f;
    public const float BreathPeriod = 3.6f;
    public const float PushInAmount = 0.045f;
    public const float PushInSeconds = 14f;

    public Mode Motion = Mode.Breath;
    private float _start;
    private float _phase;
    private Sprite _sprite;

    // Called by VisualUIHelper whenever an image gets a sprite: Zero's portraits breathe, anything else stops.
    public static void SyncPortrait(Image image, string visualID) {
        if (image == null) {
            return;
        }

        bool portrait = !string.IsNullOrEmpty(visualID) && visualID.StartsWith(PortraitPrefix, System.StringComparison.Ordinal)
            && !visualID.EndsWith("_icon", System.StringComparison.Ordinal);
        UIStillMotion motion = image.GetComponent<UIStillMotion>();
        if (portrait) {
            Attach(image, Mode.Breath);
        } else if (motion != null && motion.Motion == Mode.Breath) {
            motion.enabled = false;
        }
    }

    public static UIStillMotion Attach(Graphic graphic, Mode mode) {
        UIStillMotion motion = graphic.GetComponent<UIStillMotion>();
        if (motion == null) {
            motion = graphic.gameObject.AddComponent<UIStillMotion>();
        }

        motion.Motion = mode;
        motion.enabled = true;
        return motion;
    }

    public static void Detach(Graphic graphic) {
        UIStillMotion motion = graphic != null ? graphic.GetComponent<UIStillMotion>() : null;
        if (motion != null) {
            motion.enabled = false;
        }
    }

    // The current scale factor (1 = at rest); the Play Mode check reads it.
    public float CurrentFactor { get; private set; } = 1f;

    protected override void OnEnable() {
        base.OnEnable();
        _start = Time.unscaledTime;
        _phase = Random.value * BreathPeriod;
    }

    private void Update() {
        Image image = graphic as Image;
        if (image != null && image.sprite != _sprite) {
            _sprite = image.sprite;
            _start = Time.unscaledTime;
        }

        graphic.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh) {
        if (!IsActive() || vh.currentVertCount == 0) {
            return;
        }

        float t = Time.unscaledTime;
        UIVertex vertex = default;
        float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < vh.currentVertCount; i++) {
            vh.PopulateUIVertex(ref vertex, i);
            minX = Mathf.Min(minX, vertex.position.x);
            maxX = Mathf.Max(maxX, vertex.position.x);
            minY = Mathf.Min(minY, vertex.position.y);
            maxY = Mathf.Max(maxY, vertex.position.y);
        }

        float centreX = (minX + maxX) * 0.5f;
        float centreY = (minY + maxY) * 0.5f;
        float sx;
        float sy;
        float pivotY;
        if (Motion == Mode.Breath) {
            float wave = Mathf.Sin((t + _phase) / BreathPeriod * Mathf.PI * 2f);
            sy = 1f + BreathAmount * wave;
            sx = 1f - BreathAmount * 0.35f * wave;
            pivotY = minY;
        } else {
            float k = 1f + PushInAmount * Mathf.SmoothStep(0f, 1f, (t - _start) / PushInSeconds);
            sx = sy = k;
            pivotY = centreY;
        }

        CurrentFactor = sy;
        for (int i = 0; i < vh.currentVertCount; i++) {
            vh.PopulateUIVertex(ref vertex, i);
            Vector3 p = vertex.position;
            p.x = centreX + (p.x - centreX) * sx;
            p.y = pivotY + (p.y - pivotY) * sy;
            vertex.position = p;
            vh.SetUIVertex(vertex, i);
        }
    }
}
