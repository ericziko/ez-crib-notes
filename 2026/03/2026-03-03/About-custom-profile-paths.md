---
uid: 6e8e6e21-add2-49bc-a146-ce8d3e3bd318
---

# About PowerShell-Custom-Module-Paths

## 🤖❓ 
How can I set a custom path at the beginning of PS module path in my profile and make sure I don't do it duplicate

## 🤖💡 

Put this in your PowerShell profile. It **prepends** your custom folder to $env:PSModulePath and **won't add it twice** (case-insensitive on Windows), even across multiple profile loads.

```powershell
# --- Custom module path (prepend once) ---
$customModulePath = 'C:\MyCustomModules'

# Normalize + split existing PSModulePath
$sep = ';'
$paths = @()
if ($env:PSModulePath) {
    $paths = $env:PSModulePath -split [regex]::Escape($sep) | Where-Object { $_ -and $_.Trim() }
}

# Add only if not already present (case-insensitive compare is typical on Windows)
if (-not ($paths -contains $customModulePath)) {
    $env:PSModulePath = ($customModulePath, $paths) -join $sep
} 
```

 A couple of practical notes:

- Use $PROFILE.CurrentUserCurrentHost (common) or $PROFILE.CurrentUserAllHosts (applies to all hosts) depending on what you want.


If your custom path might not exist yet, you can guard it:


```powershell
if (Test-Path $customModulePath) { ... }
```
