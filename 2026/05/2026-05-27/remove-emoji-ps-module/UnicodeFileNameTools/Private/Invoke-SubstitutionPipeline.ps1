#Requires -Version 7.0

<#
.SYNOPSIS
    Computes the replacement text for a single targeted Rune by running the
    layered substitution pipeline.

.DESCRIPTION
    Resolves what a targeted character should become, according to the chosen
    action:

      Remove     -> always an empty string (the character is deleted).
      Replace    -> always the -Replacement string (map/transliterate/emoji
                    options are ignored).
      Substitute -> the layered pipeline, evaluated in priority order:
                      1. an explicit -Map entry,
                      2. ASCII transliteration (if -Transliterate),
                      3. an emoji shortcode :name: (if -EmojiNames),
                      4. the -Replacement fallback.

    -Map keys may be the literal character, the uppercase hex code point
    ('1F389'), or the 'U+1F389' form; the first match wins.

.PARAMETER Rune
    The targeted [System.Text.Rune] to resolve.

.PARAMETER Action
    Remove, Replace, or Substitute.

.PARAMETER Replacement
    The fallback replacement string.

.PARAMETER Transliterate
    Enable ASCII transliteration of characters that have an ASCII fold.

.PARAMETER EmojiNames
    Enable emoji-shortcode substitution (:name:).

.PARAMETER Map
    Explicit per-character overrides (highest priority).

.PARAMETER AdditionalEmojiNames
    Extra emoji shortcodes passed through to Get-RuneEmojiName.

.OUTPUTS
    System.String. The replacement text for the rune (may be empty).

.NOTES
    Internal helper. Not exported.
#>
function Invoke-SubstitutionPipeline {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory)]
        [System.Text.Rune]$Rune,

        [Parameter(Mandatory)]
        [ValidateSet('Remove', 'Replace', 'Substitute')]
        [string]$Action,

        [string]$Replacement = '_',

        [switch]$Transliterate,

        [switch]$EmojiNames,

        [hashtable]$Map,

        [hashtable]$AdditionalEmojiNames
    )

    if ($Action -eq 'Remove') {
        return ''
    }

    if ($Action -eq 'Replace') {
        return $Replacement
    }

    # --- Substitute: layered pipeline ---

    # 1. Explicit map (literal char, hex, or U+ form).
    if ($Map -and $Map.Count -gt 0) {
        $candidates = @(
            $Rune.ToString()
            '{0:X}' -f $Rune.Value
            'U+{0:X4}' -f $Rune.Value
        )
        foreach ($key in $candidates) {
            if ($Map.ContainsKey($key)) {
                return [string]$Map[$key]
            }
        }
    }

    # 2. ASCII transliteration (accept only a pure-ASCII fold).
    if ($Transliterate) {
        $fold = ConvertTo-TransliteratedText -Text $Rune.ToString()
        if ($fold -match '^[\x00-\x7F]*$' -and $fold -ne $Rune.ToString()) {
            return $fold
        }
    }

    # 3. Emoji shortcode.
    if ($EmojiNames) {
        $name = Get-RuneEmojiName -Rune $Rune -AdditionalNames $AdditionalEmojiNames
        if ($name) {
            return ":${name}:"
        }
    }

    # 4. Fallback.
    return $Replacement
}
