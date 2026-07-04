using UnityEngine;
using UnityEngine.UI;

public static class DollDynamicVisualResolverSmokeTest {
    public static void Run() {
        VisualAssetRegistry registry = null;
        VisualAssetRegistry missingFallbackRegistry = null;
        GameObject dynamicPrefab = null;
        GameObject missingPrefab = null;
        GameObject presenterObject = null;
        Sprite fallbackSprite = null;

        try {
            Debug.Log("=== Running Doll Dynamic Visual Resolver Smoke Test ===");

            registry = ScriptableObject.CreateInstance<VisualAssetRegistry>();
            dynamicPrefab = new GameObject("DollPuppet_doll_proto_0_Test");
            missingPrefab = new GameObject("MissingDollDynamic_Prefab_Test");
            fallbackSprite = CreateSprite("doll_proto_0_stand_test_sprite");
            registry.MissingPrefab = missingPrefab;
            registry.Entries.Add(new VisualAssetEntry {
                VisualID = DollDynamicVisualResolver.DefaultDynamicVisualID,
                Prefab = dynamicPrefab
            });
            registry.Entries.Add(new VisualAssetEntry {
                VisualID = DollDynamicVisualResolver.DefaultFallbackSpriteID,
                Sprite = fallbackSprite
            });
            VisualAssetService.SetRegistry(registry);

            DollDynamicVisualLoadResult dynamicResult = DollDynamicVisualResolver.ResolveDollProto0();
            if (dynamicResult.ModelKind == DollDynamicModelKind.DollPuppet
                && dynamicResult.UseDynamicPrefab
                && dynamicResult.Prefab == dynamicPrefab
                && dynamicResult.FallbackSprite == fallbackSprite) {
                Debug.Log("Doll Dynamic Prefab Lookup PASSED.");
            } else {
                Debug.LogError("Doll Dynamic Prefab Lookup FAILED.");
            }

            DollDynamicVisualLoadResult missingPrefabResult = DollDynamicVisualResolver.Resolve(
                DollDynamicVisualResolver.DefaultDollID,
                "missing_doll_proto_0_live2d",
                DollDynamicVisualResolver.DefaultFallbackSpriteID);
            if (missingPrefabResult.UseStaticFallback && missingPrefabResult.FallbackReason == "dynamic_prefab_missing") {
                Debug.Log("Doll Dynamic Missing Prefab Fallback PASSED.");
            } else {
                Debug.LogError($"Doll Dynamic Missing Prefab Fallback FAILED. Mode={missingPrefabResult.Mode}, Reason={missingPrefabResult.FallbackReason}");
            }

            DollDynamicVisualLoadResult missingRuntimeResult = DollDynamicVisualResolver.ResolveDollProto0(dynamicRuntimeAvailable: false);
            if (missingRuntimeResult.UseStaticFallback && missingRuntimeResult.FallbackReason == "dynamic_runtime_missing") {
                Debug.Log("Doll Dynamic Missing Runtime Fallback PASSED.");
            } else {
                Debug.LogError($"Doll Dynamic Missing Runtime Fallback FAILED. Mode={missingRuntimeResult.Mode}, Reason={missingRuntimeResult.FallbackReason}");
            }

            DollDynamicVisualLoadResult missingMotionResult = DollDynamicVisualResolver.ResolveDollProto0(
                dynamicRuntimeAvailable: true,
                requiredMotionAvailable: false,
                requiredMotionID: DollDynamicMotionIDs.HitReact);
            if (missingMotionResult.UseStaticFallback && missingMotionResult.FallbackReason == "required_motion_missing:hit_react") {
                Debug.Log("Doll Dynamic Missing Motion Fallback PASSED.");
            } else {
                Debug.LogError($"Doll Dynamic Missing Motion Fallback FAILED. Mode={missingMotionResult.Mode}, Reason={missingMotionResult.FallbackReason}");
            }

            DollLive2DVisualLoadResult compatibilityResult = DollLive2DVisualResolver.ResolveDollProto0();
            if (DollDynamicVisualResolver.DefaultDynamicVisualID == DollLive2DVisualResolver.DefaultDynamicVisualID
                && compatibilityResult.UseDynamicPrefab
                && compatibilityResult.Prefab == dynamicPrefab) {
                Debug.Log("Doll Live2D Naming Compatibility PASSED.");
            } else {
                Debug.LogError("Doll Live2D Naming Compatibility FAILED.");
            }

            presenterObject = new GameObject("FallbackDollDynamicPresenter_Test", typeof(RectTransform), typeof(Image));
            FallbackDollLive2DPresenter presenter = presenterObject.AddComponent<FallbackDollLive2DPresenter>();
            presenter.Initialize(missingPrefabResult, presenterObject.GetComponent<Image>());
            IDollDynamicPresenter dynamicPresenter = presenter;
            DollDynamicPresenterDriver.Apply(dynamicPresenter, new DollDynamicPresentationIntent {
                UseLowSanIdle = true,
                ExpressionID = DollDynamicExpressionIDs.LowSan,
                CoreGlowNormalized = 0.25f,
                SanStressNormalized = 0.85f
            });
            if (presenter.CurrentMotionID == DollDynamicMotionIDs.LowSanIdle
                && presenter.CurrentExpressionID == DollDynamicExpressionIDs.LowSan
                && presenter.CoreGlowNormalized == 0.25f
                && presenter.SanStressNormalized == 0.85f) {
                Debug.Log("Doll Dynamic Presenter Compatibility PASSED.");
            } else {
                Debug.LogError("Doll Dynamic Presenter Compatibility FAILED.");
            }

            missingFallbackRegistry = ScriptableObject.CreateInstance<VisualAssetRegistry>();
            VisualAssetService.SetRegistry(missingFallbackRegistry);
            DollDynamicVisualLoadResult missingFallbackResult = DollDynamicVisualResolver.ResolveDollProto0();
            if (missingFallbackResult.Mode == DollDynamicVisualMode.MissingFallback && missingFallbackResult.FallbackSprite == null) {
                Debug.Log("Doll Dynamic Missing Fallback Detection PASSED.");
            } else {
                Debug.LogError($"Doll Dynamic Missing Fallback Detection FAILED. Mode={missingFallbackResult.Mode}");
            }

            Debug.Log("=== Doll Dynamic Visual Resolver Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[DollDynamicVisualResolverSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        } finally {
            VisualAssetService.SetRegistry(null);
            DestroyTestObject(dynamicPrefab);
            DestroyTestObject(missingPrefab);
            DestroyTestObject(presenterObject);
            DestroyTestObject(registry);
            DestroyTestObject(missingFallbackRegistry);
            if (fallbackSprite != null) {
                DestroyTestObject(fallbackSprite.texture);
                DestroyTestObject(fallbackSprite);
            }
        }
    }

    private static Sprite CreateSprite(string spriteName) {
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] {
            Color.white,
            Color.cyan,
            Color.cyan,
            Color.white
        });
        texture.Apply();
        texture.name = $"{spriteName}_texture";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        sprite.name = spriteName;
        return sprite;
    }

    private static void DestroyTestObject(Object target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Object.Destroy(target);
        } else {
            Object.DestroyImmediate(target);
        }
    }
}
