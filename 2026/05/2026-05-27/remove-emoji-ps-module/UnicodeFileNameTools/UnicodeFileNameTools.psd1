#
# Module manifest for module 'UnicodeFileNameTools'
#

@{
    RootModule        = 'UnicodeFileNameTools.psm1'
    ModuleVersion     = '1.0.0'
    GUID              = '7910dff1-43be-46b7-b4ad-5dcb0d9c2f36'
    Author            = 'Eric Ziko'
    CompanyName       = 'Eric Ziko'
    Copyright         = '(c) Eric Ziko. All rights reserved.'
    Description       = 'Detect, remove, replace, or substitute emoji and other Unicode characters in file names. Cross-platform PowerShell 7+.'

    PowerShellVersion = '7.0'
    CompatiblePSEditions = @('Core')

    # Public surface. Kept explicit (rather than '*') for discoverability and
    # faster module auto-loading. Must match the files under Public/.
    FunctionsToExport = @(
        'Test-UnicodeFileName'
        'Get-UnicodeFileNameCharacter'
        'Convert-UnicodeFileName'
        'Repair-UnicodeFileName'
    )
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    PrivateData = @{
        PSData = @{
            Tags         = @('Unicode', 'Emoji', 'FileName', 'Rename', 'Sanitize', 'Filesystem', 'PSEdition_Core', 'Windows', 'Linux', 'MacOS')
            ProjectUri   = ''
            LicenseUri   = ''
            ReleaseNotes = 'Initial release: detection, conversion (remove/replace/substitute with layered pipeline), and ShouldProcess-based renaming.'
        }
    }
}
