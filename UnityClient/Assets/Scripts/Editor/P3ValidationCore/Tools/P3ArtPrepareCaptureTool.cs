using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_art_prepare_capture", Description = "Prepare an exact MCP Game View capture ticket")]
public static class P3ArtPrepareCaptureTool
{
    public sealed class Parameters
    {
        [ToolParameter("ArtRunID")] public string run_id { get; set; }
        [ToolParameter("Registered art profile ID")] public string profile_id { get; set; }
        [ToolParameter("Registered target ID")] public string target_id { get; set; }
        [ToolParameter("Capture role, for example baseline or final")] public string capture_role { get; set; }
        [ToolParameter("Optional iteration ID", Required = false)] public string iteration_id { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        try
        {
            ArtCaptureRole role;
            if (!System.Enum.TryParse(p.Value<string>("capture_role"), true, out role)) return new ErrorResponse("invalid capture_role");
            var t = ArtCaptureTicketStore.Prepare(p.Value<string>("run_id"), p.Value<string>("profile_id"), p.Value<string>("target_id"), role, p.Value<string>("iteration_id"));
            return new SuccessResponse("Capture ticket prepared", new { capture_ticket_id = t.TicketId, mcp_tool = "manage_camera", mcp_arguments = new { action = "screenshot", capture_source = "game_view", include_image = true, max_resolution = 1280, output_folder = t.OutputFolder, screenshot_file_name = t.ScreenshotFileName, screenshot_super_size = 1 } });
        }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
