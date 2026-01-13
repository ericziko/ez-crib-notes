---
title: 📰 The Best Way To Validate Your Options In .NET Core 9
source: https://medium.com/codetodeploy/the-best-way-to-validate-your-options-in-net-core-9-c34b3533376e
author:
  - "[[Mori]]"
published: 2025-11-25
created: 2026-01-07
description: The Best Way To Validate Your Options In .NET Core 9 Part 1 — Foundations, Pitfalls, and Building a Bulletproof Configuration System 🚀 Top Remote Tech Roles — $50–$120/hr  Hiring experienced …
tags:
  - clippings
updated: 2026-01-07T23:22
---

# 📰 The Best Way To Validate Your Options In .NET Core 9

The First Publication That Blends Tech Insights + Real Job Opportunities

![](<_resources/📰 The Best Way To Validate Your Options In .NET Core 9/99f1d8c1da5f667ddeda9dc1ed83af2a_MD5.webp>)

🚀 **Top Remote Tech Roles — $50–$120/hr**  
Hiring **experienced developers (3+ years)** only.

- Frontend / Backend / Full Stack
- Mobile (iOS/Android)
- AI / ML
- DevOps & Cloud

## "Configuration without validation is just delayed failure."

In most.NET Core applications, configuration is treated as an afterthought.  
A quick `appsettings.json`, some environment variables, and we move on.

Until one day — your application crashes in production because of a missing or malformed value.

This article is not about **basic** configuration.  
This is a deep, production-grade guide to building a **fully validated, enterprise-ready configuration system** using the **Options Pattern in.NET Core 9**.

By the end of this series, you will know how to build configuration that:

✅ Fails fast at startup  
✅ Produces meaningful, actionable errors  
✅ Supports complex validation rules  
✅ Integrates cleanly with Clean Architecture  
✅ Works with Minimal APIs and DI  
✅ Prevents hidden production failures

# 1\. Why Configuration Validation Is Critical

Most applications depend on dozens of configuration values:

- Database connection strings
- SMTP servers
- API keys
- JWT secrets
- External service URLs
- Feature flags
- Cache settings

Now imagine this happens in production:

```c
"Jwt": {
  "Issuer": "",
  "Audience": "my-api-users",
  "Secret": "123"
}
```

The app boots.  
Users authenticate.  
Tokens get generated…

…with a weak secret.

Security compromised.

And no one noticed — because you never validated your options properly.

# 2\. What Is the Options Pattern (Quick Recap)

The Options pattern is the official way to bind configuration sections into strongly-typed objects.

Example `appsettings.json`:

```c
{
  "EmailSettings": {
    "SmtpServer": "smtp.myapp.com",
    "Port": 587,
    "SenderEmail": "no-reply@myapp.com",
    "UseSsl": true
  }
}
```

Strongly typed class:

```c
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SenderEmail { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
}
```

Binding in `Program.cs`:

```c
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
```

At this point…

⚠ Your application will accept empty, invalid or broken values without complaint.

That's dangerous.

# 3\. The 5 Levels of Options Validation in.NET 9

.NET Core 9 gives us a powerful options validation pipeline:

1. Data Annotations Validation
2. Custom Validation using `IValidateOptions<T>`
3. Startup-time Validation
4. Advanced Cross-Options Validation
5. Environment-based Validation

We will implement all of them properly.

# 4\. Level 1 — Data Annotations Validation

Let's start with the basics: validation using attributes.

# 4.1 Add Data Annotations to Your Options

```c
using System.ComponentModel.DataAnnotations;
public class EmailSettings
{
    [Required]
    [MinLength(3)]
    public string SmtpServer { get; set; } = string.Empty;
    [Range(1, 65535)]
    public int Port { get; set; }
    [Required]
    [EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
}
```

These attributes:

- Prevent empty values
- Prevent invalid email formats
- Enforce valid port ranges

# 4.2 Enable Data Annotations Validation

In `Program.cs`:

```c
builder.Services
    .AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("EmailSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

✅ `.ValidateDataAnnotations()` activates attribute validation  
✅ `.ValidateOnStart()` forces validation during app startup

Now the app **won't even start** if the settings are invalid.

Exactly what we want.

# 5\. Why DataAnnotations Alone Are Not Enough

DataAnnotations are great for:

✔ Simple validation  
✔ Basic constraints  
✔ Syntax checks

But they **don't work** for:

❌ Cross-field rules  
❌ Business logic validation  
❌ Environment-aware validation  
❌ Reserved value detection  
❌ External dependency checks

Example of complex rule:

## If UseSsl = true, then Port must not be 25

DataAnnotations alone cannot express this properly.

So we go to Level 2.

# 6\. Level 2 — Custom Validation Using IValidateOptions<T>

This is where real professional validation begins.

# 6.1 Create a Custom Validator

```c
using Microsoft.Extensions.Options;
public class EmailSettingsValidator : IValidateOptions<EmailSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailSettings settings)
    {
        if (settings.Port == 25 && settings.UseSsl)
            return ValidateOptionsResult.Fail(
                "Port 25 cannot be used with SSL enabled.");
        if (settings.SmtpServer.Contains("localhost")
            && Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            return ValidateOptionsResult.Fail(
                "Using localhost SMTP in production is forbidden.");
        return ValidateOptionsResult.Success;
    }
}
```

This allows:

✅ Complex logic  
✅ Environment-based rules  
✅ Full business constraints

# 6.2 Register the Validator

In `Program.cs`:

```c
builder.Services.AddSingleton<IValidateOptions<EmailSettings>, EmailSettingsValidator>();
```

Now this validator is part of the pipeline.

Every time `EmailSettings` is resolved, validation is applied.

# 7\. Level 3 — Failing Fast on Startup

One of the **most powerful features** is forcing validation when the app starts.

Already done with:

```c
.ValidateOnStart();
```

Now, if any validation fails:

- App startup fails
- Error is logged
- You fix configuration before deployment continues

This is how real enterprise systems behave.

# 8\. Real Example: Validating JWT Configuration

Let's do a serious example: JWT configuration.

# 8.1 JWT Settings Model

```c
using System.ComponentModel.DataAnnotations;
public class JwtSettings
{
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;
    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; }
}
```

# 8.2 JWT Custom Validator

```c
using Microsoft.Extensions.Options;
public class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings settings)
    {
        if (settings.Secret.Length < 32)
            return ValidateOptionsResult.Fail("JWT Secret must be at least 32 characters long.");
        if (settings.ExpiryMinutes <= 0)
            return ValidateOptionsResult.Fail("Expiry must be positive.");
        return ValidateOptionsResult.Success;
    }
}
```

# 8.3 Register It

```c
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();
```

# 9\. What We Achieved So Far

Right now, your application has:

✅ Strong typing  
✅ Annotation validation  
✅ Custom logic validation  
✅ Environment-aware rules  
✅ Startup enforcement

This is already far beyond **95%** of tutorials online.

But this is still just Part 1.

# Coming in Part 2

In Part 2, I'll cover:

✅ Cross-options validation (validating multiple option classes together)  
✅ Dealing with external services configuration  
✅ Async validation for options  
✅ Custom error formatting for logging  
✅ How this fits in Clean Architecture  
✅ Real-world project structure

# The Best Way To Validate Your Options In.NET Core 9

Part 2 — Advanced Validation, Cross-Options, and Clean Architecture Integration

![](<_resources/📰 The Best Way To Validate Your Options In .NET Core 9/17f6b1cc09146246eced5d18c588500c_MD5.webp>)

"Validation is only as good as the rules you enforce, not the ones you skip."

# 1️⃣ Cross-Options Validation

Sometimes, a single options class is not enough.  
In large applications, multiple configuration sections **depend on each other**.

Example: `EmailSettings` + `FeatureFlags`:

```c
public class FeatureFlags
{
    public bool EnableEmailNotifications { get; set; }
}
```

# Problem

If `EnableEmailNotifications = true` but `EmailSettings.SmtpServer` is empty, the app will crash at runtime.

We need **cross-options validation**.

# 1.1 Implementing Cross-Options Validator

```c
using Microsoft.Extensions.Options;
public class CrossOptionsValidator : IValidateOptions<EmailSettings>
{
    private readonly IOptions<FeatureFlags> _featureFlags;
    public CrossOptionsValidator(IOptions<FeatureFlags> featureFlags)
    {
        _featureFlags = featureFlags;
    }
    public ValidateOptionsResult Validate(string? name, EmailSettings settings)
    {
        var flags = _featureFlags.Value;
        var errors = new List<string>();
        if (flags.EnableEmailNotifications && string.IsNullOrWhiteSpace(settings.SmtpServer))
        {
            errors.Add("Email notifications are enabled but SMTP Server is not configured.");
        }
        return errors.Any()
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
```

# 1.2 Register the Cross-Options Validator

```c
builder.Services.AddSingleton<IValidateOptions<EmailSettings>, CrossOptionsValidator>();
```

✅ Now the system **enforces dependencies** between multiple options.

# 2️⃣ Async Options Validation

Sometimes, validation depends on **external services**.

Example: validate SMTP host is reachable, or API key is valid.

# 2.1 Async Validator Example

```c
public class AsyncEmailValidator : IValidateOptions<EmailSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailSettings options)
    {
        // Simulate external async check
        var reachable = CheckSmtpServerAsync(options.SmtpServer).GetAwaiter().GetResult();
if (!reachable)
            return ValidateOptionsResult.Fail("SMTP server is not reachable.");
        return ValidateOptionsResult.Success;
    }
    private async Task<bool> CheckSmtpServerAsync(string smtpServer)
    {
        // Use TcpClient, HttpClient, or SMTP probe
        await Task.Delay(50); // simulation
        return true;
    }
}
```

⚠ Note: `IValidateOptions<T>` is synchronous, so you must **block on async calls** carefully.  
⚠ For production, consider **pre-start health checks** for async validations.

# 3️⃣ Environment-Aware Validation

Different rules for different environments.

```c
public class EnvAwareValidator : IValidateOptions<EmailSettings>
{
    private readonly IWebHostEnvironment _env;
public EnvAwareValidator(IWebHostEnvironment env)
    {
        _env = env;
    }
    public ValidateOptionsResult Validate(string? name, EmailSettings settings)
    {
        var errors = new List<string>();
        if (_env.IsProduction() && !settings.UseSsl)
        {
            errors.Add("SSL must be enabled in Production.");
        }
        return errors.Any() ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}
```

✅ Environment-aware validation ensures **safety in production** without blocking local development.

# 4️⃣ Nested Options Validation

Configurations often contain **nested sections**:

```c
"DatabaseSettings": {
  "Postgres": {
    "Host": "localhost",
    "Port": 5432
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

# 4.1 Nested Options Classes

```c
public class DatabaseSettings
{
    public PostgresSettings Postgres { get; set; } = new();
    public RedisSettings Redis { get; set; } = new();
}
public class PostgresSettings
{
    [Required]
    public string Host { get; set; } = string.Empty;
    [Range(1025, 65535)]
    public int Port { get; set; }
}
public class RedisSettings
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
```

# 4.2 Bind & Validate Nested Options

```c
builder.Services.AddOptions<DatabaseSettings>()
    .Bind(builder.Configuration.GetSection("DatabaseSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Nested classes **automatically validate their properties**.

# 5️⃣ Integration With Clean Architecture

In Clean Architecture:

LayerRoleDomainPure domain, no optionsApplicationInterfaces, commands, queriesInfrastructureConcrete options + validatorsWebBinding + DI

Example structure:

```c
/Domain
    Entities
/Application
    Interfaces
Infrastructure
    Config
        EmailSettings.cs
        EmailSettingsValidator.cs
Web
    Program.cs
```

✅ Keeps configuration **testable, maintainable, and environment safe**.

# 6️⃣ Advanced Logging for Validation Failures

Instead of crashing silently, log detailed messages:

```c
builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});
try
{
    var emailOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<EmailSettings>>().Value;
}
catch (OptionsValidationException ex)
{
    foreach (var failure in ex.Failures)
    {
        Console.WriteLine($"Configuration Error: {failure}");
    }
    throw;
}
```

✅ You get **detailed, actionable logs** before the app fully starts.

# 7️⃣ Unit Testing Configuration Validation

You can test validators **without starting the app**:

```c
[Fact]
public void EmailSettingsValidator_ShouldFail_WhenSmtpServerEmpty()
{
    var validator = new EmailSettingsValidator();
var options = new EmailSettings
    {
        SmtpServer = "",
        Port = 587,
        SenderEmail = "test@domain.com"
    };
    var result = validator.Validate(null, options);
    Assert.False(result.Succeeded);
    Assert.Contains(result.FailureMessage, m => m.Contains("SMTP"));
}
```

✅ Ensures **CI/CD safety**.  
✅ Catch configuration bugs **before deployment**.

# 8️⃣ Part 2 Summary

In Part 2, we have covered:

- Cross-options validation
- Async validation for external services
- Environment-specific rules
- Nested configuration validation
- Clean Architecture integration
- Logging & Unit Testing

You now have a **robust configuration pipeline** that is production-ready.

# Coming in Part 3

In the final part, we will explore:

✅ Validating multiple environments dynamically  
✅ Hot-reloading configuration with validation  
==✅ Integration with Minimal APIs and Dependency Injection==  
✅ Real-world production-ready project setup  
✅ Health check integration for options  
✅ Pro tips and best practices for enterprise-grade systems

# The Best Way To Validate Your Options In.NET Core 9

## Part 3 — Dynamic Environment Validation, Hot Reload, Minimal APIs, and Production Best Practices

![](<_resources/📰 The Best Way To Validate Your Options In .NET Core 9/d46e808a65bdc22d4972f2c399909553_MD5.webp>)

## "Configuration is the backbone of stable applications. Master it, and your system can survive anything."

# 1️⃣ Validating Multiple Environments Dynamically

In real-world applications, configuration differs per environment:

- **Development**: relaxed validation, mock services
- **Staging**: near-production behavior
- **Production**: strict rules, fail-fast

# 1.1 Example: Environment-Specific Options Validator

```c
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
public class EnvironmentAwareEmailValidator : IValidateOptions<EmailSettings>
{
    private readonly IWebHostEnvironment _env;
    public EnvironmentAwareEmailValidator(IWebHostEnvironment env)
    {
        _env = env;
    }
    public ValidateOptionsResult Validate(string? name, EmailSettings settings)
    {
        var errors = new List<string>();
        if (_env.IsProduction() && !settings.UseSsl)
        {
            errors.Add("SSL must be enabled in Production.");
        }
        if (_env.IsDevelopment() && string.IsNullOrEmpty(settings.SmtpServer))
        {
            // Development allows empty SMTP for local testing
        }
        else if (string.IsNullOrEmpty(settings.SmtpServer))
        {
            errors.Add("SMTP server cannot be empty in Staging/Production.");
        }
        return errors.Any() ? ValidateOptionsResult.Fail(errors) : ValidateOptionsResult.Success;
    }
}
```

# 1.2 Registration

```c
builder.Services.AddSingleton<IValidateOptions<EmailSettings>, EnvironmentAwareEmailValidator>();
```

✅ This approach **automatically adapts rules** per environment without changing the core code.

# 2️⃣ Hot-Reloading Configuration With Validation

.NET 9 supports **hot-reload of configuration**. Combine it with options validation to safely reload configs at runtime.

# 2.1 Enable Hot Reload

```c
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
```

# 2.2 Use IOptionsMonitor<T> to React to Changes

```c
public class EmailService
{
    private EmailSettings _settings;
public EmailService(IOptionsMonitor<EmailSettings> optionsMonitor)
    {
        _settings = optionsMonitor.CurrentValue;
        optionsMonitor.OnChange(updated =>
        {
            ValidateOptions(updated);
            _settings = updated;
            Console.WriteLine("EmailSettings updated dynamically.");
        });
    }
    private void ValidateOptions(EmailSettings settings)
    {
        var validator = new EnvironmentAwareEmailValidator(new WebHostEnvironmentStub());
        var result = validator.Validate(null, settings);
        if (!result.Succeeded)
            throw new OptionsValidationException("EmailSettings failed dynamic validation", typeof(EmailSettings), result.Failures);
    }
}
// Stub for IWebHostEnvironment in this context
public class WebHostEnvironmentStub : IWebHostEnvironment
{
    public string EnvironmentName => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    public string ApplicationName { get; set; } = "MyApp";
    public string WebRootPath { get; set; } = string.Empty;
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string ContentRootPath { get; set; } = string.Empty;
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}
```

✅ Now your application **automatically reloads and validates configuration at runtime**.

# 3️⃣ Integration With Minimal APIs & Dependency Injection

Minimal APIs simplify modern.NET applications. Options validation integrates seamlessly.

# 3.1 Minimal API Example

```c
var builder = WebApplication.CreateBuilder(args);
// Configure options with validation
builder.Services.AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection("EmailSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<EmailSettings>, EnvironmentAwareEmailValidator>();
// Register EmailService
builder.Services.AddSingleton<EmailService>();
var app = builder.Build();
app.MapGet("/email-settings", (EmailService emailService) =>
{
    return Results.Ok(emailService.GetCurrentSettings());
});
app.Run();
```

# 3.2 EmailService

```c
public class EmailService
{
    private readonly EmailSettings _settings;
public EmailService(IOptions<EmailSettings> options)
    {
        _settings = options.Value;
    }
    public EmailSettings GetCurrentSettings() => _settings;
}
```

✅ Minimal APIs **consume validated options directly**, making the system safe by default.

# 4️⃣ Real-World Production-Ready Project Setup

# 4.1 Folder Structure (Clean Architecture + Config)

```c
/Domain
    Entities
/Application
    Interfaces
    Services
/Infrastructure
    Config
        EmailSettings.cs
        EmailSettingsValidator.cs
    Services
/Web
    Program.cs
    appsettings.json
    appsettings.Development.json
    appsettings.Production.json
```

# 4.2 Multi-Environment JSON

**appsettings.Development.json**

```c
{
  "EmailSettings": {
    "SmtpServer": "",
    "Port": 587,
    "SenderEmail": "dev@myapp.com",
    "UseSsl": false
  }
}
```

**appsettings.Production.json**

```c
{
  "EmailSettings": {
    "SmtpServer": "smtp.myapp.com",
    "Port": 587,
    "SenderEmail": "no-reply@myapp.com",
    "UseSsl": true
  }
}
```

# 5️⃣ Health Check Integration For Options

.NET 9 allows integrating validated options with health checks:

```c
builder.Services.AddHealthChecks()
    .AddCheck("EmailSettings", () =>
    {
        var options = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
        var validator = new EnvironmentAwareEmailValidator(new WebHostEnvironmentStub());
        var result = validator.Validate(null, options);
return result.Succeeded ? 
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy() :
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(string.Join(", ", result.Failures));
    });
```

- Health endpoint `/health` will **report configuration errors**.
- Perfect for monitoring and CI/CD pipelines.

# 6️⃣ Advanced Production Tips

1. ==**Fail Fast on Invalid Configuration**== ==— Always use== ==`ValidateOnStart()`====.==
2. **Use** `**IOptionsMonitor**` **for Hot Reload** – No restarts for small config changes.
3. **Separate Dev / Staging / Prod Configs** — Avoid mistakes.
4. ==**Centralize Validators**== ==— Keep validation in infrastructure layer.==
5. **Test Validators** — Unit-test to catch config mistakes before deployment.
6. **Integrate with Health Checks** — Monitor config state in production.
7. **Cross-Options Validation** — Catch dependency errors between configs.

# 🔥 Pro Tips Summary

- Treat configuration as **code**. Strong typing + validation saves production headaches.
- Always combine **DataAnnotations + Custom Validators** for enterprise safety.
- Environment-aware validators prevent local configs from breaking production.
- Use `IOptionsMonitor<T>` + hot reload for runtime adaptability.
- Integrate configuration validation with **health checks** for monitoring.
- Test every validator in **unit and integration tests**.

# 🏁 Conclusion

By following this complete guide, your.NET Core 9 application will:

- Have strongly-typed, validated configurations
- Fail fast for invalid settings
- Adapt validation per environment
- Support dynamic hot-reload
- Integrate cleanly with Minimal APIs, DI, and Clean Architecture
- Be production-ready with health monitoring and CI/CD safety

# 🌟 Final Thoughts

1. Configuration **is not optional**; it is a core part of system stability.
2. Validation ensures your system is **predictable and safe**.
3. Treat configuration like code: testable, versioned, and monitored.
4. Hot-reload and dynamic environment validation are game-changers for modern apps.
5. With this approach, your.NET Core 9 apps will survive scaling, deployments, and complex environments.

## Proper configuration validation is the silent hero of production-ready applications. Master it, and you eliminate a huge class of runtime failures before they happen
