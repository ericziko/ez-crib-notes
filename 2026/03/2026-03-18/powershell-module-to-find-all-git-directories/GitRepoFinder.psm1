<#
.SYNOPSIS
    GitRepoFinder - PowerShell module to locate all Git repositories under a given directory.

.DESCRIPTION
    Uses `fd` (fast-find) to efficiently discover `.git` directories and reports
    the relative path, current branch, and remote URL for each repository found.
#>

function Find-GitRepositories {
    <#
    .SYNOPSIS
        Recursively finds all Git repositories under a root directory.

    .DESCRIPTION
        Uses `fd` to locate .git directories efficiently, then interrogates
        each one for its current branch and remote URL.

    .PARAMETER RootPath
        The directory to start searching from. Defaults to the current directory.

    .PARAMETER MaxDepth
        Maximum directory depth to search. Default is 10. Use 0 for unlimited.

    .PARAMETER NoRemoteOnly
        If set, only report repositories that have NO remote configured.

    .PARAMETER OutputFormat
        Output format: 'Table' (default), 'List', 'Json', 'Csv'

    .PARAMETER ExcludePaths
        Array of directory name patterns to exclude (e.g. 'node_modules', 'vendor').

    .EXAMPLE
        Find-GitRepositories

    .EXAMPLE
        Find-GitRepositories -RootPath ~ -MaxDepth 5

    .EXAMPLE
        Find-GitRepositories -RootPath ~ -NoRemoteOnly

    .EXAMPLE
        Find-GitRepositories -RootPath ~ -OutputFormat Json

    .EXAMPLE
        Find-GitRepositories -RootPath ~ -ExcludePaths 'node_modules','vendor'
    #>
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [string]$RootPath = (Get-Location).Path,

        [Parameter()]
        [int]$MaxDepth = 10,

        [Parameter()]
        [switch]$NoRemoteOnly,

        [Parameter()]
        [ValidateSet('Table', 'List', 'Json', 'Csv')]
        [string]$OutputFormat = 'Table',

        [Parameter()]
        [string[]]$ExcludePaths = @()
    )

    # Validate fd is available
    if (-not (Get-Command fd -ErrorAction SilentlyContinue)) {
        Write-Error "fd is not installed or not on PATH. Install via: brew install fd"
        return
    }

    $RootPath = Resolve-Path $RootPath | Select-Object -ExpandProperty Path

    Write-Verbose "Searching for Git repositories under: $RootPath"

    # Build fd arguments
    # fd searches for directories named .git
    $fdArgs = @(
        '--type', 'd',       # only directories
        '--hidden',          # include hidden dirs
        '--no-ignore',       # don't skip .gitignore'd paths
        '--glob', '.git'     # match the name .git
    )

    if ($MaxDepth -gt 0) {
        $fdArgs += @('--max-depth', ($MaxDepth + 1))  # +1 because .git is one level below the repo root
    }

    foreach ($exclude in $ExcludePaths) {
        $fdArgs += @('--exclude', $exclude)
    }

    # Always exclude nested .git dirs (submodules will appear naturally at their own level)
    $fdArgs += $RootPath

    Write-Verbose "fd command: fd $($fdArgs -join ' ')"

    $gitDirs = & fd @fdArgs 2>$null

    if (-not $gitDirs) {
        Write-Warning "No Git repositories found under $RootPath"
        return
    }

    $results = @()

    foreach ($gitDir in $gitDirs) {
        $repoPath = Split-Path $gitDir -Parent

        # Relative path from root
        $relativePath = $repoPath.Replace($RootPath, '').TrimStart([System.IO.Path]::DirectorySeparatorChar)
        if ([string]::IsNullOrEmpty($relativePath)) {
            $relativePath = '.'
        }

        # Current branch
        $branch = git -C $repoPath rev-parse --abbrev-ref HEAD 2>$null
        if (-not $branch) {
            $branch = '(unknown)'
        }
        if ($branch -eq 'HEAD') {
            # Detached HEAD - show the short commit hash instead
            $hash = git -C $repoPath rev-parse --short HEAD 2>$null
            $branch = "(detached@$hash)"
        }

        # Remote URL (origin preferred, falls back to first remote)
        $remoteUrl = git -C $repoPath remote get-url origin 2>$null
        if (-not $remoteUrl) {
            $firstRemote = git -C $repoPath remote 2>$null | Select-Object -First 1
            if ($firstRemote) {
                $remoteUrl = git -C $repoPath remote get-url $firstRemote 2>$null
                $remoteUrl = "[$firstRemote] $remoteUrl"
            } else {
                $remoteUrl = '(no remote)'
            }
        }

        # Dirty status (uncommitted changes)
        $dirtyStatus = git -C $repoPath status --porcelain 2>$null
        $isDirty = [bool]$dirtyStatus

        # Stash count
        $stashCount = (git -C $repoPath stash list 2>$null | Measure-Object -Line).Lines

        $entry = [PSCustomObject]@{
            RelativePath = $relativePath
            Branch       = $branch
            RemoteUrl    = $remoteUrl
            HasRemote    = ($remoteUrl -ne '(no remote)')
            IsDirty      = $isDirty
            StashCount   = $stashCount
            FullPath     = $repoPath
        }

        if ($NoRemoteOnly -and $entry.HasRemote) {
            continue
        }

        $results += $entry
    }

    Write-Verbose "Found $($results.Count) repositories"

    switch ($OutputFormat) {
        'Json' {
            $results | ConvertTo-Json -Depth 3
        }
        'Csv' {
            $results | Select-Object RelativePath, Branch, RemoteUrl, HasRemote, IsDirty, StashCount |
                ConvertTo-Csv -NoTypeInformation
        }
        'List' {
            foreach ($r in $results) {
                Write-Host ""
                Write-Host "  Path:    $($r.RelativePath)" -ForegroundColor Cyan
                Write-Host "  Branch:  $($r.Branch)" -ForegroundColor Yellow
                Write-Host "  Remote:  $($r.RemoteUrl)" -ForegroundColor $(if ($r.HasRemote) { 'Green' } else { 'Red' })
                Write-Host "  Dirty:   $($r.IsDirty)  |  Stashes: $($r.StashCount)"
                Write-Host "  ─────────────────────────────────────────" -ForegroundColor DarkGray
            }
        }
        default {
            # Table - show core columns, with colour hints via Format-Table
            $results | Select-Object RelativePath, Branch, RemoteUrl, IsDirty, StashCount |
                Format-Table -AutoSize -Wrap
        }
    }

    # Always return the objects to the pipeline so callers can process them
    if ($OutputFormat -notin @('Table', 'List')) {
        return
    }
    # Surface the raw objects for pipeline use when in Table/List mode
    return $results
}

# Convenience alias
Set-Alias -Name fgr -Value Find-GitRepositories

Export-ModuleMember -Function Find-GitRepositories -Alias fgr
