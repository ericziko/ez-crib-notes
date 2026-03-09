function Test-HelmVariableConsistency {
    <#
    .SYNOPSIS
        Validates that variables are used consistently across multiple Helm charts.

    .DESCRIPTION
        Compares variables across multiple charts and identifies inconsistencies:
        - Variables used in one chart but not others
        - Variables with different default values across charts
        - Missing variable definitions (used in templates but not in values.yaml)

    .PARAMETER ChartPaths
        Array of paths to Helm chart directories to compare.

    .PARAMETER ReferenceChart
        Optional path to a specific chart to use as the reference.
        If not specified, uses the first chart in ChartPaths.

    .OUTPUTS
        [PSCustomObject[]] array of consistency issues, each with properties:
        - VariableName: Name of the variable
        - Issue: Description of the inconsistency
        - Charts: Array of charts where the issue occurs
        - Severity: Error, Warning, or Info

    .EXAMPLE
        Test-HelmVariableConsistency -ChartPaths @('./my-chart-v1', './my-chart-v2')

    .EXAMPLE
        Test-HelmVariableConsistency -ChartPaths @('./my-chart-v1', './my-chart-v2') `
          -ReferenceChart './my-chart-v1'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$ChartPaths,

        [string]$ReferenceChart
    )

    # ── Validate inputs ───────────────────────────────────────────────────────────
    if (@($ChartPaths).Count -lt 2) {
        Write-Error "At least 2 charts are required for consistency testing"
        return
    }

    $validPaths = @()
    foreach ($path in $ChartPaths) {
        if (Test-Path $path -PathType Container) {
            $validPaths += (Resolve-Path $path).Path
        } else {
            Write-Warning "Chart path not found, skipping: $path"
        }
    }

    if ($validPaths.Count -lt 2) {
        Write-Error "Could not find at least 2 valid chart paths"
        return
    }

    # ── Determine reference chart ─────────────────────────────────────────────────
    $refPath = if ($ReferenceChart) {
        if (Test-Path $ReferenceChart -PathType Container) {
            (Resolve-Path $ReferenceChart).Path
        } else {
            Write-Error "Reference chart not found: $ReferenceChart"
            return
        }
    } else {
        $validPaths[0]
    }

    $refChartName = Split-Path -Leaf $refPath
    Write-Verbose "Using reference chart: $refChartName"

    # ── Extract variables from all charts ─────────────────────────────────────────
    $chartVariables = @{}
    foreach ($path in $validPaths) {
        $chartName = Split-Path -Leaf $path
        $variables = Get-HelmChartVariables -ChartPath $path
        $chartVariables[$chartName] = $variables
    }

    $refVariables = Get-HelmChartVariables -ChartPath $refPath
    $refVarNames = $refVariables | Select-Object -ExpandProperty Name | Sort-Object -Unique

    # ── Check consistency ─────────────────────────────────────────────────────────
    $issues = @()

    # Check if variables are used consistently
    foreach ($varName in $refVarNames) {
        $chartsWithVar = @()
        $chartsWithoutVar = @()

        foreach ($chartName in $chartVariables.Keys) {
            $path = if ($chartName -eq $refChartName) { $refPath } else {
                $validPaths | Where-Object { (Split-Path -Leaf $_) -eq $chartName } | Select-Object -First 1
            }
            $vars = Get-HelmChartVariables -ChartPath $path
            $hasVar = $vars | Where-Object { $_.Name -eq $varName }

            if ($hasVar) {
                $chartsWithVar += $chartName
            } else {
                $chartsWithoutVar += $chartName
            }
        }

        # Report variable used in some but not all charts
        if ($chartsWithoutVar.Count -gt 0 -and $chartsWithVar.Count -gt 0) {
            $issues += [PSCustomObject]@{
                VariableName = $varName
                Issue        = "Variable used in some charts but not others"
                Charts       = $chartsWithoutVar
                Severity     = 'Warning'
            }
        }
    }

    # Check for values.yaml consistency
    $refValuesPath = Join-Path $refPath 'values.yaml'
    if (Test-Path $refValuesPath -PathType Leaf) {
        $refValues = ConvertTo-FlatYaml -Path $refValuesPath
        $refValueNames = $refValues.Keys | Sort-Object

        foreach ($chartPath in $validPaths) {
            if ($chartPath -eq $refPath) { continue }

            $chartName = Split-Path -Leaf $chartPath
            $chartValuesPath = Join-Path $chartPath 'values.yaml'

            if (Test-Path $chartValuesPath -PathType Leaf) {
                $chartValues = ConvertTo-FlatYaml -Path $chartValuesPath
                $comparison = Compare-FlatYaml -Reference $refValues -Difference $chartValues

                # Report missing keys
                if ($comparison.Removed.Count -gt 0) {
                    $issues += [PSCustomObject]@{
                        VariableName = "Multiple values"
                        Issue        = "Values defined in reference but missing in $chartName"
                        Charts       = @($chartName)
                        Severity     = 'Warning'
                    }
                }

                # Report added keys
                if ($comparison.Added.Count -gt 0) {
                    $issues += [PSCustomObject]@{
                        VariableName = "Multiple values"
                        Issue        = "New values in $chartName not in reference"
                        Charts       = @($chartName)
                        Severity     = 'Info'
                    }
                }

                # Report changed values
                if ($comparison.Changed.Count -gt 0) {
                    foreach ($key in $comparison.Changed.Keys) {
                        $oldVal = $comparison.Changed[$key].OldValue
                        $newVal = $comparison.Changed[$key].NewValue

                        $issues += [PSCustomObject]@{
                            VariableName = $key
                            Issue        = "Value changed from '$oldVal' to '$newVal'"
                            Charts       = @($refChartName, $chartName)
                            Severity     = 'Info'
                        }
                    }
                }
            }
        }
    }

    Write-Verbose "Found $($issues.Count) consistency issue(s)"
    return $issues
}
