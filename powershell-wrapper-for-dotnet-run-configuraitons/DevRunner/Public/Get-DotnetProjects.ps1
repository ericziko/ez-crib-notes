<#
.SYNOPSIS
    Lists all .NET projects found within the solution.

.DESCRIPTION
    Recursively searches the solution root for all .csproj files and returns
    summary information about each: name, relative path, and available launch
    profiles (from launchSettings.json if present).

.PARAMETER SolutionRoot
    The root directory to search. Defaults to the auto-detected solution root
    (nearest directory containing a .sln file walking up from the current location).

.OUTPUTS
    [PSCustomObject[]] Objects with Name, RelativePath, ProjectFile, and Profiles properties.

.EXAMPLE
    Get-DotnetProjects

.EXAMPLE
    Get-DotnetProjects -SolutionRoot "C:\Dev\MySolution"

.EXAMPLE
    # Show only project names
    Get-DotnetProjects | Select-Object -ExpandProperty Name

.EXAMPLE
    # Find projects that have a Staging profile
    Get-DotnetProjects | Where-Object { $_.Profiles -contains 'Staging' }
#>
function Get-DotnetProjects {
    [CmdletBinding()]
    [OutputType([PSCustomObject[]])]
    param(
        [Parameter()]
        [string]$SolutionRoot
    )

    if (-not $SolutionRoot) {
        $SolutionRoot = Resolve-SolutionRoot
    }

    Write-Verbose "Scanning for projects under '$SolutionRoot'"

    $projects = Get-ChildItem -Path $SolutionRoot -Recurse -Filter '*.csproj' -ErrorAction SilentlyContinue

    foreach ($proj in $projects) {
        $launchSettings = Get-LaunchSettings -ProjectFilePath $proj.FullName
        $profiles = @()

        if ($launchSettings -and $launchSettings.profiles) {
            $profiles = $launchSettings.profiles.PSObject.Properties.Name
        }

        $relativePath = $proj.FullName.Replace($SolutionRoot, '').TrimStart([System.IO.Path]::DirectorySeparatorChar)

        [PSCustomObject]@{
            Name         = $proj.BaseName
            RelativePath = $relativePath
            ProjectFile  = $proj.FullName
            Profiles     = $profiles
        }
    }
}
