param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Plan", "SyncApproved", "Finalize")]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$ArtImportRunID,
    [string[]]$VisualID = @(),
    [ValidateSet("interactive", "auto")]
    [string]$Mode = "interactive",
    [string]$UnityInstance = "",
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [string]$ApprovedRoot = "",
    [string]$EvidenceRoot = "",
    [switch]$AuthorizeApprovedSync,
    [switch]$AllowExistingTargetOverwrite,
    [switch]$AllowNewApprovedTarget,
    [switch]$RefreshProgramHandoff
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = (Resolve-Path (Join-Path $scriptDir "..\..")).Path
$pythonScript = Join-Path $scriptDir "art_approved_unity_registration.py"
$syncScript = Join-Path $scriptDir "Sync-ApprovedArt.ps1"
$effectiveEvidenceRoot = if ($EvidenceRoot -ne "") {
    $EvidenceRoot
} else {
    "UnityClient/Logs/P3ArtImport"
}

function Invoke-PythonPhase {
    param([string[]]$Arguments)

    python @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if ($Phase -eq "Plan") {
    if ($VisualID.Count -eq 0) {
        throw "Plan requires at least one -VisualID."
    }
    $argsList = @(
        $pythonScript,
        "plan",
        "--art-import-run-id", $ArtImportRunID,
        "--mode", $Mode,
        "--unity-instance", $UnityInstance,
        "--evidence-root", $effectiveEvidenceRoot
    )
    if ($ManifestPath -ne "") {
        $argsList += @("--manifest-path", $ManifestPath)
    }
    if ($IncomingRoot -ne "") {
        $argsList += @("--incoming-root", $IncomingRoot)
    }
    if ($ApprovedRoot -ne "") {
        $argsList += @("--approved-root", $ApprovedRoot)
    }
    foreach ($item in $VisualID) {
        $argsList += @("--visual-id", $item)
    }
    if ($AuthorizeApprovedSync) {
        $argsList += "--allow-approved-sync"
    }
    if ($AllowExistingTargetOverwrite) {
        $argsList += "--allow-existing-target-overwrite"
    }
    if ($AllowNewApprovedTarget) {
        $argsList += "--allow-new-approved-target"
    }
    Invoke-PythonPhase -Arguments $argsList
    exit 0
}

if ($Phase -eq "SyncApproved") {
    $verifyArgs = @(
        $pythonScript,
        "verify-sync",
        "--art-import-run-id", $ArtImportRunID,
        "--evidence-root", $effectiveEvidenceRoot
    )
    if ($AuthorizeApprovedSync) {
        $verifyArgs += "--authorize-approved-sync"
    }
    if ($AllowExistingTargetOverwrite) {
        $verifyArgs += "--allow-existing-target-overwrite"
    }
    if ($AllowNewApprovedTarget) {
        $verifyArgs += "--allow-new-approved-target"
    }
    Invoke-PythonPhase -Arguments $verifyArgs

    $evidenceRootPath = if ([System.IO.Path]::IsPathRooted($effectiveEvidenceRoot)) {
        $effectiveEvidenceRoot
    } else {
        Join-Path $projectRoot $effectiveEvidenceRoot
    }
    $planPath = Join-Path (Join-Path $evidenceRootPath $ArtImportRunID) "approved-plan.json"
    $plan = Get-Content -Raw -Encoding UTF8 -LiteralPath $planPath | ConvertFrom-Json
    $syncParams = @{
        VisualID = @($plan.items | ForEach-Object { $_.visual_id })
    }
    if ($ManifestPath -ne "") {
        $syncParams.ManifestPath = $ManifestPath
    }
    if ($IncomingRoot -ne "") {
        $syncParams.InRoot = $IncomingRoot
    }
    if (@($plan.items | Where-Object { $_.action -eq "overwrite" }).Count -gt 0) {
        $syncParams.Overwrite = $true
    }
    & $syncScript @syncParams
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Invoke-PythonPhase -Arguments @(
        $pythonScript,
        "record-sync",
        "--art-import-run-id", $ArtImportRunID,
        "--evidence-root", $effectiveEvidenceRoot
    )
    exit 0
}

throw "Finalize is not available until the live Unity evidence verifier is installed."
