<#
.SYNOPSIS
    Reads and parses a project's launchSettings.json file.

.DESCRIPTION
    Given a path to a .csproj file, locates the adjacent Properties/launchSettings.json
    and returns its parsed content as a PSCustomObject. Returns $null if no
    launchSettings.json exists (not all projects have one).

.PARAMETER ProjectFilePath
    The full path to a .csproj file.

.OUTPUTS
    [PSCustomObject] The parsed launchSettings.json content, or $null.

.EXAMPLE
    $settings = Get-LaunchSettings -ProjectFilePath "C:\Dev\MySolution\src\MyWorker\MyWorker.csproj"
    $settings.profiles.PSObject.Properties | Select-Object Name
#>
function Get-LaunchSettings {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectFilePath
    )

    $projectDir = Split-Path $ProjectFilePath -Parent
    $launchSettingsPath = Join-Path $projectDir 'Properties' 'launchSettings.json'

    if (-not (Test-Path $launchSettingsPath)) {
        Write-Verbose "No launchSettings.json found at '$launchSettingsPath'"
        return $null
    }

    Write-Verbose "Reading launchSettings.json from '$launchSettingsPath'"

    try {
        $content = Get-Content -Path $launchSettingsPath -Raw | ConvertFrom-Json
        return $content
    }
    catch {
        throw "Failed to parse launchSettings.json at '$launchSettingsPath': $_"
    }
}
