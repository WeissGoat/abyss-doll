param(
    [string]$ReadinessPath = "",
    [string[]]$Domain = @(),
    [string[]]$VisualID = @(),
    [int]$Limit = 0,
    [string]$BatchID = "nai_formalv2_program_integrate_prompt_specific_20260609_01",
    [int]$Variants = 1,
    [double]$DelaySeconds = 1.0,
    [switch]$DryRun,
    [switch]$RequireToken
)

$ErrorActionPreference = "Stop"

if ($RequireToken -and [string]::IsNullOrWhiteSpace($env:NAI_ACCESS_TOKEN)) {
    throw "NAI_ACCESS_TOKEN is not set. Set it before running real NovelAI generation."
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Resolve-Path (Join-Path $scriptDir "..\..")
$pythonScript = Join-Path $scriptDir "run_formal_v2_prompt_ready_generation.py"

$argsList = @($pythonScript)

if ($ReadinessPath -ne "") {
    $argsList += @("--readiness-path", $ReadinessPath)
}

foreach ($item in $Domain) {
    $argsList += @("--domain", $item)
}

foreach ($item in $VisualID) {
    $argsList += @("--visual-id", $item)
}

if ($Limit -gt 0) {
    $argsList += @("--limit", $Limit)
}

if ($BatchID -ne "") {
    $argsList += @("--batch-id", $BatchID)
}

if ($Variants -gt 0) {
    $argsList += @("--variants", $Variants)
}

$argsList += @("--delay-seconds", $DelaySeconds)

if ($DryRun) {
    $argsList += "--dry-run"
}

if ($RequireToken) {
    $argsList += "--require-token"
}

python @argsList
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
