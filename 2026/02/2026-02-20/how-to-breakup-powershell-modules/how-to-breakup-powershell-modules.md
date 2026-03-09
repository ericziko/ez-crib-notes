---
uid: de4e386e-a211-43d4-96f5-233cb33ba8a2
title: how-to-breakup-powershell-modules
created: 2026-02-20T12:38:38
modified: 2026-02-20T12:40:09
---

# how-to-breakup-powershell-modules

## Prompt
- I have a PowerShell module with a whole bunch of PowerShell functions in it, and I would like to break them up into individual script files and load them dynamically when the module loads.
- I would like to keep them in a subfolder relative to the module's location and load them from that dynamically.
- How do I do this and what are the common conventions around doing this?
- Please write me a tutorial/cheatsheet/tips markdown file explaining this.

---

## Tutorial: Breaking Up a PowerShell Module into Individual Script Files

### Why Do This?

- Easier to navigate and maintain — one file per function
- Cleaner git diffs (changes isolated to one file)
- Easier to reuse individual functions across modules
- Supports team workflows where different people own different functions

---

### Conventional Folder Structure

```
MyModule/
├── MyModule.psd1          ← Module manifest (optional but recommended)
├── MyModule.psm1          ← Module loader (the only "logic" here is loading)
└── Functions/
    ├── Public/            ← Functions exported to consumers
    │   ├── Get-Widget.ps1
    │   ├── Set-Widget.ps1
    │   └── Remove-Widget.ps1
    └── Private/           ← Internal helpers, NOT exported
        ├── Invoke-InternalHelper.ps1
        └── ConvertTo-InternalFormat.ps1
```

**Key conventions:**
- One function per file
- File named exactly after the function it contains: `Get-Widget.ps1` contains `function Get-Widget`
- `Public/` = functions your module exposes; `Private/` = implementation details
- The `.psm1` file contains **only** the loading logic — no actual functions

---

### The .psm1 Loader Pattern

This is the heart of the pattern. Your `MyModule.psm1` dot-sources every `.ps1` file:

```powershell
# MyModule.psm1

# Load private functions first (public functions may depend on them)
$privatePath = Join-Path $PSScriptRoot 'Functions\Private'
if (Test-Path $privatePath) {
    Get-ChildItem -Path $privatePath -Filter '*.ps1' -Recurse | ForEach-Object {
        . $_.FullName
    }
}

# Load public functions
$publicPath = Join-Path $PSScriptRoot 'Functions\Public'
$publicFunctions = @()
if (Test-Path $publicPath) {
    Get-ChildItem -Path $publicPath -Filter '*.ps1' -Recurse | ForEach-Object {
        . $_.FullName
        $publicFunctions += $_.BaseName   # BaseName = filename without extension = function name
    }
}

# Export only the public functions
Export-ModuleMember -Function $publicFunctions
```

> [!TIP]
> `$PSScriptRoot` always resolves to the directory containing the `.psm1` file — use it instead of relative paths like `.\` which can break depending on the caller's working directory.

> [!NOTE]
> The dot-sourcing operator `. $_.FullName` (note the space after the dot) runs the script in the **current scope**, making the functions available. Without the space it would be a property accessor — a common mistake.

---

### Individual Function File Structure

Each `.ps1` file should contain exactly one function:

```powershell
# Functions/Public/Get-Widget.ps1

function Get-Widget {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    # implementation here
}
```

---

### Module Manifest (.psd1) — Optional but Recommended

The manifest describes the module and controls what gets exported:

```powershell
# Generate a starter manifest
New-ModuleManifest -Path .\MyModule.psd1 `
    -RootModule 'MyModule.psm1' `
    -ModuleVersion '1.0.0' `
    -Author 'Your Name' `
    -Description 'What this module does'
```

With this pattern, leave `FunctionsToExport = @('*')` in the `.psd1` and let the `.psm1` handle the actual export control via `Export-ModuleMember`. Or list them explicitly in the manifest for slightly faster import times on large modules.

---

### Loading / Reloading During Development

```powershell
# Load the module
Import-Module .\MyModule.psm1

# Force reload after making changes (crucial during development)
Import-Module .\MyModule.psm1 -Force

# Check what got exported
Get-Command -Module MyModule

# Remove without reloading
Remove-Module MyModule
```

> [!TIP]
> Add `-Verbose` to `Import-Module` to see each file being dot-sourced — useful for debugging load order issues.

---

### Cross-Platform Path Note (Windows vs macOS/Linux)

Use `Join-Path` rather than hardcoding `\` or `/` separators — PowerShell handles it correctly on all platforms:

```powershell
# Good — works everywhere
$publicPath = Join-Path $PSScriptRoot 'Functions' 'Public'

# Fragile — breaks on macOS/Linux
$publicPath = "$PSScriptRoot\Functions\Public"
```

---

### Migration Cheatsheet: Splitting an Existing Module

1. Create the folder structure: `Functions/Public/` and `Functions/Private/`
2. For each function in your `.psm1`:
   - Decide: public (exported) or private (internal)?
   - Cut the function body
   - Paste into a new `.ps1` file named after the function
3. Replace all function code in `.psm1` with the loader pattern above
4. Run `Import-Module .\MyModule.psm1 -Force -Verbose` and verify all functions load
5. Run `Get-Command -Module MyModule` to confirm only public functions are exported

---

### Common Mistakes

| Mistake | Fix |
|---|---|
| `. $file` (no space after dot) | Use `. $file.FullName` (space is required for dot-sourcing) |
| Using `.\` instead of `$PSScriptRoot` | Always use `$PSScriptRoot` in module files |
| Loading public before private | Load `Private/` first — public functions may call private helpers |
| Forgetting `-Force` when reloading | `Import-Module -Force` to pick up changes |
| File name doesn't match function name | Convention: `Get-Widget.ps1` contains `function Get-Widget` |
| Exporting private functions | Only dot-source private files; only add public `BaseName`s to `Export-ModuleMember` |

---

### Minimal Working Example

```
MyModule/
├── MyModule.psm1
└── Functions/
    ├── Public/
    │   └── Say-Hello.ps1
    └── Private/
        └── Format-Greeting.ps1
```

```powershell
# Functions/Private/Format-Greeting.ps1
function Format-Greeting { param([string]$Name) "Hello, $Name!" }
```

```powershell
# Functions/Public/Say-Hello.ps1
function Say-Hello { param([string]$Name) Write-Output (Format-Greeting -Name $Name) }
```

```powershell
# MyModule.psm1
foreach ($folder in @('Private','Public')) {
    $path = Join-Path $PSScriptRoot 'Functions' $folder
    if (Test-Path $path) {
        Get-ChildItem -Path $path -Filter '*.ps1' | ForEach-Object { . $_.FullName }
    }
}
Export-ModuleMember -Function (
    Get-ChildItem (Join-Path $PSScriptRoot 'Functions\Public') -Filter '*.ps1' |
    Select-Object -ExpandProperty BaseName
)
```

```powershell
Import-Module .\MyModule.psm1 -Force
Say-Hello -Name "World"   # Output: Hello, World!
```
