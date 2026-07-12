using System;
using Newtonsoft.Json;
using UnityEngine;

namespace P3.Validation {
[Serializable] public sealed class ArtIterationRequest {
 [JsonProperty("run_id")] public string RunId;
 [JsonProperty("iteration_id")] public string IterationId;
 [JsonProperty("target_id")] public string TargetId;
 [JsonProperty("component_path")] public string ComponentPath;
 [JsonProperty("visual_id")] public string VisualId;
 [JsonProperty("action")] public ArtIterationAction Action;
 [JsonProperty("vector2_value")] public Vector2 Vector2Value;
 [JsonProperty("color_value")] public Color ColorValue;
 [JsonProperty("float_value")] public float FloatValue;
 [JsonProperty("int_value")] public int IntValue;
 [JsonProperty("bool_value")] public bool BoolValue;
}
[Serializable] public sealed class ArtIterationResult { public string RunId,IterationId,TargetId,Mode,DurableSource,Status;public bool SatisfiesFormalValidation; }
[Serializable] public sealed class ArtPersistResult { public string TargetId,DurableSource;public bool Changed; }
public interface IArtPersistAdapter { string TargetId{get;} ArtPersistResult Apply(ArtIterationRequest request); }
}
