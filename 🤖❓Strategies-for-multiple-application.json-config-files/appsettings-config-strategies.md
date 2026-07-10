---
title: Strategies for Combining appsettings.json Configuration Files
created: 2026-04-02T00:00:00+00:00
modified: 2026-04-02T00:00:00+00:00
tags:
  - dotnet
  - configuration
  - appsettings
  - feature-flags
  - local-dev
uid: 5aca82c7-0619-4b7c-8749-93148b9f420f
---

# 🤖💡 Strategies for Combining `appsettings.json` Configuration Files

A comprehensive guide to merging, layering, and managing configuration files in .NET for local development, feature flags, and environment-specific overrides.

---

## 📐 How .NET Configuration Merging Works

.NET configuration is built as a **layered pipeline**. Later sources **override** earlier ones on a key-by-key basis. Arrays are replaced entirely (not merged element-by-element).

```csharp
// Default host builder order (lowest → highest priority)
appsettings.json
appsettings.{Environment}.json
User Secrets (Development only)
Environment Variables
Command-line Arguments
```

Each key is a flat dotted path. The last writer wins.

---

## 🗂️ Strategy 1: Environment-Based Layering (Built-In)

The simplest and most idiomatic approach. Use `ASPNETCORE_ENVIRONMENT` to select a layer on top of the base config.

### File Layout

```
appsettings.json                 ← shared defaults
appsettings.Development.json     ← local dev overrides
appsettings.Staging.json         ← staging environment
appsettings.Production.json      ← production
```

### Example

**`appsettings.json`**
```json
{
  "ConnectionStrings": {
    "Default": "Server=prod-db;Database=MyApp"
  },
  "FeatureFlags": {
    "NewCheckout": false,
    "DarkMode": false
  }
}
```

**`appsettings.Development.json`**
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyApp_Dev"
  },
  "FeatureFlags": {
    "NewCheckout": true
  }
}
```

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Zero configuration, works out of the box | Only one environment active at a time |
| Well understood by all .NET devs | Arrays are replaced, not merged |
| Supported by all tooling | No per-developer or per-feature isolation |

---

## 👤 Strategy 2: User Secrets (Per-Developer Overrides)

**User Secrets** store sensitive values outside the project directory, preventing accidental commits. Ideal for connection strings, API keys, and per-developer feature flag overrides.

### Setup

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Server=my-local-db"
dotnet user-secrets set "FeatureFlags:NewCheckout" "true"
```

Secrets are stored in:
- **Mac/Linux**: `~/.microsoft/usersecrets/{UserSecretsId}/secrets.json`
- **Windows**: `%APPDATA%\Microsoft\UserSecrets\{UserSecretsId}\secrets.json`

### When It Loads

User Secrets are automatically loaded in `Development` environment when using `WebApplication.CreateBuilder()`.

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Never committed to source control | Development environment only |
| Per-developer isolation | Requires `dotnet user-secrets` CLI or IDE |
| Overrides environment files | Not sharable across a team |

---

## 🧩 Strategy 3: Feature-Specific Supplemental Config Files

Add extra config files for specific features or scenarios, loaded manually alongside the standard pipeline.

### File Layout

```
appsettings.json
appsettings.Development.json
appsettings.Features.NewCheckout.json    ← feature-specific
appsettings.Features.DarkMode.json       ← feature-specific
appsettings.Local.json                   ← gitignored, per-dev
```

### Loading in `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Load a feature-specific file if it exists
builder.Configuration.AddJsonFile(
    "appsettings.Features.NewCheckout.json",
    optional: true,
    reloadOnChange: true);

// Load a gitignored local override file
builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);
```

### `.gitignore` entry

```gitignore
appsettings.Local.json
appsettings.Features.*.json
```

> **Tip:** Commit `appsettings.Local.json.example` as a template so new devs know what to create.

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Explicit, visible in the project | Requires manual loading code |
| Easy to toggle features by including/excluding files | Risk of committing if `.gitignore` not set |
| Shareable as examples | Proliferates files over time |

---

## 🔀 Strategy 4: Environment Variable Overrides

Environment variables override all JSON files. Great for CI/CD pipelines and Docker.

### Key Format

Nested keys use `__` (double underscore) as a separator:

```bash
# Bash / shell
export ConnectionStrings__Default="Server=ci-db;Database=MyApp"
export FeatureFlags__NewCheckout="true"
```

```yaml
# docker-compose.yml
environment:
  - ConnectionStrings__Default=Server=db;Database=MyApp
  - FeatureFlags__NewCheckout=true
```

### `.env` File with `dotenv` for Local Dev

Pair with a `.env` file (gitignored) and load it in your shell or `docker-compose`:

```bash
# .env (gitignored)
ConnectionStrings__Default=Server=localhost;Database=MyApp_Dev
FeatureFlags__NewCheckout=true
```

```bash
# Load into shell
export $(cat .env | xargs)
dotnet run
```

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Highest priority, always wins | Verbose key names with `__` |
| No files to manage | Not discoverable in IDE |
| Works well in containers and CI | Easy to forget to set/unset |

---

## 🏷️ Strategy 5: Named Configuration Profiles (Custom)

Define a "profile" key in your local config to switch between pre-baked sets of settings.

### `appsettings.Development.json`

```json
{
  "ActiveProfile": "FeatureTestingA",

  "Profiles": {
    "Default": {
      "FeatureFlags": { "NewCheckout": false, "DarkMode": false }
    },
    "FeatureTestingA": {
      "FeatureFlags": { "NewCheckout": true, "DarkMode": false }
    },
    "FeatureTestingB": {
      "FeatureFlags": { "NewCheckout": true, "DarkMode": true }
    }
  }
}
```

### Reading in Code

```csharp
var activeProfile = config["ActiveProfile"] ?? "Default";
var flags = config.GetSection($"Profiles:{activeProfile}:FeatureFlags")
                  .Get<FeatureFlagsOptions>();
```

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Switch scenarios by changing one value | Custom code required to apply the profile |
| All profiles visible in one file | Config doesn't use standard options binding |
| Easy to share profile definitions | Profiles can drift out of sync |

---

## 🚩 Strategy 6: Feature Flags via a Dedicated Library

Use a purpose-built feature flag library rather than raw JSON booleans.

### Option A: Microsoft.FeatureManagement

```bash
dotnet add package Microsoft.FeatureManagement.AspNetCore
```

**`appsettings.json`**
```json
{
  "FeatureManagement": {
    "NewCheckout": false,
    "DarkMode": {
      "EnabledFor": [
        { "Name": "Percentage", "Parameters": { "Value": 25 } }
      ]
    }
  }
}
```

**`appsettings.Development.json`**
```json
{
  "FeatureManagement": {
    "NewCheckout": true,
    "DarkMode": true
  }
}
```

**`Program.cs`**
```csharp
builder.Services.AddFeatureManagement();
```

**Usage in code**
```csharp
public class CheckoutController(IFeatureManager features) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (await features.IsEnabledAsync("NewCheckout"))
            return View("NewCheckout");

        return View("LegacyCheckout");
    }
}
```

### Option B: Unleash / LaunchDarkly / Flagsmith

For large teams, use a remote feature flag service. Flags are managed in a dashboard, not in JSON files. Local dev typically uses a local SDK fallback or a staging environment instance.

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Rich targeting (user %, rollout, A/B) | Additional dependency |
| No redeployment for flag changes | Remote services have cost/complexity |
| Audit trail of flag changes | Microsoft.FeatureManagement still uses config files |

---

## 🔧 Strategy 7: `IConfiguration` Merging via `MemoryCollection`

Inject config values programmatically — useful in tests or for computed defaults.

```csharp
var extraConfig = new Dictionary<string, string?>
{
    ["FeatureFlags:NewCheckout"] = "true",
    ["FeatureFlags:DarkMode"] = "false",
};

builder.Configuration.AddInMemoryCollection(extraConfig);
```

In tests:

```csharp
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["FeatureFlags:NewCheckout"] = "true"
    })
    .Build();
```

### Pros & Cons
| ✅ Pros | ❌ Cons |
|---|---|
| Precise, no file system dependency | Not suitable for production config |
| Great for test isolation | Values must be coded manually |
| Highest control | Easy to miss updating alongside JSON files |

---

## 🗺️ Recommended Setup for Local Dev Testing

A pragmatic combination for most teams:

```
appsettings.json                  ← committed, production-safe defaults
appsettings.Development.json      ← committed, safe dev defaults
appsettings.Local.json            ← gitignored, per-developer overrides
appsettings.Local.json.example    ← committed template for new devs
```

**`Program.cs`**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true) // gitignored
    .AddEnvironmentVariables()
    .AddCommandLine(args);
```

**`.gitignore`**
```gitignore
appsettings.Local.json
```

**`appsettings.Local.json.example`**
```json
{
  "_comment": "Copy this file to appsettings.Local.json and customise for your machine.",
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MyApp_Dev"
  },
  "FeatureFlags": {
    "NewCheckout": false,
    "DarkMode": false
  }
}
```

---

## 📊 Strategy Comparison Summary

| Strategy | Dev Isolation | Shareable | Gitignore Needed | Complexity |
|---|---|---|---|---|
| Environment layering | ❌ Shared | ✅ | ❌ | Low |
| User Secrets | ✅ Per-dev | ❌ | ❌ (stored outside repo) | Low |
| Feature-specific files | ⚠️ Partial | ✅ | ✅ | Medium |
| Environment variables | ✅ Per-shell | ⚠️ Via `.env` | ✅ `.env` | Low |
| Named profiles | ⚠️ Single toggle | ✅ | ❌ | Medium |
| Feature flag library | ✅ | ✅ | ❌ | High |
| `appsettings.Local.json` | ✅ Per-dev | Via example | ✅ | Low |

---

## 💡 Key Rules to Remember

1. **Arrays are replaced, not merged** — if you override an array key, the entire array is replaced.
2. **Null coalescing doesn't apply** — an explicit `null` in a later layer will override a previous value.
3. **`reloadOnChange: true`** — allows hot-reload of config without restarting the app in development.
4. **Never commit secrets** — use User Secrets or environment variables, never raw JSON files with passwords or API keys.
5. **Document the local setup** — always commit an `.example` file so new developers know what local overrides are expected.
