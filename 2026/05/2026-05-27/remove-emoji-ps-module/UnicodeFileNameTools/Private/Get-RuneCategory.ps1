#Requires -Version 7.0

<#
.SYNOPSIS
    Classifies a Unicode scalar value (Rune) into a logical category used by the
    module's targeting and substitution logic.

.DESCRIPTION
    The module reasons about characters in terms of a small set of *logical*
    categories rather than raw Unicode general categories, because that is the
    vocabulary a user thinks in when sanitizing file names ("strip emoji and
    symbols, but leave accents alone").

    Returned categories:
      Ascii         - printable ASCII (U+0020..U+007E)
      Control       - C0/C1 and other control characters
      Emoji         - emoji / pictographs / regional indicators / skin tones
      Symbol        - other symbols (math, currency, ©, ™, ★, arrows, ...)
      Diacritic     - accented Latin letters and combining marks (fold to ASCII)
      Cjk           - CJK ideographs and Japanese/Korean syllabaries
      Whitespace    - non-ASCII whitespace (NBSP, em space, ideographic space)
      Invisible     - zero-width / format characters and variation selectors
      Punctuation   - non-ASCII punctuation (guillemets, dashes, curly quotes)
      OtherNonAscii - anything else outside ASCII (e.g. Greek, Cyrillic)

.PARAMETER Rune
    The [System.Text.Rune] to classify. Using Rune (a full Unicode scalar value)
    means surrogate-pair characters such as emoji are handled as a single unit.

.OUTPUTS
    System.String. One of the logical category names listed above.

.EXAMPLE
    Get-RuneCategory -Rune ([System.Text.Rune]::new(0x1F600))
    # Returns 'Emoji'

.NOTES
    Internal helper. Not exported.
#>
function Get-RuneCategory {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [System.Text.Rune]$Rune
    )

    process {
        $cp  = $Rune.Value
        $cat = [System.Text.Rune]::GetUnicodeCategory($Rune)

        # 1. ASCII range is the common, fast path.
        if ($cp -lt 0x80) {
            if ($cat -eq [System.Globalization.UnicodeCategory]::Control) {
                return 'Control'
            }
            return 'Ascii'
        }

        # 2. Invisible / format characters (zero-width joiners, BOM, word joiner)
        #    and variation selectors. Checked early because some are otherwise
        #    categorized as marks.
        if ($cat -eq [System.Globalization.UnicodeCategory]::Format) {
            return 'Invisible'
        }
        if (($cp -ge 0xFE00 -and $cp -le 0xFE0F) -or ($cp -ge 0xE0100 -and $cp -le 0xE01EF)) {
            return 'Invisible'  # variation selectors
        }

        # 3. Emoji and pictographs, identified by the dedicated emoji blocks plus a
        #    handful of older symbol code points that render as emoji.
        if (Test-RuneInEmojiRange -CodePoint $cp) {
            return 'Emoji'
        }

        # 4. Control characters (C1 etc.) above ASCII.
        if ($cat -eq [System.Globalization.UnicodeCategory]::Control) {
            return 'Control'
        }

        # 5. Non-ASCII whitespace (NBSP, the various Unicode spaces, ideographic space).
        if ([System.Text.Rune]::IsWhiteSpace($Rune)) {
            return 'Whitespace'
        }

        # 6. Combining marks and precomposed accented Latin letters -> Diacritic.
        if ($cat -in @(
                [System.Globalization.UnicodeCategory]::NonSpacingMark,
                [System.Globalization.UnicodeCategory]::SpacingCombiningMark,
                [System.Globalization.UnicodeCategory]::EnclosingMark)) {
            return 'Diacritic'
        }
        if ([System.Text.Rune]::IsLetter($Rune) -and (Test-RuneIsLatinDiacritic -CodePoint $cp)) {
            return 'Diacritic'
        }

        # 7. CJK ideographs and Japanese / Korean syllabaries.
        if (Test-RuneInCjkRange -CodePoint $cp) {
            return 'Cjk'
        }

        # 8. Symbols (math, currency, modifier, other) that are not emoji.
        if ($cat -in @(
                [System.Globalization.UnicodeCategory]::MathSymbol,
                [System.Globalization.UnicodeCategory]::CurrencySymbol,
                [System.Globalization.UnicodeCategory]::ModifierSymbol,
                [System.Globalization.UnicodeCategory]::OtherSymbol)) {
            return 'Symbol'
        }

        # 9. Punctuation.
        if ($cat -in @(
                [System.Globalization.UnicodeCategory]::ConnectorPunctuation,
                [System.Globalization.UnicodeCategory]::DashPunctuation,
                [System.Globalization.UnicodeCategory]::OpenPunctuation,
                [System.Globalization.UnicodeCategory]::ClosePunctuation,
                [System.Globalization.UnicodeCategory]::InitialQuotePunctuation,
                [System.Globalization.UnicodeCategory]::FinalQuotePunctuation,
                [System.Globalization.UnicodeCategory]::OtherPunctuation)) {
            return 'Punctuation'
        }

        # 10. Everything else outside ASCII.
        return 'OtherNonAscii'
    }
}

# --- Range helpers (kept local to the classifier) --------------------------

function Test-RuneInEmojiRange {
    param([int]$CodePoint)

    # Supplementary-plane pictograph blocks: Mahjong/Dominoes/Cards (U+1F000),
    # Enclosed alphanumeric/ideographic supplements, Misc Symbols & Pictographs
    # (U+1F300), Emoticons (U+1F600), Transport & Map (U+1F680), Supplemental
    # Symbols & Pictographs (U+1F900), Symbols & Pictographs Extended-A (U+1FA70),
    # and the regional-indicator (U+1F1E6) and skin-tone modifier (U+1F3FB) ranges
    # that fall inside it.
    #
    # Legacy BMP symbols that can render with emoji presentation (e.g. U+2764 red
    # heart, U+2728 sparkles, U+2600 sun) are intentionally classified as 'Symbol'
    # rather than 'Emoji': range-based detection cannot reliably separate them from
    # neighbouring text symbols (U+2605 black star), and both categories are
    # targeted by default so the practical cleaning result is identical. Emoji
    # *shortcoding* keys off the EmojiNames data table, not this category, so BMP
    # emoji still receive shortcodes when present in that table.
    return ($CodePoint -ge 0x1F000 -and $CodePoint -le 0x1FAFF)
}

function Test-RuneInCjkRange {
    param([int]$CodePoint)

    return (
        ($CodePoint -ge 0x3040  -and $CodePoint -le 0x30FF)  -or # Hiragana + Katakana
        ($CodePoint -ge 0x31F0  -and $CodePoint -le 0x31FF)  -or # Katakana phonetic ext.
        ($CodePoint -ge 0x3400  -and $CodePoint -le 0x4DBF)  -or # CJK Ext-A
        ($CodePoint -ge 0x4E00  -and $CodePoint -le 0x9FFF)  -or # CJK Unified Ideographs
        ($CodePoint -ge 0xF900  -and $CodePoint -le 0xFAFF)  -or # CJK Compatibility Ideographs
        ($CodePoint -ge 0xAC00  -and $CodePoint -le 0xD7A3)  -or # Hangul syllables
        ($CodePoint -ge 0x1100  -and $CodePoint -le 0x11FF)  -or # Hangul Jamo
        ($CodePoint -ge 0x20000 -and $CodePoint -le 0x2FA1F)     # CJK Ext-B..F + compat supplement
    )
}

function Test-RuneIsLatinDiacritic {
    param([int]$CodePoint)

    # Latin code points (with diacritics / extended forms) that fold to ASCII.
    return (
        ($CodePoint -ge 0x00C0 -and $CodePoint -le 0x024F) -or # Latin-1 Supplement + Latin Extended-A/B
        ($CodePoint -ge 0x1E00 -and $CodePoint -le 0x1EFF)     # Latin Extended Additional
    )
}
