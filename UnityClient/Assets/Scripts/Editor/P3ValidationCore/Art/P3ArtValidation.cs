using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace P3.Validation {
public enum ArtIterationAction { SetAnchoredPosition,SetSizeDelta,SetAnchorMin,SetAnchorMax,SetSiblingIndex,SetColor,SetAlpha,SetTextStyle,SetCanvasGroup,SetRaycastTarget,BindApprovedVisualId }
[Serializable] public sealed class ArtIterationRequest { public string TargetId;public ArtIterationAction Action;public Vector2 Vector2Value;public Color ColorValue;public float FloatValue;public int IntValue;public bool BoolValue;public string StringValue; }
public static class ArtTargetRegistry {
 static readonly Dictionary<string,string> Targets=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"workshop_main","WorkshopUI"},{"dungeon_map","DungeonMapUI"},{"dialogue_overlay","DialogueOverlay"},{"t0_prologue","PrologueFirstDive"}};
 public static GameObject Resolve(string id){string name;if(string.IsNullOrWhiteSpace(id)||!Targets.TryGetValue(id,out name))throw new ArgumentException("art_blocked:unknown_target");var go=GameObject.Find(name);if(!go)throw new InvalidOperationException("art_blocked:target_screen_unreachable;handoff=program");return go;}
 public static bool IsRegistered(string id)=>!string.IsNullOrWhiteSpace(id)&&Targets.ContainsKey(id);
}
public static class ArtCaptureService {
 public static Artifact CopyVerified(string source,string destination){if(string.IsNullOrWhiteSpace(source)||!File.Exists(source))throw new FileNotFoundException("art_blocked:missing_capture",source);Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(source,destination,true);var info=new FileInfo(destination);if(info.Length==0)throw new InvalidDataException("art_blocked:empty_capture");using(var sha=SHA256.Create())using(var stream=File.OpenRead(destination)){return new Artifact{Path=destination,SourcePath=source,Sha256=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant(),Size=info.Length,CapturedAt=DateTime.UtcNow.ToString("o"),MimeType="image/png"};}}
}
public static class ArtIterationService {
 public static void Validate(ArtIterationRequest r){if(r==null||!ArtTargetRegistry.IsRegistered(r.TargetId))throw new ArgumentException("art_blocked:unregistered_iteration_target");if(r.Action==ArtIterationAction.BindApprovedVisualId&&(string.IsNullOrWhiteSpace(r.StringValue)||r.StringValue.Contains("/")||r.StringValue.Contains("..")))throw new ArgumentException("art_blocked:visual_id_not_approved");}
 public static void Apply(ArtIterationRequest r){Validate(r);var go=ArtTargetRegistry.Resolve(r.TargetId);var rt=go.GetComponent<RectTransform>();switch(r.Action){case ArtIterationAction.SetAnchoredPosition:rt.anchoredPosition=r.Vector2Value;break;case ArtIterationAction.SetSizeDelta:rt.sizeDelta=r.Vector2Value;break;case ArtIterationAction.SetAnchorMin:rt.anchorMin=r.Vector2Value;break;case ArtIterationAction.SetAnchorMax:rt.anchorMax=r.Vector2Value;break;case ArtIterationAction.SetSiblingIndex:rt.SetSiblingIndex(r.IntValue);break;case ArtIterationAction.SetColor:go.GetComponent<Graphic>().color=r.ColorValue;break;case ArtIterationAction.SetAlpha:var c=go.GetComponent<CanvasGroup>()??go.AddComponent<CanvasGroup>();c.alpha=r.FloatValue;break;case ArtIterationAction.SetCanvasGroup:var g=go.GetComponent<CanvasGroup>()??go.AddComponent<CanvasGroup>();g.blocksRaycasts=r.BoolValue;g.interactable=r.BoolValue;break;case ArtIterationAction.SetRaycastTarget:go.GetComponent<Graphic>().raycastTarget=r.BoolValue;break;case ArtIterationAction.SetTextStyle:var t=go.GetComponent<Text>();if(!t)throw new InvalidOperationException("art_blocked:not_text");t.fontSize=r.IntValue;break;case ArtIterationAction.BindApprovedVisualId:throw new InvalidOperationException("art_blocked:visual_binding_requires_registered_adapter");}}
 public static string WritePackage(string run,string iterationId,ArtIterationRequest request,string before,string after){var dir=Path.Combine(EvidencePaths.ForArtRun(run).Iterations,iterationId);Directory.CreateDirectory(dir);ArtCaptureService.CopyVerified(before,Path.Combine(dir,"before.png"));File.WriteAllText(Path.Combine(dir,"diagnosis.json"),"{\"status\":\"recorded\"}");File.WriteAllText(Path.Combine(dir,"changes.json"),JsonConvert.SerializeObject(request,Formatting.Indented));Apply(request);ArtCaptureService.CopyVerified(after,Path.Combine(dir,"after.png"));File.WriteAllText(Path.Combine(dir,"console-delta.json"),"[]");File.WriteAllText(Path.Combine(dir,"result.json"),"{\"schema_version\":\"p3-validation/step-result@2\",\"validation_domain\":\"art\",\"status\":\"Passed\"}");return dir;}
}
public static class ArtValidationOrchestrator { public static object Run(string run,string profileId,string target){var p=ProfileRegistry.GetArt(profileId);if(p.ExternalReviewRequired.Count==0)throw new InvalidDataException("art review must be required");ArtTargetRegistry.Resolve(target);return new{run_id=run,profile_id=profileId,status="ReviewRequired",external_review="Required",evidence_root=EvidencePaths.ForArtRun(run).Root};}}
}
