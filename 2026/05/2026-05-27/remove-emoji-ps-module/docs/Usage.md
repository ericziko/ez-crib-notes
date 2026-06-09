---
uid: 7a585018-d508-4c07-ab07-15d217c0cd57
---
# Usage recipes

All examples assume the module is imported:

```powershell
Import-Module ./UnicodeFileNameTools/UnicodeFileNameTools.psd1
```

## Detection

### Is this name "dirty"?

```powershell
Test-UnicodeFileName -Name 'release 🎉.txt'   # True
Test-UnicodeFileName -Name 'release.txt'      # False
```

### Filter a directory listing

```powershell
Get-ChildItem -Recurse | Where-Object { $_ | Test-UnicodeFileName }
```

### Rich detail (with shortcode lookup)

```powershell
Get-UnicodeFileNameCharacter -Name 'Q3 🎉 résumé.docx' | Format-Table
```

```
Name                Index Character CodePoint UnicodeCategory   LogicalCategory IsTargeted EmojiName
----                ----- --------- --------- ---------------   --------------- ---------- ---------
Q3 🎉 résumé.docx       3 🎉        U+1F389   OtherSymbol       Emoji                True party-popper
Q3 🎉 résumé.docx       8 é         U+00E9    LowercaseLetter   Diacritic           False
Q3 🎉 résumé.docx      11 é         U+00E9    LowercaseLetter   Diacritic           False
```

Only flagged characters that *would be acted upon*:

```powershell
Get-UnicodeFileNameCharacter -Name 'Q3 🎉 résumé.docx' -TargetedOnly
```

## Cleaning a name (string-only)

### Default behaviour — keep accents, replace emoji & invisibles with `_`

```powershell
Convert-UnicodeFileName -Name 'Q3 Report 🎉 final.docx'
# Q3 Report _ final.docx

Convert-UnicodeFileName -Name 'café 🎉.md'
# café _.md            <-- accents preserved by default
```

### Remove targeted characters outright

```powershell
Convert-UnicodeFileName -Name 'a 😀 b 🚀 c.txt' -Action Remove
# a  b  c.txt
```

### Collapse runs and trim edges

```powershell
Convert-UnicodeFileName -Name '😀😀😀data.txt' -CollapseRuns
# data.txt

Convert-UnicodeFileName -Name 'a😀😀b.txt' -CollapseRuns
# a_b.txt
```

### Transliterate accents to ASCII

```powershell
Convert-UnicodeFileName -Name 'Crème brûlée.md' -TargetCategory Diacritic -Transliterate
# Creme brulee.md
```

### Emoji shortcodes

```powershell
Convert-UnicodeFileName -Name 'launch 🚀.md' -EmojiNames
# launch :rocket:.md
```

Note: `:` is reserved on Windows. Combine `-EmojiNames` with `-PortableNames`
only if you intentionally want the colons stripped.

### Layered: accents → ASCII, emoji → shortcodes, everything else → `-`

```powershell
Convert-UnicodeFileName -Name 'Crème brûlée 🎂.md' `
    -TargetCategory Diacritic, Emoji, Symbol, Invisible `
    -Transliterate -EmojiNames `
    -Replacement '-' -CollapseRuns
# Creme brulee :birthday-cake:.md
```

### Explicit custom map (highest priority)

```powershell
Convert-UnicodeFileName -Name 'a 🎉 b' -Map @{ '🎉' = '-PARTY-' }
# a -PARTY- b

# Keys may be the literal character, hex code point, or U+XXXX form.
Convert-UnicodeFileName -Name 'a 🎉 b' -Map @{ 'U+1F389' = '-PARTY-' }
```

### Portable names (clean Windows-reserved chars too)

```powershell
# ':' is legal on macOS/Linux but reserved on Windows.
Convert-UnicodeFileName -Name 'a:b.txt'                  # 'a:b.txt'
Convert-UnicodeFileName -Name 'a:b.txt' -PortableNames   # 'a_b.txt'
```

### Pipeline

```powershell
'a😀.txt', 'b🎉.txt' | Convert-UnicodeFileName -Action Remove
# a.txt
# b.txt
```

## Renaming on disk

### Preview first (always recommended)

```powershell
Repair-UnicodeFileName -Path ./notes -Recurse -Action Remove -WhatIf
```

### Do the rename

```powershell
Repair-UnicodeFileName -Path ./notes -Recurse -Action Remove
```

### From a Get-ChildItem pipeline

```powershell
Get-ChildItem -Recurse | Repair-UnicodeFileName -Action Remove
```

### Skip collisions instead of suffixing

```powershell
Get-ChildItem | Repair-UnicodeFileName -CollisionAction Skip
```

Collision modes:
- **Suffix** (default) — append `' (1)'`, `' (2)'`, ... before the extension.
- **Skip** — leave the source alone, emit `Status = 'Skipped'`.
- **Fail** — write an error and continue with the next item.
- **Overwrite** — delete the colliding target then rename.

### Return the renamed items for further work

```powershell
$renamed = Get-ChildItem | Repair-UnicodeFileName -Action Remove -PassThru
$renamed | Get-FileHash
```

## Result objects (default output of `Repair-UnicodeFileName`)

| Property       | Meaning                                            |
| -------------- | -------------------------------------------------- |
| `Path`         | Original full path                                 |
| `OriginalName` | Original leaf name                                 |
| `ProposedName` | Raw cleaned name from `Convert-UnicodeFileName`    |
| `FinalName`    | Name actually used (after collision resolution)    |
| `Status`       | `Renamed` / `Unchanged` / `Skipped` / `Overwritten` / `WhatIf` |
