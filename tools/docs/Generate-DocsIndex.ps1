param(
    [string]$OutputMarkdown = "DOCS_INDEX.md",
    [string]$OutputJson = "docs_index.json"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$pythonScript = Join-Path $scriptDir "generate_docs_index.py"

python $pythonScript --markdown $OutputMarkdown --json $OutputJson
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
