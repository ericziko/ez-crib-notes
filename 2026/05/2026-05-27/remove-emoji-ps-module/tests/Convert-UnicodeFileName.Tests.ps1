#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Convert-UnicodeFileName' {
    It 'is exported by the module' {
        (Get-Command Convert-UnicodeFileName).ModuleName | Should -Be 'UnicodeFileNameTools'
    }

    Context 'default behaviour (Substitute, default targets)' {
        It 'replaces emoji with the default replacement underscore' {
            Convert-UnicodeFileName -Name 'report😀.txt' | Should -BeExactly 'report_.txt'
        }

        It 'leaves a clean ASCII name unchanged' {
            Convert-UnicodeFileName -Name 'plain-file_v2.txt' | Should -BeExactly 'plain-file_v2.txt'
        }

        It 'leaves accented Latin letters alone by default (Diacritic not targeted)' {
            Convert-UnicodeFileName -Name 'café😀.txt' | Should -BeExactly 'café_.txt'
        }

        It 'strips invisible characters (e.g. zero-width joiner) by default' {
            $name = "a$([char]0x200D)b.txt"
            Convert-UnicodeFileName -Name $name | Should -BeExactly 'a_b.txt'
        }
    }

    Context 'Action = Remove' {
        It 'deletes targeted characters entirely' {
            Convert-UnicodeFileName -Name 'a😀b🎉c.txt' -Action Remove | Should -BeExactly 'abc.txt'
        }
    }

    Context 'Action = Replace' {
        It 'uses a custom replacement string' {
            Convert-UnicodeFileName -Name 'a😀b.txt' -Action Replace -Replacement '-' | Should -BeExactly 'a-b.txt'
        }
    }

    Context 'transliteration' {
        It 'folds accents to ASCII when Diacritic is targeted and -Transliterate is set' {
            Convert-UnicodeFileName -Name 'Crème_Brûlée.txt' -TargetCategory Diacritic -Transliterate |
                Should -BeExactly 'Creme_Brulee.txt'
        }
    }

    Context 'emoji shortcodes' {
        It 'substitutes :shortcode: when -EmojiNames is set' {
            Convert-UnicodeFileName -Name 'release🎉.txt' -EmojiNames | Should -BeExactly 'release:party-popper:.txt'
        }
    }

    Context 'custom map' {
        It 'applies an explicit map keyed by the literal character' {
            Convert-UnicodeFileName -Name 'a😀b.txt' -Map @{ '😀' = '-smiley-' } | Should -BeExactly 'a-smiley-b.txt'
        }
    }

    Context 'CollapseRuns' {
        It 'collapses consecutive replacements and trims leading/trailing ones' {
            Convert-UnicodeFileName -Name '😀😀😀data.txt' -CollapseRuns | Should -BeExactly 'data.txt'
        }

        It 'collapses an internal run to a single replacement' {
            Convert-UnicodeFileName -Name 'a😀😀b.txt' -CollapseRuns | Should -BeExactly 'a_b.txt'
        }
    }

    Context 'filesystem-illegal characters' {
        It 'always cleans characters illegal on the current OS (e.g. forward slash)' {
            Convert-UnicodeFileName -Name 'a/b.txt' | Should -BeExactly 'a_b.txt'
        }

        It 'cleans the Windows-reserved set only when -PortableNames is set' {
            # ':' is legal on macOS/Linux but reserved on Windows.
            Convert-UnicodeFileName -Name 'a:b.txt' -PortableNames | Should -BeExactly 'a_b.txt'
        }
    }

    Context 'pipeline' {
        It 'processes multiple names from the pipeline' {
            $result = 'a😀.txt', 'b🎉.txt' | Convert-UnicodeFileName -Action Remove
            $result | Should -Be @('a.txt', 'b.txt')
        }
    }
}
