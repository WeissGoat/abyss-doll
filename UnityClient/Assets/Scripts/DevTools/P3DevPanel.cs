using System;
using UnityEngine;
using UnityEngine.UI;

// In-game developer panel, toggled with F1. Only created in the Editor and in development builds.
// Every button calls P3DevTools and shows the first lines of its report; the full report goes to the console.
public class P3DevPanel : MonoBehaviour {
    private const string RootName = "P3DevPanel_Runtime";
    private const int OutputLines = 8;

    private GameObject _canvasObj;
    private Text _outputText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        if (!Application.isEditor && !Debug.isDebugBuild) {
            return;
        }

        if (FindObjectOfType<P3DevPanel>() != null) {
            return;
        }

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        root.AddComponent<P3DevPanel>();
    }

    public static bool IsDevPanelObject(Transform transform) {
        for (Transform current = transform; current != null; current = current.parent) {
            if (current.name == RootName) {
                return true;
            }
        }

        return false;
    }

    private void Awake() {
        Build();
        // The canvas stays inactive while hidden, so FindObjectOfType<Canvas>() elsewhere never picks it up.
        _canvasObj.SetActive(false);
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.F1)) {
            _canvasObj.SetActive(!_canvasObj.activeSelf);
        }
    }

    private void Build() {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _canvasObj = new GameObject("DevCanvas");
        _canvasObj.transform.SetParent(transform, false);
        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        CanvasScaler scaler = _canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        _canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(_canvasObj.transform, false);
        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.92f);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-16f, -16f);
        panelRect.sizeDelta = new Vector2(420f, 810f);
        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 12, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        AddLabel(panel.transform, font, "开发者面板（F1 开关）", 22, 34f);
        AddButton(panel.transform, font, "跳过序章", P3DevTools.SkipPrologue);
        AddButton(panel.transform, font, "启动时播放序章：开/关", P3DevTools.TogglePrologueOnStartup);
        AddButton(panel.transform, font, "回工坊", () => P3DevTools.GoTo("workshop"));
        AddButton(panel.transform, font, "新下潜（地图）", () => P3DevTools.GoTo("map"));
        AddButton(panel.transform, font, "进战斗", () => P3DevTools.GoTo("combat"));
        AddButton(panel.transform, font, "自动战斗", P3DevTools.AutoCombat);
        AddButton(panel.transform, font, "拿战利品", P3DevTools.TakeLoot);
        AddButton(panel.transform, font, "跑完一次下潜", () => P3DevTools.RunDive());
        AddButton(panel.transform, font, "+500 金币", () => P3DevTools.AddMoney(500));
        AddButton(panel.transform, font, "回满状态", P3DevTools.Heal);
        AddButton(panel.transform, font, "当前状态", P3DevTools.Describe);
        _outputText = AddLabel(panel.transform, font, string.Empty, 15, 200f);
        _outputText.alignment = TextAnchor.UpperLeft;
    }

    private void AddButton(Transform parent, Font font, string label, Func<string> action) {
        GameObject buttonObj = new GameObject("DevButton_" + label);
        buttonObj.transform.SetParent(parent, false);
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.32f, 0.42f, 1f);
        buttonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => Show(label, action));

        Text text = AddLabel(buttonObj.transform, font, label, 20, 40f);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleCenter;
    }

    private Text AddLabel(Transform parent, Font font, string content, int fontSize, float height) {
        GameObject textObj = new GameObject("DevText");
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        textObj.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
        return text;
    }

    private void Show(string label, Func<string> action) {
        string report;
        try {
            report = action();
        } catch (Exception e) {
            report = $"{label} failed: {e.Message}";
            Debug.LogException(e);
        }

        Debug.Log($"[P3DevPanel] {label}\n{report}");
        string[] lines = (report ?? string.Empty).Split('\n');
        _outputText.text = string.Join("\n", lines, 0, Mathf.Min(OutputLines, lines.Length));
    }
}
