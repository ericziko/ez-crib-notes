function Compare-FlatYaml {
    <#
    .SYNOPSIS
        Compares two flattened YAML hashtables and returns differences.

    .DESCRIPTION
        Takes two hashtables (typically from ConvertTo-FlatYaml) and returns a structured
        object containing added keys, removed keys, and changed values.

    .PARAMETER Reference
        The reference hashtable to compare against.

    .PARAMETER Difference
        The difference hashtable to compare.

    .OUTPUTS
        [PSCustomObject] with properties: Added, Removed, Changed
        - Added: [hashtable] keys in Difference but not in Reference
        - Removed: [hashtable] keys in Reference but not in Difference
        - Changed: [hashtable] keys with different values

    .EXAMPLE
        $ref = ConvertTo-FlatYaml -Path values-v1.yaml
        $diff = ConvertTo-FlatYaml -Path values-v2.yaml
        $comparison = Compare-FlatYaml -Reference $ref -Difference $diff
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Reference,

        [Parameter(Mandatory)]
        [hashtable]$Difference
    )

    $added = @{}
    $removed = @{}
    $changed = @{}

    # Find added and changed keys
    foreach ($key in $Difference.Keys) {
        if (-not $Reference.ContainsKey($key)) {
            $added[$key] = $Difference[$key]
        } elseif ($Reference[$key] -ne $Difference[$key]) {
            $changed[$key] = @{
                OldValue = $Reference[$key]
                NewValue = $Difference[$key]
            }
        }
    }

    # Find removed keys
    foreach ($key in $Reference.Keys) {
        if (-not $Difference.ContainsKey($key)) {
            $removed[$key] = $Reference[$key]
        }
    }

    return [PSCustomObject]@{
        Added   = $added
        Removed = $removed
        Changed = $changed
    }
}
