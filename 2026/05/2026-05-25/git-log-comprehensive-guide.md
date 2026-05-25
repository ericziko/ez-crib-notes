---
uid: git-log-guide-2026-05-25
title: Comprehensive Git Log Guide for PowerShell C# Developers
created: 2026-05-25T00:00:00Z
modified: 2026-05-25T00:00:00Z
tags:
  - git
  - powershell
  - dotnet
  - tools
  - version-control
---

# Comprehensive Git Log Guide for PowerShell C# Developers

> A deep dive into `git log` from the perspective of an intermediate PowerShell user developing .NET applications.

## Table of Contents

1. [Introduction](#introduction)
2. [Fundamentals](#fundamentals)
3. [Basic Options & Flags](#basic-options--flags)
4. [Output Formatting](#output-formatting)
5. [PowerShell Integration](#powershell-integration)
6. [Complex Filtering & Searching](#complex-filtering--searching)
7. [Tracking Code Changes Across .NET Solutions](#tracking-code-changes-across-net-solutions)
8. [Performance Optimization](#performance-optimization)
9. [Cheatsheet by Use Case](#cheatsheet-by-use-case)
10. [Real-World Scenarios](#real-world-scenarios)

---

## Introduction

`git log` is one of the most powerful tools in your git arsenal, yet many developers only scratch the surface with `git log --oneline`. As a C# developer working with multi-project solutions, you'll find `git log` invaluable for:

- **Tracking changes** across multiple projects in a monorepo or solution
- **Generating release notes** with proper formatting and categorization
- **Blaming** and understanding why changes were made
- **Code review preparation** by seeing what changed in a feature branch
- **Auditing** which commits touched specific classes or namespaces
- **Debugging** by identifying when a behavior changed

This guide assumes you're comfortable with basic git workflow (commits, branches, merges) and primarily work in PowerShell/Windows environment while developing C# applications.

---

## Fundamentals

### What Does `git log` Actually Do?

`git log` walks backward through your commit history, showing you what happened. By default, it starts at your current branch's HEAD and traverses parent commits.

```powershell
# Shows all commits reachable from HEAD, most recent first
git log

# Shows commits only reachable from specific branch
git log develop
git log feature/user-auth
```

### Key Concepts

**Commit Walk**: Git processes commits in reverse chronological order (newest first). This is important for filters—they apply *while walking*, not after.

**Ancestry**: Each commit knows its parent(s). Merge commits have multiple parents.

**References**: Branches, tags, and HEAD are just pointers to commits. `git log` can start from any reference.

```powershell
# These all walk the same history
git log HEAD
git log main
git log v1.0.0  # tag
git log abc123  # specific commit hash
```

---

## Basic Options & Flags

### Essential Flags

#### `--oneline`
Condenses each commit to one line (hash + subject).

```powershell
git log --oneline
# Output:
# abc1234 Fix null reference in UserService
# def5678 Add async/await to repository pattern
# ghi9012 Merge pull request #42 from feature/caching
```

**Use when**: You want a quick overview of recent commits.

---

#### `--graph`
Shows ASCII visualization of the branch structure.

```powershell
git log --oneline --graph
# Output:
# * abc1234 Fix null reference in UserService
# |\
# | * def5678 Add async/await to repository pattern
# |/
# * ghi9012 Initial commit
```

**Use when**: You need to understand merge patterns and branch topology.

---

#### `-n <number>` or `--max-count`
Limit output to N commits.

```powershell
git log -n 10          # Last 10 commits
git log --max-count=5  # Last 5 commits (same thing, verbose syntax)
```

**Use when**: You only care about recent activity.

---

#### `--all`
Shows commits reachable from ANY branch/tag, not just current branch.

```powershell
git log --oneline --all
git log --oneline --graph --all  # Entire repo history with visualization
```

**Important**: Without `--all`, you only see the current branch's history. This is crucial when comparing feature branches!

```powershell
# Only shows commits in current branch
git log --oneline

# Shows entire repo history
git log --oneline --all

# See what commits are on main but NOT on current branch
git log --oneline main ^HEAD  # or: git log main --not HEAD
```

---

#### `--decorate`
Shows branch/tag names next to commits.

```powershell
git log --oneline --decorate
# Output:
# abc1234 (HEAD -> feature/auth, origin/feature/auth) Add login validation
# def5678 (main, origin/main) Merge pull request #40
```

**Use when**: You want to see which commits are tagged or on which branches.

---

#### `--reverse`
Shows commits in reverse order (oldest first instead of newest first).

```powershell
git log --reverse --oneline
```

**Use when**: You're tracing the evolution of a feature from start to finish.

---

#### `--follow`
Tracks file history across renames.

```powershell
git log --oneline --follow -- src/Services/UserService.cs
```

If the file was renamed, this continues showing history under the old name.

**Use when**: You renamed a file and need complete history.

---

### Date & Author Flags

#### `--since` and `--until`
Filter by date range.

```powershell
git log --since="2 weeks ago"
git log --since="2026-05-01" --until="2026-05-25"
git log --since="1 month ago" --until="1 week ago"
git log --since="2026-05-20 09:00:00"  # Specific timestamp
```

**Supported date formats**:
- Relative: `"2 weeks ago"`, `"1 month ago"`, `"3 days ago"`
- ISO8601: `"2026-05-25"`, `"2026-05-25T14:30:00"`
- RFC: `"May 25 2026"`

---

#### `--author`
Filter commits by author name or email.

```powershell
git log --author="Eric Ziko"
git log --author="eric.ziko@gmail.com"
git log --author="Eric|Sarah"  # Multiple authors (regex)
```

**Note**: This is a regex pattern, so special regex characters need escaping.

---

#### `--committer`
Filter by committer (different from author in workflows with cherry-picks or patch submissions).

```powershell
git log --committer="GitHub Actions"
```

---

### Path & File Filtering

#### `--` (double dash)
Separates git commands from file paths. Essential for log!

```powershell
# Shows commits that touched this file
git log -- src/Services/UserService.cs

# Multiple files
git log -- src/Services/*.cs

# Entire directory
git log -- src/
```

**Critical difference**:
```powershell
# These are DIFFERENT:
git log main
# vs
git log -- main
# First: log of branch "main"
# Second: log of file named "main" (if it existed)
```

---

#### `-p` or `--patch`
Shows the full diff of what changed in each commit.

```powershell
git log -p -- src/Services/UserService.cs
```

**Warning**: Can be very verbose! Combine with `-n` to limit:

```powershell
git log -p -n 3 -- src/Services/UserService.cs  # Last 3 changes to file
```

---

#### `--stat`
Shows statistics of changes (files changed, insertions, deletions).

```powershell
git log --stat
# Output:
# commit abc1234567890abcdef1234567890abcdef123456
# Author: Eric Ziko <eric.ziko@gmail.com>
# Date:   Thu May 25 09:00:00 2026 -0600
#
#     Add async patterns to UserRepository
#
#  src/Services/UserService.cs      | 45 ++++++++++++++++++++
#  src/Repositories/UserRepository.cs | 32 +++++++++-----
#  tests/UserRepositoryTests.cs       | 78 +++++++++++++++++++++++++++++++++++
#  3 files changed, 145 insertions(+), 13 deletions(-)
```

**Use when**: You want a summary of impact without seeing full diffs.

---

#### `--name-only`
Shows only filenames changed (no diffs, no stats).

```powershell
git log --name-only
```

**Use when**: You only care which files were touched, not the details.

---

#### `--name-status`
Like `--name-only` but with status (M=modified, A=added, D=deleted, R=renamed, C=copied).

```powershell
git log --name-status
# Output:
# M src/Services/UserService.cs
# A src/Services/EmailService.cs
# D src/Legacy/OldUserHandler.cs
# R src/Repositories/UserRepository.cs => src/Data/UserRepository.cs
```

---

## Output Formatting

### `--pretty` Presets

Git includes several built-in formatting templates.

#### `--pretty=oneline`
Equivalent to `--oneline`. Hash + subject on one line.

```powershell
git log --pretty=oneline
```

---

#### `--pretty=short`
Hash + Author + Subject.

```powershell
git log --pretty=short
# Output:
# commit abc1234567890abcdef1234567890abcdef123456
# Author: Eric Ziko <eric.ziko@gmail.com>
#
#     Add async patterns to UserRepository
```

---

#### `--pretty=medium`
Default format. Hash + Author + Date + Subject + Body.

```powershell
git log --pretty=medium
```

---

#### `--pretty=full`
Like medium but also shows committer info (useful in workflows with multiple hands on patches).

---

#### `--pretty=fuller`
Full details: author, author date, committer, committer date, subject, body.

---

#### `--pretty=email`
Formats as email with headers.

```powershell
git log --pretty=email
# Output:
# From: Eric Ziko <eric.ziko@gmail.com>
# Date: Thu, 25 May 2026 09:00:00 -0600
# Subject: [PATCH] Add async patterns to UserRepository
#
# Add async patterns to UserRepository
```

---

#### `--pretty=tformat`
Tail format. Like format, but adds a newline after each commit. Used for custom formatting.

---

### `--format` Custom Formatting

For fine-grained control, use `--format` with placeholders.

#### Common Placeholders

| Placeholder | Meaning |
|---|---|
| `%h` | Abbreviated commit hash (7 chars) |
| `%H` | Full commit hash |
| `%an` | Author name |
| `%ae` | Author email |
| `%ad` | Author date |
| `%cn` | Committer name |
| `%ce` | Committer email |
| `%cd` | Committer date |
| `%s` | Subject (first line of commit message) |
| `%b` | Body (everything after subject) |
| `%d` | Ref names (branches, tags) |
| `%n` | Newline |
| `%aN` | Author name (with unicode) |
| `%ar` | Author date (relative, like "2 weeks ago") |
| `%cr` | Committer date (relative) |
| `%ai` | Author date (ISO8601 strict) |
| `%ci` | Committer date (ISO8601 strict) |
| `%G?` | GPG signature status (N/G/B/U/R/E/X/Y) |
| `%GS` | Signer name (if signed) |

#### Examples

**One-liner with hash, author, and date:**
```powershell
git log --format="%h %an - %ar - %s"
# Output:
# abc1234 Eric Ziko - 2 hours ago - Add async patterns to UserRepository
# def5678 Sarah Smith - 3 days ago - Fix null reference in UserService
```

**CSV format (useful for piping to PowerShell):**
```powershell
git log --format="%h,%an,%ad,%s" --date=short
# Output:
# abc1234,Eric Ziko,2026-05-25,Add async patterns to UserRepository
# def5678,Sarah Smith,2026-05-22,Fix null reference in UserService
```

**Multi-line with colors and indentation:**
```powershell
git log --format="%C(bold cyan)%h%C(reset) - %C(green)%an%C(reset) (%C(yellow)%ar%C(reset))%n  %C(white)%s%C(reset)"
```

**For release notes (author grouped):**
```powershell
git log --format="- [%h] %s (%an)" v1.0.0..HEAD
```

#### Date Formatting

Pair `--format` with `--date=<format>`:

```powershell
git log --format="%ad - %s" --date=short      # YYYY-MM-DD
git log --format="%ad - %s" --date=relative   # "2 weeks ago"
git log --format="%ad - %s" --date=iso        # ISO 8601 with timezone
git log --format="%ad - %s" --date=iso-strict # Strict ISO 8601
git log --format="%ad - %s" --date=rfc2822    # RFC 2822 (email style)
```

---

## PowerShell Integration

### Using Git Log with PowerShell Cmdlets

Since `git log` outputs text, you can pipe it to PowerShell for filtering and transformation.

#### Parsing `--format` Output

Create CSV output and pipe to `ConvertFrom-Csv`:

```powershell
# Format commits as CSV
git log --format="%h,%an,%ad,%s" --date=short | 
  ConvertFrom-Csv -Header @("Hash", "Author", "Date", "Message") |
  Where-Object { $_.Author -eq "Eric Ziko" } |
  Select-Object Date, Message
```

#### Filtering with Where-Object

```powershell
# Get all commits from the last week as objects
git log --format="%h|%an|%ad" --date=short --since="1 week ago" |
  ForEach-Object { 
    $parts = $_ -split '\|'
    [PSCustomObject]@{
      Hash = $parts[0]
      Author = $parts[1]
      Date = $parts[2]
    }
  } |
  Where-Object { $_.Author -match "Eric|Sarah" } |
  Format-Table
```

#### Grouping by Author

```powershell
# Count commits per author
git log --format="%an" --all |
  Group-Object |
  Sort-Object Count -Descending |
  Format-Table -AutoSize
# Output:
# Count Name
# ----- ----
#    42 Eric Ziko
#    28 Sarah Smith
#    15 GitHub Actions
```

#### Generating Statistics

```powershell
# Average commits per day
$commits = git log --format="%ad" --date=short | Measure-Object -Line
$days = (git log --format="%ad" --date=short | Sort-Object | Select-Object -First 1 -Last 1).Count
$avgPerDay = $commits.Lines / 7
Write-Host "Average commits per day (last 7 days): $avgPerDay"
```

### PowerShell Aliases for Common Patterns

Add these to your PowerShell profile (`$PROFILE`):

```powershell
# ~/.config/powershell/profile.ps1 or $PROFILE

# Recent commits
function glr { git log --oneline -20 }

# With graph
function glg { git log --oneline --graph --all -20 }

# My commits
function glm { git log --format="%h %an - %ar - %s" --author=$env:USERNAME --max-count=20 }

# Commits since last tag
function glst { git log --oneline $(git describe --tags --abbrev=0)..HEAD }

# Changes in current feature branch vs main
function glfd { git log --oneline --graph main...HEAD }

# All commits by author (interactive)
function glba {
  param([string]$author = $(Read-Host "Author name"))
  git log --oneline --author=$author --all
}
```

Usage:
```powershell
glr              # Last 20 commits
glg              # With graph visualization
glm              # My commits
glst             # Since last tag
glfd             # Feature branch diff from main
glba "Eric Ziko" # All commits by specific author
```

### Handling Windows Line Endings

On Windows, piping git output sometimes includes carriage returns. Handle this:

```powershell
# Method 1: Use git's core.quotepath
git config core.quotepath false

# Method 2: Strip CR in PowerShell pipe
git log --format="%an" | ForEach-Object { $_ -replace "`r", "" }

# Method 3: Use git with --pretty explicitly
git log --pretty=format:"%an" | ForEach-Object { $_.Trim() }
```

---

## Complex Filtering & Searching

### Branch Comparisons

#### What's on branch X but not on branch Y?

```powershell
# Commits on develop but not on main
git log main..develop --oneline

# Commits on current branch but not on main
git log main..HEAD --oneline
```

**Reverse it:**
```powershell
# Commits on main but not on current branch
git log HEAD..main --oneline
```

#### Symmetric difference (on either branch but not both)

```powershell
git log main...develop --oneline --graph
```

---

### `--grep` Pattern Matching

Search commit messages (subjects) for patterns.

```powershell
# Find commits mentioning "UserService"
git log --grep="UserService" --oneline

# Case-insensitive search
git log --grep="userservice" --ignore-case --oneline

# Regex search
git log --grep="Add|Fix|Refactor" --oneline

# NOT matching (prefix with ^)
git log --grep="WIP" --grep="Debug" --all-match --invert-grep --oneline
```

**Important flags:**
- `--ignore-case`: Make search case-insensitive
- `--invert-grep`: Show commits that DON'T match
- `--all-match`: All patterns must match (AND logic)

---

### Search Commit Content

#### Search actual code changes (`-S`)

Search for commits that added or removed a specific string.

```powershell
# Commits that changed the string "ConnectionString"
git log -S "ConnectionString" --oneline

# With full diff of the change
git log -S "ConnectionString" -p
```

#### Search with regex (`-G`)

Like `-S` but uses regex pattern.

```powershell
# Commits that changed number of spaces at start of line (whitespace changes)
git log -G "^[[:space:]]+" --oneline

# Commits touching async/await keywords
git log -G "\basync\b|\bawait\b" --oneline
```

**Difference between `-S` and `-G`:**
- `-S` : Exact string. Ignores whitespace changes.
- `-G` : Regex pattern. Detects any change matching regex.

---

### Merges, No-Merges, First Parent

#### See only merge commits

```powershell
git log --merges --oneline
```

#### See only non-merge commits

```powershell
git log --no-merges --oneline
```

#### First parent only (linear history)

Useful for main branch where you use "squash and merge":

```powershell
git log --first-parent --oneline
```

---

### Complex Commit Selection

#### `--not` and `^` (negation)

```powershell
# Everything reachable from HEAD, except from main
git log --not main
# Same as:
git log main..HEAD

# Everything except from develop AND staging
git log --all --not develop staging
```

#### Multiple references

```powershell
# Everything from main, develop, release/* but not hotfix/*
git log main develop release/* --not hotfix/*
```

---

## Tracking Code Changes Across .NET Solutions

This section addresses common C# / .NET multi-project scenarios.

### Finding Which Project Changed

In a multi-project solution:

```
src/
├── Core/
│   └── UserService.cs
├── Data/
│   └── UserRepository.cs
├── Web/
│   └── UserController.cs
└── Tests/
    └── UserServiceTests.cs
```

**See changes by project:**

```powershell
# Changes in Data project
git log --oneline -- src/Data/

# Changes in Core project
git log --oneline -- src/Core/

# Changes in multiple projects
git log --oneline -- src/Data/ src/Web/

# Exclude tests
git log --oneline -- src/ --not -- src/Tests/
```

**Using name-status to see what changed:**

```powershell
git log --name-status --oneline -- src/Data/ | head -30
# Output shows: M, A, D, R with filenames
```

### Blame Across Projects

Find who last touched a specific class:

```powershell
git blame src/Services/UserService.cs
```

Focus on a specific method:

```powershell
# Find commits touching GetUserAsync method
git log -p -S "GetUserAsync" -- src/Services/UserService.cs

# Or with grep in commit messages
git log --grep="GetUserAsync|UserService" --oneline
```

### Tracking Namespace Changes

C# refactoring often involves moving classes between namespaces.

```powershell
# Commits that changed "namespace MyApp.Data"
git log -S "namespace MyApp.Data" --oneline

# Commits with "namespace" in diff (any namespace change)
git log -G "namespace\s+\w+" -p | head -50
```

### Finding Cross-Project Impact

When a shared class changed, see which projects reference it:

```powershell
# Find commits touching shared Model
git log -p -S "class User" --follow -- src/

# Then in each project, check if it broke:
git log --since="2 days ago" -- src/Web/ | head  # Any Web changes?
git log --since="2 days ago" -- src/Tests/       # Any test commits?
```

### Release Notes from Multiple Projects

Group changes by project for release notes:

```powershell
# Get commits between releases, showing which projects changed
git log v1.0.0..v1.1.0 --name-status --oneline |
  grep -E "^M|^A|^D" |
  Cut -d' ' -f2 |
  Cut -d'/' -f1-2 |
  Sort | Uniq -c | Sort -rn
```

Or in PowerShell:

```powershell
# Commits between two tags, grouped by project folder
git log v1.0.0..HEAD --name-status --oneline |
  ConvertFrom-Csv -Delimiter ' ' -Header Status, File |
  ForEach-Object {
    $folder = ($_.File -split '/')[0]
    [PSCustomObject]@{ Project = $folder; Files = $_.File }
  } |
  Group-Object Project |
  Sort-Object Count -Descending |
  ForEach-Object { "$($_.Name): $($_.Count) changes" }
```

### Finding When Tests Broke

```powershell
# Commits touching test files recently
git log --oneline --since="1 week ago" -- src/Tests/ src/**/*Tests.cs

# Commits that added/removed test methods
git log -S "[Fact]" --oneline -- src/Tests/
git log -S "[Theory]" --oneline -- src/Tests/
```

### Tracking Database/EF Core Migrations

```powershell
# Commits to Migrations folder
git log --oneline -- src/Data/Migrations/

# See diff of a migration
git log -p -- src/Data/Migrations/20260525000000_AddUserTable.cs

# Commits that touched DbContext
git log -S "DbContext" --oneline -- src/Data/
```

---

## Performance Optimization

### Large Repository Concerns

Git log can be slow on large repos with deep history.

#### Limit Search Scope

```powershell
# Much faster: only recent commits
git log -n 100

# Much faster: only current branch (not --all)
git log --oneline -n 50

# Much faster: specific path only
git log --oneline -- src/Services/
```

#### Use References Not Ancestry

```powershell
# Slower: traverses all ancestry
git log main

# Faster: specific commit range
git log v1.0.0..v1.1.0
git log main@{2.weeks.ago}..main
```

#### Avoid Expensive Operations

```powershell
# SLOW: Traverses all commits, checking every diff
git log -G "pattern"

# FAST: Just message search
git log --grep="keyword"

# FAST: Specific file
git log -- file.cs
```

#### Consider `--since` for Bounded Searches

```powershell
# Unbounded (potential slowness on old repos)
git log --author="Eric"

# Bounded to recent
git log --since="3 months ago" --author="Eric"
```

### One-Liners for Checking Performance

```powershell
# Count commits (might be slow on huge repos)
git rev-list --count HEAD

# Estimate repo size
git count-objects -v

# Check largest blobs
git rev-list --all --objects | sort -k2 | tail -10
```

---

## Cheatsheet by Use Case

This section organizes common patterns by what you're trying to accomplish.

### 📋 Review Recent Work

**See last 10 commits:**
```powershell
git log --oneline -n 10
```

**See last 10 commits with graph:**
```powershell
git log --oneline --graph -n 10
```

**See my commits from today:**
```powershell
git log --oneline --author="Eric Ziko" --since="1 day ago"
```

**See what changed in last commit:**
```powershell
git log -p -n 1
```

---

### 🔀 Compare Branches

**What's on feature branch but not main?**
```powershell
git log main..HEAD --oneline

# Or more explicitly:
git log --oneline feature/auth ^main
```

**What's on main but not feature branch?**
```powershell
git log HEAD..main --oneline
```

**Visual branch comparison:**
```powershell
git log --oneline --graph --all -n 50
```

**How many commits ahead/behind?**
```powershell
git rev-list --count main..HEAD        # Ahead
git rev-list --count HEAD..main        # Behind
```

---

### 🎯 Find When Something Changed

**When was this method added?**
```powershell
git log -S "public async Task<User> GetUserAsync" --oneline -p
```

**When was this class deleted?**
```powershell
git log -S "class EmailService" --oneline -p
```

**When was file renamed?**
```powershell
git log --follow -- OldName.cs
```

**Commits touching specific namespace:**
```powershell
git log -S "namespace MyApp.Services" --oneline
```

---

### 📝 Generate Release Notes

**Since last tag:**
```powershell
git log $(git describe --tags --abbrev=0)..HEAD --oneline
```

**With nicer formatting:**
```powershell
git log $(git describe --tags --abbrev=0)..HEAD `
  --format="- [%h] %s (%an)" `
  --no-merges
```

**Group by author:**
```powershell
git log --format="%an|%s" v1.0.0..HEAD `
  | ConvertFrom-Csv -Delimiter '|' -Header Author,Message `
  | Group-Object Author `
  | ForEach-Object { "## $($_.Name)`n"; $_.Group | ForEach-Object { "- $($_.Message)" } }
```

**Between two tags:**
```powershell
git log v1.0.0..v1.1.0 --oneline --no-merges
```

---

### 🐛 Debug: Find Breaking Change

**When did this test start failing?**
```powershell
# Commits touching test file
git log --oneline -p -- Tests/UserServiceTests.cs | head -100
```

**When did this method signature change?**
```powershell
git log -S "GetUser(string id)" --oneline -p
```

**Search commit messages for clues:**
```powershell
git log --grep="breaking\|breaking change\|breaking:" -i --oneline
```

**Commits touching specific folder (likely culprit):**
```powershell
git log --oneline -- src/Data/ | head -20
```

---

### 👤 Find Who Did What

**All commits by specific author:**
```powershell
git log --oneline --author="Eric Ziko"
```

**Commits by multiple people:**
```powershell
git log --oneline --author="Eric|Sarah|GitHub Actions"
```

**Who touched this file most recently?**
```powershell
git log --oneline -- src/Services/UserService.cs | head -1
```

**Commits by author in date range:**
```powershell
git log --author="Eric" --since="2026-05-01" --until="2026-05-25" --oneline
```

**Count commits per author (all-time):**
```powershell
git shortlog -sn
# or in PowerShell:
git log --format="%an" --all | Group-Object | Sort-Object Count -Descending
```

---

### 📁 Track Changes in .NET Project

**Changes in specific project:**
```powershell
git log --oneline -- src/Core/
git log --oneline -- src/Data/Services/
```

**See what files changed:**
```powershell
git log --name-status -- src/Services/
```

**Get stats (insertions/deletions):**
```powershell
git log --stat -- src/Services/ | head -30
```

**Show all changes to a project:**
```powershell
git log -p -- src/Services/ | head -200
```

**Find when a class was deleted:**
```powershell
git log -p -- src/Services/OldUserHandler.cs | head -50
```

---

### 🏷️ Work with Tags and Versions

**Commits since last tag:**
```powershell
git log $(git describe --tags --abbrev=0)..HEAD --oneline
```

**Commits in specific version range:**
```powershell
git log v1.0.0..v2.0.0 --oneline
```

**When was tag created?**
```powershell
git log -1 v1.0.0 --format="%ai - %s"
```

**All commits from tag to now:**
```powershell
git log v1.0.0..HEAD --oneline
```

---

### 🔍 Code Search & Pattern Matching

**Find commits that mentioned a keyword:**
```powershell
git log --grep="null reference" -i --oneline
```

**Commits that changed a specific string:**
```powershell
git log -S "ConnectionString" --oneline
```

**Commits with async/await changes (regex):**
```powershell
git log -G "\b(async|await)\b" --oneline
```

**Complex: Find async methods that were added:**
```powershell
git log -G "public async Task" --oneline
```

**Commits touching a class definition:**
```powershell
git log -S "class UserRepository" --oneline -p
```

---

### 🎨 Format & Customize Output

**Custom comma-separated (for parsing):**
```powershell
git log --format="%h,%an,%ad,%s" --date=short
```

**Prettier with colors (terminal):**
```powershell
git log --format="%C(auto)%h%C(reset) - %C(cyan)%an%C(reset) %C(green)(%ar)%C(reset) %s"
```

**For pasting into Slack:**
```powershell
git log --format="> `%h` %s — %an" --no-decorate -n 5
```

**Author with email:**
```powershell
git log --format="[%an <%ae>] %s" --oneline
```

**With timezone info:**
```powershell
git log --format="%h %ai %s" --oneline
```

---

### 🚀 CI/CD & Automation

**Commits since last release:**
```powershell
$lastTag = git describe --tags --abbrev=0
git log "$lastTag..HEAD" --oneline
```

**Check if branch has unpushed commits:**
```powershell
git log origin/main..HEAD --oneline
```

**Validate commit message quality:**
```powershell
git log origin/main..HEAD --format="%s" | 
  ForEach-Object { 
    if ($_ -notmatch "^(feat|fix|refactor|docs|test|chore)(\(.+\))?:") {
      Write-Host "Invalid commit message: $_" -ForegroundColor Red
    }
  }
```

**Export to JSON for tooling:**
```powershell
git log --format='{"hash":"%h","author":"%an","date":"%ad","message":"%s"}' --date=short
```

---

## Real-World Scenarios

### Scenario 1: Code Review Prep

You're about to review a pull request. You want to see what changed.

```powershell
# On feature branch
git log main..HEAD --oneline --graph

# See actual changes
git log main..HEAD --stat

# Full diffs
git log main..HEAD -p

# Or from command line:
git diff main...HEAD
```

**PowerShell enhanced version:**
```powershell
$featureBranch = "feature/auth-refactor"
$baseBranch = "main"

Write-Host "Changes in $featureBranch (not in $baseBranch):" -ForegroundColor Cyan
git log "$baseBranch..$featureBranch" --oneline

Write-Host "`nFiles changed:" -ForegroundColor Cyan
git log "$baseBranch..$featureBranch" --name-status | grep -E "^[MAD]"

Write-Host "`nLines changed:" -ForegroundColor Cyan
git log "$baseBranch..$featureBranch" --stat | grep "files changed"
```

---

### Scenario 2: "This Used to Work—What Broke It?"

A test that passed last week now fails. Find the culprit.

```powershell
# Option 1: Blame the test file
git blame tests/AuthServiceTests.cs | grep -i "ExpectLogin"

# Option 2: Find recent commits touching test
git log --oneline --since="7 days ago" -- tests/AuthServiceTests.cs

# Option 3: Search for method changes
git log -S "public async Task TestLoginValidation" --oneline -p

# Option 4: Find commits that touched both test and code
git log --oneline --since="2 weeks ago" -- tests/AuthServiceTests.cs src/Services/AuthService.cs
```

**PowerShell diagnostic:**
```powershell
$testFile = "tests/AuthServiceTests.cs"
$serviceFile = "src/Services/AuthService.cs"

Write-Host "Commits touching test or service (last 2 weeks):" -ForegroundColor Cyan
git log --since="2 weeks ago" --oneline --date=short --format="%ad | %an | %s" -- $testFile $serviceFile |
  ForEach-Object { 
    $date, $author, $message = $_ -split '\s*\|\s*'
    [PSCustomObject]@{
      Date = $date
      Author = $author.Trim()
      Message = $message.Trim()
    }
  } |
  Format-Table -AutoSize
```

---

### Scenario 3: Release Notes Generation

It's release time. You need to generate release notes from commits between v1.0.0 and v1.1.0.

```powershell
# Simple version
git log v1.0.0..v1.1.0 --oneline --no-merges

# Better: Categorized
git log v1.0.0..v1.1.0 --format="- %s" --no-merges

# With author
git log v1.0.0..v1.1.0 --format="- %s (%an)" --no-merges
```

**PowerShell sophisticated version:**
```powershell
function Generate-ReleaseNotes {
    param(
        [string]$from = $(git describe --tags --abbrev=0),
        [string]$to = "HEAD",
        [string]$title = "Release Notes"
    )
    
    Write-Host "# $title`n" -ForegroundColor Cyan
    Write-Host "## Changes from $from to $to`n"
    
    $commits = git log "$from..$to" --format="%s" --no-merges
    
    # Categorize by keyword
    $features = $commits | Where-Object { $_ -match "^feat|add|new" -i }
    $fixes = $commits | Where-Object { $_ -match "^fix|bug" -i }
    $refactors = $commits | Where-Object { $_ -match "^refactor|refact" -i }
    $other = $commits | Where-Object { 
        $_ -notmatch "^(feat|add|new|fix|bug|refactor|refact)" -i 
    }
    
    if ($features) {
        Write-Host "### ✨ Features`n"
        $features | ForEach-Object { Write-Host "- $_" }
        Write-Host
    }
    
    if ($fixes) {
        Write-Host "### 🐛 Bug Fixes`n"
        $fixes | ForEach-Object { Write-Host "- $_" }
        Write-Host
    }
    
    if ($refactors) {
        Write-Host "### 🔧 Refactoring`n"
        $refactors | ForEach-Object { Write-Host "- $_" }
        Write-Host
    }
    
    if ($other) {
        Write-Host "### 📝 Other`n"
        $other | ForEach-Object { Write-Host "- $_" }
        Write-Host
    }
}

# Usage:
Generate-ReleaseNotes -from "v1.0.0" -to "v1.1.0" -title "Release v1.1.0"
```

---

### Scenario 4: Merging Back to Main—What Do We Get?

You're about to merge your feature branch. Verify what commits are coming in.

```powershell
# What's new from feature branch?
git log main..feature/big-refactor --oneline

# What will change?
git diff main...feature/big-refactor --stat

# Which files specifically?
git diff main...feature/big-refactor --name-only
```

**Check for potential conflicts:**
```powershell
# Find commits that touched same files on main recently
git log --since="2 weeks ago" --oneline -- (git diff main...HEAD --name-only)
```

---

### Scenario 5: Auditing Who Changed What (Compliance)

You need to document who changed sensitive code (e.g., encryption, payment processing).

```powershell
# All commits touching encryption file
git log --oneline --format="%h|%an|%ad|%s" --date=short -- src/Security/EncryptionService.cs |
  ConvertFrom-Csv -Delimiter '|' -Header Hash,Author,Date,Message |
  Format-Table -AutoSize

# Export to file for audit trail
git log --format="%h|%an|%ad|%s|%b" --date=short -- src/Security/ |
  Out-File -FilePath "encryption-audit.txt"
```

---

### Scenario 6: Finding the Right Commit to Cherry-Pick

You need to pick a specific fix from another branch, but there are many commits.

```powershell
# Search for the fix by message
git log develop --grep="fix.*null reference" --oneline

# See the commit details
git log -1 <commit-hash> -p

# Cherry-pick it
git cherry-pick <commit-hash>
```

---

### Scenario 7: Tracking .NET-Specific Changes

Find all commits that touched async/await or Entity Framework patterns.

```powershell
# Added async/await
git log -G "async Task" --oneline

# Touched DbContext configuration  
git log -S "modelBuilder" --oneline -- src/Data/

# Migration commits
git log --oneline -- src/Data/Migrations/

# Dependency Injection setup changes
git log -G "services\.(Add|Register)" --oneline
```

**For audit:**
```powershell
# Find all DbContext changes with author/date
git log -S "DbContext" `
  --format="%h %an <%ae> [%ad] %s" `
  --date=short `
  -- src/Data/

# Or full diff:
git log -S "DbContext" -p -- src/Data/ | head -100
```

---

## Troubleshooting Common Issues

### "Git log is too slow"

```powershell
# Use --since to bound search
git log --since="3 months ago"

# Use -n to limit output
git log -n 100

# Don't use --all if you don't need it
git log (instead of) git log --all
```

### "I don't see a commit I expect"

```powershell
# Did you forget --all?
git log --all --oneline | grep "keywords"

# Is it on a different branch?
git log --oneline <branch-name>

# Check if it's been reflogged
git reflog | head -20
```

### "The output is cut off / wrapping badly"

```powershell
# Pipe to 'less' for pagination
git log | less

# Or use PowerShell pager
git log | more

# Or redirect to file
git log > commits.txt
```

### "Special characters look weird (boxes, ?, etc.)"

This is usually UTF-8 encoding or terminal font issues.

```powershell
# Try with --no-decorate to simplify
git log --oneline --no-decorate

# Or use simpler format
git log --format="%h %s" --oneline

# Check PowerShell encoding
$OutputEncoding = [System.Text.UTF8Encoding]::new()
```

---

## Summary Table: Quick Reference

| What You Want | Command |
|---|---|
| Last 10 commits | `git log -n 10 --oneline` |
| With branch visualization | `git log --oneline --graph --all` |
| Commits by author | `git log --author="Name" --oneline` |
| Since specific date | `git log --since="2026-05-01" --oneline` |
| Commits in file | `git log -- src/Services/UserService.cs` |
| Commits in folder | `git log -- src/Services/` |
| Full diff | `git log -p -n 5` |
| Statistics only | `git log --stat -n 10` |
| Feature vs main | `git log main..HEAD --oneline` |
| What's new in main | `git log main --not HEAD --oneline` |
| Since last tag | `` git log `git describe --tags --abbrev=0`..HEAD --oneline `` |
| Search commit messages | `git log --grep="keyword" --oneline` |
| Search code changes | `git log -S "string" --oneline` |
| Merges only | `git log --merges --oneline` |
| No-merge commits | `git log --no-merges --oneline` |
| My commits | `git log --author=$env:USERNAME --oneline` |

---

## Final Tips

1. **Always use `--oneline`** as your default viewing mode. Add `--graph` when comparing branches.

2. **Use `--all`** when you're confused. It shows you the complete picture.

3. **Combine flags strategically**:
   - `--format` + `--date` for custom output
   - `--since` + `--author` for scoped searches
   - `-S` or `-G` + file path for targeted code archaeology

4. **For .NET solutions**, organize your `git log` thinking by project:
   - `git log -- src/ProjectName/` for scoped history
   - `git log -p -- src/ProjectName/ClassName.cs` for file-specific archaeology

5. **Create PowerShell aliases** for your 3-4 most common patterns. You'll use them daily.

6. **When debugging**, start broad (`git log --all --oneline`) then narrow (`git log -S "keyword" -p`).

7. **Remember**: `--` (double dash) separates git operations from file paths. Use it consistently.

8. **Complex searches are cheaper than reading diffs**. Use `-S` and `--grep` before diving into `git show` or `git diff`.

---

