using UnityEngine;
using UnityEngine.UI;

public interface IDollLive2DPresenter : IDollDynamicPresenter {
}

public static class DollLive2DMotionIDs {
    public const string Idle = DollDynamicMotionIDs.Idle;
    public const string LowSanIdle = DollDynamicMotionIDs.LowSanIdle;
    public const string RepairReact = DollDynamicMotionIDs.RepairReact;
    public const string HitReact = DollDynamicMotionIDs.HitReact;
}

public static class DollLive2DExpressionIDs {
    public const string Normal = DollDynamicExpressionIDs.Normal;
    public const string Blink = DollDynamicExpressionIDs.Blink;
    public const string LowSan = DollDynamicExpressionIDs.LowSan;
    public const string Hurt = DollDynamicExpressionIDs.Hurt;
    public const string Relaxed = DollDynamicExpressionIDs.Relaxed;
}

public sealed class DollLive2DPresentationIntent : DollDynamicPresentationIntent {
}

public static class DollLive2DPresenterDriver {
    public static void Apply(IDollLive2DPresenter presenter, DollLive2DPresentationIntent intent) {
        DollDynamicPresenterDriver.Apply(presenter, intent);
    }
}

public sealed class FallbackDollLive2DPresenter : MonoBehaviour, IDollLive2DPresenter {
    [SerializeField] private Image fallbackImage;

    public DollLive2DVisualLoadResult VisualResult { get; private set; }
    public string CurrentMotionID { get; private set; } = DollLive2DMotionIDs.Idle;
    public string CurrentExpressionID { get; private set; } = DollLive2DExpressionIDs.Normal;
    public float CoreGlowNormalized { get; private set; } = 1f;
    public float SanStressNormalized { get; private set; }

    public void Initialize(DollLive2DVisualLoadResult visualResult, Image fallbackImageOverride = null) {
        VisualResult = visualResult;
        if (fallbackImageOverride != null) {
            fallbackImage = fallbackImageOverride;
        }

        ApplyFallbackSprite();
        PlayIdle();
        SetExpression(DollLive2DExpressionIDs.Normal);
        SetCoreGlow(1f);
        SetSanStress(0f);
    }

    public void Initialize(DollDynamicVisualLoadResult visualResult, Image fallbackImageOverride = null) {
        Initialize(visualResult != null ? new DollLive2DVisualLoadResult(visualResult) : null, fallbackImageOverride);
    }

    public void PlayIdle() {
        CurrentMotionID = DollLive2DMotionIDs.Idle;
    }

    public void PlayLowSanIdle() {
        CurrentMotionID = DollLive2DMotionIDs.LowSanIdle;
    }

    public void PlayRepairReact() {
        CurrentMotionID = DollLive2DMotionIDs.RepairReact;
    }

    public void PlayHitReact() {
        CurrentMotionID = DollLive2DMotionIDs.HitReact;
    }

    public void SetExpression(string expressionID) {
        CurrentExpressionID = string.IsNullOrEmpty(expressionID)
            ? DollLive2DExpressionIDs.Normal
            : expressionID;
    }

    public void SetCoreGlow(float normalized) {
        CoreGlowNormalized = Mathf.Clamp01(normalized);
    }

    public void SetSanStress(float normalized) {
        SanStressNormalized = Mathf.Clamp01(normalized);
    }

    private void ApplyFallbackSprite() {
        if (fallbackImage == null || VisualResult == null || VisualResult.FallbackSprite == null) {
            return;
        }

        fallbackImage.sprite = VisualResult.FallbackSprite;
        fallbackImage.preserveAspect = true;
        fallbackImage.raycastTarget = false;
    }
}
