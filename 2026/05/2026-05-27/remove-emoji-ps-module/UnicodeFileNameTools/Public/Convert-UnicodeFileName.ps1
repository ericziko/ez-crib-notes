#Requires -Version 7.0

<#
.SYNOPSIS
    Cleans a file name string by removing, replacing, or substituting emoji and
    other Unicode characters. Pure string transform — does not touch the disk.

.DESCRIPTION
    Convert-UnicodeFileName walks a file name one Unicode scalar value (Rune) at a
    time. Characters whose logical category is in -TargetCategory (and characters
    that are illegal in a file name) are transformed according to -Action:

      Remove     - the character is deleted.
      Replace    - the character becomes -Replacement.
      Substitute - the layered pipeline runs (default): an explicit -Map entry, then
                   ASCII transliteration (if -Transliterate), then an emoji
                   shortcode :name: (if -EmojiNames), then the -Replacement fallback.

    Characters that are not targeted are passed through unchanged, so by default
    accented Latin letters, CJK text, and punctuation are preserved.

    Filesystem-illegal characters (those returned by
    [System.IO.Path]::GetInvalidFileNameChars() on the current OS) are always
    cleaned, regardless of -TargetCategory. With -PortableNames, the full
    Windows-reserved set (\ / : * ? " < > |) is also cleaned so names stay valid
    when moved between operating systems.

.PARAMETER Name
    One or more file name strings (accepts pipeline input). This is a name, not a
    path; pass only the leaf name.

.PARAMETER Action
    Remove, Replace, or Substitute. Default: Substitute.

.PARAMETER TargetCategory
    Logical categories to target. Default: Emoji, Symbol, Invisible. Other choices:
    Diacritic, Cjk, Whitespace, Punctuation, Control, OtherNonAscii.

.PARAMETER Replacement
    The fallback replacement string for Replace and for the last step of
    Substitute. Default: '_'.

.PARAMETER Transliterate
    During Substitute, fold characters that have an ASCII equivalent (e.g. accents)
    to ASCII.

.PARAMETER EmojiNames
    During Substitute, replace recognised emoji with :shortcode: from the bundled
    table. Note: the ':' delimiter is itself reserved on Windows, so combine with
    care when also using -PortableNames.

.PARAMETER Map
    Explicit overrides applied first during Substitute. Keys may be the literal
    character, the uppercase hex code point ('1F389'), or the 'U+1F389' form.

.PARAMETER CollapseRuns
    Collapse consecutive runs of the replacement string into one, and trim leading
    and trailing replacement strings from the result. Avoids names like
    'a__b_.txt'.

.PARAMETER PortableNames
    Also clean characters reserved on Windows even when running on macOS/Linux.

.OUTPUTS
    System.String. The cleaned name (one per input name). May be empty if every
    character was removed.

.EXAMPLE
    Convert-UnicodeFileName -Name 'Q3 Report 🎉 final.docx'
    # Q3 Report _ final.docx

.EXAMPLE
    Convert-UnicodeFileName -Name 'Crème brûlée 🎂.md' -TargetCategory Diacritic,Emoji -Transliterate -EmojiNames -CollapseRuns
    # Creme brulee :birthday-cake:.md

.EXAMPLE
    'a😀.txt','b🚀.txt' | Convert-UnicodeFileName -Action Remove
    # a.txt
    # b.txt

.LINK
    Repair-UnicodeFileName
.LINK
    Test-UnicodeFileName
#>
function Convert-UnicodeFileName {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName, Position = 0)]
        [Alias('FullName')]
        [AllowEmptyString()]
        [string]$Name,

        [ValidateSet('Remove', 'Replace', 'Substitute')]
        [string]$Action = 'Substitute',

        [ValidateSet('Emoji', 'Symbol', 'Diacritic', 'Cjk', 'Whitespace', 'Invisible', 'Punctuation', 'Control', 'OtherNonAscii')]
        [string[]]$TargetCategory = @('Emoji', 'Symbol', 'Invisible'),

        [string]$Replacement = '_',

        [switch]$Transliterate,

        [switch]$EmojiNames,

        [hashtable]$Map,

        [switch]$CollapseRuns,

        [switch]$PortableNames
    )

    begin {
        # Characters that cannot appear in a file name. Always the current OS set;
        # optionally the Windows-reserved superset for portable output.
        $illegal = [System.Collections.Generic.HashSet[int]]::new()
        foreach ($c in [System.IO.Path]::GetInvalidFileNameChars()) {
            [void]$illegal.Add([int]$c)
        }
        if ($PortableNames) {
            foreach ($c in [char[]]@('\', '/', ':', '*', '?', '"', '<', '>', '|')) {
                [void]$illegal.Add([int]$c)
            }
            for ($i = 0; $i -le 0x1F; $i++) { [void]$illegal.Add($i) }
        }
    }

    process {
        if ([string]::IsNullOrEmpty($Name)) {
            return ''
        }

        $builder = [System.Text.StringBuilder]::new($Name.Length)

        foreach ($rune in $Name.EnumerateRunes()) {
            $isIllegal  = $illegal.Contains($rune.Value)
            $isTargeted = Test-RuneTargeted -Rune $rune -TargetCategory $TargetCategory

            if ($isIllegal -or $isTargeted) {
                $replacementText = Invoke-SubstitutionPipeline -Rune $rune -Action $Action `
                    -Replacement $Replacement -Transliterate:$Transliterate -EmojiNames:$EmojiNames -Map $Map
                [void]$builder.Append($replacementText)
            }
            else {
                [void]$builder.Append($rune.ToString())
            }
        }

        $result = $builder.ToString()

        if ($CollapseRuns -and -not [string]::IsNullOrEmpty($Replacement)) {
            $escaped = [regex]::Escape($Replacement)
            $result = [regex]::Replace($result, "(?:$escaped){2,}", $Replacement)
            $result = $result -replace "^(?:$escaped)+", ''
            $result = $result -replace "(?:$escaped)+$", ''
        }

        return $result
    }
}
