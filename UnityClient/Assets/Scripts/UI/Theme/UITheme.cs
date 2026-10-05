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

    // Fallback when the bundled font is missing: OS fonts with good CJK coverage, in order of preference. The
    // built-in LegacyRuntime font has no Chinese
    // glyphs, so Unity used to fall back to whatever the OS picked (often a bitmap-looking SimSun).
    private static readonly string[] ChineseFontNames = {
        "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "Source Han Sans SC", "SimHei"
    };

    private static Font _font;
    private static Font _legacyFont;

    // Bundled with the game so text reads the same on every machine (Noto Sans SC, SIL OFL; license next to it).
    public const string BundledFontPath = "Fonts/NotoSansSC-Regular";

    public static Font Font {
        get {
            if (_font == null) {
                _font = Resources.Load<Font>(BundledFontPath);
            }

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

    public static Button CreateButton(Transform parent, string name, string label, Color background, Color textColor, int fontSize = BodySize) {
        Image image = CreatePanel(parent, name, background);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
        button.colors = colors;
        Text text = CreateText(image.transform, "Label_Text", label, fontSize, textColor, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        button.onClick.AddListener(() => GameAudio.PlaySfx("click"));
        return button;
    }

    public static void SetButtonLabel(Button button, string label) {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        if (text != null) {
            text.text = label;
        }
    }

    // A full-screen dimmed modal on its own sorting layer (above the backpack grid and other late siblings). Returns
    // the root; the centred dialog panel is root.Find("Dialog").
    public static GameObject CreateModal(Transform parent, string name, Vector2 dialogSize, int sortingOrder = 540) {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Stretch((RectTransform)root.transform);
        Canvas overlay = root.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = sortingOrder;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.66f);

        Image dialog = CreatePanel(root.transform, "Dialog", Panel);
        AddBorder(dialog, Border, 2f);
        Place(dialog.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, dialogSize);
        return root;
    }

    public static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Anchors and pivots at the same point, so position is relative to that point of the parent.
    public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size) {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
