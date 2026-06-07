param(
    [switch]$SkipUnity,
    [switch]$SkipArtAcceptance = $true,
    [switch]$Strict,
    [string]$OutputRoot = "UnityClient/Logs/P0Validation/latest",
    [switch]$History,
    [string]$SeedProfile = "p0_core",
    [int]$TimeoutSeconds = 180,
    [int]$ArtAcceptanceTimeoutSeconds = 240
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
$runId = Get-Date -Format "yyyyMMdd_HHmmss"
$startedAt = [DateTimeOffset]::Now
$steps = New-Object System.Collections.Generic.List[object]
$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]

function Resolve-ProjectPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Convert-ToRepoPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ""
    }

    $full = [System.IO.Path]::GetFullPath($Path)
    $root = $repoRoot.TrimEnd("\") + "\"
    if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $full.Substring($root.Length).Replace("\", "/")
    }

    return $full.Replace("\", "/")
}

function Get-Count {
    param($Value)

    if ($null -eq $Value) {
        return 0
    }

    if ($Value -is [array]) {
        return $Value.Count
    }

    if ($Value -is [System.Collections.ICollection]) {
        return $Value.Count
    }

    return 1
}

function Get-PowerShellExecutable {
    $pwsh = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -ne $pwsh) {
        return $pwsh.Source
    }

    $powershell = Get-Command powershell -ErrorAction SilentlyContinue
    if ($null -ne $powershell) {
        return $powershell.Source
    }

    throw "Could not locate pwsh or powershell."
}

function New-Step {
    param([string]$Name)

    return [ordered]@{
        Name = $Name
        Status = "Pending"
        StartedAt = ""
        FinishedAt = ""
        DurationMs = 0
        ErrorCount = 0
        WarningCount = 0
        Output = ""
        Logs = @()
        Details = [ordered]@{}
    }
}

function Complete-Step {
    param(
        $Step,
        [string]$Status,
        [DateTime]$Started,
        [string[]]$Logs = @(),
        [int]$ErrorCount = 0,
        [int]$WarningCount = 0,
        [string]$Output = "",
        $Details = $null
    )

    $finished = Get-Date
    $Step.Status = $Status
    $Step.StartedAt = ([DateTimeOffset]$Started).ToString("o")
    $Step.FinishedAt = ([DateTimeOffset]$finished).ToString("o")
    $Step.DurationMs = [int][Math]::Round(($finished - $Started).TotalMilliseconds)
    $Step.ErrorCount = $ErrorCount
    $Step.WarningCount = $WarningCount
    $Step.Output = Convert-ToRepoPath $Output
    $Step.Logs = @($Logs)
    if ($null -ne $Details) {
        $Step.Details = $Details
    }

    $steps.Add([pscustomobject]$Step) | Out-Null
}

function Write-JsonFile {
    param(
        [string]$Path,
        $Value
    )

    $json = $Value | ConvertTo-Json -Depth 20
    Set-Content -Path $Path -Value $json -Encoding UTF8
}

function Read-JsonFile {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }

    try {
        return Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
    } catch {
        return $null
    }
}

function Invoke-ChildPowerShell {
    param(
        [string]$ScriptPath,
        [string[]]$Arguments = @()
    )

    $childStarted = Get-Date
    $commandArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath) + $Arguments
    $output = @()
    $exitCode = 1

    try {
        $output = & $script:PowerShellExe @commandArgs 2>&1
        $exitCode = $LASTEXITCODE
        if ($null -eq $exitCode) {
            $exitCode = 0
        }
    } catch {
        $output += $_.Exception.Message
        $exitCode = 1
    }

    $lines = @()
    foreach ($line in $output) {
        $lines += [string]$line
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Logs = $lines
        StartedAt = $childStarted
        DurationMs = [int][Math]::Round(((Get-Date) - $childStarted).TotalMilliseconds)
    }
}

function Test-UnityEditorRunning {
    $process = Get-Process Unity -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -and $_.Path.ToLowerInvariant().Contains("unity.exe")
    } | Select-Object -First 1

    return $null -ne $process
}

function Parse-TestReport {
    param(
        [string]$ReportPath,
        [string]$ExpectedCommand
    )

    $report = Read-JsonFile $ReportPath
    if ($null -eq $report) {
        return $null
    }

    if ($report.Command -ne $ExpectedCommand) {
        return $null
    }

    return $report
}

function Count-TestWarnings {
    param($TestReport)

    $count = 0
    if ($null -eq $TestReport -or -not $TestReport.Logs) {
        return 0
    }

    foreach ($log in $TestReport.Logs) {
        $text = [string]$log
        if ($text.StartsWith("[Warning]") -or $text.Contains("WARNING:")) {
            $count++
        }
    }

    return $count
}

function Invoke-UnityTest {
    param(
        [string]$TestName,
        [string]$OutputPath
    )

    $unitySmokeScript = Join-Path $repoRoot "tools\agent\Invoke-UnitySmokeTests.ps1"
    $stableTestReportPath = [System.IO.Path]::ChangeExtension($OutputPath, ".TestReport.json")
    if (Test-Path -LiteralPath $stableTestReportPath) {
        Remove-Item -LiteralPath $stableTestReportPath -Force
    }

    $args = @(
        "-Tests", $TestName,
        "-TimeoutSeconds", [string]$TimeoutSeconds,
        "-ReportOutputPath", $stableTestReportPath
    )

    $result = Invoke-ChildPowerShell -ScriptPath $unitySmokeScript -Arguments $args
    $testReport = Parse-TestReport -ReportPath $stableTestReportPath -ExpectedCommand $TestName
    $status = "Blocked"
    $errorCount = 0
    $warningCount = 0
    $details = [ordered]@{
        Test = $TestName
        ExitCode = $result.ExitCode
        TestReportPath = Convert-ToRepoPath $stableTestReportPath
        TestReport = $testReport
    }

    if ($null -eq $testReport) {
        $errorCount = 1
        $details.BlockReason = "No matching Unity TestReport.json was produced."
    } elseif ($testReport.Status -eq "PASSED") {
        $status = "Passed"
        $warningCount = Count-TestWarnings -TestReport $testReport
    } else {
        $status = "Failed"
        $errorCount = 1
        $warningCount = Count-TestWarnings -TestReport $testReport
    }

    if ($result.ExitCode -ne 0 -and $status -eq "Passed") {
        $status = "Failed"
        $errorCount = 1
    }

    $payload = [ordered]@{
        Status = $status
        ErrorCount = $errorCount
        WarningCount = $warningCount
        StartedAt = ([DateTimeOffset]$result.StartedAt).ToString("o")
        DurationMs = $result.DurationMs
        Logs = $result.Logs
        Details = $details
    }
    Write-JsonFile -Path $OutputPath -Value $payload

    return [pscustomobject]$payload
}

function Extract-ConfigValidationCounts {
    param($TestReport)

    $errorCount = 0
    $warningCount = 0

    if ($null -ne $TestReport -and $TestReport.Logs) {
        foreach ($log in $TestReport.Logs) {
            $text = [string]$log
            if ($text -match "Errors=(\d+)") {
                $errorCount = [int]$Matches[1]
            }
            if ($text -match "Warnings=(\d+)") {
                $warningCount = [int]$Matches[1]
            }
        }
    }

    if ($null -ne $TestReport -and $TestReport.Status -ne "PASSED" -and $errorCount -eq 0) {
        $errorCount = 1
    }

    return [pscustomobject]@{
        ErrorCount = $errorCount
        WarningCount = $warningCount
    }
}

function Get-LatestWriteTime {
    param([string[]]$Paths)

    $latest = $null
    foreach ($path in $Paths) {
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }

        $absolute = Resolve-ProjectPath $path
        if (-not (Test-Path -LiteralPath $absolute)) {
            continue
        }

        $item = Get-Item -LiteralPath $absolute
        if ($null -eq $latest -or $item.LastWriteTime -gt $latest) {
            $latest = $item.LastWriteTime
        }
    }

    return $latest
}

function Find-ProjectFile {
    param(
        [string]$Name,
        [string]$PreferredPathPart = ""
    )

    $searchRoots = @(
        (Join-Path $repoRoot "tools"),
        (Join-Path $repoRoot "UnityClient\Assets")
    )
    $searchRoots += Get-ChildItem -LiteralPath $repoRoot -Directory -Force -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -notin @(".git", "UnityClient", "tools") -and
            -not $_.Name.StartsWith(".")
        } |
        ForEach-Object { $_.FullName }

    $matches = @()
    foreach ($root in $searchRoots) {
        if (-not (Test-Path -LiteralPath $root)) {
            continue
        }

        $matches += Get-ChildItem -LiteralPath $root -Recurse -File -Filter $Name -ErrorAction SilentlyContinue
    }

    if (-not [string]::IsNullOrWhiteSpace($PreferredPathPart)) {
        $preferred = $matches | Where-Object {
            $_.FullName.Replace("\", "/").Contains($PreferredPathPart)
        } | Select-Object -First 1

        if ($null -ne $preferred) {
            return $preferred.FullName
        }
    }

    $first = $matches | Select-Object -First 1
    if ($null -eq $first) {
        return ""
    }

    return $first.FullName
}

function Get-ReportTime {
    param($Report, [string]$ReportPath)

    $timeText = ""
    if ($null -ne $Report) {
        if ($Report.FinishedAt) {
            $timeText = [string]$Report.FinishedAt
        } elseif ($Report.StartedAt) {
            $timeText = [string]$Report.StartedAt
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($timeText)) {
        try {
            return [DateTimeOffset]::Parse($timeText).LocalDateTime
        } catch {
        }
    }

    if (Test-Path -LiteralPath $ReportPath) {
        return (Get-Item -LiteralPath $ReportPath).LastWriteTime
    }

    return $null
}

function Invoke-ArtAcceptanceRun {
    param([string]$LatestReportPath)

    $step = New-Step "ArtAcceptanceRerun"
    $start = Get-Date

    if (-not (Test-UnityEditorRunning)) {
        Complete-Step -Step $step -Status "Blocked" -Started $start -ErrorCount 1 -Logs @("Unity Editor is not running; cannot rerun ArtAcceptance.") -Details ([ordered]@{
            Trigger = "UnityClient/Logs/.art_acceptance_trigger"
        })
        $errors.Add("ArtAcceptance rerun blocked: Unity Editor is not running.") | Out-Null
        return
    }

    $oldReport = Read-JsonFile $LatestReportPath
    $oldRunId = ""
    if ($null -ne $oldReport -and $oldReport.RunID) {
        $oldRunId = [string]$oldReport.RunID
    }

    $logsDir = Join-Path $repoRoot "UnityClient\Logs"
    if (-not (Test-Path -LiteralPath $logsDir)) {
        New-Item -ItemType Directory -Force -Path $logsDir | Out-Null
    }

    $triggerFile = Join-Path $logsDir ".art_acceptance_trigger"
    Set-Content -Path $triggerFile -Value "RUN_ART_ACCEPTANCE" -Encoding UTF8

    $deadline = (Get-Date).AddSeconds($ArtAcceptanceTimeoutSeconds)
    $newReport = $null
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $candidate = Read-JsonFile $LatestReportPath
        if ($null -eq $candidate) {
            continue
        }

        $candidateRunId = ""
        if ($candidate.RunID) {
            $candidateRunId = [string]$candidate.RunID
        }

        if ($candidateRunId -ne "" -and $candidateRunId -ne $oldRunId -and -not $candidate.IsRunning) {
            $newReport = $candidate
            break
        }
    }

    if ($null -eq $newReport) {
        Complete-Step -Step $step -Status "Blocked" -Started $start -ErrorCount 1 -Logs @("Timed out waiting for ArtAcceptance latest report.") -Details ([ordered]@{
            Trigger = Convert-ToRepoPath $triggerFile
            TimeoutSeconds = $ArtAcceptanceTimeoutSeconds
        })
        $errors.Add("ArtAcceptance rerun blocked: timeout waiting for latest report.") | Out-Null
        return
    }

    Complete-Step -Step $step -Status "Passed" -Started $start -Logs @("ArtAcceptance rerun completed.") -Details ([ordered]@{
        RunID = $newReport.RunID
        Status = $newReport.Status
    })
}

function New-ArtAcceptanceSummary {
    param([string]$OutputPath)

    $latestReportPath = Join-Path $repoRoot "UnityClient\Logs\ArtAcceptance\latest\report.json"
    $report = Read-JsonFile $latestReportPath
    $summaryStatus = "Failed"
    $errorCount = 0
    $warningCount = 0
    $summaryErrors = @()
    $summaryWarnings = @()
    $captureCount = 0
    $captureTags = @()
    $reportTime = $null

    if ($null -eq $report) {
        $errorCount = 1
        $summaryErrors += "ArtAcceptance latest report is missing or unreadable."
    } else {
        $reportTime = Get-ReportTime -Report $report -ReportPath $latestReportPath
        $captureCount = Get-Count $report.Captures
        if ($report.Captures) {
            foreach ($capture in $report.Captures) {
                $captureTags += [string]$capture.ScreenTag
            }
        }

        $reportStatus = [string]$report.Status
        if ($report.IsRunning) {
            $summaryStatus = "Blocked"
            $errorCount = 1
            $summaryErrors += "ArtAcceptance latest report is still running."
        } elseif ($reportStatus -eq "FAILED") {
            $summaryStatus = "Failed"
            $errorCount = [Math]::Max(1, (Get-Count $report.Errors))
            foreach ($item in @($report.Errors)) {
                if ($item) {
                    $summaryErrors += [string]$item
                }
            }
        } else {
            $summaryStatus = "Passed"
            $warningCount = Get-Count $report.Warnings
            foreach ($item in @($report.Warnings)) {
                if ($item) {
                    $summaryWarnings += [string]$item
                }
            }
            if ($reportStatus -eq "WARNING" -and $warningCount -eq 0) {
                $warningCount = 1
                $summaryWarnings += "ArtAcceptance latest status is WARNING."
            }
        }
    }

    $specLatest = Get-LatestWriteTime @(
        (Find-ProjectFile -Name "design_tokens.json" -PreferredPathPart "/ui_design/"),
        (Find-ProjectFile -Name "component_catalog.json" -PreferredPathPart "/ui_design/"),
        (Find-ProjectFile -Name "screen_layouts.json" -PreferredPathPart "/ui_design/"),
        (Find-ProjectFile -Name "art_manifest.json" -PreferredPathPart "/_generated/")
    )

    $isStale = $false
    if ($null -ne $reportTime -and $null -ne $specLatest -and $reportTime -lt $specLatest) {
        $isStale = $true
        $warningCount += 1
        $summaryWarnings += "ArtAcceptance latest report is older than active UI/art specs."
    }

    $summary = [ordered]@{
        Status = $summaryStatus
        ErrorCount = $errorCount
        WarningCount = $warningCount
        ReportPath = Convert-ToRepoPath $latestReportPath
        RunID = if ($null -ne $report) { [string]$report.RunID } else { "" }
        ReportStatus = if ($null -ne $report) { [string]$report.Status } else { "" }
        FinishedAt = if ($null -ne $report -and $report.FinishedAt) { [string]$report.FinishedAt } else { "" }
        CaptureCount = $captureCount
        CaptureTags = $captureTags
        SpecLatestWriteTime = if ($null -ne $specLatest) { ([DateTimeOffset]$specLatest).ToString("o") } else { "" }
        IsStaleAgainstActiveUi = $isStale
        Errors = $summaryErrors
        Warnings = $summaryWarnings
    }

    Write-JsonFile -Path $OutputPath -Value $summary
    return [pscustomobject]$summary
}

function Add-StepMessages {
    param($Step)

    if ($Step.ErrorCount -gt 0) {
        $errors.Add("$($Step.Name): $($Step.Status)") | Out-Null
    }

    if ($Step.WarningCount -gt 0) {
        $warnings.Add("$($Step.Name): $($Step.WarningCount) warning(s)") | Out-Null
    }
}

function Write-MarkdownReport {
    param(
        [string]$Path,
        $Report
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# P0 Validation Report") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add(('* Status: `{0}`' -f $Report.Status)) | Out-Null
    $lines.Add(('* RunID: `{0}`' -f $Report.RunID)) | Out-Null
    $lines.Add(('* StartedAt: `{0}`' -f $Report.StartedAt)) | Out-Null
    $lines.Add(('* DurationMs: `{0}`' -f $Report.DurationMs)) | Out-Null
    $lines.Add(('* Strict: `{0}`' -f $Report.Strict)) | Out-Null
    $lines.Add(('* SeedProfile: `{0}`' -f $Report.SeedProfile)) | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("## Steps") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| Step | Status | Errors | Warnings | Output |") | Out-Null
    $lines.Add("|---|---|---:|---:|---|") | Out-Null
    foreach ($step in $Report.Steps) {
        $lines.Add(('| `{0}` | `{1}` | {2} | {3} | `{4}` |' -f $step.Name, $step.Status, $step.ErrorCount, $step.WarningCount, $step.Output)) | Out-Null
    }

    $lines.Add("") | Out-Null
    $lines.Add("## Errors") | Out-Null
    if ((Get-Count $Report.Errors) -eq 0) {
        $lines.Add("") | Out-Null
        $lines.Add("None.") | Out-Null
    } else {
        foreach ($item in $Report.Errors) {
            $lines.Add("* $item") | Out-Null
        }
    }

    $lines.Add("") | Out-Null
    $lines.Add("## Warnings") | Out-Null
    if ((Get-Count $Report.Warnings) -eq 0) {
        $lines.Add("") | Out-Null
        $lines.Add("None.") | Out-Null
    } else {
        foreach ($item in $Report.Warnings) {
            $lines.Add("* $item") | Out-Null
        }
    }

    $lines.Add("") | Out-Null
    $lines.Add("## ArtAcceptance Latest") | Out-Null
    $art = $Report.ArtAcceptanceLatest
    $lines.Add("") | Out-Null
    $lines.Add(('* ReportPath: `{0}`' -f $art.ReportPath)) | Out-Null
    $lines.Add(('* RunID: `{0}`' -f $art.RunID)) | Out-Null
    $lines.Add(('* Status: `{0}`' -f $art.ReportStatus)) | Out-Null
    $lines.Add(('* FinishedAt: `{0}`' -f $art.FinishedAt)) | Out-Null
    $lines.Add(('* IsStaleAgainstActiveUi: `{0}`' -f $art.IsStaleAgainstActiveUi)) | Out-Null

    Set-Content -Path $Path -Value $lines -Encoding UTF8
}

$script:PowerShellExe = Get-PowerShellExecutable
$outputPath = Resolve-ProjectPath $OutputRoot
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

foreach ($fileName in @(
    "report.json",
    "report.md",
    "config_validation.json",
    "smoke_tests.json",
    "ui_validation.json",
    "art_acceptance_summary.json",
    "ui_design_handoff.md"
)) {
    $target = Join-Path $outputPath $fileName
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Force
    }
}

$syncScript = Join-Path $repoRoot "tools\config\Sync-Configs.ps1"
$syncStep = New-Step "ConfigSync"
$syncStart = Get-Date
$syncResult = Invoke-ChildPowerShell -ScriptPath $syncScript -Arguments @("-Clean")
if ($syncResult.ExitCode -eq 0) {
    Complete-Step -Step $syncStep -Status "Passed" -Started $syncStart -Logs $syncResult.Logs
} else {
    Complete-Step -Step $syncStep -Status "Failed" -Started $syncStart -Logs $syncResult.Logs -ErrorCount 1
    $errors.Add("ConfigSync failed.") | Out-Null
}

$configValidationPath = Join-Path $outputPath "config_validation.json"
$smokeTestsPath = Join-Path $outputPath "smoke_tests.json"
$unityRunning = Test-UnityEditorRunning
$smokeTests = @(
    "InventoryInteractionServiceSmokeTest.Run",
    "InventoryDisplaySpecSmokeTest.Run",
    "InventoryGridLayoutAssetValidatorTest.Run",
    "RewardSystemSmokeTest.Run",
    "CombatLootDropTest.Run",
    "DungeonNodeTypesSmokeTest.Run",
    "MonsterActionAITest.Run",
    "DungeonStairsProgressionTest.Run",
    "VisualAssetSmokeTest.Run"
)

if ($SkipUnity) {
    $skipPayload = [ordered]@{
        Status = "Skipped"
        ErrorCount = 0
        WarningCount = 1
        Reason = "-SkipUnity was specified; ConfigValidator and Unity smoke tests were not executed."
    }
    Write-JsonFile -Path $configValidationPath -Value $skipPayload
    $step = New-Step "ConfigValidator"
    $start = Get-Date
    Complete-Step -Step $step -Status "Skipped" -Started $start -WarningCount 1 -Output $configValidationPath -Logs @($skipPayload.Reason)
    $warnings.Add($skipPayload.Reason) | Out-Null

    $smokePayload = [ordered]@{
        Status = "Skipped"
        ErrorCount = 0
        WarningCount = 1
        Reason = "-SkipUnity was specified; Unity smoke tests were not executed."
        Tests = @($smokeTests | ForEach-Object {
            [ordered]@{
                Test = $_
                Status = "Skipped"
                ErrorCount = 0
                WarningCount = 1
            }
        })
    }
    Write-JsonFile -Path $smokeTestsPath -Value $smokePayload
    $step = New-Step "UnitySmokeTests"
    $start = Get-Date
    Complete-Step -Step $step -Status "Skipped" -Started $start -WarningCount 1 -Output $smokeTestsPath -Logs @($smokePayload.Reason)
    $warnings.Add($smokePayload.Reason) | Out-Null
} elseif (-not $unityRunning) {
    $blockedReason = "Unity Editor is not running; AutoTestDaemon cannot execute runtime validation."
    $configPayload = [ordered]@{
        Status = "Blocked"
        ErrorCount = 1
        WarningCount = 0
        Reason = $blockedReason
    }
    Write-JsonFile -Path $configValidationPath -Value $configPayload
    $step = New-Step "ConfigValidator"
    $start = Get-Date
    Complete-Step -Step $step -Status "Blocked" -Started $start -ErrorCount 1 -Output $configValidationPath -Logs @($blockedReason)
    $errors.Add("ConfigValidator blocked: $blockedReason") | Out-Null

    $smokePayload = [ordered]@{
        Status = "Blocked"
        ErrorCount = 1
        WarningCount = 0
        Reason = $blockedReason
        Tests = @($smokeTests | ForEach-Object {
            [ordered]@{
                Test = $_
                Status = "Blocked"
                ErrorCount = 1
                WarningCount = 0
            }
        })
    }
    Write-JsonFile -Path $smokeTestsPath -Value $smokePayload
    $step = New-Step "UnitySmokeTests"
    $start = Get-Date
    Complete-Step -Step $step -Status "Blocked" -Started $start -ErrorCount 1 -Output $smokeTestsPath -Logs @($blockedReason)
    $errors.Add("UnitySmokeTests blocked: $blockedReason") | Out-Null
} else {
    $configTestOutput = Join-Path $outputPath "config_validation.raw.json"
    $configStepStart = Get-Date
    $configResult = Invoke-UnityTest -TestName "ConfigValidationSmokeTest.Run" -OutputPath $configTestOutput
    $testReport = $configResult.Details.TestReport
    $counts = Extract-ConfigValidationCounts -TestReport $testReport
    $configPayload = [ordered]@{
        Status = $configResult.Status
        ErrorCount = $counts.ErrorCount
        WarningCount = $counts.WarningCount
        Logs = $configResult.Logs
        Details = $configResult.Details
    }
    if ($configResult.Status -eq "Blocked") {
        $configPayload.ErrorCount = 1
    }
    Write-JsonFile -Path $configValidationPath -Value $configPayload

    $step = New-Step "ConfigValidator"
    Complete-Step -Step $step -Status $configPayload.Status -Started $configStepStart -ErrorCount $configPayload.ErrorCount -WarningCount $configPayload.WarningCount -Output $configValidationPath -Logs $configResult.Logs -Details $configPayload.Details

    if ($configPayload.ErrorCount -gt 0) {
        $errors.Add("ConfigValidator reported $($configPayload.ErrorCount) error(s).") | Out-Null
    }
    if ($configPayload.WarningCount -gt 0) {
        $warnings.Add("ConfigValidator reported $($configPayload.WarningCount) warning(s).") | Out-Null
    }

    $testResults = @()
    $smokeErrorCount = 0
    $smokeWarningCount = 0
    $smokeStatus = "Passed"
    $smokeLogs = @()
    $smokeStepStart = Get-Date

    foreach ($test in $smokeTests) {
        $singleOutput = Join-Path $outputPath ("smoke_" + ($test -replace "[^A-Za-z0-9_.-]", "_") + ".json")
        $result = Invoke-UnityTest -TestName $test -OutputPath $singleOutput
        $testResults += [ordered]@{
            Test = $test
            Status = $result.Status
            ErrorCount = $result.ErrorCount
            WarningCount = $result.WarningCount
            Output = Convert-ToRepoPath $singleOutput
            Details = $result.Details
        }
        $smokeLogs += $result.Logs
        $smokeErrorCount += $result.ErrorCount
        $smokeWarningCount += $result.WarningCount
        if ($result.Status -eq "Blocked") {
            $smokeStatus = "Blocked"
        } elseif ($result.Status -eq "Failed" -and $smokeStatus -ne "Blocked") {
            $smokeStatus = "Failed"
        }
    }

    $smokePayload = [ordered]@{
        Status = $smokeStatus
        ErrorCount = $smokeErrorCount
        WarningCount = $smokeWarningCount
        Tests = $testResults
    }
    Write-JsonFile -Path $smokeTestsPath -Value $smokePayload
    $step = New-Step "UnitySmokeTests"
    Complete-Step -Step $step -Status $smokeStatus -Started $smokeStepStart -ErrorCount $smokeErrorCount -WarningCount $smokeWarningCount -Output $smokeTestsPath -Logs $smokeLogs -Details ([ordered]@{
        Tests = $testResults
    })

    if ($smokeErrorCount -gt 0) {
        $errors.Add("Unity smoke tests reported $smokeErrorCount error(s).") | Out-Null
    }
    if ($smokeWarningCount -gt 0) {
        $warnings.Add("Unity smoke tests reported $smokeWarningCount warning(s).") | Out-Null
    }
}

$uiValidationJsonPath = Join-Path $outputPath "ui_validation.json"
$uiHandoffPath = Join-Path $outputPath "ui_design_handoff.md"
$uiScript = Find-ProjectFile -Name "Validate-UIDesign.ps1" -PreferredPathPart "/tools/"
$uiStep = New-Step "UIDesignValidation"
$uiStart = Get-Date
if ([string]::IsNullOrWhiteSpace($uiScript) -or -not (Test-Path -LiteralPath $uiScript)) {
    $uiResult = [pscustomobject]@{
        ExitCode = 1
        Logs = @("Validate-UIDesign.ps1 was not found.")
    }
} else {
    $uiResult = Invoke-ChildPowerShell -ScriptPath $uiScript -Arguments @("-OutputPath", $uiHandoffPath)
}
$uiStatus = if ($uiResult.ExitCode -eq 0) { "Passed" } else { "Failed" }
$uiErrorCount = if ($uiResult.ExitCode -eq 0) { 0 } else { 1 }
$uiPayload = [ordered]@{
    Status = $uiStatus
    ErrorCount = $uiErrorCount
    WarningCount = 0
    ExitCode = $uiResult.ExitCode
    HandoffPath = Convert-ToRepoPath $uiHandoffPath
    Logs = $uiResult.Logs
}
Write-JsonFile -Path $uiValidationJsonPath -Value $uiPayload
Complete-Step -Step $uiStep -Status $uiStatus -Started $uiStart -ErrorCount $uiErrorCount -Output $uiValidationJsonPath -Logs $uiResult.Logs -Details ([ordered]@{
    HandoffPath = Convert-ToRepoPath $uiHandoffPath
    ExitCode = $uiResult.ExitCode
})
if ($uiErrorCount -gt 0) {
    $errors.Add("UIDesignValidation failed.") | Out-Null
}

$artSummaryPath = Join-Path $outputPath "art_acceptance_summary.json"
if (-not $SkipArtAcceptance) {
    Invoke-ArtAcceptanceRun -LatestReportPath (Join-Path $repoRoot "UnityClient\Logs\ArtAcceptance\latest\report.json")
}

$artSummary = New-ArtAcceptanceSummary -OutputPath $artSummaryPath
$artStep = New-Step "ArtAcceptanceLatest"
$artStart = Get-Date
Complete-Step -Step $artStep -Status $artSummary.Status -Started $artStart -ErrorCount $artSummary.ErrorCount -WarningCount $artSummary.WarningCount -Output $artSummaryPath -Logs @("Read ArtAcceptance latest report.") -Details $artSummary
foreach ($item in @($artSummary.Errors)) {
    if ($item) {
        $errors.Add("ArtAcceptance: $item") | Out-Null
    }
}
foreach ($item in @($artSummary.Warnings)) {
    if ($item) {
        $warnings.Add("ArtAcceptance: $item") | Out-Null
    }
}

$hasFailed = $false
$hasBlocked = $false
foreach ($step in $steps) {
    if ($step.Status -eq "Failed") {
        $hasFailed = $true
    }
    if ($step.Status -eq "Blocked") {
        $hasBlocked = $true
    }
}

$finalStatus = "Passed"
if ($hasFailed) {
    $finalStatus = "Failed"
} elseif ($hasBlocked) {
    $finalStatus = "Blocked"
}

if ($Strict -and $warnings.Count -gt 0 -and $finalStatus -eq "Passed") {
    $finalStatus = "Failed"
    $errors.Add("Strict mode failed because warningCount is greater than zero.") | Out-Null
}

$totalStepErrors = 0
$totalStepWarnings = 0
foreach ($step in $steps) {
    $totalStepErrors += [int]$step.ErrorCount
    $totalStepWarnings += [int]$step.WarningCount
}

$finishedAt = [DateTimeOffset]::Now
$reportJsonPath = Join-Path $outputPath "report.json"
$reportMdPath = Join-Path $outputPath "report.md"
$report = [ordered]@{
    Status = $finalStatus
    RunID = $runId
    StartedAt = $startedAt.ToString("o")
    FinishedAt = $finishedAt.ToString("o")
    DurationMs = [int][Math]::Round(($finishedAt - $startedAt).TotalMilliseconds)
    Strict = [bool]$Strict
    SkipUnity = [bool]$SkipUnity
    SkipArtAcceptance = [bool]$SkipArtAcceptance
    SeedProfile = $SeedProfile
    OutputRoot = Convert-ToRepoPath $outputPath
    Steps = $steps
    ErrorCount = $totalStepErrors
    WarningCount = $totalStepWarnings
    Errors = @($errors)
    Warnings = @($warnings)
    ArtAcceptanceLatest = $artSummary
}

Write-JsonFile -Path $reportJsonPath -Value $report
Write-MarkdownReport -Path $reportMdPath -Report ([pscustomobject]$report)

if ($History) {
    $parent = Split-Path -Parent $outputPath
    $historyRoot = Join-Path $parent "history"
    $historyPath = Join-Path $historyRoot $runId
    New-Item -ItemType Directory -Force -Path $historyPath | Out-Null
    Get-ChildItem -LiteralPath $outputPath -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $historyPath $_.Name) -Force
    }
}

Write-Host "[P0Validation] Status=$finalStatus Errors=$($errors.Count) Warnings=$($warnings.Count)"
Write-Host "[P0Validation] Report=$(Convert-ToRepoPath $reportJsonPath)"

if ($finalStatus -ne "Passed") {
    exit 1
}

exit 0
