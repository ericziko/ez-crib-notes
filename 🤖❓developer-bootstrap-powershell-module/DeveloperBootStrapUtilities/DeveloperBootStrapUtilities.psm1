#Requires -Modules PwshSpectreConsole

<#
.SYNOPSIS
    DeveloperBootStrapUtilities — Automate common new-developer environment setup tasks.

.DESCRIPTION
    A PowerShell module that wraps repetitive onboarding toil into interactive,
    well-presented CLI experiences using Spectre.Console.

    Functions exported:
        Initialize-GitHubRepositories  — interactively clone team repositories
#>

function Initialize-GitHubRepositories {
    <#
    .SYNOPSIS
        Interactively clone a curated set of GitHub repositories for developer onboarding.

    .DESCRIPTION
        Reads a list of GitHub repositories from a JSON config file adjacent to this module,
        presents an interactive multi-select menu using Spectre.Console, then clones the
        selected repos into the configured base directory. A summary table is shown on completion.

        Authentication is handled by your existing Git credential store (Windows Credential
        Manager, Git Credential Manager, SSH agent, or a configured PAT).

    .PARAMETER ConfigPath
        Path to the JSON configuration file.
        Defaults to 'repositories.json' in the same directory as this module.

    .PARAMETER ClonePath
        Override the default clone root directory specified in the config file.
        Environment variables in the config value (e.g. $env:USERPROFILE) are expanded.

    .PARAMETER SelectAll
        Skip the interactive prompt and clone every repository in the config.
        Useful for automated/CI scenarios.

    .EXAMPLE
        Initialize-GitHubRepositories

        Loads repositories.json from the module directory, shows the multi-select picker,
        and clones selected repos to the configured defaultClonePath.

    .EXAMPLE
        Initialize-GitHubRepositories -ClonePath "D:\Projects"

        Same as above but overrides the clone destination to D:\Projects.

    .EXAMPLE
        Initialize-GitHubRepositories -SelectAll -ClonePath "$env:USERPROFILE\src"

        Clones all repositories non-interactively — suitable for a bootstrap script.

    .LINK
        https://github.com/ShaunLawrie/PwshSpectreConsole
    #>
    [CmdletBinding()]
    param (
        [Parameter(HelpMessage = 'Path to the repositories JSON config. Defaults to repositories.json beside this module.')]
        [string]$ConfigPath = (Join-Path $PSScriptRoot 'repositories.json'),

        [Parameter(HelpMessage = 'Override the base directory where repositories will be cloned.')]
        [string]$ClonePath,

        [Parameter(HelpMessage = 'Clone all repositories without showing the interactive prompt.')]
        [switch]$SelectAll
    )

    # ── Prerequisites ────────────────────────────────────────────────────────────

    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Error 'git is not installed or not on PATH. Install Git for Windows: https://git-scm.com/download/win'
        return
    }

    if (-not (Get-Module -Name PwshSpectreConsole -ListAvailable)) {
        Write-Error @'
PwshSpectreConsole is required but not installed.
Install it with:

    Install-Module PwshSpectreConsole -Scope CurrentUser
'@
        return
    }

    Import-Module PwshSpectreConsole -ErrorAction Stop

    # ── Load Config ──────────────────────────────────────────────────────────────

    if (-not (Test-Path $ConfigPath)) {
        Write-Error "Repository config not found at: $ConfigPath"
        return
    }

    $config = Get-Content $ConfigPath -Raw | ConvertFrom-Json

    if (-not $config.repositories -or $config.repositories.Count -eq 0) {
        Write-SpectreHost '[red]No repositories are defined in the config file.[/]'
        return
    }

    # Resolve clone root — expand any $env:VAR tokens in the JSON value
    $resolvedClonePath = if ($ClonePath) {
        $ClonePath
    } elseif ($config.defaultClonePath) {
        [System.Environment]::ExpandEnvironmentVariables($config.defaultClonePath)
    } else {
        $PWD.Path
    }

    # ── Header ───────────────────────────────────────────────────────────────────

    Write-SpectreHost ''
    Write-SpectreRule -Title ' Developer Bootstrap — Repository Setup ' -Color 'Green'
    Write-SpectreHost ''
    Write-SpectreHost "[dim]Config : $ConfigPath[/]"
    Write-SpectreHost "[dim]Target : $resolvedClonePath[/]"
    Write-SpectreHost ''

    # ── Build selection items ────────────────────────────────────────────────────

    # Sort alphabetically; combine name + description into a single display label
    $repoItems = $config.repositories |
        Sort-Object -Property name |
        ForEach-Object {
            [PSCustomObject]@{
                Label       = '{0,-40} {1}' -f $_.name, "— $($_.description)"
                Name        = $_.name
                Url         = $_.url
                Description = $_.description
            }
        }

    # ── Prompt or auto-select ────────────────────────────────────────────────────

    $selected = if ($SelectAll) {
        Write-SpectreHost '[yellow]-SelectAll specified — all repositories will be cloned.[/]'
        Write-SpectreHost ''
        $repoItems
    } else {
        Invoke-SpectreMultiSelectionPrompt `
            -Title '[bold green]Select the repositories to clone[/]  [dim](Space to toggle, Enter to confirm)[/]' `
            -Choices $repoItems `
            -ChoiceLabelProperty 'Label' `
            -PageSize 20
    }

    # Normalise to array so Count works on single selections
    $selected = @($selected)

    if ($selected.Count -eq 0) {
        Write-SpectreHost ''
        Write-SpectreHost '[yellow]No repositories selected — nothing to do.[/]'
        return
    }

    # ── Ensure target directory exists ───────────────────────────────────────────

    if (-not (Test-Path $resolvedClonePath)) {
        Write-SpectreHost "[yellow]Creating directory:[/] $resolvedClonePath"
        New-Item -ItemType Directory -Path $resolvedClonePath -Force | Out-Null
    }

    Write-SpectreHost ''
    Write-SpectreHost "[bold]Cloning [cyan]$($selected.Count)[/] repositor$(if ($selected.Count -eq 1) { 'y' } else { 'ies' }) into [white]$resolvedClonePath[/] ...[/]"
    Write-SpectreHost ''

    # ── Clone ────────────────────────────────────────────────────────────────────

    $results = foreach ($repo in $selected) {
        $destFolder = Join-Path $resolvedClonePath $repo.Name

        if (Test-Path $destFolder) {
            Write-SpectreHost "  [yellow]⚠[/]  [white]$($repo.Name)[/]  [dim](skipped — directory already exists)[/]"
            [PSCustomObject]@{
                Repository = $repo.Name
                Status     = 'Skipped'
                Detail     = $destFolder
            }
            continue
        }

        Write-SpectreHost "  [dim]   Cloning $($repo.Name) ...[/]"

        # Capture both stdout and stderr; git writes progress to stderr
        $gitOutput = git clone $repo.Url $destFolder 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-SpectreHost "  [green]✓[/]  [white]$($repo.Name)[/]"
            [PSCustomObject]@{
                Repository = $repo.Name
                Status     = 'Cloned'
                Detail     = $destFolder
            }
        } else {
            $errLine = ($gitOutput |
                Where-Object { $_ -match '^(fatal|error):' } |
                Select-Object -First 1) -replace '^(fatal|error):\s*', ''

            if (-not $errLine) { $errLine = $gitOutput | Select-Object -Last 1 }

            Write-SpectreHost "  [red]✗[/]  [white]$($repo.Name)[/]  [red]$errLine[/]"
            [PSCustomObject]@{
                Repository = $repo.Name
                Status     = 'Failed'
                Detail     = $errLine
            }
        }
    }

    # ── Results table ────────────────────────────────────────────────────────────

    Write-SpectreHost ''
    Write-SpectreRule -Title ' Results ' -Color 'Green'
    Write-SpectreHost ''

    $tableData = $results | ForEach-Object {
        $statusMarkup = switch ($_.Status) {
            'Cloned'  { '[bold green]✓  Cloned[/]' }
            'Skipped' { '[bold yellow]⚠  Skipped[/]' }
            'Failed'  { '[bold red]✗  Failed[/]' }
        }
        [PSCustomObject]@{
            Repository = $_.Repository
            Status     = $statusMarkup
            Detail     = $_.Detail
        }
    }

    $tableData | Format-SpectreTable -Border Rounded -AllowMarkup

    # Final summary counts
    $cloned  = @($results | Where-Object Status -eq 'Cloned').Count
    $skipped = @($results | Where-Object Status -eq 'Skipped').Count
    $failed  = @($results | Where-Object Status -eq 'Failed').Count

    Write-SpectreHost ''
    Write-SpectreHost "[bold]Done.[/]  [green]$cloned cloned[/]  |  [yellow]$skipped skipped[/]  |  [red]$failed failed[/]"
    Write-SpectreHost ''
}
