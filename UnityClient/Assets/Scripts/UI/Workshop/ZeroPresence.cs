using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Zero standing in a room (docs/specs/2026-10-18-home-evening-thread.md): a speech bubble beside her that fades after a
// while, the small moves her portrait makes when she answers (a hop, a flinch, leaning in), and three places on her
// that answer a touch (head, shoulder, hand). When you talk with her she steps up close: her large portrait
// (ZeroBust) with the line's face over the dimmed room, in place of her small figure, until a click sends her back.
// What she says comes from the backend (HomeEveningService, ZeroInteractionService); this only shows it and forwards
// the touch.
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
    private GameObject _closeUp;
    private Image _bust;
    private RectTransform _closeBubble;
    private Text _closeLine;
    private Text _closeNote;
    private CanvasGroup _hiddenDoll;

    public bool IsCloseUp => _closeUp != null && _closeUp.activeSelf;
    public bool IsSpeaking => IsCloseUp || (_bubble != null && _bubble.gameObject.activeSelf);
    public string CurrentLine => IsCloseUp ? _closeLine.text : IsSpeaking ? _line.text : string.Empty;
    // Her face on the close-up portrait (ZeroFace), or empty when she is not up close.
    public string CurrentFace { get; private set; } = string.Empty;

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

        EndCloseUp();
        _line.text = $"「{text}」";
        _line.color = story ? UITheme.Accent : UITheme.TextPrimary;
        _emote.text = emote ?? string.Empty;
        _emote.gameObject.SetActive(!string.IsNullOrEmpty(emote));
        _note.text = note ?? string.Empty;
        _bubble.gameObject.SetActive(true);
        UIPortraitPuppet.On(_doll?.Invoke())?.Talk();
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

    // 和她说话: up close with her face, or the bubble when there is no portrait art.
    public void CloseUp(ZeroAnswer answer) {
        if (answer == null || string.IsNullOrEmpty(answer.Line)) {
            return;
        }

        if (!ZeroBust.HasArt || !isActiveAndEnabled) {
            Show(answer);
            return;
        }

        if (_closeUp == null) {
            BuildCloseUp();
        }

        Hide();
        RectTransform root = (RectTransform)transform;
        RectTransform doll = _doll?.Invoke();
        // Where she stands, kept on screen; the bubble goes toward the middle.
        float x = doll != null ? root.InverseTransformPoint(doll.TransformPoint(doll.rect.center)).x : -560f;
        float reach = Mathf.Max(0f, root.rect.width * 0.5f - ZeroBust.Size.x * 0.5f);
        x = Mathf.Clamp(x, -reach, reach);
        _bust.rectTransform.anchoredPosition = new Vector2(x, 0f);
        _closeBubble.anchoredPosition = new Vector2(x + (x <= 0f ? 1f : -1f) * (ZeroBust.Size.x * 0.5f + 180f), 760f);

        _closeLine.text = $"「{answer.Line}」";
        _closeLine.color = answer.IsLore ? UITheme.Accent : UITheme.TextPrimary;
        _closeNote.text = answer.Bond != null && answer.Bond.PointsAdded > 0 ? $"羁绊 +{answer.Bond.PointsAdded}" : string.Empty;
        _closeUp.SetActive(true);
        CurrentFace = ZeroBust.Apply(_bust, answer.Face, Color.white);
        StandIn(doll, false);
        transform.SetAsLastSibling();
        StudioCareView.AnnounceBond(answer.Bond);
    }

    // Back to her small figure in the room.
    public void EndCloseUp() {
        if (_closeUp != null) {
            _closeUp.SetActive(false);
        }

        CurrentFace = string.Empty;
        StandIn(null, true);
    }

    public void Hide() {
        _bubble.gameObject.SetActive(false);
    }

    // Her small figure out of sight while a view shows her large portrait in its place (the arrival page).
    public void HideFigure(bool hidden) {
        StandIn(hidden ? _doll?.Invoke() : null, !hidden);
    }

    // Her small figure fades out while her portrait is up close, and comes back after.
    private void StandIn(RectTransform doll, bool visible) {
        if (!visible && doll != null) {
            _hiddenDoll = doll.GetComponent<CanvasGroup>();
            if (_hiddenDoll == null) {
                _hiddenDoll = doll.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (_hiddenDoll != null) {
            _hiddenDoll.alpha = visible ? 1f : 0f;
        }

        if (visible) {
            _hiddenDoll = null;
        }
    }

    public void React(ZeroReaction reaction) {
        RectTransform doll = _doll?.Invoke();
        if (doll == null || reaction == ZeroReaction.None || !isActiveAndEnabled) {
            return;
        }

        if (_moveRoutine != null) {
            StopCoroutine(_moveRoutine);
        }

        UIPortraitPuppet.On(doll)?.React(reaction);
        _moveRoutine = StartCoroutine(Move(doll, reaction));
    }

    // Head, shoulder and hand on her portrait answer a click (headOnly: just the head). Built on the portrait itself so
    // they follow it. On a puppet portrait her head can also be patted and stroked, and her hand held (PortraitPatZone).
    public void EnableTouch(System.Action<TouchRegion> onTouch, bool headOnly = false) {
        _onTouch = onTouch;
        RectTransform doll = _doll?.Invoke();
        if (doll == null || doll == _zonesOn) {
            return;
        }

        _zonesOn = doll;
        AddZone(doll, TouchRegion.Head, HeadArea);
        if (!headOnly) {
            AddZone(doll, TouchRegion.Shoulder, ShoulderArea);
            AddZone(doll, TouchRegion.Hand, HandArea);
        }
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
        if (region != TouchRegion.Shoulder) {
            zone.gameObject.AddComponent<PortraitPatZone>().Init(doll, region, () => Touch(region));
        }
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

        EndCloseUp();

        RectTransform doll = _doll?.Invoke();
        if (doll != null) {
            doll.localRotation = Quaternion.identity;
        }
    }

    private void BuildCloseUp() {
        Image dim = UITheme.CreatePanel(transform, "ZeroCloseUp", new Color(0.05f, 0.04f, 0.06f, 0.58f));
        UITheme.Stretch(dim.rectTransform);
        dim.raycastTarget = true;
        Button back = dim.gameObject.AddComponent<Button>();
        back.targetGraphic = dim;
        back.transition = Selectable.Transition.None;
        back.onClick.AddListener(EndCloseUp);
        _closeUp = dim.gameObject;

        _bust = UITheme.CreatePanel(dim.transform, "ZeroBust", Color.white);
        _bust.raycastTarget = false;
        UITheme.Place(_bust.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, ZeroBust.Size);

        Image bubble = UITheme.CreatePanel(dim.transform, "ZeroCloseUpBubble", UITheme.PanelRaised);
        UITheme.AddBorder(bubble, UITheme.Border, 1.5f);
        bubble.raycastTarget = false;
        UITheme.Place(bubble.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(560f, 120f));
        _closeBubble = bubble.rectTransform;
        _closeLine = UITheme.CreateText(bubble.transform, "Line_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Stretch(_closeLine.rectTransform);
        _closeLine.rectTransform.offsetMin = new Vector2(18f, 22f);
        _closeLine.rectTransform.offsetMax = new Vector2(-18f, -6f);
        _closeLine.raycastTarget = false;
        _closeNote = UITheme.CreateText(bubble.transform, "Note_Text", string.Empty, UITheme.CaptionSize - 2, UITheme.Accent, TextAnchor.LowerRight);
        UITheme.Stretch(_closeNote.rectTransform);
        _closeNote.rectTransform.offsetMin = new Vector2(12f, 6f);
        _closeNote.rectTransform.offsetMax = new Vector2(-14f, 0f);
        _closeNote.raycastTarget = false;
        _closeUp.SetActive(false);
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

// Her head or hand on a puppet portrait (UIPortraitPuppet). Head: pressing pats it (she dips under your hand),
// dragging strokes it (she leans with the stroke), and a stroke long enough counts as one touch on her head. Hand:
// holding it lifts her clasped hands to yours, and held long enough it counts as one touch on her hand. A plain click
// stays the zone button's. Without a puppet it does nothing.
public class PortraitPatZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler {
    public const float StrokeDistance = 240f;
    public const float HoldSeconds = 0.9f;

    private RectTransform _doll;
    private TouchRegion _region;
    private System.Action _onTouch;
    private PointerEventData _press;
    private float _pressedAt;
    private float _stroked;
    private bool _counted;

    public TouchRegion Region => _region;
    public UIPortraitPuppet Puppet => UIPortraitPuppet.On(_doll);

    public void Init(RectTransform doll, TouchRegion region, System.Action onTouch) {
        _doll = doll;
        _region = region;
        _onTouch = onTouch;
    }

    public void OnPointerDown(PointerEventData eventData) {
        _press = eventData;
        _pressedAt = Time.unscaledTime;
        _stroked = 0f;
        _counted = false;
        Hold(true);
    }

    public void OnBeginDrag(PointerEventData eventData) {
        // A stroke is not also a click.
        eventData.eligibleForClick = false;
    }

    public void OnDrag(PointerEventData eventData) {
        UIPortraitPuppet puppet = UIPortraitPuppet.On(_doll);
        if (puppet == null || _region != TouchRegion.Head) {
            return;
        }

        puppet.Stroke(eventData.delta.x);
        _stroked += eventData.delta.magnitude;
        if (!_counted && _stroked >= StrokeDistance) {
            Count();
        }
    }

    public void OnPointerUp(PointerEventData eventData) {
        _press = null;
        Hold(false);
    }

    private void Update() {
        if (_press != null && _region == TouchRegion.Hand && !_counted && Time.unscaledTime - _pressedAt >= HoldSeconds
            && UIPortraitPuppet.On(_doll) != null) {
            Count();
        }
    }

    private void Count() {
        _counted = true;
        if (_press != null) {
            _press.eligibleForClick = false;
        }

        _onTouch?.Invoke();
    }

    private void Hold(bool down) {
        UIPortraitPuppet puppet = UIPortraitPuppet.On(_doll);
        if (_region == TouchRegion.Hand) {
            puppet?.HoldHand(down);
        } else {
            puppet?.Press(down);
        }
    }

    // A touch without a hand (P3DevTools.Pat / HoldHand): a pat with two strokes, or holding her hand for a while;
    // then it counts as the touch.
    public IEnumerator Play() {
        UIPortraitPuppet puppet = UIPortraitPuppet.On(_doll);
        if (puppet == null) {
            yield break;
        }

        if (_region == TouchRegion.Hand) {
            puppet.HoldHand(true);
            yield return new WaitForSecondsRealtime(HoldSeconds);
            _onTouch?.Invoke();
            yield return new WaitForSecondsRealtime(1.2f);
            puppet.HoldHand(false);
            yield break;
        }

        yield return puppet.PlayPat();
        _onTouch?.Invoke();
    }

    private void OnDisable() {
        _press = null;
        Hold(false);
    }
}
