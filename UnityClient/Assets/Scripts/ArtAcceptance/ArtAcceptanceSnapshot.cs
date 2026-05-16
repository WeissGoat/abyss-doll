using System;
using System.Collections.Generic;

/// <summary>
/// 运行时 UI 层级快照数据结构。
/// 由 ArtAcceptanceRunner 在每个截图点构建，最终序列化为 ui_snapshot.json。
/// 按 capture 分组，记录 Canvas 层级、元素属性、风险标记等客观状态。
/// </summary>
[Serializable]
public class ArtAcceptanceUiSnapshotReport {
    public string SchemaVersion;
    public string RunID;
    public List<ArtAcceptanceUiCaptureSnapshot> Captures = new List<ArtAcceptanceUiCaptureSnapshot>();
}

[Serializable]
public class ArtAcceptanceUiCaptureSnapshot {
    public string ScreenTag;
    public List<ArtAcceptanceCanvasSnapshot> Canvases = new List<ArtAcceptanceCanvasSnapshot>();
}

[Serializable]
public class ArtAcceptanceCanvasSnapshot {
    public string Name;
    public string Path;
    public string RenderMode;
    public int SortingOrder;
    public List<ArtAcceptanceUiElementSnapshot> Elements = new List<ArtAcceptanceUiElementSnapshot>();
}

[Serializable]
public class ArtAcceptanceUiElementSnapshot {
    public string Name;
    public string Path;
    public bool Active;
    public string AnchoredPosition;
    public string SizeDelta;
    public string AnchorMin;
    public string AnchorMax;
    public string Pivot;
    public List<string> ComponentTypes = new List<string>();
    public ArtAcceptanceImageSnapshot Image = new ArtAcceptanceImageSnapshot();
    public ArtAcceptanceButtonSnapshot Button = new ArtAcceptanceButtonSnapshot();
    public ArtAcceptanceTextSnapshot Text = new ArtAcceptanceTextSnapshot();
    public ArtAcceptanceCanvasGroupSnapshot CanvasGroup = new ArtAcceptanceCanvasGroupSnapshot();
    public List<string> Risks = new List<string>();
}

[Serializable]
public class ArtAcceptanceImageSnapshot {
    public bool Found;
    public bool Enabled;
    public string SpriteName;
    public string Type;
    public bool RaycastTarget;
    public bool PreserveAspect;
}

[Serializable]
public class ArtAcceptanceButtonSnapshot {
    public bool Found;
    public bool Interactable;
    public bool HasTargetGraphic;
}

[Serializable]
public class ArtAcceptanceTextSnapshot {
    public bool Found;
    public bool Enabled;
    public int TextLength;
    public bool RaycastTarget;
    public int FontSize;
}

[Serializable]
public class ArtAcceptanceCanvasGroupSnapshot {
    public bool Found;
    public float Alpha;
    public bool Interactable;
    public bool BlocksRaycasts;
}
