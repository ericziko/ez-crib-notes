---
uid: d9bd46a0-8d4e-4a01-bcaa-c47497c6a388
---
# UnicodeFileNameTools

A PowerShell 7+ module for detecting and cleaning emoji & other Unicode
characters out of file names. Cross-platform (macOS, Linux, Windows).

## Why

Synced notes, screenshots, downloads, and exported artefacts routinely arrive
with names like `Q3 Report 🎉 final ✅.docx` or `crème brûlée 📷.heic`. These
work fine until they don't — Windows reserved characters, broken shell scripts,
fragile diffing, surprising sort order, paths that won't survive a ZIP round
trip. `UnicodeFileNameTools` gives you a small, well-bounded set of cmdlets to
detect, preview, and fix names — either as a pure string transform or against
real files on disk, with `-WhatIf` previews and collision handling.

## Install

The module is a folder; copy it onto a `$env:PSModulePath` entry, or import it
in place:

```powershell
Import-Module ./UnicodeFileNameTools/UnicodeFileNameTools.psd1
```

Requirements:
- PowerShell **7.0 or later** (Core edition). The module uses
  `[System.Text.Rune]` and modern normalization APIs that aren't available in
  Windows PowerShell 5.1.

## Quick start

```powershell
# Does this name contain emoji or other unwanted Unicode?
Test-UnicodeFileName -Name 'release 🎉.txt'       # True

# Show every non-ASCII character with full detail.
Get-UnicodeFileNameCharacter -Name 'café 🎉.md' | Format-Table

# Pure string transform — does not touch the disk.
Convert-UnicodeFileName -Name 'release 🎉.txt'    # release _.txt

# Rename actual files (with a preview first).
Get-ChildItem ./notes -Recurse | Repair-UnicodeFileName -Action Remove -WhatIf
Get-ChildItem ./notes -Recurse | Repair-UnicodeFileName -Action Remove
```

## Public surface

| Command                         | Purpose                                       |
| ------------------------------- | --------------------------------------------- |
| `Test-UnicodeFileName`          | Predicate: does this name contain targets?   |
| `Get-UnicodeFileNameCharacter`  | One detail object per non-ASCII character.   |
| `Convert-UnicodeFileName`       | Pure string cleaner. Highly configurable.    |
| `Repair-UnicodeFileName`        | Rename items on disk, with ShouldProcess.    |

See [`Usage.md`](Usage.md) for recipes and [`Architecture.md`](Architecture.md)
for how the category engine and substitution pipeline are wired together.

## Testing

```powershell
./Invoke-Tests.ps1
```

Pester 5+ required.

## Linting

```powershell
Install-Module PSScriptAnalyzer -Scope CurrentUser
Invoke-ScriptAnalyzer -Path ./UnicodeFileNameTools -Recurse -Settings ./PSScriptAnalyzerSettings.psd1
```
