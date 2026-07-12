using System;
using System.IO;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

[McpForUnityTool("p3_collect_unity_evidence", Description="Collect RunID-scoped P3 Unity evidence")]
public static class P3CollectUnityEvidenceTool { public static object HandleCommand(JObject p){ var run=p?.Value<string>("run_id"); if(string.IsNullOrWhiteSpace(run))return new ErrorResponse("run_id is required"); var paths=P3ValidationEvidencePaths.ForRun(run); return new SuccessResponse("P3 evidence location",new{run_id=run,evidence_root=paths.Root,step_results=Directory.GetFiles(paths.Steps,"result.json",SearchOption.AllDirectories)}); } }
