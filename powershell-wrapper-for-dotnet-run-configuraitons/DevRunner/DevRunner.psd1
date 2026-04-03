@{
    # --- Module Identity ---
    ModuleVersion     = '1.0.0'
    GUID              = 'a3f7c2e1-84b6-4d2a-9f1e-c5b8d3a60e72'
    Author            = 'Dev Team'
    Description       = 'Ergonomic dotnet run wrappers for multi-project .NET solutions. Provides discoverable commands for running, listing, and inspecting projects without memorising deep paths or launch profile flags.'
    PowerShellVersion = '5.1'

    # --- Module Files ---
    RootModule        = 'DevRunner.psm1'

    # --- Exported Surface ---
    # Only these functions are visible outside the module.
    # Private helpers (Resolve-SolutionRoot, Find-ProjectFile, Get-LaunchSettings)
    # are dot-sourced inside the .psm1 and intentionally NOT listed here.
    FunctionsToExport = @(
        'Get-DotnetProjects'
        'Show-RunConfigurations'
        'Invoke-DotnetRun'
        'Start-DevApp'
    )

    # No aliases or cmdlets exported by this module
    AliasesToExport   = @()
    CmdletsToExport   = @()
    VariablesToExport = @()

    # --- Metadata ---
    PrivateData       = @{
        PSData = @{
            Tags       = @('dotnet', 'development', 'runner', 'cli')
            ProjectUri = ''
        }
    }
}
