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
    public ArtAcceptanceDataSourceSummary DataSourceSummary = new ArtAcceptanceDataSourceSummary();
    public string PreviousRunID;
    public List<ArtAcceptanceCaptureRecord> Captures = new List<ArtAcceptanceCaptureRecord>();
    public List<string> Warnings = new List<string>();
    public List<string> Errors = new List<string>();
}

/// <summary>
/// 按数据来源分类的截图统计。
/// </summary>
[Serializable]
public class ArtAcceptanceDataSourceSummary {
    public int RealGameplay;
    public int AcceptancePreview;
    public int FormalV1Template;
    public int Total;
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
    /// <summary>
    /// 数据来源标记。取值:
    /// "real_gameplay" — 使用真实游戏状态驱动
    /// "acceptance_preview" — 使用验收专用构造 payload
    /// "formal_v1_template" — 使用 Formal V1 模板面板渲染
    /// </summary>
    public string DataSource;
    public List<string> ActiveControllers = new List<string>();
    public List<string> Warnings = new List<string>();
    public List<string> Errors = new List<string>();
}
