using UnityEngine;
using UnityEngine.UI;

public sealed class DollLive2DUGUIBridge : MonoBehaviour {
    [SerializeField] private RawImage dynamicRawImage;
    [SerializeField] private Image fallbackImage;
    [SerializeField] private Camera renderCamera;

    private RenderTexture ownedRenderTexture;
    private Sprite currentFallbackSprite;

    public RawImage DynamicRawImage => dynamicRawImage;
    public Image FallbackImage => fallbackImage;
    public Camera RenderCamera => renderCamera;
    public RenderTexture CurrentRenderTexture => dynamicRawImage != null ? dynamicRawImage.texture as RenderTexture : null;

    public void EnsureDisplayObjects() {
        dynamicRawImage = EnsureGraphic(dynamicRawImage, "DollLive2D_RawImage");
        fallbackImage = EnsureGraphic(fallbackImage, "DollLive2D_FallbackImage");
        ApplyGraphicRules(dynamicRawImage);
        ApplyGraphicRules(fallbackImage);
    }

    public void Configure(DollLive2DVisualLoadResult visualResult) {
        EnsureDisplayObjects();
        currentFallbackSprite = visualResult != null ? visualResult.FallbackSprite : null;
        if (visualResult != null && visualResult.UseStaticFallback) {
            ShowFallback(visualResult.FallbackSprite);
            return;
        }

        if (CurrentRenderTexture == null) {
            ShowFallback(currentFallbackSprite);
        }
    }

    public RenderTexture AttachRenderCamera(Camera camera, int width = 512, int height = 768, int depth = 16) {
        EnsureDisplayObjects();
        ReleaseOwnedRenderTexture();
        renderCamera = camera;
        if (renderCamera == null) {
            ShowFallback(currentFallbackSprite);
            return null;
        }

        ownedRenderTexture = new RenderTexture(width, height, depth, RenderTextureFormat.ARGB32) {
            name = "DollLive2D_RenderTexture"
        };
        ownedRenderTexture.Create();
        renderCamera.targetTexture = ownedRenderTexture;
        ShowDynamic(ownedRenderTexture);
        return ownedRenderTexture;
    }

    public void ShowDynamic(RenderTexture renderTexture) {
        EnsureDisplayObjects();
        if (renderTexture == null) {
            ShowFallback(currentFallbackSprite);
            return;
        }

        dynamicRawImage.texture = renderTexture;
        dynamicRawImage.enabled = true;
        fallbackImage.enabled = false;
        ApplyGraphicRules(dynamicRawImage);
        ApplyGraphicRules(fallbackImage);
    }

    public void ShowFallback(Sprite fallbackSprite) {
        EnsureDisplayObjects();
        currentFallbackSprite = fallbackSprite;
        dynamicRawImage.texture = null;
        dynamicRawImage.enabled = false;
        fallbackImage.sprite = fallbackSprite;
        fallbackImage.enabled = fallbackSprite != null;
        fallbackImage.preserveAspect = true;
        ApplyGraphicRules(dynamicRawImage);
        ApplyGraphicRules(fallbackImage);
    }

    public void ClearDynamic() {
        if (dynamicRawImage != null) {
            dynamicRawImage.texture = null;
            dynamicRawImage.enabled = false;
        }

        ShowFallback(currentFallbackSprite);
    }

    private void OnDestroy() {
        if (renderCamera != null && renderCamera.targetTexture == ownedRenderTexture) {
            renderCamera.targetTexture = null;
        }

        ReleaseOwnedRenderTexture();
    }

    private T EnsureGraphic<T>(T graphic, string objectName) where T : Graphic {
        if (graphic != null) {
            return graphic;
        }

        Transform existing = transform.Find(objectName);
        if (existing != null && existing.TryGetComponent(out T existingGraphic)) {
            return existingGraphic;
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(transform, false);
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return child.AddComponent<T>();
    }

    private static void ApplyGraphicRules(Graphic graphic) {
        if (graphic == null) {
            return;
        }

        graphic.raycastTarget = false;
    }

    private void ReleaseOwnedRenderTexture() {
        if (ownedRenderTexture == null) {
            return;
        }

        ownedRenderTexture.Release();
        if (Application.isPlaying) {
            Destroy(ownedRenderTexture);
        } else {
            DestroyImmediate(ownedRenderTexture);
        }

        ownedRenderTexture = null;
    }
}
