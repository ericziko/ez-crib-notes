#Requires -Version 7.0

<#
.SYNOPSIS
    Transliterates text to its closest ASCII form by Unicode compatibility
    decomposition, removing combining marks.

.DESCRIPTION
    Applies Unicode Normalization Form KD (compatibility decomposition), which
    splits precomposed characters into a base character plus combining marks
    (e.g. 'é' -> 'e' + U+0301) and expands compatibility forms (e.g. the 'ﬁ'
    ligature -> 'fi', the circled digit '①' -> '1'). All combining marks are then
    removed, leaving the base letters.

    This reliably folds accented Latin text to ASCII. Characters that do not
    decompose to a Latin base (Greek, Cyrillic, CJK, atomic ligatures such as 'Æ',
    'ß') are returned unchanged, because no ASCII fold exists for them.

.PARAMETER Text
    The text to transliterate. May be empty.

.OUTPUTS
    System.String. The transliterated text.

.EXAMPLE
    ConvertTo-TransliteratedText -Text 'Crème Brûlée'
    # Returns 'Creme Brulee'

.NOTES
    Internal helper. Not exported. Per-rune ASCII-fold decisions in the
    substitution pipeline are made by inspecting whether this function's output is
    pure ASCII.
#>
function ConvertTo-TransliteratedText {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowEmptyString()]
        [string]$Text
    )

    process {
        if ([string]::IsNullOrEmpty($Text)) {
            return ''
        }

        $decomposed = $Text.Normalize([System.Text.NormalizationForm]::FormKD)

        $builder = [System.Text.StringBuilder]::new($decomposed.Length)
        foreach ($rune in $decomposed.EnumerateRunes()) {
            $category = [System.Text.Rune]::GetUnicodeCategory($rune)
            if ($category -in @(
                    [System.Globalization.UnicodeCategory]::NonSpacingMark,
                    [System.Globalization.UnicodeCategory]::SpacingCombiningMark,
                    [System.Globalization.UnicodeCategory]::EnclosingMark)) {
                continue
            }
            [void]$builder.Append($rune.ToString())
        }

        return $builder.ToString()
    }
}
