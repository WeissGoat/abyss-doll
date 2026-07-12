[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ProgramRunID,[Parameter(Mandatory=$true)][string]$ArtRunID,[Parameter(Mandatory=$true)][ValidateSet('vertical_slice_release','t0_release')][string]$ProfileId,[string]$ReleaseRunID)
$ErrorActionPreference='Stop'
if($PSBoundParameters.Keys | Where-Object { $_ -match 'Execute|Mutate|Test' }){throw 'release aggregation is read-only'}
$base=Resolve-Path (Join-Path $PSScriptRoot '../../../UnityClient/Logs/P3Validation')
$p=Join-Path $base "program-runs/$ProgramRunID/summary.json";$a=Join-Path $base "art-runs/$ArtRunID/summary.json"
if(!(Test-Path $p)-or !(Test-Path $a)){throw 'release blocked: missing input RunID summary'}
$program=Get-Content $p -Raw|ConvertFrom-Json;$art=Get-Content $a -Raw|ConvertFrom-Json
$pf=$program.input_fingerprint;$af=$art.input_fingerprint;$match=($pf -and $af -and $pf -eq $af)
$ps=if($program.AutomationStatus){$program.AutomationStatus}else{$program.automation_status};$as=if($art.AutomationStatus){$art.AutomationStatus}else{$art.automation_status};$review=if($art.ExternalReview){$art.ExternalReview}else{$art.external_review}
if($ps -eq 'Failed'-or $as -eq 'Failed'){$status='Failed'}elseif(!$match){$status='Blocked'}elseif($ps -eq 'Blocked'-or $as -eq 'Blocked'){$status='Blocked'}elseif($ps -eq 'Limited'-or $as -eq 'Limited'){$status='Limited'}elseif($review -ne 'Passed'){$status='ReviewRequired'}else{$status='Passed'}
if(!$ReleaseRunID){$ReleaseRunID='release_'+(Get-Date -Format yyyyMMdd_HHmmss)}
$out=Join-Path $base "release-runs/$ReleaseRunID";New-Item -ItemType Directory -Force $out|Out-Null
$summary=[ordered]@{schema_version='p3-validation/release-summary@1';release_run_id=$ReleaseRunID;profile_id=$ProfileId;program_run_id=$ProgramRunID;art_run_id=$ArtRunID;fingerprints_match=$match;release_status=$status}
$summary|ConvertTo-Json -Depth 8|Set-Content -Encoding UTF8 (Join-Path $out 'summary.json');[pscustomobject]$summary
