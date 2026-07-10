---
uid: 3435b09f-b6ff-4781-a98f-a0f7f252151f
---

# Git-Branch-Report

I have a local Git repository that is hooked up to two different remote repositories, and branches have been removed or pruned on some of the remotes. How do I get a report that shows me what branches exist where and what branches they might be missing from which remotes 

# 🤖💡Git Branch Report Across Multiple Remotes

## The Core Problem

Git doesn't have a built-in "branch inventory" command, so you need to combine several commands.

---

## Quick Overview Commands

```bash
# See all branches - local and all remotes together
git branch -a

# See remotes you have configured
git remote -v
```

---

## The Useful Report: Branch Tracking Status

```bash
# Show all local branches with their upstream tracking info
# This immediately reveals branches with no upstream or a dead upstream
git branch -vv
```

**Reading the output:**

```
* main         a1b2c3d [origin/main] latest commit message
  feature-x    e4f5g6h [github/feature-x: gone] last commit here
  orphan-work  i7j8k9l no upstream configured at all
```

| Indicator | Meaning |
|-----------|---------|
| `[origin/main]` | Tracking cleanly |
| `[origin/feature: gone]` | Remote branch was deleted |
| *(nothing)* | No upstream set at all |

---

## Fetch Everything First

Before any report, make sure your local knowledge is current:

```bash
# Fetch all remotes, and mark deleted remote branches as gone
git fetch --all --prune
```

> Without this step you are reporting on **stale cached data**, not reality

---

## Cross-Remote Comparison

With two remotes named `origin` and `github` for example:

```bash
# List branches on remote 1 only
git branch -r | grep "origin/" | sed 's|origin/||' | sort > /tmp/origin_branches.txt

# List branches on remote 2 only
git branch -r | grep "github/" | sed 's|github/||' | sort > /tmp/github_branches.txt

# Show what origin has that github does not
comm -23 /tmp/origin_branches.txt /tmp/github_branches.txt

# Show what github has that origin does not
comm -13 /tmp/origin_branches.txt /tmp/github_branches.txt

# Show branches that exist on both
comm -12 /tmp/origin_branches.txt /tmp/github_branches.txt
```

---

## Full Shell Script Report

Save this as `git-branch-report.sh`:

```bash
#!/usr/bin/env bash
# Run from inside your git repository

set -euo pipefail

echo "========================================"
echo " GIT BRANCH REPORT"
echo " Repository: $(git rev-parse --show-toplevel)"
echo " Generated:  $(date)"
echo "========================================"

# Get configured remotes
REMOTES=$(git remote)
REMOTE_COUNT=$(echo "$REMOTES" | wc -l | tr -d ' ')

echo ""
echo "CONFIGURED REMOTES ($REMOTE_COUNT found)"
echo "----------------------------------------"
git remote -v | grep "(fetch)"

# Fetch and prune all remotes so data is current
echo ""
echo "FETCHING ALL REMOTES (with prune)"
echo "----------------------------------------"
git fetch --all --prune 2>&1

# Local branch summary
echo ""
echo "LOCAL BRANCHES WITH TRACKING STATUS"
echo "----------------------------------------"
git branch -vv

# Per-remote branch lists
echo ""
echo "BRANCHES BY REMOTE"
echo "----------------------------------------"
for remote in $REMOTES; do
    echo ""
    echo "  [$remote]"
    git branch -r | grep "^  $remote/" | sed "s|  $remote/||" | sed 's/^/    /'
done

# Cross-remote diff (only meaningful with 2+ remotes)
if [ "$REMOTE_COUNT" -ge 2 ]; then
    echo ""
    echo "CROSS-REMOTE DIFFERENCES"
    echo "----------------------------------------"

    REMOTE_ARRAY=($REMOTES)

    for ((i=0; i<${#REMOTE_ARRAY[@]}; i++)); do
        for ((j=i+1; j<${#REMOTE_ARRAY[@]}; j++)); do
            R1="${REMOTE_ARRAY[$i]}"
            R2="${REMOTE_ARRAY[$j]}"

            # Build sorted branch lists for each remote
            BRANCHES_R1=$(git branch -r | grep "^  $R1/" \
                | sed "s|  $R1/||" | sort)
            BRANCHES_R2=$(git branch -r | grep "^  $R2/" \
                | sed "s|  $R2/||" | sort)

            echo ""
            echo "  Comparing [$R1] vs [$R2]"

            ONLY_R1=$(comm -23 \
                <(echo "$BRANCHES_R1") \
                <(echo "$BRANCHES_R2"))
            ONLY_R2=$(comm -13 \
                <(echo "$BRANCHES_R1") \
                <(echo "$BRANCHES_R2"))
            IN_BOTH=$(comm -12 \
                <(echo "$BRANCHES_R1") \
                <(echo "$BRANCHES_R2"))

            echo ""
            echo "  Only on $R1 (missing from $R2):"
            if [ -z "$ONLY_R1" ]; then
                echo "    (none)"
            else
                echo "$ONLY_R1" | sed 's/^/    /'
            fi

            echo ""
            echo "  Only on $R2 (missing from $R1):"
            if [ -z "$ONLY_R2" ]; then
                echo "    (none)"
            else
                echo "$ONLY_R2" | sed 's/^/    /'
            fi

            echo ""
            echo "  Present on both:"
            if [ -z "$IN_BOTH" ]; then
                echo "    (none)"
            else
                echo "$IN_BOTH" | sed 's/^/    /'
            fi
        done
    done
fi

# Highlight dead tracking branches specifically
echo ""
echo "LOCAL BRANCHES TRACKING DELETED REMOTES"
echo "----------------------------------------"
GONE=$(git branch -vv | grep ': gone]')
if [ -z "$GONE" ]; then
    echo "  (none - all tracked remotes still exist)"
else
    echo "$GONE"
    echo ""
    echo "  To delete these stale local branches:"
    echo "$GONE" | awk '{print $1}' | sed 's/^/    git branch -d /'
fi

echo ""
echo "========================================"
echo " END OF REPORT"
echo "========================================"
```

```bash
chmod +x git-branch-report.sh
./git-branch-report.sh
```

---

## Sample Output

```
========================================
 GIT BRANCH REPORT
 Repository: /home/user/myproject
 Generated:  Mon Jun  8 23:49:35 2026
========================================

CONFIGURED REMOTES (2 found)
----------------------------------------
origin   git@github.com:user/myproject.git (fetch)
backup   git@gitlab.com:user/myproject.git (fetch)

LOCAL BRANCHES WITH TRACKING STATUS
----------------------------------------
* main         a1b2c3d [origin/main] add login feature
  feature-x    e4f5g6h [origin/feature-x: gone] wip
  hotfix-99    i7j8k9l [backup/hotfix-99] critical fix

CROSS-REMOTE DIFFERENCES

  Comparing [origin] vs [backup]

  Only on origin (missing from backup):
    develop
    release-2.0

  Only on backup (missing from origin):
    hotfix-99
    legacy-support

  Present on both:
    main

LOCAL BRANCHES TRACKING DELETED REMOTES
----------------------------------------
  feature-x    e4f5g6h [origin/feature-x: gone] wip

  To delete these stale local branches:
    git branch -d feature-x
```

---

## Key Takeaways

| Task                           | Command                         |
| ------------------------------ | ------------------------------- |
| Refresh all remote data        | `git fetch --all --prune`       |
| See all branches with tracking | `git branch -vv`                |
| Find deleted remote branches   | `git branch -vv \| grep gone`   |
| List one remote's branches     | `git branch -r \| grep origin/` |
| Full automated report          | Use the script above            |

> **The `--prune` flag on fetch is the most important habit** — without it, deleted remote branches stay visible in your local cache indefinitely and any report you generate will be misleading

# Can you write me a PowerShell version of `Full Shell Script Report`

# 🤖💡PowerShell Git Branch Report

```powershell
#!/usr/bin/env pwsh
# Git-BranchReport.ps1
# Run from inside your git repository
# Compatible with PowerShell 5.1+ and PowerShell Core (cross-platform)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

#region ── Helpers ────────────────────────────────────────────────────────────

function Write-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host $Title -ForegroundColor Cyan
    Write-Host ("─" * 60) -ForegroundColor DarkGray
}

function Write-SectionItem {
    param(
        [string]$Label,
        [string]$Value,
        [System.ConsoleColor]$LabelColor  = 'Yellow',
        [System.ConsoleColor]$ValueColor  = 'White'
    )
    Write-Host "  $Label" -ForegroundColor $LabelColor -NoNewline
    Write-Host " $Value"  -ForegroundColor $ValueColor
}

function Invoke-Git {
    <#
    .SYNOPSIS
        Runs a git command and returns stdout lines as an array.
        Throws a clean error if git exits non-zero.
    #>
    param([string[]]$Arguments)

    $result = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') failed (exit $LASTEXITCODE): $result"
    }
    # Return as array of non-empty strings
    return ($result | Where-Object { $_ -match '\S' })
}

function Assert-GitRepository {
    try {
        Invoke-Git 'rev-parse', '--git-dir' | Out-Null
    }
    catch {
        Write-Host "ERROR: Not inside a Git repository." -ForegroundColor Red
        exit 1
    }
}

#endregion

#region ── Data Collection ────────────────────────────────────────────────────

function Get-RemoteBranchNames {
    <#
    .SYNOPSIS
        Returns a sorted list of plain branch names (no remote prefix)
        for the given remote.
    .EXAMPLE
        Get-RemoteBranchNames 'origin'   # returns @('main','develop',...)
    #>
    param([string]$Remote)

    $pattern = "^  $Remote/"             # matches "  origin/main" etc.

    Invoke-Git 'branch', '-r'            |
        Where-Object { $_ -like "  $Remote/*" -and $_ -notlike '*HEAD*' } |
        ForEach-Object { $_.Trim() -replace "^$Remote/", '' } |
        Sort-Object
}

function Get-LocalBranchTrackingInfo {
    <#
    .SYNOPSIS
        Parses 'git branch -vv' and returns a list of PSCustomObjects with
        properties: Name, Hash, Upstream, Status, Message
    #>
    $lines = Invoke-Git 'branch', '-vv'

    foreach ($line in $lines) {
        # Format:
        #   * main         a1b2c3d [origin/main] some message
        #     feature-x    e4f5g6h [origin/feature-x: gone] wip
        #     orphan       i7j8k9l local only commit
        $isCurrent = $line.StartsWith('*')
        $line      = $line.TrimStart('* ').Trim()

        # Named capture groups make parsing explicit and readable
        if ($line -match '^(?<name>\S+)\s+(?<hash>[0-9a-f]+)\s+\[(?<upstream>[^\]]+)\]\s+(?<msg>.*)$') {
            $upstreamRaw = $Matches['upstream']
            $status      = if ($upstreamRaw -like '*: gone*') { 'gone' }
                           else                               { 'tracking' }
            $upstream    = $upstreamRaw -replace ': gone.*', ''

            [PSCustomObject]@{
                Name      = $Matches['name']
                Hash      = $Matches['hash']
                Upstream  = $upstream
                Status    = $status
                Message   = $Matches['msg']
                IsCurrent = $isCurrent
            }
        }
        elseif ($line -match '^(?<name>\S+)\s+(?<hash>[0-9a-f]+)\s+(?<msg>.*)$') {
            [PSCustomObject]@{
                Name      = $Matches['name']
                Hash      = $Matches['hash']
                Upstream  = $null
                Status    = 'local-only'
                Message   = $Matches['msg']
                IsCurrent = $isCurrent
            }
        }
    }
}

#endregion

#region ── Report Sections ────────────────────────────────────────────────────

function Show-Banner {
    param(
        [string]$RepoRoot,
        [string]$GeneratedAt
    )

    $banner = @"

════════════════════════════════════════════════════════════
  GIT BRANCH REPORT
  Repository : $RepoRoot
  Generated  : $GeneratedAt
════════════════════════════════════════════════════════════
"@
    Write-Host $banner -ForegroundColor Cyan
}

function Show-RemoteList {
    param([string[]]$Remotes)

    Write-Header "CONFIGURED REMOTES ($($Remotes.Count) found)"

    foreach ($remote in $Remotes) {
        $urls = Invoke-Git 'remote', 'get-url', '--all', $remote
        foreach ($url in $urls) {
            Write-SectionItem -Label "$remote" -Value $url
        }
    }
}

function Show-FetchProgress {
    param([string[]]$Remotes)

    Write-Header "FETCHING ALL REMOTES (with prune)"

    foreach ($remote in $Remotes) {
        Write-Host "  Fetching $remote..." -ForegroundColor DarkYellow -NoNewline
        try {
            # Capture stderr too since fetch writes progress there
            $output = & git fetch --prune $remote 2>&1
            Write-Host " done" -ForegroundColor Green
            if ($output) {
                $output | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
            }
        }
        catch {
            Write-Host " FAILED" -ForegroundColor Red
            Write-Host "    $_"  -ForegroundColor Red
        }
    }
}

function Show-LocalBranchStatus {
    param([PSCustomObject[]]$Branches)

    Write-Header "LOCAL BRANCHES WITH TRACKING STATUS"

    # Determine column widths dynamically so output stays aligned
    $maxName     = ($Branches | Measure-Object -Property Name    -Maximum).Maximum + 2
    $maxHash     = 8
    $maxUpstream = ($Branches |
                        Where-Object { $_.Upstream } |
                        ForEach-Object { $_.Upstream.Length } |
                        Measure-Object -Maximum).Maximum + 2

    $maxName     = [Math]::Max($maxName,     10)
    $maxUpstream = [Math]::Max($maxUpstream, 15)

    # Column headers
    $header = "  {0} {1} {2,-$maxUpstream} {3}" -f
              ("Branch").PadRight($maxName),
              "Commit  ",
              "Upstream",
              "Status / Message"
    Write-Host $header -ForegroundColor DarkGray
    Write-Host ("  " + "─" * ($maxName + $maxHash + $maxUpstream + 30)) -ForegroundColor DarkGray

    foreach ($branch in $Branches) {
        $prefix   = if ($branch.IsCurrent) { '* ' } else { '  ' }
        $nameStr  = $branch.Name.PadRight($maxName)
        $hashStr  = $branch.Hash.Substring(0, [Math]::Min(7, $branch.Hash.Length))
        $upStr    = if ($branch.Upstream) { $branch.Upstream } else { '(no upstream)' }
        $upStr    = $upStr.PadRight($maxUpstream)

        # Pick colours based on tracking status
        $nameColor = switch ($branch.Status) {
            'gone'       { 'Red' }
            'local-only' { 'DarkYellow' }
            default      { if ($branch.IsCurrent) { 'Green' } else { 'White' } }
        }
        $statusTag = switch ($branch.Status) {
            'gone'       { '[GONE]      ' }
            'local-only' { '[local-only]' }
            default      { '[tracking]  ' }
        }
        $tagColor  = switch ($branch.Status) {
            'gone'       { 'Red' }
            'local-only' { 'DarkYellow' }
            default      { 'DarkGreen' }
        }

        Write-Host $prefix                         -NoNewline
        Write-Host $nameStr -ForegroundColor $nameColor -NoNewline
        Write-Host " $hashStr " -ForegroundColor DarkGray   -NoNewline
        Write-Host $upStr   -ForegroundColor DarkCyan  -NoNewline
        Write-Host " $statusTag " -ForegroundColor $tagColor  -NoNewline
        Write-Host $branch.Message -ForegroundColor DarkGray
    }
}

function Show-BranchesByRemote {
    param([string[]]$Remotes)

    Write-Header "BRANCHES BY REMOTE"

    foreach ($remote in $Remotes) {
        Write-Host ""
        Write-Host "  [$remote]" -ForegroundColor Magenta

        $branches = Get-RemoteBranchNames $remote
        if (-not $branches) {
            Write-Host "    (no branches found)" -ForegroundColor DarkGray
        }
        else {
            $branches | ForEach-Object {
                Write-Host "    $_" -ForegroundColor White
            }
        }
    }
}

function Show-CrossRemoteDiff {
    param([string[]]$Remotes)

    if ($Remotes.Count -lt 2) { return }

    Write-Header "CROSS-REMOTE DIFFERENCES"

    # Compare every unique pair of remotes
    for ($i = 0; $i -lt $Remotes.Count; $i++) {
        for ($j = $i + 1; $j -lt $Remotes.Count; $j++) {
            $r1 = $Remotes[$i]
            $r2 = $Remotes[$j]

            $b1 = [System.Collections.Generic.HashSet[string]]::new(
                      (Get-RemoteBranchNames $r1),
                      [System.StringComparer]::OrdinalIgnoreCase)

            $b2 = [System.Collections.Generic.HashSet[string]]::new(
                      (Get-RemoteBranchNames $r2),
                      [System.StringComparer]::OrdinalIgnoreCase)

            # Compute set differences using clones so originals stay intact
            $onlyR1  = [System.Collections.Generic.HashSet[string]]::new($b1)
            $onlyR1.ExceptWith($b2)

            $onlyR2  = [System.Collections.Generic.HashSet[string]]::new($b2)
            $onlyR2.ExceptWith($b1)

            $inBoth  = [System.Collections.Generic.HashSet[string]]::new($b1)
            $inBoth.IntersectWith($b2)

            Write-Host ""
            Write-Host "  Comparing " -NoNewline
            Write-Host "[$r1]" -ForegroundColor Magenta -NoNewline
            Write-Host " vs "  -NoNewline
            Write-Host "[$r2]" -ForegroundColor Magenta

            # ── Only on R1 ──────────────────────────────────────────────────
            Write-Host ""
            Write-Host "  Only on $r1 " -ForegroundColor Yellow -NoNewline
            Write-Host "(missing from $r2):" -ForegroundColor DarkGray

            if ($onlyR1.Count -eq 0) {
                Write-Host "    (none)" -ForegroundColor DarkGray
            }
            else {
                $onlyR1 | Sort-Object | ForEach-Object {
                    Write-Host "    + $_" -ForegroundColor Yellow
                }
            }

            # ── Only on R2 ──────────────────────────────────────────────────
            Write-Host ""
            Write-Host "  Only on $r2 " -ForegroundColor Yellow -NoNewline
            Write-Host "(missing from $r1):" -ForegroundColor DarkGray

            if ($onlyR2.Count -eq 0) {
                Write-Host "    (none)" -ForegroundColor DarkGray
            }
            else {
                $onlyR2 | Sort-Object | ForEach-Object {
                    Write-Host "    + $_" -ForegroundColor Yellow
                }
            }

            # ── Present on both ─────────────────────────────────────────────
            Write-Host ""
            Write-Host "  Present on both:" -ForegroundColor DarkGray

            if ($inBoth.Count -eq 0) {
                Write-Host "    (none)" -ForegroundColor DarkGray
            }
            else {
                $inBoth | Sort-Object | ForEach-Object {
                    Write-Host "    ✓ $_" -ForegroundColor Green
                }
            }
        }
    }
}

function Show-StaleBranches {
    param([PSCustomObject[]]$Branches)

    Write-Header "LOCAL BRANCHES TRACKING DELETED REMOTES"

    $gone = $Branches | Where-Object { $_.Status -eq 'gone' }

    if (-not $gone) {
        Write-Host "  (none — all tracked remotes still exist)" -ForegroundColor Green
        return
    }

    foreach ($branch in $gone) {
        Write-Host "  $($branch.Name)" -ForegroundColor Red -NoNewline
        Write-Host "  →  " -ForegroundColor DarkGray -NoNewline
        Write-Host $branch.Upstream -ForegroundColor DarkRed -NoNewline
        Write-Host " no longer exists on remote" -ForegroundColor DarkGray
    }

    Write-Host ""
    Write-Host "  To delete these stale local branches:" -ForegroundColor DarkYellow
    foreach ($branch in $gone) {
        Write-Host "    git branch -d $($branch.Name)" -ForegroundColor DarkGray
    }
}

function Show-Footer {
    Write-Host ""
    Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  END OF REPORT" -ForegroundColor Cyan
    Write-Host "════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
}

#endregion

#region ── Entry Point ────────────────────────────────────────────────────────

function Invoke-GitBranchReport {

    Assert-GitRepository

    $repoRoot    = (Invoke-Git 'rev-parse', '--show-toplevel')[0]
    $generatedAt = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $remotes     = @(Invoke-Git 'remote')

    Show-Banner      -RepoRoot $repoRoot -GeneratedAt $generatedAt
    Show-RemoteList  -Remotes  $remotes
    Show-FetchProgress         -Remotes  $remotes

    # Collect local branch data AFTER fetching so 'gone' flags are current
    $localBranches = @(Get-LocalBranchTrackingInfo)

    Show-LocalBranchStatus  -Branches $localBranches
    Show-BranchesByRemote   -Remotes  $remotes
    Show-CrossRemoteDiff    -Remotes  $remotes
    Show-StaleBranches      -Branches $localBranches
    Show-Footer
}

# ── Run it ────────────────────────────────────────────────────────────────────
Invoke-GitBranchReport
```

---

## How To Run It

```powershell
# Standard PowerShell execution policy may block unsigned scripts
# This unblocks for the current session only
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

# Run it
.\Git-BranchReport.ps1
```

---

## What Each Section Does

| Section | What it shows |
|---|---|
| **Banner** | Repo path and timestamp |
| **Configured Remotes** | Names and fetch URLs |
| **Fetching** | Live fetch output per remote, errors caught individually so one bad remote does not abort the whole report |
| **Local Branch Status** | Dynamically padded table with colour-coded `[tracking]`, `[GONE]`, `[local-only]` per branch |
| **Branches By Remote** | Flat list per remote |
| **Cross-Remote Diff** | Every unique remote pair compared using `HashSet` set operations — scales to three or more remotes automatically |
| **Stale Branches** | Only the gone branches, plus the exact `git branch -d` commands ready to copy and paste |

---

## Key PowerShell Decisions Worth Noting

**`HashSet` for set operations** rather than sorting and piping through `comm` which has no native equivalent in PowerShell — `ExceptWith` and `IntersectWith` are cleaner and faster

**Named capture groups** in the regex parser `(?<name>...)` make the branch parsing readable instead of relying on positional `$Matches[1]` indices

**Fetch per-remote in a `try/catch`** so a single unreachable remote produces a red warning and continues rather than throwing and abandoning the whole reportkjkjkkjkjj
