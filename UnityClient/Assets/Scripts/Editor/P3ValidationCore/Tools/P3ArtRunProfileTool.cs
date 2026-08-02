using System.Linq;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_art_run_profile", Description = "Run or poll a registered live-first P3 art profile")]
public static class P3ArtRunProfileTool
{
    public sealed class Parameters
    {
        [ToolParameter("Action: start, status, record_review, finalize_runtime_validated, or complete", DefaultValue = "start", Required = false)] public string action { get; set; }
        [ToolParameter("ArtRunID")] public string run_id { get; set; }
        [ToolParameter("Registered art profile ID", Required = false)] public string profile_id { get; set; }
        [ToolParameter("Registered target ID", Required = false)] public string target_id { get; set; }
        [ToolParameter("Registered target IDs", Required = false)] public string[] target_ids { get; set; }
        [ToolParameter("Unity instance ID", DefaultValue = "default", Required = false)] public string instance_id { get; set; }
        [ToolParameter("Agent review object for record_review", Required = false)] public object review { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        try
        {
            var action = (p.Value<string>("action") ?? "start").ToLowerInvariant();
            var run = p.Value<string>("run_id");
            if (action == "start")
            {
                var ids = (p["target_ids"] as JArray)?.Values<string>() ?? new[] { p.Value<string>("target_id") };
                return new SuccessResponse("Art profile started", ArtValidationOrchestrator.Start(run, p.Value<string>("profile_id"), ids.Where(x => !string.IsNullOrWhiteSpace(x)), p.Value<string>("instance_id") ?? "default"));
            }
            if (action == "status") return new SuccessResponse("Art profile status", ArtValidationOrchestrator.Load(run));
            if (action == "record_review")
            {
                var r = p["review"].ToObject<ArtValidationReview>();
                return new SuccessResponse("Agent review recorded", ArtValidationOrchestrator.RecordReview(run, r));
            }
            if (action == "finalize_runtime_validated") return new SuccessResponse("Runtime validation finalized", ArtValidationOrchestrator.FinalizeRuntimeValidated(run));
            if (action == "complete") return new SuccessResponse("Art profile completed", ArtValidationOrchestrator.Complete(run));
            return new ErrorResponse("unknown action");
        }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
