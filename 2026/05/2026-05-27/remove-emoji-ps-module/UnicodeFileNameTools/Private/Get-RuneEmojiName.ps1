#Requires -Version 7.0

<#
.SYNOPSIS
    Looks up the kebab-case shortcode for an emoji Rune.

.DESCRIPTION
    Resolves a [System.Text.Rune] to a shortcode (e.g. U+1F389 -> 'party-popper')
    using the bundled Data/EmojiNames.psd1 table. The table is loaded once and
    cached for the lifetime of the module session.

    The returned value is the bare shortcode; the substitution pipeline is
    responsible for any :colon: wrapping. Code points absent from the table return
    $null.

.PARAMETER Rune
    The [System.Text.Rune] to resolve.

.PARAMETER AdditionalNames
    An optional hashtable (same key format as the bundled table: uppercase hex
    code point) that extends and overrides the bundled names for this call.

.OUTPUTS
    System.String, or $null when the code point has no known shortcode.

.NOTES
    Internal helper. Not exported.
#>

# Module-scoped cache for the bundled emoji table. Declared here (at dot-source
# time) so the lazy-load check below is safe under Set-StrictMode.
$script:EmojiNameTable = $null

function Get-RuneEmojiName {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [System.Text.Rune]$Rune,

        [hashtable]$AdditionalNames
    )

    begin {
        if ($null -eq $script:EmojiNameTable) {
            $dataPath = Join-Path (Split-Path -Parent $PSScriptRoot) 'Data' 'EmojiNames.psd1'
            $script:EmojiNameTable = Import-PowerShellDataFile -Path $dataPath
        }
    }

    process {
        $key = '{0:X}' -f $Rune.Value

        if ($AdditionalNames -and $AdditionalNames.ContainsKey($key)) {
            return [string]$AdditionalNames[$key]
        }

        if ($script:EmojiNameTable.ContainsKey($key)) {
            return [string]$script:EmojiNameTable[$key]
        }

        return $null
    }
}
