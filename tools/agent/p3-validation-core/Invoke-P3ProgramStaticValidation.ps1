[CmdletBinding()]
param([Parameter(Mandatory=$true)][ValidateSet('config_sync','config_static_validate','ui_spec_validate')][string]$Step)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
switch($Step){
 'config_sync' { & (Join-Path $root 'tools/config/Sync-Configs.ps1') -Clean }
 'config_static_validate' { & (Join-Path $root 'tools/config/Validate-Configs.ps1') }
 'ui_spec_validate' { & (Join-Path $root 'tools/美术工具/Validate-UIDesign.ps1') }
 default { throw "unregistered program static step: $Step" }
}
