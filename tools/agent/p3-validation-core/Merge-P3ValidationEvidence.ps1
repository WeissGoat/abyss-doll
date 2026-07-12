[CmdletBinding()]param([Parameter(Mandatory)][ValidateSet("program","art")][string]$Domain,[Parameter(Mandatory)][string]$RunId,[string]$RepoRoot=(Resolve-Path(Join-Path $PSScriptRoot "..\..\..")).ProviderPath,[ValidateSet("NotStarted","Passed","Failed")][string]$OwnerValidation="NotStarted",[ValidateSet("NotRequired","Required","Passed","Failed")][string]$ExternalReview="NotRequired",[switch]$PassThru)
$ErrorActionPreference="Stop"
$folders=@{program="program-runs";art="art-runs"};$files=@{program="program_validation_profiles.json";art="art_validation_profiles.json"}
$root=Join-Path $RepoRoot "UnityClient/Logs/P3Validation/$($folders[$Domain])/$RunId"
$request=Get-Content (Join-Path $root "request.json") -Raw -Encoding UTF8 | ConvertFrom-Json
if($request.validation_domain-ne$Domain){throw "request domain mismatch"}
$profile=((Get-Content (Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation/$($files[$Domain])") -Raw -Encoding UTF8|ConvertFrom-Json).profiles|Where-Object id -eq $request.profile_id|Select-Object -First 1)
$steps=@(Get-ChildItem (Join-Path $root "steps") -Recurse -Filter result.json -ErrorAction SilentlyContinue|ForEach-Object{Get-Content $_.FullName -Raw -Encoding UTF8|ConvertFrom-Json})
foreach($step in $steps){if($step.validation_domain-notin@($Domain,"infrastructure")){throw "cross-domain step rejected: $($step.step_id)"};foreach($a in @($step.artifacts)){$path=Join-Path $root $a.path;if(-not(Test-Path $path)){throw "artifact missing: $($a.path)"};$file=Get-Item $path;if($file.Length-ne$a.size-or$file.Length-lt1){throw "artifact size mismatch: $($a.path)"};$hash=(Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant();if($hash-ne$a.sha256){throw "artifact hash mismatch: $($a.path)"}}}
$required=@($steps|Where-Object required -eq $true);$missing=@($profile.required_steps|Where-Object{$_-notin@($required.step_id)})
if($required.status-contains"Failed"){$automation="Failed"}elseif($missing.Count-gt0-or$required.status-contains"Blocked"){$automation="Blocked"}elseif($required.status-contains"Limited"){$automation="Limited"}elseif($required.status-contains"Cancelled"){$automation="Cancelled"}else{$automation="Passed"}
if($Domain-eq"art"-and@($profile.external_review_required).Count-gt0-and$ExternalReview-eq"NotRequired"){$ExternalReview="Required"}
$ceiling=if($automation-ne"Passed"){"evidence_collected"}elseif($OwnerValidation-ne"Passed"){"automation_passed"}elseif($ExternalReview-eq"Passed"){"externally_reviewed"}else{"owner_validated"}
$summary=[ordered]@{schema_version="p3-validation/summary@2";validation_domain=$Domain;run_id=$RunId;profile_id=$profile.id;automation_status=$automation;owner_validation=$OwnerValidation;external_review=$ExternalReview;claim_ceiling=$ceiling;missing_required_steps=$missing;steps=$steps}
$tmp=Join-Path $root "validation-summary.json.tmp";$summary|ConvertTo-Json -Depth 20|Set-Content $tmp -Encoding UTF8;Move-Item $tmp (Join-Path $root "validation-summary.json") -Force
$result=[pscustomobject]@{AutomationStatus=$automation;ExternalReview=$ExternalReview;ClaimCeiling=$ceiling};if($PassThru){$result}else{$result|ConvertTo-Json}
