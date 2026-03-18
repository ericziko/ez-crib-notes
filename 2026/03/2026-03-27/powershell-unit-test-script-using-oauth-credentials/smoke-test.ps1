# smoke-test.ps1
# Quick smoke test script using Auth0 Client Credentials OAuth.
#
# USAGE:
#   .\smoke-test.ps1
#
# CREDENTIALS (choose one approach — see tutorial for details):
#   Option A: Set environment variables before running:
#       $env:AUTH0_CLIENT_ID     = "your-client-id"
#       $env:AUTH0_CLIENT_SECRET = "your-client-secret"
#
#   Option B: Hardcode values below for local dev (do NOT commit secrets to git)

# ---------------------------------------------------------------------------
# ⚙️  CONFIG — edit these values
# ---------------------------------------------------------------------------

$AUTH0_TOKEN_URL = "https://YOUR-DOMAIN.us.auth0.com/oauth/token"
$AUTH0_AUDIENCE  = "https://api.yourcompany.com"          # API Identifier in Auth0
$API_ENDPOINT    = "https://api.yourcompany.com/health"   # Endpoint to smoke test
$EXPECTED_TEXT   = "healthy"                              # Text you expect in the response

# Credentials: prefer environment variables over hardcoded values
$CLIENT_ID     = if ($env:AUTH0_CLIENT_ID)     { $env:AUTH0_CLIENT_ID }     else { "YOUR_CLIENT_ID" }
$CLIENT_SECRET = if ($env:AUTH0_CLIENT_SECRET) { $env:AUTH0_CLIENT_SECRET } else { "YOUR_CLIENT_SECRET" }

# ---------------------------------------------------------------------------
# 🚀  SMOKE TEST — no edits needed below this line
# ---------------------------------------------------------------------------

# Ensure TLS 1.2 (required for Auth0 on older Windows PowerShell)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Import the module (resolves relative to this script's location)
$modulePath = Join-Path $PSScriptRoot "OAuthSmokeTest.psm1"
Import-Module $modulePath -Force

Write-Host ""
Write-Host "=== Auth0 OAuth Smoke Test ===" -ForegroundColor Cyan
Write-Host "Endpoint : $API_ENDPOINT"
Write-Host "Audience : $AUTH0_AUDIENCE"
Write-Host ""

# Step 1 — Get a token
Write-Host "Step 1: Fetching access token from Auth0..." -ForegroundColor Cyan
$token = Get-Auth0AccessToken `
    -TokenUrl     $AUTH0_TOKEN_URL `
    -ClientId     $CLIENT_ID `
    -ClientSecret $CLIENT_SECRET `
    -Audience     $AUTH0_AUDIENCE

Write-Host "        Token acquired (first 20 chars): $($token.Substring(0, 20))..." -ForegroundColor DarkGray

# Step 2 — Call the endpoint
Write-Host "Step 2: Calling endpoint..." -ForegroundColor Cyan
$response = Invoke-AuthenticatedGet `
    -Url   $API_ENDPOINT `
    -Token $token

# Step 3 — Assert
Write-Host "Step 3: Asserting response contains `"$EXPECTED_TEXT`"..." -ForegroundColor Cyan
$passed = Assert-ResponseContains -Response $response -ExpectedText $EXPECTED_TEXT

Write-Host ""
if ($passed) {
    Write-Host "Smoke test PASSED." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Smoke test FAILED." -ForegroundColor Red
    exit 1
}
