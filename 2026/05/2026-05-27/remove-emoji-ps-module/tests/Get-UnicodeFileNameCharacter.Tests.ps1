#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Get-UnicodeFileNameCharacter' {
    It 'is exported by the module' {
        (Get-Command Get-UnicodeFileNameCharacter).ModuleName | Should -Be 'UnicodeFileNameTools'
    }

    It 'emits nothing for a pure-ASCII name' {
        Get-UnicodeFileNameCharacter -Name 'plain-file.txt' | Should -BeNullOrEmpty
    }

    It 'reports a single emoji with full detail' {
        $info = Get-UnicodeFileNameCharacter -Name 'a🎉b.txt'
        $info | Should -HaveCount 1
        $info.Character       | Should -BeExactly '🎉'
        $info.CodePoint       | Should -BeExactly 'U+1F389'
        $info.LogicalCategory | Should -Be 'Emoji'
        $info.IsTargeted      | Should -BeTrue
        $info.EmojiName       | Should -BeExactly 'party-popper'
        $info.Index           | Should -Be 1   # UTF-16 offset (a = 0, emoji starts at 1)
    }

    It 'reports non-ASCII characters that are not targeted, flagging IsTargeted = false' {
        $info = Get-UnicodeFileNameCharacter -Name 'café.txt'
        $info | Should -HaveCount 1
        $info.Character       | Should -BeExactly 'é'
        $info.LogicalCategory | Should -Be 'Diacritic'
        $info.IsTargeted      | Should -BeFalse  # Diacritic not in default targets
    }

    It 'reports every non-ASCII character in order' {
        $info = Get-UnicodeFileNameCharacter -Name 'café🎉.txt'
        $info | Should -HaveCount 2
        $info[0].Character | Should -BeExactly 'é'
        $info[1].Character | Should -BeExactly '🎉'
    }

    It 'with -TargetedOnly emits only targeted characters' {
        $info = Get-UnicodeFileNameCharacter -Name 'café🎉.txt' -TargetedOnly
        $info | Should -HaveCount 1
        $info.Character | Should -BeExactly '🎉'
    }

    It 'honours a custom -TargetCategory for the IsTargeted flag' {
        $info = Get-UnicodeFileNameCharacter -Name 'café.txt' -TargetCategory Diacritic
        $info.IsTargeted | Should -BeTrue
    }

    It 'accepts FileInfo objects from the pipeline' {
        $file = [System.IO.FileInfo]::new('/tmp/holiday🎉.txt')
        $info = $file | Get-UnicodeFileNameCharacter
        $info.Character | Should -BeExactly '🎉'
    }
}
