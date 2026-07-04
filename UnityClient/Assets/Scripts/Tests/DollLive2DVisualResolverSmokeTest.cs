using UnityEngine;

public static class DollLive2DVisualResolverSmokeTest {
    public static void Run() {
        VisualAssetRegistry registry = null;
        VisualAssetRegistry missingFallbackRegistry = null;
        GameObject dynamicPrefab = null;
        GameObject missingPrefab = null;
        Sprite fallbackSprite = null;

        try {
            Debug.Log("=== Running Doll Live2D Visual Resolver Smoke Test ===");

            registry = ScriptableObject.CreateInstance<VisualAssetRegistry>();
            dynamicPrefab = new GameObject("DollLive2D_doll_proto_0_Test");
            missingPrefab = new GameObject("MissingLive2D_Prefab_Test");
            fallbackSprite = CreateSprite("doll_proto_0_stand_test_sprite");
            registry.MissingPrefab = missingPrefab;
            registry.Entries.Add(new VisualAssetEntry {
                VisualID = DollLive2DVisualResolver.DefaultDynamicVisualID,
                Prefab = dynamicPrefab
            });
            registry.Entries.Add(new VisualAssetEntry {
                VisualID = DollLive2DVisualResolver.DefaultFallbackSpriteID,
                Sprite = fallbackSprite
            });
            VisualAssetService.SetRegistry(registry);

            DollLive2DVisualLoadResult dynamicResult = DollLive2DVisualResolver.ResolveDollProto0();
            if (dynamicResult.UseDynamicPrefab && dynamicResult.Prefab == dynamicPrefab && dynamicResult.FallbackSprite == fallbackSprite) {
                Debug.Log("Doll Live2D Dynamic Prefab Lookup PASSED.");
            } else {
                Debug.LogError("Doll Live2D Dynamic Prefab Lookup FAILED.");
            }

            DollLive2DVisualLoadResult missingPrefabResult = DollLive2DVisualResolver.Resolve(
                DollLive2DVisualResolver.DefaultDollID,
                "missing_doll_proto_0_live2d",
                DollLive2DVisualResolver.DefaultFallbackSpriteID);
            if (missingPrefabResult.UseStaticFallback && missingPrefabResult.FallbackReason == "dynamic_prefab_missing") {
                Debug.Log("Doll Live2D Missing Prefab Fallback PASSED.");
            } else {
                Debug.LogError($"Doll Live2D Missing Prefab Fallback FAILED. Mode={missingPrefabResult.Mode}, Reason={missingPrefabResult.FallbackReason}");
            }

            DollLive2DVisualLoadResult missingRuntimeResult = DollLive2DVisualResolver.ResolveDollProto0(runtimePackageAvailable: false);
            if (missingRuntimeResult.UseStaticFallback && missingRuntimeResult.FallbackReason == "runtime_package_missing") {
                Debug.Log("Doll Live2D Missing Runtime Fallback PASSED.");
            } else {
                Debug.LogError($"Doll Live2D Missing Runtime Fallback FAILED. Mode={missingRuntimeResult.Mode}, Reason={missingRuntimeResult.FallbackReason}");
            }

            DollLive2DVisualLoadResult missingMotionResult = DollLive2DVisualResolver.ResolveDollProto0(
                runtimePackageAvailable: true,
                requiredMotionAvailable: false,
                requiredMotionID: "hit_react");
            if (missingMotionResult.UseStaticFallback && missingMotionResult.FallbackReason == "required_motion_missing:hit_react") {
                Debug.Log("Doll Live2D Missing Motion Fallback PASSED.");
            } else {
                Debug.LogError($"Doll Live2D Missing Motion Fallback FAILED. Mode={missingMotionResult.Mode}, Reason={missingMotionResult.FallbackReason}");
            }

            missingFallbackRegistry = ScriptableObject.CreateInstance<VisualAssetRegistry>();
            VisualAssetService.SetRegistry(missingFallbackRegistry);
            DollLive2DVisualLoadResult missingFallbackResult = DollLive2DVisualResolver.ResolveDollProto0();
            if (missingFallbackResult.Mode == DollLive2DVisualMode.MissingFallback && missingFallbackResult.FallbackSprite == null) {
                Debug.Log("Doll Live2D Missing Fallback Detection PASSED.");
            } else {
                Debug.LogError($"Doll Live2D Missing Fallback Detection FAILED. Mode={missingFallbackResult.Mode}");
            }

            Debug.Log("=== Doll Live2D Visual Resolver Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[DollLive2DVisualResolverSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        } finally {
            VisualAssetService.SetRegistry(null);
            DestroyTestObject(dynamicPrefab);
            DestroyTestObject(missingPrefab);
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
