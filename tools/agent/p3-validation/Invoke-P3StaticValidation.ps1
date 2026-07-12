[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RunId,
    [Parameter(Mandatory)][string]$ProfileId,
    [string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath,
    [switch]$PassThru
)
$ErrorActionPreference="Stop"
$profiles=(Get-Content (Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json") -Raw -Encoding UTF8|ConvertFrom-Json).profiles
$profile=$profiles|Where-Object id -eq $ProfileId|Select-Object -First 1
if(-not $profile){throw "unknown ProfileId: $ProfileId"}
$root=Join-Path $RepoRoot "UnityClient/Logs/P3Validation/runs/$RunId"
if(-not(Test-Path (Join-Path $root "request.json"))){throw "validation run missing: $RunId"}
$results=@()
$uiValidateScript=(Get-ChildItem (Join-Path $RepoRoot "tools") -Recurse -Filter "Validate-UIDesign.ps1" | Select-Object -First 1).FullName
$artManifestScript=(Get-ChildItem (Join-Path $RepoRoot "tools") -Recurse -Filter "Update-ArtManifest.ps1" | Select-Object -First 1).FullName
foreach($step in $profile.static_steps){
    $stepDir=Join-Path $root "steps/$step";New-Item -ItemType Directory -Force $stepDir|Out-Null
    $started=Get-Date; $status="Passed";$code=$null;$message=""
    try {
        switch($step){
            "config_sync" { & (Join-Path $RepoRoot "tools/config/Sync-Configs.ps1") -Clean | Out-String | Set-Content (Join-Path $stepDir "output.log") -Encoding UTF8 }
            "config_static_validate" { & (Join-Path $RepoRoot "tools/agent/Invoke-P0Validation.ps1") -SkipUnity -SkipArtAcceptance -OutputRoot (Join-Path $root "source_reports/p0_static") | Out-String | Set-Content (Join-Path $stepDir "output.log") -Encoding UTF8 }
            "ui_spec_validate" { if(-not $uiValidateScript){throw "Validate-UIDesign.ps1 not found"}; & $uiValidateScript | Out-String | Set-Content (Join-Path $stepDir "output.log") -Encoding UTF8 }
            "art_manifest_check" { if(-not $artManifestScript){throw "Update-ArtManifest.ps1 not found"}; & $artManifestScript -ManifestPath (Join-Path $stepDir "art_manifest.json") -MarkdownPath (Join-Path $stepDir "art_manifest.md") | Out-String | Set-Content (Join-Path $stepDir "output.log") -Encoding UTF8 }
            default { throw "unregistered static step: $step" }
        }
        if($LASTEXITCODE -and $LASTEXITCODE-ne 0){throw "step exited $LASTEXITCODE"}
    } catch { $status="Failed";$code="static_step_failed";$message=$_.Exception.Message }
    $result=[ordered]@{schema_version="p3-validation/step-result@1";run_id=$RunId;profile_id=$ProfileId;step_id=$step;required=$true;status=$status;code=$code;message=$message;started_at=$started.ToUniversalTime().ToString("o");finished_at=(Get-Date).ToUniversalTime().ToString("o");artifacts=@()}
    $result|ConvertTo-Json -Depth 8|Set-Content (Join-Path $stepDir "result.json.tmp") -Encoding UTF8;Move-Item (Join-Path $stepDir "result.json.tmp") (Join-Path $stepDir "result.json") -Force
    $results+=[pscustomobject]$result
}
if($PassThru){$results}else{$results|ConvertTo-Json -Depth 8}
