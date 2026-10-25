using UnityEngine;
using UnityEngine.UI;

// The one place for UI colours, type sizes and the font. Screens rebuilt from M2 on take their look from here, so the
// M4 art pass restyles the game by changing this file instead of every controller.
public static class UITheme {
    // M4 linen theme (美术文档/04_美术风格基准.md 1.2): stitched oatmeal linen boards, leather edging, ink-brown text,
    // madder red for the one primary action, blue only for AP and energy. Call sites use these roles, not raw colours.
    public static readonly Color Background = Hex(0x2B2018);
    public static readonly Color Panel = Hex(0xE9DCC3);
    public static readonly Color PanelRaised = Hex(0xF6ECD9);
    public static readonly Color Inset = Hex(0xDCCBAA);
    public static readonly Color Border = Hex(0x7A5236);
    public static readonly Color TextPrimary = Hex(0x3B2A1E);
    public static readonly Color TextSecondary = Hex(0x6B5544);
    public static readonly Color TextMuted = Hex(0x8F7A62);
    public static readonly Color Accent = Hex(0xB5402F);
    public static readonly Color OnAccent = Hex(0xF7EEDC);
    public static readonly Color Positive = Hex(0x4F7A3A);
    public static readonly Color Warning = Hex(0xB06A1E);
    public static readonly Color Danger = Hex(0xA8382A);
    public static readonly Color Info = Hex(0x2F7FA8);
    public static readonly Color Energy = Hex(0x3FA7D6);

    // Text that sits straight on a game background (map, combat) instead of a linen board.
    public static readonly Color TextOnDark = Hex(0xF4E6CC);
    public static readonly Color TextOnDarkMuted = Hex(0xC9B596);
    public static readonly Color AccentOnDark = Hex(0xF2B864);
    public static readonly Color PositiveOnDark = Hex(0xA9D68C);

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
        if (color == Panel) {
            ApplyBoardSkin(image, VisualAssetService.UIPanelInfoID);
        }

        return image;
    }

    public static Button CreateButton(Transform parent, string name, string label, Color background, Color textColor, int fontSize = BodySize) {
        Image image = CreatePanel(parent, name, background);
        ApplyButtonSkin(image, background);
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
        root.AddComponent<Image>().color = new Color(0.1f, 0.06f, 0.03f, 0.72f);

        Image dialog = CreatePanel(root.transform, "Dialog", Panel);
        if (!ApplyBoardSkin(dialog, VisualAssetService.UIPanelMainID)) {
            AddBorder(dialog, Border, 2f);
        }
        Place(dialog.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, dialogSize);
        return root;
    }

    // Button looks by role: the madder cloth for the primary action, linen cloth with leather stitching for the rest,
    // a greyed linen for disabled (Border). Other colours (item tints) stay flat.
    public static void ApplyButtonSkin(Image image, Color background) {
        if (image == null || background.a <= 0.01f) {
            return;
        }

        string visualID = null;
        Color tint = Color.white;
        if (background == Accent) {
            visualID = VisualAssetService.UIButtonPrimaryID;
        } else if (background == Danger) {
            visualID = VisualAssetService.UIButtonDangerID;
        } else if (background == Border) {
            visualID = VisualAssetService.UIButtonSecondaryID;
            tint = new Color(0.78f, 0.74f, 0.7f, 1f);
        } else if (background == PanelRaised || background == Panel || background == Inset || background == Background) {
            visualID = VisualAssetService.UIButtonSecondaryID;
        }

        if (visualID != null && VisualAssetService.TryGetSprite(visualID, out Sprite sprite)) {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = tint;
        }
    }

    // Restyles a button built elsewhere (scene, prefab or legacy controller): cloth skin by role and a readable label.
    public static void StyleButton(Button button, bool primary) {
        if (button == null) {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null) {
            ApplyButtonSkin(image, primary ? Accent : PanelRaised);
            button.targetGraphic = image;
        }

        Outline outline = button.GetComponent<Outline>();
        if (outline != null) {
            outline.enabled = false;
        }

        foreach (Text label in button.GetComponentsInChildren<Text>(true)) {
            label.font = Font;
            label.color = primary ? OnAccent : TextPrimary;
        }
    }

    // A stitched linen board (9-slice) behind a panel; false when the sprite is not registered (tests, missing art).
    public static bool ApplyBoardSkin(Image image, string visualID, float borderScale = 1.5f) {
        if (image == null || !VisualAssetService.TryGetSprite(visualID, out Sprite sprite)) {
            return false;
        }

        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = borderScale;
        image.color = Color.white;
        return true;
    }

    public static Color Hex(int rgb, float alpha = 1f) {
        return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);
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

    // A thin outline for flat panels. Panels and buttons that already wear a stitched sprite frame get none.
    public static Outline AddBorder(Graphic graphic, Color color, float width = 2f) {
        Image image = graphic as Image;
        if (image != null && image.sprite != null && image.type == Image.Type.Sliced) {
            return null;
        }

        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null) {
            outline = graphic.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, -width);
        return outline;
    }
}
