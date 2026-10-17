using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum ZeroReaction {
    None = 0,
    Hop = 1,
    Flinch = 2,
    Lean = 3
}

// Zero standing in a room (docs/specs/2026-10-18-home-evening-thread.md): a speech bubble beside her that fades after a
// while, and the small moves her portrait makes when she answers (a hop, a flinch, leaning in). What she says comes
// from the backend (HomeEveningService, ZeroInteractionService); this only shows it.
public class ZeroPresence : MonoBehaviour {
    public const float LineSeconds = 5f;

    private Image _bubble;
    private Text _line;
    private Text _emote;
    private System.Func<RectTransform> _doll;
    private Coroutine _lineRoutine;
    private Coroutine _moveRoutine;

    public bool IsSpeaking => _bubble != null && _bubble.gameObject.activeSelf;
    public string CurrentLine => IsSpeaking ? _line.text : string.Empty;

    // parent: the room layer; doll: resolves her portrait (the hall's lives on the workshop screen underneath).
    public static ZeroPresence Create(Transform parent, string name, System.Func<RectTransform> doll, Vector2 bubblePosition) {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        UITheme.Stretch((RectTransform)root.transform);
        ZeroPresence presence = root.AddComponent<ZeroPresence>();
        presence._doll = doll;
        presence.Build(bubblePosition);
        return presence;
    }

    public void Say(string text, string emote = "") {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        _line.text = $"「{text}」";
        _emote.text = emote ?? string.Empty;
        _emote.gameObject.SetActive(!string.IsNullOrEmpty(emote));
        _bubble.gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (_lineRoutine != null) {
            StopCoroutine(_lineRoutine);
        }

        if (isActiveAndEnabled) {
            _lineRoutine = StartCoroutine(FadeLine());
        }
    }

    public void Hide() {
        _bubble.gameObject.SetActive(false);
    }

    public void React(ZeroReaction reaction) {
        RectTransform doll = _doll?.Invoke();
        if (doll == null || reaction == ZeroReaction.None || !isActiveAndEnabled) {
            return;
        }

        if (_moveRoutine != null) {
            StopCoroutine(_moveRoutine);
        }

        _moveRoutine = StartCoroutine(Move(doll, reaction));
    }

    private IEnumerator FadeLine() {
        CanvasGroup group = _bubble.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        for (float t = 0f; t < LineSeconds; t += Time.unscaledDeltaTime) {
            yield return null;
        }

        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime) {
            group.alpha = 1f - t / 0.4f;
            yield return null;
        }

        group.alpha = 1f;
        _bubble.gameObject.SetActive(false);
        _lineRoutine = null;
    }

    // A small move around her rest pose, always back to it.
    private IEnumerator Move(RectTransform doll, ZeroReaction reaction) {
        Vector2 home = doll.anchoredPosition;
        const float duration = 0.45f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime) {
            float k = t / duration;
            float wave = Mathf.Sin(k * Mathf.PI);
            switch (reaction) {
                case ZeroReaction.Hop:
                    doll.anchoredPosition = home + new Vector2(0f, 26f * wave);
                    break;
                case ZeroReaction.Flinch:
                    doll.anchoredPosition = home + new Vector2(-18f * Mathf.Sin(k * Mathf.PI * 3f) * (1f - k), -8f * wave);
                    break;
                case ZeroReaction.Lean:
                    doll.localRotation = Quaternion.Euler(0f, 0f, -4f * wave);
                    doll.anchoredPosition = home + new Vector2(14f * wave, 0f);
                    break;
            }

            yield return null;
        }

        doll.anchoredPosition = home;
        doll.localRotation = Quaternion.identity;
        _moveRoutine = null;
    }

    private void OnDisable() {
        _lineRoutine = null;
        _moveRoutine = null;
        if (_bubble != null) {
            _bubble.gameObject.SetActive(false);
        }

        RectTransform doll = _doll?.Invoke();
        if (doll != null) {
            doll.localRotation = Quaternion.identity;
        }
    }

    private void Build(Vector2 bubblePosition) {
        _bubble = UITheme.CreatePanel(transform, "ZeroLineBubble", UITheme.PanelRaised);
        UITheme.AddBorder(_bubble, UITheme.Border, 1.5f);
        _bubble.raycastTarget = false;
        _bubble.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        UITheme.Place(_bubble.rectTransform, new Vector2(0.5f, 0.5f), bubblePosition, new Vector2(460f, 90f));
        _line = UITheme.CreateText(_bubble.transform, "Line_Text", string.Empty, UITheme.BodySize - 1, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Stretch(_line.rectTransform);
        _line.rectTransform.offsetMin = new Vector2(16f, 6f);
        _line.rectTransform.offsetMax = new Vector2(-16f, -6f);
        _line.raycastTarget = false;
        _emote = UITheme.CreateText(_bubble.transform, "Emote_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_emote.rectTransform, new Vector2(0f, 1f), new Vector2(6f, 10f), new Vector2(48f, 48f));
        _emote.raycastTarget = false;
        _bubble.gameObject.SetActive(false);
    }
}
