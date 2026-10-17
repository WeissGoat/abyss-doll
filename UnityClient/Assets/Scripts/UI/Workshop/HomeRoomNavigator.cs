using System.Collections;
using UnityEngine;
using UnityEngine.UI;

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
    public const string ShelfLabel = "架子 · 纪念物";
    public const string TalkLabel = "和她说话";
    private const string StudioBackgroundID = "bg_workshop_studio";
    private const string AtticBackgroundID = "bg_workshop_home_room";
    private const string DollVisualID = "doll_proto_0_stand";

    private WorkshopUIController _workshop;
    private GameObject _hallLayer;
    private GameObject _studioRoom;
    private GameObject _atticRoom;
    private StudioBenchView _bench;
    private StudioCareView _care;
    private Text _bedHint;
    private Button _journalTag;
    private HomeJournalView _journal;
    private HomeNightTalkView _nightTalk;
    private HomeWishView _wish;
    private HomeShelfView _shelf;
    private GameObject _tabBar;
    private readonly System.Collections.Generic.List<Button> _hallTags = new System.Collections.Generic.List<Button>();
    private readonly System.Collections.Generic.List<Button> _atticTags = new System.Collections.Generic.List<Button>();
    private readonly Button[] _tabs = new Button[3];
    private Image _fade;
    private Image _hallLight;
    private Image _lampGlow;
    private readonly System.Collections.Generic.List<Image> _roomLights = new System.Collections.Generic.List<Image>();
    // Evening (after the day's dive or rest, before the business): a warm dusk over every room.
    private static readonly Color EveningLight = new Color(0.42f, 0.2f, 0.1f, 0.2f);
    private Coroutine _fadeRoutine;
    private HomeEveningStamps _stamps;
    private HomeUnpackView _unpack;
    private ZeroPresence _hallZero;
    private ZeroPresence _studioZero;
    private ZeroPresence _atticZero;
    private Button _counterTag;
    private Button _studioDoorTag;
    private Text _counterHint;
    private bool _quiet;
    // Rooms she has already spoken in, keyed by day and hour, so each says its line once.
    private readonly System.Collections.Generic.HashSet<string> _spoken = new System.Collections.Generic.HashSet<string>();

    public HomeRoom Current { get; private set; }

    public StudioBenchView Bench => _bench;
    public StudioCareView Care => _care;
    public HomeJournalView Journal => _journal;
    public HomeNightTalkView NightTalk => _nightTalk;
    public HomeWishView Wish => _wish;
    public HomeShelfView Shelf => _shelf;
    public HomeUnpackView Unpack => _unpack;
    public HomeEveningStamps Stamps => _stamps;

    public ZeroPresence Zero(HomeRoom room) {
        return room == HomeRoom.Studio ? _studioZero : room == HomeRoom.Attic ? _atticZero : _hallZero;
    }

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

        SpeakOnEntering(room);
    }

    // Walking into a room she says one thing, once per room each evening and each morning; the hall's morning line is
    // the send-off at the door.
    public void SpeakOnEntering(HomeRoom room) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null || _quiet || (_journal != null && _journal.IsShowing) || (_nightTalk != null && _nightTalk.IsShowing)) {
            return;
        }

        string key = $"{player.CurrentDay}:{player.IsNight}:{room}";
        if (!_spoken.Add(key)) {
            return;
        }

        string line = room == HomeRoom.Hall && !player.IsNight ? HomeEveningService.MorningLine(player) : HomeEveningService.RoomLine(player, room);
        Zero(room)?.Say(line);
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
            _care.Refresh();
        }

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        bool reading = _journal != null && _journal.IsShowing;
        foreach (Button tag in _hallTags) {
            tag.gameObject.SetActive(!reading);
        }
        _journalTag.gameObject.SetActive(!reading && player?.DiveJournal != null && player.DiveJournal.Count > 0);
        _wish.Refresh(reading);
        _tabBar.SetActive(!_nightTalk.IsShowing);
        foreach (Button tag in _atticTags) {
            tag.gameObject.SetActive(!_nightTalk.IsShowing);
        }
        bool night = player?.IsNight ?? false;
        bool mattress = HomeUpgradeService.Has(player, "upg_mattress");
        _bedHint.text = night ? "开张营业之后才睡" : mattress ? "新床垫：休息一天，SAN 回得更多" : "休息一天，恢复 HP 和 SAN";
        Color light = night && !_nightTalk.IsShowing ? EveningLight : Color.clear;
        _hallLight.color = light;
        foreach (Image roomLight in _roomLights) {
            roomLight.color = light;
        }
        _lampGlow.gameObject.SetActive(HomeUpgradeService.Has(player, "upg_bench_lamp"));
        _workshop?.DayHud?.SetChoiceVisible(Current == HomeRoom.Hall);

        bool talk = _nightTalk.IsShowing;
        if (Current != HomeRoom.Studio) {
            Zero(Current).EnableTouch(region => Touch(Current, region));
        }

        _stamps.Refresh(player, night || talk, talk);
        EveningStamp next = talk ? EveningStamp.Goodnight : HomeEveningService.Next(player);
        int haul = night ? HomeEveningService.Haul(player).Count : 0;
        _counterHint.text = haul > 0 ? $"卸货：今天带回 {haul} 件" : night ? "摆货，开张营业" : "摆货、营业、出售";
        SetPulse(_counterTag, night && !talk && (next == EveningStamp.Unpack || next == EveningStamp.Business));
        SetPulse(_studioDoorTag, night && !talk && next == EveningStamp.Care);
    }

    private static void SetPulse(Button tag, bool on) {
        TagPulse pulse = tag.GetComponent<TagPulse>();
        if (pulse == null) {
            pulse = tag.gameObject.AddComponent<TagPulse>();
        }

        pulse.enabled = on;
    }

    private void Build() {
        _hallLight = UITheme.CreatePanel(transform, "HallLight_Image", Color.clear);
        UITheme.Stretch(_hallLight.rectTransform);
        _hallLight.raycastTarget = false;

        _hallLayer = new GameObject("HallTags", typeof(RectTransform));
        _hallLayer.transform.SetParent(transform, false);
        UITheme.Stretch((RectTransform)_hallLayer.transform);
        _counterTag = CreateTag(_hallLayer.transform, "Counter_Tag", CounterLabel, "摆货、营业、出售", new Vector2(600f, -40f), OpenCounter);
        _counterHint = _counterTag.transform.Find("Hint_Text").GetComponent<Text>();
        _hallTags.Add(_counterTag);
        _hallTags.Add(CreateTag(_hallLayer.transform, "Door_Tag", DoorLabel, "买修复剂和镇静剂", new Vector2(760f, -330f), () => _workshop?.OpenSupplyShop()));
        _studioDoorTag = CreateTag(_hallLayer.transform, "StudioDoor_Tag", "去工作室", "改装与维护", new Vector2(140f, 150f), () => Show(HomeRoom.Studio));
        _hallTags.Add(_studioDoorTag);
        _hallTags.Add(CreateTag(_hallLayer.transform, "Stairs_Tag", "上阁楼", "零号的房间", new Vector2(60f, 340f), () => Show(HomeRoom.Attic)));
        _journalTag = CreateTag(_hallLayer.transform, "Journal_Tag", JournalLabel, "以前的每一次下潜", new Vector2(-60f, -10f), () => _journal.OpenJournal());
        _hallTags.Add(_journalTag);
        _journal = HomeJournalView.Create(_hallLayer.transform, () => ShowStudio(StudioTab.Maintenance), showing => {
            Refresh();
            if (!showing) {
                SpeakOnEntering(HomeRoom.Hall);
            }
        });
        _wish = HomeWishView.Create(_hallLayer.transform, transform, () => {
            Refresh();
            _workshop?.RefreshUI();
        });
        // The hall's Zero is the workshop screen's portrait underneath.
        _hallZero = ZeroPresence.Create(_hallLayer.transform, "HallZero", () => _workshop != null ? _workshop.transform.Find("DollDisplay/DollImage") as RectTransform : null, new Vector2(-250f, 180f));
        _hallTags.Add(CreateTag(_hallLayer.transform, "HallTalk_Tag", TalkLabel, "摸摸头、牵牵手也行", new Vector2(-190f, -230f), () => Talk(HomeRoom.Hall)));

        _studioRoom = CreateRoom("StudioRoom", StudioBackgroundID, new Vector2(-600f, -100f), new Vector2(540f, 810f));
        _bench = StudioBenchView.Create(_studioRoom.transform, () => {
            _care?.Refresh();
            _workshop?.RefreshUI();
        });
        // The bench lamp (home upgrade): a warm pool of light on the workbench.
        _lampGlow = UITheme.CreatePanel(_studioRoom.transform, "LampGlow_Image", new Color(1f, 0.8f, 0.45f, 0.2f));
        _lampGlow.sprite = CareMarkSprites.Get(CareMarkKind.Haze, 7);
        _lampGlow.raycastTarget = false;
        UITheme.Place(_lampGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-160f, -60f), new Vector2(520f, 340f));
        _lampGlow.transform.SetSiblingIndex(2);
        _care = StudioCareView.Create(_studioRoom.transform, (RectTransform)_studioRoom.transform.Find("DollDisplay/DollImage"), () => {
            _bench.Refresh();
            _workshop?.RefreshUI();
            Refresh();
        });
        RectTransform studioDoll = (RectTransform)_studioRoom.transform.Find("DollDisplay/DollImage");
        _studioZero = ZeroPresence.Create(_studioRoom.transform, "StudioZero", () => studioDoll, new Vector2(-330f, 250f));

        _atticRoom = CreateRoom("AtticRoom", AtticBackgroundID, new Vector2(560f, -150f), new Vector2(460f, 690f));
        Button bed = CreateTag(_atticRoom.transform, "Bed_Tag", BedLabel, string.Empty, new Vector2(-60f, -10f), RequestRest);
        _bedHint = bed.transform.Find("Hint_Text").GetComponent<Text>();
        _atticTags.Add(bed);
        _atticTags.Add(CreateTag(_atticRoom.transform, "Ladder_Tag", "下楼 · 大厅", "回到店面", new Vector2(-420f, -330f), () => Show(HomeRoom.Hall)));
        _atticTags.Add(CreateTag(_atticRoom.transform, "Shelf_Tag", ShelfLabel, "她留着的东西", new Vector2(-360f, 140f), () => _shelf.Show()));
        RectTransform atticDoll = (RectTransform)_atticRoom.transform.Find("DollDisplay/DollImage");
        _atticZero = ZeroPresence.Create(_atticRoom.transform, "AtticZero", () => atticDoll, new Vector2(520f, 270f));
        _atticTags.Add(CreateTag(_atticRoom.transform, "AtticTalk_Tag", TalkLabel, "摸摸头、牵牵手也行", new Vector2(200f, -330f), () => Talk(HomeRoom.Attic)));

        BuildTabs();
        _stamps = HomeEveningStamps.Create(transform, new Vector2(650f, -88f), GoToStamp);
        _nightTalk = HomeNightTalkView.Create(transform);
        _shelf = HomeShelfView.Create(transform, transform, () => _workshop?.RefreshUI());
        _unpack = HomeUnpackView.Create(transform, () => {
            Refresh();
            _workshop?.RefreshUI();
        });

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
        _quiet = true;
        Show(HomeRoom.Attic);
        _quiet = false;
        _nightTalk.Show(report, () => {
            Show(HomeRoom.Hall);
            Refresh();
            if (report.WishExpired != null) {
                PlayerToast.Show("零号：「算了，下次吧。」", PlayerToastKind.Info);
            }

            if (report.NewWish != null) {
                PlayerToast.Show($"零号有一个小心愿：{report.NewWish.Text}。", PlayerToastKind.Info);
            }

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

        Image roomLight = UITheme.CreatePanel(root.transform, "RoomLight_Image", Color.clear);
        UITheme.Stretch(roomLight.rectTransform);
        roomLight.raycastTarget = false;
        _roomLights.Add(roomLight);
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

    // The counter: at night, what the dive brought home is unpacked first (卸货), then the business.
    public void OpenCounter() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        bool night = player?.IsNight ?? false;
        Show(HomeRoom.Hall);
        if (night && HomeEveningService.Haul(player).Count > 0) {
            _unpack.Show(() => _workshop?.DayHud?.OpenBusiness(true));
            return;
        }

        _workshop?.DayHud?.OpenBusiness(night);
    }

    // 和她说话, and a touch on her head, shoulder or hand (ZeroInteractionService).
    public void Talk(HomeRoom room) {
        Zero(room).Show(ZeroInteractionService.Chat(GameRoot.Core?.CurrentPlayer, room));
        Refresh();
        _workshop?.DayHud?.Refresh();
    }

    public void Touch(HomeRoom room, TouchRegion region) {
        // The hall's touch areas live on the workshop portrait, which the prologue also shows without the rooms.
        if (!isActiveAndEnabled || _nightTalk.IsShowing || (_journal != null && _journal.IsShowing)) {
            return;
        }

        Zero(room).Show(ZeroInteractionService.Touch(GameRoot.Core?.CurrentPlayer, region));
        Refresh();
    }

    private void GoToStamp(EveningStamp stamp) {
        switch (stamp) {
            case EveningStamp.Unpack:
            case EveningStamp.Business:
                OpenCounter();
                break;
            case EveningStamp.Care:
                ShowStudio(StudioTab.Maintenance);
                break;
            default:
                PlayerToast.Show(_nightTalk.IsShowing ? "和她说完话，就说晚安吧。" : "开张营业之后，上阁楼和她说晚安。", PlayerToastKind.Info);
                break;
        }
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
