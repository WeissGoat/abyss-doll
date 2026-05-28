using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArtAcceptanceRunner 的截图渲染功能。
/// 包含 Camera.Render + RenderTexture 同步渲染、Canvas 模式切换、截图空白检测。
/// </summary>
public partial class ArtAcceptanceRunner {

    private IEnumerator CaptureCurrentScreen(ArtAcceptanceCaptureRecord capture) {
        capture.ActiveControllers = CollectActiveControllers();
        ArtAcceptanceUiCaptureSnapshot uiCapture = BuildUiCaptureSnapshot(capture.ScreenTag);
        _uiSnapshot.Captures.Add(uiCapture);
        ApplyUiRisksToCapture(uiCapture, capture);
        ApplyRequiredUiChecksToCapture(uiCapture, capture);

        string absolutePath = Path.Combine(_outputRoot, capture.File.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        // Wait for UI layout to settle before capturing.
        yield return WaitSecondsRealtime(0.1f);
        Canvas.ForceUpdateCanvases();
        yield return WaitSecondsRealtime(0.05f);

        RenderCameraScreenshot(absolutePath, capture);

        capture.Status = File.Exists(absolutePath) ? "captured" : "failed";
        if (capture.Status == "failed") {
            capture.Errors.Add($"Screenshot file was not created: {absolutePath}");
            AddError($"Capture [{capture.ScreenTag}] failed to write screenshot.");
        }
    }

    /// <summary>
    /// Synchronous screenshot via Camera.Render + RenderTexture.
    /// This approach works at any point in the frame — no WaitForEndOfFrame required.
    /// Temporarily converts Screen Space Overlay canvases to Camera mode so UI is captured.
    /// </summary>
    private void RenderCameraScreenshot(string absolutePath, ArtAcceptanceCaptureRecord capture) {
        Camera camera = Camera.main;
        if (camera == null) {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras) {
                if (cam != null && cam.isActiveAndEnabled) {
                    camera = cam;
                    break;
                }
            }
        }

        if (camera == null) {
            AddError($"No active camera found for screenshot [{absolutePath}].");
            Debug.LogError($"[ArtAcceptance] No active camera for screenshot: {absolutePath}");
            return;
        }

        int width = ReferenceWidth;
        int height = ReferenceHeight;

        // Temporarily attach Screen Space Overlay canvases to this camera so they render.
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        var overlayBackup = new System.Collections.Generic.List<(Canvas canvas, RenderMode mode, Camera cam)>();
        foreach (Canvas c in allCanvases) {
            if (c != null && c.isActiveAndEnabled && c.renderMode == RenderMode.ScreenSpaceOverlay) {
                overlayBackup.Add((c, c.renderMode, c.worldCamera));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
            }
        }

        RenderTexture renderTexture = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        Texture2D screenshot = null;

        try {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            camera.targetTexture = renderTexture;
            camera.aspect = (float)width / height;
            camera.Render();

            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            ApplyScreenshotContentChecks(screenshot, capture);
            File.WriteAllBytes(absolutePath, screenshot.EncodeToPNG());
            Debug.Log($"[ArtAcceptance] Screenshot written: {absolutePath} ({width}x{height})");
        } catch (Exception ex) {
            AddError($"Failed to write screenshot [{absolutePath}]: {ex.Message}");
            Debug.LogError($"[ArtAcceptance] Failed to write screenshot: {absolutePath}\n{ex}");
        } finally {
            // Restore camera state.
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;

            if (renderTexture != null) {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (screenshot != null) {
                Destroy(screenshot);
            }

            // Restore overlay canvases.
            foreach (var backup in overlayBackup) {
                if (backup.canvas != null) {
                    backup.canvas.renderMode = backup.mode;
                    backup.canvas.worldCamera = backup.cam;
                }
            }
        }
    }

    private void ApplyScreenshotContentChecks(Texture2D screenshot, ArtAcceptanceCaptureRecord capture) {
        if (screenshot == null || capture == null) {
            return;
        }

        if (IsScreenshotVisuallyBlank(screenshot)) {
            AddCaptureError(capture, "Screenshot appears visually blank or near-solid color.");
        }
    }

    private bool IsScreenshotVisuallyBlank(Texture2D screenshot) {
        if (screenshot == null) {
            return true;
        }

        int width = screenshot.width;
        int height = screenshot.height;
        int stepX = Mathf.Max(1, width / 32);
        int stepY = Mathf.Max(1, height / 18);
        Color32 first = screenshot.GetPixel(0, 0);
        int sampled = 0;
        int different = 0;

        for (int y = 0; y < height; y += stepY) {
            for (int x = 0; x < width; x += stepX) {
                sampled++;
                Color32 current = screenshot.GetPixel(x, y);
                int delta =
                    Mathf.Abs(current.r - first.r) +
                    Mathf.Abs(current.g - first.g) +
                    Mathf.Abs(current.b - first.b);
                if (delta > 8) {
                    different++;
                }
            }
        }

        if (sampled == 0) {
            return true;
        }

        return different < Mathf.Max(4, sampled / 100);
    }
}
