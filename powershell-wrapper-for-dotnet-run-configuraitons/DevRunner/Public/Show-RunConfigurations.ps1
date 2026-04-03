<#
.SYNOPSIS
    Displays the available launch profiles for a .NET project.

.DESCRIPTION
    Looks up the named project's launchSettings.json and prints each defined
    profile along with its environment variables and command-line arguments.
    Useful for discovering what run configurations are available before calling
    Invoke-DotnetRun.

.PARAMETER ProjectName
    The name of the project (matches the .csproj filename without extension).

.PARAMETER SolutionRoot
    The root directory to search for the project. Defaults to auto-detected solution root.

.EXAMPLE
    Show-RunConfigurations -ProjectName MyWorker

.EXAMPLE
    Show-RunConfigurations -ProjectName DbMigrator
#>
function Show-RunConfigurations {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]$ProjectName,

        [Parameter()]
        [string]$SolutionRoot
    )

    if (-not $SolutionRoot) {
        $SolutionRoot = Resolve-SolutionRoot
    }

    $projectFile = Find-ProjectFile -ProjectName $ProjectName -SolutionRoot $SolutionRoot
    $launchSettings = Get-LaunchSettings -ProjectFilePath $projectFile

    Write-Host ""
    Write-Host "Project: " -NoNewline -ForegroundColor Cyan
    Write-Host $ProjectName -ForegroundColor White
    Write-Host "Path:    " -NoNewline -ForegroundColor Cyan
    Write-Host ($projectFile.Replace($SolutionRoot, '').TrimStart([System.IO.Path]::DirectorySeparatorChar)) -ForegroundColor Gray

    if (-not $launchSettings -or -not $launchSettings.profiles) {
        Write-Host ""
        Write-Warning "No launchSettings.json found. Run with: dotnet run --project <path>"
        return
    }

    Write-Host ""
    Write-Host "Launch Profiles:" -ForegroundColor Green

    foreach ($profile in $launchSettings.profiles.PSObject.Properties) {
        $profileName = $profile.Name
        $profileData = $profile.Value

        Write-Host ""
        Write-Host "  [$profileName]" -ForegroundColor Yellow

        if ($profileData.environmentVariables) {
            Write-Host "    Environment Variables:" -ForegroundColor DarkCyan
            foreach ($envVar in $profileData.environmentVariables.PSObject.Properties) {
                Write-Host "      $($envVar.Name) = $($envVar.Value)" -ForegroundColor Gray
            }
        }

        if ($profileData.commandLineArgs) {
            Write-Host "    Args: $($profileData.commandLineArgs)" -ForegroundColor DarkCyan
        }
    }

    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Green
    foreach ($profile in $launchSettings.profiles.PSObject.Properties) {
        Write-Host "  Invoke-DotnetRun -ProjectName $ProjectName -Profile '$($profile.Name)'" -ForegroundColor White
    }
    Write-Host ""
}
