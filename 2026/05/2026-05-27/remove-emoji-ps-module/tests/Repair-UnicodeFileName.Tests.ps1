#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Repair-UnicodeFileName' {
    It 'is exported by the module' {
        (Get-Command Repair-UnicodeFileName).ModuleName | Should -Be 'UnicodeFileNameTools'
    }

    Context 'basic rename' {
        BeforeEach {
            $script:dir = Join-Path $TestDrive ([guid]::NewGuid().Guid)
            New-Item -ItemType Directory -Path $script:dir | Out-Null
        }

        It 'renames a file containing emoji to the cleaned name' {
            $original = Join-Path $script:dir 'release🎉.txt'
            New-Item -ItemType File -Path $original | Out-Null

            $result = Repair-UnicodeFileName -Path $original -Action Remove

            Test-Path -LiteralPath $original | Should -BeFalse
            Test-Path -LiteralPath (Join-Path $script:dir 'release.txt') | Should -BeTrue
            $result.Status      | Should -Be 'Renamed'
            $result.OriginalName | Should -BeExactly 'release🎉.txt'
            $result.FinalName    | Should -BeExactly 'release.txt'
        }

        It 'reports Unchanged for a clean file' {
            $clean = Join-Path $script:dir 'clean.txt'
            New-Item -ItemType File -Path $clean | Out-Null

            $result = Repair-UnicodeFileName -Path $clean

            Test-Path -LiteralPath $clean | Should -BeTrue
            $result.Status | Should -Be 'Unchanged'
        }

        It 'renames a directory as well as a file' {
            $emojiDir = Join-Path $script:dir 'photos🎉'
            New-Item -ItemType Directory -Path $emojiDir | Out-Null

            $result = Repair-UnicodeFileName -Path $emojiDir -Action Remove

            Test-Path -LiteralPath $emojiDir | Should -BeFalse
            Test-Path -LiteralPath (Join-Path $script:dir 'photos') | Should -BeTrue
            $result.Status | Should -Be 'Renamed'
        }
    }

    Context '-WhatIf' {
        BeforeEach {
            $script:dir = Join-Path $TestDrive ([guid]::NewGuid().Guid)
            New-Item -ItemType Directory -Path $script:dir | Out-Null
        }

        It 'does not modify the filesystem and reports WhatIf status' {
            $original = Join-Path $script:dir 'preview😀.txt'
            New-Item -ItemType File -Path $original | Out-Null

            $result = Repair-UnicodeFileName -Path $original -Action Remove -WhatIf

            Test-Path -LiteralPath $original | Should -BeTrue
            $result.Status      | Should -Be 'WhatIf'
            $result.FinalName    | Should -BeExactly 'preview.txt'
        }
    }

    Context 'collision handling' {
        BeforeEach {
            $script:dir = Join-Path $TestDrive ([guid]::NewGuid().Guid)
            New-Item -ItemType Directory -Path $script:dir | Out-Null
        }

        It 'Suffix appends " (1)" when the cleaned name already exists' {
            New-Item -ItemType File -Path (Join-Path $script:dir 'report.txt') | Out-Null
            $emoji = Join-Path $script:dir 'report😀.txt'
            New-Item -ItemType File -Path $emoji | Out-Null

            $result = Repair-UnicodeFileName -Path $emoji -Action Remove -CollisionAction Suffix

            Test-Path -LiteralPath (Join-Path $script:dir 'report.txt')       | Should -BeTrue
            Test-Path -LiteralPath (Join-Path $script:dir 'report (1).txt')   | Should -BeTrue
            $result.FinalName | Should -BeExactly 'report (1).txt'
            $result.Status    | Should -Be 'Renamed'
        }

        It 'Skip leaves the file alone and reports Skipped on collision' {
            New-Item -ItemType File -Path (Join-Path $script:dir 'report.txt') | Out-Null
            $emoji = Join-Path $script:dir 'report😀.txt'
            New-Item -ItemType File -Path $emoji | Out-Null

            $result = Repair-UnicodeFileName -Path $emoji -Action Remove -CollisionAction Skip

            Test-Path -LiteralPath $emoji | Should -BeTrue
            $result.Status | Should -Be 'Skipped'
        }
    }

    Context '-Recurse' {
        It 'descends into subdirectories and renames matching files' {
            $root = Join-Path $TestDrive ([guid]::NewGuid().Guid)
            $sub  = Join-Path $root 'nested'
            New-Item -ItemType Directory -Path $sub | Out-Null
            New-Item -ItemType File -Path (Join-Path $sub 'inner🎉.txt') | Out-Null

            Repair-UnicodeFileName -Path $root -Recurse -Action Remove | Out-Null

            Test-Path -LiteralPath (Join-Path $sub 'inner🎉.txt') | Should -BeFalse
            Test-Path -LiteralPath (Join-Path $sub 'inner.txt')   | Should -BeTrue
        }
    }

    Context 'pipeline' {
        It 'accepts FileInfo from Get-ChildItem' {
            $dir = Join-Path $TestDrive ([guid]::NewGuid().Guid)
            New-Item -ItemType Directory -Path $dir | Out-Null
            New-Item -ItemType File -Path (Join-Path $dir 'a🎉.txt') | Out-Null
            New-Item -ItemType File -Path (Join-Path $dir 'b😀.txt') | Out-Null

            $results = Get-ChildItem -LiteralPath $dir | Repair-UnicodeFileName -Action Remove

            $results | Should -HaveCount 2
            $results.Status | Should -Be @('Renamed', 'Renamed')
            (Get-ChildItem -LiteralPath $dir).Name | Sort-Object | Should -Be @('a.txt', 'b.txt')
        }
    }

    Context '-PassThru' {
        It 'returns the renamed item' {
            $dir = Join-Path $TestDrive ([guid]::NewGuid().Guid)
            New-Item -ItemType Directory -Path $dir | Out-Null
            $original = Join-Path $dir 'x🎉.txt'
            New-Item -ItemType File -Path $original | Out-Null

            $item = Repair-UnicodeFileName -Path $original -Action Remove -PassThru

            $item.Name | Should -BeExactly 'x.txt'
        }
    }
}
