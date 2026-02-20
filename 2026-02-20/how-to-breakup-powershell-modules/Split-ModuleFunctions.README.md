---
title: Split-ModuleFunctions Design Notes
date: 2026-02-20
tags:
  - powershell
  - modules
  - tooling
uid: aff0b89a-818e-4296-a08e-598774023a57
---

# Split-ModuleFunctions.ps1 — Design Decisions

## What the Script Does

1. Parses a `.psm1` file with the PowerShell AST
2. Writes each top-level function to `Functions/Public/<FunctionName>.ps1`
3. Removes the function bodies from the original `.psm1`
4. Appends the dynamic loader pattern to the `.psm1`
5. Creates a `.psm1.bak` backup before touching anything

---

## Design Decisions

### 1. Use the PowerShell AST, not regex

The AST (`[System.Management.Automation.Language.Parser]`) is the only reliable way to locate function boundaries. Regex approaches break on:

- Nested braces inside here-strings or string literals
- Multiline `param()` blocks
- Comment-based help blocks inside the function body
- Functions that contain other functions (nested functions)

The AST understands all of this correctly.

### 2. Parse from the in-memory string, not from file

```powershell
$rawContent = [System.IO.File]::ReadAllText($resolvedPath)
$ast = [System.Management.Automation.Language.Parser]::ParseInput($rawContent, ...)
```

`Parser::ParseFile` and `Parser::ParseInput` both work, but using `ParseInput` on a string we already hold in memory guarantees that `Extent.StartOffset` / `Extent.EndOffset` align exactly with `$rawContent`. If we parsed from file and the file had a BOM or different line-ending normalization, offsets could be misaligned. This way there is no ambiguity.

### 3. Use `EndBlock.Statements` for top-level functions only

```powershell
$topLevelFunctions = $ast.EndBlock.Statements |
    Where-Object { $_ -is [FunctionDefinitionAst] }
```

Using `FindAll({...}, $true)` would return **all** functions in the file, including nested functions defined inside other functions. We only want top-level functions. `EndBlock.Statements` is exactly the list of top-level statements, so nested functions are automatically excluded and stay embedded in their parent.

### 4. `Extent.Text` for verbatim extraction

`$func.Extent.Text` returns the exact source text of the function as written — including the `function` keyword, name, param block, attributes, and body — with no reformatting. This preserves the author's indentation and style perfectly.

### 5. Remove functions end-to-start when rewriting the .psm1

```powershell
$orderedFunctions = $topLevelFunctions | Sort-Object { $_.Extent.StartOffset } -Descending
```

When removing text from a string by character offset, removing a segment at position X reduces the length of everything after X. If we removed start-to-end, the offsets of later functions would be wrong by the time we tried to use them. By processing end-to-start, earlier offsets are never shifted.

### 6. Abort .psm1 rewrite if any function file was skipped

If a `Functions/Public/<Name>.ps1` already exists, the function is skipped rather than overwritten. If any function was skipped, the `.psm1` is **not** rewritten. Reason: removing the function body from the `.psm1` without a corresponding file on disk would silently lose that function. The safest policy is to require a clean slate before modifying the source.

### 7. Backup before any destructive write

```powershell
Copy-Item -Path $resolvedPath -Destination ($resolvedPath + '.bak') -Force
```

The `.psm1` rewrite is irreversible from the script's perspective. A `.bak` file is created unconditionally before the write. The backup also preserves the original in case the user needs to recover comment-based help that sat outside a function (see below).

### 8. Preserve non-function module-level code

After removing function definitions, any remaining code in the `.psm1` is kept verbatim — `#Requires` directives, `using namespace` statements, `Set-StrictMode`, module-level variables, etc. These are prepended before the loader block in the rewritten file.

### 9. Warn about existing `Export-ModuleMember` calls

If non-function code contains an existing `Export-ModuleMember`, a warning is emitted. The script cannot safely remove it (it might be intentionally specific), so the user is told to inspect and remove any duplicate.

### 10. `-WhatIf` support via `SupportsShouldProcess`

Both destructive operations (writing `.ps1` files and rewriting the `.psm1`) are wrapped in `ShouldProcess` calls, so the script respects `-WhatIf` and `-Confirm` natively.

---

## Known Limitations

| Limitation | Explanation |
|---|---|
| External comment-based help not moved | If `.SYNOPSIS` etc. sits **above** the `function` keyword (outside the braces), it is not part of `Extent.Text` and stays in the `.psm1`. Check the `.bak` file to recover it. |
| Private functions not auto-detected | All functions land in `Functions/Public/`. There is no reliable way to know which functions are internal helpers without additional metadata. Move private functions manually. |
| `filter` functions treated as public | PowerShell's `filter` keyword creates a `FunctionDefinitionAst` with `IsFilter = $true`. These are extracted the same way — which is correct since `Extent.Text` preserves the `filter` keyword. |
| One function per file assumed | If your original `.psm1` has two functions with the same noun sharing a file by convention, they end up in separate files. This is the intended outcome of the refactor. |

---

## Loader Pattern Inserted into .psm1

```powershell
foreach ($folder in @('Private', 'Public')) {
    $path = Join-Path $PSScriptRoot 'Functions' $folder
    if (Test-Path $path) {
        Get-ChildItem -Path $path -Filter '*.ps1' -Recurse |
            ForEach-Object { . $_.FullName }
    }
}

Export-ModuleMember -Function (
    Get-ChildItem (Join-Path $PSScriptRoot 'Functions' 'Public') -Filter '*.ps1' -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty BaseName
)
```

Key choices in the loader:

- **`$PSScriptRoot`** — resolves to the directory containing the `.psm1`, regardless of the caller's working directory.
- **`Private` loaded before `Public`** — public functions may call private helpers; helpers must exist in scope first.
- **`-Recurse`** on `Get-ChildItem` — allows subdirectories within `Public/` and `Private/` if you want to group functions by noun/area.
- **`Export-ModuleMember` derives names from `BaseName`** — the exported function name is the filename without extension, which matches the convention that `Get-Widget.ps1` contains `function Get-Widget`.
- **`-ErrorAction SilentlyContinue` on the export** — gracefully handles the case where `Functions/Public/` is empty or doesn't exist yet.
