<#
.SYNOPSIS
    Finds the solution root directory by locating the nearest .sln file.

.DESCRIPTION
    Walks up the directory tree from the current location (or a specified path)
    until it finds a directory containing a .sln file. Returns that directory's
    full path. Throws if no solution file is found before reaching the filesystem root.

.PARAMETER StartPath
    The directory to start searching from. Defaults to the current working directory.

.OUTPUTS
    [string] The full path to the directory containing the .sln file.

.EXAMPLE
    $root = Resolve-SolutionRoot
    # Returns e.g. C:\Dev\MySolution

.EXAMPLE
    $root = Resolve-SolutionRoot -StartPath "C:\Dev\MySolution\src\MyApp"
#>
function Resolve-SolutionRoot {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter()]
        [string]$StartPath = (Get-Location).Path
    )

    $current = $StartPath

    while ($current -ne [System.IO.Path]::GetPathRoot($current)) {
        $slnFiles = Get-ChildItem -Path $current -Filter '*.sln' -ErrorAction SilentlyContinue
        if ($slnFiles) {
            Write-Verbose "Solution root found at: $current"
            return $current
        }
        $current = Split-Path $current -Parent
    }

    throw "No .sln file found starting from '$StartPath'. Are you inside a .NET solution directory?"
}
