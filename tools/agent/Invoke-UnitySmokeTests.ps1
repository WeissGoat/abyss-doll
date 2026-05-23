param(
    [string[]]$Tests = @(
        "InventoryDisplaySpecSmokeTest.Run",
        "InventoryGridLayoutAssetValidatorTest.Run"
    ),
    [int]$TimeoutSeconds = 90,
    [string]$UnityClientPath = "UnityClient"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$unityRoot = Join-Path $repoRoot $UnityClientPath
$logsDir = Join-Path $unityRoot "Logs"
$triggerFile = Join-Path $logsDir ".test_trigger"
$reportFile = Join-Path $logsDir "TestReport.json"

if (-not (Test-Path $unityRoot)) {
    throw "Unity client path not found: $unityRoot"
}

if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir | Out-Null
}

$unityProcess = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object {
    $_.Path -and $_.Path.ToLowerInvariant().Contains("unity.exe")
}

if (-not $unityProcess) {
    Write-Warning "Unity Editor is not running. AutoTestDaemon only executes when the project is open in Unity."
}

$failed = $false
foreach ($test in $Tests) {
    $startedAt = [DateTime]::UtcNow
    Write-Host "[UnitySmoke] Running $test"
    Set-Content -Path $triggerFile -Value $test

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $matchedReport = $null
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        if (-not (Test-Path $reportFile)) {
            continue
        }

        $reportItem = Get-Item $reportFile
        if ($reportItem.LastWriteTimeUtc -lt $startedAt) {
            continue
        }

        try {
            $report = Get-Content -Raw -Path $reportFile | ConvertFrom-Json
        } catch {
            continue
        }

        if ($report.Command -eq $test) {
            $matchedReport = $report
            break
        }
    }

    if ($null -eq $matchedReport) {
        Write-Warning "[UnitySmoke] TIMEOUT waiting for $test"
        $failed = $true
        continue
    }

    if ($matchedReport.Status -ne "PASSED") {
        Write-Warning "[UnitySmoke] FAILED $test status=$($matchedReport.Status)"
        if ($matchedReport.Logs) {
            $matchedReport.Logs | ForEach-Object { Write-Host "  $_" }
        }
        $failed = $true
        continue
    }

    Write-Host "[UnitySmoke] PASSED $test"
}

if ($failed) {
    exit 1
}

Write-Host "[UnitySmoke] All selected Unity smoke tests passed."
