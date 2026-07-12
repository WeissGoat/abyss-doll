using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class P3ArtAcceptanceAdapter {
    public static void Start(){ ArtAcceptanceRunner.BeginAutomatedRun(true); }
    public static P3ValidationStepResult Collect(string runId,string profileId,DateTime startedAt){ var source=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/ArtAcceptance/latest/report.json")); if(!File.Exists(source)||File.GetLastWriteTimeUtc(source)<startedAt) return P3ValidationStepResult.Limited(runId,profileId,"art_acceptance",true,"validation_limited:ArtAcceptanceReportPending","Report not available for this run"); var json=JObject.Parse(File.ReadAllText(source)); if(json.Value<bool?>("IsRunning")==true) return P3ValidationStepResult.Limited(runId,profileId,"art_acceptance",true,"validation_limited:ArtAcceptanceRunning","ArtAcceptance is still running"); var status=json.Value<string>("Status")??json.Value<string>("status"); var r=string.Equals(status,"PASSED",StringComparison.OrdinalIgnoreCase)?P3ValidationStepResult.Passed(runId,profileId,"art_acceptance",true):P3ValidationStepResult.Failed(runId,profileId,"art_acceptance",true,"art_acceptance_failed",status??"Unknown status"); r.Artifacts.Add(P3ValidationEvidenceWriter.Copy(source,P3ValidationEvidencePaths.ForRun(runId).SourceReports,"art_acceptance/report.json")); P3ValidationEvidenceWriter.Write(r); return r; }
}
