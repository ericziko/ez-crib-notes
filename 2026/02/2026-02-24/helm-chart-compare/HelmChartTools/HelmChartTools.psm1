# =============================================================================
# HelmChartTools.psm1 — Dynamic Function Loader
# =============================================================================
# This module loads all private helpers first, then public functions.
# Exported functions are controlled via Export-ModuleMember.
# =============================================================================

# Load private functions first (public functions may depend on them)
$privatePath = Join-Path $PSScriptRoot 'Functions' 'Private'
if (Test-Path $privatePath) {
    Get-ChildItem -Path $privatePath -Filter '*.ps1' -Recurse |
        ForEach-Object {
            Write-Verbose "Loading private function: $($_.BaseName)"
            . $_.FullName
        }
}

# Load public functions
$publicPath = Join-Path $PSScriptRoot 'Functions' 'Public'
$publicFunctions = @()
if (Test-Path $publicPath) {
    Get-ChildItem -Path $publicPath -Filter '*.ps1' -Recurse |
        ForEach-Object {
            Write-Verbose "Loading public function: $($_.BaseName)"
            . $_.FullName
            $publicFunctions += $_.BaseName
        }
}

# Export only the public functions
Export-ModuleMember -Function $publicFunctions
