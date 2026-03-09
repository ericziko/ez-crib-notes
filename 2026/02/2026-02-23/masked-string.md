


```cs
public static string MaskKeepLast(string? value, int keep = 3, char maskChar = '*')
{
    if (string.IsNullOrEmpty(value))
        return value ?? string.Empty;

    if (keep <= 0)
        return new string(maskChar, value.Length);

    if (value.Length <= keep)
        return value;

    var maskedLength = value.Length - keep;
    return new string(maskChar, maskedLength) + value[^keep..];
}

```
