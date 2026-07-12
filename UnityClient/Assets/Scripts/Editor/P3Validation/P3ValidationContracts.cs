using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum P3ValidationStepStatus { Passed, Failed, Blocked, Limited, Cancelled }

[Serializable]
public sealed class P3ValidationArtifact {
    [JsonProperty("path")] public string Path;
    [JsonProperty("source_path")] public string SourcePath;
    [JsonProperty("sha256")] public string Sha256;
    [JsonProperty("size")] public long Size;
    [JsonProperty("captured_at")] public string CapturedAt;
    [JsonProperty("mime_type")] public string MimeType;
}

[Serializable]
public sealed class P3ValidationStepResult {
    [JsonProperty("schema_version")] public string SchemaVersion = "p3-validation/step-result@1";
    [JsonProperty("run_id")] public string RunId;
    [JsonProperty("profile_id")] public string ProfileId;
    [JsonProperty("step_id")] public string StepId;
    [JsonProperty("required")] public bool Required;
    [JsonProperty("status")] public P3ValidationStepStatus Status;
    [JsonProperty("code")] public string Code;
    [JsonProperty("limitation_code")] public string LimitationCode;
    [JsonProperty("message")] public string Message;
    [JsonProperty("artifacts")] public List<P3ValidationArtifact> Artifacts = new List<P3ValidationArtifact>();
    public static P3ValidationStepResult Passed(string runId,string profileId,string stepId,bool required) => New(runId,profileId,stepId,required,P3ValidationStepStatus.Passed,null,null);
    public static P3ValidationStepResult Failed(string runId,string profileId,string stepId,bool required,string code,string message) => New(runId,profileId,stepId,required,P3ValidationStepStatus.Failed,code,message);
    public static P3ValidationStepResult Blocked(string runId,string profileId,string stepId,bool required,string code,string message) { var r=New(runId,profileId,stepId,required,P3ValidationStepStatus.Blocked,null,message); r.LimitationCode=code; return r; }
    public static P3ValidationStepResult Limited(string runId,string profileId,string stepId,bool required,string code,string message) { var r=New(runId,profileId,stepId,required,P3ValidationStepStatus.Limited,null,message); r.LimitationCode=code; return r; }
    private static P3ValidationStepResult New(string runId,string profileId,string stepId,bool required,P3ValidationStepStatus status,string code,string message) => new P3ValidationStepResult { RunId=runId,ProfileId=profileId,StepId=stepId,Required=required,Status=status,Code=code,Message=message };
}

[Serializable]
public sealed class P3ValidationProfile {
    [JsonProperty("id")] public string Id;
    [JsonProperty("version")] public string Version;
    [JsonProperty("static_steps")] public List<string> StaticSteps;
    [JsonProperty("unity_profile_id")] public string UnityProfileId;
    [JsonProperty("required_evidence")] public List<string> RequiredEvidence;
    [JsonProperty("timeout_seconds")] public int TimeoutSeconds;
    [JsonProperty("strict_warnings")] public bool StrictWarnings;
    [JsonProperty("editor_control")] public string EditorControl;
    [JsonProperty("external_review_required")] public List<string> ExternalReviewRequired;
    [JsonProperty("smoke_set")] public string SmokeSet;
}

[Serializable]
public sealed class P3ValidationJobState {
    public string RunId, ProfileId, InstanceId, CurrentStep, EvidenceRoot, SourceRunnerId, LastError, RestoreAction, StartedAt, UpdatedAt, Status;
    public float Progress;
    public bool OriginalPlayMode;
}
