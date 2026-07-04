using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class DollPuppetSchemas {
    public const string RigV1 = "p3.doll_puppet.rig.v1";
    public const string MeshV1 = "p3.doll_puppet.mesh.v1";
    public const string MotionV1 = "p3.doll_puppet.motion.v1";
    public const string ExpressionV1 = "p3.doll_puppet.expression.v1";
}

public sealed class DollPuppetValidationResult {
    private readonly List<string> errors = new List<string>();
    private readonly List<string> warnings = new List<string>();

    public bool IsValid => errors.Count == 0;
    public IReadOnlyList<string> Errors => errors;
    public IReadOnlyList<string> Warnings => warnings;

    public void AddError(string message) {
        if (!string.IsNullOrEmpty(message)) {
            errors.Add(message);
        }
    }

    public void AddWarning(string message) {
        if (!string.IsNullOrEmpty(message)) {
            warnings.Add(message);
        }
    }
}

[Serializable]
public sealed class DollPuppetRigDocument {
    [JsonProperty("schema")] public string Schema;
    [JsonProperty("dollId")] public string DollID;
    [JsonProperty("visualId")] public string VisualID;
    [JsonProperty("fallbackSpriteId")] public string FallbackSpriteID;
    [JsonProperty("canvas")] public DollPuppetCanvas Canvas;
    [JsonProperty("layers")] public List<DollPuppetLayer> Layers = new List<DollPuppetLayer>();
    [JsonProperty("parameters")] public List<DollPuppetParameter> Parameters = new List<DollPuppetParameter>();
}

[Serializable]
public sealed class DollPuppetCanvas {
    [JsonProperty("width")] public int Width;
    [JsonProperty("height")] public int Height;
    [JsonProperty("pixelsPerUnit")] public float PixelsPerUnit;
}

[Serializable]
public sealed class DollPuppetLayer {
    [JsonProperty("id")] public string ID;
    [JsonProperty("path")] public string Path;
    [JsonProperty("parent")] public string Parent;
    [JsonProperty("pivot")] public List<float> Pivot = new List<float>();
    [JsonProperty("drawOrder")] public int DrawOrder;
    [JsonProperty("deformMode")] public string DeformMode;
}

[Serializable]
public sealed class DollPuppetParameter {
    [JsonProperty("id")] public string ID;
    [JsonProperty("type")] public string Type;
    [JsonProperty("min")] public float Min;
    [JsonProperty("max")] public float Max;
    [JsonProperty("default")] public float Default;
}

[Serializable]
public sealed class DollPuppetMeshDocument {
    [JsonProperty("schema")] public string Schema;
    [JsonProperty("meshes")] public List<DollPuppetMesh> Meshes = new List<DollPuppetMesh>();
}

[Serializable]
public sealed class DollPuppetMesh {
    [JsonProperty("layerId")] public string LayerID;
    [JsonProperty("vertices")] public List<List<float>> Vertices = new List<List<float>>();
    [JsonProperty("triangles")] public List<int> Triangles = new List<int>();
    [JsonProperty("weights")] public List<float> Weights = new List<float>();
}

[Serializable]
public sealed class DollPuppetMotionDocument {
    [JsonProperty("schema")] public string Schema;
    [JsonProperty("motionId")] public string MotionID;
    [JsonProperty("loop")] public bool Loop;
    [JsonProperty("duration")] public float Duration;
    [JsonProperty("tracks")] public List<DollPuppetMotionTrack> Tracks = new List<DollPuppetMotionTrack>();
}

[Serializable]
public sealed class DollPuppetMotionTrack {
    [JsonProperty("target")] public string Target;
    [JsonProperty("property")] public string Property;
    [JsonProperty("keys")] public List<List<float>> Keys = new List<List<float>>();
}

[Serializable]
public sealed class DollPuppetExpressionDocument {
    [JsonProperty("schema")] public string Schema;
    [JsonProperty("expressionId")] public string ExpressionID;
    [JsonProperty("overrides")] public List<DollPuppetExpressionOverride> Overrides = new List<DollPuppetExpressionOverride>();
}

[Serializable]
public sealed class DollPuppetExpressionOverride {
    [JsonProperty("target")] public string Target;
    [JsonProperty("property")] public string Property;
    [JsonProperty("value")] public JToken Value;
}

public static class DollPuppetJsonValidator {
    private static readonly HashSet<string> AllowedDeformModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "transform",
        "material",
        "sprite_skin"
    };

    private static readonly HashSet<string> AllowedMotionProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "localX",
        "localY",
        "rotationZ",
        "scaleX",
        "scaleY",
        "alpha",
        "material.emission",
        "value"
    };

    private static readonly HashSet<string> ForbiddenGameplayFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "hp",
        "san",
        "gold",
        "coins",
        "inventory",
        "backpack",
        "bond",
        "affection",
        "friendship"
    };

    public static DollPuppetValidationResult ValidateRigJson(string json, out DollPuppetRigDocument rig) {
        DollPuppetValidationResult result = new DollPuppetValidationResult();
        rig = Deserialize<DollPuppetRigDocument>(json, result);
        ValidateNoGameplayState(json, result);
        ValidateRig(rig, result);
        return result;
    }

    public static DollPuppetValidationResult ValidateMeshJson(string json, out DollPuppetMeshDocument mesh) {
        DollPuppetValidationResult result = new DollPuppetValidationResult();
        mesh = Deserialize<DollPuppetMeshDocument>(json, result);
        ValidateNoGameplayState(json, result);
        ValidateMesh(mesh, result);
        return result;
    }

    public static DollPuppetValidationResult ValidateMotionJson(string json, out DollPuppetMotionDocument motion) {
        DollPuppetValidationResult result = new DollPuppetValidationResult();
        motion = Deserialize<DollPuppetMotionDocument>(json, result);
        ValidateNoGameplayState(json, result);
        ValidateMotion(motion, result);
        return result;
    }

    public static DollPuppetValidationResult ValidateExpressionJson(string json, out DollPuppetExpressionDocument expression) {
        DollPuppetValidationResult result = new DollPuppetValidationResult();
        expression = Deserialize<DollPuppetExpressionDocument>(json, result);
        ValidateNoGameplayState(json, result);
        ValidateExpression(expression, result);
        return result;
    }

    private static T Deserialize<T>(string json, DollPuppetValidationResult result) where T : class {
        if (string.IsNullOrWhiteSpace(json)) {
            result.AddError("json_empty");
            return null;
        }

        try {
            return JsonConvert.DeserializeObject<T>(json);
        } catch (Exception ex) {
            result.AddError($"json_parse_failed:{ex.Message}");
            return null;
        }
    }

    private static void ValidateRig(DollPuppetRigDocument rig, DollPuppetValidationResult result) {
        if (rig == null) {
            return;
        }

        RequireSchema(rig.Schema, DollPuppetSchemas.RigV1, "rig.schema", result);
        RequireString(rig.DollID, "rig.dollId", result);
        RequireString(rig.VisualID, "rig.visualId", result);
        RequireString(rig.FallbackSpriteID, "rig.fallbackSpriteId", result);

        if (rig.Canvas == null) {
            result.AddError("rig.canvas_missing");
        } else {
            if (rig.Canvas.Width <= 0) {
                result.AddError("rig.canvas.width_must_be_positive");
            }

            if (rig.Canvas.Height <= 0) {
                result.AddError("rig.canvas.height_must_be_positive");
            }

            if (rig.Canvas.PixelsPerUnit <= 0f) {
                result.AddError("rig.canvas.pixelsPerUnit_must_be_positive");
            }
        }

        if (rig.Layers == null || rig.Layers.Count == 0) {
            result.AddError("rig.layers_empty");
        } else {
            HashSet<string> layerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rig.Layers.Count; i++) {
                DollPuppetLayer layer = rig.Layers[i];
                string prefix = $"rig.layers[{i}]";
                if (layer == null) {
                    result.AddError($"{prefix}_missing");
                    continue;
                }

                RequireString(layer.ID, $"{prefix}.id", result);
                RequireString(layer.Path, $"{prefix}.path", result);
                RequireString(layer.Parent, $"{prefix}.parent", result);
                if (!string.IsNullOrEmpty(layer.ID) && !layerIds.Add(layer.ID)) {
                    result.AddError($"{prefix}.id_duplicate:{layer.ID}");
                }

                if (layer.Pivot == null || layer.Pivot.Count != 2) {
                    result.AddError($"{prefix}.pivot_must_have_two_values");
                }

                if (string.IsNullOrEmpty(layer.DeformMode) || !AllowedDeformModes.Contains(layer.DeformMode)) {
                    result.AddError($"{prefix}.deformMode_invalid:{layer.DeformMode}");
                }
            }
        }

        if (rig.Parameters == null || rig.Parameters.Count == 0) {
            result.AddError("rig.parameters_empty");
        } else {
            HashSet<string> parameterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < rig.Parameters.Count; i++) {
                DollPuppetParameter parameter = rig.Parameters[i];
                string prefix = $"rig.parameters[{i}]";
                if (parameter == null) {
                    result.AddError($"{prefix}_missing");
                    continue;
                }

                RequireString(parameter.ID, $"{prefix}.id", result);
                RequireString(parameter.Type, $"{prefix}.type", result);
                if (!string.IsNullOrEmpty(parameter.ID) && !parameterIds.Add(parameter.ID)) {
                    result.AddError($"{prefix}.id_duplicate:{parameter.ID}");
                }

                if (parameter.Max <= parameter.Min) {
                    result.AddError($"{prefix}.range_invalid");
                }

                if (parameter.Default < parameter.Min || parameter.Default > parameter.Max) {
                    result.AddError($"{prefix}.default_out_of_range");
                }
            }
        }
    }

    private static void ValidateMesh(DollPuppetMeshDocument mesh, DollPuppetValidationResult result) {
        if (mesh == null) {
            return;
        }

        RequireSchema(mesh.Schema, DollPuppetSchemas.MeshV1, "mesh.schema", result);
        if (mesh.Meshes == null) {
            result.AddError("mesh.meshes_missing");
            return;
        }

        for (int i = 0; i < mesh.Meshes.Count; i++) {
            DollPuppetMesh entry = mesh.Meshes[i];
            string prefix = $"mesh.meshes[{i}]";
            if (entry == null) {
                result.AddError($"{prefix}_missing");
                continue;
            }

            RequireString(entry.LayerID, $"{prefix}.layerId", result);
            if (entry.Vertices == null || entry.Vertices.Count < 3) {
                result.AddError($"{prefix}.vertices_need_at_least_three");
            } else {
                for (int v = 0; v < entry.Vertices.Count; v++) {
                    if (entry.Vertices[v] == null || entry.Vertices[v].Count != 2) {
                        result.AddError($"{prefix}.vertices[{v}]_must_have_two_values");
                    }
                }
            }

            if (entry.Triangles == null || entry.Triangles.Count == 0 || entry.Triangles.Count % 3 != 0) {
                result.AddError($"{prefix}.triangles_must_be_triplets");
            }
        }
    }

    private static void ValidateMotion(DollPuppetMotionDocument motion, DollPuppetValidationResult result) {
        if (motion == null) {
            return;
        }

        RequireSchema(motion.Schema, DollPuppetSchemas.MotionV1, "motion.schema", result);
        RequireString(motion.MotionID, "motion.motionId", result);
        if (motion.Duration <= 0f) {
            result.AddError("motion.duration_must_be_positive");
        }

        if (motion.Tracks == null || motion.Tracks.Count == 0) {
            result.AddError("motion.tracks_empty");
        } else {
            for (int i = 0; i < motion.Tracks.Count; i++) {
                DollPuppetMotionTrack track = motion.Tracks[i];
                string prefix = $"motion.tracks[{i}]";
                if (track == null) {
                    result.AddError($"{prefix}_missing");
                    continue;
                }

                RequireString(track.Target, $"{prefix}.target", result);
                RequireString(track.Property, $"{prefix}.property", result);
                if (!string.IsNullOrEmpty(track.Property) && !AllowedMotionProperties.Contains(track.Property)) {
                    result.AddError($"{prefix}.property_invalid:{track.Property}");
                }

                ValidateKeyframes(track.Keys, prefix, motion.Duration, result);
            }
        }
    }

    private static void ValidateExpression(DollPuppetExpressionDocument expression, DollPuppetValidationResult result) {
        if (expression == null) {
            return;
        }

        RequireSchema(expression.Schema, DollPuppetSchemas.ExpressionV1, "expression.schema", result);
        RequireString(expression.ExpressionID, "expression.expressionId", result);
        if (expression.Overrides == null || expression.Overrides.Count == 0) {
            result.AddError("expression.overrides_empty");
        } else {
            for (int i = 0; i < expression.Overrides.Count; i++) {
                DollPuppetExpressionOverride entry = expression.Overrides[i];
                string prefix = $"expression.overrides[{i}]";
                if (entry == null) {
                    result.AddError($"{prefix}_missing");
                    continue;
                }

                RequireString(entry.Target, $"{prefix}.target", result);
                RequireString(entry.Property, $"{prefix}.property", result);
                if (entry.Value == null || entry.Value.Type == JTokenType.Null) {
                    result.AddError($"{prefix}.value_missing");
                }
            }
        }
    }

    private static void ValidateKeyframes(List<List<float>> keys, string prefix, float duration, DollPuppetValidationResult result) {
        if (keys == null || keys.Count < 2) {
            result.AddError($"{prefix}.keys_need_at_least_two");
            return;
        }

        float previousTime = -1f;
        for (int i = 0; i < keys.Count; i++) {
            List<float> key = keys[i];
            if (key == null || key.Count != 2) {
                result.AddError($"{prefix}.keys[{i}]_must_have_time_and_value");
                continue;
            }

            float time = key[0];
            if (time < 0f || time > duration) {
                result.AddError($"{prefix}.keys[{i}]_time_out_of_duration");
            }

            if (time < previousTime) {
                result.AddError($"{prefix}.keys[{i}]_time_not_sorted");
            }

            previousTime = time;
        }
    }

    private static void ValidateNoGameplayState(string json, DollPuppetValidationResult result) {
        if (string.IsNullOrWhiteSpace(json)) {
            return;
        }

        try {
            JToken root = JToken.Parse(json);
            ValidateNoGameplayState(root, string.Empty, result);
        } catch (Exception) {
            // Parse errors are reported by Deserialize; avoid duplicate noise here.
        }
    }

    private static void ValidateNoGameplayState(JToken token, string path, DollPuppetValidationResult result) {
        if (token == null) {
            return;
        }

        JObject obj = token as JObject;
        if (obj != null) {
            foreach (JProperty property in obj.Properties()) {
                string nextPath = string.IsNullOrEmpty(path) ? property.Name : $"{path}.{property.Name}";
                if (ForbiddenGameplayFields.Contains(property.Name)) {
                    result.AddError($"gameplay_state_field_forbidden:{nextPath}");
                }

                ValidateNoGameplayState(property.Value, nextPath, result);
            }

            return;
        }

        JArray array = token as JArray;
        if (array != null) {
            for (int i = 0; i < array.Count; i++) {
                ValidateNoGameplayState(array[i], $"{path}[{i}]", result);
            }
        }
    }

    private static void RequireSchema(string actual, string expected, string field, DollPuppetValidationResult result) {
        if (!string.Equals(actual, expected, StringComparison.Ordinal)) {
            result.AddError($"{field}_invalid:{actual}");
        }
    }

    private static void RequireString(string value, string field, DollPuppetValidationResult result) {
        if (string.IsNullOrWhiteSpace(value)) {
            result.AddError($"{field}_missing");
        }
    }
}
