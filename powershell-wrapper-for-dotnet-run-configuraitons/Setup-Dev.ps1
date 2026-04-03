<#
.SYNOPSIS
    Local development environment bootstrapper.

.DESCRIPTION
    Dot-source this script once per shell session from the solution root to load
    the DevRunner module and make ergonomic dotnet run commands available.

    Usage:
        cd C:\Dev\MySolution
        . ./Setup-Dev.ps1

    After running, the following commands are available:
        Get-DotnetProjects                          - list all projects
        Show-RunConfigurations -ProjectName <name>  - view launch profiles
        Invoke-DotnetRun -ProjectName <name>        - run a project
        Start-DevApp <partial-name>                 - fuzzy-run shortcut

.NOTES
    Must be dot-sourced (. ./Setup-Dev.ps1), not called directly, so that
    the imported module functions land in the calling shell's scope.
#>

#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

# Ensure we are being dot-sourced so exports reach the caller's scope
if ($MyInvocation.InvocationName -ne '.') {
    Write-Warning "Setup-Dev.ps1 should be dot-sourced: '. ./Setup-Dev.ps1'"
    Write-Warning "Running directly means module functions will NOT be available in your shell."
}

Write-Host ""
Write-Host "DevRunner — loading dev environment..." -ForegroundColor Cyan

# Locate and import the DevRunner module (relative to this script, not CWD)
$moduleManifest = Join-Path $PSScriptRoot "DevRunner" "DevRunner.psd1"

if (-not (Test-Path $moduleManifest)) {
    throw "DevRunner module not found at '$moduleManifest'. Ensure you cloned the full repo."
}

Import-Module $moduleManifest -Force -Verbose:$false

Write-Host "DevRunner module loaded." -ForegroundColor Green
Write-Host ""

# Show what's available
Write-Host "Available projects:" -ForegroundColor Green
$projects = Get-DotnetProjects
if ($projects) {
    $projects | ForEach-Object {
        $profileList = if ($_.Profiles) { " [profiles: $($_.Profiles -join ', ')]" } else { "" }
        Write-Host "  • $($_.Name)$profileList" -ForegroundColor White
    }
} else {
    Write-Host "  (none found — are you at the solution root?)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Quick reference:" -ForegroundColor Green
Write-Host "  Get-DotnetProjects" -ForegroundColor White
Write-Host "  Show-RunConfigurations -ProjectName <name>" -ForegroundColor White
Write-Host "  Invoke-DotnetRun       -ProjectName <name> [-Profile <profile>]" -ForegroundColor White
Write-Host "  Start-DevApp           <partial-name>      [-Profile <profile>]" -ForegroundColor White
Write-Host ""
