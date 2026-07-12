using System;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

[McpForUnityTool("p3_run_smoke_profile",Description="Run an allowlisted P3 smoke profile",RequiresPolling=true,PollAction="status",MaxPollSeconds=900)]
public static class P3RunSmokeProfileTool { public static object HandleCommand(JObject p){ var run=p?.Value<string>("run_id"); var profile=p?.Value<string>("profile_id")??"smoke_focus"; var set=p?.Value<string>("smoke_profile_id"); var action=(p?.Value<string>("action")??"start").ToLowerInvariant(); if(string.IsNullOrWhiteSpace(run)||string.IsNullOrWhiteSpace(set))return new ErrorResponse("run_id and smoke_profile_id are required"); if(action=="cancel")return new SuccessResponse("Smoke cancellation acknowledged",new{run_id=run,state="cancelled"}); if(action=="status"){var state=P3ValidationJobStore.Load(run); return state==null?new ErrorResponse("Unknown run_id"):new SuccessResponse("Smoke status",state);} var result=P3UnityValidationOrchestrator.RunSmoke(run,profile,set); return new SuccessResponse("Smoke profile completed",new{run_id=run,status=result.Status.ToString(),result_path=P3ValidationEvidencePaths.ForRun(run).StepResult("smoke")}); } }
