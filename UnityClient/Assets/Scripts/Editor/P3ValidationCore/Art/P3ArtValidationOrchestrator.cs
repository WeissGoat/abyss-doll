using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace P3.Validation {
    [Serializable]
    public sealed class ArtValidationSession {
        public string RunId, ProfileId, InstanceId, Status, AutomationStatus, UpdatedAt;
        public string BindingStatus, Claim, RuntimeValidationEvidencePath;
        public List<string> TargetIds = new List<string>();
        public List<string> RecordedRoles = new List<string>();
        public List<string> CapturedTargetIds = new List<string>();
        public List<ArtInspectionResult> Inspections = new List<ArtInspectionResult>();
        public List<ArtBindingEvidence> Bindings = new List<ArtBindingEvidence>();
        public ArtValidationReview AgentReview;
        public bool HasInspection, HasBlockingIssue, HasPersistedChanges, HasPostPersistInspection;
        public bool HasAgentReview, AgentReviewPassed;
        public int PersistedPlayModeGeneration;
    }

    public static class ArtValidationOrchestrator {
        static string PathFor(string run) => Path.Combine(EvidencePaths.ForArtRun(run).Root, "session.json");

        static void Save(ArtValidationSession session) {
            session.UpdatedAt = DateTime.UtcNow.ToString("o");
            File.WriteAllText(PathFor(session.RunId), JsonConvert.SerializeObject(session, Formatting.Indented));
        }

        static bool HasBlockingInspectionIssue(ArtInspectionResult inspection) {
            return inspection != null
                && (inspection.Issues.Any(issue => issue.Blocking)
                    || (inspection.BindingStatus != "passed" && inspection.BindingStatus != "not_requested"));
        }

        static string AggregateBindingStatus(IEnumerable<ArtInspectionResult> inspections) {
            var items = inspections.Where(item => item != null).ToList();
            if (items.Count == 0) return "not_requested";
            if (items.Any(item => item.BindingStatus == "failed")) return "failed";
            if (items.Any(item => item.BindingStatus == "not_configured")) return "not_configured";
            if (items.All(item => item.BindingStatus == "passed")) return "passed";
            return "not_requested";
        }

        public static ArtValidationSession Load(string run) {
            var path = PathFor(run);
            if (!File.Exists(path)) throw new ArgumentException("art_blocked:unknown_run");
            return JsonConvert.DeserializeObject<ArtValidationSession>(File.ReadAllText(path));
        }

        public static ArtValidationSession Start(string run, string profileId, IEnumerable<string> targets, string instance) {
            if (string.IsNullOrWhiteSpace(run)) throw new ArgumentException("art_blocked:run_id_required");
            var profile = ProfileRegistry.GetArt(profileId);
            var session = new ArtValidationSession {
                RunId = run,
                ProfileId = profileId,
                InstanceId = instance,
                Status = "AwaitingLiveInspection",
                BindingStatus = "not_requested"
            };
            foreach (var id in targets) {
                ArtTargetRegistry.Get(id);
                if (!session.TargetIds.Contains(id)) session.TargetIds.Add(id);
            }
            if (session.TargetIds.Count == 0 && profile.Mode != "regression") throw new ArgumentException("target_id required");
            Save(session);
            return session;
        }

        public static ArtValidationSession RecordInspection(string run, ArtInspectionResult result) {
            if (result == null || string.IsNullOrWhiteSpace(result.TargetId)) throw new ArgumentException("art_blocked:inspection_missing");
            var session = Load(run);
            if (session.ProfileId != "art_regression" && !session.TargetIds.Contains(result.TargetId)) {
                throw new ArgumentException("art_blocked:inspection_target_not_registered:" + result.TargetId);
            }
            session.HasInspection = true;
            session.Inspections.RemoveAll(item => item.TargetId == result.TargetId);
            session.Inspections.Add(result);
            session.Bindings.RemoveAll(item => item.TargetId == result.TargetId);
            foreach (var binding in result.Bindings ?? new List<ArtBindingEvidence>()) {
                if (string.IsNullOrWhiteSpace(binding.TargetId)) binding.TargetId = result.TargetId;
                session.Bindings.Add(binding);
            }
            session.HasBlockingIssue = session.Inspections.Any(HasBlockingInspectionIssue);
            session.BindingStatus = AggregateBindingStatus(session.Inspections);
            if (session.HasPersistedChanges && ArtPlayModeGenerationTracker.Current > session.PersistedPlayModeGeneration) {
                session.HasPostPersistInspection = true;
            }
            session.Status = "AwaitingCaptureDecision";
            ArtLiveInspectionService.Write(run, result);
            Save(session);
            return session;
        }

        public static ArtValidationSession RecordCapture(string run, ArtCaptureRole role) {
            return RecordCapture(run, role, null);
        }

        public static ArtValidationSession RecordCapture(string run, ArtCaptureRole role, string targetId) {
            var session = Load(run);
            var roleName = role.ToString().ToLowerInvariant();
            if (!session.RecordedRoles.Contains(roleName)) session.RecordedRoles.Add(roleName);
            if (!string.IsNullOrWhiteSpace(targetId) && !session.CapturedTargetIds.Contains(targetId)) {
                session.CapturedTargetIds.Add(targetId);
            }
            session.Status = "EvidenceReady";
            Save(session);
            return session;
        }

        public static ArtValidationSession RecordReview(string run, ArtValidationReview review) {
            var session = Load(run);
            if (review == null || string.IsNullOrWhiteSpace(review.Decision)) throw new ArgumentException("art_blocked:review_missing");
            review.ObservedAt = string.IsNullOrWhiteSpace(review.ObservedAt) ? DateTime.UtcNow.ToString("o") : review.ObservedAt;
            session.AgentReview = review;
            session.HasAgentReview = true;
            session.AgentReviewPassed = string.Equals(review.Decision, "passed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(review.Decision, "approved", StringComparison.OrdinalIgnoreCase);
            var directory = EvidencePaths.ForArtRun(run).Root;
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "agent-review.json"), JsonConvert.SerializeObject(review, Formatting.Indented));
            session.Status = "AwaitingRuntimeValidationFinalize";
            Save(session);
            return session;
        }

        public static ArtValidationSession FinalizeRuntimeValidated(string run) {
            return FinalizeRuntimeValidated(run, ArtTargetRegistry.Get);
        }

        internal static ArtValidationSession FinalizeRuntimeValidated(
            string run,
            Func<string, ArtTargetDefinition> targetResolver) {
            var session = Load(run);
            if (session.Claim == "runtime_validated"
                && !string.IsNullOrWhiteSpace(session.RuntimeValidationEvidencePath)
                && File.Exists(session.RuntimeValidationEvidencePath)) {
                return session;
            }
            var profile = ProfileRegistry.GetArt(session.ProfileId);
            if (profile.Mode == "regression") throw new InvalidOperationException("art_blocked:runtime_validation_regression_profile");
            if (!session.HasInspection) throw new InvalidOperationException("art_blocked:inspection_required");

            foreach (var targetId in session.TargetIds) {
                var target = targetResolver(targetId);
                var requiredVisualIds = target.RequiredVisualIds == null
                    ? new List<string>()
                    : target.RequiredVisualIds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (requiredVisualIds.Count == 0) {
                    throw new InvalidOperationException("art_blocked:runtime_binding_contract_missing:" + targetId);
                }
                var inspection = session.Inspections.FirstOrDefault(item => item.TargetId == targetId);
                if (inspection == null) throw new InvalidOperationException("art_blocked:inspection_required:" + targetId);
                if (!inspection.Reachable) {
                    throw new InvalidOperationException("art_blocked:target_screen_unreachable:" + targetId);
                }
                if (inspection.BindingStatus == "not_configured") {
                    throw new InvalidOperationException("art_blocked:runtime_binding_contract_missing:" + targetId);
                }
                if (inspection.BindingStatus != "passed"
                    || requiredVisualIds.Any(visualId => session.Bindings.Count(item =>
                        item.TargetId == targetId
                        && item.Status == "passed"
                        && string.Equals(item.VisualId, visualId, StringComparison.OrdinalIgnoreCase)) != 1)) {
                    throw new InvalidOperationException("art_blocked:runtime_binding_missing:" + targetId);
                }
                if (inspection.Issues.Any(issue => issue.Blocking)) {
                    throw new InvalidOperationException("art_failed:inspection_blocking_issue:" + targetId);
                }
            }
            if (session.HasBlockingIssue) throw new InvalidOperationException("art_failed:inspection_blocking_issue");
            if (!session.HasAgentReview) throw new InvalidOperationException("art_blocked:agent_review_required");
            if (!session.AgentReviewPassed) throw new InvalidOperationException("art_failed:agent_review");

            bool captureRequired;
            if (profile.Mode == "iteration") {
                captureRequired = session.RecordedRoles.Contains("after") && session.HasPostPersistInspection;
            } else if (profile.Mode == "seal") {
                captureRequired = session.RecordedRoles.Contains("seal");
            } else {
                captureRequired = session.RecordedRoles.Contains("final")
                    && (session.TargetIds.Count <= 1 || session.TargetIds.All(id => session.CapturedTargetIds.Contains(id)));
            }
            if (!captureRequired) throw new InvalidOperationException("art_blocked:required_final_capture_missing");

            var evidencePath = Path.Combine(EvidencePaths.ForArtRun(run).Root, "runtime-validation.json");
            var evidence = new {
                schema = "p3-art-runtime-validation@1",
                run_id = session.RunId,
                profile_id = session.ProfileId,
                instance_id = session.InstanceId,
                target_ids = session.TargetIds,
                visual_ids = session.Bindings.Select(binding => binding.VisualId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                binding_status = "passed",
                checks = new {
                    registry_check = "passed",
                    binding_check = "passed",
                    runtime_target_check = "passed",
                    display_check = "passed",
                    console_check = "passed",
                    agent_review = "passed"
                },
                bindings = session.Bindings,
                agent_review = session.AgentReview,
                claim = "runtime_validated",
                status = "passed",
                completed_at = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(evidencePath, JsonConvert.SerializeObject(evidence, Formatting.Indented));
            session.Claim = "runtime_validated";
            session.RuntimeValidationEvidencePath = evidencePath;
            session.Status = "Complete";
            session.AutomationStatus = "Passed";
            Save(session);
            return session;
        }

        public static ArtValidationSession Complete(string run) {
            var session = Load(run);
            var profile = ProfileRegistry.GetArt(session.ProfileId);
            bool ok;
            if (profile.Mode == "iteration") ok = session.RecordedRoles.Contains("before") && session.RecordedRoles.Contains("after") && session.HasPersistedChanges && session.HasPostPersistInspection;
            else if (profile.Mode == "seal") ok = session.RecordedRoles.Contains("seal");
            else if (profile.Mode == "regression") ok = session.RecordedRoles.Contains("regression");
            else ok = session.RecordedRoles.Contains(session.HasBlockingIssue ? "issue" : "final");
            if (!session.HasInspection && profile.Mode != "regression") throw new InvalidOperationException("art_blocked:inspection_required");
            if (!ok) throw new InvalidOperationException(profile.Mode == "iteration" ? "art_blocked:playmode_reload_and_reinspect_required" : "art_blocked:required_capture_missing");
            session.Status = "Complete";
            session.AutomationStatus = session.HasBlockingIssue ? "Failed" : "Passed";
            Save(session);
            return session;
        }
    }
}
