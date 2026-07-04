using UnityEngine;
using UnityEngine.UI;

public static class DollLive2DUGUIBridgeSmokeTest {
    public static void Run() {
        GameObject bridgeObject = null;
        GameObject cameraObject = null;
        Sprite fallbackSprite = null;
        RenderTexture externalTexture = null;

        try {
            Debug.Log("=== Running Doll Live2D UGUI Bridge Smoke Test ===");

            bridgeObject = new GameObject("DollLive2DUGUIBridge_Test", typeof(RectTransform));
            DollLive2DUGUIBridge bridge = bridgeObject.AddComponent<DollLive2DUGUIBridge>();
            fallbackSprite = CreateSprite("doll_proto_0_bridge_fallback_sprite");

            bridge.ShowFallback(fallbackSprite);
            if (bridge.FallbackImage != null
                && bridge.FallbackImage.enabled
                && bridge.FallbackImage.sprite == fallbackSprite
                && bridge.FallbackImage.preserveAspect
                && !bridge.FallbackImage.raycastTarget
                && bridge.DynamicRawImage != null
                && !bridge.DynamicRawImage.enabled
                && !bridge.DynamicRawImage.raycastTarget) {
                Debug.Log("Doll Live2D UGUI Bridge Fallback PASSED.");
            } else {
                Debug.LogError("Doll Live2D UGUI Bridge Fallback FAILED.");
            }

            externalTexture = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32);
            externalTexture.Create();
            bridge.ShowDynamic(externalTexture);
            if (bridge.DynamicRawImage.enabled
                && bridge.DynamicRawImage.texture == externalTexture
                && !bridge.DynamicRawImage.raycastTarget
                && !bridge.FallbackImage.enabled) {
                Debug.Log("Doll Live2D UGUI Bridge Dynamic Texture PASSED.");
            } else {
                Debug.LogError("Doll Live2D UGUI Bridge Dynamic Texture FAILED.");
            }

            bridge.ClearDynamic();
            if (bridge.FallbackImage.enabled && bridge.FallbackImage.sprite == fallbackSprite && !bridge.DynamicRawImage.enabled) {
                Debug.Log("Doll Live2D UGUI Bridge Clear Dynamic PASSED.");
            } else {
                Debug.LogError("Doll Live2D UGUI Bridge Clear Dynamic FAILED.");
            }

            cameraObject = new GameObject("DollLive2D_Camera_Test");
            Camera camera = cameraObject.AddComponent<Camera>();
            RenderTexture ownedTexture = bridge.AttachRenderCamera(camera, 256, 384, 16);
            if (ownedTexture != null
                && camera.targetTexture == ownedTexture
                && bridge.DynamicRawImage.enabled
                && bridge.DynamicRawImage.texture == ownedTexture
                && !bridge.DynamicRawImage.raycastTarget) {
                Debug.Log("Doll Live2D UGUI Bridge Render Camera PASSED.");
            } else {
                Debug.LogError("Doll Live2D UGUI Bridge Render Camera FAILED.");
            }

            Debug.Log("=== Doll Live2D UGUI Bridge Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[DollLive2DUGUIBridgeSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        } finally {
            DestroyTestObject(bridgeObject);
            DestroyTestObject(cameraObject);
            if (externalTexture != null) {
                externalTexture.Release();
                DestroyTestObject(externalTexture);
            }

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
            Color.magenta,
            Color.magenta,
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
