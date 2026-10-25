using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Caring for Zero by hand in the studio (docs/specs/2026-10-17-home-loop.md, batch 3): what the dive left on her is
// drawn on her portrait. Smudges are rubbed off with the mouse (free; a little SAN and bond); a crack or the dark haze
// opens the treatments for it (MaintenanceService). A gauge beside her shows the bond. Rules live in CareService and
// BondService.
public class StudioCareView : MonoBehaviour {
    public const string RootName = "StudioCare_Runtime";

    // Where marks sit on her portrait, as fractions of its rect (x from the left, y from the bottom): cape, dress,
    // skirt and legs, never the face.
    private static readonly Vector2[] GrimeSpots = {
        new Vector2(0.43f, 0.56f), new Vector2(0.58f, 0.46f), new Vector2(0.36f, 0.36f),
        new Vector2(0.54f, 0.30f), new Vector2(0.465f, 0.14f), new Vector2(0.56f, 0.06f)
    };
    private static readonly Vector2[] CrackSpots = { new Vector2(0.38f, 0.63f), new Vector2(0.6f, 0.52f), new Vector2(0.47f, 0.22f) };
    private static readonly Vector2[] HazeSpots = { new Vector2(0.50f, 0.32f), new Vector2(0.41f, 0.48f), new Vector2(0.60f, 0.62f) };

    private RectTransform _portrait;
    private readonly List<GameObject> _marks = new List<GameObject>();
    private Text _hint;
    private Text _bondText;
    private Text _nextText;
    private Image _bondFill;
    private GameObject _popover;
    private Text _popoverTitle;
    private Transform _popoverList;
    private Text _popoverMessage;
    private readonly List<GameObject> _popoverRows = new List<GameObject>();
    private CareMarkKind _popoverKind;
    private System.Action _onChanged;

    public static StudioCareView Create(Transform room, RectTransform portrait, System.Action onChanged) {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(room, false);
        UITheme.Stretch((RectTransform)root.transform);
        StudioCareView view = root.AddComponent<StudioCareView>();
        view._portrait = portrait;
        view._onChanged = onChanged;
        view.Build();
        return view;
    }

    public void Refresh() {
        foreach (GameObject mark in _marks) {
            if (mark != null) {
                DestroyNow(mark);
            }
        }
        _marks.Clear();

        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        int grime = CareService.Count(doll, CareMarkKind.Grime);
        int cracks = CareService.Count(doll, CareMarkKind.Crack);
        int haze = CareService.Count(doll, CareMarkKind.Haze);
        for (int i = 0; i < haze; i++) {
            _marks.Add(CreateMark(CareMarkKind.Haze, HazeSpots[i], 150f, i));
        }
        for (int i = 0; i < cracks; i++) {
            _marks.Add(CreateMark(CareMarkKind.Crack, CrackSpots[i], 96f, i));
        }
        for (int i = 0; i < grime; i++) {
            _marks.Add(CreateMark(CareMarkKind.Grime, GrimeSpots[i], 70f, i));
        }

        List<string> hints = new List<string>();
        if (grime > 0) hints.Add("按住拖过污渍，擦掉");
        if (cracks > 0) hints.Add("点裂痕上油");
        if (haze > 0) hints.Add("点黑雾净化");
        _hint.text = hints.Count > 0 ? string.Join("；", hints) : "她身上很干净。";

        int level = BondService.Level(doll);
        int needed = BondService.Needed(doll);
        _bondText.text = needed > 0 ? $"羁绊 {level} 级  {BondService.Points(doll)}/{needed}" : $"羁绊 {level} 级";
        _nextText.text = level < BondService.MaxLevel ? $"下一级：{HomeLoop.UnlockText(level + 1)}" : "她很信任你。";
        _bondFill.fillAmount = needed > 0 ? (float)BondService.Points(doll) / needed : 1f;

        if (_popover.activeSelf) {
            BuildPopoverRows();
        }
    }

    private GameObject CreateMark(CareMarkKind kind, Vector2 spot, float size, int index) {
        Image mark = UITheme.CreatePanel(_portrait, $"{kind}Mark_{index}", Color.white);
        mark.sprite = CareMarkSprites.Get(kind, index);
        mark.color = kind == CareMarkKind.Grime ? new Color(0.30f, 0.21f, 0.14f, 0.78f)
            : kind == CareMarkKind.Crack ? new Color(0.22f, 0.13f, 0.10f, 0.9f)
            : new Color(0.20f, 0.10f, 0.28f, 0.62f);
        RectTransform rect = mark.rectTransform;
        rect.anchorMin = rect.anchorMax = spot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        mark.raycastTarget = true;
        UIPuppetPin.Attach(mark);
        if (kind == CareMarkKind.Grime) {
            mark.gameObject.AddComponent<GrimeSpot>().Init(this);
        } else {
            Button button = mark.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => OpenTreatments(kind));
            if (kind == CareMarkKind.Haze) {
                mark.gameObject.AddComponent<HazeDrift>();
            }
        }

        return mark.gameObject;
    }

    // A smudge rubbed away (GrimeSpot calls this once it is gone).
    public void Wiped() {
        if (CareService.Wipe(GameRoot.Core?.CurrentPlayer, out BondChange bond, out WishResult wish)) {
            GameAudio.PlaySfx("click");
            AnnounceBond(bond);
            HomeWishView.AnnounceWish(wish);
        }

        Refresh();
        _onChanged?.Invoke();
    }

    private void OpenTreatments(CareMarkKind kind) {
        _popoverKind = kind;
        _popoverTitle.text = kind == CareMarkKind.Crack ? "补裂痕" : "净化黑雾";
        _popoverMessage.text = string.Empty;
        _popover.SetActive(true);
        _popover.transform.SetAsLastSibling();
        BuildPopoverRows();
    }

    private void BuildPopoverRows() {
        foreach (GameObject row in _popoverRows) {
            if (row != null) {
                DestroyNow(row);
            }
        }
        _popoverRows.Clear();

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        List<MaintenanceConfig> options = CareService.Treatments(_popoverKind);
        for (int i = 0; i < options.Count; i++) {
            MaintenanceConfig config = options[i];
            bool can = MaintenanceService.CanApply(player, player?.ActiveDoll, config.MaintenanceID, out string reason);
            Image row = UITheme.CreatePanel(_popoverList, "Treatment_" + config.MaintenanceID, UITheme.PanelRaised);
            StudioBenchView.PlaceRow(row.rectTransform, i, 76f, 8f);
            string cost = StudioBenchView.DescribeCost(HomeUpgradeService.MaintenanceCost(player, config.Cost));
            Text label = UITheme.CreateText(row.transform, "Label_Text", $"{config.Name}    {cost}\n{config.Description}{(can ? string.Empty : "  · " + reason)}", UITheme.CaptionSize - 2, can ? UITheme.TextPrimary : UITheme.TextMuted);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(14f, 4f);
            label.rectTransform.offsetMax = new Vector2(-124f, -4f);
            Button apply = UITheme.CreateButton(row.transform, "Apply_Button", _popoverKind == CareMarkKind.Crack ? "上油" : "净化", can ? UITheme.Accent : UITheme.Border, can ? UITheme.OnAccent : UITheme.TextMuted, UITheme.CaptionSize);
            apply.interactable = can;
            UITheme.Place((RectTransform)apply.transform, new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(104f, 50f));
            string id = config.MaintenanceID;
            apply.onClick.AddListener(() => ApplyTreatment(id));
            _popoverRows.Add(row.gameObject);
        }
    }

    private void ApplyTreatment(string maintenanceID) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        MaintenanceApplicationResult result = MaintenanceService.Apply(player, player?.ActiveDoll, maintenanceID);
        if (!result.Success) {
            _popoverMessage.text = result.Reason;
            _popoverMessage.color = UITheme.Warning;
            BuildPopoverRows();
            return;
        }

        GameAudio.PlaySfx("coin");
        AnnounceBond(result.Bond);
        if (CareService.Count(player.ActiveDoll, _popoverKind) == 0) {
            _popover.SetActive(false);
        } else {
            _popoverMessage.text = "好一些了。";
            _popoverMessage.color = UITheme.Positive;
        }

        Refresh();
        _onChanged?.Invoke();
    }

    public static void AnnounceBond(BondChange bond) {
        if (bond != null && bond.LeveledUp) {
            PlayerToast.Show($"和零号的羁绊到了 {bond.LevelAfter} 级：{HomeLoop.UnlockText(bond.LevelAfter)}。", PlayerToastKind.Success);
        }
    }

    private void Build() {
        // Under the room tabs, clear of her head and feet.
        Image plate = UITheme.CreatePanel(transform, "CarePlate", UITheme.Panel);
        RectTransform plateRect = plate.rectTransform;
        plateRect.anchorMin = plateRect.anchorMax = plateRect.pivot = new Vector2(0f, 1f);
        plateRect.anchoredPosition = new Vector2(24f, -152f);
        plateRect.sizeDelta = new Vector2(560f, 120f);
        plate.raycastTarget = false;
        _bondText = UITheme.CreateText(plate.transform, "Bond_Text", string.Empty, UITheme.BodySize - 2, UITheme.Accent);
        RectTransform bondRect = _bondText.rectTransform;
        bondRect.anchorMin = bondRect.anchorMax = bondRect.pivot = new Vector2(0f, 1f);
        bondRect.anchoredPosition = new Vector2(22f, -12f);
        bondRect.sizeDelta = new Vector2(240f, 32f);
        Image track = UITheme.CreatePanel(plate.transform, "BondTrack", UITheme.Inset);
        RectTransform trackRect = track.rectTransform;
        trackRect.anchorMin = trackRect.anchorMax = trackRect.pivot = new Vector2(1f, 1f);
        trackRect.anchoredPosition = new Vector2(-22f, -22f);
        trackRect.sizeDelta = new Vector2(240f, 14f);
        _nextText = UITheme.CreateText(plate.transform, "BondNext_Text", string.Empty, UITheme.CaptionSize - 2, UITheme.TextPrimary);
        RectTransform nextRect = _nextText.rectTransform;
        nextRect.anchorMin = nextRect.anchorMax = nextRect.pivot = new Vector2(0f, 1f);
        nextRect.anchoredPosition = new Vector2(22f, -46f);
        nextRect.sizeDelta = new Vector2(520f, 28f);
        _bondFill = UITheme.CreatePanel(track.transform, "BondFill", UITheme.Accent);
        UITheme.Stretch(_bondFill.rectTransform);
        _bondFill.sprite = CareMarkSprites.Solid;
        _bondFill.type = Image.Type.Filled;
        _bondFill.fillMethod = Image.FillMethod.Horizontal;
        _hint = UITheme.CreateText(plate.transform, "CareHint_Text", string.Empty, UITheme.CaptionSize - 1, UITheme.TextSecondary);
        RectTransform hintRect = _hint.rectTransform;
        hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(0f, 0f);
        hintRect.anchoredPosition = new Vector2(22f, 10f);
        hintRect.sizeDelta = new Vector2(520f, 30f);

        Image popover = UITheme.CreatePanel(transform, "TreatmentPopover", UITheme.Panel);
        UITheme.ApplyBoardSkin(popover, VisualAssetService.UIPanelMainID);
        UITheme.Place(popover.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-260f, 140f), new Vector2(560f, 420f));
        _popover = popover.gameObject;
        _popoverTitle = UITheme.CreateText(popover.transform, "Title_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent);
        UITheme.Place(_popoverTitle.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -40f), new Vector2(240f, 40f));
        Button close = UITheme.CreateButton(popover.transform, "PopoverClose_Button", "先这样", UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize);
        UITheme.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-84f, -40f), new Vector2(120f, 46f));
        close.onClick.AddListener(() => _popover.SetActive(false));
        GameObject list = new GameObject("Treatment_List", typeof(RectTransform));
        list.transform.SetParent(popover.transform, false);
        RectTransform listRect = (RectTransform)list.transform;
        listRect.anchorMin = Vector2.zero;
        listRect.anchorMax = Vector2.one;
        listRect.offsetMin = new Vector2(28f, 64f);
        listRect.offsetMax = new Vector2(-28f, -80f);
        _popoverList = list.transform;
        _popoverMessage = UITheme.CreateText(popover.transform, "Message_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary);
        UITheme.Place(_popoverMessage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(500f, 32f));
        _popover.SetActive(false);
    }

    private static void DestroyNow(GameObject target) {
        if (Application.isPlaying) {
            Destroy(target);
        } else {
            DestroyImmediate(target);
        }
    }
}

// A smudge on Zero: pressing and moving over it rubs it off (alpha falls with the distance moved); gone, it tells the
// care view.
public class GrimeSpot : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler {
    public const float RubDistance = 140f;

    private StudioCareView _care;
    private Image _image;
    private float _left = 1f;
    private bool _done;

    public void Init(StudioCareView care) {
        _care = care;
        _image = GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData) {
        Rub(RubDistance * 0.2f);
    }

    public void OnDrag(PointerEventData eventData) {
        Rub(eventData.delta.magnitude);
    }

    // Dragging across from a neighbouring smudge rubs this one too.
    public void OnPointerEnter(PointerEventData eventData) {
        if (eventData.dragging) {
            Rub(RubDistance * 0.2f);
        }
    }

    // One full rub (P3DevTools and tests).
    public void RubAway() {
        Rub(RubDistance);
    }

    private void Rub(float distance) {
        if (_done) {
            return;
        }

        _left -= distance / RubDistance;
        Color color = _image.color;
        color.a = Mathf.Clamp01(_left) * 0.78f;
        _image.color = color;
        if (_left <= 0f) {
            _done = true;
            _care.Wiped();
        }
    }
}

// The haze breathes a little so it reads as something alive, not a stain.
public class HazeDrift : MonoBehaviour {
    private Image _image;
    private float _phase;
    private float _baseAlpha;

    private void Awake() {
        _image = GetComponent<Image>();
        _baseAlpha = _image.color.a;
        _phase = Random.value * 6f;
    }

    private void Update() {
        Color color = _image.color;
        color.a = _baseAlpha * (0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 1.3f + _phase));
        _image.color = color;
        transform.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(Time.unscaledTime * 0.9f + _phase));
    }
}

// Mark shapes drawn once at runtime: a soft ragged blob (smudge), a jagged line (crack), a wide soft cloud (haze).
public static class CareMarkSprites {
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    private static Sprite _solid;

    public static Sprite Solid {
        get {
            if (_solid == null) {
                Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) {
                    pixels[i] = Color.white;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                _solid = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            }

            return _solid;
        }
    }

    public static Sprite Get(CareMarkKind kind, int variant) {
        string key = kind + "_" + variant;
        if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null) {
            return sprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Color[] pixels = new Color[size * size];
        System.Random random = new System.Random(17 + variant * 31 + (int)kind * 101);
        if (kind == CareMarkKind.Crack) {
            DrawCrack(pixels, size, random);
        } else {
            DrawBlob(pixels, size, random, kind == CareMarkKind.Haze ? 0.95f : 0.7f, kind == CareMarkKind.Haze ? 0.6f : 0.25f);
        }

        texture.SetPixels(pixels);
        texture.Apply();
        sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        Cache[key] = sprite;
        return sprite;
    }

    private static void DrawBlob(Color[] pixels, int size, System.Random random, float radius, float softness) {
        float[] lobes = new float[7];
        for (int i = 0; i < lobes.Length; i++) {
            lobes[i] = 0.8f + 0.35f * (float)random.NextDouble();
        }

        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float angle = Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 0.5f;
                float lobe = lobes[Mathf.Clamp((int)(angle * lobes.Length), 0, lobes.Length - 1)];
                float distance = Mathf.Sqrt(dx * dx + dy * dy) / (radius * lobe);
                float edge = Mathf.Clamp01((distance - (1f - softness)) / softness);
                float alpha = 1f - edge * edge * (3f - 2f * edge);
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            }
        }
    }

    private static void DrawCrack(Color[] pixels, int size, System.Random random) {
        Vector2 point = new Vector2(size * 0.2f, size * (0.3f + 0.4f * (float)random.NextDouble()));
        for (int step = 0; step < 9; step++) {
            Vector2 next = point + new Vector2(size * 0.08f, size * ((float)random.NextDouble() - 0.5f) * 0.22f);
            for (float t = 0f; t <= 1f; t += 0.05f) {
                Vector2 at = Vector2.Lerp(point, next, t);
                for (int oy = -2; oy <= 2; oy++) {
                    for (int ox = -2; ox <= 2; ox++) {
                        int px = Mathf.Clamp((int)at.x + ox, 0, size - 1);
                        int py = Mathf.Clamp((int)at.y + oy, 0, size - 1);
                        pixels[py * size + px] = Color.white;
                    }
                }
            }

            if (step == 4) {
                // A short branch off the middle.
                Vector2 branch = point + new Vector2(size * 0.1f, size * 0.18f * (random.NextDouble() < 0.5 ? 1f : -1f));
                for (float t = 0f; t <= 1f; t += 0.05f) {
                    Vector2 at = Vector2.Lerp(point, branch, t);
                    pixels[Mathf.Clamp((int)at.y, 0, size - 1) * size + Mathf.Clamp((int)at.x, 0, size - 1)] = Color.white;
                }
            }

            point = next;
        }
    }
}
