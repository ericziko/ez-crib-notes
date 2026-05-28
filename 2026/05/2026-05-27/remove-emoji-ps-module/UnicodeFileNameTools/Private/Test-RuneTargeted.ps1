#Requires -Version 7.0

<#
.SYNOPSIS
    Determines whether a Rune should be acted upon, given a set of targeted
    logical categories.

.DESCRIPTION
    A rune is "targeted" when its logical category (see Get-RuneCategory) is one
    of the requested target categories. Plain printable ASCII is never targeted,
    regardless of the requested set, because removing ASCII from a file name is
    never the intent of this module. (Filesystem-illegal ASCII characters are
    handled separately by the conversion logic, which always sanitizes them.)

.PARAMETER Rune
    The [System.Text.Rune] to test.

.PARAMETER TargetCategory
    The logical categories considered "targets" (e.g. 'Emoji', 'Symbol',
    'Invisible', 'Diacritic', 'Cjk', 'Whitespace', 'Punctuation', 'Control',
    'OtherNonAscii').

.OUTPUTS
    System.Boolean.

.NOTES
    Internal helper. Not exported.
#>
function Test-RuneTargeted {
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [System.Text.Rune]$Rune,

        [Parameter(Mandatory)]
        [string[]]$TargetCategory
    )

    process {
        $category = Get-RuneCategory -Rune $Rune

        if ($category -eq 'Ascii') {
            return $false
        }

        return $TargetCategory -contains $category
    }
}
