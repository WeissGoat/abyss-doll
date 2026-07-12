using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace P3.Validation {
public static class ArtLiveInspectionService {
 static string BuildPath(Transform t,Transform root){if(t==root)return root.name;var p=t.name;while(t.parent&&t.parent!=root){t=t.parent;p=t.name+"/"+p;}return root.name+"/"+p;}
 public static ArtInspectionResult Inspect(string targetId,int maxNodes=200){
  if(maxNodes<1||maxNodes>500)throw new ArgumentOutOfRangeException("maxNodes");var def=ArtTargetRegistry.Get(targetId);var result=new ArtInspectionResult{TargetId=targetId,ScreenTag=def.ScreenTag,CapturedAt=DateTime.UtcNow.ToString("o")};GameObject root;
  try{root=ArtTargetRegistry.ResolveVisibleRoot(targetId);result.Reachable=true;}catch{result.Issues.Add(new ArtInspectionIssue{IssueId="reachability",TargetId=targetId,Category=ArtIssueCategory.ProgramReachability,Severity="P0",Observation="Registered target root is not visible.",Blocking=true});return result;}
  var transforms=root.GetComponentsInChildren<Transform>(true);for(int i=0;i<transforms.Length&&result.Nodes.Count<maxNodes;i++){var go=transforms[i].gameObject;var rt=go.GetComponent<RectTransform>();var graphic=go.GetComponent<Graphic>();var cg=go.GetComponent<CanvasGroup>();var image=go.GetComponent<Image>();var n=new ArtUiNodeSnapshot{Path=BuildPath(transforms[i],root.transform),Active=go.activeInHierarchy,HasRectTransform=rt,HasGraphic=graphic,RaycastTarget=graphic&&graphic.raycastTarget,Alpha=cg?cg.alpha:(graphic?graphic.color.a:1),SpriteName=image&&image.sprite?image.sprite.name:null};if(rt){n.X=rt.anchoredPosition.x;n.Y=rt.anchoredPosition.y;n.Width=rt.rect.width;n.Height=rt.rect.height;if(float.IsNaN(n.X)||float.IsInfinity(n.X)||float.IsNaN(n.Width)||float.IsInfinity(n.Width))result.Issues.Add(new ArtInspectionIssue{IssueId="invalid_rect_"+i,TargetId=targetId,Category=ArtIssueCategory.Layout,Severity="P0",Observation="RectTransform contains NaN or infinity.",Blocking=true});}if(image&&go.activeInHierarchy&&!image.sprite)result.Issues.Add(new ArtInspectionIssue{IssueId="missing_sprite_"+i,TargetId=targetId,Category=ArtIssueCategory.SpriteBinding,Severity="P1",Observation="Visible Image has no sprite.",Blocking=true,SuspectedComponents={n.Path}});result.Nodes.Add(n);}
  var scaler=root.GetComponentInParent<Canvas>()?.GetComponent<CanvasScaler>();if(scaler&&(scaler.uiScaleMode!=CanvasScaler.ScaleMode.ScaleWithScreenSize||scaler.referenceResolution!=new Vector2(1920,1080)))result.Issues.Add(new ArtInspectionIssue{IssueId="canvas_scaler",TargetId=targetId,Category=ArtIssueCategory.Layout,Severity="P1",Observation="CanvasScaler is not 1920x1080 ScaleWithScreenSize.",Blocking=true});
  return result;
 }
 public static string Write(string run,ArtInspectionResult result){var dir=Path.Combine(EvidencePaths.ForArtRun(run).Root,"targets",result.TargetId);Directory.CreateDirectory(dir);var p=Path.Combine(dir,"components.json");File.WriteAllText(p,JsonConvert.SerializeObject(result,Formatting.Indented));return p;}
}
}
