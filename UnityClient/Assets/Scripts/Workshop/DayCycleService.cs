using UnityEngine;

public enum DayChoice {
    Dive,
    Rest
}

// What happened on a day that just ended; the workshop shows it as the night card (一天结束).
public class DayEndReport {
    public DayChoice Choice;
    public int DiveLayerID;
    public bool DiveDefeated;
    public int EndedDay;
    public int NewDay;
    public int HPBefore;
    public int HPAfter;
    public int HPMax;
    public int SANBefore;
    public int SANAfter;
    public int SANMax;
    public float WearBefore;
    public float WearAfter;
    public float CorruptionBefore;
    public float CorruptionAfter;
    public DailyEconomyReport Economy;
    public BusinessReport Business;
    public bool RentPending;
    public bool Bankrupt;
    // The dive's journal page (null on a rest day).
    public DiveLog DiveLog;
    // Zero's wish: done today (a rest day), let go this morning, or new this morning (WishService).
    public WishResult WishDone;
    public WishConfig WishExpired;
    public WishConfig NewWish;

    public MonthlyRentSettlementReport Rent => Economy?.MonthlyRentReport;
}

public struct DayCalendarInfo {
    public int Day;
    public int Week;
    public int WeekDay;
    public string WeekDayName;
    public int RentCountdown;
    public int NextRent;
    public bool HasPendingRent;
    public int PendingRentAmount;
    public int DebtAmount;
}

// GDD_07 / rule card 01: each day has one core choice, 跳下深渊 or 停工休息; everything else (selling, loadout,
// shop, upgrades) is free. Ending a day runs the night settlement (TownEconomyService.SettleDailyBusiness: rent at
// the end of each rent period) and moves the calendar on. A rent bill the player could not cover stays pending and
// must be settled (pawn or owe) before the next choice.
public static class DayCycleService {
    private static readonly string[] WeekDayNames = { "一", "二", "三", "四", "五", "六", "日" };

    private static int _diveStartHP = -1;
    private static int _diveStartSAN = -1;
    private static float _diveStartWear = -1f;
    private static float _diveStartCorruption = -1f;
    private static int _diveLayerID;

    // The latest day-end report the workshop has not shown yet.
    public static DayEndReport PendingReport { get; private set; }

    // A restarted game must not inherit the previous game's night.
    public static void ResetRuntimeState() {
        PendingReport = null;
        _nightReport = null;
    }

    public static DayEndReport TakePendingReport() {
        DayEndReport report = PendingReport;
        PendingReport = null;
        return report;
    }

    public static string GetWeekDayName(int day) {
        int weekLength = Mathf.Max(1, TownEconomyService.GetConfig()?.WeekLength ?? 7);
        int weekDay = (Mathf.Max(1, day) - 1) % weekLength + 1;
        return weekLength == WeekDayNames.Length ? "星期" + WeekDayNames[weekDay - 1] : $"第 {weekDay} 天";
    }

    public static DayCalendarInfo GetCalendar(PlayerProfile player) {
        EconomyConfig config = TownEconomyService.GetConfig();
        int day = Mathf.Max(1, player?.CurrentDay ?? 1);
        int weekLength = Mathf.Max(1, config?.WeekLength ?? 7);
        int weekDay = (day - 1) % weekLength + 1;
        return new DayCalendarInfo {
            Day = day,
            Week = (day - 1) / weekLength + 1,
            WeekDay = weekDay,
            WeekDayName = weekLength == WeekDayNames.Length ? "星期" + WeekDayNames[weekDay - 1] : $"第 {weekDay} 天",
            RentCountdown = TownEconomyService.CalculateRentCountdown(player, config),
            NextRent = TownEconomyService.CalculateMonthlyBill(player, config),
            HasPendingRent = player != null && player.HasPendingMonthlyRent,
            PendingRentAmount = player?.PendingMonthlyBillAmount ?? 0,
            DebtAmount = player?.EconomyDebtAmount ?? 0
        };
    }

    // Whether the player may make today's choice at all.
    public static bool CanStartDay(PlayerProfile player, out string reason) {
        if (player == null) {
            reason = "没有玩家数据。";
            return false;
        }

        if (TownEconomyService.IsBankrupt(player)) {
            reason = "工坊已经破产。";
            return false;
        }

        if (player.IsNight) {
            reason = "今晚还没营业：先开张，明天再做决定。";
            return false;
        }

        if (player.HasPendingMonthlyRent) {
            reason = $"房租 {player.PendingMonthlyBillAmount}G 还没处理：先交租、典当或记账。";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static bool CanDive(PlayerProfile player, out string reason) {
        if (!CanStartDay(player, out reason)) {
            return false;
        }

        DollEntity doll = player.ActiveDoll;
        if (doll != null && doll.Status.HP_Current <= 0) {
            reason = "零号还站不起来（HP 0）：先停工休息一天。";
            return false;
        }

        return true;
    }

    // 停工休息: the day passes without a dive; Zero recovers RestHPRecoverRatio / RestSANRecoverRatio of max HP / SAN.
    public static DayEndReport Rest(PlayerProfile player, out string reason) {
        if (!CanStartDay(player, out reason)) {
            return null;
        }

        EconomyConfig config = TownEconomyService.GetConfig();
        DollEntity doll = player.ActiveDoll;
        DayEndReport report = new DayEndReport { Choice = DayChoice.Rest };
        CaptureBefore(report, doll, doll?.Status.HP_Current ?? 0, doll?.Status.SAN_Current ?? 0);

        if (doll != null) {
            float hpRatio = Mathf.Clamp01(config?.RestHPRecoverRatio ?? 1f);
            float sanRatio = Mathf.Clamp01((config?.RestSANRecoverRatio ?? 1f) + HomeUpgradeService.Value(player, "RestSANBonus"));
            doll.Status.HP_Current = Mathf.Min(doll.Status.HP_Max, doll.Status.HP_Current + Mathf.CeilToInt(doll.Status.HP_Max * hpRatio));
            doll.Status.SAN_Current = Mathf.Min(doll.Status.SAN_Max, doll.Status.SAN_Current + Mathf.CeilToInt(doll.Status.SAN_Max * sanRatio));
            DollStrainService.ApplyRest(doll, config);
            GameEventBus.PublishHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
            GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
        }

        report.WishDone = WishService.OnRest(player);
        FinishDay(player, report);
        Debug.Log($"[DayCycle] Rest on day {report.EndedDay}: HP {report.HPBefore}->{report.HPAfter}, SAN {report.SANBefore}->{report.SANAfter}.");
        return report;
    }

    // Called when a run starts, so the night card can show how the dive changed Zero.
    public static void BeginDiveDay(PlayerProfile player, int layerID) {
        DollEntity doll = player?.ActiveDoll;
        _diveStartHP = doll?.Status.HP_Current ?? -1;
        _diveStartSAN = doll?.Status.SAN_Current ?? -1;
        _diveStartWear = doll?.Status.WearAndTear ?? -1f;
        _diveStartCorruption = doll?.Status.Corruption ?? -1f;
        _diveLayerID = layerID;
    }

    // A dive takes the whole day (rule card 01 §3). HP and SAN come back as they are (GDD_02 §5.1); recovery is rest
    // or maintenance, not a free refill.
    public static DayEndReport EndDiveDay(PlayerProfile player, bool defeated) {
        if (player == null) {
            return null;
        }

        DollEntity doll = player.ActiveDoll;
        DayEndReport report = new DayEndReport {
            Choice = DayChoice.Dive,
            DiveDefeated = defeated,
            DiveLayerID = _diveLayerID,
            DiveLog = DiveLogService.LastFinished
        };
        CaptureBefore(
            report,
            doll,
            _diveStartHP >= 0 ? _diveStartHP : doll?.Status.HP_Current ?? 0,
            _diveStartSAN >= 0 ? _diveStartSAN : doll?.Status.SAN_Current ?? 0);
        if (_diveStartWear >= 0f) {
            report.WearBefore = _diveStartWear;
            report.CorruptionBefore = _diveStartCorruption;
        }
        _diveStartHP = -1;
        _diveStartSAN = -1;
        _diveStartWear = -1f;
        _diveStartCorruption = -1f;

        if (defeated) {
            DollStrainService.ApplyDefeat(doll);
        }

        FinishDay(player, report);
        return report;
    }

    private static void CaptureBefore(DayEndReport report, DollEntity doll, int hp, int san) {
        report.HPBefore = hp;
        report.SANBefore = san;
        report.HPMax = doll?.Status.HP_Max ?? 0;
        report.SANMax = doll?.Status.SAN_Max ?? 0;
        report.WearBefore = doll?.Status.WearAndTear ?? 0f;
        report.CorruptionBefore = doll?.Status.Corruption ?? 0f;
    }

    private static DayEndReport _nightReport;

    // The day's choice is done; the night business screen comes next (FinishNight closes it).
    private static void FinishDay(PlayerProfile player, DayEndReport report) {
        report.EndedDay = player.CurrentDay;
        player.IsNight = true;
        _nightReport = report;
    }

    // 开始营业: sells the dump box and showcase, runs the night settlement (business income first, then rent) and
    // moves the calendar to the next morning. The report goes to the night card.
    public static DayEndReport FinishNight(PlayerProfile player) {
        if (player == null || !player.IsNight) {
            return null;
        }

        DayEndReport report = _nightReport ?? new DayEndReport { Choice = DayChoice.Rest, EndedDay = player.CurrentDay };
        _nightReport = null;
        if (report.HPMax == 0) {
            CaptureBefore(report, player.ActiveDoll, player.ActiveDoll?.Status.HP_Current ?? 0, player.ActiveDoll?.Status.SAN_Current ?? 0);
        }

        report.Business = NightBusinessService.Open(player);
        player.IsNight = false;
        report.Economy = TownEconomyService.SettleDailyBusiness(player, new DailyEconomySettlementInput {
            DumpBoxIncome = report.Business.DumpBoxIncome,
            ShowcaseIncome = report.Business.ShowcaseIncome
        });
        if (report.Economy != null && !report.Economy.Success) {
            Debug.LogWarning($"[DayCycle] Night settlement did not finish: {report.Economy.Reason}");
        }

        DollEntity doll = player.ActiveDoll;
        // The gramophone: a little music upstairs every night.
        int nightSAN = Mathf.RoundToInt(HomeUpgradeService.Value(player, "NightSAN"));
        if (doll != null && nightSAN > 0) {
            doll.Status.SAN_Current = Mathf.Min(doll.Status.SAN_Max, doll.Status.SAN_Current + nightSAN);
            GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
        }

        report.HPAfter = doll?.Status.HP_Current ?? 0;
        report.SANAfter = doll?.Status.SAN_Current ?? 0;
        report.WearAfter = doll?.Status.WearAndTear ?? 0f;
        report.CorruptionAfter = doll?.Status.Corruption ?? 0f;
        report.NewDay = player.CurrentDay;
        if (report.Rent != null && report.Rent.Status == MonthlyRentSettlementStatus.Paid) {
            MementoService.Milestone(player, "first_rent");
        }

        report.NewWish = WishService.Morning(player, out WishConfig expired);
        report.WishExpired = expired;
        report.RentPending = player.HasPendingMonthlyRent;
        report.Bankrupt = TownEconomyService.IsBankrupt(player);
        PendingReport = report;
        return report;
    }
}
