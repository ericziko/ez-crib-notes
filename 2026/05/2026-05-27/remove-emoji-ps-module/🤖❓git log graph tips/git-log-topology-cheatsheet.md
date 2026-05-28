---
title: Git Log & Topology Cheatsheet (CLI, Vim Fugitive, PowerShell)
created: 2026-05-27
modified: 2026-05-27
tags:
  - git
  - cli
  - powershell
  - vim
  - fugitive
  - cheatsheet
---

# 🤖💡 Git Log & Topology Cheatsheet

A practical guide for **querying, traversing, and visualizing** Git branch topology from the command line — with Vim Fugitive and PowerShell workflows layered on top.

---

## 📑 Table of Contents

1. [TL;DR — Highest-Value Commands](#-tldr--the-highest-value-commands)
2. [Mental Model: Topology in Git](#-mental-model-topology-in-git)
3. [Core `git log` Flags for Topology](#-core-git-log-flags-for-topology)
4. [Range Syntax (`..`, `...`, `^`)](#-range-syntax----)
5. [Visualizing N Branches and Their Divergence From `main`](#-visualizing-n-branches-and-their-divergence-from-main)
6. [Pretty Formats & Color](#-pretty-formats--color)
7. [Querying / Traversing Specific History](#-querying--traversing-specific-history)
8. [Useful Git Aliases](#-useful-git-aliases)
9. [Vim Fugitive](#-vim-fugitive)
10. [Other Classic Vim Plugins (Windows-Friendly)](#-other-classic-vim-plugins-windows-friendly)
11. [PowerShell Workflow](#-powershell-workflow)
12. [Speed Boosters (delta, fzf, lazygit, tig)](#-speed-boosters-delta-fzf-lazygit-tig)
13. [Workflow Recipes](#-workflow-recipes)

---

## 🚀 TL;DR — The Highest-Value Commands

```bash
# The "one command to rule them all" — adjustable, memorable
git log --graph --oneline --decorate --all

# Show three branches + main and where they diverge
git log --graph --oneline --decorate main feat-a feat-b feat-c

# Only the commits unique to those branches (relative to main)
git log --graph --oneline --decorate main..feat-a main..feat-b main..feat-c

# Pretty topology with author + relative date
git log --graph --all --decorate \
  --pretty=format:'%C(yellow)%h%Creset %C(cyan)%ar%Creset %C(green)%an%Creset %C(auto)%d%Creset %s'

# High-level "topology only" — collapses non-decorated commits
git log --graph --simplify-by-decoration --all --decorate --oneline
```

> 🧠 Memorize the four flags: `--graph --oneline --decorate --all`. They show up in nearly every useful invocation.

---

## 🧭 Mental Model: Topology in Git

Git history is a **directed acyclic graph (DAG)** of commits. Each commit has 0+ parent commits:

- **0 parents** → root commit
- **1 parent** → normal commit
- **2+ parents** → merge commit

`git log` walks this DAG starting from one or more **tips** (refs you supply, or `HEAD` by default), following parent links. Everything else is filtering:

| Concept | What it controls |
|---|---|
| **Tips** (`main`, `feat-a`, `HEAD`) | Where the walk starts |
| **Ranges** (`A..B`, `A...B`, `^A`) | Which commits to include/exclude |
| **Ordering** (`--topo-order`, `--date-order`, `--reverse`) | How walked commits are printed |
| **Simplification** (`--simplify-by-decoration`, `--first-parent`) | Hide commits irrelevant to topology |
| **Formatting** (`--graph`, `--pretty`, `--decorate`) | What you actually see |

---

## 🎛 Core `git log` Flags for Topology

### Graph & decoration

```bash
--graph                  # ASCII graph in left margin
--decorate               # show ref names (branches, tags) inline
--decorate=full          # show full refs/heads/... paths
--all                    # include all refs (branches + remotes + tags)
--branches               # include all local branches
--branches='feat-*'      # glob filter
--remotes                # include remote-tracking branches
--tags                   # include tags
--oneline                # = --pretty=oneline --abbrev-commit
```

### Filtering

```bash
--author="Eric"          # by author (regex)
--committer="bot"        # by committer
--since="2 weeks ago"    # date filter
--until="2026-05-27"
--grep="JIRA-123"        # commit message regex
-S"functionName"         # pickaxe: commits adding/removing this string
-G"regex"                # pickaxe: regex variant
--no-merges              # hide merge commits
--merges                 # show only merge commits
--first-parent           # follow only the first parent (mainline view)
```

### Ordering

```bash
--topo-order             # topology first (children before parents)
--date-order             # by commit date
--author-date-order      # by author date
--reverse                # oldest first
```

### Simplification (huge for topology)

```bash
--simplify-by-decoration # only commits that have a ref pointing at them
--ancestry-path A..B     # commits actually on a path from A to B
--full-history           # don't simplify when path-filtering files
```

### Path filtering

```bash
git log -- path/to/file
git log --follow -- path/to/file        # follow renames
git log --graph --oneline -- src/**/*.ps1
```

---

## ➗ Range Syntax (`..`, `...`, `^`)

This trips everyone up. Memorize this table:

| Syntax | Meaning | Example |
|---|---|---|
| `A` | Commits reachable from `A` | `git log main` |
| `A B` | Reachable from `A` **or** `B` | `git log main feat-a` |
| `^A B` | Reachable from `B` but **not** from `A` | `git log ^main feat-a` |
| `A..B` | Same as `^A B` — commits in `B` not in `A` | `git log main..feat-a` |
| `A...B` | **Symmetric difference** — commits in either but not both | `git log main...feat-a` |
| `A...B --left-right` | Mark `<` (in A only) vs `>` (in B only) | great for diverged branches |

> 🧠 **Rule of thumb:** Use `A..B` when you want "what's in `B` that's not in `A`" (e.g., PR review). Use `A...B` when you want to **see both sides of a fork**.

```bash
# What's on feat-a that's not on main (your PR's commits)
git log --oneline main..feat-a

# Show both sides of divergence between main and feat-a
git log --graph --oneline --left-right --boundary main...feat-a
```

The `--boundary` flag adds the **merge base** (where they diverged) marked with `o`.

---

## 🌳 Visualizing N Branches and Their Divergence From `main`

This was your headline question. Here's a layered answer.

### Option 1 — Quick visual: just list the tips

```bash
git log --graph --oneline --decorate main feat-a feat-b feat-c
```

Walks from all four tips, draws merges/branches as ASCII. Includes shared history back to root — fine, but noisy.

### Option 2 — Trim to "only what diverges from main"

```bash
git log --graph --oneline --decorate --boundary \
  main..feat-a main..feat-b main..feat-c
```

`--boundary` keeps the merge-base commits visible so you can **see the fork point**.

### Option 3 — The cleanest topology view

```bash
git log --graph --oneline --decorate --simplify-by-decoration \
  main feat-a feat-b feat-c
```

Collapses everything that isn't a ref tip or merge — gives you a tiny topology diagram.

### Option 4 — Find merge bases explicitly

```bash
# Pairwise merge base
git merge-base main feat-a

# Octopus merge base across many branches
git merge-base --octopus main feat-a feat-b feat-c

# Show commit each branch diverged at
for b in feat-a feat-b feat-c; do
  printf "%s diverged at: " "$b"
  git merge-base main "$b"
done
```

### Option 5 — "Walk from the fork point forward"

```bash
base=$(git merge-base --octopus main feat-a feat-b feat-c)
git log --graph --oneline --decorate "$base"^.. main feat-a feat-b feat-c
```

That includes one commit before the fork (`^..`) so the graph is anchored visually.

### Option 6 — `--left-right` for two-way comparisons

```bash
# Show which side each commit is on between main and feat-a
git log --graph --oneline --left-right --boundary main...feat-a
```

`<` = on `main` side, `>` = on `feat-a` side, `o` = shared (merge base).

---

## 🎨 Pretty Formats & Color

The `--pretty=format:` placeholders you'll actually use:

| Placeholder | Meaning |
|---|---|
| `%H` / `%h` | Full / abbreviated SHA |
| `%P` / `%p` | Parent SHAs (full/abbreviated) — great for inspecting merges |
| `%an` / `%ae` | Author name / email |
| `%cn` / `%ce` | Committer name / email |
| `%ad` / `%cd` | Author / committer date (respects `--date=...`) |
| `%ar` / `%cr` | Relative date ("3 days ago") |
| `%s` | Subject (first line) |
| `%b` | Body |
| `%d` | Ref names (decoration) |
| `%D` | Ref names without surrounding parens |
| `%C(color)` ... `%Creset` | Color regions |

A go-to format:

```bash
git log --graph --all --decorate \
  --pretty=format:'%C(yellow)%h%Creset %C(cyan)%ad%Creset %C(green)%<(14,trunc)%an%Creset %C(auto)%d%Creset %s' \
  --date=short
```

Highlights:
- `%<(14,trunc)%an` — left-align author to 14 chars, truncate if longer
- `%C(auto)%d` — auto-color refs (HEAD red, branches green, etc.)
- `--date=short` — `YYYY-MM-DD`

---

## 🔎 Querying / Traversing Specific History

```bash
# What changed between two tags?
git log --oneline v1.0..v2.0

# Last commit that touched a file
git log -1 -- path/to/file

# Who introduced this line?
git log -S"const FLAG = true" --source --all -- src/

# All merges into main in the last month
git log --merges --first-parent main --since="1 month ago"

# What commits are on origin but not local?
git log --oneline HEAD..@{u}

# Reverse: what have I done that's not pushed?
git log --oneline @{u}..HEAD

# Visualize all of my unpushed work across branches
git log --graph --oneline --branches --not --remotes
```

That last one is **gold** for "what work do I have lying around that isn't on the remote?"

---

## 🪄 Useful Git Aliases

Put these in `~/.gitconfig` (or run `git config --global alias.XX "..."`):

```ini
[alias]
    # The everyday graph
    lg     = log --graph --oneline --decorate --all

    # Pretty graph with author + relative date
    lgp    = log --graph --all --decorate --date=short \
             --pretty=format:'%C(yellow)%h%Creset %C(cyan)%ad%Creset %C(green)%<(14,trunc)%an%Creset %C(auto)%d%Creset %s'

    # Topology only (decoration nodes)
    lgt    = log --graph --oneline --decorate --simplify-by-decoration --all

    # Mainline view (skip side branches)
    lgm    = log --graph --oneline --decorate --first-parent

    # Last 20 commits
    last   = log -20 --oneline --decorate

    # Commits I haven't pushed
    unpushed = log --branches --not --remotes --oneline --decorate

    # What's in this branch but not main
    here   = "!f() { git log --oneline --decorate main..HEAD; }; f"

    # Show divergence vs main with both sides
    diverge = "!f() { git log --graph --oneline --left-right --boundary main...HEAD; }; f"

    # Show the merge base with main
    base   = "!f() { git merge-base main HEAD; }; f"

    # Show three branches + main
    three  = "!f() { git log --graph --oneline --decorate main \"$1\" \"$2\" \"$3\"; }; f"
```

Usage:

```bash
git lg
git lgp
git three feat-a feat-b feat-c
git diverge
```

---

## 🧬 Vim Fugitive

[Fugitive](https://github.com/tpope/vim-fugitive) is the gold-standard Git plugin for Vim. The relevant commands for topology work:

### Log & graph

| Command | What it does |
|---|---|
| `:Git log --graph --oneline --all` | Runs in a `:terminal` buffer — full graph |
| `:Git log --graph --oneline --decorate main..HEAD` | Branch vs main |
| `:0Gclog` | Quickfix-loadable log of the **current file** |
| `:Gclog -- path/to/file` | Quickfix-loadable log of a path |
| `:Gclog -10` | Last 10 commits into quickfix (use `:cnext` / `:cprev`) |
| `:Gllog` | Same but into **location list** |

> 💡 `Gclog` and `Gllog` are the killer features — they load log results into Vim's quickfix/loclist so you can hop between commits with `:cnext`, `:cprev`, and `<C-w>gf`-style navigation.

### Status & inspection

| Command | What it does |
|---|---|
| `:Git` (or `:G`) | Open the Fugitive status window |
| `:Git blame` | Blame in a side buffer; press `Enter` on a line to open that commit |
| `:Gdiff` / `:Git diff` | Diff current file vs index |
| `:Gvdiffsplit main` | 3-way diff against main |
| `:GBrowse` | Open commit/file on GitHub/etc. (needs `vim-rhubarb`) |
| `:Gedit <SHA>:%` | Open this file at a specific commit |
| `:Gread <SHA>` | Replace buffer contents with file at that commit |

### Workflow inside `:Git` status window

- `s` — stage hunk/file
- `u` — unstage
- `=` — toggle inline diff
- `cc` — commit
- `dv` — vertical diff vs index
- `<CR>` — open file
- `X` — discard changes (be careful!)

### Topology-flavored Fugitive recipes

```vim
" Three-branch graph in a scratch terminal buffer
:Git log --graph --oneline --decorate main feat-a feat-b feat-c

" Divergence between current branch and main, into quickfix
:Gclog --left-right --boundary main...HEAD

" Just commits unique to this branch, into quickfix
:Gclog main..HEAD
```

Once a `Gclog` populates the quickfix:

- `:copen` — open quickfix
- `:cnext` / `:cprev` — jump commits
- `<CR>` on entry — open the commit in a Fugitive buffer

---

## 🪟 Other Classic Vim Plugins (Windows-Friendly)

All of these install cleanly on classic Vim on Windows via [vim-plug](https://github.com/junegunn/vim-plug) or [Vundle](https://github.com/VundleVim/Vundle.vim).

| Plugin | Why you want it |
|---|---|
| [**vim-flog**](https://github.com/rbong/vim-flog) | Beautiful, interactive `git log --graph` viewer inside Vim. The `:Flog` command opens a navigable graph buffer. Best-in-class for topology browsing. |
| [**gv.vim**](https://github.com/junegunn/gv.vim) | Lighter alternative to vim-flog. `:GV`, `:GV!` (current file), `:GV?` (current file changes). Built on Fugitive. |
| [**vim-gitgutter**](https://github.com/airblade/vim-gitgutter) | Sign column shows added/changed/deleted lines vs HEAD. `]c` / `[c` to hop hunks. |
| [**vim-signify**](https://github.com/mhinz/vim-signify) | Like gitgutter but multi-VCS and async. Pick one. |
| [**vim-rhubarb**](https://github.com/tpope/vim-rhubarb) | Adds `:GBrowse` GitHub integration to Fugitive. |
| [**vim-conflicted**](https://github.com/christoomey/vim-conflicted) | Streamlines 3-way merge conflict resolution. |
| [**fzf.vim**](https://github.com/junegunn/fzf.vim) | `:Commits`, `:BCommits`, `:GFiles?` — fuzzy-find through history and changed files. |
| [**vim-merginal**](https://github.com/idanarye/vim-merginal) | Branch management UI on top of Fugitive. |
| [**committia.vim**](https://github.com/rhysd/committia.vim) | Richer commit-message editing experience. |

### Minimal `vimrc` snippet for the Git-heavy setup

```vim
" Plug
call plug#begin('~/vimfiles/plugged')   " Windows path
Plug 'tpope/vim-fugitive'
Plug 'tpope/vim-rhubarb'
Plug 'rbong/vim-flog'
Plug 'junegunn/gv.vim'
Plug 'airblade/vim-gitgutter'
Plug 'junegunn/fzf', { 'do': { -> fzf#install() } }
Plug 'junegunn/fzf.vim'
call plug#end()

" Quick mappings
nnoremap <leader>gs :Git<CR>
nnoremap <leader>gl :Flog<CR>
nnoremap <leader>gg :Git log --graph --oneline --decorate --all<CR>
nnoremap <leader>gb :Git blame<CR>
nnoremap <leader>gd :Gvdiffsplit<CR>
nnoremap <leader>gc :Commits<CR>
nnoremap <leader>gC :BCommits<CR>
```

---

## 🟦 PowerShell Workflow

You're PowerShell-centric on Windows, so these are designed to live in your `$PROFILE`.

### Open your profile

```powershell
# Edit your current-user, all-hosts profile
notepad $PROFILE.CurrentUserAllHosts

# Or in Vim
vim $PROFILE.CurrentUserAllHosts

# Make sure it exists
if (-not (Test-Path $PROFILE.CurrentUserAllHosts)) {
    New-Item -ItemType File -Path $PROFILE.CurrentUserAllHosts -Force
}
```

### Install `posh-git` (branch info in prompt)

```powershell
Install-Module posh-git -Scope CurrentUser -Force
Import-Module posh-git
```

Add to `$PROFILE`:

```powershell
Import-Module posh-git
```

### Drop-in PowerShell functions

```powershell
# ─────────────────────────────────────────────
# Git topology helpers — paste into $PROFILE
# ─────────────────────────────────────────────

function Get-GitGraph {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]] $Args
    )
    git log --graph --oneline --decorate --all @Args
}
Set-Alias glg Get-GitGraph

function Get-GitGraphPretty {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]] $Args
    )
    git log --graph --all --decorate --date=short `
        --pretty=format:'%C(yellow)%h%Creset %C(cyan)%ad%Creset %C(green)%<(14,trunc)%an%Creset %C(auto)%d%Creset %s' `
        @Args
}
Set-Alias glgp Get-GitGraphPretty

function Show-GitDivergence {
    <#
    .SYNOPSIS
        Show the divergence graph between the current branch (or supplied branch)
        and a base branch.
    .EXAMPLE
        Show-GitDivergence -Base main -Branch feat-a
    #>
    [CmdletBinding()]
    param(
        [string] $Base = 'main',
        [string] $Branch = 'HEAD'
    )
    git log --graph --oneline --decorate --left-right --boundary "$Base...$Branch"
}
Set-Alias gdiv Show-GitDivergence

function Show-GitThreeBranches {
    <#
    .SYNOPSIS
        Visualize three branches plus a base branch and where they fork.
    .EXAMPLE
        Show-GitThreeBranches main feat-a feat-b feat-c
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)] [string] $Base,
        [Parameter(Mandatory)] [string] $BranchA,
        [Parameter(Mandatory)] [string] $BranchB,
        [Parameter(Mandatory)] [string] $BranchC
    )
    $mb = git merge-base --octopus $Base $BranchA $BranchB $BranchC
    git log --graph --oneline --decorate "$mb^..HEAD" $Base $BranchA $BranchB $BranchC
}
Set-Alias g3 Show-GitThreeBranches

function Get-GitUnpushed {
    git log --branches --not --remotes --oneline --decorate
}
Set-Alias gunp Get-GitUnpushed

function Get-GitMergeBase {
    param(
        [string] $A = 'main',
        [string] $B = 'HEAD'
    )
    git merge-base $A $B
}
Set-Alias gmb Get-GitMergeBase

function Get-GitBranchSummary {
    <#
    .SYNOPSIS
        Show each local branch with ahead/behind counts vs a base.
    .EXAMPLE
        Get-GitBranchSummary -Base main
    #>
    [CmdletBinding()]
    param([string] $Base = 'main')

    git for-each-ref --format='%(refname:short)' refs/heads | ForEach-Object {
        $branch = $_
        if ($branch -eq $Base) { return }
        $counts = git rev-list --left-right --count "$Base...$branch" 2>$null
        if (-not $counts) { return }
        $parts = $counts -split '\s+'
        [pscustomobject]@{
            Branch  = $branch
            Behind  = [int]$parts[0]
            Ahead   = [int]$parts[1]
            Base    = $Base
        }
    } | Sort-Object Ahead -Descending | Format-Table -AutoSize
}
Set-Alias gbs Get-GitBranchSummary
```

Now from any repo:

```powershell
glg                                  # graph everything
glgp                                 # pretty graph
gdiv -Base main                      # divergence of current branch vs main
g3 main feat-a feat-b feat-c         # three branches + main
gunp                                 # what's not on the remote
gbs -Base main                       # ahead/behind table for every branch
gmb origin/main                      # merge base of HEAD vs origin/main
```

### Piping `git log` output into PowerShell objects

The real PowerShell superpower — turn log lines into objects you can filter/sort:

```powershell
function Get-GitLog {
    [CmdletBinding()]
    param(
        [int] $Count = 50,
        [string[]] $Args
    )
    $fmt = '%H%x09%h%x09%an%x09%ad%x09%s'   # tab-separated
    git log -n $Count --date=short --pretty="format:$fmt" @Args |
        ForEach-Object {
            $h, $sh, $an, $ad, $s = $_ -split "`t", 5
            [pscustomobject]@{
                Sha     = $h
                Short   = $sh
                Author  = $an
                Date    = [datetime]$ad
                Subject = $s
            }
        }
}

# Now you can do object-y things
Get-GitLog -Count 200 |
    Where-Object Author -like 'Eric*' |
    Where-Object Subject -match 'fix' |
    Sort-Object Date -Descending |
    Select-Object -First 10 |
    Format-Table -AutoSize
```

### Wrap with `Out-Host -Paging` for huge logs

```powershell
glg | Out-Host -Paging
```

> ⚠️ **PowerShell quirk:** `git` uses a pager (`less`) by default on many systems, which can hang in PowerShell when stdout is captured. If `git log` hangs, set: `git config --global core.pager ''` or use `git --no-pager log ...`.

---

## ⚡ Speed Boosters (delta, fzf, lazygit, tig)

These are the tools that change Git from "slow chore" to "fast feedback loop."

### [delta](https://github.com/dandavison/delta) — gorgeous diffs & syntax-highlighted logs

```powershell
# Install via scoop or winget
winget install dandavison.delta
# or
scoop install delta
```

Then in `~/.gitconfig`:

```ini
[core]
    pager = delta
[interactive]
    diffFilter = delta --color-only
[delta]
    navigate = true
    line-numbers = true
    side-by-side = true
[merge]
    conflictstyle = diff3
```

Now `git log -p`, `git show`, and `git diff` are dramatically better.

### [fzf](https://github.com/junegunn/fzf) — interactive commit picker

```powershell
winget install junegunn.fzf
```

PowerShell function:

```powershell
function Select-GitCommit {
    $sha = git log --oneline --decorate --all |
        & fzf --ansi --no-sort --reverse --tiebreak=index `
              --preview 'git show --stat --color=always {1}' |
        ForEach-Object { ($_ -split ' ')[0] }
    if ($sha) {
        git show $sha
    }
}
Set-Alias gpick Select-GitCommit
```

### [lazygit](https://github.com/jesseduffield/lazygit) — TUI for Git

```powershell
winget install JesseDuffield.lazygit
```

`lazygit` in any repo gives you a top-tier interactive UI. The commits panel shows the graph; press `?` for help.

### [tig](https://github.com/jonas/tig) — text-mode interface for Git

```powershell
scoop install tig
```

Run `tig` in a repo — you get a navigable graph view. Press `Enter` on any commit to see the diff. `m` for the main view, `s` for status, `l` for log.

---

## 🧪 Workflow Recipes

### "I'm about to PR — what am I shipping?"

```powershell
gdiv -Base main           # see both sides of divergence
git here                  # alias: commits unique to this branch
git diff main...HEAD --stat
```

### "Three feature branches diverge from main — what's going on?"

```powershell
g3 main feat-a feat-b feat-c

# Or in pure git:
git log --graph --oneline --decorate --simplify-by-decoration `
    main feat-a feat-b feat-c
```

### "I rebased and want to verify topology before pushing"

```powershell
glgp -20                                            # last 20 with pretty format
git log --graph --oneline --decorate @{u}..HEAD     # vs upstream
```

### "Find when a string was introduced"

```bash
git log -S"const FLAG = true" --source --all --oneline
```

### "Who touched this function last?"

```bash
git log -L :functionName:path/to/file
```

`-L` follows a specific function or line range through history — extraordinarily powerful and underused.

### "What branches contain this commit?"

```bash
git branch --contains <sha>
git branch -r --contains <sha>          # remote branches
git tag --contains <sha>                # tags
```

### "What branches have I forgotten about?"

```bash
# Local branches sorted by last commit date
git for-each-ref --sort=-committerdate refs/heads/ \
    --format='%(committerdate:short) %(refname:short) %(authorname)'
```

PowerShell version:

```powershell
git for-each-ref --sort=-committerdate refs/heads/ `
    --format='%(committerdate:short)%09%(refname:short)%09%(authorname)' |
    ForEach-Object {
        $d, $b, $a = $_ -split "`t"
        [pscustomobject]@{ Date=[datetime]$d; Branch=$b; Author=$a }
    } | Format-Table -AutoSize
```

### "Show me the commit graph as I work" (live)

```powershell
# Refresh every 2 seconds
while ($true) {
    Clear-Host
    git log --graph --oneline --decorate --all -20
    Start-Sleep -Seconds 2
}
```

Or use `watch` if you have Git Bash / WSL:

```bash
watch -n 2 'git log --graph --oneline --decorate --all -20'
```

---

## 🧷 Cheatsheet Card (print this)

```text
GRAPH         git log --graph --oneline --decorate --all
PRETTY        git log --graph --all --decorate --pretty=format:'%h %ad %an%d %s' --date=short
TOPOLOGY      git log --graph --simplify-by-decoration --all --decorate --oneline
3-BRANCHES    git log --graph --oneline --decorate main feat-a feat-b feat-c
DIVERGENCE    git log --graph --oneline --left-right --boundary main...feat-a
PR-SCOPE      git log --oneline main..HEAD
UNPUSHED      git log --branches --not --remotes --oneline --decorate
MERGE BASE    git merge-base main HEAD
OCTO BASE     git merge-base --octopus main feat-a feat-b feat-c
WHO TOUCHED   git log -L :funcName:path/to/file
STRING ORIGIN git log -S"text" --source --all --oneline
CONTAINS      git branch --contains <sha>
BRANCH AGE    git for-each-ref --sort=-committerdate refs/heads/
```

---

## 🤖💬❓ Follow-up Questions for You

If you want, I can expand any of these into a deeper note:

1. A **PowerShell module** packaging the `Get-GitGraph` / `Show-GitDivergence` / `Get-GitBranchSummary` functions with Pester tests (matches your `UnicodeFileNameTools` pattern in this repo).
2. A **Vim Fugitive-only** cheatsheet card optimized for keyboard-only Git review on Windows.
3. A **`git log` → object pipeline** for richer PowerShell analytics (e.g., commit velocity per author, churn per file).
4. **delta + fzf + lazygit** Windows installation walkthrough with screenshots.

Pick any and I'll write it up.
