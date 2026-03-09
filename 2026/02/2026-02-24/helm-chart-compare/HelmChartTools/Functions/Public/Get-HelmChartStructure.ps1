function Get-HelmChartStructure {
    <#
    .SYNOPSIS
        Displays the structure and metadata of a Helm chart.

    .DESCRIPTION
        Analyzes a Helm chart and returns information about:
        - Chart name, version, and description (from Chart.yaml)
        - File structure and purposes
        - Dependencies
        - Count of unique variables

    .PARAMETER ChartPath
        Path to the Helm chart directory.

    .OUTPUTS
        [PSCustomObject] with properties:
        - ChartName: Name from Chart.yaml
        - Version: Version from Chart.yaml
        - Description: Description from Chart.yaml
        - Files: Array of [file info objects] showing name and purpose
        - Dependencies: Array of dependency names (from Chart.yaml)
        - UniqueVariableCount: Number of unique variables in the chart

    .EXAMPLE
        Get-HelmChartStructure -ChartPath ./my-chart

    .EXAMPLE
        Get-HelmChartStructure -ChartPath ./my-chart | Select-Object ChartName, Version, UniqueVariableCount
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ChartPath
    )

    # ── Validate input ────────────────────────────────────────────────────────────
    if (-not (Test-Path $ChartPath -PathType Container)) {
        Write-Error "Chart path not found: $ChartPath"
        return
    }

    $resolvedPath = (Resolve-Path $ChartPath).Path
    Write-Verbose "Analyzing chart: $resolvedPath"

    # ── Parse Chart.yaml ─────────────────────────────────────────────────────────
    $chartYamlPath = Join-Path $resolvedPath 'Chart.yaml'
    $chartName = 'Unknown'
    $version = 'Unknown'
    $description = ''
    $dependencies = @()

    if (Test-Path $chartYamlPath -PathType Leaf) {
        $chartContent = Get-Content -Path $chartYamlPath -Raw
        $lines = $chartContent -split "`n"

        foreach ($line in $lines) {
            if ($line -match '^name:\s*(.+)') {
                $chartName = $matches[1].Trim() -replace '"', ''
            }
            elseif ($line -match '^version:\s*(.+)') {
                $version = $matches[1].Trim() -replace '"', ''
            }
            elseif ($line -match '^description:\s*(.+)') {
                $description = $matches[1].Trim() -replace '"', ''
            }
            elseif ($line -match '^\s*-\s*name:\s*(.+)') {
                $dependencies += $matches[1].Trim()
            }
        }
    }

    # ── Get file structure ────────────────────────────────────────────────────────
    $files = @()
    $allFiles = Get-ChildItem -Path $resolvedPath -Recurse -File

    foreach ($file in $allFiles) {
        $relativePath = $file.FullName -replace [regex]::Escape($resolvedPath), '' -replace '^[/\\]', ''
        $fileName = $file.Name

        # Categorize file by purpose
        $purpose = switch ($relativePath) {
            'Chart.yaml' { 'Chart metadata' }
            'Chart.lock' { 'Dependency lock file' }
            'values.yaml' { 'Default values' }
            { $_ -match '^values-' } { 'Custom values file' }
            { $_ -match '^templates/' } { 'Template' }
            { $_ -match '^charts/' } { 'Dependency' }
            { $_ -match 'README' } { 'Documentation' }
            { $_ -match '\.tpl$' } { 'Template helper' }
            default { 'Other' }
        }

        $files += [PSCustomObject]@{
            Name    = $relativePath
            Purpose = $purpose
        }
    }

    # ── Count variables ──────────────────────────────────────────────────────────
    $variables = Get-HelmChartVariables -ChartPath $resolvedPath
    $uniqueVariables = $variables | Select-Object -ExpandProperty Name -Unique
    $variableCount = @($uniqueVariables).Count

    # ── Build result ──────────────────────────────────────────────────────────────
    $result = [PSCustomObject]@{
        ChartName               = $chartName
        Version                 = $version
        Description             = $description
        Files                   = $files | Sort-Object -Property Name
        Dependencies            = @($dependencies)
        UniqueVariableCount     = $variableCount
        TotalFileCount          = @($allFiles).Count
    }

    Write-Verbose "Chart: $chartName v$version"
    Write-Verbose "Files: $($result.TotalFileCount)"
    Write-Verbose "Unique variables: $variableCount"
    Write-Verbose "Dependencies: $($dependencies.Count)"

    return $result
}
