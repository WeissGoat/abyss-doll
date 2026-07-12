using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using P3.Validation;

[McpForUnityTool("p3_program_run_profile",Description="Run a registered P3 program validation profile")]
public static class P3ProgramRunProfileTool { public static object HandleCommand(JObject p){var run=p?.Value<string>("run_id");var profile=p?.Value<string>("profile_id")??"smoke_focus";var instance=p?.Value<string>("instance_id")??"default";if(string.IsNullOrWhiteSpace(run))return new ErrorResponse("run_id is required");try{return new SuccessResponse("Program profile completed",ProgramValidationOrchestrator.RunProfile(run,profile,instance));}catch(System.Exception e){return new ErrorResponse(e.Message);}}}
