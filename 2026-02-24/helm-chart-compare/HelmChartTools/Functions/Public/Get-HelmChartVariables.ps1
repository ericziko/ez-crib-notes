function Get-HelmChartVariables {
    <#
    .SYNOPSIS
        Extracts all variables from Helm chart files.

    .DESCRIPTION
        Scans a Helm chart directory for all variable references in template files.
        Detects Helm template variables, Helm builtins, Go template variables,
        and environment variables.

    .PARAMETER ChartPath
        Path to the Helm chart directory to scan.

    .PARAMETER VariableType
        Filter to a specific type of variable: All, HelmValue, HelmBuiltin, GoVar, or EnvVar.
        Default is 'All' to detect all types.

    .PARAMETER FileFilter
        Array of file patterns to scan. Default: '*.yaml', '*.yml', '*.tpl'

    .PARAMETER CustomPattern
        Optional regex pattern for custom variable formats. If provided, runs in addition to
        the standard patterns.

    .OUTPUTS
        [PSCustomObject[]] array with properties:
        - Name: Variable name (extracted from match)
        - FullMatch: The complete match from the file (e.g., {{ .Values.something }})
        - Type: Variable type (HelmValue, HelmBuiltin, GoVar, EnvVar, or Custom)
        - File: Relative file path within the chart
        - LineNumber: Line number where found

    .EXAMPLE
        Get-HelmChartVariables -ChartPath ./my-chart

    .EXAMPLE
        Get-HelmChartVariables -ChartPath ./my-chart -VariableType HelmValue

    .EXAMPLE
        Get-HelmChartVariables -ChartPath ./my-chart -FileFilter '*.yaml'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]$ChartPath,

        [ValidateSet('All', 'HelmValue', 'HelmBuiltin', 'GoVar', 'EnvVar')]
        [string]$VariableType = 'All',

        [string[]]$FileFilter = @('*.yaml', '*.yml', '*.tpl'),

        [string]$CustomPattern
    )

    # ── Validate input ────────────────────────────────────────────────────────────
    if (-not (Test-Path $ChartPath -PathType Container)) {
        Write-Error "Chart path not found or is not a directory: $ChartPath"
        return
    }

    $resolvedPath = (Resolve-Path $ChartPath).Path
    Write-Verbose "Scanning chart: $resolvedPath"

    # ── Get patterns ──────────────────────────────────────────────────────────────
    $patterns = Get-HelmVariablePattern -VariableType $VariableType

    # ── Find all matching files ───────────────────────────────────────────────────
    $files = @()
    foreach ($filter in $FileFilter) {
        $files += Get-ChildItem -Path $resolvedPath -Filter $filter -Recurse -File
    }

    if (-not $files) {
        Write-Verbose "No files found matching filter: $($FileFilter -join ', ')"
        return
    }

    Write-Verbose "Found $($files.Count) file(s) to scan"

    # ── Extract variables ─────────────────────────────────────────────────────────
    $variables = @()

    foreach ($file in $files) {
        $content = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $content) {
            continue
        }

        $lines = $content -split "`n"
        $lineNumber = 1

        foreach ($line in $lines) {
            # ── Try each pattern ──────────────────────────────────────────────────
            $patternsToCheck = if ($VariableType -eq 'All') {
                $patterns.Values
            } else {
                @($patterns)
            }

            foreach ($pattern in $patternsToCheck) {
                $matches = [regex]::Matches($line, $pattern)

                foreach ($match in $matches) {
                    $fullMatch = $match.Value.Trim()
                    $name = $fullMatch

                    # Extract just the variable name from the match
                    switch -Regex ($fullMatch) {
                        '^\{\{\s*\.Values\.(.+?)\s*\}\}$' {
                            $name = $matches[-1].Groups[1].Value
                            $varType = 'HelmValue'
                        }
                        '^\{\{\s*\.(Release|Chart|Capabilities|Files|Template|Namespace)' {
                            $name = $fullMatch -replace '[{}]', '' -replace '^\s+|\s+$', ''
                            $varType = 'HelmBuiltin'
                        }
                        '^\{\{\s*\$' {
                            $name = $fullMatch -replace '[{}$\s]', ''
                            $varType = 'GoVar'
                        }
                        '^\$' {
                            $name = $fullMatch -replace '\$|[{}]', ''
                            $varType = 'EnvVar'
                        }
                        default {
                            $varType = if ($VariableType -eq 'All') { 'Unknown' } else { $VariableType }
                        }
                    }

                    $relativeFile = $file.FullName -replace [regex]::Escape($resolvedPath), '' -replace '^[/\\]', ''

                    $variables += [PSCustomObject]@{
                        Name       = $name
                        FullMatch  = $fullMatch
                        Type       = $varType
                        File       = $relativeFile
                        LineNumber = $lineNumber
                    }
                }
            }

            # ── Check custom pattern if provided ──────────────────────────────────
            if ($CustomPattern) {
                $customMatches = [regex]::Matches($line, $CustomPattern)
                foreach ($match in $customMatches) {
                    $variables += [PSCustomObject]@{
                        Name       = $match.Value
                        FullMatch  = $match.Value
                        Type       = 'Custom'
                        File       = $relativeFile
                        LineNumber = $lineNumber
                    }
                }
            }

            $lineNumber++
        }
    }

    Write-Verbose "Found $($variables.Count) variable(s)"
    return $variables | Sort-Object -Property File, LineNumber, Name -Unique
}
