# 🤖❓ Ergonomic `dotnet run` for Multi-Project Solutions

> **Problem:** You have several console apps 4+ levels deep in a solution and want to make running them discoverable and ergonomic for the whole team — from the solution root.

---

## 📋 Table of Contents

1. [The Problem Framed](#the-problem-framed)
2. [Option 1: Raw `dotnet run --project`](#option-1-raw-dotnet-run---project)
3. [Option 2: PowerShell Aliases in `$PROFILE`](#option-2-powershell-aliases-in-profile)
4. [Option 3: Simple Script Files](#option-3-simple-script-files)
5. [Option 4: LaunchSettings.json Profiles](#option-4-launchsettingsjson-profiles)
6. [Option 5: PowerShell Module (Recommended)](#option-5-powershell-module-recommended)
7. [The `Setup-Dev.ps1` Loader Pattern](#the-setup-devps1-loader-pattern)
8. [Module Reference](#module-reference)

---

## 🎯 The Problem Framed

A typical solution layout:

```
MySolution/
├── MySolution.sln
├── Setup-Dev.ps1              ← team entry point
├── src/
│   ├── Services/
│   │   └── Worker/
│   │       └── MyWorker/
│   │           └── MyWorker.csproj
│   └── Tools/
│       └── Migration/
│           └── DbMigrator/
│               └── DbMigrator.csproj
└── tests/
```

Without tooling, a developer must know:
```powershell
dotnet run --project src/Services/Worker/MyWorker/MyWorker.csproj --launch-profile Development
```

That's not discoverable. Let's fix it.

---

## 🔧 Option 1: Raw `dotnet run --project`

The baseline. Works everywhere, no setup required.

```powershell
# From solution root
dotnet run --project src/Tools/Migration/DbMigrator/DbMigrator.csproj

# With a launch profile (from launchSettings.json)
dotnet run --project src/Tools/Migration/DbMigrator/DbMigrator.csproj --launch-profile Staging

# With environment variables overridden
dotnet run --project src/Workers/MyWorker/MyWorker.csproj `
    --launch-profile Development `
    -- --SomeArg value
```

**Pros:** No setup, works in CI  
**Cons:** Hard to remember paths, not discoverable, error-prone

---

## 🔧 Option 2: PowerShell Aliases in `$PROFILE`

Add to `~/.config/powershell/Microsoft.PowerShell_profile.ps1` (or `$PROFILE`):

```powershell
function Run-Worker   { dotnet run --project src/Workers/MyWorker/MyWorker.csproj @args }
function Run-Migrator { dotnet run --project src/Tools/Migration/DbMigrator/DbMigrator.csproj @args }
```

**Pros:** Simple, fast  
**Cons:** Machine-specific, not checked in, teammates must configure individually

---

## 🔧 Option 3: Simple Script Files

Check in small `.ps1` scripts at the solution root:

```powershell
# run-worker.ps1
param(
    [string]$Profile = "Development"
)
dotnet run --project src/Workers/MyWorker/MyWorker.csproj --launch-profile $Profile
```

```powershell
# run-migrator.ps1
param(
    [string]$Profile = "Development",
    [switch]$DryRun
)
$env:DRY_RUN = if ($DryRun) { "true" } else { "false" }
dotnet run --project src/Tools/Migration/DbMigrator/DbMigrator.csproj --launch-profile $Profile
```

**Pros:** Checked in, no module needed, dead simple  
**Cons:** One file per app, no discoverability, duplicated logic

---

## 🔧 Option 4: LaunchSettings.json Profiles

Each project has `Properties/launchSettings.json`. Use named profiles for different environments:

```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Development",
        "ConnectionStrings__Default": "Server=localhost;Database=MyDb_Dev;..."
      }
    },
    "Staging": {
      "commandName": "Project",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Staging"
      }
    },
    "DockerLocal": {
      "commandName": "Project",
      "environmentVariables": {
        "DOTNET_ENVIRONMENT": "Development",
        "ConnectionStrings__Default": "Server=db;Database=MyDb;..."
      }
    }
  }
}
```

Then run with:
```powershell
dotnet run --launch-profile DockerLocal
```

**Pros:** Built into .NET tooling, works in VS/Rider too, checked in  
**Cons:** Still need to navigate to the project directory or use `--project`

---

## 🏆 Option 5: PowerShell Module (Recommended)

A module checked into the repo gives you:
- **Discoverability** — `Get-Command -Module DevRunner`
- **Tab completion** — project names, launch profiles
- **Consistent interface** — one pattern for all apps
- **Team-wide** — `Setup-Dev.ps1` imports it for everyone

### Module Structure

```
DevRunner/
├── DevRunner.psd1          ← Module manifest
├── DevRunner.psm1          ← Module loader
├── Public/                 ← Exported (usable) functions
│   ├── Get-DotnetProjects.ps1
│   ├── Invoke-DotnetRun.ps1
│   ├── Show-RunConfigurations.ps1
│   └── Start-DevApp.ps1
└── Private/                ← Internal helpers
    ├── Find-ProjectFile.ps1
    ├── Get-LaunchSettings.ps1
    └── Resolve-SolutionRoot.ps1
```

### Usage After Import

```powershell
# Import from Setup-Dev.ps1 (or manually)
Import-Module ./DevRunner/DevRunner.psd1

# Discover what's available
Get-DotnetProjects

# See run configurations for a project
Show-RunConfigurations -ProjectName MyWorker

# Run a project (resolves path automatically)
Invoke-DotnetRun -ProjectName MyWorker -Profile Development

# Short alias style
Start-DevApp worker          # Fuzzy-matches project name
Start-DevApp migrator -Profile Staging
```

---

## 📂 The `Setup-Dev.ps1` Loader Pattern

Create this at the **solution root**, checked into source control:

```powershell
# Setup-Dev.ps1
# Run this once per shell session: . ./Setup-Dev.ps1
# (dot-source to import into current scope)

$ErrorActionPreference = 'Stop'

Write-Host "🚀 Loading dev environment..." -ForegroundColor Cyan

# Load the DevRunner module from repo
$moduleRoot = Join-Path $PSScriptRoot "DevRunner"
if (-not (Test-Path "$moduleRoot/DevRunner.psd1")) {
    throw "DevRunner module not found at $moduleRoot. Are you at the solution root?"
}

Import-Module "$moduleRoot/DevRunner.psd1" -Force

# Print available projects
Write-Host ""
Write-Host "Available projects:" -ForegroundColor Green
Get-DotnetProjects | ForEach-Object {
    Write-Host "  • $($_.Name)" -ForegroundColor White
}

Write-Host ""
Write-Host "Usage:" -ForegroundColor Green
Write-Host "  Invoke-DotnetRun -ProjectName <name> [-Profile <profile>]" -ForegroundColor White
Write-Host "  Show-RunConfigurations -ProjectName <name>" -ForegroundColor White
Write-Host "  Get-DotnetProjects" -ForegroundColor White
```

**Team workflow:**
```powershell
# Clone the repo, then:
cd MySolution
. ./Setup-Dev.ps1         # dot-source once per shell session

# Now run any project by name:
Invoke-DotnetRun -ProjectName MyWorker
```

---

## 📖 Module Reference

### `Get-DotnetProjects`

Lists all .csproj files found under the solution root.

```powershell
Get-DotnetProjects
Get-DotnetProjects -SolutionRoot C:\MySolution
```

### `Show-RunConfigurations`

Shows launch profiles defined in a project's `launchSettings.json`.

```powershell
Show-RunConfigurations -ProjectName MyWorker
Show-RunConfigurations -ProjectName DbMigrator
```

### `Invoke-DotnetRun`

Runs a named project, optionally with a launch profile and extra args.

```powershell
Invoke-DotnetRun -ProjectName MyWorker
Invoke-DotnetRun -ProjectName MyWorker -Profile Staging
Invoke-DotnetRun -ProjectName MyWorker -Profile Development -AdditionalArgs "--verbose"
```

### `Start-DevApp`

Convenience wrapper with fuzzy project name matching.

```powershell
Start-DevApp worker           # matches MyWorker, MyBackgroundWorker, etc.
Start-DevApp migrator -Profile Staging
```

---

## 💡 Key Takeaways

| Approach | Discoverability | Checked-in | No Setup | Recommended |
|---|---|---|---|---|
| Raw `dotnet run` | ❌ | N/A | ✅ | For CI only |
| `$PROFILE` aliases | ❌ | ❌ | ✅ | No |
| Script files | ⚠️ | ✅ | ✅ | Simple repos |
| launchSettings.json | ✅ | ✅ | ✅ | Always (combine) |
| PowerShell module | ✅✅ | ✅ | ❌ (one-time) | **Yes** |

**Recommended combination:**
1. Define `launchSettings.json` profiles per project (works in all IDEs)
2. Build the `DevRunner` module for ergonomic CLI use
3. `Setup-Dev.ps1` at solution root — one dot-source and the team is productive
