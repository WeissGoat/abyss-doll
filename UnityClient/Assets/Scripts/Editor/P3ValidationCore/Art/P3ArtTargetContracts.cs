using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace P3.Validation {
public enum ArtCaptureRole { Issue, Before, After, Final, Seal, Regression }
[Serializable] public sealed class ArtTargetDefinition {
 [JsonProperty("target_id")] public string TargetId;
 [JsonProperty("screen_tag")] public string ScreenTag;
 [JsonProperty("expected_roots")] public List<string> ExpectedRoots=new List<string>();
 [JsonProperty("required_visual_ids")] public List<string> RequiredVisualIds=new List<string>();
 [JsonProperty("reference_width")] public int ReferenceWidth=1920;
 [JsonProperty("reference_height")] public int ReferenceHeight=1080;
 [JsonProperty("persist_mode")] public string PersistMode;
 [JsonProperty("prefab_asset_path")] public string PrefabAssetPath;
}
}
