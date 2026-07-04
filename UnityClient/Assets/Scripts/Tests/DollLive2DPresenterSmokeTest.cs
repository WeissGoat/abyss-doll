using UnityEngine;
using UnityEngine.UI;

public static class DollLive2DPresenterSmokeTest {
    public static void Run() {
        VisualAssetRegistry registry = null;
        Sprite fallbackSprite = null;
        GameObject presenterObject = null;

        try {
            Debug.Log("=== Running Doll Live2D Presenter Smoke Test ===");

            registry = ScriptableObject.CreateInstance<VisualAssetRegistry>();
            fallbackSprite = CreateSprite("doll_proto_0_presenter_fallback_sprite");
            registry.Entries.Add(new VisualAssetEntry {
                VisualID = DollLive2DVisualResolver.DefaultFallbackSpriteID,
                Sprite = fallbackSprite
            });
            VisualAssetService.SetRegistry(registry);

            DollLive2DVisualLoadResult visualResult = DollLive2DVisualResolver.Resolve(
                DollLive2DVisualResolver.DefaultDollID,
                "missing_doll_proto_0_live2d",
                DollLive2DVisualResolver.DefaultFallbackSpriteID);

            presenterObject = new GameObject("FallbackDollLive2DPresenter_Test", typeof(RectTransform));
            Image fallbackImage = presenterObject.AddComponent<Image>();
            FallbackDollLive2DPresenter presenter = presenterObject.AddComponent<FallbackDollLive2DPresenter>();
            presenter.Initialize(visualResult, fallbackImage);

            if (fallbackImage.sprite == fallbackSprite && fallbackImage.preserveAspect && !fallbackImage.raycastTarget) {
                Debug.Log("Doll Live2D Presenter Fallback Sprite Binding PASSED.");
            } else {
                Debug.LogError("Doll Live2D Presenter Fallback Sprite Binding FAILED.");
            }

            DollLive2DPresenterDriver.Apply(presenter, new DollLive2DPresentationIntent {
                UseLowSanIdle = true,
                ExpressionID = DollLive2DExpressionIDs.LowSan,
                CoreGlowNormalized = 0.35f,
                SanStressNormalized = 1.4f
            });
            if (presenter.CurrentMotionID == DollLive2DMotionIDs.LowSanIdle
                && presenter.CurrentExpressionID == DollLive2DExpressionIDs.LowSan
                && Approximately(presenter.CoreGlowNormalized, 0.35f)
                && Approximately(presenter.SanStressNormalized, 1f)) {
                Debug.Log("Doll Live2D Presenter Low SAN Intent PASSED.");
            } else {
                Debug.LogError("Doll Live2D Presenter Low SAN Intent FAILED.");
            }

            DollLive2DPresenterDriver.Apply(presenter, new DollLive2DPresentationIntent {
                TriggerRepairReact = true,
                ExpressionID = DollLive2DExpressionIDs.Relaxed,
                CoreGlowNormalized = 1.2f,
                SanStressNormalized = -0.4f
            });
            if (presenter.CurrentMotionID == DollLive2DMotionIDs.RepairReact
                && presenter.CurrentExpressionID == DollLive2DExpressionIDs.Relaxed
                && Approximately(presenter.CoreGlowNormalized, 1f)
                && Approximately(presenter.SanStressNormalized, 0f)) {
                Debug.Log("Doll Live2D Presenter Repair Intent PASSED.");
            } else {
                Debug.LogError("Doll Live2D Presenter Repair Intent FAILED.");
            }

            DollLive2DPresenterDriver.Apply(presenter, new DollLive2DPresentationIntent {
                TriggerRepairReact = true,
                TriggerHitReact = true,
                ExpressionID = DollLive2DExpressionIDs.Hurt
            });
            if (presenter.CurrentMotionID == DollLive2DMotionIDs.HitReact
                && presenter.CurrentExpressionID == DollLive2DExpressionIDs.Hurt) {
                Debug.Log("Doll Live2D Presenter Hit Intent Priority PASSED.");
            } else {
                Debug.LogError("Doll Live2D Presenter Hit Intent Priority FAILED.");
            }

            Debug.Log("=== Doll Live2D Presenter Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[DollLive2DPresenterSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        } finally {
            VisualAssetService.SetRegistry(null);
            DestroyTestObject(presenterObject);
            DestroyTestObject(registry);
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
            Color.green,
            Color.green,
            Color.white
        });
        texture.Apply();
        texture.name = $"{spriteName}_texture";
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        sprite.name = spriteName;
        return sprite;
    }

    private static bool Approximately(float left, float right) {
        return Mathf.Abs(left - right) < 0.001f;
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
