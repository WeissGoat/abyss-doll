using System;
using System.IO;
using UnityEngine;

public sealed class P3ValidationEvidencePaths {
    public string Root, Steps, Unity, Screenshots, SourceReports;
    public static P3ValidationEvidencePaths ForRun(string runId) {
        if(string.IsNullOrWhiteSpace(runId) || runId.IndexOfAny(new[]{'/', '\\'})>=0 || runId.Contains("..")) throw new ArgumentException("Invalid RunID");
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/P3Validation/runs",runId));
        var allowed=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/P3Validation/runs"))+Path.DirectorySeparatorChar;
        if(!root.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Evidence path escaped root");
        var p=new P3ValidationEvidencePaths { Root=root,Steps=Path.Combine(root,"steps"),Unity=Path.Combine(root,"unity"),Screenshots=Path.Combine(root,"screenshots"),SourceReports=Path.Combine(root,"source_reports") };
        foreach(var d in new[]{p.Root,p.Steps,p.Unity,p.Screenshots,p.SourceReports}) Directory.CreateDirectory(d);
        return p;
    }
    public string StepResult(string stepId) { if(string.IsNullOrWhiteSpace(stepId)||stepId.Contains("..")||stepId.IndexOfAny(new[]{'/', '\\'})>=0) throw new ArgumentException("Invalid step ID"); var d=Path.Combine(Steps,stepId); Directory.CreateDirectory(d); return Path.Combine(d,"result.json"); }
}
