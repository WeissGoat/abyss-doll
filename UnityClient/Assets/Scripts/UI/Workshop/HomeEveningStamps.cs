using UnityEngine;
using UnityEngine.UI;

// Tonight's four stamps under the status bar (docs/specs/2026-10-18-home-evening-thread.md, A): 卸货 · 照看 · 开张 ·
// 晚安. Done ones are filled, the next one is outlined and pulses, the rest wait. Clicking one walks to where it is
// done. State comes from HomeEveningService.
public class HomeEveningStamps : MonoBehaviour {
    public const string RootName = "EveningStamps";
    public static readonly string[] Labels = { "卸货", "照看", "开张", "晚安" };

    private readonly Button[] _stamps = new Button[4];
    private readonly TagPulse[] _pulses = new TagPulse[4];
    private System.Action<EveningStamp> _onClick;

    public static HomeEveningStamps Create(Transform parent, Vector2 topLeft, System.Action<EveningStamp> onClick) {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeft;
        rect.sizeDelta = new Vector2(4 * 118f, 52f);
        HomeEveningStamps stamps = root.AddComponent<HomeEveningStamps>();
        stamps._onClick = onClick;
        stamps.Build();
        return stamps;
    }

    // goodnightReached: the business is open and the night talk is on (晚安 is the step left).
    public void Refresh(PlayerProfile player, bool visible, bool goodnightReached) {
        gameObject.SetActive(visible && player != null);
        if (!gameObject.activeSelf) {
            return;
        }

        EveningStamp next = goodnightReached ? EveningStamp.Goodnight : HomeEveningService.Next(player);
        for (int i = 0; i < _stamps.Length; i++) {
            EveningStamp stamp = (EveningStamp)i;
            bool done = stamp == EveningStamp.Business ? goodnightReached : stamp != EveningStamp.Goodnight && HomeEveningService.Done(player, stamp);
            bool isNext = stamp == next;
            Image image = (Image)_stamps[i].targetGraphic;
            UITheme.ApplyButtonSkin(image, done ? UITheme.Accent : isNext ? UITheme.PanelRaised : UITheme.Inset);
            Outline border = image.GetComponent<Outline>();
            if (border != null) {
                border.effectColor = isNext ? UITheme.Accent : UITheme.Border;
            }

            Text label = _stamps[i].GetComponentInChildren<Text>();
            label.text = done ? "● " + Labels[i] : Labels[i];
            label.color = done ? UITheme.OnAccent : isNext ? UITheme.Accent : UITheme.TextMuted;
            _pulses[i].enabled = isNext;
        }
    }

    private void Build() {
        for (int i = 0; i < _stamps.Length; i++) {
            EveningStamp stamp = (EveningStamp)i;
            Button button = UITheme.CreateButton(transform, "Stamp_" + stamp, Labels[i], UITheme.Inset, UITheme.TextMuted, UITheme.CaptionSize);
            UITheme.AddBorder(button.targetGraphic, UITheme.Border, 1.5f);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(i * 118f + 54f, -26f);
            rect.sizeDelta = new Vector2(108f, 46f);
            button.onClick.AddListener(() => _onClick?.Invoke(stamp));
            _stamps[i] = button;
            _pulses[i] = button.gameObject.AddComponent<TagPulse>();
            _pulses[i].enabled = false;
        }
    }
}

// A gentle pulse on a thing to click next (a stamp, a room plaque).
public class TagPulse : MonoBehaviour {
    public float Amount = 0.06f;

    private void Update() {
        float s = 1f + Amount * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
        transform.localScale = new Vector3(s, s, 1f);
    }

    private void OnDisable() {
        transform.localScale = Vector3.one;
    }
}
