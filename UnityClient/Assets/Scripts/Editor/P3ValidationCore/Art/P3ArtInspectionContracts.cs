using System;
using System.Collections.Generic;

namespace P3.Validation {
public enum ArtIssueCategory { Layout,Hierarchy,Typography,Color,SpriteBinding,VisualId,Mask,Raycast,RuntimeState,ProgramReachability,SemanticVisual,CommercialPolish }
[Serializable] public sealed class ArtInspectionIssue { public string IssueId,TargetId,Severity,Observation;public ArtIssueCategory Category;public List<string> SuspectedComponents=new List<string>();public bool Blocking; }
[Serializable] public sealed class ArtBindingEvidence { public string TargetId,VisualId,ComponentPath,AssetPath,AssetGuid,Status,Observation; }
[Serializable] public sealed class ArtBindingResolution { public string Status;public List<ArtBindingEvidence> Bindings=new List<ArtBindingEvidence>();public List<ArtInspectionIssue> Issues=new List<ArtInspectionIssue>(); }
[Serializable] public sealed class ArtValidationReview { public string Decision,Reviewer,ObservedAt,Notes; }
[Serializable] public sealed class ArtUiNodeSnapshot { public string Path;public bool Active,HasRectTransform,HasGraphic,RaycastTarget;public float Alpha;public string SpriteName,VisualId,SpriteAssetPath,SpriteGuid;public float X,Y,Width,Height; }
[Serializable] public sealed class ArtInspectionResult { public string TargetId,ScreenTag,CapturedAt,BindingStatus;public bool Reachable;public List<ArtUiNodeSnapshot> Nodes=new List<ArtUiNodeSnapshot>();public List<ArtInspectionIssue> Issues=new List<ArtInspectionIssue>();public List<ArtBindingEvidence> Bindings=new List<ArtBindingEvidence>(); }
}
