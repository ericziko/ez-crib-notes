<#
.SYNOPSIS
    Runs a named .NET project using `dotnet run`, optionally with a launch profile.

.DESCRIPTION
    Resolves the project by name (matching the .csproj filename), constructs the
    appropriate `dotnet run --project <path>` command, and executes it. Supports
    launch profiles from launchSettings.json and pass-through of additional CLI args.

    The working directory for `dotnet run` is set to the solution root so relative
    paths in the app behave consistently regardless of where the developer's shell is.

.PARAMETER ProjectName
    The name of the project to run (matches .csproj filename without extension).
    Case-insensitive.

.PARAMETER Profile
    The launch profile name from launchSettings.json (e.g., "Development", "Staging").
    If not specified, dotnet uses the first profile or project defaults.

.PARAMETER SolutionRoot
    The root directory to search for the project. Defaults to auto-detected solution root.

.PARAMETER AdditionalArgs
    Any additional arguments to pass after `--` to the application itself.

.PARAMETER NoBuild
    If specified, passes `--no-build` to skip compilation (faster for repeated runs).

.PARAMETER Configuration
    Build configuration: Debug (default) or Release.

.EXAMPLE
    Invoke-DotnetRun -ProjectName MyWorker

.EXAMPLE
    Invoke-DotnetRun -ProjectName MyWorker -Profile Staging

.EXAMPLE
    Invoke-DotnetRun -ProjectName DbMigrator -Profile Development -NoBuild

.EXAMPLE
    Invoke-DotnetRun -ProjectName MyWorker -Profile Development -AdditionalArgs "--verbose --dry-run"

.EXAMPLE
    # Pipe from Get-DotnetProjects
    Get-DotnetProjects | Where-Object Name -eq MyWorker | Invoke-DotnetRun -Profile Staging
#>
function Invoke-DotnetRun {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipelineByPropertyName)]
        [string]$ProjectName,

        [Parameter(Position = 1)]
        [string]$Profile,

        [Parameter()]
        [string]$SolutionRoot,

        [Parameter()]
        [string]$AdditionalArgs,

        [Parameter()]
        [switch]$NoBuild,

        [Parameter()]
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration = 'Debug'
    )

    process {
        if (-not $SolutionRoot) {
            $SolutionRoot = Resolve-SolutionRoot
        }

        $projectFile = Find-ProjectFile -ProjectName $ProjectName -SolutionRoot $SolutionRoot

        # Build the argument list
        $dotnetArgs = @('run', '--project', $projectFile, '--configuration', $Configuration)

        if ($Profile) {
            $dotnetArgs += @('--launch-profile', $Profile)
        }

        if ($NoBuild) {
            $dotnetArgs += '--no-build'
        }

        if ($AdditionalArgs) {
            $dotnetArgs += '--'
            $dotnetArgs += $AdditionalArgs -split ' '
        }

        $commandDisplay = "dotnet $($dotnetArgs -join ' ')"
        Write-Verbose "Executing: $commandDisplay"
        Write-Host "▶ $commandDisplay" -ForegroundColor DarkGray

        if ($PSCmdlet.ShouldProcess($ProjectName, 'dotnet run')) {
            & dotnet @dotnetArgs
            if ($LASTEXITCODE -ne 0) {
                throw "'dotnet run' for project '$ProjectName' exited with code $LASTEXITCODE"
            }
        }
    }
}
