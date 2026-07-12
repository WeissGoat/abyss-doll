[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet("program","art","release")][string]$Domain,[Parameter(Mandatory)][string]$ProfileId,[string]$RunId="",[string]$RepoRoot=(Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath,[switch]$PassThru)
$ErrorActionPreference="Stop"
$files=@{program="program_validation_profiles.json";art="art_validation_profiles.json";release="release_validation_profiles.json"}
$folders=@{program="program-runs";art="art-runs";release="release-runs"}
$profiles=(Get-Content (Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation/$($files[$Domain])") -Raw -Encoding UTF8 | ConvertFrom-Json).profiles
$profile=$profiles | Where-Object id -eq $ProfileId | Select-Object -First 1
if(-not $profile){throw "unknown $Domain ProfileId: $ProfileId"}
if($RunId-eq""){$RunId="${Domain}_{0}_{1}" -f (Get-Date -Format "yyyyMMdd_HHmmss"),$ProfileId}
if($RunId-notmatch("^"+[regex]::Escape($Domain)+"_[A-Za-z0-9_-]+$")){throw "invalid $Domain RunId: $RunId"}
$root=Join-Path $RepoRoot "UnityClient/Logs/P3Validation/$($folders[$Domain])/$RunId"
if(Test-Path (Join-Path $root "request.json")){throw "RunId already exists: $RunId"}
foreach($d in @("steps","unity","screenshots","source_reports","iterations")){New-Item -ItemType Directory -Force (Join-Path $root $d)|Out-Null}
$request=[ordered]@{schema_version="p3-validation/request@2";validation_domain=$Domain;run_id=$RunId;profile_id=$ProfileId;profile_version=$profile.version;created_at=(Get-Date).ToUniversalTime().ToString("o");evidence_root=$root}
$tmp=Join-Path $root "request.json.tmp";$request|ConvertTo-Json -Depth 8|Set-Content $tmp -Encoding UTF8;Move-Item $tmp (Join-Path $root "request.json")
$result=[pscustomobject]@{Domain=$Domain;RunId=$RunId;ProfileId=$ProfileId;EvidenceRoot=$root};if($PassThru){$result}else{$result|ConvertTo-Json}
