#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Get-RuneCategory' {
    # Helper: build a Rune from a code point.
    BeforeAll {
        function script:R([int]$cp) { [System.Text.Rune]::new($cp) }
    }

    It 'classifies printable ASCII as Ascii: <Name>' -ForEach @(
        @{ Name = 'lowercase letter'; Cp = 0x0061 }  # a
        @{ Name = 'uppercase letter'; Cp = 0x005A }  # Z
        @{ Name = 'digit';            Cp = 0x0039 }  # 9
        @{ Name = 'space';            Cp = 0x0020 }  # space
        @{ Name = 'hyphen';           Cp = 0x002D }  # -
        @{ Name = 'period';           Cp = 0x002E }  # .
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Ascii'
        }
    }

    It 'classifies ASCII control as Control' {
        InModuleScope UnicodeFileNameTools {
            Get-RuneCategory -Rune ([System.Text.Rune]::new(0x0007)) | Should -Be 'Control'
        }
    }

    It 'classifies supplementary-plane pictographs as Emoji: <Name>' -ForEach @(
        @{ Name = 'grinning face';     Cp = 0x1F600 }
        @{ Name = 'party popper';      Cp = 0x1F389 }
        @{ Name = 'rocket';            Cp = 0x1F680 }
        @{ Name = 'sparkling heart';   Cp = 0x1F496 }
        @{ Name = 'sign of the horns'; Cp = 0x1F918 }
        @{ Name = 'regional ind. A';   Cp = 0x1F1E6 }
        @{ Name = 'skin tone';         Cp = 0x1F3FB }
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Emoji'
        }
    }

    It 'classifies symbols as Symbol: <Name>' -ForEach @(
        @{ Name = 'rightwards arrow'; Cp = 0x2192 }  # →  math symbol
        @{ Name = 'black star';       Cp = 0x2605 }  # ★  other symbol
        @{ Name = 'copyright';        Cp = 0x00A9 }  # ©
        @{ Name = 'trademark';        Cp = 0x2122 }  # ™
        @{ Name = 'red heart (BMP)';  Cp = 0x2764 }  # ❤  legacy BMP emoji -> Symbol
        @{ Name = 'sparkles (BMP)';   Cp = 0x2728 }  # ✨  legacy BMP emoji -> Symbol
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Symbol'
        }
    }

    It 'classifies accented Latin letters as Diacritic: <Name>' -ForEach @(
        @{ Name = 'e acute';        Cp = 0x00E9 }  # é
        @{ Name = 'n tilde';        Cp = 0x00F1 }  # ñ
        @{ Name = 'u umlaut';       Cp = 0x00FC }  # ü
        @{ Name = 'c cedilla';      Cp = 0x00E7 }  # ç
        @{ Name = 'a macron';       Cp = 0x0101 }  # ā  (Latin Extended-A)
        @{ Name = 'combining acute';Cp = 0x0301 }  # ́   (combining mark)
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Diacritic'
        }
    }

    It 'classifies CJK/East-Asian letters as Cjk: <Name>' -ForEach @(
        @{ Name = 'cjk ri';     Cp = 0x65E5 }  # 日
        @{ Name = 'hiragana a'; Cp = 0x3042 }  # あ
        @{ Name = 'katakana a'; Cp = 0x30A2 }  # ア
        @{ Name = 'hangul han'; Cp = 0xD55C }  # 한
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Cjk'
        }
    }

    It 'classifies non-ASCII whitespace as Whitespace: <Name>' -ForEach @(
        @{ Name = 'no-break space';  Cp = 0x00A0 }
        @{ Name = 'em space';        Cp = 0x2003 }
        @{ Name = 'ideographic sp.'; Cp = 0x3000 }
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Whitespace'
        }
    }

    It 'classifies invisible/format characters as Invisible: <Name>' -ForEach @(
        @{ Name = 'zero width joiner';     Cp = 0x200D }
        @{ Name = 'zero width non-joiner'; Cp = 0x200C }
        @{ Name = 'variation selector-16'; Cp = 0xFE0F }
        @{ Name = 'byte order mark';       Cp = 0xFEFF }
        @{ Name = 'word joiner';           Cp = 0x2060 }
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Invisible'
        }
    }

    It 'classifies non-ASCII punctuation as Punctuation: <Name>' -ForEach @(
        @{ Name = 'left guillemet';   Cp = 0x00AB }  # «
        @{ Name = 'em dash';          Cp = 0x2014 }  # —
        @{ Name = 'left double quote';Cp = 0x201C }  # “
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'Punctuation'
        }
    }

    It 'classifies other non-ASCII scripts as OtherNonAscii: <Name>' -ForEach @(
        @{ Name = 'greek alpha';    Cp = 0x03B1 }  # α
        @{ Name = 'cyrillic de';    Cp = 0x0434 }  # д
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp } {
            param($Cp)
            Get-RuneCategory -Rune ([System.Text.Rune]::new($Cp)) | Should -Be 'OtherNonAscii'
        }
    }
}
