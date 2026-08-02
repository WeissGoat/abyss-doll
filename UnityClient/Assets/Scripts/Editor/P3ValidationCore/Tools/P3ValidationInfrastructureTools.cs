using System;
using System.IO;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_validation_readiness", Description = "Read-only P3 validation readiness")]
public static class P3ValidationReadinessTool
{
    public static object HandleCommand(JObject p)
    {
        var s = EditorSnapshot.Capture();
        var code = s.HasDirtyScenes ? "infra_blocked:dirty_scene" : s.PrefabStageDirty ? "infra_blocked:dirty_prefab_stage" : s.IsCompiling ? "infra_limited:editor_compiling" : null;
        return new SuccessResponse("P3 readiness", new { ready = code == null, blocking_code = code, snapshot = s });
    }
}

[McpForUnityTool("p3_validation_collect_console", Description = "Collect a P3 RunID Console delta")]
public static class P3ValidationCollectConsoleTool
{
    public sealed class Parameters
    {
        [ToolParameter("P3 RunID")] public string run_id { get; set; }
        [ToolParameter("Validation domain: program, art, or release")] public string validation_domain { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        var run = p?.Value<string>("run_id");
        var domain = p?.Value<string>("validation_domain");
        if (string.IsNullOrWhiteSpace(run) || domain == null) return new ErrorResponse("run_id and validation_domain are required");
        return new SuccessResponse("Console collection is owned by the active profile job", new { run_id = run, validation_domain = domain });
    }
}

[McpForUnityTool("p3_validation_collect_evidence", Description = "List domain-scoped P3 evidence")]
public static class P3ValidationCollectEvidenceTool
{
    public sealed class Parameters
    {
        [ToolParameter("P3 RunID")] public string run_id { get; set; }
        [ToolParameter("Validation domain: program, art, or release")] public string validation_domain { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        var run = p?.Value<string>("run_id");
        var domain = p?.Value<string>("validation_domain");
        if (string.IsNullOrWhiteSpace(run) || domain == null) return new ErrorResponse("run_id and validation_domain are required");
        EvidencePaths paths;
        try { paths = domain == "program" ? EvidencePaths.ForProgramRun(run) : domain == "art" ? EvidencePaths.ForArtRun(run) : EvidencePaths.ForReleaseRun(run); }
        catch (Exception e) { return new ErrorResponse(e.Message); }
        return new SuccessResponse("P3 evidence", new { evidence_root = paths.Root, results = Directory.GetFiles(paths.Steps, "result.json", SearchOption.AllDirectories) });
    }
}
