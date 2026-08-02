using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_art_open_target", Description = "Verify a registered P3 art target is currently reachable")]
public static class P3ArtOpenTargetTool
{
    public sealed class Parameters
    {
        [ToolParameter("Registered target ID")] public string target_id { get; set; }
    }

    public static object HandleCommand(JObject p)
    {
        try
        {
            var id = p?.Value<string>("target_id");
            var go = ArtTargetRegistry.ResolveVisibleRoot(id);
            return new SuccessResponse("Art target reachable", new { target_id = id, root = go.name, screen_tag = ArtTargetRegistry.Get(id).ScreenTag });
        }
        catch (System.Exception e) { return new ErrorResponse(e.Message); }
    }
}
