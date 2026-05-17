using System;
using System.Collections.Generic;

/// <summary>
/// 美术自动验收报告主结构及其关联数据类。
/// 由 ArtAcceptanceRunner 在验收流程结束时序列化为 report.json。
/// </summary>
[Serializable]
public class ArtAcceptanceReport {
    public string SchemaVersion;
    public string RunID;
    public string Project;
    public string Mode;
    public string Status;
    public bool IsRunning;
    public string StartedAt;
    public string FinishedAt;
    public string OutputRoot;
    public ArtAcceptanceResolution ReferenceResolution = new ArtAcceptanceResolution();
    public ArtAcceptanceResolution ActualResolution = new ArtAcceptanceResolution();
    public ArtAcceptanceCanvasScalerInfo CanvasScaler = new ArtAcceptanceCanvasScalerInfo();
    public ArtAcceptanceRegistrySummary Registry = new ArtAcceptanceRegistrySummary();
    public List<ArtAcceptanceCaptureRecord> Captures = new List<ArtAcceptanceCaptureRecord>();
    public List<string> Warnings = new List<string>();
    public List<string> Errors = new List<string>();
}

[Serializable]
public class ArtAcceptanceResolution {
    public int Width;
    public int Height;
}

[Serializable]
public class ArtAcceptanceCanvasScalerInfo {
    public bool Found;
    public string Mode;
    public string ReferenceResolution;
    public float MatchWidthOrHeight;
}

[Serializable]
public class ArtAcceptanceRegistrySummary {
    public bool RegistryFound;
    public bool MissingSpriteFound;
    public string MissingSpriteVisualID;
    public int EntryCount;
    public List<string> MissingRequiredVisualIDs = new List<string>();
}

[Serializable]
public class ArtAcceptanceCaptureRecord {
    public int Index;
    public string ScreenTag;
    public string File;
    public string CapturedAt;
    public string Status;
    public string Resolution;
    public List<string> ActiveControllers = new List<string>();
    public List<string> Warnings = new List<string>();
    public List<string> Errors = new List<string>();
}
