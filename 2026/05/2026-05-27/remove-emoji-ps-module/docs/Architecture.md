# Architecture

The module is small (four public functions, six private helpers) but the
internals are organised around two ideas: a **category-rule engine** that
labels each Unicode character, and a **layered substitution pipeline** that
decides what each targeted character should become.

## Layout

```
UnicodeFileNameTools/
├── UnicodeFileNameTools.psd1   # manifest (PS 7+, Core, explicit FunctionsToExport)
├── UnicodeFileNameTools.psm1   # loader: dot-sources Private/, then Public/
├── Public/                     # one .ps1 per exported function
│   ├── Test-UnicodeFileName.ps1
│   ├── Get-UnicodeFileNameCharacter.ps1
│   ├── Convert-UnicodeFileName.ps1
│   └── Repair-UnicodeFileName.ps1
├── Private/                    # one .ps1 per internal helper
│   ├── Get-RuneCategory.ps1
│   ├── Test-RuneTargeted.ps1
│   ├── ConvertTo-TransliteratedText.ps1
│   ├── Get-RuneEmojiName.ps1
│   ├── Invoke-SubstitutionPipeline.ps1
│   └── Resolve-FileNameCollision.ps1
└── Data/
    └── EmojiNames.psd1         # curated emoji -> shortcode table
```

The root `.psm1` enumerates `Private/*.ps1` then `Public/*.ps1`, dot-sources
each, and `Export-ModuleMember`s the public file basenames. The manifest also
lists the four public names explicitly in `FunctionsToExport` (for fast module
auto-loading and discoverability).

## The category engine

Every character is processed as a [`System.Text.Rune`][rune] — a full Unicode
scalar value — so surrogate-pair characters such as 😀 (U+1F600) are handled
as one unit. `Get-RuneCategory` maps each Rune to one of these
*logical categories*:

| Category        | Meaning                                                          |
| --------------- | ---------------------------------------------------------------- |
| `Ascii`         | Printable ASCII (U+0020..U+007E)                                 |
| `Control`       | Control characters                                               |
| `Emoji`         | Supplementary-plane pictographs (≥ U+1F000)                      |
| `Symbol`        | Math, currency, modifier, "other" symbols (©, ™, →, ★, ❤, ✨...)  |
| `Diacritic`     | Accented Latin letters and combining marks                       |
| `Cjk`           | CJK ideographs, Hiragana, Katakana, Hangul                       |
| `Whitespace`    | Non-ASCII whitespace (NBSP, em-space, ideographic space, ...)    |
| `Invisible`     | Format characters and variation selectors (ZWJ, BOM, VS-16, ...) |
| `Punctuation`   | Non-ASCII punctuation (guillemets, dashes, curly quotes)         |
| `OtherNonAscii` | Everything else (Greek, Cyrillic, ...)                           |

`Test-RuneTargeted` returns `$true` when a Rune's logical category is in the
caller's target set. Printable ASCII is never reported as targeted; sanitising
ASCII is not what this module is for. (Filesystem-illegal ASCII characters are
handled separately, see below.)

### Why range-based emoji detection?

There is no built-in .NET API for the Unicode `Emoji` property, and the BMP
contains both true emoji (❤ U+2764, ✨ U+2728) and look-alike text symbols
(★ U+2605) in the same blocks — range-based detection cannot reliably tell
them apart. The module classifies all supplementary-plane pictographs
(≥ U+1F000) as `Emoji`, and all BMP symbols as `Symbol`. Since both
categories are targeted by default, the default cleaning result is identical.
Emoji *shortcoding* (`-EmojiNames`) keys off the `Data/EmojiNames.psd1` table,
not the category, so BMP emoji still receive shortcodes when present in the
table.

## The substitution pipeline

`Invoke-SubstitutionPipeline` resolves one targeted Rune to its replacement
string, based on `-Action`:

- **Remove** → always `''` (delete the character).
- **Replace** → always `-Replacement` (ignore the other knobs).
- **Substitute** → run the layered pipeline, *first match wins*:
  1. An explicit `-Map` entry (literal char, hex, or `U+XXXX` key).
  2. ASCII transliteration via NFKD-decompose + strip combining marks
     (if `-Transliterate`).
  3. An emoji shortcode `:name:` (if `-EmojiNames`).
  4. Fall back to `-Replacement`.

`Convert-UnicodeFileName` walks the input one Rune at a time, calls
`Test-RuneTargeted`, and either appends the original character or whatever
the pipeline returns. Filesystem-illegal characters
(`[System.IO.Path]::GetInvalidFileNameChars()` + the Windows-reserved set
when `-PortableNames` is on) are always run through the pipeline, regardless
of category.

## Disk-side concerns

`Repair-UnicodeFileName` is the only function that touches the filesystem. It:

1. Resolves `-Path` / `-LiteralPath` (and pipeline `PSPath`) to
   `FileSystemInfo` items.
2. Expands directories with `-Recurse` when requested.
3. Sorts items deepest-first so a parent rename never invalidates a child path
   we're about to process.
4. Calls `Convert-UnicodeFileName` for each item's leaf name.
5. Queries siblings to detect collisions, then resolves the final name via
   `Resolve-FileNameCollision`.
6. Guards every change with `$PSCmdlet.ShouldProcess`, so `-WhatIf` and
   `-Confirm` work natively.
7. Calls `Rename-Item` (and `Remove-Item` first, for `-CollisionAction
   Overwrite`).

The pure functions (`Convert-UnicodeFileName`, the private helpers) never
touch disk, which keeps the test surface large and trivially fast.

[rune]: https://learn.microsoft.com/dotnet/api/system.text.rune
