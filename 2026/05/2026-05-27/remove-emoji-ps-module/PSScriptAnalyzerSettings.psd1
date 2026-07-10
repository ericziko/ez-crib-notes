#
# PSScriptAnalyzer settings for UnicodeFileNameTools.
#
# Run:
#   Install-Module PSScriptAnalyzer -Scope CurrentUser   # one-time
#   Invoke-ScriptAnalyzer -Path ./UnicodeFileNameTools -Recurse -Settings ./PSScriptAnalyzerSettings.psd1
#
@{
    Severity = @('Error', 'Warning')

    # Use all the default rules. Add explicit excludes here only if we hit a
    # rule that meaningfully conflicts with the module's design.
    ExcludeRules = @(
        # Module-scoped cache used inside Get-RuneEmojiName (and the collection
        # state used inside Repair-UnicodeFileName) is intentional and shows up
        # as a false positive on this rule.
        'PSUseDeclaredVarsMoreThanAssignments'
    )

    Rules = @{
        PSPlaceOpenBrace = @{
            Enable             = $true
            OnSameLine         = $true
            NewLineAfter       = $true
            IgnoreOneLineBlock = $true
        }
        PSUseConsistentIndentation = @{
            Enable          = $true
            IndentationSize = 4
            Kind            = 'space'
        }
    }
}
