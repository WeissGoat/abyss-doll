using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

[McpForUnityTool("p3_unity_readiness", Description="Read-only P3 Unity validation readiness")]
public static class P3UnityReadinessTool { public static object HandleCommand(JObject p)=>new SuccessResponse("P3 Unity readiness",P3UnityValidationOrchestrator.Readiness()); }
