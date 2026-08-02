using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_art_run_regression", Description = "Run the registered legacy ArtAcceptance backend as an ArtRun regression")]
public static class P3ArtRunRegressionTool
{
    public sealed class Parameters
    {
        [ToolParameter("ArtRunID")] public string run_id { get; set; }
        [ToolParameter("Unity instance ID", DefaultValue = "default", Required = false)] public string instance_id { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        try
        {
            var run = p.Value<string>("run_id");
            try { ArtValidationOrchestrator.Load(run); }
            catch { ArtValidationOrchestrator.Start(run, "art_regression", new string[0], p.Value<string>("instance_id") ?? "default"); }
            ArtAcceptanceAdapter.Start(run);
            return new SuccessResponse("Art regression started", new { run_id = run });
        }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
