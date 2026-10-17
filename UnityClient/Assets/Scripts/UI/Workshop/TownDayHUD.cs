using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// The workshop's day layer (GDD_07): a status bar (day, week, Zero, money, rent countdown), today's core choice
// (跳下深渊 / 停工休息) and the night dialogs (一天结束, 交租, 破产). Rules live in DayCycleService and
// TownEconomyService; this only shows state and forwards clicks. Hidden while the prologue's half-open workshop runs.
public class TownDayHUD : MonoBehaviour {
    public const string RootName = "TownDayHUD_Runtime";
    public const string DiveLabel = "跳下深渊";
    public const string RestLabel = "停工休息";
    public const string GoBusinessLabel = "去营业";

    private WorkshopUIController _workshop;
    private GameObject _content;
    private Text _dayText;
    private Text _dollText;
    private Button _maintenanceButton;
    private NightBusinessPanel _businessPanel;
    private Text _moneyText;
    private Button _rentButton;
    private GameObject _choicePanel;
    private bool _choiceVisible = true;
    private Text _choiceTitle;
    private Button _diveButton;
    private Button _restButton;
    private Text _diveHint;
    private Text _restHint;

    private GameObject _restConfirm;
    private Text _restConfirmText;
    private GameObject _nightCard;
    private Text _nightTitle;
    private Text _nightBody;
    private GameObject _rentPanel;
    private Text _rentTitle;
    private Text _rentBody;
    private Text _rentSelection;
    private Transform _pawnList;
    private Button _payButton;
    private Button _pawnPayButton;
    private Button _oweButton;
    private GameObject _bankruptPanel;
    private readonly HashSet<ItemEntity> _selectedPawn = new HashSet<ItemEntity>();
    private readonly List<GameObject> _pawnRows = new List<GameObject>();

    public static TownDayHUD Ensure(Transform workshopRoot, WorkshopUIController workshop) {
        Transform existing = workshopRoot.Find(RootName);
        TownDayHUD hud = existing != null ? existing.GetComponent<TownDayHUD>() : null;
        if (hud == null) {
            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(workshopRoot, false);
            UITheme.Stretch((RectTransform)root.transform);
            hud = root.AddComponent<TownDayHUD>();
            hud.Build();
        }

        hud._workshop = workshop;
        return hud;
    }

    public bool IsAnyDialogOpen =>
        (_restConfirm != null && _restConfirm.activeSelf)
        || (_nightCard != null && _nightCard.activeSelf)
        || (_rentPanel != null && _rentPanel.activeSelf)
        || (_bankruptPanel != null && _bankruptPanel.activeSelf);

    public void Refresh() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            return;
        }

        transform.SetAsLastSibling();
        // Town state shown here is what the save holds; AutoSave writes only when it changed.
        SaveService.AutoSave(player);
        DayCalendarInfo calendar = DayCycleService.GetCalendar(player);
        DollEntity doll = player.ActiveDoll;
        _dayText.text = $"第 {calendar.Day} 天 · 第 {calendar.Week} 周 {calendar.WeekDayName}";
        _dollText.text = doll == null ? string.Empty : $"零号  HP {doll.Status.HP_Current}/{doll.Status.HP_Max}  SAN {doll.Status.SAN_Current}/{doll.Status.SAN_Max}  {MaintenancePanel.DescribeStrain(doll)}";
        bool hurt = doll != null && doll.Status.HP_Current * 10 < doll.Status.HP_Max * 3;
        _dollText.color = hurt ? UITheme.Danger : doll != null ? MaintenancePanel.StrainColor(Mathf.Max(doll.Status.WearAndTear, doll.Status.Corruption)) : UITheme.TextPrimary;
        _moneyText.text = $"资金 {player.Money}G";

        string rentLabel;
        Color rentColor;
        if (calendar.HasPendingRent) {
            rentLabel = $"房租 {calendar.PendingRentAmount}G 待处理（点这里）";
            rentColor = UITheme.Danger;
        } else {
            string debt = calendar.DebtAmount > 0 ? $" · 欠款 {calendar.DebtAmount}G" : string.Empty;
            rentLabel = calendar.RentCountdown <= 1
                ? $"今晚交房租 {calendar.NextRent}G{debt}"
                : $"房租 {calendar.NextRent}G · 还有 {calendar.RentCountdown} 天{debt}";
            rentColor = calendar.RentCountdown <= 3 ? UITheme.Warning : UITheme.TextSecondary;
        }
        UITheme.SetButtonLabel(_rentButton, rentLabel);
        _rentButton.GetComponentInChildren<Text>().color = rentColor;
        _rentButton.interactable = calendar.HasPendingRent;

        // At night the choice panel waits for the player to open the business; until then they can maintain,
        // arrange the backpack or look around the workshop.
        bool night = player.IsNight;
        _restButton.gameObject.SetActive(!night);
        _restHint.gameObject.SetActive(!night);
        ((RectTransform)_diveButton.transform).anchoredPosition = new Vector2(night ? 0f : -180f, -32f);
        _diveHint.rectTransform.anchoredPosition = new Vector2(night ? 0f : -180f, -100f);
        if (night) {
            _choiceTitle.text = "夜晚了：先维护、整理背包，准备好再开张";
            UITheme.SetButtonLabel(_diveButton, GoBusinessLabel);
            _diveButton.interactable = true;
            _diveHint.text = "摆橱窗、倾倒战利品，开张后这一天结束";
            _diveHint.color = UITheme.TextSecondary;
            return;
        }

        _choiceTitle.text = "今天做什么？";
        UITheme.SetButtonLabel(_diveButton, DiveLabel);
        bool canDive = DayCycleService.CanDive(player, out string diveReason);
        bool canRest = DayCycleService.CanStartDay(player, out string restReason);
        _diveButton.interactable = canDive;
        _restButton.interactable = canRest;
        _diveHint.text = canDive ? "选一层下潜，今天就过去了" : diveReason;
        _diveHint.color = canDive ? UITheme.TextSecondary : UITheme.Warning;
        _restHint.text = canRest ? "恢复 HP、SAN，减轻磨损和侵蚀；今天没有收获" : restReason;
        _restHint.color = canRest ? UITheme.TextSecondary : UITheme.Warning;
    }

    // Night card first, then the rent decision, then bankruptcy; each shows after the previous one closes.
    public void ShowPendingDayEvents() {
        Refresh();
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player != null && player.IsNight) {
            // The player opens the business from the choice panel when ready (去营业).
            return;
        }

        DayEndReport report = DayCycleService.TakePendingReport();
        if (report != null) {
            // The day ends upstairs with Zero (HomeRoomNavigator); the card is the fallback without rooms.
            HomeRoomNavigator rooms = _workshop?.Rooms;
            if (rooms != null) {
                rooms.ShowNightTalk(report, ShowFollowUps);
            } else {
                ShowNightCard(report);
            }
            return;
        }

        ShowFollowUps();
    }

    private void ShowFollowUps() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        Refresh();
        if (player == null) {
            return;
        }

        if (TownEconomyService.IsBankrupt(player)) {
            _bankruptPanel.SetActive(true);
            _bankruptPanel.transform.SetAsLastSibling();
            return;
        }

        if (player.HasPendingMonthlyRent) {
            OpenRentPanel();
        }
    }

    private void HandleDiveClicked() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player != null && player.IsNight) {
            OpenBusiness(true);
            return;
        }

        if (!DayCycleService.CanDive(player, out string reason)) {
            PlayerToast.Show(reason, PlayerToastKind.Warning);
            Refresh();
            return;
        }

        _workshop?.OpenDungeonStartLayerPanel();
    }

    // Today's choice belongs to the hall; the studio and the attic hide it (HomeRoomNavigator).
    public void SetChoiceVisible(bool visible) {
        _choiceVisible = visible;
        if (_choicePanel != null && _choicePanel.activeSelf != visible) {
            _choicePanel.SetActive(visible);
        }
    }

    // 停工休息 from the hall, or the bed in the attic.
    public void RequestRest() {
        HandleRestClicked();
    }

    private void HandleRestClicked() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (!DayCycleService.CanStartDay(player, out string reason)) {
            PlayerToast.Show(reason, PlayerToastKind.Warning);
            Refresh();
            return;
        }

        EconomyConfig config = TownEconomyService.GetConfig();
        DollEntity doll = player.ActiveDoll;
        DayCalendarInfo calendar = DayCycleService.GetCalendar(player);
        StringBuilder body = new StringBuilder("今天不下深渊，让零号好好休息。\n");
        if (doll != null) {
            int hpAfter = Mathf.Min(doll.Status.HP_Max, doll.Status.HP_Current + Mathf.CeilToInt(doll.Status.HP_Max * Mathf.Clamp01(config?.RestHPRecoverRatio ?? 1f)));
            int sanAfter = Mathf.Min(doll.Status.SAN_Max, doll.Status.SAN_Current + Mathf.CeilToInt(doll.Status.SAN_Max * Mathf.Clamp01(config?.RestSANRecoverRatio ?? 1f)));
            body.Append($"HP {doll.Status.HP_Current} → {hpAfter}，SAN {doll.Status.SAN_Current} → {sanAfter}。\n");
        }
        body.Append(calendar.RentCountdown <= 1
            ? $"今晚要交房租 {calendar.NextRent}G。"
            : $"房租 {calendar.NextRent}G，还有 {calendar.RentCountdown} 天。");
        _restConfirmText.text = body.ToString();
        _restConfirm.SetActive(true);
        _restConfirm.transform.SetAsLastSibling();
    }

    private void ConfirmRest() {
        _restConfirm.SetActive(false);
        DayEndReport report = DayCycleService.Rest(GameRoot.Core?.CurrentPlayer, out string reason);
        if (report == null) {
            PlayerToast.Show(reason, PlayerToastKind.Warning);
            Refresh();
            return;
        }

        _workshop?.RefreshUI();
        ShowPendingDayEvents();
    }

    private void ShowNightCard(DayEndReport report) {
        _nightTitle.text = $"第 {report.EndedDay} 天结束";
        _nightBody.text = BuildNightText(report);
        _nightCard.SetActive(true);
        _nightCard.transform.SetAsLastSibling();
    }

    public static string BuildNightText(DayEndReport report) {
        StringBuilder text = new StringBuilder();
        if (report.Choice == DayChoice.Rest) {
            text.AppendLine("停工休息了一天。");
        } else if (report.DiveDefeated) {
            text.AppendLine($"下潜第 {report.DiveLayerID} 层：战败，零号被拖回了工坊。");
        } else {
            text.AppendLine($"下潜第 {report.DiveLayerID} 层：平安撤离。");
        }

        text.AppendLine($"零号  HP {report.HPBefore} → {report.HPAfter} / {report.HPMax}    SAN {report.SANBefore} → {report.SANAfter} / {report.SANMax}");
        text.AppendLine($"磨损 {report.WearBefore:0} → {report.WearAfter:0}    侵蚀 {report.CorruptionBefore:0} → {report.CorruptionAfter:0}");

        BusinessReport business = report.Business;
        if (business != null && business.Lines.Count > 0) {
            text.AppendLine($"今晚营业：卖出 {business.Lines.Count} 件，倾倒箱 {business.DumpBoxIncome}G，橱窗 {business.ShowcaseIncome}G，共 {business.Total}G。");
        } else if (business != null) {
            text.AppendLine("今晚没有摆货，店里冷冷清清：0G。");
        }

        MonthlyRentSettlementReport rent = report.Rent;
        if (rent != null) {
            switch (rent.Status) {
                case MonthlyRentSettlementStatus.Paid:
                    text.AppendLine($"交了第 {rent.Month} 周的房租 {rent.TotalBill}G，剩 {rent.EndingMoney}G。");
                    break;
                case MonthlyRentSettlementStatus.LightDebtAccepted:
                    text.AppendLine($"房租差 {rent.Shortfall}G，先记为欠款，下次交租时加利息。");
                    break;
                case MonthlyRentSettlementStatus.HeavyDefault:
                    text.AppendLine($"房租交不起，欠下 {rent.NewDebtAmount}G：严重违约。");
                    break;
                case MonthlyRentSettlementStatus.RequiresPawn:
                    text.AppendLine($"房租 {rent.TotalBill}G 交不起，需要你决定怎么办。");
                    break;
            }
        } else if (report.Economy != null) {
            text.AppendLine(report.Economy.RentCountdown <= 1
                ? $"明晚交房租 {report.Economy.EstimatedMonthlyBill}G。"
                : $"离交房租还有 {report.Economy.RentCountdown} 天（{report.Economy.EstimatedMonthlyBill}G）。");
        }

        if (report.HPAfter <= 0) {
            text.AppendLine("零号伤得站不起来，下次下潜前得先休息。");
        }

        if (report.Bankrupt) {
            text.AppendLine("连续严重违约，债主要收走工坊了。");
        }

        if (report.NewDay > 0) {
            text.AppendLine();
            text.Append($"明天：第 {report.NewDay} 天 · {DayCycleService.GetWeekDayName(report.NewDay)}");
        }

        return text.ToString().TrimEnd();
    }

    private void CloseNightCard() {
        _nightCard.SetActive(false);
        ShowFollowUps();
    }

    private void OpenRentPanel() {
        _selectedPawn.Clear();
        RefreshRentPanel();
        _rentPanel.SetActive(true);
        _rentPanel.transform.SetAsLastSibling();
    }

    private void RefreshRentPanel() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            return;
        }

        EconomyConfig config = TownEconomyService.GetConfig();
        int bill = player.PendingMonthlyBillAmount;
        int shortfall = Mathf.Max(0, bill - player.Money);
        int dueDay = player.PendingMonthlyRentDay > 0 ? player.PendingMonthlyRentDay : Mathf.Max(1, player.CurrentDay - 1);
        _rentTitle.text = $"第 {(dueDay - 1) / TownEconomyService.GetRentPeriodLength(config) + 1} 周的房租";
        _rentBody.text = shortfall > 0
            ? $"账单 {bill}G，资金 {player.Money}G，还差 {shortfall}G。\n可以典当仓库或背包里的东西（按 {Mathf.RoundToInt((config?.PawnValueMultiplier ?? 0.25f) * 100)}% 折价），或者先欠着。\n欠下账单一半以上算严重违约；连续 {config?.BankruptcyHeavyDefaults ?? 2} 次严重违约，工坊破产。"
            : $"账单 {bill}G，资金 {player.Money}G，够交了。";

        foreach (GameObject row in _pawnRows) {
            if (row != null) {
                row.SetActive(false);
                Destroy(row);
            }
        }
        _pawnRows.Clear();

        List<PawnCandidate> candidates = TownEconomyService.BuildPawnCandidates(player);
        _selectedPawn.RemoveWhere(item => candidates.Find(candidate => candidate.Item == item) == null);
        int selectedValue = 0;
        for (int i = 0; i < candidates.Count && i < 8; i++) {
            PawnCandidate candidate = candidates[i];
            bool selected = _selectedPawn.Contains(candidate.Item);
            if (selected) {
                selectedValue += candidate.PawnValue;
            }

            Button row = UITheme.CreateButton(_pawnList, "Pawn_" + i, $"{(selected ? "已选 · " : string.Empty)}{candidate.Name}    +{candidate.PawnValue}G", selected ? UITheme.PanelRaised : UITheme.Inset, selected ? UITheme.Accent : UITheme.TextPrimary, UITheme.CaptionSize);
            row.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
            row.GetComponentInChildren<Text>().rectTransform.offsetMin = new Vector2(16f, 0f);
            UITheme.Place((RectTransform)row.transform, new Vector2(0.5f, 1f), new Vector2(0f, -i * 44f), new Vector2(620f, 40f));
            ItemEntity item = candidate.Item;
            row.onClick.AddListener(() => {
                if (!_selectedPawn.Remove(item)) {
                    _selectedPawn.Add(item);
                }
                RefreshRentPanel();
            });
            _pawnRows.Add(row.gameObject);
        }

        if (candidates.Count == 0 && shortfall > 0) {
            Text none = UITheme.CreateText(_pawnList, "NoPawn_Text", "没有能典当的东西（绑定的装备和重要物品不能典当）。", UITheme.CaptionSize, UITheme.TextMuted, TextAnchor.MiddleCenter);
            UITheme.Place(none.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(620f, 40f));
            _pawnRows.Add(none.gameObject);
        }

        _rentSelection.text = selectedValue > 0 ? $"已选典当 +{selectedValue}G，合计 {player.Money + selectedValue}G" : string.Empty;
        _payButton.gameObject.SetActive(shortfall == 0);
        _pawnPayButton.gameObject.SetActive(shortfall > 0);
        _pawnPayButton.interactable = selectedValue > 0 && player.Money + selectedValue >= bill;
        _oweButton.gameObject.SetActive(shortfall > 0);
        UITheme.SetButtonLabel(_oweButton, $"先欠着（欠 {shortfall}G）");
    }

    private void PayRent(bool withPawn) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        MonthlyRentSettlementReport report = TownEconomyService.ResolveMonthlyRent(player, withPawn ? new List<ItemEntity>(_selectedPawn) : null);
        if (report.Status == MonthlyRentSettlementStatus.Paid) {
            PlayerToast.Show(report.PawnIncome > 0 ? $"典当得 {report.PawnIncome}G，交了房租 {report.TotalBill}G。" : $"交了房租 {report.TotalBill}G。", PlayerToastKind.Success);
            CloseRentPanelAfterDecision();
            return;
        }

        PlayerToast.Show("还是不够交房租。", PlayerToastKind.Warning);
        RefreshRentPanel();
    }

    private void OweRent() {
        MonthlyRentSettlementReport report = TownEconomyService.AcceptPendingRentAsDebt(GameRoot.Core?.CurrentPlayer);
        string message = report.Status == MonthlyRentSettlementStatus.HeavyDefault
            ? $"欠下 {report.NewDebtAmount}G：严重违约。"
            : $"欠下 {report.NewDebtAmount}G，下次交租时加利息。";
        PlayerToast.Show(message, report.Status == MonthlyRentSettlementStatus.HeavyDefault ? PlayerToastKind.Warning : PlayerToastKind.Info);
        CloseRentPanelAfterDecision();
    }

    private void CloseRentPanelAfterDecision() {
        _rentPanel.SetActive(false);
        _workshop?.RefreshUI();
        ShowFollowUps();
    }

    // night: today's business (开始营业 ends the day). Otherwise the daytime 营业准备 view of the same screen.
    public void OpenBusiness(bool night) {
        if (_businessPanel == null) {
            _businessPanel = NightBusinessPanel.Create(transform);
        }

        _businessPanel.Show(night, () => {
            DayEndReport report = DayCycleService.FinishNight(GameRoot.Core?.CurrentPlayer);
            if ((report?.Business?.DumpBoxIncome ?? 0) + (report?.Business?.ShowcaseIncome ?? 0) > 0) {
                GameAudio.PlaySfx("coin");
            }

            _workshop?.RefreshUI();
            ShowPendingDayEvents();
        }, () => {
            _workshop?.RefreshUI();
            Refresh();
        });
    }

    // Maintenance is done at the studio's workbench.
    public void OpenMaintenance() {
        _workshop?.Rooms?.ShowStudio(StudioTab.Maintenance);
    }

    private void RestartAfterBankruptcy() {
        SaveService.Delete();
        GameRoot.RestartGame();
    }

    // The bar and the choice step aside while a workshop panel (ledger, studio, layer select, formal panels) is open.
    private void LateUpdate() {
        bool overlay = _workshop != null && _workshop.IsOverlayPanelOpen;
        if (_content.activeSelf == overlay) {
            _content.SetActive(!overlay);
        }
    }

    private void Build() {
        _content = new GameObject("Content", typeof(RectTransform));
        _content.transform.SetParent(transform, false);
        UITheme.Stretch((RectTransform)_content.transform);
        BuildStatusBar();
        BuildChoicePanel();
        BuildRestConfirm();
        BuildNightCard();
        BuildRentPanel();
        BuildBankruptPanel();
    }

    private void BuildStatusBar() {
        Image bar = UITheme.CreatePanel(_content.transform, "DayStatus_Bar", UITheme.Panel);
        UITheme.AddBorder(bar, UITheme.Border, 1.5f);
        UITheme.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(1560f, 60f));
        bar.raycastTarget = false;

        _dayText = UITheme.CreateText(bar.transform, "Day_Text", string.Empty, UITheme.BodySize + 2, UITheme.Accent);
        UITheme.Place(_dayText.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(330f, 48f));
        _dollText = UITheme.CreateText(bar.transform, "Doll_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(_dollText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-170f, 0f), new Vector2(500f, 48f));
        _maintenanceButton = UITheme.CreateButton(bar.transform, "Maintenance_Button", "维护", UITheme.PanelRaised, UITheme.Accent, UITheme.CaptionSize);
        UITheme.AddBorder(_maintenanceButton.targetGraphic, UITheme.Accent, 1f);
        UITheme.Place((RectTransform)_maintenanceButton.transform, new Vector2(0.5f, 0.5f), new Vector2(135f, 0f), new Vector2(90f, 38f));
        _maintenanceButton.onClick.AddListener(OpenMaintenance);
        _moneyText = UITheme.CreateText(bar.transform, "Money_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(_moneyText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(270f, 0f), new Vector2(150f, 48f));

        _rentButton = UITheme.CreateButton(bar.transform, "Rent_Button", string.Empty, new Color(0f, 0f, 0f, 0f), UITheme.TextSecondary);
        _rentButton.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleRight;
        UITheme.Place((RectTransform)_rentButton.transform, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(400f, 48f));
        _rentButton.onClick.AddListener(OpenRentPanel);
    }

    private void BuildChoicePanel() {
        Image panel = UITheme.CreatePanel(_content.transform, "DayChoice_Panel", UITheme.Panel);
        _choicePanel = panel.gameObject;
        _choicePanel.SetActive(_choiceVisible);
        UITheme.AddBorder(panel, UITheme.Border, 1.5f);
        UITheme.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(780f, 176f));

        _choiceTitle = UITheme.CreateText(panel.transform, "Choice_Title", "今天做什么？", UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleCenter);
        UITheme.Place(_choiceTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(600f, 26f));

        _diveButton = UITheme.CreateButton(panel.transform, "Dive_Button", DiveLabel, UITheme.Accent, UITheme.OnAccent, UITheme.HeadingSize);
        UITheme.Place((RectTransform)_diveButton.transform, new Vector2(0.5f, 1f), new Vector2(-180f, -38f), new Vector2(330f, 64f));
        _diveButton.onClick.AddListener(HandleDiveClicked);
        _diveHint = UITheme.CreateText(panel.transform, "Dive_Hint", string.Empty, UITheme.CaptionSize - 2, UITheme.TextSecondary, TextAnchor.UpperCenter);
        UITheme.Place(_diveHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(-180f, -108f), new Vector2(340f, 48f));

        _restButton = UITheme.CreateButton(panel.transform, "Rest_Button", RestLabel, UITheme.PanelRaised, UITheme.TextPrimary, UITheme.HeadingSize);
        UITheme.AddBorder(_restButton.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)_restButton.transform, new Vector2(0.5f, 1f), new Vector2(180f, -38f), new Vector2(330f, 64f));
        _restButton.onClick.AddListener(HandleRestClicked);
        _restHint = UITheme.CreateText(panel.transform, "Rest_Hint", string.Empty, UITheme.CaptionSize - 2, UITheme.TextSecondary, TextAnchor.UpperCenter);
        UITheme.Place(_restHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(180f, -108f), new Vector2(340f, 48f));
    }

    private void BuildRestConfirm() {
        _restConfirm = UITheme.CreateModal(transform, "RestConfirm_Modal", new Vector2(640f, 300f));
        Transform dialog = _restConfirm.transform.Find("Dialog");
        Text title = UITheme.CreateText(dialog, "Title_Text", RestLabel, UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(560f, 44f));
        _restConfirmText = UITheme.CreateText(dialog, "Body_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(_restConfirmText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(580f, 120f));
        Button ok = UITheme.CreateButton(dialog, "RestConfirm_Button", "休息一天", UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(-120f, 24f), new Vector2(200f, 58f));
        ok.onClick.AddListener(ConfirmRest);
        Button cancel = UITheme.CreateButton(dialog, "RestCancel_Button", "算了", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(120f, 24f), new Vector2(200f, 58f));
        cancel.onClick.AddListener(() => _restConfirm.SetActive(false));
        _restConfirm.SetActive(false);
    }

    private void BuildNightCard() {
        _nightCard = UITheme.CreateModal(transform, "NightCard_Modal", new Vector2(760f, 380f), 560);
        Transform dialog = _nightCard.transform.Find("Dialog");
        _nightTitle = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.TitleSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_nightTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(680f, 52f));
        _nightBody = UITheme.CreateText(dialog, "Body_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.UpperLeft);
        _nightBody.lineSpacing = 1.25f;
        UITheme.Place(_nightBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(680f, 190f));
        Button next = UITheme.CreateButton(dialog, "NightNext_Button", "迎接新的一天", UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)next.transform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(260f, 60f));
        next.onClick.AddListener(CloseNightCard);
        _nightCard.SetActive(false);
    }

    private void BuildRentPanel() {
        _rentPanel = UITheme.CreateModal(transform, "Rent_Modal", new Vector2(760f, 700f), 560);
        Transform dialog = _rentPanel.transform.Find("Dialog");
        _rentTitle = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_rentTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(680f, 44f));
        _rentBody = UITheme.CreateText(dialog, "Body_Text", string.Empty, UITheme.CaptionSize + 1, UITheme.TextPrimary, TextAnchor.UpperLeft);
        UITheme.Place(_rentBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -74f), new Vector2(660f, 110f));

        GameObject list = new GameObject("PawnList", typeof(RectTransform));
        list.transform.SetParent(dialog, false);
        UITheme.Place((RectTransform)list.transform, new Vector2(0.5f, 1f), new Vector2(0f, -192f), new Vector2(620f, 352f));
        _pawnList = list.transform;

        _rentSelection = UITheme.CreateText(dialog, "Selection_Text", string.Empty, UITheme.CaptionSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_rentSelection.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(660f, 30f));

        _payButton = UITheme.CreateButton(dialog, "PayRent_Button", "交房租", UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)_payButton.transform, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(230f, 60f));
        _payButton.onClick.AddListener(() => PayRent(false));
        _pawnPayButton = UITheme.CreateButton(dialog, "PawnPay_Button", "典当选中的并交租", UITheme.Accent, UITheme.OnAccent, UITheme.CaptionSize + 1);
        UITheme.Place((RectTransform)_pawnPayButton.transform, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(230f, 60f));
        _pawnPayButton.onClick.AddListener(() => PayRent(true));
        _oweButton = UITheme.CreateButton(dialog, "Owe_Button", "先欠着", UITheme.PanelRaised, UITheme.Warning, UITheme.CaptionSize + 1);
        UITheme.AddBorder(_oweButton.targetGraphic, UITheme.Warning, 1.5f);
        UITheme.Place((RectTransform)_oweButton.transform, new Vector2(0.5f, 0f), new Vector2(130f, 28f), new Vector2(230f, 60f));
        _oweButton.onClick.AddListener(OweRent);
        _rentPanel.SetActive(false);
    }

    private void BuildBankruptPanel() {
        _bankruptPanel = UITheme.CreateModal(transform, "Bankrupt_Modal", new Vector2(700f, 320f), 580);
        Transform dialog = _bankruptPanel.transform.Find("Dialog");
        Text title = UITheme.CreateText(dialog, "Title_Text", "工坊破产了", UITheme.TitleSize, UITheme.Danger, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(620f, 52f));
        Text body = UITheme.CreateText(dialog, "Body_Text", "连续交不起房租，债主收走了工坊。\n零号和你的这段经营到此为止。", UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(620f, 100f));
        Button restart = UITheme.CreateButton(dialog, "Restart_Button", "重新开始", UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)restart.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(240f, 60f));
        restart.onClick.AddListener(RestartAfterBankruptcy);
        _bankruptPanel.SetActive(false);
    }
}
