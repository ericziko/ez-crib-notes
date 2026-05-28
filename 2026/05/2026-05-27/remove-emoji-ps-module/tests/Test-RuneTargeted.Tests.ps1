#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Test-RuneTargeted' {
    It 'returns true when the rune category is in the target set' {
        InModuleScope UnicodeFileNameTools {
            $emoji = [System.Text.Rune]::new(0x1F600)
            Test-RuneTargeted -Rune $emoji -TargetCategory @('Emoji', 'Symbol') | Should -BeTrue
        }
    }

    It 'returns false when the rune category is not in the target set' {
        InModuleScope UnicodeFileNameTools {
            $accent = [System.Text.Rune]::new(0x00E9)  # é -> Diacritic
            Test-RuneTargeted -Rune $accent -TargetCategory @('Emoji', 'Symbol') | Should -BeFalse
        }
    }

    It 'never targets plain ASCII even if Ascii were requested' {
        InModuleScope UnicodeFileNameTools {
            $a = [System.Text.Rune]::new([int][char]'a')
            Test-RuneTargeted -Rune $a -TargetCategory @('Emoji') | Should -BeFalse
        }
    }

    It 'targets diacritics when Diacritic is opted into' {
        InModuleScope UnicodeFileNameTools {
            $accent = [System.Text.Rune]::new(0x00E9)
            Test-RuneTargeted -Rune $accent -TargetCategory @('Emoji', 'Symbol', 'Diacritic') | Should -BeTrue
        }
    }
}
