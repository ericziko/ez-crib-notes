#
# DeveloperBootStrapUtilities.psd1
# Module manifest for DeveloperBootStrapUtilities
#
# Edit Author, CompanyName, and Copyright to match your organisation.
#

@{
    # ── Identity ────────────────────────────────────────────────────────────────
    RootModule        = 'DeveloperBootStrapUtilities.psm1'
    ModuleVersion     = '1.0.0'
    GUID              = 'a3f1c2e4-7b8d-4f5a-9c3e-1d2b6a7f0e9c'

    Author            = 'Platform Engineering Team'
    CompanyName       = 'MyOrg'
    Copyright         = "(c) $(Get-Date -Format yyyy) MyOrg. All rights reserved."
    Description       = 'Automates common new-developer environment setup tasks (repository cloning, tooling installation, etc.)'

    # ── Requirements ────────────────────────────────────────────────────────────
    # Requires PowerShell 7+ for ternary operator and other modern syntax
    PowerShellVersion = '7.0'

    # PwshSpectreConsole provides the interactive UI components
    RequiredModules   = @(
        @{ ModuleName = 'PwshSpectreConsole'; ModuleVersion = '1.0.0' }
    )

    # ── Exports ─────────────────────────────────────────────────────────────────
    FunctionsToExport = @(
        'Initialize-GitHubRepositories'
    )

    # Unexported — internal helpers should go in Private/ and NOT be listed here
    CmdletsToExport   = @()
    VariablesToExport = @()
    AliasesToExport   = @()

    # ── Metadata (shown in Find-Module / Install-Module output) ─────────────────
    PrivateData = @{
        PSData = @{
            Tags         = @('bootstrap', 'developer-setup', 'git', 'onboarding', 'spectre')
            ReleaseNotes = '1.0.0 — Initial release: Initialize-GitHubRepositories'
        }
    }
}
