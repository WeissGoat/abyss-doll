using UnityEngine;
using UnityEngine.UI;

// Still-art motion: Zero's portraits get the breathing mesh effect wherever the sprite helper puts them, other art does
// not; breathing keeps the feet where they are, push-in grows from the centre, and neither moves the RectTransform.
public static class UIStillMotionTest {
    public static void Run() {
        Debug.Log("=== Running UI Still Motion Test ===");
        GameObject canvasObject = new GameObject("StillMotionTestCanvas", typeof(Canvas));
        try {
            GameObject imageObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            Image image = imageObject.GetComponent<Image>();
            RectTransform rect = image.rectTransform;
            rect.sizeDelta = new Vector2(200f, 400f);
            Vector2 layout = rect.anchoredPosition;

            VisualUIHelper.ApplySimpleSprite(image, "doll_proto_0_stand", Color.white, Color.gray, false);
            UIStillMotion motion = image.GetComponent<UIStillMotion>();
            Expect(motion != null && motion.enabled && motion.Motion == UIStillMotion.Mode.Breath, "a doll portrait breathes");

            VertexHelper quad = Quad(-100f, -200f, 100f, 200f);
            motion.ModifyMesh(quad);
            Bounds bounds = BoundsOf(quad);
            Expect(Mathf.Abs(bounds.min.y + 200f) < 0.01f, $"breathing keeps the feet in place (bottom {bounds.min.y})");
            Expect(Mathf.Abs(bounds.max.y - (-200f + 400f * motion.CurrentFactor)) < 0.01f, "the head rises and settles with the breath");
            Expect(Mathf.Abs(motion.CurrentFactor - 1f) <= UIStillMotion.BreathAmount + 0.0001f, $"breath stays small ({motion.CurrentFactor})");
            Expect(rect.anchoredPosition == layout, "breathing does not move the RectTransform");

            VisualUIHelper.ApplySimpleSprite(image, "item_gear_whetstone_icon", Color.white, Color.gray, false);
            Expect(!motion.enabled, "the same image showing other art stops breathing");

            UIStillMotion.Attach(image, UIStillMotion.Mode.PushIn);
            VertexHelper cg = Quad(-100f, -50f, 100f, 50f);
            motion.ModifyMesh(cg);
            Bounds pushed = BoundsOf(cg);
            Expect(Mathf.Abs(pushed.center.x) < 0.01f && Mathf.Abs(pushed.center.y) < 0.01f, "push-in grows from the centre");
            Expect(pushed.size.x >= 200f - 0.01f && pushed.size.x <= 200f * (1f + UIStillMotion.PushInAmount) + 0.01f, $"push-in stays within its range ({pushed.size.x})");
        } finally {
            Object.DestroyImmediate(canvasObject);
        }

        Debug.Log("=== UI Still Motion Test Finished ===");
    }

    private static VertexHelper Quad(float xMin, float yMin, float xMax, float yMax) {
        VertexHelper vh = new VertexHelper();
        vh.AddVert(new Vector3(xMin, yMin), Color.white, Vector2.zero);
        vh.AddVert(new Vector3(xMin, yMax), Color.white, Vector2.up);
        vh.AddVert(new Vector3(xMax, yMax), Color.white, Vector2.one);
        vh.AddVert(new Vector3(xMax, yMin), Color.white, Vector2.right);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
        return vh;
    }

    private static Bounds BoundsOf(VertexHelper vh) {
        UIVertex vertex = default;
        vh.PopulateUIVertex(ref vertex, 0);
        Bounds bounds = new Bounds(vertex.position, Vector3.zero);
        for (int i = 1; i < vh.currentVertCount; i++) {
            vh.PopulateUIVertex(ref vertex, i);
            bounds.Encapsulate(vertex.position);
        }

        return bounds;
    }

    private static void Expect(bool condition, string message) {
        if (condition) {
            Debug.Log("PASSED: " + message);
        } else {
            Debug.LogError("FAILED: " + message);
        }
    }
}
