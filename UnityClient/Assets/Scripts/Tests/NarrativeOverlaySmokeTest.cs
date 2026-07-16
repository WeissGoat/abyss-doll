using UnityEngine;
using UnityEngine.UI;

public static class NarrativeOverlaySmokeTest {
    public static void Run() {
        Debug.Log("=== Running Narrative Overlay Smoke Test ===");

        GameObject canvasObj = new GameObject("NarrativeOverlaySmoke_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = VisualDisplaySpecs.ReferenceResolution;
        canvasObj.AddComponent<GraphicRaycaster>();

        P3DialogueOverlayController overlay = P3DialogueOverlayController.CreateUnder(canvas);
        bool constructed = overlay != null
            && overlay.rootGroup != null
            && overlay.blackoutImage != null
            && overlay.visualImage != null
            && overlay.dialoguePanelImage != null
            && overlay.speakerText != null
            && overlay.bodyText != null
            && overlay.continueButton != null
            && overlay.actionButton != null;

        overlay.PresentBlackoutSubtitle("主角", "又到早上了。");
        bool blackoutSubtitle = overlay.IsShowing
            && overlay.IsInputBlocking
            && overlay.blackoutImage.gameObject.activeSelf
            && overlay.CurrentSpeaker == "主角"
            && overlay.CurrentText == "又到早上了。"
            && !overlay.continueButton.gameObject.activeSelf
            && !overlay.HasActionVisible;

        string actionID = string.Empty;
        bool continued = false;
        overlay.ActionRequested += id => actionID = id;
        overlay.ContinueRequested += () => continued = true;

        overlay.PresentLine(new NarrativeOverlayPayload {
            BlockingMode = "modal_light",
            SpeakerName = "零号",
            Text = "你手在抖。",
            ContinueLabel = string.Empty,
            ActionID = "start_doll",
            ActionLabel = "启动人偶",
            VisualRequest = new NarrativeVisualRequest {
                VisualID = "cg_t0_01a_find_no0",
                FallbackVisualID = VisualAssetService.MissingSpriteVisualID,
                Slot = "center",
                UseCover = true
            }
        });

        bool prologueDollStaging = overlay.IsStageDollVisible
            && !overlay.IsStagePropVisible
            && overlay.HasComicPageVisible
            && overlay.VisibleComicPanelCount == 3
            && overlay.HasStandaloneActionVisible
            && !overlay.HasAutoLogControlsVisible;
        string dollStageDiagnostics = $"sprite={overlay.StageBackgroundSpriteName},comic={overlay.HasComicPageVisible}/{overlay.VisibleComicPanelCount},doll={overlay.IsStageDollVisible},action={overlay.HasActionVisible}/{overlay.HasStandaloneActionVisible},continue={overlay.HasContinueVisible},autoLog={overlay.HasAutoLogControlsVisible}";

        overlay.actionButton.onClick.Invoke();
        bool modalLightLine = overlay.IsShowing
            && overlay.IsInputBlocking
            && overlay.CurrentSpeaker == "零号"
            && overlay.CurrentText == "你手在抖。"
            && overlay.LastVisualID == "cg_t0_01a_find_no0"
            && !overlay.visualImage.gameObject.activeSelf
            && !overlay.visualImage.raycastTarget
            && !overlay.continueButton.gameObject.activeSelf
            && overlay.HasActionVisible
            && !continued
            && actionID == "start_doll";

        overlay.PresentLine(new NarrativeOverlayPayload {
            BlockingMode = "modal",
            SpeakerName = "protagonist",
            Text = "debt notice staging",
            ContinueLabel = string.Empty,
            VisualRequest = new NarrativeVisualRequest {
                VisualID = "cg_t0_01a_debt_notice",
                FallbackVisualID = "ui_paper_panel",
                UseCover = true
            }
        });

        bool prologueDebtStaging = overlay.HasComicPageVisible
            && overlay.VisibleComicPanelCount == 3
            && !overlay.IsStagePropVisible
            && overlay.IsLargeStageFocusVisible
            && !overlay.IsStageDollVisible
            && !overlay.HasAutoLogControlsVisible;
        string debtStageDiagnostics = $"sprite={overlay.StageBackgroundSpriteName},comic={overlay.HasComicPageVisible}/{overlay.VisibleComicPanelCount},prop={overlay.IsStagePropVisible},large={overlay.IsLargeStageFocusVisible},doll={overlay.IsStageDollVisible},autoLog={overlay.HasAutoLogControlsVisible}";

        overlay.PresentLine(new NarrativeOverlayPayload {
            BlockingMode = "modal",
            SpeakerName = "零号",
            Text = "……这里……",
            ContinueLabel = "继续",
            VisualRequest = new NarrativeVisualRequest {
                VisualID = "stand_no0_weak_sitting",
                FallbackVisualID = "doll_proto_0_stand",
                UseCover = false
            }
        });
        bool wakeDialogueUtilities = overlay.HasAutoLogControlsVisible;

        overlay.Hide();
        bool hiddenUnlocks = !overlay.IsShowing
            && !overlay.IsInputBlocking
            && !overlay.rootGroup.interactable;

        if (constructed && blackoutSubtitle && modalLightLine && prologueDollStaging && prologueDebtStaging && wakeDialogueUtilities && hiddenUnlocks) {
            Debug.Log("Narrative Overlay Smoke PASSED.");
        } else {
            Debug.LogError(
                "Narrative Overlay Smoke FAILED. "
                + $"Constructed={constructed}, Blackout={blackoutSubtitle}, Line={modalLightLine}, "
                + $"DollStaging={prologueDollStaging}, DebtStaging={prologueDebtStaging}, "
                + $"DollDiag=[{dollStageDiagnostics}], DebtDiag=[{debtStageDiagnostics}], "
                + $"Comic={overlay.HasComicPageVisible}/{overlay.VisibleComicPanelCount}, Action={overlay.HasActionVisible}/{overlay.HasStandaloneActionVisible}, "
                + $"Continue={overlay.HasContinueVisible}, AutoLog={overlay.HasAutoLogControlsVisible}, DollFocus={overlay.IsStageDollVisible}, "
                + $"WakeUtilities={wakeDialogueUtilities}, Stage=[{overlay.StageDebugSummary}], Hidden={hiddenUnlocks}");
        }

        Object.DestroyImmediate(canvasObj);
        Debug.Log("=== Narrative Overlay Smoke Test Finished ===");
    }
}
