param(
    [string[]]$Tests = @(
        "InventoryInteractionServiceSmokeTest.Run",
        "InventoryDisplaySpecSmokeTest.Run",
        "InventoryGridLayoutAssetValidatorTest.Run"
    ),
    [int]$TimeoutSeconds = 90,
    [int]$PostTimeoutReportGraceSeconds = 90,
    [string]$UnityClientPath = "UnityClient",
    [string]$ReportOutputPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$unityRoot = Join-Path $repoRoot $UnityClientPath
$logsDir = Join-Path $unityRoot "Logs"
$triggerFile = Join-Path $logsDir ".test_trigger"
$reportFile = Join-Path $logsDir "TestReport.json"

function Read-MatchedReport {
    param(
        [string]$ReportFile,
        [string]$Test
    )

    if (-not (Test-Path $ReportFile)) {
        return $null
    }

    try {
        $rawReport = Get-Content -Raw -Encoding UTF8 -LiteralPath $ReportFile
    } catch {
        return $null
    }

    $escapedTest = [System.Text.RegularExpressions.Regex]::Escape($Test)
    if ($rawReport -notmatch ('"Command"\s*:\s*"' + $escapedTest + '"')) {
        return $null
    }

    try {
        $report = $rawReport | ConvertFrom-Json
    } catch {
        return $null
    }

    if ($report.Command -ne $Test) {
        return $null
    }

    return $report
}

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
    Write-Host "[UnitySmoke] Running $test"

    if (Test-Path $reportFile) {
        Remove-Item -LiteralPath $reportFile -Force
    }

    $clearDeadline = (Get-Date).AddSeconds(5)
    while ((Get-Date) -lt $clearDeadline -and (Test-Path $reportFile)) {
        Start-Sleep -Milliseconds 100
    }

    Set-Content -Path $triggerFile -Value $test

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $matchedReport = $null
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $matchedReport = Read-MatchedReport -ReportFile $reportFile -Test $test
        if ($null -ne $matchedReport) {
            break
        }
    }

    if ($null -eq $matchedReport) {
        $graceDeadline = (Get-Date).AddSeconds($PostTimeoutReportGraceSeconds)
        while ((Get-Date) -lt $graceDeadline) {
            Start-Sleep -Seconds 1
            $matchedReport = Read-MatchedReport -ReportFile $reportFile -Test $test
            if ($null -ne $matchedReport) {
                Write-Host "[UnitySmoke] Accepted late report for $test"
                break
            }
        }
    }

    if ($null -eq $matchedReport) {
        Write-Warning "[UnitySmoke] TIMEOUT waiting for $test"
        $failed = $true
        continue
    }

    if (-not [string]::IsNullOrWhiteSpace($ReportOutputPath)) {
        $reportOutputFullPath = [System.IO.Path]::GetFullPath($ReportOutputPath)
        $reportOutputDir = Split-Path -Parent $reportOutputFullPath
        if (-not [string]::IsNullOrWhiteSpace($reportOutputDir) -and -not (Test-Path -LiteralPath $reportOutputDir)) {
            New-Item -ItemType Directory -Force -Path $reportOutputDir | Out-Null
        }

        $matchedReport | ConvertTo-Json -Depth 20 | Set-Content -Path $reportOutputFullPath -Encoding UTF8
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
