@{
    RootModule            = 'HelmChartTools.psm1'
    ModuleVersion         = '1.0.0'
    GUID                  = 'f47a2c8b-d3f1-4a9e-b8c2-5f1a3e9d7b2c'
    Author                = 'PowerShell Developer'
    Description           = 'PowerShell module for comparing Helm charts, extracting variables, and managing chart differences across multiple versions and environments'

    PowerShellVersion     = '5.1'
    FunctionsToExport     = @(
        'Get-HelmChartVariables'
        'Compare-HelmCharts'
        'Compare-HelmChartValues'
        'Invoke-HelmVariableReplace'
        'Export-HelmVariableRegistry'
        'Get-HelmChartStructure'
        'Test-HelmVariableConsistency'
    )

    PrivateData = @{
        PSData = @{
            Tags       = @('Helm', 'Kubernetes', 'Docker', 'DevOps', 'Chart', 'Comparison', 'Variables')
            ProjectUri = 'https://github.com/ericziko/ez-crib-notes'
            LicenseUri = ''
        }
    }
}
