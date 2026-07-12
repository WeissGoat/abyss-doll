using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

[McpForUnityTool("p3_run_unity_profile",Description="Run a registered P3 Unity validation profile",RequiresPolling=true,PollAction="status",MaxPollSeconds=900)]
public static class P3RunUnityProfileTool { public static object HandleCommand(JObject p){var run=p?.Value<string>("run_id");var profile=p?.Value<string>("unity_profile_id");var policy=p?.Value<string>("control_policy")??"exclusive_restore";var action=(p?.Value<string>("action")??"start").ToLowerInvariant();if(string.IsNullOrWhiteSpace(run)||string.IsNullOrWhiteSpace(profile))return new ErrorResponse("run_id and unity_profile_id are required");if(policy!="exclusive_restore")return new ErrorResponse("Only exclusive_restore is supported");if(action=="start"){var state=P3UnityValidationOrchestrator.Start(run,profile);return new SuccessResponse("Unity profile finished",state);}var existing=P3ValidationJobStore.Load(run);if(existing==null)return new ErrorResponse("Unknown run_id");if(action=="cancel"){existing.Status="Cancelled";P3ValidationJobStore.Save(existing);}return new SuccessResponse("Unity profile status",existing);} }
