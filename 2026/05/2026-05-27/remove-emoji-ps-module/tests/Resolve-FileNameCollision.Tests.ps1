#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Resolve-FileNameCollision' {
    It 'returns the desired name unchanged when there is no collision' {
        InModuleScope UnicodeFileNameTools {
            Resolve-FileNameCollision -DesiredName 'report.txt' -ExistingName @('other.txt') -CollisionAction Suffix |
                Should -BeExactly 'report.txt'
        }
    }

    Context 'CollisionAction = Suffix' {
        It 'appends " (1)" before the extension on a single collision' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'report.txt' -ExistingName @('report.txt') -CollisionAction Suffix |
                    Should -BeExactly 'report (1).txt'
            }
        }

        It 'increments the suffix until a free name is found' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'report.txt' `
                    -ExistingName @('report.txt', 'report (1).txt') -CollisionAction Suffix |
                    Should -BeExactly 'report (2).txt'
            }
        }

        It 'handles names without an extension' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'README' -ExistingName @('README') -CollisionAction Suffix |
                    Should -BeExactly 'README (1)'
            }
        }

        It 'treats collisions case-insensitively' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'Report.txt' -ExistingName @('report.txt') -CollisionAction Suffix |
                    Should -BeExactly 'Report (1).txt'
            }
        }
    }

    Context 'CollisionAction = Skip' {
        It 'returns null on collision' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'report.txt' -ExistingName @('report.txt') -CollisionAction Skip |
                    Should -BeNullOrEmpty
            }
        }

        It 'returns the name when there is no collision' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'report.txt' -ExistingName @() -CollisionAction Skip |
                    Should -BeExactly 'report.txt'
            }
        }
    }

    Context 'CollisionAction = Fail' {
        It 'throws on collision' {
            InModuleScope UnicodeFileNameTools {
                { Resolve-FileNameCollision -DesiredName 'report.txt' -ExistingName @('report.txt') -CollisionAction Fail } |
                    Should -Throw
            }
        }
    }

    Context 'CollisionAction = Overwrite' {
        It 'returns the desired name even on collision' {
            InModuleScope UnicodeFileNameTools {
                Resolve-FileNameCollision -DesiredName 'report.txt' -ExistingName @('report.txt') -CollisionAction Overwrite |
                    Should -BeExactly 'report.txt'
            }
        }
    }
}
