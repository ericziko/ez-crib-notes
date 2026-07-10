#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Get-RuneEmojiName' {
    It 'returns the shortcode for a supplementary-plane emoji: <Name>' -ForEach @(
        @{ Name = 'party popper'; Cp = 0x1F389; Code = 'party-popper' }
        @{ Name = 'thumbs up';    Cp = 0x1F44D; Code = 'thumbs-up' }
        @{ Name = 'rocket';       Cp = 0x1F680; Code = 'rocket' }
        @{ Name = 'grinning';     Cp = 0x1F600; Code = 'grinning-face' }
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ Cp = $Cp; Code = $Code } {
            param($Cp, $Code)
            Get-RuneEmojiName -Rune ([System.Text.Rune]::new($Cp)) | Should -BeExactly $Code
        }
    }

    It 'returns the shortcode for a BMP emoji code point' {
        InModuleScope UnicodeFileNameTools {
            Get-RuneEmojiName -Rune ([System.Text.Rune]::new(0x2764)) | Should -BeExactly 'red-heart'
        }
    }

    It 'returns null for a code point not in the table' {
        InModuleScope UnicodeFileNameTools {
            Get-RuneEmojiName -Rune ([System.Text.Rune]::new(0x65E5)) | Should -BeNullOrEmpty  # 日
        }
    }

    It 'honours an additional name table that overrides the default' {
        InModuleScope UnicodeFileNameTools {
            $extra = @{ '1F389' = 'tada' }
            Get-RuneEmojiName -Rune ([System.Text.Rune]::new(0x1F389)) -AdditionalNames $extra | Should -BeExactly 'tada'
        }
    }

    It 'falls back to the default table for code points not in the additional table' {
        InModuleScope UnicodeFileNameTools {
            $extra = @{ '1F389' = 'tada' }
            Get-RuneEmojiName -Rune ([System.Text.Rune]::new(0x1F680)) -AdditionalNames $extra | Should -BeExactly 'rocket'
        }
    }
}
