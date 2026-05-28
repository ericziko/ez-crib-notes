#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Test-UnicodeFileName' {
    It 'is exported by the module' {
        (Get-Command Test-UnicodeFileName).ModuleName | Should -Be 'UnicodeFileNameTools'
    }

    It 'returns false for a clean ASCII name' {
        Test-UnicodeFileName -Name 'plain-file_v2.txt' | Should -BeFalse
    }

    It 'returns true when a targeted character is present' {
        Test-UnicodeFileName -Name 'release🎉.txt' | Should -BeTrue
    }

    It 'returns false for accented Latin by default (Diacritic not targeted)' {
        Test-UnicodeFileName -Name 'café.txt' | Should -BeFalse
    }

    It 'returns true for accented Latin when Diacritic is targeted' {
        Test-UnicodeFileName -Name 'café.txt' -TargetCategory Diacritic | Should -BeTrue
    }

    It 'returns a boolean type, not a truthy object' {
        Test-UnicodeFileName -Name 'release🎉.txt' | Should -BeOfType [bool]
    }

    Context '-Detailed' {
        It 'returns a result object with matched characters' {
            $result = Test-UnicodeFileName -Name 'a🎉b.txt' -Detailed
            $result.Name              | Should -BeExactly 'a🎉b.txt'
            $result.IsMatch           | Should -BeTrue
            $result.Count             | Should -Be 1
            $result.MatchedCharacter.Character | Should -BeExactly '🎉'
        }

        It 'reports IsMatch false and empty matches for a clean name' {
            $result = Test-UnicodeFileName -Name 'clean.txt' -Detailed
            $result.IsMatch | Should -BeFalse
            $result.Count   | Should -Be 0
        }
    }

    Context 'pipeline' {
        It 'returns one boolean per piped name' {
            $results = 'clean.txt', 'party🎉.txt' | Test-UnicodeFileName
            $results | Should -Be @($false, $true)
        }

        It 'accepts FileInfo objects' {
            $file = [System.IO.FileInfo]::new('/tmp/holiday🎉.txt')
            $file | Test-UnicodeFileName | Should -BeTrue
        }
    }
}
