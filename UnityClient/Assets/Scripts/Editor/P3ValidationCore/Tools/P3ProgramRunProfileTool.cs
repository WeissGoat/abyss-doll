using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_program_run_profile", Description = "Run a registered P3 program validation profile")]
public static class P3ProgramRunProfileTool
{
    public sealed class Parameters
    {
        [ToolParameter("ProgramRunID")] public string run_id { get; set; }
        [ToolParameter("Registered program profile ID", DefaultValue = "smoke_focus", Required = false)] public string profile_id { get; set; }
        [ToolParameter("Unity instance ID", DefaultValue = "default", Required = false)] public string instance_id { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        var run = p?.Value<string>("run_id");
        var profile = p?.Value<string>("profile_id") ?? "smoke_focus";
        var instance = p?.Value<string>("instance_id") ?? "default";
        if (string.IsNullOrWhiteSpace(run)) return new ErrorResponse("run_id is required");
        try { return new SuccessResponse("Program profile completed", ProgramValidationOrchestrator.RunProfile(run, profile, instance)); }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
