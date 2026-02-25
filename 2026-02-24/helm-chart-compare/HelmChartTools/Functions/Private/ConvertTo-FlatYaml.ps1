function ConvertTo-FlatYaml {
    <#
    .SYNOPSIS
        Converts a YAML file to a flat hashtable of key-value pairs.

    .DESCRIPTION
        Parses a simple YAML file and converts it to a flattened hashtable where nested
        keys become dot-separated paths (e.g., replicaCount becomes global.image.tag).
        This parser handles basic YAML structures without external dependencies.

        Note: This is a simple parser for basic YAML. Complex structures (lists, anchors,
        complex types) may not parse completely.

    .PARAMETER Path
        Path to the YAML file to parse.

    .OUTPUTS
        [hashtable] where keys are dot-separated paths and values are the parsed values.

    .EXAMPLE
        $values = ConvertTo-FlatYaml -Path './values.yaml'
        $values['global.image.tag']  # Returns the value at that path
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Path
    )

    if (-not (Test-Path $Path -PathType Leaf)) {
        Write-Error "YAML file not found: $Path"
        return @{}
    }

    $content = Get-Content -Path $Path -Raw
    $lines = $content -split "`n"
    $result = @{}
    $stack = [System.Collections.Generic.Stack[object]]::new()  # Stack of (key, indent) pairs to track hierarchy

    foreach ($line in $lines) {
        # Skip empty lines and comments
        if (-not $line -or $line -match '^\s*#') {
            continue
        }

        # Calculate indentation level
        $indent = ($line | Select-String '^ *' | ForEach-Object { $_.Matches[0].Length })
        $trimmedLine = $line.TrimStart()

        # Skip lines without colons (not key-value pairs)
        if ($trimmedLine -notmatch ':') {
            continue
        }

        # Parse key: value
        $parts = $trimmedLine -split ':\s*', 2
        $key = $parts[0].Trim()
        $value = if ($parts.Count -gt 1) { $parts[1].Trim() } else { '' }

        # Remove nested keys from stack if indent is less
        while ($stack.Count -gt 0 -and $stack.Peek().Indent -ge $indent) {
            $stack.Pop() | Out-Null
        }

        # Build full path
        if ($stack.Count -eq 0) {
            $fullKey = $key
        } else {
            $stackKeys = @()
            $tempStack = [System.Collections.Generic.Stack[object]]::new()
            # Reverse the stack to build path in order
            foreach ($item in $stack) {
                $tempStack.Push($item)
            }
            while ($tempStack.Count -gt 0) {
                $stackKeys += $tempStack.Pop().Key
            }
            [System.Array]::Reverse($stackKeys)
            $fullKey = ($stackKeys -join '.') + ".$key"
        }

        # Add to result
        if ($value) {
            # Remove quotes if present
            $value = $value.Trim('"').Trim("'")
            $result[$fullKey] = $value
        } else {
            # This is a parent key (will be added to stack for future children)
            $result[$fullKey] = $null
        }

        # Add to stack for future nested keys
        $stack.Push(@{ Key = $key; Indent = $indent })
    }

    return $result
}
