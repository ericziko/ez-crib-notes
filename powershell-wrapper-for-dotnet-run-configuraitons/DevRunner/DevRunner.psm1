#Requires -Version 5.1
<#
.SYNOPSIS
    DevRunner — ergonomic dotnet run wrappers for multi-project .NET solutions.

.DESCRIPTION
    Provides discoverable PowerShell commands for running, listing, and inspecting
    .NET projects within a solution without needing to remember deep project paths
    or launch profile flags.

    Public functions exported by this module:
      - Get-DotnetProjects       List all projects in the solution
      - Show-RunConfigurations   Display launch profiles for a project
      - Invoke-DotnetRun         Run a project by name with optional profile
      - Start-DevApp             Fuzzy-match convenience wrapper for Invoke-DotnetRun
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# --- Load private helpers first (they are dot-sourced, not exported) ---
$privateDir = Join-Path $PSScriptRoot 'Private'
if (Test-Path $privateDir) {
    Get-ChildItem -Path $privateDir -Filter '*.ps1' | ForEach-Object {
        Write-Verbose "Loading private function: $($_.Name)"
        . $_.FullName
    }
}

# --- Load public functions (exported via manifest FunctionsToExport) ---
$publicDir = Join-Path $PSScriptRoot 'Public'
if (Test-Path $publicDir) {
    Get-ChildItem -Path $publicDir -Filter '*.ps1' | ForEach-Object {
        Write-Verbose "Loading public function: $($_.Name)"
        . $_.FullName
    }
}
