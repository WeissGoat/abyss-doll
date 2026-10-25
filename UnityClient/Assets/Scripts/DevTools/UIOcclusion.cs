using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Answers "what is drawn on top of this element?" from UGUI draw order, without a screenshot: sorting order of the
// nearest sorting canvas first, then hierarchy order. Event raycasts cannot answer it (a held item ignores raycasts),
// so this walks the visible graphics instead. Used by P3DevTools.Occlusion() and by tests.
public static class UIOcclusion {
    private const float VisibleAlpha = 0.05f;

    // Visible graphics outside the target's own subtree that contain the point and draw after the target.
    // The point defaults to the target's centre.
    public static List<Graphic> CoveringGraphics(RectTransform target, Vector2? screenPoint = null) {
        List<Graphic> covering = new List<Graphic>();
        Graphic targetGraphic = target != null ? target.GetComponent<Graphic>() : null;
        if (targetGraphic == null || targetGraphic.canvas == null) {
            return covering;
        }

        Camera camera = EventCamera(targetGraphic.canvas);
        Vector2 point = screenPoint ?? RectTransformUtility.WorldToScreenPoint(camera, target.TransformPoint(target.rect.center));
        DrawKey targetKey = DrawKey.Of(targetGraphic);
        foreach (Graphic graphic in Object.FindObjectsOfType<Graphic>()) {
            if (graphic == targetGraphic || graphic.transform.IsChildOf(target) || !IsVisible(graphic)) {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, point, EventCamera(graphic.canvas))) {
                continue;
            }

            if (DrawKey.Of(graphic).CompareTo(targetKey) > 0) {
                covering.Add(graphic);
            }
        }

        return covering;
    }

    // a is drawn after (on top of) b; false when they sit on different sorting canvases with the same order.
    public static bool DrawsAfter(Graphic a, Graphic b) {
        return DrawKey.Of(a).CompareTo(DrawKey.Of(b)) > 0;
    }

    public static bool IsVisible(Graphic graphic) {
        if (graphic == null || !graphic.isActiveAndEnabled || graphic.canvas == null || graphic.canvasRenderer.cull) {
            return false;
        }

        // Text with nothing in it draws nothing.
        Text text = graphic as Text;
        if (text != null && string.IsNullOrEmpty(text.text)) {
            return false;
        }

        return graphic.color.a * graphic.canvasRenderer.GetInheritedAlpha() > VisibleAlpha;
    }

    public static string PathOf(Transform transform) {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null && parent.GetComponent<Canvas>() == null; parent = parent.parent) {
            path = parent.name + "/" + path;
        }

        return path;
    }

    private static Camera EventCamera(Canvas canvas) {
        Canvas root = canvas != null ? canvas.rootCanvas : null;
        return root == null || root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    // Draw order: the sorting order of the nearest canvas that sorts (root or override), then depth-first hierarchy
    // order below that canvas. Two different sorting canvases with the same order are treated as unordered.
    private struct DrawKey {
        private int _sortingOrder;
        private Canvas _sortingCanvas;
        private List<int> _path;

        public static DrawKey Of(Graphic graphic) {
            Canvas sorting = graphic.canvas;
            while (sorting != null && !sorting.isRootCanvas && !sorting.overrideSorting) {
                Transform parent = sorting.transform.parent;
                sorting = parent != null ? parent.GetComponentInParent<Canvas>() : null;
            }

            List<int> path = new List<int>();
            for (Transform t = graphic.transform; t != null && (sorting == null || t != sorting.transform); t = t.parent) {
                path.Insert(0, t.GetSiblingIndex());
            }

            return new DrawKey { _sortingOrder = sorting != null ? sorting.sortingOrder : 0, _sortingCanvas = sorting, _path = path };
        }

        public int CompareTo(DrawKey other) {
            if (_sortingOrder != other._sortingOrder) {
                return _sortingOrder.CompareTo(other._sortingOrder);
            }

            if (_sortingCanvas != other._sortingCanvas) {
                return 0;
            }

            // A child draws after its parent; otherwise the first differing sibling index decides.
            int shared = Mathf.Min(_path.Count, other._path.Count);
            for (int i = 0; i < shared; i++) {
                if (_path[i] != other._path[i]) {
                    return _path[i].CompareTo(other._path[i]);
                }
            }

            return _path.Count.CompareTo(other._path.Count);
        }
    }
}
