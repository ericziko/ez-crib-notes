function Invoke-HelmVariableReplace {
    <#
    .SYNOPSIS
        Replaces variable references across Helm chart files.

    .DESCRIPTION
        Performs a find-and-replace operation across chart files.
        Supports literal string matching or regex patterns.
        Use -WhatIf to preview changes before executing.

    .PARAMETER ChartPath
        Path to the Helm chart directory to modify.

    .PARAMETER Find
        The string or pattern to find. Treated as literal unless -IsRegex is specified.

    .PARAMETER Replace
        The replacement string.

    .PARAMETER FileFilter
        Array of file patterns to scan. Default: '*.yaml', '*.yml', '*.tpl'

    .PARAMETER IsRegex
        If specified, -Find is treated as a regular expression pattern.

    .PARAMETER WhatIf
        Preview what would be replaced without making actual changes.

    .PARAMETER Confirm
        Prompt for confirmation before making changes.

    .OUTPUTS
        [PSCustomObject[]] array with properties:
        - File: Relative path to modified file
        - LineNumber: Line number where replacement occurred
        - OldText: The text that was replaced
        - NewText: The replacement text

    .EXAMPLE
        Invoke-HelmVariableReplace -ChartPath ./my-chart `
          -Find '{{ .Values.oldName }}' `
          -Replace '{{ .Values.newName }}'

    .EXAMPLE
        Invoke-HelmVariableReplace -ChartPath ./my-chart `
          -Find 'gcr\.io/my-project' `
          -Replace 'docker.io/my-org' `
          -IsRegex

    .EXAMPLE
        # Dry-run first
        Invoke-HelmVariableReplace -ChartPath ./my-chart `
          -Find 'oldValue' `
          -Replace 'newValue' `
          -WhatIf
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$ChartPath,

        [Parameter(Mandatory)]
        [string]$Find,

        [Parameter(Mandatory)]
        [string]$Replace,

        [string[]]$FileFilter = @('*.yaml', '*.yml', '*.tpl'),

        [switch]$IsRegex
    )

    # ── Validate input ────────────────────────────────────────────────────────────
    if (-not (Test-Path $ChartPath -PathType Container)) {
        Write-Error "Chart path not found: $ChartPath"
        return
    }

    $resolvedPath = (Resolve-Path $ChartPath).Path
    Write-Verbose "Scanning chart for replacements: $resolvedPath"

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

    $replacements = @()

    # ── Process each file ─────────────────────────────────────────────────────────
    foreach ($file in $files) {
        $originalContent = Get-Content -Path $file.FullName -Raw -ErrorAction SilentlyContinue
        if (-not $originalContent) {
            continue
        }

        $lines = $originalContent -split "`n"
        $modified = $false
        $newLines = @()
        $lineNumber = 1

        foreach ($line in $lines) {
            $newLine = $line
            $changed = $false

            if ($IsRegex) {
                if ($line -match $Find) {
                    $newLine = $line -replace $Find, $Replace
                    $changed = $true
                }
            } else {
                if ($line -contains $Find) {
                    $newLine = $line.Replace($Find, $Replace)
                    $changed = $true
                }
            }

            if ($changed) {
                $modified = $true
                $relativeFile = $file.FullName -replace [regex]::Escape($resolvedPath), '' -replace '^[/\\]', ''

                $replacements += [PSCustomObject]@{
                    File       = $relativeFile
                    LineNumber = $lineNumber
                    OldText    = $line
                    NewText    = $newLine
                }

                Write-Verbose "Found match in $($relativeFile):$lineNumber"
            }

            $newLines += $newLine
            $lineNumber++
        }

        # ── Write changes if needed ───────────────────────────────────────────────
        if ($modified) {
            $relativeFile = $file.FullName -replace [regex]::Escape($resolvedPath), '' -replace '^[/\\]', ''

            if ($PSCmdlet.ShouldProcess($relativeFile, "Replace $Find with $Replace")) {
                $newContent = $newLines -join "`n"
                Set-Content -Path $file.FullName -Value $newContent -NoNewline -ErrorAction Stop
                Write-Verbose "Updated: $relativeFile"
            }
        }
    }

    Write-Verbose "Found $($replacements.Count) replacement(s)"
    return $replacements
}
