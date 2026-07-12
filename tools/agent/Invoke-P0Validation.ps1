param(
    [switch]$SkipUnity,
    [switch]$SkipArtAcceptance = $true,
    [switch]$Strict,
    [string]$OutputRoot = "UnityClient/Logs/P0Validation/latest",
    [switch]$History,
    [string]$SeedProfile = "p0_core",
    [int]$TimeoutSeconds = 180,
    [int]$ArtAcceptanceTimeoutSeconds = 240,
    [string]$ParentRunId = "",
    [string]$EvidenceRoot = "",
    [switch]$StaticOnly
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).ProviderPath
if (-not [string]::IsNullOrWhiteSpace($EvidenceRoot)) { $OutputRoot = Join-Path $EvidenceRoot "source_reports/p0" }
if ($StaticOnly) { $SkipUnity = $true; $SkipArtAcceptance = $true }
$runId = Get-Date -Format "yyyyMMdd_HHmmss"
$startedAt = [DateTimeOffset]::Now
$steps = New-Object System.Collections.Generic.List[object]
$errors = New-Object System.Collections.Generic.List[string]
$warnings = New-Object System.Collections.Generic.List[string]
$validationLimitations = New-Object System.Collections.Generic.List[object]

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
        StatusCode = "pending"
        StartedAt = ""
        FinishedAt = ""
        DurationMs = 0
        ErrorCount = 0
        WarningCount = 0
        BlockedCount = 0
        LimitationCount = 0
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
        [int]$BlockedCount = 0,
        [int]$LimitationCount = 0,
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
    $Step.BlockedCount = $BlockedCount
    $Step.LimitationCount = $LimitationCount
    $Step.Output = Convert-ToRepoPath $Output
    $Step.Logs = @($Logs)
    $Step.StatusCode = $Status.ToLowerInvariant()
    if ($null -ne $Details) {
        $Step.Details = $Details
    }

    $steps.Add([pscustomobject]$Step) | Out-Null
}

function Add-ValidationLimitation {
    param(
        [string]$Code,
        [string]$Step,
        [string]$Scope,
        [string]$Message,
        [bool]$Blocking = $true
    )

    if ([string]::IsNullOrWhiteSpace($Code)) {
        $Code = "validation_limited:Unknown"
    }

    foreach ($existing in $validationLimitations) {
        if ($existing.Code -eq $Code -and $existing.Step -eq $Step -and $existing.Scope -eq $Scope) {
            return
        }
    }

    $validationLimitations.Add([pscustomobject][ordered]@{
        Code = $Code
        Step = $Step
        Scope = $Scope
        Message = $Message
        Blocking = $Blocking
    }) | Out-Null
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
    $blockedCount = 0
    $limitationCount = 0
    $details = [ordered]@{
        Test = $TestName
        ExitCode = $result.ExitCode
        TestReportPath = Convert-ToRepoPath $stableTestReportPath
        TestReport = $testReport
    }

    if ($null -eq $testReport) {
        $blockedCount = 1
        $limitationCount = 1
        $details.ReasonCode = "validation_limited:UnityTestReportMissing"
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
        BlockedCount = $blockedCount
        LimitationCount = $limitationCount
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
        $reason = "Unity Editor is not running; cannot rerun ArtAcceptance."
        $reasonCode = "validation_limited:UnityEditorNotRunning"
        Complete-Step -Step $step -Status "Blocked" -Started $start -BlockedCount 1 -LimitationCount 1 -Logs @($reason) -Details ([ordered]@{
            ReasonCode = $reasonCode
            Reason = $reason
            Trigger = "UnityClient/Logs/.art_acceptance_trigger"
        })
        Add-ValidationLimitation -Code $reasonCode -Step "ArtAcceptanceRerun" -Scope "ArtAcceptance" -Message $reason -Blocking $true
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
        $reason = "Timed out waiting for ArtAcceptance latest report."
        $reasonCode = "validation_limited:ArtAcceptanceTimeout"
        Complete-Step -Step $step -Status "Blocked" -Started $start -BlockedCount 1 -LimitationCount 1 -Logs @($reason) -Details ([ordered]@{
            ReasonCode = $reasonCode
            Reason = $reason
            Trigger = Convert-ToRepoPath $triggerFile
            TimeoutSeconds = $ArtAcceptanceTimeoutSeconds
        })
        Add-ValidationLimitation -Code $reasonCode -Step "ArtAcceptanceRerun" -Scope "ArtAcceptance" -Message $reason -Blocking $true
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
    $lines.Add(('* ErrorCount: `{0}`' -f $Report.ErrorCount)) | Out-Null
    $lines.Add(('* WarningCount: `{0}`' -f $Report.WarningCount)) | Out-Null
    $lines.Add(('* BlockedCount: `{0}`' -f $Report.BlockedCount)) | Out-Null
    $lines.Add(('* ValidationLimitations: `{0}`' -f (Get-Count $Report.ValidationLimitations))) | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("## Steps") | Out-Null
    $lines.Add("") | Out-Null
    $lines.Add("| Step | Status | Errors | Warnings | Blocked | Limited | Output |") | Out-Null
    $lines.Add("|---|---|---:|---:|---:|---:|---|") | Out-Null
    foreach ($step in $Report.Steps) {
        $lines.Add(('| `{0}` | `{1}` | {2} | {3} | {4} | {5} | `{6}` |' -f $step.Name, $step.Status, $step.ErrorCount, $step.WarningCount, $step.BlockedCount, $step.LimitationCount, $step.Output)) | Out-Null
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
    $lines.Add("## Validation Limitations") | Out-Null
    if ((Get-Count $Report.ValidationLimitations) -eq 0) {
        $lines.Add("") | Out-Null
        $lines.Add("None.") | Out-Null
    } else {
        foreach ($item in $Report.ValidationLimitations) {
            $lines.Add(('* `{0}` step=`{1}` scope=`{2}` blocking=`{3}` - {4}' -f $item.Code, $item.Step, $item.Scope, $item.Blocking, $item.Message)) | Out-Null
        }
    }

    $lines.Add("") | Out-Null
    $lines.Add("## Main Flow Smoke Registry") | Out-Null
    $mainFlowTests = @($Report.SmokeTestRegistry | Where-Object { $_.IsMainFlow })
    if ($mainFlowTests.Count -eq 0) {
        $lines.Add("") | Out-Null
        $lines.Add("None.") | Out-Null
    } else {
        foreach ($item in $mainFlowTests) {
            $lines.Add(('* `{0}` category=`{1}` required=`{2}`' -f $item.Test, $item.Category, $item.Required)) | Out-Null
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
$smokeTestDefinitions = @(
    [ordered]@{ Test = "InventoryInteractionServiceSmokeTest.Run"; Category = "Inventory"; IsMainFlow = $false; Required = $true },
    [ordered]@{ Test = "InventoryDisplaySpecSmokeTest.Run"; Category = "Inventory"; IsMainFlow = $false; Required = $true },
    [ordered]@{ Test = "InventoryGridLayoutAssetValidatorTest.Run"; Category = "Inventory"; IsMainFlow = $false; Required = $true },
    [ordered]@{ Test = "RewardSystemSmokeTest.Run"; Category = "RewardLoot"; IsMainFlow = $false; Required = $true },
    [ordered]@{ Test = "CombatLootDropTest.Run"; Category = "RewardLoot"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "DungeonNodeTypesSmokeTest.Run"; Category = "MainFlowDungeon"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "MonsterActionAITest.Run"; Category = "Combat"; IsMainFlow = $false; Required = $true },
    [ordered]@{ Test = "DungeonStairsProgressionTest.Run"; Category = "MainFlowDungeon"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "MainFlowGoldenPathSmokeTest.Run"; Category = "MainFlowGoldenPath"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "TownEconomyServiceSmokeTest.Run"; Category = "MainFlowEconomy"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "WorkshopSmokeTest.Run"; Category = "MainFlowEconomy"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "MaintenanceServiceSmokeTest.Run"; Category = "MainFlowGrowth"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "GrowthFeedbackServiceSmokeTest.Run"; Category = "MainFlowGrowth"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "WorkshopFormalV1PanelBindingSmokeTest.Run"; Category = "MainFlowGrowth"; IsMainFlow = $true; Required = $true },
    [ordered]@{ Test = "VisualAssetSmokeTest.Run"; Category = "VisualAsset"; IsMainFlow = $false; Required = $true }
)
$smokeTests = @($smokeTestDefinitions | ForEach-Object { $_.Test })

if ($SkipUnity) {
    $skipReasonCode = "validation_limited:SkipUnityRequested"
    $skipPayload = [ordered]@{
        Status = "Skipped"
        ErrorCount = 0
        WarningCount = 1
        BlockedCount = 0
        LimitationCount = 1
        ReasonCode = $skipReasonCode
        Reason = "-SkipUnity was specified; ConfigValidator and Unity smoke tests were not executed."
    }
    Write-JsonFile -Path $configValidationPath -Value $skipPayload
    $step = New-Step "ConfigValidator"
    $start = Get-Date
    Complete-Step -Step $step -Status "Skipped" -Started $start -WarningCount 1 -LimitationCount 1 -Output $configValidationPath -Logs @($skipPayload.Reason) -Details ([ordered]@{
        ReasonCode = $skipReasonCode
        Reason = $skipPayload.Reason
    })
    Add-ValidationLimitation -Code $skipReasonCode -Step "ConfigValidator" -Scope "ConfigValidator" -Message $skipPayload.Reason -Blocking $false
    $warnings.Add($skipPayload.Reason) | Out-Null

    $smokePayload = [ordered]@{
        Status = "Skipped"
        ErrorCount = 0
        WarningCount = 1
        BlockedCount = 0
        LimitationCount = 1
        ReasonCode = $skipReasonCode
        Reason = "-SkipUnity was specified; Unity smoke tests were not executed."
        RegisteredTests = @($smokeTestDefinitions)
        Tests = @($smokeTestDefinitions | ForEach-Object {
            [ordered]@{
                Test = $_.Test
                Category = $_.Category
                IsMainFlow = $_.IsMainFlow
                Required = $_.Required
                Status = "Skipped"
                ErrorCount = 0
                WarningCount = 1
                BlockedCount = 0
                LimitationCount = 1
                ReasonCode = $skipReasonCode
            }
        })
    }
    Write-JsonFile -Path $smokeTestsPath -Value $smokePayload
    $step = New-Step "UnitySmokeTests"
    $start = Get-Date
    Complete-Step -Step $step -Status "Skipped" -Started $start -WarningCount 1 -LimitationCount 1 -Output $smokeTestsPath -Logs @($smokePayload.Reason) -Details ([ordered]@{
        ReasonCode = $skipReasonCode
        RegisteredTests = @($smokeTestDefinitions)
    })
    Add-ValidationLimitation -Code $skipReasonCode -Step "UnitySmokeTests" -Scope "UnitySmokeTests" -Message $smokePayload.Reason -Blocking $false
    $warnings.Add($smokePayload.Reason) | Out-Null
} elseif (-not $unityRunning) {
    $blockedReason = "Unity Editor is not running; AutoTestDaemon cannot execute runtime validation."
    $blockedReasonCode = "validation_limited:UnityEditorNotRunning"
    $configPayload = [ordered]@{
        Status = "Blocked"
        ErrorCount = 0
        WarningCount = 0
        BlockedCount = 1
        LimitationCount = 1
        ReasonCode = $blockedReasonCode
        Reason = $blockedReason
    }
    Write-JsonFile -Path $configValidationPath -Value $configPayload
    $step = New-Step "ConfigValidator"
    $start = Get-Date
    Complete-Step -Step $step -Status "Blocked" -Started $start -BlockedCount 1 -LimitationCount 1 -Output $configValidationPath -Logs @($blockedReason) -Details ([ordered]@{
        ReasonCode = $blockedReasonCode
        Reason = $blockedReason
    })
    Add-ValidationLimitation -Code $blockedReasonCode -Step "ConfigValidator" -Scope "ConfigValidator" -Message $blockedReason -Blocking $true

    $smokePayload = [ordered]@{
        Status = "Blocked"
        ErrorCount = 0
        WarningCount = 0
        BlockedCount = 1
        LimitationCount = 1
        ReasonCode = $blockedReasonCode
        Reason = $blockedReason
        RegisteredTests = @($smokeTestDefinitions)
        Tests = @($smokeTestDefinitions | ForEach-Object {
            [ordered]@{
                Test = $_.Test
                Category = $_.Category
                IsMainFlow = $_.IsMainFlow
                Required = $_.Required
                Status = "Blocked"
                ErrorCount = 0
                WarningCount = 0
                BlockedCount = 1
                LimitationCount = 1
                ReasonCode = $blockedReasonCode
            }
        })
    }
    Write-JsonFile -Path $smokeTestsPath -Value $smokePayload
    $step = New-Step "UnitySmokeTests"
    $start = Get-Date
    Complete-Step -Step $step -Status "Blocked" -Started $start -BlockedCount 1 -LimitationCount 1 -Output $smokeTestsPath -Logs @($blockedReason) -Details ([ordered]@{
        ReasonCode = $blockedReasonCode
        RegisteredTests = @($smokeTestDefinitions)
    })
    Add-ValidationLimitation -Code $blockedReasonCode -Step "UnitySmokeTests" -Scope "UnitySmokeTests" -Message $blockedReason -Blocking $true
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
        BlockedCount = $configResult.BlockedCount
        LimitationCount = $configResult.LimitationCount
        Logs = $configResult.Logs
        Details = $configResult.Details
    }
    if ($configResult.Status -eq "Blocked") {
        $configPayload.ErrorCount = 0
        $configPayload.BlockedCount = [Math]::Max(1, [int]$configPayload.BlockedCount)
        $configPayload.LimitationCount = [Math]::Max(1, [int]$configPayload.LimitationCount)
        Add-ValidationLimitation -Code $configResult.Details.ReasonCode -Step "ConfigValidator" -Scope "ConfigValidator" -Message $configResult.Details.BlockReason -Blocking $true
    }
    Write-JsonFile -Path $configValidationPath -Value $configPayload

    $step = New-Step "ConfigValidator"
    Complete-Step -Step $step -Status $configPayload.Status -Started $configStepStart -ErrorCount $configPayload.ErrorCount -WarningCount $configPayload.WarningCount -BlockedCount $configPayload.BlockedCount -LimitationCount $configPayload.LimitationCount -Output $configValidationPath -Logs $configResult.Logs -Details $configPayload.Details

    if ($configPayload.ErrorCount -gt 0) {
        $errors.Add("ConfigValidator reported $($configPayload.ErrorCount) error(s).") | Out-Null
    }
    if ($configPayload.WarningCount -gt 0) {
        $warnings.Add("ConfigValidator reported $($configPayload.WarningCount) warning(s).") | Out-Null
    }

    $testResults = @()
    $smokeErrorCount = 0
    $smokeWarningCount = 0
    $smokeBlockedCount = 0
    $smokeLimitationCount = 0
    $smokeStatus = "Passed"
    $smokeLogs = @()
    $smokeStepStart = Get-Date

    foreach ($definition in $smokeTestDefinitions) {
        $test = $definition.Test
        $singleOutput = Join-Path $outputPath ("smoke_" + ($test -replace "[^A-Za-z0-9_.-]", "_") + ".json")
        $result = Invoke-UnityTest -TestName $test -OutputPath $singleOutput
        $testResults += [ordered]@{
            Test = $test
            Category = $definition.Category
            IsMainFlow = $definition.IsMainFlow
            Required = $definition.Required
            Status = $result.Status
            ErrorCount = $result.ErrorCount
            WarningCount = $result.WarningCount
            BlockedCount = $result.BlockedCount
            LimitationCount = $result.LimitationCount
            ReasonCode = if ($result.Details.ReasonCode) { $result.Details.ReasonCode } else { "" }
            Output = Convert-ToRepoPath $singleOutput
            Details = $result.Details
        }
        $smokeLogs += $result.Logs
        $smokeErrorCount += $result.ErrorCount
        $smokeWarningCount += $result.WarningCount
        $smokeBlockedCount += $result.BlockedCount
        $smokeLimitationCount += $result.LimitationCount
        if ($result.Status -eq "Blocked") {
            $smokeStatus = "Blocked"
            Add-ValidationLimitation -Code $result.Details.ReasonCode -Step "UnitySmokeTests" -Scope $test -Message $result.Details.BlockReason -Blocking $true
        } elseif ($result.Status -eq "Failed" -and $smokeStatus -ne "Blocked") {
            $smokeStatus = "Failed"
        }
    }

    $smokePayload = [ordered]@{
        Status = $smokeStatus
        ErrorCount = $smokeErrorCount
        WarningCount = $smokeWarningCount
        BlockedCount = $smokeBlockedCount
        LimitationCount = $smokeLimitationCount
        RegisteredTests = @($smokeTestDefinitions)
        Tests = $testResults
    }
    Write-JsonFile -Path $smokeTestsPath -Value $smokePayload
    $step = New-Step "UnitySmokeTests"
    Complete-Step -Step $step -Status $smokeStatus -Started $smokeStepStart -ErrorCount $smokeErrorCount -WarningCount $smokeWarningCount -BlockedCount $smokeBlockedCount -LimitationCount $smokeLimitationCount -Output $smokeTestsPath -Logs $smokeLogs -Details ([ordered]@{
        RegisteredTests = @($smokeTestDefinitions)
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
$passedStepCount = 0
$failedStepCount = 0
$blockedStepCount = 0
$skippedStepCount = 0
foreach ($step in $steps) {
    if ($step.Status -eq "Passed") {
        $passedStepCount++
    }
    if ($step.Status -eq "Failed") {
        $hasFailed = $true
        $failedStepCount++
    }
    if ($step.Status -eq "Blocked") {
        $hasBlocked = $true
        $blockedStepCount++
    }
    if ($step.Status -eq "Skipped") {
        $skippedStepCount++
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
$totalStepBlocked = 0
$totalStepLimitations = 0
foreach ($step in $steps) {
    $totalStepErrors += [int]$step.ErrorCount
    $totalStepWarnings += [int]$step.WarningCount
    $totalStepBlocked += [int]$step.BlockedCount
    $totalStepLimitations += [int]$step.LimitationCount
}

$finishedAt = [DateTimeOffset]::Now
$reportJsonPath = Join-Path $outputPath "report.json"
$reportMdPath = Join-Path $outputPath "report.md"
$stepArray = @()
foreach ($step in $steps) {
    $stepArray += $step
}

$errorArray = @()
foreach ($item in $errors) {
    $errorArray += $item
}

$warningArray = @()
foreach ($item in $warnings) {
    $warningArray += $item
}

$limitationArray = @()
foreach ($item in $validationLimitations) {
    $limitationArray += $item
}

$report = New-Object System.Collections.Specialized.OrderedDictionary
$report.Add("Status", $finalStatus)
$report.Add("RunID", $runId)
$report.Add("StartedAt", $startedAt.ToString("o"))
$report.Add("FinishedAt", $finishedAt.ToString("o"))
$report.Add("DurationMs", [int][Math]::Round(($finishedAt - $startedAt).TotalMilliseconds))
$report.Add("Strict", [bool]$Strict)
$report.Add("SkipUnity", [bool]$SkipUnity)
$report.Add("SkipArtAcceptance", [bool]$SkipArtAcceptance)
$report.Add("SeedProfile", $SeedProfile)
$report.Add("OutputRoot", (Convert-ToRepoPath $outputPath))
$report.Add("Steps", $stepArray)
$report.Add("ErrorCount", $totalStepErrors)
$report.Add("WarningCount", $totalStepWarnings)
$report.Add("BlockedCount", $totalStepBlocked)
$report.Add("LimitationCount", $totalStepLimitations)
$report.Add("PassedStepCount", $passedStepCount)
$report.Add("FailedStepCount", $failedStepCount)
$report.Add("BlockedStepCount", $blockedStepCount)
$report.Add("SkippedStepCount", $skippedStepCount)
$report.Add("ValidationLimitations", $limitationArray)
$report.Add("SmokeTestRegistry", @($smokeTestDefinitions))
$report.Add("MainFlowSmokeTests", @($smokeTestDefinitions | Where-Object { $_.IsMainFlow }))
$report.Add("Errors", $errorArray)
$report.Add("Warnings", $warningArray)
$report.Add("ArtAcceptanceLatest", $artSummary)

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

Write-Host "[P0Validation] Status=$finalStatus Errors=$totalStepErrors Warnings=$totalStepWarnings Blocked=$totalStepBlocked Limitations=$($validationLimitations.Count)"
Write-Host "[P0Validation] Report=$(Convert-ToRepoPath $reportJsonPath)"

if ($finalStatus -ne "Passed") {
    exit 1
}

exit 0
