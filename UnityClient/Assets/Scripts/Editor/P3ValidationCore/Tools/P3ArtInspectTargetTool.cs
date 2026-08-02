using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_art_inspect_target", Description = "Inspect bounded UGUI state for a registered P3 art target and record it in the active ArtRun")]
public static class P3ArtInspectTargetTool
{
    public sealed class Parameters
    {
        [ToolParameter("ArtRunID")] public string run_id { get; set; }
        [ToolParameter("Registered target ID")] public string target_id { get; set; }
        [ToolParameter("Maximum UGUI nodes to inspect", DefaultValue = "200", Required = false)] public int max_nodes { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        try
        {
            var run = p.Value<string>("run_id");
            var r = ArtLiveInspectionService.Inspect(p.Value<string>("target_id"), p.Value<int?>("max_nodes") ?? 200);
            var session = ArtValidationOrchestrator.RecordInspection(run, r);
            var path = ArtLiveInspectionService.Write(run, r);
            return new SuccessResponse("Art target inspected", new { inspection = r, evidence_path = path, session = session });
        }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
