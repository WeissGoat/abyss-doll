public interface IDollDynamicPresenter {
    void PlayIdle();
    void PlayLowSanIdle();
    void PlayRepairReact();
    void PlayHitReact();
    void SetExpression(string expressionID);
    void SetCoreGlow(float normalized);
    void SetSanStress(float normalized);
}

public static class DollDynamicMotionIDs {
    public const string Idle = "idle";
    public const string LowSanIdle = "low_san_idle";
    public const string RepairReact = "repair_react";
    public const string HitReact = "hit_react";
}

public static class DollDynamicExpressionIDs {
    public const string Normal = "normal";
    public const string Blink = "blink";
    public const string LowSan = "low_san";
    public const string Hurt = "hurt";
    public const string Relaxed = "relaxed";
}

public class DollDynamicPresentationIntent {
    public bool UseLowSanIdle;
    public bool TriggerRepairReact;
    public bool TriggerHitReact;
    public string ExpressionID = DollDynamicExpressionIDs.Normal;
    public float CoreGlowNormalized = 1f;
    public float SanStressNormalized;
}

public static class DollDynamicPresenterDriver {
    public static void Apply(IDollDynamicPresenter presenter, DollDynamicPresentationIntent intent) {
        if (presenter == null || intent == null) {
            return;
        }

        if (intent.TriggerHitReact) {
            presenter.PlayHitReact();
        } else if (intent.TriggerRepairReact) {
            presenter.PlayRepairReact();
        } else if (intent.UseLowSanIdle) {
            presenter.PlayLowSanIdle();
        } else {
            presenter.PlayIdle();
        }

        presenter.SetExpression(intent.ExpressionID);
        presenter.SetCoreGlow(intent.CoreGlowNormalized);
        presenter.SetSanStress(intent.SanStressNormalized);
    }
}
