# OAuthSmokeTest.psm1
# Reusable PowerShell module for Auth0 Client Credentials OAuth smoke testing.
#
# USAGE:
#   Import-Module .\OAuthSmokeTest.psm1
#
# EXPORTED FUNCTIONS:
#   Get-Auth0AccessToken      - Fetches a bearer token from Auth0
#   Invoke-AuthenticatedGet   - Makes an authenticated GET request
#   Assert-ResponseContains   - Asserts that a response body contains expected text

#region Get-Auth0AccessToken

function Get-Auth0AccessToken {
    <#
    .SYNOPSIS
        Fetches an OAuth 2.0 access token from Auth0 using the Client Credentials flow.

    .PARAMETER TokenUrl
        The Auth0 token endpoint.
        Example: https://dev-xyz.us.auth0.com/oauth/token

    .PARAMETER ClientId
        Your Auth0 application's Client ID.

    .PARAMETER ClientSecret
        Your Auth0 application's Client Secret.

    .PARAMETER Audience
        The API Identifier registered in Auth0 (must match exactly).
        Example: https://api.yourcompany.com

    .OUTPUTS
        [string] The raw access token (JWT string).

    .EXAMPLE
        $token = Get-Auth0AccessToken `
            -TokenUrl     "https://dev-xyz.us.auth0.com/oauth/token" `
            -ClientId     "abc123" `
            -ClientSecret "super-secret" `
            -Audience     "https://api.yourcompany.com"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string] $TokenUrl,
        [Parameter(Mandatory)][string] $ClientId,
        [Parameter(Mandatory)][string] $ClientSecret,
        [Parameter(Mandatory)][string] $Audience
    )

    $body = @{
        grant_type    = "client_credentials"
        client_id     = $ClientId
        client_secret = $ClientSecret
        audience      = $Audience
    }

    Write-Verbose "Requesting access token from $TokenUrl"

    try {
        $response = Invoke-RestMethod `
            -Method  Post `
            -Uri     $TokenUrl `
            -Body    ($body | ConvertTo-Json) `
            -ContentType "application/json"

        if (-not $response.access_token) {
            throw "Auth0 response did not contain an access_token. Response: $($response | ConvertTo-Json)"
        }

        Write-Verbose "Access token acquired. Token type: $($response.token_type), Expires in: $($response.expires_in)s"
        return $response.access_token
    }
    catch {
        Write-Error "Failed to retrieve access token from Auth0.`nURL: $TokenUrl`nError: $_"
        throw
    }
}

#endregion

#region Invoke-AuthenticatedGet

function Invoke-AuthenticatedGet {
    <#
    .SYNOPSIS
        Makes an HTTP GET request to a URL, attaching a Bearer token in the Authorization header.

    .PARAMETER Url
        The full URL of the endpoint to call.

    .PARAMETER Token
        The bearer token string returned by Get-Auth0AccessToken.

    .PARAMETER TimeoutSeconds
        Request timeout in seconds. Defaults to 30.

    .OUTPUTS
        [PSCustomObject] The parsed JSON response body.
        If the response is not valid JSON, returns the raw string.

    .EXAMPLE
        $response = Invoke-AuthenticatedGet `
            -Url   "https://api.yourcompany.com/health" `
            -Token $token
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string] $Url,
        [Parameter(Mandatory)][string] $Token,
        [int] $TimeoutSeconds = 30
    )

    $headers = @{
        Authorization = "Bearer $Token"
        Accept        = "application/json"
    }

    Write-Verbose "GET $Url"

    try {
        $response = Invoke-RestMethod `
            -Method             Get `
            -Uri                $Url `
            -Headers            $headers `
            -TimeoutSec         $TimeoutSeconds

        return $response
    }
    catch [System.Net.WebException] {
        $statusCode = [int]$_.Exception.Response.StatusCode
        Write-Error "HTTP $statusCode error calling $Url`nError: $_"
        throw
    }
    catch {
        Write-Error "Request failed for $Url`nError: $_"
        throw
    }
}

#endregion

#region Assert-ResponseContains

function Assert-ResponseContains {
    <#
    .SYNOPSIS
        Asserts that a response object contains the expected text.
        Writes a coloured PASS/FAIL result and sets $LASTEXITCODE.

    .PARAMETER Response
        The response object returned by Invoke-AuthenticatedGet (or any object/string).

    .PARAMETER ExpectedText
        The text you expect to find somewhere in the serialized response.

    .PARAMETER CaseSensitive
        If specified, the text match is case-sensitive. Default is case-insensitive.

    .OUTPUTS
        [bool] $true if the assertion passed, $false if it failed.

    .EXAMPLE
        Assert-ResponseContains -Response $response -ExpectedText "healthy"

    .EXAMPLE
        Assert-ResponseContains -Response $response -ExpectedText "ACTIVE" -CaseSensitive
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][object] $Response,
        [Parameter(Mandatory)][string] $ExpectedText,
        [switch] $CaseSensitive
    )

    # Normalise the response to a string for searching
    $responseText = if ($Response -is [string]) {
        $Response
    } else {
        $Response | ConvertTo-Json -Depth 10 -Compress
    }

    $found = if ($CaseSensitive) {
        $responseText.Contains($ExpectedText)
    } else {
        $responseText.ToLower().Contains($ExpectedText.ToLower())
    }

    if ($found) {
        Write-Host "[PASS] Response contains expected text: `"$ExpectedText`"" -ForegroundColor Green
        $global:LASTEXITCODE = 0
        return $true
    } else {
        $preview = if ($responseText.Length -gt 200) { $responseText.Substring(0, 200) + "..." } else { $responseText }
        Write-Host "[FAIL] Expected `"$ExpectedText`" but it was NOT found in the response." -ForegroundColor Red
        Write-Host "       Response preview: $preview" -ForegroundColor Yellow
        $global:LASTEXITCODE = 1
        return $false
    }
}

#endregion

#region Module exports

Export-ModuleMember -Function @(
    'Get-Auth0AccessToken'
    'Invoke-AuthenticatedGet'
    'Assert-ResponseContains'
)

#endregion
