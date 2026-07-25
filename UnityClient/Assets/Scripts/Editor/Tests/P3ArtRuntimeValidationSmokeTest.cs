using System;
using System.IO;
using P3.Validation;
using UnityEditor;
using UnityEngine;

public static class P3ArtRuntimeValidationSmokeTest {
    public static void Run() {
        var registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
        VisualAssetEntry registeredEntry;
        if (registry == null
            || !registry.TryGetEntry("doll_zero_dialogue_neutral", out registeredEntry)
            || registeredEntry == null
            || registeredEntry.Sprite == null) {
            throw new Exception("registered neutral portrait fixture is unavailable");
        }
        var registeredPath = AssetDatabase.GetAssetPath(registeredEntry.Sprite).Replace('\\', '/');
        var registeredGuid = AssetDatabase.AssetPathToGUID(registeredPath);
        Func<string, ArtTargetDefinition> targetResolver = targetId => new ArtTargetDefinition {
            TargetId = targetId,
            RequiredVisualIds = { "doll_zero_dialogue_neutral" }
        };

        var run = "art_runtime_validation_" + Guid.NewGuid().ToString("N");
        ArtValidationOrchestrator.Start(run, "art_focus", new[] { "workshop_main" }, "test");
        ArtValidationOrchestrator.RecordInspection(run, new ArtInspectionResult {
            TargetId = "workshop_main",
            Reachable = true,
            BindingStatus = "passed",
            Bindings = {
                new ArtBindingEvidence {
                    TargetId = "workshop_main",
                    VisualId = "doll_zero_dialogue_neutral",
                    ComponentPath = "WorkshopPanel/Dialogue/Portrait",
                    AssetPath = registeredPath,
                    AssetGuid = registeredGuid,
                    Status = "passed"
                }
            }
        });
        ArtValidationOrchestrator.RecordCapture(run, ArtCaptureRole.Final, "workshop_main");

        var rejected = false;
        try {
            ArtValidationOrchestrator.FinalizeRuntimeValidated(run, targetResolver);
        } catch (InvalidOperationException) {
            rejected = true;
        }
        if (!rejected) {
            throw new Exception("runtime validation completed without Agent review");
        }

        ArtValidationOrchestrator.RecordReview(run, new ArtValidationReview {
            Decision = "passed",
            Reviewer = "agent",
            Notes = "bounded runtime art review passed"
        });
        var completed = ArtValidationOrchestrator.FinalizeRuntimeValidated(run, targetResolver);
        if (completed.Claim != "runtime_validated" || completed.AutomationStatus != "Passed") {
            throw new Exception("runtime validated claim was not persisted");
        }
        if (string.IsNullOrWhiteSpace(completed.RuntimeValidationEvidencePath)
            || !File.Exists(completed.RuntimeValidationEvidencePath)) {
            throw new Exception("runtime validation evidence was not written");
        }
        if (ArtValidationOrchestrator.FinalizeRuntimeValidated(run, targetResolver).RuntimeValidationEvidencePath != completed.RuntimeValidationEvidencePath) {
            throw new Exception("runtime validation finalization is not idempotent");
        }

        var failedRun = "art_runtime_validation_missing_binding_" + Guid.NewGuid().ToString("N");
        ArtValidationOrchestrator.Start(failedRun, "art_focus", new[] { "workshop_main" }, "test");
        ArtValidationOrchestrator.RecordInspection(failedRun, new ArtInspectionResult {
            TargetId = "workshop_main",
            Reachable = true,
            BindingStatus = "failed"
        });
        ArtValidationOrchestrator.RecordCapture(failedRun, ArtCaptureRole.Final);
        ArtValidationOrchestrator.RecordReview(failedRun, new ArtValidationReview {
            Decision = "passed",
            Reviewer = "agent"
        });
        rejected = false;
        try {
            ArtValidationOrchestrator.FinalizeRuntimeValidated(failedRun, targetResolver);
        } catch (InvalidOperationException error) {
            rejected = error.Message.Contains("runtime_binding_missing");
        }
        if (!rejected) {
            throw new Exception("runtime binding failure was not routed");
        }

        var missingContractRun = "art_runtime_validation_missing_contract_" + Guid.NewGuid().ToString("N");
        ArtValidationOrchestrator.Start(missingContractRun, "art_focus", new[] { "workshop_main" }, "test");
        ArtValidationOrchestrator.RecordInspection(missingContractRun, new ArtInspectionResult {
            TargetId = "workshop_main",
            Reachable = true,
            BindingStatus = "passed",
            Bindings = {
                new ArtBindingEvidence {
                    TargetId = "workshop_main",
                    VisualId = "doll_zero_dialogue_neutral",
                    ComponentPath = "WorkshopPanel/Dialogue/Portrait",
                    AssetPath = registeredPath,
                    AssetGuid = registeredGuid,
                    Status = "passed"
                }
            }
        });
        ArtValidationOrchestrator.RecordCapture(missingContractRun, ArtCaptureRole.Final, "workshop_main");
        ArtValidationOrchestrator.RecordReview(missingContractRun, new ArtValidationReview {
            Decision = "passed",
            Reviewer = "agent"
        });
        rejected = false;
        try {
            ArtValidationOrchestrator.FinalizeRuntimeValidated(missingContractRun);
        } catch (InvalidOperationException error) {
            rejected = error.Message.Contains("runtime_binding_contract_missing");
        }
        if (!rejected) {
            throw new Exception("empty runtime binding contract was accepted");
        }

        var contract = new ArtTargetDefinition {
            TargetId = "runtime_binding_contract",
            RequiredVisualIds = { "doll_zero_dialogue_neutral" }
        };
        var resolution = ArtLiveInspectionService.ResolveBindingEvidence(contract, new[] {
            new ArtUiNodeSnapshot {
                Path = "Portrait",
                Active = true,
                VisualId = "doll_zero_dialogue_neutral",
                SpriteAssetPath = registeredPath,
                SpriteGuid = registeredGuid
            }
        });
        if (resolution.Status != "passed" || resolution.Bindings.Count != 1) {
            throw new Exception("valid runtime binding contract was rejected");
        }
        var missing = ArtLiveInspectionService.ResolveBindingEvidence(contract, new ArtUiNodeSnapshot[0]);
        if (missing.Status != "failed" || missing.Issues.Count != 1 || !missing.Issues[0].Blocking) {
            throw new Exception("missing runtime binding was not detected");
        }
        var notConfigured = ArtLiveInspectionService.ResolveBindingEvidence(
            new ArtTargetDefinition { TargetId = "missing_contract" },
            new ArtUiNodeSnapshot[0]);
        if (notConfigured.Status != "not_configured"
            || notConfigured.Issues.Count != 1
            || notConfigured.Issues[0].IssueId != "runtime_binding_contract_missing") {
            throw new Exception("missing runtime binding contract was not detected");
        }
        var incoming = ArtLiveInspectionService.ResolveBindingEvidence(contract, new[] {
            new ArtUiNodeSnapshot {
                Path = "Portrait",
                Active = true,
                VisualId = "doll_zero_dialogue_neutral",
                SpriteAssetPath = "Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral.png",
                SpriteGuid = registeredGuid
            }
        });
        if (incoming.Status != "failed") {
            throw new Exception("IncomingAI runtime binding was accepted");
        }
        var duplicate = ArtLiveInspectionService.ResolveBindingEvidence(contract, new[] {
            new ArtUiNodeSnapshot {
                Path = "PortraitA",
                Active = true,
                VisualId = "doll_zero_dialogue_neutral",
                SpriteAssetPath = registeredPath,
                SpriteGuid = registeredGuid
            },
            new ArtUiNodeSnapshot {
                Path = "PortraitB",
                Active = true,
                VisualId = "doll_zero_dialogue_neutral",
                SpriteAssetPath = registeredPath,
                SpriteGuid = registeredGuid
            }
        });
        if (duplicate.Status != "failed") {
            throw new Exception("duplicate runtime binding was accepted");
        }

        Debug.Log("P3 Art Runtime Validation PASSED");
    }
}
