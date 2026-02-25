function Get-HelmVariablePattern {
    <#
    .SYNOPSIS
        Returns regex patterns for detecting different types of variables in Helm charts.

    .DESCRIPTION
        Internal helper function that provides standardized regex patterns for:
        - Helm Values: {{ .Values.something }}
        - Helm Builtins: {{ .Release.Name }}, {{ .Chart.Version }}
        - Go template vars: {{ $varName }}
        - Environment vars: ${VAR}, $ENV:VAR

    .PARAMETER VariableType
        The type of variable pattern to return. Defaults to 'All' which returns all patterns.

    .OUTPUTS
        [hashtable] with pattern names as keys and compiled regex objects as values.
        If VariableType is specific, returns just that pattern.

    .EXAMPLE
        $allPatterns = Get-HelmVariablePattern -VariableType All
        $helmPatterns = Get-HelmVariablePattern -VariableType HelmValue
    #>
    [CmdletBinding()]
    param(
        [ValidateSet('All', 'HelmValue', 'HelmBuiltin', 'GoVar', 'EnvVar')]
        [string]$VariableType = 'All'
    )

    # Define all regex patterns
    $patterns = @{
        # Helm Values: {{ .Values.something }} or {{ .Values.nested.path }}
        HelmValue = [regex]'\{\{\s*\.Values\.[^\}]+\}\}'

        # Helm Builtins: {{ .Release.Name }}, {{ .Chart.Version }}, etc.
        HelmBuiltin = [regex]'\{\{\s*\.(Release|Chart|Capabilities|Files|Template|Namespace)\.[^\}]*\}\}'

        # Go template variables: {{ $varName }}
        GoVar = [regex]'\{\{\s*\$[A-Za-z_]\w*\s*\}\}'

        # Environment variables: ${VAR_NAME} or $ENV:VAR_NAME
        EnvVar = [regex]'\$(?:ENV:|\{)[A-Za-z_][A-Za-z0-9_]*\}?'
    }

    if ($VariableType -eq 'All') {
        return $patterns
    } else {
        return $patterns[$VariableType]
    }
}
