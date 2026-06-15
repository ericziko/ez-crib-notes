---
title: "ASP.NET Core .NET 10 Program.cs Explained: The Complete Guide to Everything You Can Configure"
source: "https://blog.stackademic.com/asp-net-core-net-10-program-cs-explained-the-complete-guide-to-everything-you-can-configure-4c14b7b554f3"
author:
  - "[[Mori]]"
published: 2026-05-16
created: 2026-06-15
description: "“” is published by   Mori in Stackademic."
tags:
  - "clippings"
---
# ASP.NET Core .NET 10 Program.cs Explained The Complete Guide to Everything You Can Configure
![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*mi_BYmhRl0fe3r9NRwy-Xg.png)

### Your Program.cs File Is the Most Important File in Your Entire Application

Let me tell you about the worst production outage I’ve ever caused.

It was 2 AM. A deployment had just gone live. The API was returning 500s for every request. I SSH’d into the server, tailing logs, sweating. The error? `InvalidOperationException: Authorization middleware must be added after Authentication middleware.`

I had reordered two lines in `Program.cs` during a "cleanup" commit. Two lines. Twelve characters of diff. And it took down a payment processing API handling $50K per hour.

That night I learned something every senior developer knows but few talk about: Program.cs is not just configuration. It’s the runtime contract of your entire application. Every service registration, every middleware ordering decision, every DI lifetime choice ripples through every request your API handles for its entire lifetime.

Yet most developers treat Program.cs like a junk drawer. Services get added wherever. Middleware gets copy-pasted from Stack Overflow in whatever order. Configuration gets hardcoded because “it’s just Program.cs.”

This article changes that. We’re going to build a production-grade.NET 10 Web API from scratch, line by line, and understand exactly what every line does, why it matters, and how to organize it so your next 2 AM deployment doesn’t end in a war room.

## The Evolution: From Startup.cs Chaos to Minimal Hosting Mastery

## The Old World (.NET Core 1.0–5.0)

```c
// Program.cs (old style)
public class Program
{
    public static void Main(string[] args)
        => CreateHostBuilder(args).Build().Run();
public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
                webBuilder.UseStartup<Startup>());
}
// Startup.cs (separate file, split concerns)
public class Startup
{
    public void ConfigureServices(IServiceCollection services) { }
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) { }
}
```

Problems with the old approach:

- Two files to understand the startup sequence
- `Startup.cs` became a god class (500+ lines was common)
- No clear visual flow — services in one file, middleware in another
- Boilerplate ceremony for simple APIs

## The Minimal Hosting Revolution (.NET 6+)

```c
// .NET 6+ — everything in one file, top-level statements
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
```

Why this won:

- Single file shows the complete startup story
- Top-level statements eliminate `Main()` ceremony
- Visual flow: builder → services → build → middleware → run
- Reduced cognitive load for new developers

## .NET 10: The Mature Minimal Model

.NET 10 refines this further with:

- Enhanced `WebApplicationBuilder` with more configuration sources
- Improved source generators for configuration binding
- Better AOT (Ahead-of-Time) compilation support for minimal APIs
- Enhanced OpenAPI integration out of the box

## Building the Foundation:.NET 10 Web API from Scratch

## Project Creation

```c
dotnet new webapi -n ProductionApi -o ProductionApi
cd ProductionApi
# Add production packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add package Microsoft.AspNetCore.OpenApi
```

## Complete Project Structure

```c
📁 ProductionApi/
├── 📄 Program.cs                    # The brain — everything lives here
├── 📄 appsettings.json              # Base configuration
├── 📄 appsettings.Development.json # Dev overrides
├── 📄 appsettings.Production.json   # Production overrides
├── 📁 Properties/
│   └── 📄 launchSettings.json      # Launch profiles
├── 📁 Controllers/                  # API controllers (or Minimal APIs)
├── 📁 Services/                     # Business logic
├── 📁 Data/                        # EF Core context
├── 📁 Models/                      # DTOs and entities
├── 📁 Middleware/                   # Custom middleware
└── 📁 Configuration/               # Options classes
```

## The Program.cs Lifecycle: Five Stages Every Request Travels Through

Understanding the lifecycle is critical. Every ASP.NET Core application follows this exact sequence:

```c
┌─────────────────────────────────────────────────────────────┐
│  STAGE 1: WebApplicationBuilder Creation                    │
│  → Reads configuration files, env vars, command line args   │
│  → Sets up the DI container                                   │
├─────────────────────────────────────────────────────────────┤
│  STAGE 2: Service Registration (builder.Services)           │
│  → AddControllers, AddDbContext, AddAuthentication, etc.    │
│  → Everything that will be injected later gets registered   │
├─────────────────────────────────────────────────────────────┤
│  STAGE 3: Build the App (builder.Build())                   │
│  → DI container is sealed and validated                     │
│  → Configuration is frozen                                    │
│  → Middleware pipeline is prepared                            │
├─────────────────────────────────────────────────────────────┤
│  STAGE 4: Middleware Pipeline Configuration (app.UseXxx)     │
│  → Order matters — first added = first executed             │
│  → Each middleware can short-circuit the pipeline           │
├─────────────────────────────────────────────────────────────┤
│  STAGE 5: Run the Host (app.Run())                          │
│  → Kestrel starts listening                                  │
│  → Requests begin flowing through the pipeline              │
└─────────────────────────────────────────────────────────────┘
```

Let’s build each stage with production-ready code.

## Stage 1 & 2: WebApplicationBuilder and Service Configuration

## The Complete Program.cs Foundation

```c
// Program.cs — Stage 1 & 2: Builder Creation and Service Registration
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductionApi.Configuration;
using ProductionApi.Data;
using ProductionApi.Middleware;
using ProductionApi.Services;
var builder = WebApplication.CreateBuilder(args);
// ============================================================================
// CONFIGURATION SOURCES (Stage 1 - happens automatically, but configurable)
// ============================================================================
// WebApplication.CreateBuilder(args) already loads:
// 1. appsettings.json
// 2. appsettings.{Environment}.json
// 3. Environment variables
// 4. Command-line arguments
// But we can add more sources:
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "PRODAPI_")     // PRODAPI_ConnectionStrings__Default
    .AddCommandLine(args);
// ============================================================================
// LOGGING CONFIGURATION (Production-grade setup)
// ============================================================================
builder.Logging.ClearProviders(); // Remove defaults, add only what we need
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (!builder.Environment.IsDevelopment())
{
    // Structured logging for production (Serilog, Application Insights, etc.)
    builder.Logging.AddEventLog(); // Windows Event Log for on-premise
    // builder.Logging.AddApplicationInsights(); // Azure
}
// Set minimum levels per environment
builder.Logging.SetMinimumLevel(
    builder.Environment.IsDevelopment() ? LogLevel.Debug : LogLevel.Warning);
// ============================================================================
// CONTROLLERS & API EXPLORATION
// ============================================================================
builder.Services.AddControllers(options =>
{
    // Global filter - applies to ALL controllers
    options.Filters.Add<GlobalExceptionFilter>();
    
    // Model validation behavior
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(
        _ => "This field is required.");
})
.AddJsonOptions(options =>
{
    // JSON serialization options - critical for API consistency
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
// API Explorer - required for Swagger/OpenAPI to discover endpoints
builder.Services.AddEndpointsApiExplorer();
// ============================================================================
// OPENAPI / SWAGGER (Production-ready configuration)
// ============================================================================
builder.Services.AddOpenApi(); // .NET 10 built-in OpenAPI support
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Production API",
        Version = "v1",
        Description = "Enterprise-grade API with full authentication and documentation",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "api-support@company.com"
        }
    });
    // JWT Authentication in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// ============================================================================
// DATABASE CONTEXT (EF Core with resilience)
// ============================================================================
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        
        sqlOptions.CommandTimeout(30); // Seconds
        sqlOptions.MigrationsAssembly("ProductionApi");
    });
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
// ============================================================================
// AUTHENTICATION (JWT Bearer - production configuration)
// ============================================================================
var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings not configured.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero // Eliminate 5-minute default tolerance
    };
    
    // Event hooks for logging and custom behavior
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            // Log to structured logging
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            // Add claims transformation, audit logging
            return Task.CompletedTask;
        }
    };
});
// ============================================================================
// AUTHORIZATION (Policy-based - the right way)
// ============================================================================
builder.Services.AddAuthorization(options =>
{
    // Default policy - authenticated users only
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    // Custom policies for role-based access
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("PremiumCustomer", policy =>
        policy.RequireClaim("CustomerTier", "Gold", "Platinum"));
    options.AddPolicy("CanManageProducts", policy =>
        policy.RequireRole("Admin", "ProductManager")
              .RequireClaim("Permission", "Products.Write"));
    // Fallback policy for endpoints without [Authorize]
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
// ============================================================================
// CORS (Cross-Origin Resource Sharing)
// ============================================================================
builder.Services.AddCors(options =>
{
    // Named policy for specific origins
    options.AddPolicy("ProductionFrontend", policy =>
    {
        policy.WithOrigins(
                "https://app.company.com",
                "https://admin.company.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for cookies/auth headers
    });
    // Development policy - relaxed
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// ============================================================================
// RESPONSE COMPRESSION (Bandwidth optimization)
// ============================================================================
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable even for HTTPS (security consideration)
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "application/xml" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
// ============================================================================
// CACHING (Distributed - Redis for production)
// ============================================================================
if (builder.Environment.IsProduction())
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis");
        options.InstanceName = "ProductionApi_";
    });
}
else
{
    // In-memory cache for development
    builder.Services.AddDistributedMemoryCache();
}
// Register local memory cache for short-lived data
builder.Services.AddMemoryCache();
// ============================================================================
// HEALTH CHECKS (Production monitoring)
// ============================================================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "db", "sql" })
    .AddCheck<ExternalApiHealthCheck>("payment-api", tags: new[] { "external", "critical" });
// ============================================================================
// BACKGROUND SERVICES
// ============================================================================
builder.Services.AddHostedService<QueuedProcessorBackgroundService>();
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
// ============================================================================
// DEPENDENCY INJECTION (Business services)
// ============================================================================
// Scoped - most common for request-scoped services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
// Singleton - stateless, thread-safe services
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddSingleton<ITimeProvider, TimeProvider>(); // .NET 8+ built-in
// Transient - lightweight, stateless, created per injection
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddTransient<IPdfGenerator, PdfGeneratorService>();
// HttpClient with resilience patterns
builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    });
// ============================================================================
// OPTIONS PATTERN (Strongly-typed configuration)
// ============================================================================
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<RateLimitSettings>(
    builder.Configuration.GetSection("RateLimitSettings"));
// Validate options at startup, not at runtime
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
// ============================================================================
// RATE LIMITING (.NET 7+ - built-in, no external packages)
// ============================================================================
builder.Services.AddRateLimiter(options =>
{
    // Global limiter
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    // Named policies for specific endpoints
    options.AddPolicy("Login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5)
            }));
});
// ============================================================================
// API VERSIONING
// ============================================================================
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.UrlSegmentApiVersionReader(),
        new Asp.Versioning.HeaderApiVersionReader("X-API-Version"),
        new Asp.Versioning.MediaTypeApiVersionReader());
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
// ============================================================================
// HSTS & SECURITY HEADERS
// ============================================================================
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
        options.Preload = true;
    });
}
// ============================================================================
// KESTREL CONFIGURATION (Production tuning)
// ============================================================================
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB for file uploads
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    
    // HTTP/2 and HTTP/3
    options.ConfigureEndpointDefaults(endpoint =>
    {
        endpoint.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2AndHttp3;
    });
});
```

What just happened? We registered 20+ services across 15 categories. The DI container now knows how to create every object your application needs. But nothing executes yet — this is pure registration.

## Configuration Classes (The Strongly-Typed Options Pattern)

```c
// Configuration/JwtSettings.cs
using System.ComponentModel.DataAnnotations;
namespace ProductionApi.Configuration;
public class JwtSettings
{
    [Required]
    [StringLength(100, MinimumLength = 32)]
    public string SecretKey { get; set; } = string.Empty;
    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Range(1, 1440)]
    public int ExpirationMinutes { get; set; } = 60;
}
```
```c
// Configuration/EmailSettings.cs
namespace ProductionApi.Configuration;
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
```

Why Options pattern beats `Configuration["Key"]`:

- Compile-time type safety
- Validation at startup (fail fast)
- Bind to multiple configuration sources seamlessly
- Injectable via `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`

## Stage 3 & 4: Building the App and Configuring Middleware

## The Middleware Pipeline: Order Is Everything

Middleware processes requests in the order added, and responses in reverse order. Wrong order = security holes, broken auth, or silent failures.

```c
┌─────────────────────────────────────────────────────────────────┐
│  REQUEST flows down (top to bottom)                             │
│  RESPONSE flows up (bottom to top)                              │
├─────────────────────────────────────────────────────────────────┤
│  1. Exception Handler              ← Catches everything        │
│  2. HSTS (non-dev)                 ← Security headers           │
│  3. HTTPS Redirection              ← Force TLS                  │
│  4. CORS                           ← Before auth to allow       │
│  5. Static Files                   ← Short-circuit for files      │
│  6. Routing                        ← Match URL to endpoint      │
│  7. Rate Limiting                  ← Before expensive ops        │
│  8. Authentication                 ← Who are you?                │
│  9. Authorization                  ← What can you do?          │
│  10. Custom Middleware               ← Logging, metrics, etc.     │
│  11. Endpoints (Controllers/MinAPI)  ← Your code executes here  │
└─────────────────────────────────────────────────────────────────┘
```

## Complete Middleware Configuration

```c
// Program.cs — Stage 3 & 4 (continued from Stage 1 & 2)
var app = builder.Build();
// ============================================================================
// ENVIRONMENT-SPECIFIC MIDDLEWARE
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Production API v1");
        options.RoutePrefix = "swagger";
        options.DisplayRequestDuration();
    });
    
    app.UseDeveloperExceptionPage(); // Detailed error pages
}
else
{
    // PRODUCTION ONLY
    app.UseExceptionHandler("/error"); // Global exception handling endpoint
    app.UseHsts(); // Strict-Transport-Security header
}
// ============================================================================
// SECURITY MIDDLEWARE (Order critical)
// ============================================================================
app.UseHttpsRedirection(); // Redirect HTTP → HTTPS (must be before auth)
// Security headers middleware (custom - see below)
app.UseMiddleware<SecurityHeadersMiddleware>();
// ============================================================================
// CORS (Before authentication - preflight requests need to succeed)
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.UseCors("Development");
}
else
{
    app.UseCors("ProductionFrontend");
}
// ============================================================================
// COMPRESSION (Before static files and endpoints)
// ============================================================================
app.UseResponseCompression();
// ============================================================================
// STATIC FILES (Short-circuits pipeline if file found)
// ============================================================================
app.UseStaticFiles();
// ============================================================================
// ROUTING (Must be before auth and endpoints)
// ============================================================================
app.UseRouting();
// ============================================================================
// RATE LIMITING (After routing, before endpoints)
// ============================================================================
app.UseRateLimiter();
// ============================================================================
// AUTHENTICATION & AUTHORIZATION (Order is CRITICAL)
// ============================================================================
// Authentication MUST come before Authorization
// Authentication identifies the user
// Authorization checks permissions based on identity
app.UseAuthentication(); // "Who are you?" - sets HttpContext.User
app.UseAuthorization();  // "What can you do?" - checks policies
// ============================================================================
// CUSTOM MIDDLEWARE (After auth, before endpoints)
// ============================================================================
app.UseMiddleware<RequestTimingMiddleware>();     // Performance metrics
app.UseMiddleware<CorrelationIdMiddleware>();      // Distributed tracing
app.UseMiddleware<AuditLoggingMiddleware>();        // Security audit trail
// ============================================================================
// HEALTH CHECKS (Before endpoints, lightweight)
// ============================================================================
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration.ToString(),
                exception = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});
// ============================================================================
// ENDPOINTS (The actual API - last in the pipeline)
// ============================================================================
app.MapControllers(); // Attribute routing from controllers
// Minimal API endpoints (alternative or complementary)
app.MapGet("/api/v1/status", () => new { status = "healthy", timestamp = DateTime.UtcNow })
   .WithName("GetStatus")
   .WithOpenApi()
   .RequireRateLimiting("Login") // Apply rate limit to this endpoint
   .AllowAnonymous(); // Override default auth policy
// ============================================================================
// RUN
// ============================================================================
app.Run();
```

## Custom Middleware: Production-Grade Examples

## Security Headers Middleware

```c
// Middleware/SecurityHeadersMiddleware.cs
namespace ProductionApi.Middleware;
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers to every response
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
        
        // Content Security Policy (adjust for your frontend)
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'");
        
        await next(context);
    }
}
```

## Request Timing Middleware

```c
// Middleware/RequestTimingMiddleware.cs
using System.Diagnostics;
namespace ProductionApi.Middleware;
public class RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        
        logger.LogInformation(
            "Request {Method} {Path} started at {StartTime}",
            requestMethod, requestPath, DateTime.UtcNow);
        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            
            var level = statusCode >= 500 ? LogLevel.Error :
                       statusCode >= 400 ? LogLevel.Warning :
                       stopwatch.ElapsedMilliseconds > 1000 ? LogLevel.Warning :
                       LogLevel.Information;
            logger.Log(level,
                "Request {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                requestMethod, requestPath, stopwatch.ElapsedMilliseconds, statusCode);
        }
    }
}
```

## Correlation ID Middleware (Distributed Tracing)

```c
// Middleware/CorrelationIdMiddleware.cs
namespace ProductionApi.Middleware;
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    public async Task InvokeAsync(HttpContext context)
    {
        // Reuse existing correlation ID from incoming request
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }
        // Add to response and logging scope
        context.Response.Headers.Append(CorrelationIdHeader, correlationId.ToString());
        
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId.ToString()
        }))
        {
            await next(context);
        }
    }
}
```

## The Complete Production API: Putting It All Together

## Product Controller with All Features

```c
// Controllers/ProductsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ProductionApi.Configuration;
using ProductionApi.Models;
using ProductionApi.Services;
namespace ProductionApi.Controllers;
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Authorize] // Requires authentication by default
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IDistributedCache _cache;
    private readonly IOptionsSnapshot<JwtSettings> _jwtSettings;
    private readonly ILogger<ProductsController> _logger;
    public ProductsController(
        IProductService productService,
        IDistributedCache cache,
        IOptionsSnapshot<JwtSettings> jwtSettings,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _cache = cache;
        _jwtSettings = jwtSettings;
        _logger = logger;
    }
    // GET: api/v1/products
    [HttpGet]
    [MapToApiVersion("1.0")]
    [AllowAnonymous] // Override for public catalog
    [EnableRateLimiting("default")] // Apply rate limiting
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var cacheKey = $"products:{search}:{minPrice}:{maxPrice}:{page}:{pageSize}";
        
        // Try cache first
        var cached = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(cached))
        {
            return Ok(System.Text.Json.JsonSerializer.Deserialize<IEnumerable<ProductDto>>(cached));
        }
        var products = await _productService.SearchAsync(search, minPrice, maxPrice, page, pageSize);
        
        // Cache for 5 minutes
        await _cache.SetStringAsync(cacheKey,
            System.Text.Json.JsonSerializer.Serialize(products),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        return Ok(products);
    }
    // GET: api/v2/products - Version 2 with enhanced data
    [HttpGet]
    [MapToApiVersion("2.0")]
    [Authorize(Policy = "CanManageProducts")]
    public async Task<ActionResult<IEnumerable<ProductAdminDto>>> GetProductsV2(
        [FromQuery] ProductSearchRequest request)
    {
        var products = await _productService.SearchAdminAsync(request);
        return Ok(products);
    }
    // POST: api/v1/products
    [HttpPost]
    [Authorize(Policy = "CanManageProducts")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var product = await _productService.CreateAsync(request);
        
        // Invalidate cache
        await _cache.RemoveAsync("products:"); // Pattern-based invalidation would use Redis key scanning
        
        return CreatedAtAction(
            nameof(GetProductById),
            new { id = product.Id, version = "1.0" },
            product);
    }
    // GET: api/v1/products/5
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return NotFound(new { error = $"Product {id} not found" });
        return Ok(product);
    }
}
```

## Environment-Specific Configuration Deep Dive

## appsettings.json (Base Configuration)

```c
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ProductionDb;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;",
    "Redis": "localhost:6379,abortConnect=false"
  },
  "JwtSettings": {
    "SecretKey": "", // Override in environment-specific files or secrets
    "Issuer": "ProductionApi",
    "Audience": "ProductionClient",
    "ExpirationMinutes": 60
  },
  "EmailSettings": {
    "SmtpServer": "smtp.company.com",
    "Port": 587,
    "EnableSsl": true,
    "FromAddress": "noreply@company.com",
    "FromName": "Production API"
  },
  "RateLimitSettings": {
    "PermitLimit": 100,
    "WindowSeconds": 60
  }
}
```

## appsettings.Development.json

```c
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProductionDb_Dev;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "dev-secret-key-at-least-32-characters-long!",
    "ExpirationMinutes": 1440
  }
}
```

## appsettings.Production.json

```c
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-sql.company.com;Database=ProductionDb;User Id=api_user;Password=${DB_PASSWORD};Encrypt=True;TrustServerCertificate=False;"
  }
}
```

## Secret Manager (Development Secrets)

```c
# Store secrets outside source control
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "your-production-secret-key-here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-dev-connection-string"
dotnet user-secrets set "EmailSettings:Password" "smtp-password"
```

Access in code:

```c
var secretKey = builder.Configuration["JwtSettings:SecretKey"];
// Automatically pulls from user secrets in Development, env vars in Production
```

## Environment Variables (Production)

```c
# Linux/Container environment
export PRODAPI_JwtSettings__SecretKey="production-secret"
export PRODAPI_ConnectionStrings__DefaultConnection="Server=..."
export PRODAPI_EmailSettings__Password="smtp-password"
# The double underscore __ maps to configuration sections (hierarchy)
```

## Background Services and Hosted Services

```c
// Services/BackgroundTaskQueue.cs
namespace ProductionApi.Services;
public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
    public BackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<Func<CancellationToken, ValueTask>>();
    }
    public async ValueTask QueueAsync(Func<CancellationToken, ValueTask> workItem)
    {
        await _queue.Writer.WriteAsync(workItem);
    }
    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
```
```c
// Services/QueuedProcessorBackgroundService.cs
namespace ProductionApi.Services;
public class QueuedProcessorBackgroundService(
    IBackgroundTaskQueue taskQueue,
    ILogger<QueuedProcessorBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background service started");
        await foreach (var workItem in taskQueue.DequeueAsync(stoppingToken))
        {
            try
            {
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing background work item");
            }
        }
    }
}
```

Usage in controllers:

```c
[HttpPost("process-report")]
public async Task<IActionResult> QueueReport([FromServices] IBackgroundTaskQueue queue)
{
    await queue.QueueAsync(async token =>
    {
        // Long-running operation
        await GenerateReportAsync(token);
    });
return Accepted(new { message = "Report generation queued" });
}
```

## Health Checks with Custom Checks

```c
// Services/ExternalApiHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace ProductionApi.Services;
public class ExternalApiHealthCheck(HttpClient httpClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                "https://payment-api.company.com/health", 
                cancellationToken);
            
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Payment API responsive")
                : HealthCheckResult.Degraded($"Payment API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Payment API unreachable", ex);
        }
    }
}
```

## Before vs After: Program.cs Organization

## ❌ Bad: The Junk Drawer Program.cs

```c
// ANTI-PATTERN: 300 lines of unrelated code, no structure
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer("Server=...;Password=hardcoded123")); // ❌ Hardcoded secrets
builder.Services.AddAuthentication().AddJwtBearer(opt => { /* 50 lines */ });
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen();
// 100 more lines of random services...
var app = builder.Build();
app.UseSwagger();
app.UseAuthentication(); // ❌ Wrong order - after routing!
app.UseRouting();
app.UseAuthorization();  // ❌ Should be after authentication
app.MapControllers();
app.Run();
```

Problems:

- Hardcoded connection string with password
- Authentication after routing (breaks endpoint authorization)
- No environment-specific configuration
- No logging configuration
- No security headers
- No rate limiting
- No health checks
- No CORS
- 300-line file with zero organization

## ✅ Good: Structured, Production-Ready Program.cs

```c
// PATTERN: Clear sections, separation of concerns, security-first
using ProductionApi.Configuration;
using ProductionApi.Data;
using ProductionApi.Middleware;
using ProductionApi.Services;
var builder = WebApplication.CreateBuilder(args);
// ─── Configuration ─────────────────────────────────────────
ConfigureConfigurationSources(builder);
ConfigureLogging(builder);
ConfigureOptions(builder);
// ─── Security ──────────────────────────────────────────────
ConfigureAuthentication(builder);
ConfigureAuthorization(builder);
ConfigureCors(builder);
// ─── Infrastructure ────────────────────────────────────────
ConfigureDatabase(builder);
ConfigureCaching(builder);
ConfigureHttpClients(builder);
// ─── API Features ──────────────────────────────────────────
ConfigureControllers(builder);
ConfigureApiDocumentation(builder);
ConfigureVersioning(builder);
ConfigureRateLimiting(builder);
// ─── Health & Monitoring ─────────────────────────────────
ConfigureHealthChecks(builder);
ConfigureTelemetry(builder);
// ─── Business Services ─────────────────────────────────────
ConfigureBusinessServices(builder);
var app = builder.Build();
ConfigureMiddlewarePipeline(app);
app.Run();
// ─── Configuration Methods ─────────────────────────────────
static void ConfigureConfigurationSources(WebApplicationBuilder builder)
{
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables(prefix: "PRODAPI_")
        .AddCommandLine(args);
}
static void ConfigureLogging(WebApplicationBuilder builder)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
    
    if (!builder.Environment.IsDevelopment())
    {
        builder.Logging.AddEventLog();
    }
}
static void ConfigureOptions(WebApplicationBuilder builder)
{
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings"));
    builder.Services.Configure<EmailSettings>(
        builder.Configuration.GetSection("EmailSettings"));
    
    builder.Services.AddOptions<JwtSettings>()
        .ValidateDataAnnotations()
        .ValidateOnStart();
}
static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    var jwtSettings = builder.Configuration
        .GetSection("JwtSettings")
        .Get<JwtSettings>()!;
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
            };
        });
}
static void ConfigureAuthorization(WebApplicationBuilder builder)
{
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        options.AddPolicy("CanManageProducts", policy => 
            policy.RequireRole("Admin", "ProductManager"));
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });
}
static void ConfigureCors(WebApplicationBuilder builder)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("Production", policy =>
            policy.WithOrigins("https://app.company.com")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });
}
static void ConfigureDatabase(WebApplicationBuilder builder)
{
    var connectionString = builder.Configuration
        .GetConnectionString("DefaultConnection")!;
    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(3);
            sqlOptions.CommandTimeout(30);
        });
    });
}
static void ConfigureCaching(WebApplicationBuilder builder)
{
    if (builder.Environment.IsProduction())
    {
        builder.Services.AddStackExchangeRedisCache(options =>
            options.Configuration = builder.Configuration.GetConnectionString("Redis"));
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
    }
    builder.Services.AddMemoryCache();
}
static void ConfigureHttpClients(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<IPaymentGatewayClient, PaymentGatewayClient>()
        .AddStandardResilienceHandler();
}
static void ConfigureControllers(WebApplicationBuilder builder)
{
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
}
static void ConfigureApiDocumentation(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo 
        { 
            Title = "Production API", 
            Version = "v1" 
        });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "Authorization"
        });
    });
}
static void ConfigureVersioning(WebApplicationBuilder builder)
{
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddMvc().AddApiExplorer();
}
static void ConfigureRateLimiting(WebApplicationBuilder builder)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });
}
static void ConfigureHealthChecks(WebApplicationBuilder builder)
{
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database");
}
static void ConfigureTelemetry(WebApplicationBuilder builder)
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
            tracing.AddSource("ProductionApi");
        });
}
static void ConfigureBusinessServices(WebApplicationBuilder builder)
{
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
    builder.Services.AddHostedService<QueuedProcessorBackgroundService>();
}
static void ConfigureMiddlewarePipeline(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }
    app.UseHttpsRedirection();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseCors("Production");
    app.UseResponseCompression();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<RequestTimingMiddleware>();
    app.MapHealthChecks("/health");
    app.MapControllers();
}
```

## Common Mistakes and How to Avoid Them

## ❌ Mistake 1: Wrong Middleware Order

```c
// WRONG: Authorization before Authentication
app.UseAuthorization();
app.UseAuthentication();
// WRONG: Routing after Authentication
app.UseAuthentication();
app.UseRouting();
// WRONG: CORS after Authentication (breaks preflight)
app.UseAuthentication();
app.UseCors();
```

Correct order:

```c
app.UseRouting();        // 1. Route matching
app.UseCors();           // 2. CORS before auth
app.UseAuthentication(); // 3. Who are you?
app.UseAuthorization();  // 4. What can you do?
app.MapControllers();    // 5. Endpoints
```

## ❌ Mistake 2: Hardcoded Secrets

```c
// WRONG
builder.Services.AddDbContext<AppDbContext>(opt => 
    opt.UseSqlServer("Server=...;Password=SuperSecret123!"));
// RIGHT
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");
```

## ❌ Mistake 3: Incorrect DI Lifetimes

```c
// WRONG: Singleton depending on Scoped
builder.Services.AddSingleton<ICacheService, CacheService>(); // Singleton
// CacheService constructor takes AppDbContext (Scoped) — CRASH at runtime!
// RIGHT: Match lifetimes or use factory
builder.Services.AddScoped<ICacheService, CacheService>(); // Same lifetime as DbContext
// OR use IServiceProvider in singleton
builder.Services.AddSingleton<ICacheService>(provider =>
{
    // Create scope to resolve scoped services
    using var scope = provider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    return new CacheService(dbContext);
});
```

## ❌ Mistake 4: Giant Program.cs

```c
// WRONG: 500 lines in one file
// RIGHT: Extract to static methods (shown in Good example above)
// OR: Use extension methods on IServiceCollection and IApplicationBuilder
```

## ❌ Mistake 5: Not Validating Options at Startup

```c
// WRONG: Options validated at first use — runtime failure
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
// RIGHT: Fail at startup if configuration is invalid
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection("JwtSettings"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

## .NET 10 Forward-Compatible Practices

1. Use `TimeProvider` instead of `DateTime.Now/DateTime.UtcNow` — Injectable, testable, handles time zones correctly:
```c
builder.Services.AddSingleton(TimeProvider.System); // .NET 8+ built-in
// In services
public class OrderService(TimeProvider timeProvider)
{
    public void CreateOrder()
    {
        var now = timeProvider.GetUtcNow(); // Testable!
    }
}
```

2\. Keyd services for multiple implementations:

```c
builder.Services.AddKeyedSingleton<ICacheService, RedisCacheService>("distributed");
builder.Services.AddKeyedSingleton<ICacheService, MemoryCacheService>("local");
// Usage
public class ProductService([FromKeyedServices("distributed")] ICacheService cache)
```

3\. Required members for configuration classes:

```c
public class JwtSettings
{
    public required string SecretKey { get; set; } // Compiler enforces initialization
    public required string Issuer { get; set; }
}
```

4\. Primary constructors for services:

```c
public class ProductService(
    AppDbContext context,
    ILogger<ProductService> logger,
    TimeProvider timeProvider) : IProductService
{
    // No boilerplate field assignments needed
}
```

## Production-Ready Checklist

Before deploying to production, verify:

## Configuration

- \[ \] No hardcoded secrets in source code
- \[ \] `appsettings.Production.json` exists and is valid
- \[ \] Connection strings use encrypted credentials or managed identity
- \[ \] JWT secret key is 32+ characters, rotated regularly
- \[ \] Options pattern with `ValidateOnStart()` enabled

## Security

- \[ \] HTTPS redirection enabled (non-development)
- \[ \] HSTS enabled with appropriate max-age
- \[ \] Security headers middleware configured
- \[ \] CORS policy restricts to known origins (no `AllowAnyOrigin` in production)
- \[ \] Authentication middleware before Authorization
- \[ \] Fallback policy requires authentication by default

## Performance

- \[ \] Response compression enabled
- \[ \] Distributed caching configured (Redis in production)
- \[ \] Rate limiting configured with appropriate limits
- \[ \] Kestrel limits configured for expected payload sizes
- \[ \] JSON serialization optimized (camelCase, null handling)

## Monitoring

- \[ \] Health checks endpoint configured with database check
- \[ \] Structured logging configured (not just console in production)
- \[ \] Request timing middleware added
- \[ \] Correlation IDs for distributed tracing
- \[ \] OpenTelemetry configured for observability

## Resilience

- \[ \] Database retry logic enabled (`EnableRetryOnFailure`)
- \[ \] HTTP client resilience handlers configured
- \[ \] Global exception handler configured (not developer pages)
- \[ \] Background services have proper error handling

## Conclusion

Program.cs is the runtime contract of your application. Every line matters. Every ordering decision has consequences. Every service registration affects memory, performance, and behavior.

The minimal hosting model in.NET 6+ didn’t just reduce ceremony — it made the startup sequence visible and intentional. In.NET 10, with enhanced configuration binding, AOT support, and refined middleware patterns, that intentionality matters more than ever.

The structured approach in this article — clear sections, extracted methods, environment-aware configuration, and security-first middleware ordering — isn’t just cleaner. It’s what separates applications that survive Black Friday traffic from those that wake you up at 2 AM.

Your Program.cs is your application’s DNA. Code it with the respect it deserves.

### If this guide helped you understand the ASP.NET Core startup pipeline, clap and follow for more deep dives into.NET architecture, production hardening, and building APIs that scale. What’s the most confusing part of Program.cs for your team? Drop a comment — I read every one.

## A message from our Founder

Hey, [Sunil](https://linkedin.com/in/sunilsandhu) here. I wanted to take a moment to thank you for reading until the end and for being a part of this community. Did you know that our team run these publications as a volunteer effort to over 3.5m monthly readers? We don’t receive any funding, we do this to support the community.

If you want to show some love, please take a moment to follow me on [LinkedIn](https://linkedin.com/in/sunilsandhu), [TikTok](https://tiktok.com/@messyfounder), [Instagram](https://instagram.com/sunilsandhu). You can also subscribe to our [weekly newsletter](https://newsletter.plainenglish.io/). And before you go, don’t forget to clap and follow the writer️!