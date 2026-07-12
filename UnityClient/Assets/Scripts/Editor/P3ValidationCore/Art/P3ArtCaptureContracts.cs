using System;
using Newtonsoft.Json;

namespace P3.Validation {
[Serializable] public sealed class ArtCaptureTicket {
 [JsonProperty("ticket_id")] public string TicketId;
 [JsonProperty("run_id")] public string RunId;
 [JsonProperty("profile_id")] public string ProfileId;
 [JsonProperty("target_id")] public string TargetId;
 [JsonProperty("role")] public ArtCaptureRole Role;
 [JsonProperty("iteration_id")] public string IterationId;
 [JsonProperty("output_folder")] public string OutputFolder;
 [JsonProperty("screenshot_file_name")] public string ScreenshotFileName;
 [JsonProperty("expected_absolute_path")] public string ExpectedAbsolutePath;
 [JsonProperty("capture_source")] public string CaptureSource="game_view";
 [JsonProperty("camera")] public string Camera;
 [JsonProperty("created_at")] public string CreatedAt;
 [JsonProperty("status")] public string Status;
}
}
