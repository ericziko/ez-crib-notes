#Requires -Version 7.0
#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

<#
.SYNOPSIS
    Runs the UnicodeFileNameTools Pester suite with concise output.
.PARAMETER Path
    Optional path (file or directory) to run a subset of tests. Defaults to ./tests.
.EXAMPLE
    ./Invoke-Tests.ps1
.EXAMPLE
    ./Invoke-Tests.ps1 -Path ./tests/Get-RuneCategory.Tests.ps1
#>
[CmdletBinding()]
param(
    [string]$Path = (Join-Path $PSScriptRoot 'tests')
)

$config = New-PesterConfiguration
$config.Run.Path = $Path
$config.Run.PassThru = $true
$config.Output.Verbosity = 'None'
$config.Should.ErrorAction = 'Continue'

$result = Invoke-Pester -Configuration $config

Write-Host ''
Write-Host ("Passed:  {0}" -f $result.PassedCount)  -ForegroundColor Green
Write-Host ("Failed:  {0}" -f $result.FailedCount)  -ForegroundColor ($result.FailedCount -gt 0 ? 'Red' : 'DarkGray')
Write-Host ("Skipped: {0}" -f $result.SkippedCount) -ForegroundColor DarkGray
Write-Host ("Duration: {0:n2}s" -f $result.Duration.TotalSeconds) -ForegroundColor DarkGray

if ($result.FailedCount -gt 0) {
    Write-Host ''
    Write-Host 'Failed tests:' -ForegroundColor Red
    foreach ($test in $result.Failed) {
        Write-Host ("  - {0}" -f $test.ExpandedPath) -ForegroundColor Red
        Write-Host ("      {0}" -f ($test.ErrorRecord[0].Exception.Message -split "`n")[0]) -ForegroundColor DarkYellow
    }
    exit 1
}
