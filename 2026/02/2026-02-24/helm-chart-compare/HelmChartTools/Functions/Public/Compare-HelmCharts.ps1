function Compare-HelmCharts {
    <#
    .SYNOPSIS
        Compares two Helm chart directories and reports structural and variable differences.

    .DESCRIPTION
        Performs a comprehensive comparison between two Helm charts:
        - Files present in one chart but not the other
        - Files that have been modified
        - (Optional) Variable differences between charts

    .PARAMETER ReferenceChart
        Path to the reference (original) Helm chart directory.

    .PARAMETER DifferenceChart
        Path to the comparison (new) Helm chart directory.

    .PARAMETER IncludeVariables
        If specified, also compares variable usage between charts.

    .OUTPUTS
        [PSCustomObject] with properties:
        - FilesOnlyInReference: Array of files in ReferenceChart but not in DifferenceChart
        - FilesOnlyInDifference: Array of files in DifferenceChart but not in ReferenceChart
        - ChangedFiles: Array of file paths that exist in both but have different content
        - VariableDifferences: (Only if -IncludeVariables) Object with variable comparison

    .EXAMPLE
        Compare-HelmCharts -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0

    .EXAMPLE
        Compare-HelmCharts -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0 -IncludeVariables
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ReferenceChart,

        [Parameter(Mandatory)]
        [string]$DifferenceChart,

        [switch]$IncludeVariables
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

    Write-Verbose "Comparing charts: $refPath <-> $diffPath"

    # ── Get file lists ────────────────────────────────────────────────────────────
    $refFiles = Get-ChildItem -Path $refPath -Recurse -File |
        ForEach-Object {
            $_.FullName -replace [regex]::Escape($refPath), '' -replace '^[/\\]', ''
        } | Sort-Object

    $diffFiles = Get-ChildItem -Path $diffPath -Recurse -File |
        ForEach-Object {
            $_.FullName -replace [regex]::Escape($diffPath), '' -replace '^[/\\]', ''
        } | Sort-Object

    # ── Find differences ──────────────────────────────────────────────────────────
    $onlyInRef = $refFiles | Where-Object { $_ -notin $diffFiles }
    $onlyInDiff = $diffFiles | Where-Object { $_ -notin $refFiles }

    # Check for modified files
    $changedFiles = @()
    $commonFiles = $refFiles | Where-Object { $_ -in $diffFiles }

    foreach ($file in $commonFiles) {
        $refContent = Get-Content -Path (Join-Path $refPath $file) -Raw -ErrorAction SilentlyContinue
        $diffContent = Get-Content -Path (Join-Path $diffPath $file) -Raw -ErrorAction SilentlyContinue

        if ($refContent -ne $diffContent) {
            $changedFiles += $file
        }
    }

    Write-Verbose "Files only in Reference: $($onlyInRef.Count)"
    Write-Verbose "Files only in Difference: $($onlyInDiff.Count)"
    Write-Verbose "Changed files: $($changedFiles.Count)"

    # ── Build result object ───────────────────────────────────────────────────────
    $result = [PSCustomObject]@{
        FilesOnlyInReference  = @($onlyInRef)
        FilesOnlyInDifference = @($onlyInDiff)
        ChangedFiles          = @($changedFiles)
    }

    # ── Optional: variable comparison ─────────────────────────────────────────────
    if ($IncludeVariables) {
        Write-Verbose "Analyzing variables..."

        $refVars = Get-HelmChartVariables -ChartPath $refPath
        $diffVars = Get-HelmChartVariables -ChartPath $diffPath

        $refVarNames = $refVars | Select-Object -ExpandProperty Name | Sort-Object -Unique
        $diffVarNames = $diffVars | Select-Object -ExpandProperty Name | Sort-Object -Unique

        $varResult = [PSCustomObject]@{
            OnlyInReference  = @($refVarNames | Where-Object { $_ -notin $diffVarNames })
            OnlyInDifference = @($diffVarNames | Where-Object { $_ -notin $refVarNames })
            Common           = @($refVarNames | Where-Object { $_ -in $diffVarNames })
        }

        $result | Add-Member -MemberType NoteProperty -Name 'VariableDifferences' -Value $varResult
    }

    return $result
}
