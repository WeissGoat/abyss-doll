using System.Collections.Generic;

public enum CombatOutcomeType {
    None,
    Victory,
    Defeat
}

public enum CombatDefeatReasonType {
    None,
    PlayerHpDepleted,
    PlayerSanCollapsed,
    PlayerHpAndSanDepleted,
    PlayerFactionWiped,
    Unknown
}

public class CombatOutcomeReport {
    public CombatOutcomeType OutcomeType = CombatOutcomeType.None;
    public CombatDefeatReasonType DefeatReason = CombatDefeatReasonType.None;
    public string Title;
    public string Summary;
    public string ActiveDollName;
    public int ActiveDollHP;
    public int ActiveDollMaxHP;
    public int ActiveDollSAN;
    public int ActiveDollMaxSAN;
    public int PlayerAliveCount;
    public int EnemyAliveCount;
    public int EnemyTotalCount;
    public List<CombatOutcomeFighterSnapshot> PlayerFighters = new List<CombatOutcomeFighterSnapshot>();
    public List<CombatOutcomeFighterSnapshot> EnemyFighters = new List<CombatOutcomeFighterSnapshot>();
    public List<CombatTimelineEvent> TimelineEvents = new List<CombatTimelineEvent>();
}

public class CombatOutcomeFighterSnapshot {
    public string Name;
    public int RuntimeHP;
    public int RuntimeMaxHP;
    public int RuntimeShield;
    public int CurrentAP;
    public int MaxAP;
    public bool IsAlive;
}

public static class CombatOutcomeReportService {
    public static CombatOutcomeReport BuildVictory(CombatSystem combat) {
        CombatOutcomeReport report = BuildBaseReport(combat);
        report.OutcomeType = CombatOutcomeType.Victory;
        report.DefeatReason = CombatDefeatReasonType.None;
        report.Title = "战斗胜利";
        report.Summary = $"已击败全部敌人。本场敌人 {report.EnemyTotalCount} 个。";
        return report;
    }

    public static CombatOutcomeReport BuildDefeat(CombatSystem combat) {
        CombatOutcomeReport report = BuildBaseReport(combat);
        report.OutcomeType = CombatOutcomeType.Defeat;
        report.DefeatReason = CombatDefeatConditionService.Evaluate(combat).Reason;
        report.Title = BuildDefeatTitle(report.DefeatReason);
        report.Summary = BuildDefeatSummary(report);
        return report;
    }

    private static CombatOutcomeReport BuildBaseReport(CombatSystem combat) {
        CombatOutcomeReport report = new CombatOutcomeReport();
        DollEntity activeDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (activeDoll != null) {
            report.ActiveDollName = activeDoll.Name;
            report.ActiveDollHP = activeDoll.Status.HP_Current;
            report.ActiveDollMaxHP = activeDoll.Status.HP_Max;
            report.ActiveDollSAN = activeDoll.Status.SAN_Current;
            report.ActiveDollMaxSAN = activeDoll.Status.SAN_Max;
        }

        AppendFactionSnapshots(combat?.PlayerFaction, report.PlayerFighters, out report.PlayerAliveCount);
        AppendFactionSnapshots(combat?.EnemyFaction, report.EnemyFighters, out report.EnemyAliveCount);
        report.EnemyTotalCount = report.EnemyFighters.Count;
        if (combat?.Timeline != null) {
            report.TimelineEvents = combat.Timeline.BuildSnapshot();
        }

        CombatOutcomeFighterSnapshot activeDollSnapshot = report.PlayerFighters.Count > 0 ? report.PlayerFighters[0] : null;
        if (activeDollSnapshot != null) {
            report.ActiveDollName = activeDollSnapshot.Name;
            report.ActiveDollHP = activeDollSnapshot.RuntimeHP;
            report.ActiveDollMaxHP = activeDollSnapshot.RuntimeMaxHP;
        }

        return report;
    }

    private static void AppendFactionSnapshots(CombatFaction faction, List<CombatOutcomeFighterSnapshot> output, out int aliveCount) {
        aliveCount = 0;
        if (faction?.Fighters == null || output == null) {
            return;
        }

        foreach (FighterEntity fighter in faction.Fighters) {
            if (fighter == null) {
                continue;
            }

            bool isAlive = fighter.RuntimeHP > 0;
            if (isAlive) {
                aliveCount++;
            }

            output.Add(new CombatOutcomeFighterSnapshot {
                Name = fighter.Name,
                RuntimeHP = fighter.RuntimeHP,
                RuntimeMaxHP = fighter.RuntimeMaxHP,
                RuntimeShield = fighter.RuntimeShield,
                CurrentAP = fighter.CurrentAP,
                MaxAP = fighter.MaxAP,
                IsAlive = isAlive
            });
        }
    }

    private static string BuildDefeatTitle(CombatDefeatReasonType reason) {
        switch (reason) {
            case CombatDefeatReasonType.PlayerHpDepleted:
                return "HP 战败";
            case CombatDefeatReasonType.PlayerSanCollapsed:
                return "SAN 崩溃";
            case CombatDefeatReasonType.PlayerHpAndSanDepleted:
                return "HP 战败 + SAN 崩溃";
            case CombatDefeatReasonType.PlayerFactionWiped:
                return "队伍战败";
            default:
                return "战斗失败";
        }
    }

    private static string BuildDefeatSummary(CombatOutcomeReport report) {
        if (report == null) {
            return "战斗失败，但缺少可复盘数据。";
        }

        switch (report.DefeatReason) {
            case CombatDefeatReasonType.PlayerHpDepleted:
                return $"魔偶 HP 归零。剩余敌人 {report.EnemyAliveCount}/{report.EnemyTotalCount}。";
            case CombatDefeatReasonType.PlayerSanCollapsed:
                return $"魔偶 SAN 归零。当前 SAN {report.ActiveDollSAN}/{report.ActiveDollMaxSAN}。";
            case CombatDefeatReasonType.PlayerHpAndSanDepleted:
                return $"魔偶 HP 与 SAN 同时归零。剩余敌人 {report.EnemyAliveCount}/{report.EnemyTotalCount}。";
            case CombatDefeatReasonType.PlayerFactionWiped:
                return $"玩家阵营全部失去战斗能力。剩余敌人 {report.EnemyAliveCount}/{report.EnemyTotalCount}。";
            default:
                return $"战斗失败。剩余敌人 {report.EnemyAliveCount}/{report.EnemyTotalCount}。";
        }
    }
}
