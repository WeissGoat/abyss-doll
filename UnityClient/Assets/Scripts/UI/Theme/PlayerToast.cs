using UnityEngine;
using UnityEngine.UI;

public enum PlayerToastKind {
    Info,
    Success,
    Warning
}

// A short message at the top of the screen for feedback the player must see ("转不开：旁边没有足够的空位"), instead of
// a Debug.Log nobody reads. One toast at a time; a new one replaces the old.
public class PlayerToast : MonoBehaviour {
    private const float VisibleSeconds = 2.2f;
    private const float FadeSeconds = 0.35f;

    public static string LastMessage { get; private set; }

    private static PlayerToast _instance;

    private CanvasGroup _group;
    private Image _panel;
    private Text _label;
    private float _shownAt = -100f;

    public static void Show(string message, PlayerToastKind kind = PlayerToastKind.Info) {
        if (string.IsNullOrEmpty(message)) {
            return;
        }

        LastMessage = message;
        Debug.Log($"[PlayerToast] {message}");
        if (!Application.isPlaying) {
            return;
        }

        PlayerToast toast = EnsureInstance();
        if (toast != null) {
            toast.Present(message, kind);
        }
    }

    private static PlayerToast EnsureInstance() {
        if (_instance != null) {
            return _instance;
        }

        Canvas mainCanvas = MainCanvas.Find();
        if (mainCanvas == null) {
            return null;
        }

        GameObject root = new GameObject("PlayerToast", typeof(RectTransform));
        root.transform.SetParent(mainCanvas.transform, false);
        Canvas overlay = root.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 900;
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _instance = root.AddComponent<PlayerToast>();
        _instance.Build();
        return _instance;
    }

    private void Build() {
        _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;
        _group.alpha = 0f;

        _panel = UITheme.CreatePanel(transform, "Toast_Panel", UITheme.PanelRaised);
        RectTransform panelRect = _panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -96f);
        panelRect.sizeDelta = new Vector2(760f, 64f);

        _label = UITheme.CreateText(_panel.transform, "Toast_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        RectTransform labelRect = _label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(20f, 6f);
        labelRect.offsetMax = new Vector2(-20f, -6f);
    }

    private void Present(string message, PlayerToastKind kind) {
        _label.text = message;
        Color edge = kind == PlayerToastKind.Warning ? UITheme.Warning : kind == PlayerToastKind.Success ? UITheme.Positive : UITheme.Info;
        UITheme.AddBorder(_panel, edge, 2f);
        transform.SetAsLastSibling();
        _shownAt = Time.unscaledTime;
        _group.alpha = 1f;
    }

    private void Update() {
        float age = Time.unscaledTime - _shownAt;
        if (age <= VisibleSeconds) {
            return;
        }

        _group.alpha = Mathf.Clamp01(1f - (age - VisibleSeconds) / FadeSeconds);
    }

    private void OnDestroy() {
        if (_instance == this) {
            _instance = null;
        }
    }
}
