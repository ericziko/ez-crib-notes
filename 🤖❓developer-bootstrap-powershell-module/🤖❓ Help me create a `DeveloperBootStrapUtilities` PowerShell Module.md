---
title: 🤖❓ Help me create a DeveloperBootStrapUtilities PowerShell Module
created: 2026-02-26
tags:
  - powershell
  - developer-experience
  - onboarding
  - spectre-console
  - git
uid: 80887118-5a2f-40f6-8ee1-f52667be96fc
---

# 🤖❓ Help me create a `DeveloperBootStrapUtilities` PowerShell Module

## Summary / Prompt

- I'm a C# developer working on a team of others in an Enterprise environment
- I would like you to help me implement a `DeveloperBootStrapUtilities` PowerShell Module
  - This module is to be used to encapsulate the automation of common new developer setup tasks that amount to a lot of toil in our environments
  - The first task I would like you to help me setup is the automation of pulling down all the git repositories that a developer needs to get started locally
    - Currently, the developer goes on to Confluence and cuts and pastes a bunch of GitHub URLs into their terminal manually
    - This can be a very labor-intensive process for something so simple

Using the following utilities:
- PowerShell
- [PwshSpectreConsole](https://github.com/ShaunLawrie/PwshSpectreConsole) — a PowerShell wrapper for Spectre.Console

Design a PowerShell module with an `Initialize-GitHubRepositories` function that:
- Given a list of GitHub repo endpoints, lets the user choose from an attractive multi-select list
- Stores the repo list in a JSON file adjacent to the module (version-controlled)

---

## 🤖💡 Solution

### 📁 Module Structure

```
DeveloperBootStrapUtilities/
├── DeveloperBootStrapUtilities.psd1   ← module manifest
├── DeveloperBootStrapUtilities.psm1   ← module implementation
└── repositories.json                  ← team repo config (version-controlled)
```

All three files live together in source control. The JSON config is resolved **relative to the module file** at runtime using `$PSScriptRoot`, so there's no hardcoded path.

---

### 📦 Prerequisites

Install PwshSpectreConsole (once, per developer machine):

```powershell
Install-Module PwshSpectreConsole -Scope CurrentUser
```

---

### 🚀 Usage

```powershell
# Import the module (or add to your $PROFILE)
Import-Module .\DeveloperBootStrapUtilities

# Interactive — shows the Spectre.Console multi-select picker
Initialize-GitHubRepositories

# Override where repos are cloned (ignores defaultClonePath in JSON)
Initialize-GitHubRepositories -ClonePath "D:\Projects"

# Non-interactive — clone everything (for scripts / CI)
Initialize-GitHubRepositories -SelectAll -ClonePath "$env:USERPROFILE\src"

# Use a different config file (e.g. a team-specific one)
Initialize-GitHubRepositories -ConfigPath ".\my-team-repos.json"
```

---

### 🔧 `repositories.json` — Config File

Edit this to reflect your team's actual repos. `defaultClonePath` supports environment variable expansion (e.g. `%USERPROFILE%`).

```json
{
  "defaultClonePath": "C:\\Dev",
  "repositories": [
    {
      "name": "MyOrg.API",
      "url": "https://github.com/myorg/myorg-api",
      "description": "Core REST API — the main backend service"
    },
    {
      "name": "MyOrg.Worker",
      "url": "https://github.com/myorg/myorg-worker",
      "description": "Background job processor (Hangfire/Temporal)"
    },
    {
      "name": "MyOrg.Frontend",
      "url": "https://github.com/myorg/myorg-frontend",
      "description": "React frontend web application"
    }
  ]
}
```

> **Note:** Authentication is delegated to whatever Git credential helper is already configured on the machine (Windows Credential Manager, Git Credential Manager, SSH keys, etc.). The module doesn't manage credentials.

---

### 🎨 User Experience Flow

```
 Developer Bootstrap — Repository Setup ─────────────────────────────────

 Config : C:\tools\DeveloperBootStrapUtilities\repositories.json
 Target : C:\Dev

 Select the repositories to clone  (Space to toggle, Enter to confirm)

 ❯ ○  MyOrg.API                         — Core REST API — the main backend service
   ● MyOrg.Worker                       — Background job processor (Hangfire/Temporal)
   ● MyOrg.Frontend                     — React frontend web application
   ○  MyOrg.SharedLibraries             — Shared NuGet packages and internal utilities

 Cloning 2 repositories into C:\Dev ...

   ✓  MyOrg.Worker
   ✓  MyOrg.Frontend

 Results ─────────────────────────────────────────────────────────────────

 ╭──────────────────┬────────────┬───────────────────────╮
 │ Repository       │ Status     │ Detail                │
 ├──────────────────┼────────────┼───────────────────────┤
 │ MyOrg.Worker     │ ✓ Cloned   │ C:\Dev\MyOrg.Worker   │
 │ MyOrg.Frontend   │ ✓ Cloned   │ C:\Dev\MyOrg.Frontend │
 ╰──────────────────┴────────────┴───────────────────────╯

 Done.  2 cloned  |  0 skipped  |  0 failed
```

---

### 🤖💡 Key Design Decisions

| Decision | Rationale |
|---|---|
| JSON config adjacent to module | Version-controlled alongside code; no separate config system needed |
| `$PSScriptRoot` for config path | Works regardless of the caller's working directory |
| `$env:VAR` expansion in clone path | Supports `%USERPROFILE%\src` style paths in the JSON |
| Auth delegated to Git | Avoids reinventing credential management; GCM handles it transparently |
| `continue` on already-existing dirs | Idempotent — safe to re-run without clobbering work-in-progress |
| `-SelectAll` switch | Makes the module usable in bootstrap scripts without interactive input |
| Flat sorted list | Simpler UX for small-to-medium repo lists; grouping can be added later |

---

### 🔮 Future Enhancements

- **Grouping** — Add a `"group"` property to each repo in the JSON and render sections in the picker
- **Post-clone hooks** — Optional `dotnet restore`, `npm install`, or `code .` per repo
- **`Update-GitHubRepositories`** — A companion function that `git pull` on already-cloned repos
- **Progress bars** — Use `Invoke-SpectreCommandWithProgress` for a live progress bar during cloning
- **Validation** — Verify Git is configured (user.name, user.email) before cloning

---

### 📎 Files

See the `DeveloperBootStrapUtilities/` folder alongside this note for the full implementation.
