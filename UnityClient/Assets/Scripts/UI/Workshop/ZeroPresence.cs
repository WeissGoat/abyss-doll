using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Zero standing in a room (docs/specs/2026-10-18-home-evening-thread.md): a speech bubble beside her that fades after a
// while, the small moves her portrait makes when she answers (a hop, a flinch, leaning in), and three places on her
// that answer a touch (head, shoulder, hand). What she says comes from the backend (HomeEveningService,
// ZeroInteractionService); this only shows it and forwards the touch.
public class ZeroPresence : MonoBehaviour {
    public const float LineSeconds = 5f;
    public const float StorySeconds = 8f;
    // Where the touchable parts sit on the standing portrait (doll_proto_0_stand), as fractions of the image.
    private static readonly Rect HeadArea = new Rect(0.38f, 0.8f, 0.24f, 0.17f);
    private static readonly Rect ShoulderArea = new Rect(0.3f, 0.71f, 0.4f, 0.08f);
    private static readonly Rect HandArea = new Rect(0.33f, 0.48f, 0.2f, 0.1f);

    private Image _bubble;
    private Text _line;
    private Text _emote;
    private Text _note;
    private System.Func<RectTransform> _doll;
    private System.Action<TouchRegion> _onTouch;
    private RectTransform _zonesOn;
    private Coroutine _lineRoutine;
    private Coroutine _moveRoutine;

    public bool IsSpeaking => _bubble != null && _bubble.gameObject.activeSelf;
    public string CurrentLine => IsSpeaking ? _line.text : string.Empty;

    // parent: the room layer; doll: resolves her portrait (the hall's lives on the workshop screen underneath).
    public static ZeroPresence Create(Transform parent, string name, System.Func<RectTransform> doll, Vector2 bubblePosition, float bubbleWidth = 480f) {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        UITheme.Stretch((RectTransform)root.transform);
        ZeroPresence presence = root.AddComponent<ZeroPresence>();
        presence._doll = doll;
        presence.Build(bubblePosition, bubbleWidth);
        return presence;
    }

    // story: a piece of her own story, held longer and set apart. note: a small line under it (羁绊 +1).
    public void Say(string text, string emote = "", string note = "", bool story = false) {
        if (string.IsNullOrEmpty(text)) {
            return;
        }

        _line.text = $"「{text}」";
        _line.color = story ? UITheme.Accent : UITheme.TextPrimary;
        _emote.text = emote ?? string.Empty;
        _emote.gameObject.SetActive(!string.IsNullOrEmpty(emote));
        _note.text = note ?? string.Empty;
        _bubble.gameObject.SetActive(true);
        transform.SetAsLastSibling();
        if (_lineRoutine != null) {
            StopCoroutine(_lineRoutine);
        }

        if (isActiveAndEnabled) {
            _lineRoutine = StartCoroutine(FadeLine(story ? StorySeconds : LineSeconds));
        }
    }

    public void Show(ZeroAnswer answer) {
        if (answer == null) {
            return;
        }

        string note = answer.Bond != null && answer.Bond.PointsAdded > 0 ? $"羁绊 +{answer.Bond.PointsAdded}" : string.Empty;
        Say(answer.Line, answer.Emote, note, answer.IsLore);
        React(answer.Reaction);
        StudioCareView.AnnounceBond(answer.Bond);
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

    // Head, shoulder and hand on her portrait answer a click. Built on the portrait itself so they follow it.
    public void EnableTouch(System.Action<TouchRegion> onTouch) {
        _onTouch = onTouch;
        RectTransform doll = _doll?.Invoke();
        if (doll == null || doll == _zonesOn) {
            return;
        }

        _zonesOn = doll;
        AddZone(doll, TouchRegion.Head, HeadArea);
        AddZone(doll, TouchRegion.Shoulder, ShoulderArea);
        AddZone(doll, TouchRegion.Hand, HandArea);
    }

    public void Touch(TouchRegion region) {
        _onTouch?.Invoke(region);
    }

    private void AddZone(RectTransform doll, TouchRegion region, Rect area) {
        string name = "Touch_" + region;
        if (doll.Find(name) != null) {
            return;
        }

        Image zone = UITheme.CreatePanel(doll, name, new Color(1f, 1f, 1f, 0f));
        zone.raycastTarget = true;
        zone.rectTransform.anchorMin = new Vector2(area.xMin, area.yMin);
        zone.rectTransform.anchorMax = new Vector2(area.xMax, area.yMax);
        zone.rectTransform.offsetMin = Vector2.zero;
        zone.rectTransform.offsetMax = Vector2.zero;
        Button button = zone.gameObject.AddComponent<Button>();
        button.targetGraphic = zone;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => Touch(region));
    }

    private IEnumerator FadeLine(float seconds) {
        CanvasGroup group = _bubble.GetComponent<CanvasGroup>();
        group.alpha = 1f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) {
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

    private void Build(Vector2 bubblePosition, float bubbleWidth) {
        _bubble = UITheme.CreatePanel(transform, "ZeroLineBubble", UITheme.PanelRaised);
        UITheme.AddBorder(_bubble, UITheme.Border, 1.5f);
        _bubble.raycastTarget = false;
        _bubble.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        UITheme.Place(_bubble.rectTransform, new Vector2(0.5f, 0.5f), bubblePosition, new Vector2(bubbleWidth, 96f));
        _line = UITheme.CreateText(_bubble.transform, "Line_Text", string.Empty, UITheme.BodySize - 1, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Stretch(_line.rectTransform);
        _line.rectTransform.offsetMin = new Vector2(18f, 10f);
        _line.rectTransform.offsetMax = new Vector2(-18f, -6f);
        _line.raycastTarget = false;
        _emote = UITheme.CreateText(_bubble.transform, "Emote_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_emote.rectTransform, new Vector2(0f, 1f), new Vector2(6f, 10f), new Vector2(48f, 48f));
        _emote.raycastTarget = false;
        _note = UITheme.CreateText(_bubble.transform, "Note_Text", string.Empty, UITheme.CaptionSize - 3, UITheme.Accent, TextAnchor.LowerRight);
        UITheme.Stretch(_note.rectTransform);
        _note.rectTransform.offsetMin = new Vector2(12f, 4f);
        _note.rectTransform.offsetMax = new Vector2(-12f, 0f);
        _note.raycastTarget = false;
        _bubble.gameObject.SetActive(false);
    }
}
