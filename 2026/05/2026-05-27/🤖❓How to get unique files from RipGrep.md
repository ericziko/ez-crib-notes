---
uid: 4f6c7cf9-110d-4f76-8fd5-46d7013ffd1c
created: '2026-05-28T00:00:05+00:00'
modified: '2026-05-28T00:12:47+00:12'
title: 🤖❓How to get unique files from RipGrep
tags:
  - para/resouces/RipGrep
---

#para/resouces/RipGrep
# 🤖❓How to get unique files from RipGrep

- Using RipGrep - `rg` and whatever else I need to pipe it to, I want to get a unique list of full file paths for files from a certain directory that contain a certain string. I don't want the matches, I just want the file names.
- Please write me a detailed tutorial with plenty of examples from the perspective of a PowerShell user on Windows that also uses classic Vim.

## 🤖💡Getting Unique File Paths with RipGrep

### The Core Concept

RipGrep already does most of the heavy lifting here. By default, `rg` groups results by file and shows the filename as a header. But for a clean list of **just file paths**, you need the right flags.

---

### The Essential Flag: `-l` / `--files-with-matches`

This is the main tool you need. It tells `rg` to **only print the path of each matching file**, not the matches themselves.

```powershell
rg -l "search string" C:\path\to\directory
```

That's it for the basic case. The output is already unique because `rg` lists each file at most once.

#### Example Output

```
C:\Projects\notes\todo.md
C:\Projects\notes\ideas.txt
C:\Projects\src\main.ps1
```

---

### Breaking Down the Flags

#### `-l` or `--files-with-matches`

```powershell
# These are identical
rg -l "TODO" C:\Projects
rg --files-with-matches "TODO" C:\Projects
```

#### `-i` — Case Insensitive Search

```powershell
# Find files containing "todo", "TODO", "Todo", etc.
rg -li "todo" C:\Projects
```

#### `-t` — Limit to a File Type

```powershell
# Only search PowerShell files
rg -l -t ps1 "function" C:\Projects

# Only search markdown files
rg -l -t md "## Installation" C:\Projects

# See all supported types
rg --type-list
```

#### `-g` — Glob Pattern (More Flexible than `-t`)

```powershell
# Only .config files
rg -l -g "*.config" "connectionString" C:\Projects

# Only files starting with "test"
rg -l -g "test*" "Assert" C:\Projects

# Exclude a directory
rg -l -g "!node_modules/**" "require" C:\Projects
```

#### `--hidden` — Include Hidden Files and Folders

```powershell
# By default rg ignores dotfiles and hidden folders
rg -l --hidden "apiKey" C:\Projects
```

#### `--no-ignore` — Search Files Listed in `.gitignore`

```powershell
# Search everything, even gitignored files
rg -l --no-ignore "debug" C:\Projects
```

---

### Practical Real-World Examples

#### Find All Files Containing a Function Name

```powershell
rg -l "Get-UserProfile" C:\Scripts
```

#### Find Config Files Containing a Server Name

```powershell
rg -li "prod-server-01" C:\Configs
```

#### Find All Markdown Files Mentioning a Topic

```powershell
rg -l -t md "kubernetes" C:\Notes
```

#### Search Only Within a Specific Subdirectory

```powershell
rg -l "password" C:\Projects\src\auth
```

#### Search Multiple Directories at Once

```powershell
rg -l "TODO" C:\Projects\AppA C:\Projects\AppB C:\Projects\Shared
```

#### Use a Regex Pattern

```powershell
# Files containing any email address
rg -l "[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}" C:\Projects

# Files containing IP addresses
rg -l "\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b" C:\Configs
```

---

### Piping the Results in PowerShell

Since you're in PowerShell, the output of `rg -l` is plain text lines. You can pipe them into standard PowerShell cmdlets.

#### Count How Many Files Match

```powershell
rg -l "TODO" C:\Projects | Measure-Object
```

#### Sort the Results

```powershell
rg -l "TODO" C:\Projects | Sort-Object
```

#### Filter Results Further with `Select-String`

```powershell
# From those matching files, only show ones with "src" in the path
rg -l "TODO" C:\Projects | Select-String "src"
```

#### Open Every Matching File in Vim

```powershell
# Open all files in Vim as a buffer list
rg -l "TODO" C:\Projects | ForEach-Object { vim $_ }

# Better: open them all at once in Vim's argument list
vim (rg -l "TODO" C:\Projects)
```

#### Open in Vim with QuickFix Instead

If you want **matches and file context** in Vim's quickfix list rather than just file names, use this from inside Vim:

```vim
:cexpr system('rg --vimgrep "TODO" C:\Projects')
:copen
```

The `--vimgrep` flag outputs in `file:line:col:match` format that Vim's quickfix understands perfectly.

---

### Dealing with Spaces in Paths

PowerShell handles this cleanly when piping, but be careful when constructing paths manually.

```powershell
# Quote the directory path if it has spaces
rg -l "config" "C:\My Projects\App One"

# When piping results into something that launches a process
rg -l "TODO" C:\Projects | ForEach-Object { 
    Write-Host "Found in: $_"
}
```

---

### Saving Results to a File

```powershell
# Save to a text file
rg -l "FIXME" C:\Projects > matching_files.txt

# Append to an existing file
rg -l "FIXME" C:\Projects >> matching_files.txt

# Save with Out-File for more control (UTF-8 explicitly)
rg -l "FIXME" C:\Projects | Out-File -FilePath matching_files.txt -Encoding utf8
```

---

### Combining with PowerShell to Process the Files

#### Copy All Matching Files to Another Location

```powershell
rg -l "confidential" C:\Documents | ForEach-Object {
    Copy-Item $_ C:\Backup\Confidential\
}
```

#### Get File Metadata for All Matching Files

```powershell
rg -l "error" C:\Logs | ForEach-Object {
    Get-Item $_
} | Select-Object Name, Length, LastWriteTime
```

#### Find Matching Files Modified in the Last 7 Days

```powershell
$cutoff = (Get-Date).AddDays(-7)

rg -l "exception" C:\Logs | ForEach-Object {
    Get-Item $_
} | Where-Object { $_.LastWriteTime -gt $cutoff } | Select-Object FullName
```

---

### Quick Reference Card

| Goal | Command |
|---|---|
| Basic unique file list | `rg -l "pattern" C:\dir` |
| Case insensitive | `rg -li "pattern" C:\dir` |
| Specific file type | `rg -l -t md "pattern" C:\dir` |
| Glob pattern | `rg -l -g "*.json" "pattern" C:\dir` |
| Include hidden files | `rg -l --hidden "pattern" C:\dir` |
| Ignore `.gitignore` | `rg -l --no-ignore "pattern" C:\dir` |
| Regex pattern | `rg -l "reg[eE]x+" C:\dir` |
| Count matching files | `rg -l "p" C:\dir \| Measure-Object` |
| Open results in Vim | `vim (rg -l "p" C:\dir)` |
| Vim quickfix with matches | `:cexpr system('rg --vimgrep "p" C:\dir')` |

---

### Key Takeaway

> **`rg -l "your pattern" C:\your\directory`** gives you a clean, unique list of file paths with no duplicates and no noise. Everything else on this page is just refinement on top of that foundation.
