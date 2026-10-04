using UnityEngine;
using UnityEngine.UI;

// The one place for UI colours, type sizes and the font. Screens rebuilt from M2 on take their look from here, so the
// M4 art pass restyles the game by changing this file instead of every controller.
public static class UITheme {
    // Neutral dark surfaces with one warm accent; the art direction (M4) replaces these values, not the call sites.
    public static readonly Color Background = new Color(0.055f, 0.07f, 0.08f, 1f);
    public static readonly Color Panel = new Color(0.09f, 0.11f, 0.125f, 0.95f);
    public static readonly Color PanelRaised = new Color(0.14f, 0.16f, 0.185f, 0.97f);
    public static readonly Color Border = new Color(0.36f, 0.41f, 0.45f, 1f);
    public static readonly Color TextPrimary = new Color(0.95f, 0.94f, 0.91f, 1f);
    public static readonly Color TextSecondary = new Color(0.72f, 0.74f, 0.74f, 1f);
    public static readonly Color TextMuted = new Color(0.5f, 0.53f, 0.55f, 1f);
    public static readonly Color Accent = new Color(1f, 0.77f, 0.33f, 1f);
    public static readonly Color Positive = new Color(0.47f, 0.83f, 0.56f, 1f);
    public static readonly Color Warning = new Color(0.98f, 0.66f, 0.3f, 1f);
    public static readonly Color Danger = new Color(0.93f, 0.4f, 0.34f, 1f);
    public static readonly Color Info = new Color(0.47f, 0.73f, 0.96f, 1f);

    public const int TitleSize = 36;
    public const int HeadingSize = 28;
    public const int BodySize = 22;
    public const int CaptionSize = 18;

    // Windows fonts with good CJK coverage, in order of preference. The built-in LegacyRuntime font has no Chinese
    // glyphs, so Unity used to fall back to whatever the OS picked (often a bitmap-looking SimSun).
    private static readonly string[] ChineseFontNames = {
        "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Source Han Sans SC", "SimHei"
    };

    private static Font _font;
    private static Font _legacyFont;

    public static Font Font {
        get {
            if (_font == null) {
                _font = Font.CreateDynamicFontFromOSFont(ChineseFontNames, BodySize);
                if (_font == null) {
                    _font = LegacyFont;
                }
            }

            return _font;
        }
    }

    public static Font LegacyFont {
        get {
            if (_legacyFont == null) {
                _legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return _legacyFont;
        }
    }

    public static bool IsLegacyFont(Font font) {
        return font == null || font == LegacyFont || font.name == "LegacyRuntime" || font.name == "Arial";
    }

    public static Text CreateText(Transform parent, string name, string content, int size, Color color, TextAnchor alignment = TextAnchor.MiddleLeft) {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = Font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    public static Image CreatePanel(Transform parent, string name, Color color) {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    public static Outline AddBorder(Graphic graphic, Color color, float width = 2f) {
        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null) {
            outline = graphic.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, -width);
        return outline;
    }
}
