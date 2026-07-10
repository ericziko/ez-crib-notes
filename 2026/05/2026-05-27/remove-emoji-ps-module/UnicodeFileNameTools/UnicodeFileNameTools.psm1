#Requires -Version 7.0

Set-StrictMode -Version Latest

# Root module for UnicodeFileNameTools.
#
# Every function lives in its own file under Public/ or Private/. This loader
# discovers and dot-sources them at import time, then exports only the public
# surface. Adding a new function is therefore just a matter of dropping a new
# .ps1 file into the appropriate folder (and listing public ones in the manifest's
# FunctionsToExport for discoverability/performance).

$ErrorActionPreference = 'Stop'

# Order matters: Private helpers must be defined before the Public functions that
# call them, so load Private first.
$private = @(Get-ChildItem -Path (Join-Path $PSScriptRoot 'Private') -Filter '*.ps1' -ErrorAction SilentlyContinue)
$public  = @(Get-ChildItem -Path (Join-Path $PSScriptRoot 'Public')  -Filter '*.ps1' -ErrorAction SilentlyContinue)

foreach ($file in @($private + $public)) {
    try {
        . $file.FullName
    }
    catch {
        throw "Failed to dot-source '$($file.FullName)': $_"
    }
}

# Export only the public functions, named after their files.
if ($public.Count -gt 0) {
    Export-ModuleMember -Function $public.BaseName
}
