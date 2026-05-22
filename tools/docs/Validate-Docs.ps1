param(
    [string]$IndexJson = "docs_index.json"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "validate_docs.py"

python $pythonScript --index $IndexJson
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
