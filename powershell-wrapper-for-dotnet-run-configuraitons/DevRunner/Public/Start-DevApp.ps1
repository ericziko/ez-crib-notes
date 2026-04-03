<#
.SYNOPSIS
    Convenience wrapper for Invoke-DotnetRun with fuzzy project name matching.

.DESCRIPTION
    Provides a short, easy-to-type entry point for running development apps.
    Unlike Invoke-DotnetRun (which requires an exact project name), Start-DevApp
    performs a case-insensitive substring match so developers can type partial names.

    If the partial name matches exactly one project, that project is run. If it
    matches multiple projects, a selection menu is presented. If it matches none,
    an error is thrown with a list of available projects.

.PARAMETER AppName
    A partial or full project name. Case-insensitive substring match against all
    .csproj filenames in the solution.

.PARAMETER Profile
    The launch profile name (e.g., "Development", "Staging"). Optional.

.PARAMETER SolutionRoot
    Override the auto-detected solution root.

.PARAMETER NoBuild
    Skip the build step (--no-build).

.PARAMETER Configuration
    Build configuration: Debug (default) or Release.

.EXAMPLE
    Start-DevApp worker
    # Matches e.g. "MyWorker", "BackgroundWorker"

.EXAMPLE
    Start-DevApp migrator -Profile Staging

.EXAMPLE
    Start-DevApp db -Profile Development -NoBuild
    # Interactive if multiple matches

.NOTES
    If multiple projects match, you will be prompted to choose one interactively.
    Use Invoke-DotnetRun with the exact name to avoid the prompt in scripts.
#>
function Start-DevApp {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$AppName,

        [Parameter(Position = 1)]
        [string]$Profile,

        [Parameter()]
        [string]$SolutionRoot,

        [Parameter()]
        [switch]$NoBuild,

        [Parameter()]
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Debug'
    )

    if (-not $SolutionRoot) {
        $SolutionRoot = Resolve-SolutionRoot
    }

    $allProjects = Get-DotnetProjects -SolutionRoot $SolutionRoot
    $matched = @($allProjects | Where-Object { $_.Name -ilike "*$AppName*" })

    if ($matched.Count -eq 0) {
        $available = ($allProjects | Select-Object -ExpandProperty Name) -join ', '
        throw "No project matching '$AppName' found. Available projects: $available"
    }

    $selectedProject = if ($matched.Count -eq 1) {
        $matched[0]
    }
    else {
        Write-Host "Multiple matches for '$AppName':" -ForegroundColor Yellow
        for ($i = 0; $i -lt $matched.Count; $i++) {
            Write-Host "  [$($i + 1)] $($matched[$i].Name)" -ForegroundColor White
        }
        $choice = Read-Host "Enter number"
        $index = [int]$choice - 1
        if ($index -lt 0 -or $index -ge $matched.Count) {
            throw "Invalid selection."
        }
        $matched[$index]
    }

    Write-Verbose "Selected project: $($selectedProject.Name)"

    $invokeParams = @{
        ProjectName   = $selectedProject.Name
        SolutionRoot  = $SolutionRoot
        Configuration = $Configuration
        NoBuild       = $NoBuild
    }
    if ($Profile) { $invokeParams['Profile'] = $Profile }

    Invoke-DotnetRun @invokeParams
}
