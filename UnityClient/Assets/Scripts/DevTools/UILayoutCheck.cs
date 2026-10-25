using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Layout problems a player would see, found from the rects alone (no screenshot), on whatever is on screen now:
// something cut off by the screen edge, a control or text spilling out of the panel it sits on, two controls on top
// of each other, a control lying on a different panel, text that does not fit its box, a picture stretched out of
// proportion. Only what is actually shown counts: graphics hidden by a mask, or under a later opaque screen-sized
// layer (a modal backdrop, a full-screen CG), are skipped. Used by P3DevTools.LayoutCheck() and the build smoke.
public static class UILayoutCheck {
    private const float Slack = 4f;
    private const float BlockerAlpha = 0.85f;
    private const float ScreenSized = 0.9f;
    // A screen-sized layer this opaque dims what is under it (a modal's backdrop): things under it and things above it
    // are not compared with each other.
    private const float DimmerAlpha = 0.3f;

    public sealed class Finding {
        public string Kind;
        public string Path;
        public string Detail;

        public override string ToString() {
            return $"{Kind}: {Path} {Detail}";
        }
    }

    private sealed class Shown {
        public Graphic Graphic;
        public Rect Rect;
        public bool ScreenSized;
        public int Layer;
    }

    public static List<Finding> Run() {
        List<Finding> findings = new List<Finding>();
        Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
        List<Shown> all = new List<Shown>();
        foreach (Graphic graphic in Object.FindObjectsOfType<Graphic>()) {
            if (!UIOcclusion.IsVisible(graphic) || P3DevPanel.IsDevPanelObject(graphic.transform)) {
                continue;
            }

            Rect rect = graphic is Text ? GlyphRect((Text)graphic) : ScreenRect(graphic.rectTransform);
            Rect clipped = Clip(graphic.transform, rect);
            if (clipped.width < 1f || clipped.height < 1f) {
                continue;
            }

            all.Add(new Shown {
                Graphic = graphic,
                Rect = clipped,
                ScreenSized = rect.width >= screen.width * ScreenSized || rect.height >= screen.height * ScreenSized
            });
        }

        List<Shown> blockers = all.FindAll(s => s.ScreenSized && s.Graphic.color.a * s.Graphic.canvasRenderer.GetInheritedAlpha() >= BlockerAlpha && s.Graphic is Image);
        List<Shown> shown = all.FindAll(s => !UnderBlocker(s, blockers));
        List<Shown> dimmers = all.FindAll(s => s.ScreenSized && s.Graphic is Image && s.Graphic.color.a * s.Graphic.canvasRenderer.GetInheritedAlpha() >= DimmerAlpha);
        foreach (Shown s in shown) {
            s.Layer = dimmers.Count(d => d != s && d.Rect.Overlaps(s.Rect) && UIOcclusion.DrawsAfter(d.Graphic, s.Graphic));
        }

        // Controls a player aims at; a screen-sized one is a click-anywhere layer, not a button.
        List<Shown> controls = new List<Shown>();
        foreach (Shown s in shown) {
            Selectable selectable = s.Graphic.GetComponent<Selectable>();
            if (selectable != null && selectable.targetGraphic == s.Graphic && s.Rect.width * s.Rect.height < screen.width * screen.height * 0.5f) {
                controls.Add(s);
            }
        }

        // Panels are drawn boxes (plain, sliced or flat-colour images), not pictures such as a portrait or a CG.
        List<Shown> panels = shown.FindAll(s => s.Graphic is Image && !s.ScreenSized && s.Rect.width * s.Rect.height >= 120f * 80f
            && s.Graphic.GetComponent<Selectable>() == null && IsBox((Image)s.Graphic));

        foreach (Shown s in shown) {
            bool isText = s.Graphic is Text;
            bool isControl = controls.Contains(s);
            Rect raw = ScreenRect(s.Graphic.rectTransform);
            if (!s.ScreenSized && (isText || isControl || raw.width * raw.height >= 32f * 32f)) {
                string cut = Outside(s.Rect, screen);
                if (cut != null) {
                    Add(findings, "off screen", s.Graphic, cut);
                }
            }

            if (isText || isControl) {
                Shown panel = NearestPanel(s, panels);
                string spill = panel != null ? Outside(s.Rect, panel.Rect) : null;
                if (spill != null) {
                    Add(findings, "spills out", s.Graphic, $"of {Name(panel.Graphic)} ({spill})");
                }
            }

            if (isText) {
                string fit = TextFit((Text)s.Graphic);
                if (fit != null) {
                    Add(findings, "text does not fit", s.Graphic, fit);
                }
            }

            Image image = s.Graphic as Image;
            // Flat colour sprites (a few pixels) stretch by design; only real pictures count.
            if (image != null && image.sprite != null && image.type == Image.Type.Simple && !image.preserveAspect && raw.width >= 64f && raw.height >= 64f
                && image.sprite.rect.width >= 64f && image.sprite.rect.height >= 64f) {
                float spriteAspect = image.sprite.rect.width / image.sprite.rect.height;
                float rectAspect = raw.width / raw.height;
                // Plates and rings are widened a little on purpose; a picture squashed by a third or more is a bug.
                if (Mathf.Abs(rectAspect / spriteAspect - 1f) > 0.35f) {
                    Add(findings, "stretched", s.Graphic, $"picture {spriteAspect:0.00}:1 drawn at {rectAspect:0.00}:1");
                }
            }
        }

        for (int i = 0; i < controls.Count; i++) {
            for (int j = i + 1; j < controls.Count; j++) {
                if (controls[i].Layer != controls[j].Layer || Related(controls[i].Graphic.transform, controls[j].Graphic.transform)) {
                    continue;
                }

                if (OverlapShare(controls[i].Rect, controls[j].Rect) > 0.1f) {
                    Add(findings, "controls overlap", controls[i].Graphic, "and " + Name(controls[j].Graphic));
                }
            }

            foreach (Shown panel in panels) {
                if (controls[i].Layer != panel.Layer || Related(controls[i].Graphic.transform, panel.Graphic.transform) || panel.Rect.width * panel.Rect.height < 2f * controls[i].Rect.width * controls[i].Rect.height) {
                    continue;
                }

                if (OverlapShare(controls[i].Rect, panel.Rect) > 0.2f && OverlapShare(controls[i].Rect, panel.Rect) < 0.999f
                    && UIOcclusion.DrawsAfter(controls[i].Graphic, panel.Graphic) && DifferentWindows(controls[i].Graphic.transform, panel.Graphic.transform, screen)) {
                    Add(findings, "control lies on another panel", controls[i].Graphic, "partly over " + Name(panel.Graphic));
                }
            }
        }

        return findings;
    }

    public static string Report(int max = 40) {
        List<Finding> findings = Run();
        if (findings.Count == 0) {
            return "Layout OK.";
        }

        StringBuilder sb = new StringBuilder($"Layout problems ({findings.Count}):\n");
        for (int i = 0; i < findings.Count && i < max; i++) {
            sb.AppendLine(findings[i].ToString());
        }

        if (findings.Count > max) {
            sb.AppendLine($"... and {findings.Count - max} more");
        }

        return sb.ToString().TrimEnd();
    }

    private static void Add(List<Finding> findings, string kind, Graphic graphic, string detail) {
        findings.Add(new Finding { Kind = kind, Path = Name(graphic), Detail = detail });
    }

    // The last three levels of the path, plus the text when it has one, so a line says which thing it is.
    private static string Name(Graphic graphic) {
        string[] parts = UIOcclusion.PathOf(graphic.transform).Split('/');
        int start = Mathf.Max(0, parts.Length - 3);
        string path = string.Join("/", parts, start, parts.Length - start);
        Text text = graphic as Text ?? graphic.GetComponentInChildren<Text>();
        string label = text != null && !string.IsNullOrEmpty(text.text) ? text.text.Replace("\n", " ") : null;
        if (label != null && label.Length > 16) {
            label = label.Substring(0, 16) + "…";
        }

        return label != null ? $"{path} '{label}'" : path;
    }

    private static Rect ScreenRect(RectTransform rect) {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
        Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
        return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
    }

    // Where a text's lines actually are: its box shrunk to the text's preferred size, placed by its alignment (a tall box
    // with one short line at the top is not "off screen" just because the empty part of the box is).
    private static Rect GlyphRect(Text text) {
        Rect box = ScreenRect(text.rectTransform);
        float scale = text.canvas != null ? text.canvas.scaleFactor : 1f;
        float width = text.horizontalOverflow == HorizontalWrapMode.Overflow ? text.preferredWidth * scale : Mathf.Min(box.width, text.preferredWidth * scale);
        float height = text.verticalOverflow == VerticalWrapMode.Overflow ? text.preferredHeight * scale : Mathf.Min(box.height, text.preferredHeight * scale);
        int column = (int)text.alignment % 3;
        int row = (int)text.alignment / 3;
        float x = column == 0 ? box.xMin : column == 1 ? box.center.x - width / 2f : box.xMax - width;
        float y = row == 0 ? box.yMax - height : row == 1 ? box.center.y - height / 2f : box.yMin;
        return new Rect(x, y, width, height);
    }

    // What is left of the rect inside every mask above it.
    private static Rect Clip(Transform transform, Rect rect) {
        for (Transform parent = transform.parent; parent != null; parent = parent.parent) {
            if (parent.GetComponent<RectMask2D>() != null || (parent.GetComponent<Mask>() != null && parent.GetComponent<Mask>().enabled)) {
                Rect mask = ScreenRect((RectTransform)parent);
                rect = Rect.MinMaxRect(Mathf.Max(rect.xMin, mask.xMin), Mathf.Max(rect.yMin, mask.yMin), Mathf.Min(rect.xMax, mask.xMax), Mathf.Min(rect.yMax, mask.yMax));
            }
        }

        return rect;
    }

    private static bool UnderBlocker(Shown s, List<Shown> blockers) {
        foreach (Shown blocker in blockers) {
            if (blocker != s && blocker.Rect.Contains(s.Rect.min) && blocker.Rect.Contains(s.Rect.max - Vector2.one) && UIOcclusion.DrawsAfter(blocker.Graphic, s.Graphic)) {
                return true;
            }
        }

        return false;
    }

    private static string Outside(Rect inner, Rect outer) {
        List<string> sides = new List<string>();
        if (inner.yMin < outer.yMin - Slack) sides.Add($"bottom {outer.yMin - inner.yMin:0}px");
        if (inner.yMax > outer.yMax + Slack) sides.Add($"top {inner.yMax - outer.yMax:0}px");
        if (inner.xMin < outer.xMin - Slack) sides.Add($"left {outer.xMin - inner.xMin:0}px");
        if (inner.xMax > outer.xMax + Slack) sides.Add($"right {inner.xMax - outer.xMax:0}px");
        return sides.Count == 0 ? null : string.Join(", ", sides.ToArray());
    }

    // The closest ancestor drawn as a panel: what a text or a button is meant to stay inside.
    private static Shown NearestPanel(Shown s, List<Shown> panels) {
        for (Transform parent = s.Graphic.transform.parent; parent != null; parent = parent.parent) {
            if (parent.GetComponent<Canvas>() != null && parent.GetComponent<Canvas>().isRootCanvas) {
                return null;
            }

            Shown panel = panels.Find(p => p.Graphic.transform == parent);
            if (panel != null) {
                return panel;
            }
        }

        return null;
    }

    private static bool IsBox(Image image) {
        return image.sprite == null || image.type != Image.Type.Simple || image.sprite.rect.width < 64f || image.sprite.rect.height < 64f;
    }

    private static string TextFit(Text text) {
        // One or two characters are symbols (×, ▶, ●) drawn larger than their box on purpose.
        if (text.text.Length <= 2) {
            return null;
        }

        RectTransform rect = text.rectTransform;
        float height = rect.rect.height;
        float width = rect.rect.width;
        // Truncated text loses lines; overflowing text is measured at its real size instead (off screen, spills out).
        if (text.verticalOverflow == VerticalWrapMode.Truncate && !text.resizeTextForBestFit) {
            float needed = text.preferredHeight;
            if (needed > height + Slack && height > 0f) {
                return $"needs {needed:0}px height, has {height:0}px";
            }
        }

        if (text.horizontalOverflow == HorizontalWrapMode.Overflow && !text.resizeTextForBestFit) {
            float needed = text.preferredWidth;
            if (needed > width + Slack && width > 0f) {
                return $"needs {needed:0}px width, has {width:0}px";
            }
        }

        return null;
    }

    private static bool Related(Transform a, Transform b) {
        return a.IsChildOf(b) || b.IsChildOf(a);
    }

    // Two things belong to different windows when the closest parent they share spans the whole screen (the canvas or
    // a full-screen layer); inside one window a button over its own panels is the design.
    private static bool DifferentWindows(Transform a, Transform b, Rect screen) {
        Transform common = a;
        while (common != null && !b.IsChildOf(common)) {
            common = common.parent;
        }

        RectTransform rect = common as RectTransform;
        if (rect == null) {
            return true;
        }

        Rect r = ScreenRect(rect);
        return r.width >= screen.width * ScreenSized && r.height >= screen.height * ScreenSized;
    }

    // How much of the smaller rect the two share.
    private static float OverlapShare(Rect a, Rect b) {
        float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
        float h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
        if (w <= 0f || h <= 0f) {
            return 0f;
        }

        return w * h / Mathf.Min(a.width * a.height, b.width * b.height);
    }
}
