using UnityEngine;

public enum DollDynamicModelKind {
    DollPuppet,
    Live2D,
    Spine
}

public enum DollDynamicVisualMode {
    DynamicPrefab,
    StaticFallback,
    MissingFallback
}

public sealed class DollDynamicVisualLoadResult {
    public DollDynamicVisualLoadResult(
        string dollID,
        string dynamicVisualID,
        string fallbackSpriteID,
        DollDynamicModelKind modelKind,
        DollDynamicVisualMode mode,
        GameObject prefab,
        Sprite fallbackSprite,
        string fallbackReason) {
        DollID = dollID;
        DynamicVisualID = dynamicVisualID;
        FallbackSpriteID = fallbackSpriteID;
        ModelKind = modelKind;
        Mode = mode;
        Prefab = prefab;
        FallbackSprite = fallbackSprite;
        FallbackReason = fallbackReason;
    }

    public string DollID { get; }
    public string DynamicVisualID { get; }
    public string FallbackSpriteID { get; }
    public DollDynamicModelKind ModelKind { get; }
    public DollDynamicVisualMode Mode { get; }
    public GameObject Prefab { get; }
    public Sprite FallbackSprite { get; }
    public string FallbackReason { get; }
    public bool UseDynamicPrefab => Mode == DollDynamicVisualMode.DynamicPrefab && Prefab != null;
    public bool UseStaticFallback => Mode == DollDynamicVisualMode.StaticFallback && FallbackSprite != null;
}

public static class DollDynamicVisualResolver {
    public const string DefaultDollID = "doll_proto_0";
    public const string DefaultDynamicVisualID = "doll_proto_0_live2d";
    public const string DefaultFallbackSpriteID = "doll_proto_0_stand";
    public const DollDynamicModelKind DefaultModelKind = DollDynamicModelKind.DollPuppet;

    public static DollDynamicVisualLoadResult ResolveDollProto0(
        bool dynamicRuntimeAvailable = true,
        bool requiredMotionAvailable = true,
        string requiredMotionID = null) {
        return Resolve(
            DefaultDollID,
            DefaultDynamicVisualID,
            DefaultFallbackSpriteID,
            DefaultModelKind,
            dynamicRuntimeAvailable,
            requiredMotionAvailable,
            requiredMotionID);
    }

    public static DollDynamicVisualLoadResult Resolve(
        string dollID,
        string dynamicVisualID,
        string fallbackSpriteID,
        DollDynamicModelKind modelKind = DefaultModelKind,
        bool dynamicRuntimeAvailable = true,
        bool requiredMotionAvailable = true,
        string requiredMotionID = null,
        string runtimeMissingReason = "dynamic_runtime_missing") {
        Sprite fallbackSprite = ResolveFallbackSprite(fallbackSpriteID);

        if (!dynamicRuntimeAvailable) {
            return BuildFallback(dollID, dynamicVisualID, fallbackSpriteID, modelKind, fallbackSprite, runtimeMissingReason);
        }

        if (!requiredMotionAvailable) {
            string reason = string.IsNullOrEmpty(requiredMotionID)
                ? "required_motion_missing"
                : $"required_motion_missing:{requiredMotionID}";
            return BuildFallback(dollID, dynamicVisualID, fallbackSpriteID, modelKind, fallbackSprite, reason);
        }

        if (VisualAssetService.TryGetPrefab(dynamicVisualID, out GameObject prefab)) {
            return new DollDynamicVisualLoadResult(
                dollID,
                dynamicVisualID,
                fallbackSpriteID,
                modelKind,
                DollDynamicVisualMode.DynamicPrefab,
                prefab,
                fallbackSprite,
                string.Empty);
        }

        return BuildFallback(dollID, dynamicVisualID, fallbackSpriteID, modelKind, fallbackSprite, "dynamic_prefab_missing");
    }

    private static Sprite ResolveFallbackSprite(string fallbackSpriteID) {
        return VisualAssetService.TryGetSprite(fallbackSpriteID, out Sprite fallbackSprite)
            ? fallbackSprite
            : null;
    }

    private static DollDynamicVisualLoadResult BuildFallback(
        string dollID,
        string dynamicVisualID,
        string fallbackSpriteID,
        DollDynamicModelKind modelKind,
        Sprite fallbackSprite,
        string reason) {
        DollDynamicVisualMode mode = fallbackSprite != null
            ? DollDynamicVisualMode.StaticFallback
            : DollDynamicVisualMode.MissingFallback;
        return new DollDynamicVisualLoadResult(
            dollID,
            dynamicVisualID,
            fallbackSpriteID,
            modelKind,
            mode,
            null,
            fallbackSprite,
            reason);
    }
}
