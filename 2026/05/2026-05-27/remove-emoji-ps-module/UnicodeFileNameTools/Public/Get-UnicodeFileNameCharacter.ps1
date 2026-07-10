#Requires -Version 7.0

<#
.SYNOPSIS
    Reports the non-ASCII characters found in a file name, one detail object per
    character.

.DESCRIPTION
    Inspects a file name (or the .Name of a FileInfo from the pipeline) and emits a
    rich object for each Unicode scalar value (Rune) that is not plain ASCII. Each
    object records where the character is, what it is, and whether it would be
    acted upon given the chosen target categories — making it the natural building
    block for "what's wrong with this name?" reports.

.PARAMETER Name
    One or more file name strings, or FileInfo objects (the .Name property is used)
    from the pipeline.

.PARAMETER TargetCategory
    Logical categories that drive the IsTargeted flag. Default: Emoji, Symbol,
    Invisible.

.PARAMETER TargetedOnly
    Emit only characters whose category is in -TargetCategory.

.OUTPUTS
    PSCustomObject with properties:
      Name             - the file name the character came from
      Index            - UTF-16 offset where the character begins
      Character        - the character itself
      CodePoint        - 'U+XXXX' form of the scalar value
      UnicodeCategory  - the .NET UnicodeCategory
      LogicalCategory  - the module's logical category (see Get-RuneCategory)
      IsTargeted       - whether the character is in -TargetCategory
      EmojiName        - the bundled emoji shortcode, or $null

.EXAMPLE
    Get-UnicodeFileNameCharacter -Name 'Q3 🎉 résumé.docx' | Format-Table

.EXAMPLE
    Get-ChildItem | Get-UnicodeFileNameCharacter -TargetedOnly

.LINK
    Convert-UnicodeFileName
.LINK
    Test-UnicodeFileName
#>
function Get-UnicodeFileNameCharacter {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName, Position = 0)]
        [Alias('FullName')]
        [AllowEmptyString()]
        [string]$Name,

        [ValidateSet('Emoji', 'Symbol', 'Diacritic', 'Cjk', 'Whitespace', 'Invisible', 'Punctuation', 'Control', 'OtherNonAscii')]
        [string[]]$TargetCategory = @('Emoji', 'Symbol', 'Invisible'),

        [switch]$TargetedOnly
    )

    process {
        if ([string]::IsNullOrEmpty($Name)) {
            return
        }

        # A FileInfo binds its FullName via the alias; reduce to the leaf name so we
        # never flag directory separators in a path.
        $leaf = [System.IO.Path]::GetFileName($Name)
        if ([string]::IsNullOrEmpty($leaf)) {
            $leaf = $Name
        }

        $index = 0
        foreach ($rune in $leaf.EnumerateRunes()) {
            $logical = Get-RuneCategory -Rune $rune

            if ($logical -ne 'Ascii') {
                $isTargeted = Test-RuneTargeted -Rune $rune -TargetCategory $TargetCategory

                if (-not $TargetedOnly -or $isTargeted) {
                    [pscustomobject]@{
                        Name            = $leaf
                        Index           = $index
                        Character       = $rune.ToString()
                        CodePoint       = 'U+{0:X4}' -f $rune.Value
                        UnicodeCategory = [System.Text.Rune]::GetUnicodeCategory($rune)
                        LogicalCategory = $logical
                        IsTargeted      = $isTargeted
                        EmojiName       = Get-RuneEmojiName -Rune $rune
                    }
                }
            }

            $index += $rune.Utf16SequenceLength
        }
    }
}
