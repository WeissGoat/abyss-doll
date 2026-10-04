using UnityEngine;
using UnityEngine.UI;

// A hollow rectangular frame (four thin bars) around its parent, for selection and "current" highlights that must
// not cover what they frame.
public static class UIFrame {
    private static readonly string[] BarNames = { "Top", "Bottom", "Left", "Right" };

    public static GameObject Create(Transform parent, string name, float thickness, Vector2 offsetMin, Vector2 offsetMax) {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = offsetMin;
        rootRect.offsetMax = offsetMax;

        foreach (string barName in BarNames) {
            GameObject bar = new GameObject(barName, typeof(RectTransform));
            bar.transform.SetParent(root.transform, false);
            Image image = bar.AddComponent<Image>();
            image.raycastTarget = false;
            RectTransform rect = (RectTransform)bar.transform;
            switch (barName) {
                case "Top":
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.sizeDelta = new Vector2(0f, thickness);
                    break;
                case "Bottom":
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.sizeDelta = new Vector2(0f, thickness);
                    break;
                case "Left":
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.sizeDelta = new Vector2(thickness, 0f);
                    break;
                default:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.sizeDelta = new Vector2(thickness, 0f);
                    break;
            }

            rect.anchoredPosition = Vector2.zero;
        }

        return root;
    }

    public static void SetColor(Transform frame, Color color) {
        if (frame == null) {
            return;
        }

        foreach (Image bar in frame.GetComponentsInChildren<Image>(true)) {
            bar.color = color;
        }
    }
}
