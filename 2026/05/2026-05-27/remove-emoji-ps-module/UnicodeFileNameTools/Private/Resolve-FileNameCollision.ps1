#Requires -Version 7.0

<#
.SYNOPSIS
    Resolves a desired file name against a set of existing names according to a
    collision policy.

.DESCRIPTION
    Pure helper (no filesystem access): the caller supplies the names that already
    exist in the target directory, and this function decides the final name.

      Suffix    - if the desired name is taken, insert " (N)" before the extension,
                  incrementing N until a free name is found.
      Skip      - return $null when the desired name is taken (signals "do nothing").
      Fail      - throw when the desired name is taken.
      Overwrite - always return the desired name.

    Comparisons are case-insensitive, matching the behaviour of the common
    case-insensitive filesystems (Windows, default macOS).

.PARAMETER DesiredName
    The proposed (already-sanitized) file name.

.PARAMETER ExistingName
    The names already present in the target directory.

.PARAMETER CollisionAction
    Suffix, Skip, Fail, or Overwrite.

.OUTPUTS
    System.String (the final name), or $null when CollisionAction is Skip and the
    name collides.

.NOTES
    Internal helper. Not exported.
#>
function Resolve-FileNameCollision {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [string]$DesiredName,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$ExistingName,

        [Parameter(Mandatory)]
        [ValidateSet('Suffix', 'Skip', 'Fail', 'Overwrite')]
        [string]$CollisionAction
    )

    $existingSet = [System.Collections.Generic.HashSet[string]]::new(
        [string[]]$ExistingName, [System.StringComparer]::OrdinalIgnoreCase)

    $collides = $existingSet.Contains($DesiredName)

    if (-not $collides) {
        return $DesiredName
    }

    switch ($CollisionAction) {
        'Overwrite' { return $DesiredName }
        'Skip'      { return $null }
        'Fail'      { throw "A file named '$DesiredName' already exists in the target directory." }
        'Suffix'    {
            $stem      = [System.IO.Path]::GetFileNameWithoutExtension($DesiredName)
            $extension = [System.IO.Path]::GetExtension($DesiredName)

            $n = 1
            do {
                $candidate = '{0} ({1}){2}' -f $stem, $n, $extension
                $n++
            } while ($existingSet.Contains($candidate))

            return $candidate
        }
    }
}
