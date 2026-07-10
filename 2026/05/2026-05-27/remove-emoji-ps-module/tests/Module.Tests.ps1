#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $script:manifestPath = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $script:manifestPath -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'Module manifest and surface' {
    It 'has a valid manifest' {
        { Test-ModuleManifest -Path $script:manifestPath -ErrorAction Stop } | Should -Not -Throw
    }

    It 'targets PowerShell 7+ Core' {
        $m = Test-ModuleManifest -Path $script:manifestPath
        $m.PowerShellVersion | Should -BeGreaterOrEqual ([version]'7.0')
        $m.CompatiblePSEditions | Should -Contain 'Core'
    }

    It 'exports exactly the four documented public functions' {
        $expected = 'Test-UnicodeFileName', 'Get-UnicodeFileNameCharacter', 'Convert-UnicodeFileName', 'Repair-UnicodeFileName'
        (Get-Command -Module UnicodeFileNameTools).Name | Sort-Object | Should -Be ($expected | Sort-Object)
    }

    It 'does not leak private helpers' {
        Get-Command Get-RuneCategory -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        Get-Command Invoke-SubstitutionPipeline -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }

    It 'has comment-based help on every public function: <Name>' -ForEach @(
        @{ Name = 'Test-UnicodeFileName' }
        @{ Name = 'Get-UnicodeFileNameCharacter' }
        @{ Name = 'Convert-UnicodeFileName' }
        @{ Name = 'Repair-UnicodeFileName' }
    ) {
        $help = Get-Help $Name -Full
        $help.Synopsis      | Should -Not -BeNullOrEmpty
        $help.Description   | Should -Not -BeNullOrEmpty
        $help.Examples      | Should -Not -BeNullOrEmpty
    }
}
