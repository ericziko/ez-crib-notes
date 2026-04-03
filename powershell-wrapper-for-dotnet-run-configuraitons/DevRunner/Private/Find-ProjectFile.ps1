<#
.SYNOPSIS
    Locates a .csproj file by project name within a solution directory.

.DESCRIPTION
    Recursively searches under the given root directory for a .csproj file whose
    base name matches the provided ProjectName (case-insensitive). Returns the
    full path to the .csproj file. Throws if no match or multiple ambiguous matches
    are found.

.PARAMETER ProjectName
    The name of the project to find (matches the .csproj filename without extension).

.PARAMETER SolutionRoot
    The root directory to search under. Defaults to the detected solution root.

.OUTPUTS
    [string] The full path to the matching .csproj file.

.EXAMPLE
    $path = Find-ProjectFile -ProjectName "MyWorker"
    # Returns e.g. C:\Dev\MySolution\src\Workers\MyWorker\MyWorker.csproj

.EXAMPLE
    $path = Find-ProjectFile -ProjectName "DbMigrator" -SolutionRoot "C:\Dev\MySolution"
#>
function Find-ProjectFile {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectName,

        [Parameter()]
        [string]$SolutionRoot
    )

    if (-not $SolutionRoot) {
        $SolutionRoot = Resolve-SolutionRoot
    }

    Write-Verbose "Searching for project '$ProjectName' under '$SolutionRoot'"

    $matches = Get-ChildItem -Path $SolutionRoot -Recurse -Filter '*.csproj' -ErrorAction SilentlyContinue |
        Where-Object { $_.BaseName -ieq $ProjectName }

    if ($matches.Count -eq 0) {
        throw "No project named '$ProjectName' found under '$SolutionRoot'."
    }

    if ($matches.Count -gt 1) {
        $paths = ($matches | Select-Object -ExpandProperty FullName) -join "`n  "
        throw "Multiple projects named '$ProjectName' found. Be more specific:`n  $paths"
    }

    Write-Verbose "Found: $($matches[0].FullName)"
    return $matches[0].FullName
}
