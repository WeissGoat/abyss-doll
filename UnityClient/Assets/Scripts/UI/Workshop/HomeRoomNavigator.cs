using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum HomeRoom {
    Hall = 0,
    Studio = 1,
    Attic = 2
}

// The home as three rooms you walk between (docs/specs/2026-10-17-home-loop.md): 大厅 (the workshop's shop floor, the
// existing day screen underneath), 工作室 (改装与维护) and 阁楼 (住处). Each room has its own background, Zero and
// labelled things to click; the wooden tabs top left and the doors in the rooms move between them. Moving costs no
// time. The status bar (TownDayHUD) stays on top in every room; today's choice shows only in the hall.
public class HomeRoomNavigator : MonoBehaviour {
    public const string RootName = "HomeRooms_Runtime";
    public static readonly string[] RoomLabels = { "大厅", "工作室", "阁楼" };
    public const string CounterLabel = "柜台 · 账本";
    public const string DoorLabel = "出门 · 补给铺";
    public const string BedLabel = "床 · 休息";
    public const string JournalLabel = "墙上的手账";
    private const string StudioBackgroundID = "bg_workshop_studio";
    private const string AtticBackgroundID = "bg_workshop_home_room";
    private const string DollVisualID = "doll_proto_0_stand";

    private WorkshopUIController _workshop;
    private GameObject _hallLayer;
    private GameObject _studioRoom;
    private GameObject _atticRoom;
    private StudioBenchView _bench;
    private Text _bedHint;
    private Button _journalTag;
    private HomeJournalView _journal;
    private HomeNightTalkView _nightTalk;
    private GameObject _tabBar;
    private readonly System.Collections.Generic.List<Button> _hallTags = new System.Collections.Generic.List<Button>();
    private readonly System.Collections.Generic.List<Button> _atticTags = new System.Collections.Generic.List<Button>();
    private readonly Button[] _tabs = new Button[3];
    private Image _fade;
    private Coroutine _fadeRoutine;

    public HomeRoom Current { get; private set; }

    public StudioBenchView Bench => _bench;
    public HomeJournalView Journal => _journal;
    public HomeNightTalkView NightTalk => _nightTalk;

    public static HomeRoomNavigator Ensure(Transform workshopRoot, WorkshopUIController workshop) {
        Transform existing = workshopRoot.Find(RootName);
        HomeRoomNavigator navigator = existing != null ? existing.GetComponent<HomeRoomNavigator>() : null;
        if (navigator == null) {
            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(workshopRoot, false);
            UITheme.Stretch((RectTransform)root.transform);
            navigator = root.AddComponent<HomeRoomNavigator>();
            navigator._workshop = workshop;
            navigator.Build();
        }

        navigator._workshop = workshop;
        return navigator;
    }

    public void Show(HomeRoom room) {
        // The night talk ends the day; nothing moves until 晚安.
        if (_nightTalk != null && _nightTalk.IsShowing) {
            return;
        }

        if (room != HomeRoom.Hall && _journal != null && _journal.IsShowing) {
            _journal.Close();
        }

        bool changed = room != Current;
        Current = room;
        Refresh();
        if (changed && isActiveAndEnabled) {
            if (_fadeRoutine != null) {
                StopCoroutine(_fadeRoutine);
            }
            _fadeRoutine = StartCoroutine(FadeIn());
        }
    }

    public void ShowStudio(StudioTab tab) {
        Show(HomeRoom.Studio);
        _bench.ShowTab(tab);
    }

    public void Refresh() {
        _hallLayer.SetActive(Current == HomeRoom.Hall);
        _studioRoom.SetActive(Current == HomeRoom.Studio);
        _atticRoom.SetActive(Current == HomeRoom.Attic);
        for (int i = 0; i < _tabs.Length; i++) {
            bool active = i == (int)Current;
            UITheme.ApplyButtonSkin((Image)_tabs[i].targetGraphic, active ? UITheme.Accent : UITheme.PanelRaised);
            _tabs[i].GetComponentInChildren<Text>().color = active ? UITheme.OnAccent : UITheme.TextPrimary;
        }

        if (Current == HomeRoom.Studio) {
            _bench.Refresh();
        }

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        bool reading = _journal != null && _journal.IsShowing;
        foreach (Button tag in _hallTags) {
            tag.gameObject.SetActive(!reading);
        }
        _journalTag.gameObject.SetActive(!reading && player?.DiveJournal != null && player.DiveJournal.Count > 0);
        _tabBar.SetActive(!_nightTalk.IsShowing);
        foreach (Button tag in _atticTags) {
            tag.gameObject.SetActive(!_nightTalk.IsShowing);
        }
        bool night = player?.IsNight ?? false;
        _bedHint.text = night ? "开张营业之后才睡" : "休息一天，恢复 HP 和 SAN";
        _workshop?.DayHud?.SetChoiceVisible(Current == HomeRoom.Hall);
    }

    private void Build() {
        _hallLayer = new GameObject("HallTags", typeof(RectTransform));
        _hallLayer.transform.SetParent(transform, false);
        UITheme.Stretch((RectTransform)_hallLayer.transform);
        _hallTags.Add(CreateTag(_hallLayer.transform, "Counter_Tag", CounterLabel, "摆货、营业、出售", new Vector2(600f, -40f), OpenCounter));
        _hallTags.Add(CreateTag(_hallLayer.transform, "Door_Tag", DoorLabel, "买修复剂和镇静剂", new Vector2(760f, -330f), () => _workshop?.OpenSupplyShop()));
        _hallTags.Add(CreateTag(_hallLayer.transform, "StudioDoor_Tag", "去工作室", "改装与维护", new Vector2(140f, 150f), () => Show(HomeRoom.Studio)));
        _hallTags.Add(CreateTag(_hallLayer.transform, "Stairs_Tag", "上阁楼", "零号的房间", new Vector2(-140f, 330f), () => Show(HomeRoom.Attic)));
        _journalTag = CreateTag(_hallLayer.transform, "Journal_Tag", JournalLabel, "以前的每一次下潜", new Vector2(-60f, -10f), () => _journal.OpenJournal());
        _hallTags.Add(_journalTag);
        _journal = HomeJournalView.Create(_hallLayer.transform, () => ShowStudio(StudioTab.Maintenance), _ => Refresh());

        _studioRoom = CreateRoom("StudioRoom", StudioBackgroundID, new Vector2(-600f, -100f), new Vector2(540f, 810f));
        _bench = StudioBenchView.Create(_studioRoom.transform, () => _workshop?.RefreshUI());

        _atticRoom = CreateRoom("AtticRoom", AtticBackgroundID, new Vector2(560f, -150f), new Vector2(460f, 690f));
        Button bed = CreateTag(_atticRoom.transform, "Bed_Tag", BedLabel, string.Empty, new Vector2(-60f, -10f), RequestRest);
        _bedHint = bed.transform.Find("Hint_Text").GetComponent<Text>();
        _atticTags.Add(bed);
        _atticTags.Add(CreateTag(_atticRoom.transform, "Ladder_Tag", "下楼 · 大厅", "回到店面", new Vector2(-420f, -330f), () => Show(HomeRoom.Hall)));

        BuildTabs();
        _nightTalk = HomeNightTalkView.Create(transform);

        _fade = UITheme.CreatePanel(transform, "RoomFade_Image", new Color(0.1f, 0.06f, 0.03f, 0f));
        UITheme.Stretch(_fade.rectTransform);
        _fade.raycastTarget = false;

        Current = HomeRoom.Hall;
        Refresh();
    }

    // Zero just came home: the hall, with the dive's page open.
    public void ShowArrival(DiveLog log) {
        Show(HomeRoom.Hall);
        _journal.ShowArrival(log);
    }

    // After the business: upstairs, Zero's word about the day; 晚安 comes down to the hall for the morning.
    public void ShowNightTalk(DayEndReport report, System.Action onGoodnight) {
        Show(HomeRoom.Attic);
        _nightTalk.Show(report, () => {
            Show(HomeRoom.Hall);
            Refresh();
            onGoodnight?.Invoke();
        });
        Refresh();
    }

    private void BuildTabs() {
        GameObject bar = new GameObject("RoomTabs", typeof(RectTransform));
        _tabBar = bar;
        bar.transform.SetParent(transform, false);
        RectTransform barRect = (RectTransform)bar.transform;
        barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0f, 1f);
        barRect.anchoredPosition = new Vector2(180f, -88f);
        barRect.sizeDelta = new Vector2(450f, 52f);
        for (int i = 0; i < _tabs.Length; i++) {
            HomeRoom room = (HomeRoom)i;
            Button tab = UITheme.CreateButton(bar.transform, "RoomTab_" + room, RoomLabels[i], UITheme.PanelRaised, UITheme.TextPrimary);
            UITheme.AddBorder(tab.targetGraphic, UITheme.Border, 1.5f);
            RectTransform rect = (RectTransform)tab.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(i * 150f, 0f);
            rect.sizeDelta = new Vector2(140f, 52f);
            tab.onClick.AddListener(() => Show(room));
            _tabs[i] = tab;
        }
    }

    // A full-screen room: its own background (which also keeps clicks off the hall underneath) and Zero standing in it.
    private GameObject CreateRoom(string name, string backgroundID, Vector2 dollPosition, Vector2 dollSize) {
        Image root = UITheme.CreatePanel(transform, name, UITheme.Background);
        UITheme.Stretch(root.rectTransform);
        root.raycastTarget = true;
        root.gameObject.AddComponent<RectMask2D>();

        Image background = UITheme.CreatePanel(root.transform, "Background_Image", Color.white);
        UITheme.Stretch(background.rectTransform);
        VisualUIHelper.ApplyCoverSprite(background, backgroundID, Color.white, UITheme.Background);

        GameObject display = new GameObject("DollDisplay", typeof(RectTransform));
        display.transform.SetParent(root.transform, false);
        UITheme.Place((RectTransform)display.transform, new Vector2(0.5f, 0.5f), dollPosition, dollSize);
        Image doll = UITheme.CreatePanel(display.transform, "DollImage", Color.white);
        RectTransform dollRect = doll.rectTransform;
        dollRect.anchorMin = dollRect.anchorMax = dollRect.pivot = new Vector2(0.5f, 0f);
        dollRect.anchoredPosition = Vector2.zero;
        VisualUIHelper.ApplyContainSprite(doll, DollVisualID, dollSize, Color.white, UITheme.Border, false);
        return root.gameObject;
    }

    // A small linen plaque hung next to a thing in the room (《火山的女儿》-style): what it is, and one line on what it does.
    private static Button CreateTag(Transform parent, string name, string label, string hint, Vector2 position, System.Action onClick) {
        Button tag = UITheme.CreateButton(parent, name, string.Empty, UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(tag.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)tag.transform, new Vector2(0.5f, 0.5f), position, new Vector2(220f, 74f));
        Text labelText = tag.GetComponentInChildren<Text>();
        labelText.text = label;
        labelText.fontSize = UITheme.BodySize;
        labelText.color = UITheme.Accent;
        labelText.alignment = TextAnchor.UpperCenter;
        labelText.rectTransform.offsetMin = new Vector2(8f, 0f);
        labelText.rectTransform.offsetMax = new Vector2(-8f, -8f);
        Text hintText = UITheme.CreateText(tag.transform, "Hint_Text", hint, UITheme.CaptionSize - 3, UITheme.TextSecondary, TextAnchor.LowerCenter);
        hintText.raycastTarget = false;
        UITheme.Stretch(hintText.rectTransform);
        hintText.rectTransform.offsetMin = new Vector2(8f, 8f);
        hintText.rectTransform.offsetMax = new Vector2(-8f, 0f);
        tag.onClick.AddListener(() => onClick());
        return tag;
    }

    private void OpenCounter() {
        _workshop?.DayHud?.OpenBusiness(GameRoot.Core?.CurrentPlayer?.IsNight ?? false);
    }

    private void RequestRest() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player != null && player.IsNight) {
            PlayerToast.Show("今晚还没开张：先到大厅的柜台营业，开张之后这一天才结束。", PlayerToastKind.Info);
            return;
        }

        _workshop?.DayHud?.RequestRest();
    }

    private IEnumerator FadeIn() {
        const float duration = 0.22f;
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime) {
            _fade.color = new Color(0.1f, 0.06f, 0.03f, 0.6f * (1f - t / duration));
            yield return null;
        }

        _fade.color = new Color(0.1f, 0.06f, 0.03f, 0f);
        _fadeRoutine = null;
    }
}
