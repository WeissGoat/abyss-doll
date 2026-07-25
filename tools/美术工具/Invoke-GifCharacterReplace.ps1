param(
    [string]$InputGif,
    [string]$Prompt,
    [string]$PromptFile,
    [string[]]$Reference,
    [string]$OutputRoot = ".",
    [string]$Config,
    [string]$Provider = "gemini_chat_image",
    [int]$MaxFrames = 30,
    [double]$DelaySeconds = 2.0,
    [int]$RetryCount = 3,
    [int]$VisualRetryCount = 2,
    [switch]$PreserveTransparency,
    [switch]$NoPreserveTransparency,
    [ValidateSet("auto", "ffmpeg", "pillow")]
    [string]$Encoder = "auto",
    [switch]$DryRun,
    [string]$RunID,
    [string]$SetActionTexts,
    [int]$SelectIdentity = -1,
    [switch]$ApproveAppearanceAnchor,
    [switch]$ApprovePreview,
    [switch]$Resume,
    [int[]]$RerunFrame,
    [ValidateSet("normal", "strict")]
    [string]$RepairMode = "normal",
    [switch]$EncodeOnly
)

$ErrorActionPreference = "Stop"
$script = Join-Path $PSScriptRoot "gif_character_replace_cli.py"
$arguments = @()
if ($InputGif) { $arguments += @("--input-gif", $InputGif) }
if ($Prompt) { $arguments += @("--prompt", $Prompt) }
if ($PromptFile) { $arguments += @("--prompt-file", $PromptFile) }
foreach ($item in $Reference) { $arguments += @("--reference", $item) }
$arguments += @("--output-root", $OutputRoot, "--provider", $Provider, "--max-frames", $MaxFrames, "--delay-seconds", $DelaySeconds, "--retry-count", $RetryCount, "--visual-retry-count", $VisualRetryCount, "--encoder", $Encoder)
if ($Config) { $arguments += @("--config", $Config) }
if ($DryRun) { $arguments += "--dry-run" }
if ($RunID) { $arguments += @("--run-id", $RunID) }
if ($SetActionTexts) { $arguments += @("--set-action-texts", $SetActionTexts) }
    if ($SelectIdentity -ge 0) { $arguments += @("--select-identity", $SelectIdentity) }
    if ($ApproveAppearanceAnchor) { $arguments += "--approve-appearance-anchor" }
    if ($ApprovePreview) { $arguments += "--approve-preview" }
if ($Resume) { $arguments += "--resume" }
foreach ($item in $RerunFrame) { $arguments += @("--rerun-frame", $item) }
if ($RepairMode -eq "strict") { $arguments += @("--repair-mode", "strict") }
if ($EncodeOnly) { $arguments += "--encode-only" }
if ($NoPreserveTransparency) { $arguments += "--no-preserve-transparency" }
elseif ($PreserveTransparency) { $arguments += "--preserve-transparency" }

python $script @arguments
exit $LASTEXITCODE
