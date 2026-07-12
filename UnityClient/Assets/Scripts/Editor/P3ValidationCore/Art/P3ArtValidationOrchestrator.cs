using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace P3.Validation {
[Serializable] public sealed class ArtValidationSession { public string RunId,ProfileId,InstanceId,Status,AutomationStatus,UpdatedAt;public List<string> TargetIds=new List<string>();public List<string> RecordedRoles=new List<string>();public bool HasInspection,HasBlockingIssue,HasPersistedChanges,HasPostPersistInspection;public int PersistedPlayModeGeneration; }
public static class ArtValidationOrchestrator {
 static string PathFor(string run)=>Path.Combine(EvidencePaths.ForArtRun(run).Root,"session.json");
 static void Save(ArtValidationSession s){s.UpdatedAt=DateTime.UtcNow.ToString("o");File.WriteAllText(PathFor(s.RunId),JsonConvert.SerializeObject(s,Formatting.Indented));}
 public static ArtValidationSession Load(string run){var p=PathFor(run);if(!File.Exists(p))throw new ArgumentException("art_blocked:unknown_run");return JsonConvert.DeserializeObject<ArtValidationSession>(File.ReadAllText(p));}
 public static ArtValidationSession Start(string run,string profileId,IEnumerable<string> targets,string instance){var profile=ProfileRegistry.GetArt(profileId);var s=new ArtValidationSession{RunId=run,ProfileId=profileId,InstanceId=instance,Status="AwaitingLiveInspection"};foreach(var id in targets){ArtTargetRegistry.Get(id);s.TargetIds.Add(id);}if(s.TargetIds.Count==0&&profile.Mode!="regression")throw new ArgumentException("target_id required");Save(s);return s;}
 public static ArtValidationSession RecordInspection(string run,ArtInspectionResult result){var s=Load(run);s.HasInspection=true;s.HasBlockingIssue=result.Issues.Exists(x=>x.Blocking);if(s.HasPersistedChanges&&ArtPlayModeGenerationTracker.Current>s.PersistedPlayModeGeneration)s.HasPostPersistInspection=true;s.Status="AwaitingCaptureDecision";ArtLiveInspectionService.Write(run,result);Save(s);return s;}
 public static ArtValidationSession RecordCapture(string run,ArtCaptureRole role){var s=Load(run);var n=role.ToString().ToLowerInvariant();if(!s.RecordedRoles.Contains(n))s.RecordedRoles.Add(n);s.Status="EvidenceReady";Save(s);return s;}
 public static ArtValidationSession Complete(string run){var s=Load(run);var profile=ProfileRegistry.GetArt(s.ProfileId);bool ok;if(profile.Mode=="iteration")ok=s.RecordedRoles.Contains("before")&&s.RecordedRoles.Contains("after")&&s.HasPersistedChanges&&s.HasPostPersistInspection;else if(profile.Mode=="seal")ok=s.RecordedRoles.Contains("seal");else if(profile.Mode=="regression")ok=s.RecordedRoles.Contains("regression");else ok=s.RecordedRoles.Contains(s.HasBlockingIssue?"issue":"final");if(!s.HasInspection&&profile.Mode!="regression")throw new InvalidOperationException("art_blocked:inspection_required");if(!ok)throw new InvalidOperationException(profile.Mode=="iteration"?"art_blocked:playmode_reload_and_reinspect_required":"art_blocked:required_capture_missing");s.Status="Complete";s.AutomationStatus=s.HasBlockingIssue?"Failed":"Passed";Save(s);return s;}
}
}
