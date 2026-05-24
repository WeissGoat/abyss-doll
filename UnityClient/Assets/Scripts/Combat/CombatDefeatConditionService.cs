public class CombatDefeatEvaluation {
    public bool IsDefeated;
    public bool ActiveDollHpDepleted;
    public bool PlayerFactionWiped;
    public bool SanCollapsed;
    public CombatDefeatReasonType Reason = CombatDefeatReasonType.None;
}

public static class CombatDefeatConditionService {
    public static CombatDefeatEvaluation Evaluate(CombatSystem combat) {
        CombatDefeatEvaluation evaluation = new CombatDefeatEvaluation();
        DollEntity activeDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        FighterEntity activeDollFighter = FindActiveDollFighter(combat, activeDoll);

        evaluation.ActiveDollHpDepleted = activeDollFighter != null && activeDollFighter.RuntimeHP <= 0;
        evaluation.PlayerFactionWiped = combat?.PlayerFaction != null
            && combat.PlayerFaction.Fighters.Count > 0
            && combat.PlayerFaction.IsWipedOut();
        evaluation.SanCollapsed = activeDoll?.Status != null
            && activeDoll.Status.SAN_Max > 0
            && activeDoll.Status.SAN_Current <= 0;
        evaluation.IsDefeated = evaluation.ActiveDollHpDepleted
            || evaluation.PlayerFactionWiped
            || evaluation.SanCollapsed;
        evaluation.Reason = ResolveReason(evaluation);
        return evaluation;
    }

    private static FighterEntity FindActiveDollFighter(CombatSystem combat, DollEntity activeDoll) {
        if (combat?.PlayerFaction?.Fighters == null || activeDoll == null) {
            return null;
        }

        foreach (FighterEntity fighter in combat.PlayerFaction.Fighters) {
            DollFighter dollFighter = fighter as DollFighter;
            if (dollFighter != null && ReferenceEquals(dollFighter.DataRef, activeDoll)) {
                return dollFighter;
            }
        }

        return combat.PlayerFaction.Fighters.Count > 0 ? combat.PlayerFaction.Fighters[0] : null;
    }

    private static CombatDefeatReasonType ResolveReason(CombatDefeatEvaluation evaluation) {
        if (evaluation == null || !evaluation.IsDefeated) {
            return CombatDefeatReasonType.None;
        }

        if ((evaluation.ActiveDollHpDepleted || evaluation.PlayerFactionWiped) && evaluation.SanCollapsed) {
            return CombatDefeatReasonType.PlayerHpAndSanDepleted;
        }

        if (evaluation.ActiveDollHpDepleted) {
            return CombatDefeatReasonType.PlayerHpDepleted;
        }

        if (evaluation.SanCollapsed) {
            return CombatDefeatReasonType.PlayerSanCollapsed;
        }

        if (evaluation.PlayerFactionWiped) {
            return CombatDefeatReasonType.PlayerFactionWiped;
        }

        return CombatDefeatReasonType.Unknown;
    }
}
