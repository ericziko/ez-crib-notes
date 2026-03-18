---
uid: 74eaa92c-8810-470f-88c9-84b2e43e6167
title: 🤖💎 PowerShell function to find Git directories with `fd`
created: 2026-03-19T13:06:31
modified: 2026-03-19T13:27:47
tags:
  - para/resources/PowerShell
  - para/resources/git
  - para/resources/fd
---

#para/resources/PowerShell
#para/resources/git
#para/resources/fd
# 🤖💎 PowerShell function to find Git directories with `fd`

```powershell
function Find-GitRepo {
    [CmdletBinding()]
    param(
        [Parameter(
            Position = 0,
            ValueFromPipeline,
            ValueFromPipelineByPropertyName
        )]
        [Alias('FullName', 'PSPath')]
        [string[]]$Path = ".",

        [switch]$ReturnGitDir,
        [int]$MaxDepth
    )

    process {
        foreach ($currentPath in $Path) {
            $fdArgs = @(
                '-H'
                '-t'; 'd'
                '^\.git$'
                $currentPath
            )

            if ($MaxDepth -gt 0) {
                $fdArgs += @('--max-depth', $MaxDepth)
            }

            $results = & fd @fdArgs 2>$null

            foreach ($gitDir in $results) {
                $resolvedGitDir = $gitDir
                $repoRoot = Split-Path $gitDir -Parent

                if ($ReturnGitDir) {
                    [PSCustomObject]@{
                        Path    = $resolvedGitDir
                        Type    = 'GitDir'
                        RepoRoot = $repoRoot
                    }
                }
                else {
                    [PSCustomObject]@{
                        Path    = $repoRoot
                        Type    = 'RepoRoot'
                        GitDir  = $resolvedGitDir
                    }
                }
            }
        }
    }
}
```

A few examples:

Pipe strings in:

```powershell
'C:\src', 'D:\work' | Find-GitRepo
```

Pipe directories from Get-ChildItem:

```powershell
Get-ChildItem C:\dev -Directory | Find-GitRepo
```

Then keep piping:

```powershell
Get-ChildItem C:\dev -Directory |
    Find-GitRepo |
    ForEach-Object { git -C $_.Path status }
```

Or filter first:

```powershell
Find-GitRepo C:\dev |
    Where-Object Path -match 'shared|common'
```

Here's a more polished, pipeline-friendly version that:
- accepts strings or Get-ChildItem directory objects from the pipeline
- resolves full paths
- returns repo root and .git dir
- can optionally include branch / dirty state / ahead-behind info
- stays friendly for further piping

```powershell
function Find-GitRepo {
    [CmdletBinding()]
    param(
        [Parameter(
            Position = 0,
            ValueFromPipeline,
            ValueFromPipelineByPropertyName
        )]
        [Alias('FullName', 'PSPath')]
        [string[]]$Path = ".",

        [switch]$ReturnGitDir,
        [int]$MaxDepth,

        [switch]$IncludeGitInfo
    )

    begin {
        $seen = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase
        )
    }

    process {
        foreach ($currentPath in $Path) {
            if ([string]::IsNullOrWhiteSpace($currentPath)) {
                continue
            }

            try {
                $resolvedPaths = Resolve-Path -LiteralPath $currentPath -ErrorAction Stop
            }
            catch {
                Write-Warning "Path not found: $currentPath"
                continue
            }

            foreach ($resolvedPath in $resolvedPaths) {
                $searchRoot = $resolvedPath.Path

                $fdArgs = @(
                    '-H'
                    '-t'; 'd'
                    '^\.git$'
                    $searchRoot
                )

                if ($MaxDepth -gt 0) {
                    $fdArgs += @('--max-depth', $MaxDepth)
                }

                $results = & fd @fdArgs 2>$null

                foreach ($gitDir in $results) {
                    try {
                        $fullGitDir = (Resolve-Path -LiteralPath $gitDir -ErrorAction Stop).Path
                    }
                    catch {
                        continue
                    }

                    $repoRoot = Split-Path -Path $fullGitDir -Parent
                    $identityPath = if ($ReturnGitDir) { $fullGitDir } else { $repoRoot }

                    if (-not $seen.Add($identityPath)) {
                        continue
                    }

                    $obj = [ordered]@{
                        Path     = $identityPath
                        RepoRoot = $repoRoot
                        GitDir   = $fullGitDir
                    }

                    if ($IncludeGitInfo) {
                        $branch = $null
                        $isDirty = $null
                        $ahead = $null
                        $behind = $null
                        $head = $null

                        try {
                            $branch = (& git -C $repoRoot branch --show-current 2>$null)
                            if (-not $branch) {
                                $branch = 'DETACHED'
                            }
                        }
                        catch {
                            $branch = $null
                        }

                        try {
                            $statusLines = @(& git -C $repoRoot status --porcelain 2>$null)
                            $isDirty = $statusLines.Count -gt 0
                        }
                        catch {
                            $isDirty = $null
                        }

                        try {
                            $head = (& git -C $repoRoot rev-parse --short HEAD 2>$null)
                        }
                        catch {
                            $head = $null
                        }

                        try {
                            $upstream = (& git -C $repoRoot rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null)
                            if ($LASTEXITCODE -eq 0 -and $upstream) {
                                $counts = & git -C $repoRoot rev-list --left-right --count "$upstream...HEAD" 2>$null
                                if ($counts -match '^\s*(\d+)\s+(\d+)\s*$') {
                                    $behind = [int]$Matches[1]
                                    $ahead  = [int]$Matches[2]
                                }
                            }
                        }
                        catch {
                            $ahead = $null
                            $behind = $null
                        }

                        $obj.Branch  = $branch
                        $obj.IsDirty = $isDirty
                        $obj.Ahead   = $ahead
                        $obj.Behind  = $behind
                        $obj.HEAD    = $head
                    }

                    [PSCustomObject]$obj
                }
            }
        }
    }
}
```

A few good usage patterns:

```powershell
Find-GitRepo
```

```powershell
Find-GitRepo c:\src -IncludeGitInfo
```

```powershell
Find-GitRepo C:\src | ForEach-Object { git -C $_.RepoRoot fetch --all --prune }
```

If you want a nice table view in the console, this works well:

```powershell
Find-GitRepo C:\src -IncludeGitInfo | Format-Table Path, Branch, IsDirty, Ahead, Behind, HEAD -AutoSize
```

And if you want a shorter alias in your profile:

```powershell
Set-Alias fgr Find-GitRepo
```

## Following version handles git worktrees
Here's a version that handles both:

- normal repos where .git is a **directory**
- worktrees / some submodules where .git is a **file**

It stays pipeline-friendly and returns useful objects.

```powershell
function Find-GitRepo {
    [CmdletBinding()]
    param(
        [Parameter(
            Position = 0,
            ValueFromPipeline,
            ValueFromPipelineByPropertyName
        )]
        [Alias('FullName', 'PSPath')]
        [string[]]$Path = ".",

        [switch]$ReturnGitEntry,
        [int]$MaxDepth,
        [switch]$IncludeGitInfo
    )

    begin {
        $seen = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase
        )
    }

    process {
        foreach ($currentPath in $Path) {
            if ([string]::IsNullOrWhiteSpace($currentPath)) {
                continue
            }

            try {
                $resolvedPaths = Resolve-Path -LiteralPath $currentPath -ErrorAction Stop
            }
            catch {
                Write-Warning "Path not found: $currentPath"
                continue
            }

            foreach ($resolvedPath in $resolvedPaths) {
                $searchRoot = $resolvedPath.Path

                $fdArgs = @('-H')

                if ($MaxDepth -gt 0) {
                    $fdArgs += @('--max-depth', $MaxDepth)
                }

                # Find both directories and files named exactly ".git"
                $fdArgs += @(
                    '^\.(git)$'
                    $searchRoot
                )

                $results = & fd @fdArgs 2>$null

                foreach ($gitEntry in $results) {
                    try {
                        $fullGitEntry = (Resolve-Path -LiteralPath $gitEntry -ErrorAction Stop).Path
                        $item = Get-Item -LiteralPath $fullGitEntry -Force -ErrorAction Stop
                    }
                    catch {
                        continue
                    }

                    $entryType = if ($item.PSIsContainer) { 'Directory' } else { 'File' }
                    $repoRoot = Split-Path -Path $fullGitEntry -Parent
                    $identityPath = if ($ReturnGitEntry) { $fullGitEntry } else { $repoRoot }

                    if (-not $seen.Add($identityPath)) {
                        continue
                    }

                    $obj = [ordered]@{
                        Path         = $identityPath
                        RepoRoot     = $repoRoot
                        GitEntry     = $fullGitEntry
                        GitEntryType = $entryType
                    }

                    if ($IncludeGitInfo) {
                        $branch = $null
                        $isDirty = $null
                        $ahead = $null
                        $behind = $null
                        $head = $null
                        $upstream = $null

                        try {
                            $branch = (& git -C $repoRoot branch --show-current 2>$null)
                            if (-not $branch) {
                                $branch = 'DETACHED'
                            }
                        }
                        catch {
                            $branch = $null
                        }

                        try {
                            $statusLines = @(& git -C $repoRoot status --porcelain 2>$null)
                            $isDirty = $statusLines.Count -gt 0
                        }
                        catch {
                            $isDirty = $null
                        }

                        try {
                            $head = (& git -C $repoRoot rev-parse --short HEAD 2>$null)
                        }
                        catch {
                            $head = $null
                        }

                        try {
                            $upstream = (& git -C $repoRoot rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null)
                            if ($LASTEXITCODE -eq 0 -and $upstream) {
                                $counts = & git -C $repoRoot rev-list --left-right --count "$upstream...HEAD" 2>$null
                                if ($counts -match '^\s*(\d+)\s+(\d+)\s*$') {
                                    $behind = [int]$Matches[1]
                                    $ahead  = [int]$Matches[2]
                                }
                            }
                        }
                        catch {
                            $ahead = $null
                            $behind = $null
                        }

                        $obj.Branch   = $branch
                        $obj.IsDirty  = $isDirty
                        $obj.Ahead    = $ahead
                        $obj.Behind   = $behind
                        $obj.HEAD     = $head
                        $obj.Upstream = $upstream
                    }

                    [PSCustomObject]$obj
                }
            }
        }
    }
}
```

## Examples

```powershell
Get-ChildItem C:\src -Directory | Find-GitRepo
```

```powershell
Find-GitRepo C:\src | Where-Object GitEntryType -eq 'File'
```

```powershell
Find-GitRepo C:\src -IncludeGitInfo |
    Format-Table Path, GitEntryType, Branch, IsDirty, Ahead, Behind, HEAD -AutoSize
```

```powershell
Find-GitRepo C:\src -IncludeGitInfo |
    Where-Object { $_.IsDirty -or $_.Ahead -gt 0 -or $_.Behind -gt 0 }
```

A couple of notes:

- GitEntryType = Directory usually means a normal repo.
- GitEntryType = File usually means a worktree checkout or a submodule-style gitlink setup.
- This version relies on `git -C <repoRoot>` for truth, which is good because it works even when the .git entry is a file.

## If you want, I can also give you a companion function like Get-GitRepoStatus so Find-GitRepo only discovers repos and a second function enriches them, which is a very PowerShell-ish split

```powershell
function Find-GitRepo {
    [CmdletBinding()]
    param(
        [Parameter(
            Position = 0,
            ValueFromPipeline,
            ValueFromPipelineByPropertyName
        )]
        [Alias('FullName', 'PSPath')]
        [string[]]$Path = ".",

        [switch]$ReturnGitEntry,
        [int]$MaxDepth
    )

    begin {
        $seen = [System.Collections.Generic.HashSet[string]]::new(
            [System.StringComparer]::OrdinalIgnoreCase
        )
    }

    process {
        foreach ($currentPath in $Path) {
            if ([string]::IsNullOrWhiteSpace($currentPath)) {
                continue
            }

            try {
                $resolvedPaths = Resolve-Path -LiteralPath $currentPath -ErrorAction Stop
            }
            catch {
                Write-Warning "Path not found: $currentPath"
                continue
            }

            foreach ($resolvedPath in $resolvedPaths) {
                $searchRoot = $resolvedPath.Path

                $fdArgs = @('-H')

                if ($MaxDepth -gt 0) {
                    $fdArgs += @('--max-depth', $MaxDepth)
                }

                $fdArgs += @(
                    '^\.(git)$'
                    $searchRoot
                )

                $results = & fd @fdArgs 2>$null

                foreach ($gitEntry in $results) {
                    try {
                        $fullGitEntry = (Resolve-Path -LiteralPath $gitEntry -ErrorAction Stop).Path
                        $item = Get-Item -LiteralPath $fullGitEntry -Force -ErrorAction Stop
                    }
                    catch {
                        continue
                    }

                    $entryType = if ($item.PSIsContainer) { 'Directory' } else { 'File' }
                    $repoRoot = Split-Path -Path $fullGitEntry -Parent
                    $identityPath = if ($ReturnGitEntry) { $fullGitEntry } else { $repoRoot }

                    if (-not $seen.Add($identityPath)) {
                        continue
                    }

                    [PSCustomObject]@{
                        PSTypeName   = 'GitRepo.Found'
                        Path         = $identityPath
                        RepoRoot     = $repoRoot
                        GitEntry     = $fullGitEntry
                        GitEntryType = $entryType
                    }
                }
            }
        }
    }
}

function Get-GitRepoStatus {
    [CmdletBinding()]
    param(
        [Parameter(
            ValueFromPipeline,
            ValueFromPipelineByPropertyName
        )]
        [Alias('Path')]
        [string[]]$RepoRoot
    )

    process {
        foreach ($repo in $RepoRoot) {
            if ([string]::IsNullOrWhiteSpace($repo)) {
                continue
            }

            $resolvedRepo = $null
            try {
                $resolvedRepo = (Resolve-Path -LiteralPath $repo -ErrorAction Stop).Path
            }
            catch {
                Write-Warning "Repo path not found: $repo"
                continue
            }

            $branch = $null
            $isDirty = $null
            $ahead = $null
            $behind = $null
            $head = $null
            $upstream = $null

            try {
                $branch = (& git -C $resolvedRepo branch --show-current 2>$null).Trim()
                if (-not $branch) {
                    $branch = 'DETACHED'
                }
            }
            catch {
                $branch = $null
            }

            try {
                $statusLines = @(& git -C $resolvedRepo status --porcelain 2>$null)
                $isDirty = $statusLines.Count -gt 0
            }
            catch {
                $isDirty = $null
            }

            try {
                $head = (& git -C $resolvedRepo rev-parse --short HEAD 2>$null).Trim()
            }
            catch {
                $head = $null
            }

            try {
                $upstream = (& git -C $resolvedRepo rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>$null).Trim()
                if ($LASTEXITCODE -eq 0 -and $upstream) {
                    $counts = & git -C $resolvedRepo rev-list --left-right --count "$upstream...HEAD" 2>$null
                    if ($counts -match '^\s*(\d+)\s+(\d+)\s*$') {
                        $behind = [int]$Matches[1]
                        $ahead  = [int]$Matches[2]
                    }
                }
                else {
                    $upstream = $null
                }
            }
            catch {
                $upstream = $null
                $ahead = $null
                $behind = $null
            }

            [PSCustomObject]@{
                PSTypeName = 'GitRepo.Status'
                RepoRoot   = $resolvedRepo
                Branch     = $branch
                IsDirty    = $isDirty
                Ahead      = $ahead
                Behind     = $behind
                HEAD       = $head
                Upstream   = $upstream
            }
        }
    }
}
```

## Usage

```powershell
Find-GitRepo C:\src

Find-GitRepo C:\src | Get-GitRepoStatus

Get-ChildItem C:\src -Directory | Find-GitRepo | Get-GitRepoStatus


Find-GitRepo C:\src |
    Get-GitRepoStatus |
    Where-Object IsDirty
    
Find-GitRepo C:\src |
    Get-GitRepoStatus |
    Sort-Object Branch, RepoRoot |
    Format-Table RepoRoot, Branch, IsDirty, Ahead, Behind, HEAD -AutoSize
```

## And here's a nice third helper if you want to preserve the discovery info and add status onto the same object instead of replacing it

```powershell
function Add-GitRepoStatus {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)]
        [object]$InputObject
    )

    process {
        if (-not $InputObject) {
            return
        }

        $repoRoot = $InputObject.RepoRoot
        if (-not $repoRoot) {
            $repoRoot = $InputObject.Path
        }

        if (-not $repoRoot) {
            Write-Warning "Input object does not contain RepoRoot or Path"
            return
        }

        $status = $repoRoot | Get-GitRepoStatus

        if (-not $status) {
            return $InputObject
        }

        $props = [ordered]@{}
        foreach ($p in $InputObject.PSObject.Properties) {
            $props[$p.Name] = $p.Value
        }

        foreach ($p in $status.PSObject.Properties) {
            if ($p.Name -ne 'RepoRoot') {
                $props[$p.Name] = $p.Value
            }
        }

        [PSCustomObject]$props
    }
}
```

That gives you this style:

```powershell
Find-GitRepo C:\src | Add-GitRepoStatus
```

Which returns one object containing both discovery fields and status fields.

My recommendation:
- use Find-GitRepo when you just want discovery
- use Find-GitRepo | Get-GitRepoStatus when status-only output is fine
- use Find-GitRepo | Add-GitRepoStatus when you want the richest pipeline object

For convenience in your profile:

```powershell
Set-Alias fgr Find-GitRepo
Set-Alias ggs Get-GitRepoStatus
```
