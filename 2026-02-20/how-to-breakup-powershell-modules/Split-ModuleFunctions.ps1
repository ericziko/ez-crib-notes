<#
.SYNOPSIS
    Extracts all top-level functions from a .psm1 file into individual .ps1
    files and rewrites the .psm1 with the dynamic loader pattern.

.DESCRIPTION
    1. Parses the module with the PowerShell AST.
    2. Writes each top-level function to Functions/Public/<FunctionName>.ps1
       relative to the module file.
    3. Removes all function bodies from the original .psm1 (keeping any
       module-level code such as #Requires, using namespace, Set-StrictMode,
       etc.) and appends the dynamic loader pattern.
    4. Creates a <ModuleName>.psm1.bak backup before touching the original.

    After running, move any private/internal helpers from Functions/Public/
    to Functions/Private/ — the loader handles both folders automatically.

.PARAMETER ModulePath
    Path to the .psm1 file to process.

.EXAMPLE
    .\Split-ModuleFunctions.ps1 -ModulePath .\MyModule.psm1

.EXAMPLE
    # Preview without writing anything
    .\Split-ModuleFunctions.ps1 -ModulePath .\MyModule.psm1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory, Position = 0)]
    [string]$ModulePath
)

# ── Validate input ────────────────────────────────────────────────────────────

if (-not (Test-Path $ModulePath -PathType Leaf)) {
    Write-Error "File not found: $ModulePath"
    return
}

$resolvedPath = (Resolve-Path $ModulePath).Path

if ([System.IO.Path]::GetExtension($resolvedPath) -ne '.psm1') {
    Write-Error "Expected a .psm1 file, got: $resolvedPath"
    return
}

# ── Parse using the PowerShell AST ───────────────────────────────────────────
# We parse from the raw string (not from file) so that Extent offsets are
# guaranteed to align with the in-memory string we will manipulate later.

Write-Host "Parsing: $resolvedPath" -ForegroundColor Cyan

$rawContent  = [System.IO.File]::ReadAllText($resolvedPath)
$tokens      = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseInput(
    $rawContent,
    [ref]$tokens,
    [ref]$parseErrors
)

if ($parseErrors.Count -gt 0) {
    Write-Warning "$($parseErrors.Count) parse error(s) — extraction may be incomplete:"
    foreach ($e in $parseErrors) {
        Write-Warning "  Line $($e.Extent.StartLineNumber): $($e.Message)"
    }
}

if (-not $ast.EndBlock) {
    Write-Error "Could not read the script body from: $resolvedPath"
    return
}

# ── Find top-level functions only ─────────────────────────────────────────────
# EndBlock.Statements contains only top-level statements, so nested functions
# (defined inside other functions) are automatically excluded.

$topLevelFunctions = $ast.EndBlock.Statements |
    Where-Object { $_ -is [System.Management.Automation.Language.FunctionDefinitionAst] }

if (-not $topLevelFunctions) {
    Write-Warning "No top-level functions found in $resolvedPath"
    return
}

Write-Host "Found $($topLevelFunctions.Count) top-level function(s):" -ForegroundColor Cyan
$topLevelFunctions | ForEach-Object { Write-Host "  - $($_.Name)" }

# ── Set up output directory ───────────────────────────────────────────────────

$moduleDir = Split-Path -Parent $resolvedPath
$outputDir = Join-Path $moduleDir 'Functions' 'Public'

# ── Step 1: Extract each function to its own file ────────────────────────────

if (-not $PSCmdlet.ShouldProcess($outputDir, 'Create Functions/Public and write .ps1 files')) {
    return
}

New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
Write-Host "`nExtracting to: $outputDir`n" -ForegroundColor Cyan

$extracted = 0
$skipped   = 0

foreach ($func in $topLevelFunctions) {
    $name       = $func.Name
    $outputFile = Join-Path $outputDir "$name.ps1"

    if (Test-Path $outputFile) {
        Write-Warning "  SKIP (already exists): $name.ps1 — delete it to re-extract"
        $skipped++
        continue
    }

    # Extent.Text is the verbatim source of the function including its keyword,
    # name, param block, and body — no reformatting applied.
    Set-Content -Path $outputFile -Value $func.Extent.Text -Encoding UTF8
    Write-Host "  OK  $name  ->  Functions/Public/$name.ps1" -ForegroundColor Green
    $extracted++
}

Write-Host ""
Write-Host "Extracted: $extracted   Skipped (already existed): $skipped" -ForegroundColor Cyan

# ── Guard: don't rewrite .psm1 if any function was skipped ───────────────────
# Skipped functions would be deleted from the .psm1 without being in a file.

if ($skipped -gt 0) {
    Write-Warning @"

Skipped $skipped function(s) because output files already exist.
The .psm1 has NOT been modified to avoid data loss.
Delete the conflicting files in $outputDir and re-run,
or move them to Functions/Private/ if they are private helpers.
"@
    return
}

if ($extracted -eq 0) {
    Write-Host "Nothing extracted — .psm1 not modified." -ForegroundColor Yellow
    return
}

# ── Step 2: Rewrite the .psm1 ────────────────────────────────────────────────

$backupPath = $resolvedPath + '.bak'

if (-not $PSCmdlet.ShouldProcess($resolvedPath, "Rewrite .psm1 with loader pattern (backup -> $backupPath)")) {
    return
}

# --- Build the loader pattern block ---
$loaderBlock = @'

# =============================================================================
# Dynamic function loader — generated by Split-ModuleFunctions.ps1
# Public  functions are exported; Private functions are internal helpers.
# To add a new function: drop a .ps1 file in Functions/Public or Functions/Private.
# =============================================================================

foreach ($folder in @('Private', 'Public')) {
    $path = Join-Path $PSScriptRoot 'Functions' $folder
    if (Test-Path $path) {
        Get-ChildItem -Path $path -Filter '*.ps1' -Recurse |
            ForEach-Object { . $_.FullName }
    }
}

Export-ModuleMember -Function (
    Get-ChildItem (Join-Path $PSScriptRoot 'Functions' 'Public') -Filter '*.ps1' -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty BaseName
)
'@

# --- Remove function definitions from the source ---
# Sort end-to-start so that removing text at higher offsets doesn't shift
# the offsets of functions that appear earlier in the file.
$orderedFunctions = $topLevelFunctions |
    Sort-Object { $_.Extent.StartOffset } -Descending

$newContent = $rawContent
foreach ($func in $orderedFunctions) {
    $start  = $func.Extent.StartOffset
    $length = $func.Extent.EndOffset - $func.Extent.StartOffset
    $newContent = $newContent.Remove($start, $length)
}

# --- Clean up: collapse runs of 3+ blank lines down to 2 ---
$newContent = $newContent -replace '(\r?\n[ \t]*){3,}', "`n`n"
$newContent = $newContent.TrimEnd()

# --- Warn if an existing Export-ModuleMember remains ---
if ($newContent -match '(?i)Export-ModuleMember') {
    Write-Warning "An existing Export-ModuleMember call was found in the non-function code."
    Write-Warning "Review the rewritten .psm1 and remove any duplicate Export-ModuleMember calls."
}

# --- Assemble final content ---
if ($newContent.Length -gt 0) {
    $finalContent = $newContent + "`n" + $loaderBlock.TrimEnd() + "`n"
} else {
    $finalContent = $loaderBlock.TrimStart().TrimEnd() + "`n"
}

# --- Backup then write ---
Copy-Item -Path $resolvedPath -Destination $backupPath -Force
Write-Host "Backup:  $backupPath" -ForegroundColor DarkGray

[System.IO.File]::WriteAllText($resolvedPath, $finalContent, [System.Text.Encoding]::UTF8)
Write-Host "Rewritten: $resolvedPath" -ForegroundColor Green

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host @"

All done.

  Functions extracted to : $outputDir
  Original backed up to  : $backupPath
  .psm1 rewritten with   : dynamic loader pattern

Next steps:
  1. Review extracted files in Functions/Public/
  2. Move private/internal helpers to Functions/Private/
  3. Test with: Import-Module '$resolvedPath' -Force
  4. Verify exports with: Get-Command -Module $([System.IO.Path]::GetFileNameWithoutExtension($resolvedPath))
  5. If everything works, delete the backup: $backupPath

Note: comment-based help blocks that sat OUTSIDE a function definition
(above it, not inside the braces) were not moved — check the backup if needed.
"@ -ForegroundColor Yellow
