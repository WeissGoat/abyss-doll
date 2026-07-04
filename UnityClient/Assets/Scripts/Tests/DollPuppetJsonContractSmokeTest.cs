using UnityEngine;

public static class DollPuppetJsonContractSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Doll Puppet JSON Contract Smoke Test ===");

            ValidateRigExample();
            ValidateMeshExample();
            ValidateMotionExample();
            ValidateExpressionExample();
            ValidateGameplayStateRejection();

            Debug.Log("=== Doll Puppet JSON Contract Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[DollPuppetJsonContractSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void ValidateRigExample() {
        string json = @"{
  ""schema"": ""p3.doll_puppet.rig.v1"",
  ""dollId"": ""doll_proto_0"",
  ""visualId"": ""doll_proto_0_live2d"",
  ""fallbackSpriteId"": ""doll_proto_0_stand"",
  ""canvas"": { ""width"": 1024, ""height"": 1536, ""pixelsPerUnit"": 100 },
  ""layers"": [
    {
      ""id"": ""body"",
      ""path"": ""Textures/layers/body.png"",
      ""parent"": ""root"",
      ""pivot"": [0.5, 0.42],
      ""drawOrder"": 10,
      ""deformMode"": ""transform""
    },
    {
      ""id"": ""core_glow"",
      ""path"": ""Textures/layers/core_glow.png"",
      ""parent"": ""body"",
      ""pivot"": [0.5, 0.5],
      ""drawOrder"": 40,
      ""deformMode"": ""material""
    }
  ],
  ""parameters"": [
    { ""id"": ""breath"", ""type"": ""float"", ""min"": 0, ""max"": 1, ""default"": 0 },
    { ""id"": ""san_stress"", ""type"": ""float"", ""min"": 0, ""max"": 1, ""default"": 0 }
  ]
}";
        DollPuppetRigDocument rig;
        DollPuppetValidationResult result = DollPuppetJsonValidator.ValidateRigJson(json, out rig);
        LogResult("DollPuppet rig JSON", result.IsValid && rig != null && rig.Layers.Count == 2);
    }

    private static void ValidateMeshExample() {
        string json = @"{
  ""schema"": ""p3.doll_puppet.mesh.v1"",
  ""meshes"": [
    {
      ""layerId"": ""hair_front"",
      ""vertices"": [[0, 0], [1, 0], [1, 1], [0, 1]],
      ""triangles"": [0, 1, 2, 0, 2, 3],
      ""weights"": []
    }
  ]
}";
        DollPuppetMeshDocument mesh;
        DollPuppetValidationResult result = DollPuppetJsonValidator.ValidateMeshJson(json, out mesh);
        LogResult("DollPuppet mesh JSON", result.IsValid && mesh != null && mesh.Meshes.Count == 1);
    }

    private static void ValidateMotionExample() {
        string json = @"{
  ""schema"": ""p3.doll_puppet.motion.v1"",
  ""motionId"": ""idle"",
  ""loop"": true,
  ""duration"": 3.2,
  ""tracks"": [
    {
      ""target"": ""layer:body"",
      ""property"": ""scaleY"",
      ""keys"": [[0, 1.0], [1.6, 1.015], [3.2, 1.0]]
    },
    {
      ""target"": ""parameter:core_glow"",
      ""property"": ""value"",
      ""keys"": [[0, 0.35], [1.6, 0.55], [3.2, 0.35]]
    }
  ]
}";
        DollPuppetMotionDocument motion;
        DollPuppetValidationResult result = DollPuppetJsonValidator.ValidateMotionJson(json, out motion);
        LogResult("DollPuppet motion JSON", result.IsValid && motion != null && motion.Tracks.Count == 2);
    }

    private static void ValidateExpressionExample() {
        string json = @"{
  ""schema"": ""p3.doll_puppet.expression.v1"",
  ""expressionId"": ""low_san"",
  ""overrides"": [
    { ""target"": ""layer:eye_l"", ""property"": ""localY"", ""value"": -2.0 },
    { ""target"": ""layer:mouth"", ""property"": ""alpha"", ""value"": 0.85 },
    { ""target"": ""parameter:san_stress"", ""property"": ""value"", ""value"": 0.75 }
  ]
}";
        DollPuppetExpressionDocument expression;
        DollPuppetValidationResult result = DollPuppetJsonValidator.ValidateExpressionJson(json, out expression);
        LogResult("DollPuppet expression JSON", result.IsValid && expression != null && expression.Overrides.Count == 3);
    }

    private static void ValidateGameplayStateRejection() {
        string json = @"{
  ""schema"": ""p3.doll_puppet.motion.v1"",
  ""motionId"": ""bad_state"",
  ""duration"": 1.0,
  ""hp"": 25,
  ""tracks"": [
    { ""target"": ""layer:body"", ""property"": ""scaleY"", ""keys"": [[0, 1], [1, 1]] }
  ]
}";
        DollPuppetMotionDocument motion;
        DollPuppetValidationResult result = DollPuppetJsonValidator.ValidateMotionJson(json, out motion);
        LogResult("DollPuppet gameplay state rejection", !result.IsValid && HasError(result, "gameplay_state_field_forbidden:hp"));
    }

    private static bool HasError(DollPuppetValidationResult result, string expected) {
        foreach (string error in result.Errors) {
            if (error == expected) {
                return true;
            }
        }

        return false;
    }

    private static void LogResult(string label, bool passed) {
        if (passed) {
            Debug.Log($"{label} PASSED.");
        } else {
            Debug.LogError($"{label} FAILED.");
        }
    }
}
