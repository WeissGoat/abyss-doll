using System;
using System.IO;
using UnityEngine;

public static class P3T0CaptureAdapter {
    public static void Start(){ T0ValidationFinalCaptureRunner.StartCapture(); }
    public static P3ValidationStepResult Collect(string runId,string profileId,DateTime startedAt){ var source=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/T0Validation/t0_val_01_final_capture_report.txt")); if(!File.Exists(source)||File.GetLastWriteTimeUtc(source)<startedAt) return P3ValidationStepResult.Limited(runId,profileId,"t0_capture",true,"validation_limited:T0CaptureReportPending","T0 capture report not available for this run"); var text=File.ReadAllText(source); var r=text.IndexOf("semantic_failed=True",StringComparison.OrdinalIgnoreCase)>=0?P3ValidationStepResult.Failed(runId,profileId,"t0_capture",true,"t0_semantic_failed","T0 semantic capture failed"):P3ValidationStepResult.Passed(runId,profileId,"t0_capture",true); r.Artifacts.Add(P3ValidationEvidenceWriter.Copy(source,P3ValidationEvidencePaths.ForRun(runId).SourceReports,"t0_capture/report.txt")); P3ValidationEvidenceWriter.Write(r); return r; }
}
