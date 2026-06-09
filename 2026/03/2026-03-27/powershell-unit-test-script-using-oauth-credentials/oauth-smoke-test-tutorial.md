---
uid: da83f984-380e-4353-a562-08d9a02beb1d
---
# 🤖❓ PowerShell OAuth Smoke Testing with Auth0 Client Credentials

---

## 🤖💡 Overview

This tutorial shows you how to replicate what Postman does when you use **OAuth 2.0 Client Credentials** against Auth0 — entirely in PowerShell. You'll end up with:

1. A reusable **PowerShell module** (`OAuthSmokeTest.psm1`) with helper functions.
2. A **smoke test script** you can run from the command line in seconds.

---

## 🧠 How Auth0 Client Credentials Works

The flow has two steps:

```
[Your Script]
     |
     |---(1) POST /oauth/token  (client_id + client_secret + audience)--->  [Auth0]
     |<--(2) access_token (JWT) -----------------------------------------------
     |
     |---(3) GET /your-api-endpoint  (Authorization: Bearer <token>) --->  [Your API]
     |<--(4) JSON response -----------------------------------------------
     |
     |---(5) Assert expected text is in the response
```

**Key Auth0 parameters for Client Credentials:**

| Parameter      | Description                                              | Example                                  |
|----------------|----------------------------------------------------------|------------------------------------------|
| `grant_type`   | Always `client_credentials` for this flow               | `client_credentials`                     |
| `client_id`    | Your Auth0 application's Client ID                      | `abc123xyz`                              |
| `client_secret`| Your Auth0 application's Client Secret                  | `super-secret-value`                     |
| `audience`     | The API Identifier registered in Auth0                  | `https://api.yourcompany.com`            |
| Token URL      | `https://<your-auth0-domain>/oauth/token`               | `https://dev-xyz.us.auth0.com/oauth/token`|

> 💡 **Finding these in Postman**: Open your Postman request → Authorization tab → OAuth 2.0 → "Get New Access Token". All five values above will be visible there.

---

## 📁 File Structure

```
powershell-unit-test-script-using-oauth-credentials/
├── OAuthSmokeTest.psm1        ← The reusable module
├── smoke-test.ps1             ← Your runnable smoke test script
└── oauth-smoke-test-tutorial.md  ← This file
```

---

## 🔧 Step 1 — The PowerShell Module

The module (`OAuthSmokeTest.psm1`) provides three functions:

| Function                  | What it does                                          |
|---------------------------|-------------------------------------------------------|
| `Get-Auth0AccessToken`    | Calls Auth0 and returns a bearer token                |
| `Invoke-AuthenticatedGet` | Makes a GET request with the token in the header      |
| `Assert-ResponseContains` | Checks the response for expected text and reports     |

See `OAuthSmokeTest.psm1` in this directory for the full implementation.

---

## ▶️ Step 2 — Running Your First Smoke Test

### Option A: Use the example `smoke-test.ps1` script

Edit the config block at the top of `smoke-test.ps1` with your real values, then run:

```powershell
.\smoke-test.ps1
```

### Option B: Use the module interactively

```powershell
# 1. Import the module
Import-Module .\OAuthSmokeTest.psm1

# 2. Get a token
$token = Get-Auth0AccessToken `
    -TokenUrl    "https://dev-xyz.us.auth0.com/oauth/token" `
    -ClientId    "YOUR_CLIENT_ID" `
    -ClientSecret "YOUR_CLIENT_SECRET" `
    -Audience    "https://api.yourcompany.com"

# 3. Call your endpoint
$response = Invoke-AuthenticatedGet `
    -Url   "https://api.yourcompany.com/health" `
    -Token $token

# 4. Assert something in the response
Assert-ResponseContains -Response $response -ExpectedText "healthy"
```

---

## 🔐 Step 3 — Storing Credentials Safely

**Never hardcode secrets in scripts.** Here are three safe approaches:

### Option 1: Environment Variables (simplest)

```powershell
# Set once in your shell session (or in your .profile):
$env:AUTH0_CLIENT_ID     = "your-client-id"
$env:AUTH0_CLIENT_SECRET = "your-client-secret"

# Then in your script:
$token = Get-Auth0AccessToken `
    -TokenUrl     "https://dev-xyz.us.auth0.com/oauth/token" `
    -ClientId     $env:AUTH0_CLIENT_ID `
    -ClientSecret $env:AUTH0_CLIENT_SECRET `
    -Audience     "https://api.yourcompany.com"
```

### Option 2: PowerShell SecretManagement (recommended for local dev)

```powershell
# Install once:
Install-Module Microsoft.PowerShell.SecretManagement
Install-Module Microsoft.PowerShell.SecretStore

# Store once:
Set-Secret -Name "Auth0ClientSecret" -Secret "your-secret"

# Retrieve in script:
$secret = Get-Secret -Name "Auth0ClientSecret" -AsPlainText
```

### Option 3: Prompt at runtime

```powershell
$cred = Get-Credential -Message "Enter Auth0 Client ID and Secret" -UserName "client_id"
$token = Get-Auth0AccessToken `
    -TokenUrl     "https://dev-xyz.us.auth0.com/oauth/token" `
    -ClientId     $cred.UserName `
    -ClientSecret ($cred.Password | ConvertFrom-SecureString -AsPlainText) `
    -Audience     "https://api.yourcompany.com"
```

---

## 🐛 Step 4 — Troubleshooting Common Errors

### `401 Unauthorized`
- Wrong `client_id` or `client_secret`
- Token expired (tokens are short-lived; `Get-Auth0AccessToken` fetches a fresh one each call)

### `403 Forbidden`
- The token was issued but the API rejected it
- Check that `audience` matches exactly what is registered in Auth0 (including trailing slash or lack thereof)

### `{"error":"access_denied","error_description":"Service not found"}`
- The `audience` value doesn't match any API registered in Auth0

### SSL/TLS errors on Windows
```powershell
# Add this at the top of your script if you hit TLS errors:
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
```

### Inspecting the raw token (JWT decode)

```powershell
# Quick JWT payload decoder (no library needed):
function Show-JwtPayload($token) {
    $parts  = $token -split '\.'
    $padded = $parts[1].PadRight($parts[1].Length + (4 - $parts[1].Length % 4) % 4, '=')
    [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($padded)) | ConvertFrom-Json
}

Show-JwtPayload $token
```

---

## ✅ Step 5 — Understanding the Assertion Output

The `Assert-ResponseContains` function writes colour-coded output:

```
[PASS] Response contains expected text: "healthy"
[FAIL] Expected "healthy" but it was NOT found in the response.
       Response preview: {"status":"degraded","uptime":99}
```

It also sets `$LASTEXITCODE` so you can use it in CI pipelines:

```powershell
.\smoke-test.ps1
if ($LASTEXITCODE -ne 0) { Write-Error "Smoke test failed!" }
```

---

## 📚 Reference

- [Auth0 Client Credentials Flow docs](https://auth0.com/docs/get-started/authentication-and-authorization-flow/client-credentials-flow)
- [PowerShell Invoke-RestMethod docs](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.utility/invoke-restmethod)
- [PowerShell SecretManagement](https://learn.microsoft.com/en-us/powershell/utility-modules/secretmanagement/overview)
