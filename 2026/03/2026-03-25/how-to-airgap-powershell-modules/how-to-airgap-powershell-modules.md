---
title: How to Air-Gap PowerShell Modules in Source Control
created: 2026-03-25T00:00:00
modified: 2026-03-25T00:00:00
tags:
  - powershell
  - modules
  - air-gap
  - source-control
  - pester
  - dbatools
  - team
---

# 🤖❓ How to Air-Gap PowerShell Modules in Source Control

## 🧭 Overview

Air-gapping PowerShell modules means bundling the module files directly into your repository so that:

- No internet connection is required at runtime
- Every team member uses the **exact same version** of every module
- CI/CD pipelines work in locked-down environments without PSGallery access
- Module updates are a deliberate, reviewable change in your git history

This guide covers **two complementary strategies**, both cross-platform (PowerShell 7+):

| Strategy | Best for |
|---|---|
| **Path-based vendor directory** | Simple scripts, small/medium modules, minimal setup |
| **Local PSRepository** | Larger teams, `Install-Module`-style workflows, many modules |

> **Note on dbatools**: dbatools is ~100–150 MB compressed and several hundred MB unzipped. Read the [Git LFS section](#-git-lfs-essential-for-large-modules) before committing it to git.

---

## 📁 Recommended Repository Structure

```
repo-root/
├── .gitattributes              # Git LFS tracking rules (required for dbatools)
├── .gitignore
├── bootstrap.ps1               # One-time team onboarding script
├── psmodules/
│   ├── vendor/                 # Strategy 1: unzipped modules, ready to import
│   │   ├── Pester/
│   │   │   └── 5.7.4/          # Always include the version folder
│   │   │       ├── Pester.psd1
│   │   │       └── ...
│   │   └── dbatools/
│   │       └── 2.1.x/
│   │           ├── dbatools.psd1
│   │           └── ...
│   ├── packages/               # Strategy 2: raw .nupkg files for local PSRepository
│   │   ├── Pester.5.7.4.nupkg
│   │   ├── dbatools.2.1.x.nupkg
│   │   └── <dependency>.nupkg  # All transitive deps go here too
│   └── local-repo/             # Strategy 2: registered PSRepository root (may be same as packages/)
└── scripts/
    └── sync-modules.ps1        # Maintainer script: refresh modules from PSGallery
```

---

## 🔽 Step 1 — Download Modules from PSGallery (Internet Required, One-Time)

This step is done by whoever is **maintaining** the air-gapped modules. Everyone else uses the bootstrap script.

### 1a. Install the modern PowerShell package manager (if not already present)

```powershell
# PSResourceGet is the modern replacement for PowerShellGet — use it for PS7+
Install-Module -Name Microsoft.PowerShell.PSResourceGet -Force -Scope CurrentUser

# Verify
Get-PSResourceRepository
```

> **PowerShell 5.1 compatibility**: If you also target Windows PowerShell 5.1, stick with
> `PowerShellGet` v2 (`Save-Module`) — covered in the [v2 compatibility section](#-powershell-51--powershellget-v2-compatibility).

### 1b. Save modules with all dependencies

`Save-PSResource` downloads the `.nupkg` files. The `-IncludeXml` flag pulls down the XML
metadata that PSResourceGet needs to resolve dependencies later.

```powershell
$packagesDir = "./psmodules/packages"
New-Item -ItemType Directory -Force -Path $packagesDir | Out-Null

# Pester — no significant external dependencies
Save-PSResource -Name Pester `
                -Path $packagesDir `
                -IncludeXml `
                -Repository PSGallery

# dbatools — large, but Save-PSResource handles transitive deps automatically
Save-PSResource -Name dbatools `
                -Path $packagesDir `
                -IncludeXml `
                -Repository PSGallery
```

After this runs, `$packagesDir` will contain `.nupkg` files for every module and
every dependency that was pulled down.

### 1c. List what was downloaded (verify dependencies)

```powershell
Get-ChildItem ./psmodules/packages/*.nupkg |
    Select-Object Name, @{n='SizeMB'; e={[math]::Round($_.Length/1MB,1)}} |
    Sort-Object SizeMB -Descending
```

You will typically see dbatools pulling in several companion modules such as
`dbatools-core`, `dbatools-library`, etc. All of these need to be committed.

---

## 📦 Step 2a — Strategy 1: Vendor Directory (Path-Based Import)

Unzip the `.nupkg` files into the `vendor/` directory. `.nupkg` files are just renamed ZIP archives.

```powershell
$packagesDir = "./psmodules/packages"
$vendorDir   = "./psmodules/vendor"

Get-ChildItem $packagesDir -Filter "*.nupkg" | ForEach-Object {
    $nupkg   = $_
    # Parse name and version from filename (e.g. Pester.5.7.4.nupkg)
    if ($nupkg.BaseName -match '^(?<name>.+?)\.(?<ver>\d+\.\d+[\.\d]*)$') {
        $modName = $Matches['name']
        $modVer  = $Matches['ver']
        $dest    = Join-Path $vendorDir "$modName/$modVer"
        New-Item -ItemType Directory -Force -Path $dest | Out-Null
        Expand-Archive -Path $nupkg.FullName -DestinationPath $dest -Force
        Write-Host "Extracted $modName $modVer → $dest"
    }
}
```

> **Why keep the version folder?** PowerShell's module loader understands
> `ModuleName/Version/ModuleName.psd1` paths. Keeping the version folder lets you
> vendor multiple versions side-by-side and makes the required version explicit.

### Importing from the vendor directory in a script

```powershell
# At the top of any script that needs the air-gapped modules:

$repoRoot   = $PSScriptRoot  # or however you resolve repo root
$vendorPath = Join-Path $repoRoot "psmodules/vendor"

# Prepend vendor path so it takes precedence over any system-installed versions
if ($env:PSModulePath -notlike "*$vendorPath*") {
    $env:PSModulePath = "$vendorPath$([System.IO.Path]::PathSeparator)$env:PSModulePath"
}

# Now Import-Module resolves from vendor/
Import-Module Pester    -ErrorAction Stop
Import-Module dbatools  -ErrorAction Stop
```

### Or: use explicit versioned import (safest for teams)

```powershell
$repoRoot = $PSScriptRoot

Import-Module (Join-Path $repoRoot "psmodules/vendor/Pester/5.7.4/Pester.psd1") `
    -ErrorAction Stop

Import-Module (Join-Path $repoRoot "psmodules/vendor/dbatools/2.1.x/dbatools.psd1") `
    -ErrorAction Stop
```

This form is completely unambiguous — it does not depend on `$env:PSModulePath` at all.

---

## 🏛️ Step 2b — Strategy 2: Local PSRepository

A local PSRepository makes the air-gapped modules feel identical to PSGallery — team
members can run `Install-PSResource -Repository LocalModules -Name Pester` without ever
touching the internet.

### Register the local repository

This only needs to be run **once per machine** (or in `bootstrap.ps1`):

```powershell
$localRepoPath = Resolve-Path "./psmodules/packages"

Register-PSResourceRepository `
    -Name        "LocalModules" `
    -Uri         $localRepoPath `
    -Trusted     `
    -Priority    10    # lower number = searched first

# Verify
Get-PSResourceRepository
```

> The `packages/` directory (containing your `.nupkg` files) **is** the repository — no
> additional tooling required. PSResourceGet reads nupkgs directly from a filesystem path.

### Install from the local repository

```powershell
# Install for current user (no admin required, cross-platform)
Install-PSResource -Name Pester   -Repository LocalModules -Scope CurrentUser
Install-PSResource -Name dbatools -Repository LocalModules -Scope CurrentUser
```

### Unregister PSGallery to enforce air-gap during CI

```powershell
# In CI pipelines: block any accidental PSGallery calls
Unregister-PSResourceRepository -Name PSGallery -ErrorAction SilentlyContinue
```

---

## 🧳 Git LFS — Essential for Large Modules

dbatools nupkg files exceed GitHub's 100 MB file size limit and will bloat your
repository history if committed as regular objects.

### Set up Git LFS

```bash
# One-time per machine
git lfs install

# Check LFS is active in this repo
git lfs status
```

### Configure `.gitattributes`

Add this to `.gitattributes` at the repo root (commit this file):

```gitattributes
# PowerShell module packages — track with LFS
psmodules/packages/*.nupkg   filter=lfs diff=lfs merge=lfs -text

# Unzipped native binaries inside vendor modules
psmodules/vendor/**/*.dll    filter=lfs diff=lfs merge=lfs -text
psmodules/vendor/**/*.so     filter=lfs diff=lfs merge=lfs -text
psmodules/vendor/**/*.dylib  filter=lfs diff=lfs merge=lfs -text
```

> If your Git host does not support LFS (self-hosted Gitea, etc.), consider committing
> only the `.nupkg` files (much smaller than unzipped) and unzipping them in
> `bootstrap.ps1` on first use — see the bootstrap script below.

### Verify LFS is tracking correctly after adding `.gitattributes`

```bash
git add .gitattributes
git add psmodules/
git lfs status   # should list .nupkg and .dll files as LFS objects
git commit -m "chore: add air-gapped PowerShell modules (Pester, dbatools)"
```

---

## 🚀 Bootstrap Script for New Team Members

`bootstrap.ps1` at the repo root gives every team member a one-liner setup:

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    Sets up air-gapped PowerShell modules for this repository.
    Run once after cloning: pwsh ./bootstrap.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot    = $PSScriptRoot
$packagesDir = Join-Path $repoRoot "psmodules/packages"
$vendorDir   = Join-Path $repoRoot "psmodules/vendor"

#region ── Unzip vendor modules if not already present ──────────────────────────
Write-Host "⚙️  Expanding vendor modules..." -ForegroundColor Cyan

Get-ChildItem $packagesDir -Filter "*.nupkg" | ForEach-Object {
    $nupkg = $_
    if ($nupkg.BaseName -match '^(?<name>.+?)\.(?<ver>\d+\.\d+[\.\d]*)$') {
        $modName = $Matches['name']
        $modVer  = $Matches['ver']
        $dest    = Join-Path $vendorDir "$modName/$modVer"
        if (-not (Test-Path $dest)) {
            New-Item -ItemType Directory -Force -Path $dest | Out-Null
            Expand-Archive -Path $nupkg.FullName -DestinationPath $dest -Force
            Write-Host "  ✅ $modName $modVer" -ForegroundColor Green
        } else {
            Write-Host "  ⏭️  $modName $modVer (already extracted)" -ForegroundColor DarkGray
        }
    }
}
#endregion

#region ── Register local PSRepository ─────────────────────────────────────────
Write-Host "📦 Registering local PSRepository..." -ForegroundColor Cyan

# Install PSResourceGet if not present
if (-not (Get-Module -ListAvailable -Name Microsoft.PowerShell.PSResourceGet)) {
    Write-Host "  Installing PSResourceGet from vendor..." -ForegroundColor Yellow
    # Assumes PSResourceGet is also in packages/ — add it with Save-PSResource if needed
    Install-Module -Name Microsoft.PowerShell.PSResourceGet -Force -Scope CurrentUser
}

$existingRepo = Get-PSResourceRepository -Name "LocalModules" -ErrorAction SilentlyContinue
if (-not $existingRepo) {
    Register-PSResourceRepository `
        -Name     "LocalModules" `
        -Uri      $packagesDir `
        -Trusted  `
        -Priority 10
    Write-Host "  ✅ LocalModules repository registered" -ForegroundColor Green
} else {
    Write-Host "  ⏭️  LocalModules already registered" -ForegroundColor DarkGray
}
#endregion

#region ── Summary ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "✅ Bootstrap complete. You can now:" -ForegroundColor Green
Write-Host "   Import-Module (Join-Path '$repoRoot' 'psmodules/vendor/Pester/5.x.x/Pester.psd1')"
Write-Host "   Install-PSResource -Name Pester -Repository LocalModules -Scope CurrentUser"
#endregion
```

Team members onboard with:

```bash
git clone <repo-url>
cd <repo>
pwsh ./bootstrap.ps1
```

---

## 🔄 Updating Modules (Maintainer Workflow)

Create `scripts/sync-modules.ps1` for the module maintainer to run when updating versions:

```powershell
#!/usr/bin/env pwsh
#Requires -Version 7.0
<#
.SYNOPSIS
    Re-downloads latest (or pinned) module versions from PSGallery.
    Run with internet access. Commit the result.

.PARAMETER ModuleNames
    Modules to update. Defaults to all air-gapped modules.

.PARAMETER PinVersions
    Hashtable of name→version to pin. E.g. @{ Pester = '5.7.4' }
#>
param(
    [string[]] $ModuleNames  = @('Pester', 'dbatools'),
    [hashtable] $PinVersions = @{}
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$packagesDir = Join-Path $PSScriptRoot "../psmodules/packages"
New-Item -ItemType Directory -Force -Path $packagesDir | Out-Null

foreach ($name in $ModuleNames) {
    $params = @{
        Name        = $name
        Path        = $packagesDir
        IncludeXml  = $true
        Repository  = 'PSGallery'
    }
    if ($PinVersions.ContainsKey($name)) {
        $params['Version'] = $PinVersions[$name]
    }

    Write-Host "⬇️  Downloading $name..." -ForegroundColor Cyan
    Save-PSResource @params
    Write-Host "  ✅ Done" -ForegroundColor Green
}

Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "  1. Run: git diff --stat psmodules/"
Write-Host "  2. Delete psmodules/vendor/ and re-run bootstrap.ps1 to test"
Write-Host "  3. Commit: git add psmodules/ && git commit -m 'chore: update PowerShell modules'"
```

---

## 🪟 PowerShell 5.1 / PowerShellGet v2 Compatibility

If you need to support **Windows PowerShell 5.1** (which ships with PowerShellGet v2, not PSResourceGet):

```powershell
# Save-Module is the v2 equivalent of Save-PSResource
# It downloads to a directory as unzipped module folders (not nupkgs)

Save-Module -Name Pester `
            -Path ./psmodules/vendor `
            -Repository PSGallery `
            -Force

Save-Module -Name dbatools `
            -Path ./psmodules/vendor `
            -Repository PSGallery `
            -Force
```

For the local repository approach under v2:

```powershell
# v2 PSRepository needs a UNC or file path
Register-PSRepository `
    -Name            "LocalModules" `
    -SourceLocation  (Resolve-Path ./psmodules/packages).Path `
    -InstallationPolicy Trusted

Install-Module -Name Pester -Repository LocalModules -Scope CurrentUser
```

> **Tip**: Maintain two separate package directories if you must support both
> `psmodules/packages-v3/` (nupkgs for PSResourceGet) and
> `psmodules/vendor/` (unzipped for v2 / direct import). The vendor directory works
> for **both** — it's the safest common ground.

---

## 🍎🐧 Cross-Platform Path Handling

Never hardcode path separators. Use PowerShell's built-in cross-platform helpers:

```powershell
# ✅ Cross-platform
$vendorDir = Join-Path $PSScriptRoot "psmodules" "vendor"
$sep       = [System.IO.Path]::PathSeparator   # ':' on macOS/Linux, ';' on Windows

# ✅ Prepend to PSModulePath cross-platform
$env:PSModulePath = "$vendorDir$sep$env:PSModulePath"

# ❌ Avoid
$env:PSModulePath = "$vendorDir;$env:PSModulePath"  # breaks on macOS/Linux
```

---

## ⚠️ Known Gotchas

### dbatools

- **Size**: Expect 100–150 MB of `.nupkg` files. Git LFS is not optional.
- **Native library**: `dbatools-library` contains a .NET native assembly. On first import
  it may try to reach the internet for a component. Pin `dbatools-library` in your
  `sync-modules.ps1` to the same version as `dbatools` to avoid mismatches.
- **Module manifest dependency**: dbatools declares `dbatools-library` as a required
  module. Ensure `dbatools-library` is in the same `vendor/` directory alongside `dbatools`
  or it will fail to import even with the full vendor path set.

### Pester

- Pester v5 ships a single module folder with no external dependencies — it is the
  simplest module to air-gap. Save it, commit it, done.
- Pester overwrites itself aggressively on Windows if `Update-Module` is run globally.
  Using the explicit `.psd1` path import form completely prevents this.

### Execution Policy (Windows)

On Windows, scripts extracted from zip/nupkg files are sometimes marked as coming from
the internet (Zone.Identifier alternate data stream). Unblock them after extracting:

```powershell
Get-ChildItem ./psmodules/vendor -Recurse -Include "*.ps1","*.psm1","*.psd1" |
    Unblock-File
```

This is not needed on macOS or Linux.

### `.nupkg` contains nuspec metadata files

When using the vendor/path-based approach, the extracted `.nupkg` will contain
`*.nuspec` and `[Content_Types].xml` files alongside the module. PowerShell ignores
these, but you can clean them up if preferred:

```powershell
Get-ChildItem ./psmodules/vendor -Recurse -Include "*.nuspec","[Content_Types].xml","_rels" |
    Remove-Item -Force -Recurse
```

---

## 📋 Quick-Reference Cheat Sheet

```powershell
# ── MAINTAINER: download modules with deps ─────────────────────────────────
Save-PSResource -Name Pester   -Path ./psmodules/packages -IncludeXml -Repository PSGallery
Save-PSResource -Name dbatools -Path ./psmodules/packages -IncludeXml -Repository PSGallery

# ── MAINTAINER: list what was pulled down ──────────────────────────────────
Get-ChildItem ./psmodules/packages/*.nupkg | Select Name, @{n='MB';e={[math]::Round($_.Length/1MB,1)}}

# ── TEAM MEMBER: onboard ───────────────────────────────────────────────────
pwsh ./bootstrap.ps1

# ── ANY SCRIPT: use vendor modules ────────────────────────────────────────
Import-Module (Join-Path $PSScriptRoot "psmodules/vendor/Pester/5.7.4/Pester.psd1")
Import-Module (Join-Path $PSScriptRoot "psmodules/vendor/dbatools/2.1.x/dbatools.psd1")

# ── TEAM MEMBER: install via local repo ───────────────────────────────────
Install-PSResource -Name Pester   -Repository LocalModules -Scope CurrentUser
Install-PSResource -Name dbatools -Repository LocalModules -Scope CurrentUser

# ── CI: lock down to local repo only ──────────────────────────────────────
Unregister-PSResourceRepository -Name PSGallery -ErrorAction SilentlyContinue
Install-PSResource -Name Pester -Repository LocalModules -Scope CurrentUser
```
