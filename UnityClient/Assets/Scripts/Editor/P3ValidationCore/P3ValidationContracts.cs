using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace P3.Validation {
[JsonConverter(typeof(StringEnumConverter))] public enum ValidationDomain { Program, Art, Release, Infrastructure }
[JsonConverter(typeof(StringEnumConverter))] public enum StepStatus { Passed, Failed, Blocked, Limited, Cancelled }
[Serializable] public sealed class Artifact { [JsonProperty("path")] public string Path; [JsonProperty("source_path")] public string SourcePath; [JsonProperty("sha256")] public string Sha256; [JsonProperty("size")] public long Size; [JsonProperty("captured_at")] public string CapturedAt; [JsonProperty("mime_type")] public string MimeType; }
[Serializable] public sealed class StepResult { [JsonProperty("schema_version")] public string SchemaVersion="p3-validation/step-result@2"; [JsonProperty("validation_domain")] public ValidationDomain ValidationDomain; [JsonProperty("run_id")] public string RunId; [JsonProperty("profile_id")] public string ProfileId; [JsonProperty("step_id")] public string StepId; [JsonProperty("required")] public bool Required; [JsonProperty("status")] public StepStatus Status; [JsonProperty("code")] public string Code; [JsonProperty("artifacts")] public List<Artifact> Artifacts=new List<Artifact>(); }
[Serializable] public sealed class Profile { [JsonProperty("id")] public string Id; [JsonProperty("version")] public string Version; [JsonProperty("validation_domain")] public ValidationDomain ValidationDomain; [JsonProperty("static_steps")] public List<string> StaticSteps=new List<string>(); [JsonProperty("required_steps")] public List<string> RequiredSteps=new List<string>(); [JsonProperty("smoke_set")] public string SmokeSet; [JsonProperty("mode")] public string Mode; }
}
