using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_program_run_smoke", Description = "Run an allowlisted P3 program smoke set")]
public static class P3ProgramRunSmokeTool
{
    public sealed class Parameters
    {
        [ToolParameter("ProgramRunID")] public string run_id { get; set; }
        [ToolParameter("Registered program smoke profile ID", DefaultValue = "smoke_focus", Required = false)] public string profile_id { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        var run = p?.Value<string>("run_id");
        var profile = p?.Value<string>("profile_id") ?? "smoke_focus";
        if (string.IsNullOrWhiteSpace(run)) return new ErrorResponse("run_id is required");
        try
        {
            var r = ProgramValidationOrchestrator.RunSmoke(run, profile);
            return new SuccessResponse("Program smoke completed", r);
        }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
