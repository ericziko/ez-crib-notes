#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Invoke-SubstitutionPipeline' {
    Context 'Action = Remove' {
        It 'returns empty string regardless of other options' {
            InModuleScope UnicodeFileNameTools {
                $emoji = [System.Text.Rune]::new(0x1F389)
                Invoke-SubstitutionPipeline -Rune $emoji -Action Remove -Replacement '_' -Transliterate -EmojiNames |
                    Should -BeExactly ''
            }
        }
    }

    Context 'Action = Replace' {
        It 'always returns the replacement, ignoring map/transliterate/emoji' {
            InModuleScope UnicodeFileNameTools {
                $emoji = [System.Text.Rune]::new(0x1F389)
                Invoke-SubstitutionPipeline -Rune $emoji -Action Replace -Replacement '_' `
                    -Transliterate -EmojiNames -Map @{ '1F389' = 'nope' } | Should -BeExactly '_'
            }
        }
    }

    Context 'Action = Substitute' {
        It 'uses an explicit map entry keyed by the literal character' {
            InModuleScope UnicodeFileNameTools {
                $emoji = [System.Text.Rune]::new(0x1F600)
                $map = @{ ([System.Text.Rune]::new(0x1F600)).ToString() = 'happy' }
                Invoke-SubstitutionPipeline -Rune $emoji -Action Substitute -Replacement '_' -Map $map |
                    Should -BeExactly 'happy'
            }
        }

        It 'uses an explicit map entry keyed by U+XXXX' {
            InModuleScope UnicodeFileNameTools {
                $emoji = [System.Text.Rune]::new(0x1F600)
                Invoke-SubstitutionPipeline -Rune $emoji -Action Substitute -Replacement '_' -Map @{ 'U+1F600' = 'grin' } |
                    Should -BeExactly 'grin'
            }
        }

        It 'transliterates accented letters to ASCII when -Transliterate is set' {
            InModuleScope UnicodeFileNameTools {
                $accent = [System.Text.Rune]::new(0x00E9)  # é
                Invoke-SubstitutionPipeline -Rune $accent -Action Substitute -Replacement '_' -Transliterate |
                    Should -BeExactly 'e'
            }
        }

        It 'substitutes emoji shortcodes wrapped in colons when -EmojiNames is set' {
            InModuleScope UnicodeFileNameTools {
                $emoji = [System.Text.Rune]::new(0x1F389)
                Invoke-SubstitutionPipeline -Rune $emoji -Action Substitute -Replacement '_' -EmojiNames |
                    Should -BeExactly ':party-popper:'
            }
        }

        It 'applies priority Map > Transliterate > EmojiNames > Replacement' {
            InModuleScope UnicodeFileNameTools {
                $accent = [System.Text.Rune]::new(0x00E9)  # é, transliterates to 'e'
                # Map should win over transliteration.
                Invoke-SubstitutionPipeline -Rune $accent -Action Substitute -Replacement '_' `
                    -Transliterate -Map @{ 'U+00E9' = 'XX' } | Should -BeExactly 'XX'
            }
        }

        It 'falls back to the replacement when nothing else applies' {
            InModuleScope UnicodeFileNameTools {
                $skin = [System.Text.Rune]::new(0x1F3FB)  # skin-tone modifier, not in name table
                Invoke-SubstitutionPipeline -Rune $skin -Action Substitute -Replacement '_' -EmojiNames -Transliterate |
                    Should -BeExactly '_'
            }
        }

        It 'falls back to the replacement when no strategies are enabled' {
            InModuleScope UnicodeFileNameTools {
                $emoji = [System.Text.Rune]::new(0x1F389)
                Invoke-SubstitutionPipeline -Rune $emoji -Action Substitute -Replacement '#' |
                    Should -BeExactly '#'
            }
        }
    }
}
