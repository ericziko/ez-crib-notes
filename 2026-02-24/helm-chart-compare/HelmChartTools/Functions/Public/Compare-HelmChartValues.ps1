function Compare-HelmChartValues {
    <#
    .SYNOPSIS
        Performs a deep diff of YAML values files between two charts.

    .DESCRIPTION
        Compares two YAML files (typically values.yaml) from different charts and reports:
        - Keys added in the new chart
        - Keys removed from the new chart
        - Keys with changed values

    .PARAMETER ReferenceChart
        Path to the reference (original) Helm chart directory.

    .PARAMETER DifferenceChart
        Path to the comparison (new) Helm chart directory.

    .PARAMETER ValuesFile
        Name of the values file to compare. Default is 'values.yaml'.
        Can also compare custom values files like 'values-prod.yaml'.

    .OUTPUTS
        [PSCustomObject] with properties:
        - Added: [hashtable] Keys present in DifferenceChart but not in ReferenceChart
        - Removed: [hashtable] Keys present in ReferenceChart but not in DifferenceChart
        - Changed: [hashtable] Keys with different values (OldValue, NewValue)

    .EXAMPLE
        Compare-HelmChartValues -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0

    .EXAMPLE
        Compare-HelmChartValues -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0 `
          -ValuesFile 'values-prod.yaml'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ReferenceChart,

        [Parameter(Mandatory)]
        [string]$DifferenceChart,

        [string]$ValuesFile = 'values.yaml'
    )

    # ── Validate inputs ───────────────────────────────────────────────────────────
    if (-not (Test-Path $ReferenceChart -PathType Container)) {
        Write-Error "Reference chart not found: $ReferenceChart"
        return
    }

    if (-not (Test-Path $DifferenceChart -PathType Container)) {
        Write-Error "Difference chart not found: $DifferenceChart"
        return
    }

    $refPath = (Resolve-Path $ReferenceChart).Path
    $diffPath = (Resolve-Path $DifferenceChart).Path

    $refValuesPath = Join-Path $refPath $ValuesFile
    $diffValuesPath = Join-Path $diffPath $ValuesFile

    if (-not (Test-Path $refValuesPath -PathType Leaf)) {
        Write-Error "Values file not found in reference chart: $ValuesFile"
        return
    }

    if (-not (Test-Path $diffValuesPath -PathType Leaf)) {
        Write-Error "Values file not found in difference chart: $ValuesFile"
        return
    }

    Write-Verbose "Comparing values files: $ValuesFile"
    Write-Verbose "  Reference: $refValuesPath"
    Write-Verbose "  Difference: $diffValuesPath"

    # ── Convert to flat hashtables ────────────────────────────────────────────────
    $refValues = ConvertTo-FlatYaml -Path $refValuesPath
    $diffValues = ConvertTo-FlatYaml -Path $diffValuesPath

    # ── Compare ───────────────────────────────────────────────────────────────────
    $comparison = Compare-FlatYaml -Reference $refValues -Difference $diffValues

    Write-Verbose "Added: $($comparison.Added.Count) key(s)"
    Write-Verbose "Removed: $($comparison.Removed.Count) key(s)"
    Write-Verbose "Changed: $($comparison.Changed.Count) key(s)"

    return $comparison
}
