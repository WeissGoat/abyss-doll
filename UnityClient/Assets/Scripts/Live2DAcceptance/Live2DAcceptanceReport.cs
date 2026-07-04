using System;
using System.Collections.Generic;

[Serializable]
public class Live2DAcceptanceReport {
    public string SchemaVersion;
    public string RunID;
    public string Project;
    public string Mode;
    public string Status;
    public bool IsRunning;
    public string StartedAt;
    public string FinishedAt;
    public string OutputRoot;
    public Live2DAcceptanceVisualSummary Visual = new Live2DAcceptanceVisualSummary();
    public Live2DAcceptanceBoundarySummary Boundaries = new Live2DAcceptanceBoundarySummary();
    public List<Live2DAcceptanceCaptureRecord> Captures = new List<Live2DAcceptanceCaptureRecord>();
    public List<string> Warnings = new List<string>();
    public List<string> Errors = new List<string>();
}

[Serializable]
public class Live2DAcceptanceVisualSummary {
    public string DollID;
    public string DynamicVisualID;
    public string FallbackSpriteID;
    public string VisualMode;
    public string FallbackReason;
    public bool RuntimePackageAvailable;
    public bool DynamicPrefabFound;
    public bool FallbackSpriteFound;
    public string PresenterMode;
}

[Serializable]
public class Live2DAcceptanceBoundarySummary {
    public bool FormalV2ArtAcceptanceTouched;
    public bool ScreenLayoutsTouched;
    public bool ApprovedAssetsTouched;
    public bool PackageManifestTouched;
    public string Notes;
}

[Serializable]
public class Live2DAcceptanceCaptureRecord {
    public int Index;
    public string StepID;
    public string MotionID;
    public string ExpressionID;
    public string File;
    public string CapturedAt;
    public string Status;
    public string Resolution;
    public bool DynamicFrameProven;
    public bool FallbackVisible;
    public List<string> Warnings = new List<string>();
    public List<string> Errors = new List<string>();
}
