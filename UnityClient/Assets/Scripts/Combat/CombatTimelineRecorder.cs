using System.Collections.Generic;

public enum CombatTimelineEventType {
    CombatStarted,
    TurnStarted,
    TurnEnded,
    DamageDealt,
    ItemInterference,
    Outcome
}

public class CombatTimelineEvent {
    public int Sequence;
    public CombatTimelineEventType EventType;
    public FactionType Faction;
    public string ActorName;
    public string TargetName;
    public string ActionID;
    public string Title;
    public string Detail;
    public string ItemID;
    public string ItemName;
    public int Amount;
    public int TargetHPBefore;
    public int TargetHPAfter;
    public int TargetShieldBefore;
    public int TargetShieldAfter;
    public int GridX = -1;
    public int GridY = -1;
}

public class CombatTimelineRecorder {
    private readonly List<CombatTimelineEvent> _events = new List<CombatTimelineEvent>();
    private int _nextSequence;

    public void Reset() {
        _events.Clear();
        _nextSequence = 0;
    }

    public List<CombatTimelineEvent> BuildSnapshot() {
        List<CombatTimelineEvent> snapshot = new List<CombatTimelineEvent>();
        foreach (CombatTimelineEvent entry in _events) {
            snapshot.Add(Clone(entry));
        }

        return snapshot;
    }

    public void RecordCombatStarted(int enemyCount) {
        Add(new CombatTimelineEvent {
            EventType = CombatTimelineEventType.CombatStarted,
            Faction = FactionType.Neutral,
            Title = "战斗开始",
            Detail = $"遭遇 {enemyCount} 个敌人。"
        });
    }

    public void RecordTurnStarted(FactionType faction) {
        Add(new CombatTimelineEvent {
            EventType = CombatTimelineEventType.TurnStarted,
            Faction = faction,
            Title = faction == FactionType.Player ? "玩家回合开始" : "敌方回合开始"
        });
    }

    public void RecordTurnEnded(FactionType faction) {
        Add(new CombatTimelineEvent {
            EventType = CombatTimelineEventType.TurnEnded,
            Faction = faction,
            Title = faction == FactionType.Player ? "玩家回合结束" : "敌方回合结束"
        });
    }

    public void RecordDamage(
        FactionType faction,
        string actorName,
        string targetName,
        string actionID,
        int amount,
        int targetHPBefore,
        int targetHPAfter,
        int targetShieldBefore,
        int targetShieldAfter) {
        Add(new CombatTimelineEvent {
            EventType = CombatTimelineEventType.DamageDealt,
            Faction = faction,
            ActorName = actorName,
            TargetName = targetName,
            ActionID = actionID,
            Amount = amount,
            TargetHPBefore = targetHPBefore,
            TargetHPAfter = targetHPAfter,
            TargetShieldBefore = targetShieldBefore,
            TargetShieldAfter = targetShieldAfter,
            Title = "造成伤害",
            Detail = $"{actorName} 使用 {actionID} 对 {targetName} 造成 {amount} 点伤害。"
        });
    }

    public void RecordItemInterference(
        FactionType faction,
        string actorName,
        string targetName,
        string actionID,
        string title,
        string detail,
        ItemEntity item = null,
        int gridX = -1,
        int gridY = -1) {
        Add(new CombatTimelineEvent {
            EventType = CombatTimelineEventType.ItemInterference,
            Faction = faction,
            ActorName = actorName,
            TargetName = targetName,
            ActionID = actionID,
            Title = title,
            Detail = detail,
            ItemID = item?.ConfigID,
            ItemName = item?.Name,
            GridX = gridX,
            GridY = gridY
        });
    }

    public void RecordOutcome(CombatOutcomeType outcomeType, CombatDefeatReasonType defeatReason) {
        Add(new CombatTimelineEvent {
            EventType = CombatTimelineEventType.Outcome,
            Faction = FactionType.Neutral,
            Title = outcomeType == CombatOutcomeType.Victory ? "战斗胜利" : "战斗失败",
            Detail = outcomeType == CombatOutcomeType.Victory ? "敌方全灭。" : $"失败原因：{defeatReason}。"
        });
    }

    private void Add(CombatTimelineEvent entry) {
        if (entry == null) {
            return;
        }

        entry.Sequence = _nextSequence++;
        _events.Add(entry);
    }

    private static CombatTimelineEvent Clone(CombatTimelineEvent source) {
        return new CombatTimelineEvent {
            Sequence = source.Sequence,
            EventType = source.EventType,
            Faction = source.Faction,
            ActorName = source.ActorName,
            TargetName = source.TargetName,
            ActionID = source.ActionID,
            Title = source.Title,
            Detail = source.Detail,
            ItemID = source.ItemID,
            ItemName = source.ItemName,
            Amount = source.Amount,
            TargetHPBefore = source.TargetHPBefore,
            TargetHPAfter = source.TargetHPAfter,
            TargetShieldBefore = source.TargetShieldBefore,
            TargetShieldAfter = source.TargetShieldAfter,
            GridX = source.GridX,
            GridY = source.GridY
        };
    }
}
