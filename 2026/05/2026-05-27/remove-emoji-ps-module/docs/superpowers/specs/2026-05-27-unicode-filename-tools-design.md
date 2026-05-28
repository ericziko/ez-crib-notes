---
title: UnicodeFileNameTools — Design Spec
date: 2026-05-27
status: approved
related: "🤖❓remove-emoji-ps-module.md"
---

# UnicodeFileNameTools — Design Spec

A PowerShell module to detect, and remove/replace/substitute, emoji and other
Unicode characters in file names.

## Goals

- Detect emoji and configurable Unicode character categories in file names.
- Clean names by removing, replacing, or substituting those characters.
- Break every function into its own file, loaded dynamically by a central module.
- Ship a module manifest.
- Provide a comprehensive Pester (v5) unit-test suite.
- Follow professional PowerShell best practices.
- Document via markdown docs and comment-based help.

## Decisions (from brainstorming)

| Topic | Decision |
| --- | --- |
| Runtime target | **PowerShell 7+ (Core), cross-platform.** Manifest `PowerShellVersion = '7.0'`, `CompatiblePSEditions = @('Core')`. Enables `[System.Text.Rune]`/grapheme-correct handling. |
| Detection scope | **Configurable Unicode categories.** Default targets: Emoji, Symbol/Pictograph, Invisible/Format. Diacritics, CJK, punctuation left alone unless opted in. |
| Substitution | **Layered pipeline:** explicit map → ASCII transliteration → emoji shortcode → single fallback replacement. |
| Disk scope | **Strings + files, with rename.** Pure string functions plus filesystem functions that rename via `Rename-Item` under `ShouldProcess`, with `-Recurse` and collision handling. |

## Architecture

Approach: **category-rule engine.** A central classifier maps each `[System.Text.Rune]`
to a *logical category*. The user selects which categories are *targeted*; a
substitution pipeline decides how each targeted rune is replaced. This supports
mixed strategies in one pass (e.g. transliterate diacritics while shortcoding emoji).

### Logical categories

`Ascii, Emoji, Symbol, Diacritic, Cjk, Punctuation, Whitespace, Invisible, Control, OtherNonAscii`

Default targeted set: `Emoji, Symbol, Invisible`. Filesystem-illegal characters for
the current OS are always cleaned regardless of category.

### Layout

```
UnicodeFileNameTools/
├── UnicodeFileNameTools.psd1       # manifest
├── UnicodeFileNameTools.psm1       # loader: dot-source Private/ then Public/, export Public
├── Public/
│   ├── Test-UnicodeFileName.ps1
│   ├── Get-UnicodeFileNameCharacter.ps1
│   ├── Convert-UnicodeFileName.ps1
│   └── Repair-UnicodeFileName.ps1
├── Private/
│   ├── Get-RuneCategory.ps1
│   ├── Test-RuneTargeted.ps1
│   ├── ConvertTo-TransliteratedText.ps1
│   ├── Get-RuneEmojiName.ps1
│   ├── Invoke-SubstitutionPipeline.ps1
│   └── Resolve-FileNameCollision.ps1
├── Data/
│   └── EmojiNames.psd1             # curated, extensible emoji → shortcode table
├── tests/                          # Pester 5
├── docs/                           # README, Usage, Architecture
└── PSScriptAnalyzerSettings.psd1
```

The `.psm1` enumerates `Private/*.ps1` then `Public/*.ps1`, dot-sources each, and
`Export-ModuleMember -Function` the public base names. The manifest lists the same
four names explicitly in `FunctionsToExport`.

## Public API

### `Test-UnicodeFileName`
Predicate. Accepts a `[string]` name or `[System.IO.FileInfo]` via pipeline.
Returns `[bool]` by default; `-Detailed` returns an object with the matched
characters. `-TargetCategory` overrides the default targeted set.

### `Get-UnicodeFileNameCharacter`
Analysis/reporting. Emits one object per flagged rune:
`Index, Char, CodePoint (U+XXXX), UnicodeName, Category, LogicalCategory, IsTargeted`.

### `Convert-UnicodeFileName`
Pure `string → string`. No disk access; the unit-test heart of the module.
Parameters:
- `-Name` (pipeline, mandatory)
- `-Action Remove | Replace | Substitute` (default `Substitute`)
- `-TargetCategory` (default `Emoji, Symbol, Invisible`)
- `-Replacement` (default `'_'`)
- `-Transliterate` (switch — diacritics → ASCII)
- `-EmojiNames` (switch — emoji → `:shortcode:`)
- `-Map` (hashtable of explicit char/codepoint → replacement)
- `-CollapseRuns` (switch — collapse consecutive replacements, trim edges)
- `-PortableNames` (switch — also clean the Windows-reserved set so output is
  portable across OSes)

### `Repair-UnicodeFileName`
Filesystem. `-Path`/`-LiteralPath`/pipeline `FileInfo`, `-Recurse`. Computes the
clean name via `Convert-UnicodeFileName`, renames via `Rename-Item` under
`SupportsShouldProcess` (`-WhatIf`/`-Confirm` are the preview mechanism).
`-CollisionAction Suffix | Skip | Fail | Overwrite` (default `Suffix`).
Emits result objects: `Original, Proposed, Final, Status`. `-PassThru` returns the
renamed items.

## Substitution pipeline

For each targeted rune, in priority order:
1. Explicit `-Map` entry.
2. If `-Transliterate` and the rune has an ASCII fold (NFKD + strip combining marks).
3. If `-EmojiNames` and the rune is an emoji → `:shortcode:` from `EmojiNames.psd1`.
4. Fall back to `-Replacement`.

`-CollapseRuns` collapses consecutive replacement output and trims leading/trailing
replacement characters (avoids `__file__`).

`Remove` action = replacement string of `''`. `Replace` = always use `-Replacement`,
ignore transliterate/emoji/map. `Substitute` = full pipeline.

## Constraints

- .NET exposes no Unicode *name* database, so emoji shortcodes come from a bundled,
  curated `EmojiNames.psd1` (common emoji), overridable/extendable by the caller.
  Documented as non-exhaustive.
- PS 7+ only; no Windows PowerShell 5.1 surrogate-pair workarounds needed.

## Testing

Pester 5. One spec per public function, helper coverage, and an integration spec.
- Pure functions: every category × action × pipeline-stage combination.
- `Repair-UnicodeFileName`: `TestDrive:`-based renames, `-WhatIf`, `-Recurse`,
  and each collision mode.
- Cross-platform path assertions.

## Documentation

- Comment-based help on every function (`.SYNOPSIS/.DESCRIPTION/.PARAMETER/.EXAMPLE/.OUTPUTS`).
- `docs/README.md` (overview, install, quick start), `docs/Usage.md` (recipes),
  `docs/Architecture.md` (category engine + pipeline).
- PSScriptAnalyzer-clean against `PSScriptAnalyzerSettings.psd1`.

## Out of scope (YAGNI)

- Windows PowerShell 5.1 support.
- Exhaustive Unicode-name database.
- Content (not name) sanitization.
- GUI / interactive prompts beyond `ShouldProcess`.
