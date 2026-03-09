function Export-HelmVariableRegistry {
    <#
    .SYNOPSIS
        Generates a registry of all variables found in one or more Helm charts.

    .DESCRIPTION
        Scans one or more charts and creates a comprehensive registry of all variables,
        their types, which files use them, and occurrence counts.
        Output can be in Markdown (table), CSV, or JSON format.

    .PARAMETER ChartPaths
        Array of paths to Helm chart directories to scan.

    .PARAMETER Format
        Output format: Markdown (default), CSV, or JSON.

    .PARAMETER OutputPath
        Optional file path to save the registry. If not specified, returns the content as string.

    .OUTPUTS
        [string] containing the formatted registry.
        If -OutputPath is specified, also saves to that file.

    .EXAMPLE
        Export-HelmVariableRegistry -ChartPaths ./my-chart -Format Markdown

    .EXAMPLE
        Export-HelmVariableRegistry -ChartPaths @('./my-chart-v1', './my-chart-v2') `
          -Format CSV -OutputPath ./variables.csv

    .EXAMPLE
        Export-HelmVariableRegistry -ChartPaths ./my-chart -Format JSON | ConvertFrom-Json
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string[]]$ChartPaths,

        [ValidateSet('Markdown', 'CSV', 'JSON')]
        [string]$Format = 'Markdown',

        [string]$OutputPath
    )

    $allVariables = @()

    # ── Scan all charts ──────────────────────────────────────────────────────────
    foreach ($chartPath in $ChartPaths) {
        if (-not (Test-Path $chartPath -PathType Container)) {
            Write-Error "Chart path not found: $chartPath"
            continue
        }

        Write-Verbose "Scanning chart: $chartPath"
        $variables = Get-HelmChartVariables -ChartPath $chartPath

        # Add chart name to each variable
        foreach ($var in $variables) {
            $chartName = Split-Path -Leaf $chartPath
            $var | Add-Member -MemberType NoteProperty -Name 'Chart' -Value $chartName
            $allVariables += $var
        }
    }

    if (-not $allVariables) {
        Write-Warning "No variables found in any charts"
        return ''
    }

    # ── Build registry data ───────────────────────────────────────────────────────
    $registry = $allVariables |
        Group-Object -Property Name, Type |
        ForEach-Object {
            $charts = $_.Group | Select-Object -ExpandProperty Chart | Sort-Object -Unique
            $files = $_.Group | Select-Object -ExpandProperty File | Sort-Object -Unique
            $count = $_.Group.Count

            @{
                Variable = $_.Name -split ', ' | Select-Object -First 1
                Type     = $_.Name -split ', ' | Select-Object -Last 1
                Charts   = $charts -join '; '
                Files    = $files -join '; '
                Count    = $count
            }
        } | Sort-Object -Property Variable

    Write-Verbose "Registry contains $($registry.Count) unique variable(s)"

    # ── Format output ─────────────────────────────────────────────────────────────
    $output = switch ($Format) {
        'Markdown' {
            $lines = @('| Variable | Type | Charts | Files | Count |')
            $lines += '|----------|------|--------|-------|-------|'
            foreach ($item in $registry) {
                $lines += "| $($item.Variable) | $($item.Type) | $($item.Charts) | $($item.Files) | $($item.Count) |"
            }
            $lines -join "`n"
        }

        'CSV' {
            $lines = @('Variable,Type,Charts,Files,Count')
            foreach ($item in $registry) {
                $variable = $item.Variable -replace ',', ';'
                $charts = $item.Charts -replace ',', ';'
                $files = $item.Files -replace ',', ';'
                $lines += "$variable,$($item.Type),$charts,$files,$($item.Count)"
            }
            $lines -join "`n"
        }

        'JSON' {
            $registry | ConvertTo-Json
        }
    }

    # ── Save if requested ─────────────────────────────────────────────────────────
    if ($OutputPath) {
        Set-Content -Path $OutputPath -Value $output -ErrorAction Stop
        Write-Verbose "Registry saved to: $OutputPath"
    }

    return $output
}
