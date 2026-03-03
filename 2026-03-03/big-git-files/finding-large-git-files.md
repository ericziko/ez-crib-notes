---
uid: 7f3a2c1e-8b4d-4f9a-b2e5-1d6c0a9f3e72
title: Finding Large Files in a Git Repository with PowerShell
created: 2026-03-03T00:00:00Z
modified: 2026-03-03T00:00:00Z
tags:
  - git
  - powershell
  - devops
  - troubleshooting
---

# 🤖❓ Finding Large Files in a Git Repository (PowerShell)

> **Problem**: Someone committed large files to a GitHub branch without using Git LFS. The repository takes too long to clone, and you need to identify what was committed, where it came from, and how to fix it.

---

## 🔍 Overview: Why Repos Get Fat

Git stores **every version of every file ever committed** in its object database (`.git/objects`). Unlike a filesystem, simply deleting a large file and committing again does **not** reduce repository size — the blob is still in history. You need to:

1. **Find** the large objects (blobs) in git history
2. **Identify** which commits and branches introduced them
3. **Remove** them from history (rewrite, BFG, or git-filter-repo)
4. **Force-push** the cleaned history
5. **Verify** the fix with a fresh clone

---

## 📋 Prerequisites

```powershell
# Verify git is available
git --version

# Optional but useful: install git-filter-repo (Python-based, faster than filter-branch)
pip install git-filter-repo

# Clone the repo fresh (so you have full history, including all branches)
git clone --mirror https://github.com/your-org/your-repo.git repo-mirror
cd repo-mirror
```

> **Tip**: Use `--mirror` to get all refs (branches, tags, etc.) and all objects. This is the safest environment for investigation.

---

## 🕵️ Step 1 — Find All Large Objects in the Object Database

This uses git's plumbing commands to list every blob by size.

```powershell
# Get all objects sorted by size (descending), show top 20 largest
git cat-file --batch-all-objects --batch-check='%(objecttype) %(objectname) %(objectsize)' |
    Where-Object { $_ -match '^blob' } |
    ForEach-Object {
        $parts = $_ -split ' '
        [PSCustomObject]@{
            Type   = $parts[0]
            Hash   = $parts[1]
            SizeKB = [math]::Round([int]$parts[2] / 1KB, 2)
            SizeMB = [math]::Round([int]$parts[2] / 1MB, 2)
        }
    } |
    Sort-Object -Property SizeMB -Descending |
    Select-Object -First 20 |
    Format-Table -AutoSize
```

**Sample output:**

```
Type  Hash                                     SizeKB    SizeMB
----  ----                                     ------    ------
blob  a3f9c2d1b4e8f7a6c5d2e1f0b9a8c7d6e5f4a3b2  512000    500
blob  b2e8d1c3a4f7e6d5c4b3a2f1e0d9c8b7a6f5e4d3  102400    100
```

---

## 🗂️ Step 2 — Map Blob Hashes Back to File Paths

Once you have suspect blob hashes, find which file paths they correspond to:

```powershell
# Replace <hash> with the blob hash from Step 1
$blobHash = 'a3f9c2d1b4e8f7a6c5d2e1f0b9a8c7d6e5f4a3b2'

git log --all --pretty=format: --name-only --diff-filter=A |
    Where-Object { $_ -ne '' } |
    ForEach-Object {
        $filePath = $_
        $fileHash = git rev-parse "HEAD:$filePath" 2>$null
        if ($fileHash -eq $blobHash) {
            $filePath
        }
    }
```

### 💡 Faster Alternative — rev-list + ls-tree

```powershell
# For each commit, check if the blob appears in the tree
git rev-list --all --objects |
    Where-Object { $_ -match $blobHash } |
    Select-Object -First 5
```

Or use the **single best command** for finding paths of large blobs:

```powershell
function Find-LargeGitBlobs {
    param(
        [int]$TopN        = 20,
        [int]$ThresholdMB = 1
    )

    Write-Host "Scanning git object database for large blobs..." -ForegroundColor Cyan

    # Get all blob sizes
    $blobs = git cat-file --batch-all-objects --batch-check='%(objecttype) %(objectname) %(objectsize)' |
        Where-Object { $_ -match '^blob' } |
        ForEach-Object {
            $parts = $_ -split ' '
            [PSCustomObject]@{
                Hash   = $parts[1]
                SizeMB = [math]::Round([int]$parts[2] / 1MB, 2)
            }
        } |
        Where-Object { $_.SizeMB -ge $ThresholdMB } |
        Sort-Object SizeMB -Descending |
        Select-Object -First $TopN

    Write-Host "Found $($blobs.Count) blob(s) >= ${ThresholdMB}MB. Resolving file paths..." -ForegroundColor Yellow

    # Build a hash->path lookup from all objects in history
    $pathLookup = @{}
    git rev-list --all --objects | ForEach-Object {
        if ($_ -match '^([0-9a-f]{40})\s+(.+)$') {
            $hash = $Matches[1]
            $path = $Matches[2]
            if (-not $pathLookup.ContainsKey($hash)) {
                $pathLookup[$hash] = $path
            }
        }
    }

    # Resolve paths for our large blobs
    $blobs | ForEach-Object {
        $blob = $_
        $path = $pathLookup[$blob.Hash]
        [PSCustomObject]@{
            SizeMB   = $blob.SizeMB
            Hash     = $blob.Hash
            FilePath = if ($path) { $path } else { '(path not resolved)' }
        }
    } | Format-Table -AutoSize
}

# Run it
Find-LargeGitBlobs -TopN 20 -ThresholdMB 1
```

---

## 🔎 Step 3 — Find Which Commits Introduced the Large Files

```powershell
# Find every commit that touched a specific file path
$suspectFile = 'data/large-export.csv'

git log --all --oneline --follow -- $suspectFile

# Get full commit details
git log --all --stat --follow -- $suspectFile |
    Select-Object -First 50
```

To find **which branch** a commit is on:

```powershell
$commitHash = 'abc1234'

# All branches containing this commit
git branch --all --contains $commitHash

# All tags containing this commit
git tag --contains $commitHash
```

---

## 📊 Step 4 — Measure Repository Size Before and After

```powershell
function Get-GitRepoSize {
    param([string]$RepoPath = '.')

    $gitDir = Join-Path $RepoPath '.git'
    if (-not (Test-Path $gitDir)) {
        # Might be a bare/mirror repo
        $gitDir = $RepoPath
    }

    $size = (Get-ChildItem $gitDir -Recurse -File | Measure-Object -Property Length -Sum).Sum
    [PSCustomObject]@{
        RepoPath = $RepoPath
        SizeMB   = [math]::Round($size / 1MB, 2)
        SizeGB   = [math]::Round($size / 1GB, 3)
    }
}

Get-GitRepoSize | Format-Table -AutoSize
```

Also useful to see the pack file breakdown:

```powershell
# Show pack file sizes
git count-objects -v -H

# Run gc to repack (helpful for accurate size before cleanup)
git gc --aggressive --prune=now
```

---

## 🧹 Step 5 — Remove Large Files from History

### Option A: `git-filter-repo` (Recommended)

```powershell
# Install
pip install git-filter-repo

# Remove a specific file from all history
git filter-repo --path 'data/large-export.csv' --invert-paths

# Remove all files matching a pattern (e.g., all .zip files)
git filter-repo --path-glob '*.zip' --invert-paths

# Remove files larger than 10MB from history
git filter-repo --strip-blobs-bigger-than 10M
```

### Option B: BFG Repo Cleaner (Java, very fast)

```powershell
# Download BFG jar from https://rtyley.github.io/bfg-repo-cleaner/
# Then run:
java -jar bfg.jar --delete-files '*.zip' repo-mirror
java -jar bfg.jar --strip-blobs-bigger-than 10M repo-mirror

# After BFG, clean up
cd repo-mirror
git reflog expire --expire=now --all
git gc --prune=now --aggressive
```

### Option C: `git filter-branch` (Old, slow — avoid if possible)

```powershell
# Remove a file from all commits (very slow on large repos)
git filter-branch --force --index-filter `
    'git rm --cached --ignore-unmatch data/large-export.csv' `
    --prune-empty --tag-name-filter cat -- --all
```

---

## 🚀 Step 6 — Push the Cleaned History

> ⚠️ **Warning**: This rewrites history. Coordinate with your team first — everyone will need to re-clone or rebase.

```powershell
# For a mirror repo, push all refs
git push --mirror origin

# For a normal repo
git push --force-with-lease --all
git push --force-with-lease --tags
```

---

## ✅ Step 7 — Verify the Fix

```powershell
# Time a fresh clone to compare
$start = Get-Date
git clone https://github.com/your-org/your-repo.git repo-verify
$elapsed = (Get-Date) - $start
Write-Host "Clone took: $($elapsed.TotalSeconds)s" -ForegroundColor Green

# Confirm large blobs are gone
Set-Location repo-verify
Find-LargeGitBlobs -ThresholdMB 1
```

---

## 🗺️ Quick Reference Cheat Sheet

| Goal | Command |
|---|---|
| List all large blobs | `git cat-file --batch-all-objects --batch-check` |
| Map blob hash to path | `git rev-list --all --objects \| grep <hash>` |
| Find commits for a file | `git log --all --follow -- <path>` |
| Find branches with commit | `git branch --all --contains <hash>` |
| Measure repo size | `git count-objects -v -H` |
| Strip large blobs | `git filter-repo --strip-blobs-bigger-than 10M` |
| Clean after rewrite | `git gc --prune=now --aggressive` |

---

## 🔗 Related Tools

- [git-filter-repo](https://github.com/newren/git-filter-repo) — Fast, safe history rewriting
- [BFG Repo Cleaner](https://rtyley.github.io/bfg-repo-cleaner/) — Simple large file removal
- [git sizer](https://github.com/github/git-sizer) — GitHub's tool for repo size analysis (`brew install git-sizer` or `winget install GitHub.GitSizer`)

```powershell
# git-sizer gives a detailed breakdown instantly
git-sizer --verbose
```

---

## 💡 Pro Tips

- **Always work on a mirror clone** (`--mirror`) when doing history rewrites — never rewrite production directly
- **Coordinate with your team** before force-pushing — history rewrites invalidate all existing local clones
- **Add a `.gitattributes` rule** after cleanup to prevent recurrence:
  ```
  *.zip filter=lfs diff=lfs merge=lfs -text
  *.csv filter=lfs diff=lfs merge=lfs -text
  *.exe filter=lfs diff=lfs merge=lfs -text
  ```
- **GitHub has a 100MB file limit** — files larger than 50MB get a warning, >100MB are rejected by the push hook (but can still exist in history from older pushes)
