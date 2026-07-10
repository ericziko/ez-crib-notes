#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $manifest = Join-Path $PSScriptRoot '..' 'UnicodeFileNameTools' 'UnicodeFileNameTools.psd1'
    Import-Module $manifest -Force -ErrorAction Stop
}

AfterAll {
    Remove-Module UnicodeFileNameTools -Force -ErrorAction SilentlyContinue
}

Describe 'ConvertTo-TransliteratedText' {
    It 'folds accented Latin to ASCII: <In> -> <Out>' -ForEach @(
        @{ In = 'café';     Out = 'cafe' }
        @{ In = 'naïve';    Out = 'naive' }
        @{ In = 'Renée';    Out = 'Renee' }
        @{ In = 'Zürich';   Out = 'Zurich' }
        @{ In = 'piñata';   Out = 'pinata' }
        @{ In = 'ångström'; Out = 'angstrom' }
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ In = $In; Out = $Out } {
            param($In, $Out)
            ConvertTo-TransliteratedText -Text $In | Should -BeExactly $Out
        }
    }

    It 'decomposes compatibility forms: <In> -> <Out>' -ForEach @(
        @{ In = '①';    Out = '1' }   # circled digit one
        @{ In = 'ﬁle';  Out = 'file' } # fi ligature
    ) {
        InModuleScope UnicodeFileNameTools -Parameters @{ In = $In; Out = $Out } {
            param($In, $Out)
            ConvertTo-TransliteratedText -Text $In | Should -BeExactly $Out
        }
    }

    It 'leaves plain ASCII unchanged' {
        InModuleScope UnicodeFileNameTools {
            ConvertTo-TransliteratedText -Text 'hello-world.txt' | Should -BeExactly 'hello-world.txt'
        }
    }

    It 'leaves non-Latin scripts intact (no fold available)' {
        InModuleScope UnicodeFileNameTools {
            # Greek letters have no combining marks, so nothing is stripped.
            ConvertTo-TransliteratedText -Text 'αβγ' | Should -BeExactly 'αβγ'
        }
    }

    It 'returns empty string for empty input' {
        InModuleScope UnicodeFileNameTools {
            ConvertTo-TransliteratedText -Text '' | Should -BeExactly ''
        }
    }

    It 'strips a standalone combining mark to empty' {
        InModuleScope UnicodeFileNameTools {
            $combiningAcute = [string][char]0x0301
            ConvertTo-TransliteratedText -Text $combiningAcute | Should -BeExactly ''
        }
    }
}
