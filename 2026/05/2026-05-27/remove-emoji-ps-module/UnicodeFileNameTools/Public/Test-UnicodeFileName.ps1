#Requires -Version 7.0

<#
.SYNOPSIS
    Tests whether a file name contains characters in the targeted Unicode
    categories.

.DESCRIPTION
    Returns $true if the file name contains any character whose logical category is
    in -TargetCategory. Returns $false otherwise. Use -Detailed to receive a result
    object listing the matched characters instead of a bare boolean.

    Filesystem-illegal ASCII characters (e.g. '/') are NOT considered matches here
    — this command answers "does this name have emoji / Unicode content?", not
    "is this a valid file name?". Use Convert-UnicodeFileName for sanitization.

.PARAMETER Name
    One or more file name strings, or FileInfo objects (the .Name property is used)
    from the pipeline.

.PARAMETER TargetCategory
    Logical categories to detect. Default: Emoji, Symbol, Invisible.

.PARAMETER Detailed
    Return a PSCustomObject ({ Name, IsMatch, Count, MatchedCharacter }) per input
    instead of a bare boolean.

.OUTPUTS
    System.Boolean, or PSCustomObject when -Detailed is supplied.

.EXAMPLE
    Test-UnicodeFileName -Name 'release🎉.txt'
    # True

.EXAMPLE
    Get-ChildItem | Where-Object { $_ | Test-UnicodeFileName }

.EXAMPLE
    Test-UnicodeFileName -Name 'café 🎉.md' -Detailed | Format-List

.LINK
    Get-UnicodeFileNameCharacter
.LINK
    Convert-UnicodeFileName
#>
function Test-UnicodeFileName {
    [CmdletBinding()]
    [OutputType([bool])]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName, Position = 0)]
        [Alias('FullName')]
        [AllowEmptyString()]
        [string]$Name,

        [ValidateSet('Emoji', 'Symbol', 'Diacritic', 'Cjk', 'Whitespace', 'Invisible', 'Punctuation', 'Control', 'OtherNonAscii')]
        [string[]]$TargetCategory = @('Emoji', 'Symbol', 'Invisible'),

        [switch]$Detailed
    )

    process {
        $leaf = if ([string]::IsNullOrEmpty($Name)) { '' } else { [System.IO.Path]::GetFileName($Name) }
        if ([string]::IsNullOrEmpty($leaf)) { $leaf = $Name }

        if ($Detailed) {
            $matches = @(Get-UnicodeFileNameCharacter -Name $leaf -TargetCategory $TargetCategory -TargetedOnly)
            return [pscustomobject]@{
                Name             = $leaf
                IsMatch          = ($matches.Count -gt 0)
                Count            = $matches.Count
                MatchedCharacter = $matches
            }
        }

        # Boolean path: short-circuit on the first targeted rune.
        foreach ($rune in $leaf.EnumerateRunes()) {
            if (Test-RuneTargeted -Rune $rune -TargetCategory $TargetCategory) {
                return $true
            }
        }
        return $false
    }
}
