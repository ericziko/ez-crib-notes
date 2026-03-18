---
title: GitRepoFinder PowerShell Module
created: 2026-03-18
modified: 2026-03-18
tags:
  - powershell
  - git
  - tools
  - fd
---

# 🤖💡 GitRepoFinder PowerShell Module

A PowerShell module that recursively discovers all Git repositories under a given directory. Uses [`fd`](https://github.com/sharkdp/fd) for fast directory traversal.

---

## 📦 Prerequisites

| Tool | Purpose | Install |
|------|---------|---------|
| PowerShell 7+ | Runtime | `brew install powershell` |
| `fd` | Fast directory search | `brew install fd` |
| `git` | Branch/remote queries | pre-installed or `brew install git` |

---

## 🚀 Installation

### Option A — Import directly (one-off use)

```powershell
Import-Module /path/to/GitRepoFinder.psm1
```

### Option B — Install to your module path (permanent)

```powershell
$dest = "$HOME/.local/share/powershell/Modules/GitRepoFinder"
New-Item -ItemType Directory -Force $dest
Copy-Item GitRepoFinder.psm1, GitRepoFinder.psd1 $dest

# Now available in every PowerShell session:
Import-Module GitRepoFinder
```

### Option C — Auto-load via profile

Add this line to your `$PROFILE`:

```powershell
Import-Module /path/to/GitRepoFinder.psm1
```

---

## 🔧 Usage

### Function signature

```powershell
Find-GitRepositories
    [-RootPath <String>]      # Where to start (default: current directory)
    [-MaxDepth <Int32>]       # How deep to recurse (default: 10, 0 = unlimited)
    [-NoRemoteOnly]           # Only show repos WITHOUT a remote
    [-OutputFormat <String>]  # Table | List | Json | Csv  (default: Table)
    [-ExcludePaths <String[]>] # Directory names to skip
```

Short alias: `fgr`

---

## 📋 Examples

### Scan from home directory (table output)

```powershell
Find-GitRepositories -RootPath ~
```

```
RelativePath                          Branch       RemoteUrl                                  IsDirty StashCount
------------                          ------       ---------                                  ------- ----------
gitHub/myproject                      main         https://github.com/me/myproject               False          0
gitHub/other-repo                     feature/auth https://github.com/me/other-repo               True          1
work/internal-tool                    main         git@github.com:company/internal-tool.git      False          0
experiments/throwaway                 main         (no remote)                                    True          0
```

### Limit search depth

```powershell
Find-GitRepositories -RootPath ~ -MaxDepth 3
```

### Find orphaned repos (no remote configured)

```powershell
Find-GitRepositories -RootPath ~ -NoRemoteOnly
```

### Rich list view

```powershell
fgr -RootPath ~ -OutputFormat List
```

```
  Path:    gitHub/myproject
  Branch:  main
  Remote:  https://github.com/me/myproject
  Dirty:   False  |  Stashes: 0
  ─────────────────────────────────────────
```

### Export to JSON

```powershell
fgr -RootPath ~ -OutputFormat Json | Out-File repos.json
```

### Export to CSV

```powershell
fgr -RootPath ~ -OutputFormat Csv | Out-File repos.csv
```

### Exclude common noise directories

```powershell
fgr -RootPath ~ -ExcludePaths 'node_modules','vendor','.cache'
```

### Pipeline usage — find all dirty repos

```powershell
(fgr -RootPath ~) | Where-Object IsDirty | Select-Object RelativePath, Branch
```

### Pipeline usage — repos with stashes

```powershell
(fgr -RootPath ~) | Where-Object { $_.StashCount -gt 0 } | Select-Object RelativePath, StashCount
```

---

## 📊 Output Object Properties

Each repository is returned as a `PSCustomObject` with these properties:

| Property | Type | Description |
|----------|------|-------------|
| `RelativePath` | String | Path relative to `RootPath` |
| `Branch` | String | Current branch name (or `(detached@abc1234)`) |
| `RemoteUrl` | String | `origin` URL, or `[remotename] url`, or `(no remote)` |
| `HasRemote` | Boolean | `$true` if any remote is configured |
| `IsDirty` | Boolean | `$true` if there are uncommitted changes |
| `StashCount` | Integer | Number of stashed changesets |
| `FullPath` | String | Absolute path to the repository root |

---

## 🔍 How It Works

1. **Discovery**: Runs `fd --type d --hidden --no-ignore --glob .git <root>` to find all `.git` directories quickly without traversing every subdirectory manually.
2. **Branch detection**: Runs `git -C <repo> rev-parse --abbrev-ref HEAD` — handles detached HEAD states gracefully.
3. **Remote URL**: Tries `origin` first, falls back to the first configured remote, then marks `(no remote)`.
4. **Dirty/stash**: Optional status enrichment via `git status --porcelain` and `git stash list`.

---

## 💡 Tips

- Use `-Verbose` to see what `fd` command is being constructed.
- Pipe results into `Out-GridView` for an interactive table (Windows/macOS with GUI):
  ```powershell
  (fgr -RootPath ~) | Out-GridView
  ```
- Combine with `gh` CLI to check for unpushed commits:
  ```powershell
  (fgr -RootPath ~) | ForEach-Object {
      $ahead = git -C $_.FullPath rev-list '@{u}..HEAD' 2>$null | Measure-Object -Line | Select-Object -Expand Lines
      if ($ahead -gt 0) { "$($_.RelativePath) is $ahead commits ahead" }
  }
  ```
