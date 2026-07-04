using UnityEngine;

public enum DollLive2DVisualMode {
    DynamicPrefab,
    StaticFallback,
    MissingFallback
}

public sealed class DollLive2DVisualLoadResult {
    public DollLive2DVisualLoadResult(DollDynamicVisualLoadResult dynamicResult)
        : this(
            dynamicResult != null ? dynamicResult.DollID : string.Empty,
            dynamicResult != null ? dynamicResult.DynamicVisualID : string.Empty,
            dynamicResult != null ? dynamicResult.FallbackSpriteID : string.Empty,
            dynamicResult != null ? MapMode(dynamicResult.Mode) : DollLive2DVisualMode.MissingFallback,
            dynamicResult != null ? dynamicResult.Prefab : null,
            dynamicResult != null ? dynamicResult.FallbackSprite : null,
            dynamicResult != null ? dynamicResult.FallbackReason : "dynamic_result_missing") {
    }

    public DollLive2DVisualLoadResult(
        string dollID,
        string dynamicVisualID,
        string fallbackSpriteID,
        DollLive2DVisualMode mode,
        GameObject prefab,
        Sprite fallbackSprite,
        string fallbackReason) {
        DollID = dollID;
        DynamicVisualID = dynamicVisualID;
        FallbackSpriteID = fallbackSpriteID;
        Mode = mode;
        Prefab = prefab;
        FallbackSprite = fallbackSprite;
        FallbackReason = fallbackReason;
    }

    public string DollID { get; }
    public string DynamicVisualID { get; }
    public string FallbackSpriteID { get; }
    public DollLive2DVisualMode Mode { get; }
    public GameObject Prefab { get; }
    public Sprite FallbackSprite { get; }
    public string FallbackReason { get; }
    public bool UseDynamicPrefab => Mode == DollLive2DVisualMode.DynamicPrefab && Prefab != null;
    public bool UseStaticFallback => Mode == DollLive2DVisualMode.StaticFallback && FallbackSprite != null;

    private static DollLive2DVisualMode MapMode(DollDynamicVisualMode mode) {
        switch (mode) {
            case DollDynamicVisualMode.DynamicPrefab:
                return DollLive2DVisualMode.DynamicPrefab;
            case DollDynamicVisualMode.StaticFallback:
                return DollLive2DVisualMode.StaticFallback;
            default:
                return DollLive2DVisualMode.MissingFallback;
        }
    }
}

public static class DollLive2DVisualResolver {
    public const string DefaultDollID = DollDynamicVisualResolver.DefaultDollID;
    public const string DefaultDynamicVisualID = DollDynamicVisualResolver.DefaultDynamicVisualID;
    public const string DefaultFallbackSpriteID = DollDynamicVisualResolver.DefaultFallbackSpriteID;

    public static DollLive2DVisualLoadResult ResolveDollProto0(
        bool runtimePackageAvailable = true,
        bool requiredMotionAvailable = true,
        string requiredMotionID = null) {
        return Resolve(
            DefaultDollID,
            DefaultDynamicVisualID,
            DefaultFallbackSpriteID,
            runtimePackageAvailable,
            requiredMotionAvailable,
            requiredMotionID);
    }

    public static DollLive2DVisualLoadResult Resolve(
        string dollID,
        string dynamicVisualID,
        string fallbackSpriteID,
        bool runtimePackageAvailable = true,
        bool requiredMotionAvailable = true,
        string requiredMotionID = null) {
        DollDynamicVisualLoadResult dynamicResult = DollDynamicVisualResolver.Resolve(
            dollID,
            dynamicVisualID,
            fallbackSpriteID,
            DollDynamicModelKind.DollPuppet,
            runtimePackageAvailable,
            requiredMotionAvailable,
            requiredMotionID,
            "runtime_package_missing");
        return new DollLive2DVisualLoadResult(dynamicResult);
    }
}
