#Requires -Version 7.0

<#
.SYNOPSIS
    Renames files and directories on disk so their names no longer contain emoji
    or other targeted Unicode characters.

.DESCRIPTION
    Computes a cleaned name for each input item using Convert-UnicodeFileName, then
    renames the item via Rename-Item under SupportsShouldProcess — so -WhatIf gives
    a free preview and -Confirm gates each change. Collisions are handled
    according to -CollisionAction (Suffix, Skip, Fail, Overwrite).

    With -Recurse, directories are walked recursively. Items are processed deepest
    first so that descendants are renamed before their parent directories.

    Accepts pipeline input from Get-ChildItem (via the PSPath alias).

.PARAMETER Path
    Paths to file or directory items. Supports wildcards.

.PARAMETER LiteralPath
    Literal paths (no wildcard expansion). Accepts pipeline FileSystemInfo objects
    via the PSPath alias — i.e. 'Get-ChildItem | Repair-UnicodeFileName' just
    works.

.PARAMETER Recurse
    When an input path is a directory, also process its descendants.

.PARAMETER CollisionAction
    What to do when the cleaned name is already taken in the target directory.
    Suffix (default) appends ' (1)', ' (2)', ... before the extension. Skip leaves
    the source alone. Fail stops with an error. Overwrite deletes the colliding
    target before renaming.

.PARAMETER PassThru
    Emit the renamed FileSystemInfo objects instead of the default result objects.

.PARAMETER Action
    Forwarded to Convert-UnicodeFileName. Remove, Replace, or Substitute. Default
    Substitute.

.PARAMETER TargetCategory
    Forwarded to Convert-UnicodeFileName. Default: Emoji, Symbol, Invisible.

.PARAMETER Replacement
    Forwarded to Convert-UnicodeFileName. Default '_'.

.PARAMETER Transliterate
    Forwarded to Convert-UnicodeFileName.

.PARAMETER EmojiNames
    Forwarded to Convert-UnicodeFileName.

.PARAMETER Map
    Forwarded to Convert-UnicodeFileName.

.PARAMETER CollapseRuns
    Forwarded to Convert-UnicodeFileName.

.PARAMETER PortableNames
    Forwarded to Convert-UnicodeFileName.

.OUTPUTS
    PSCustomObject by default, with properties:
      Path          - the original full path
      OriginalName  - the original leaf name
      ProposedName  - the raw cleaned name from Convert-UnicodeFileName
      FinalName     - the name actually used (may differ if a suffix was added)
      Status        - Renamed | Unchanged | Skipped | Overwritten | WhatIf

    With -PassThru, emits the new FileSystemInfo for items that were renamed (or
    that were already clean).

.EXAMPLE
    Repair-UnicodeFileName -Path './*.md' -Action Remove -WhatIf
    # Preview what would happen to every Markdown file in the current directory.

.EXAMPLE
    Get-ChildItem -Recurse | Repair-UnicodeFileName -Transliterate -EmojiNames -CollapseRuns -PassThru
    # Walk the tree, fold accents, shortcode emoji, return the renamed items.

.EXAMPLE
    Repair-UnicodeFileName -Path ./photos -Recurse -Action Remove -CollisionAction Skip

.LINK
    Convert-UnicodeFileName
.LINK
    Test-UnicodeFileName
#>
function Repair-UnicodeFileName {
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium', DefaultParameterSetName = 'Path')]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory, Position = 0, ParameterSetName = 'Path')]
        [string[]]$Path,

        [Parameter(Mandatory, ParameterSetName = 'LiteralPath', ValueFromPipelineByPropertyName)]
        [Alias('PSPath')]
        [string[]]$LiteralPath,

        [switch]$Recurse,

        [ValidateSet('Suffix', 'Skip', 'Fail', 'Overwrite')]
        [string]$CollisionAction = 'Suffix',

        [switch]$PassThru,

        # ---- forwarded to Convert-UnicodeFileName ----
        [ValidateSet('Remove', 'Replace', 'Substitute')]
        [string]$Action = 'Substitute',

        [ValidateSet('Emoji', 'Symbol', 'Diacritic', 'Cjk', 'Whitespace', 'Invisible', 'Punctuation', 'Control', 'OtherNonAscii')]
        [string[]]$TargetCategory = @('Emoji', 'Symbol', 'Invisible'),

        [string]$Replacement = '_',

        [switch]$Transliterate,

        [switch]$EmojiNames,

        [hashtable]$Map,

        [switch]$CollapseRuns,

        [switch]$PortableNames
    )

    begin {
        $convertParams = @{
            Action         = $Action
            TargetCategory = $TargetCategory
            Replacement    = $Replacement
            Transliterate  = $Transliterate
            EmojiNames     = $EmojiNames
            CollapseRuns   = $CollapseRuns
            PortableNames  = $PortableNames
        }
        if ($Map) { $convertParams.Map = $Map }

        # Collect items across all process-block calls so we can sort
        # deepest-first before renaming.
        $script:_collected = [System.Collections.Generic.List[System.IO.FileSystemInfo]]::new()
        $script:_seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    }

    process {
        $inputs = if ($PSCmdlet.ParameterSetName -eq 'Path') {
            foreach ($p in $Path) { Resolve-Path -Path $p -ErrorAction Stop }
        }
        else {
            foreach ($p in $LiteralPath) { Resolve-Path -LiteralPath $p -ErrorAction Stop }
        }

        foreach ($resolved in $inputs) {
            $item = Get-Item -LiteralPath $resolved.ProviderPath -Force -ErrorAction Stop
            if ($script:_seenPaths.Add($item.FullName)) {
                $script:_collected.Add($item)
            }

            if ($Recurse -and $item.PSIsContainer) {
                foreach ($child in Get-ChildItem -LiteralPath $item.FullName -Recurse -Force) {
                    if ($script:_seenPaths.Add($child.FullName)) {
                        $script:_collected.Add($child)
                    }
                }
            }
        }
    }

    end {
        # Deepest first: rename children before their parent directories, so the
        # parent rename does not invalidate paths we are about to act on.
        $ordered = $script:_collected | Sort-Object -Property @{ Expression = { $_.FullName.Length }; Descending = $true }

        foreach ($item in $ordered) {
            $original = $item.Name
            $proposed = Convert-UnicodeFileName -Name $original @convertParams

            if ($proposed -eq $original) {
                $result = [pscustomobject]@{
                    Path         = $item.FullName
                    OriginalName = $original
                    ProposedName = $proposed
                    FinalName    = $original
                    Status       = 'Unchanged'
                }
                if ($PassThru) { $item } else { $result }
                continue
            }

            if ([string]::IsNullOrEmpty($proposed)) {
                Write-Warning "Skipping '$($item.FullName)': cleaned name would be empty."
                if (-not $PassThru) {
                    [pscustomobject]@{
                        Path         = $item.FullName
                        OriginalName = $original
                        ProposedName = $proposed
                        FinalName    = $null
                        Status       = 'Skipped'
                    }
                }
                continue
            }

            $parentPath = [System.IO.Path]::GetDirectoryName($item.FullName)
            # Force array context so a directory with one file (the source) doesn't
            # collapse to $null after filtering out the source itself.
            $siblings   = @(Get-ChildItem -LiteralPath $parentPath -Force | Where-Object { $_.Name -ne $original } | ForEach-Object Name)

            try {
                $finalName = Resolve-FileNameCollision -DesiredName $proposed -ExistingName $siblings -CollisionAction $CollisionAction
            }
            catch {
                Write-Error -Message "Collision resolving '$($item.FullName)' -> '$proposed': $_" -ErrorAction Continue
                continue
            }

            if ($null -eq $finalName) {
                if (-not $PassThru) {
                    [pscustomobject]@{
                        Path         = $item.FullName
                        OriginalName = $original
                        ProposedName = $proposed
                        FinalName    = $null
                        Status       = 'Skipped'
                    }
                }
                continue
            }

            $target = Join-Path $parentPath $finalName
            $willOverwrite = ($CollisionAction -eq 'Overwrite' -and $finalName -eq $proposed -and (Test-Path -LiteralPath $target))

            $message = if ($willOverwrite) {
                "Overwrite '$target' with renamed '$($item.FullName)'"
            }
            else {
                "Rename to '$finalName'"
            }

            if (-not $PSCmdlet.ShouldProcess($item.FullName, $message)) {
                if (-not $PassThru) {
                    [pscustomobject]@{
                        Path         = $item.FullName
                        OriginalName = $original
                        ProposedName = $proposed
                        FinalName    = $finalName
                        Status       = 'WhatIf'
                    }
                }
                continue
            }

            if ($willOverwrite) {
                Remove-Item -LiteralPath $target -Force -Recurse -Confirm:$false
            }

            Rename-Item -LiteralPath $item.FullName -NewName $finalName -Confirm:$false -ErrorAction Stop

            $newItem = Get-Item -LiteralPath (Join-Path $parentPath $finalName) -Force

            if ($PassThru) {
                $newItem
            }
            else {
                [pscustomobject]@{
                    Path         = $item.FullName
                    OriginalName = $original
                    ProposedName = $proposed
                    FinalName    = $finalName
                    Status       = if ($willOverwrite) { 'Overwritten' } else { 'Renamed' }
                }
            }
        }
    }
}
