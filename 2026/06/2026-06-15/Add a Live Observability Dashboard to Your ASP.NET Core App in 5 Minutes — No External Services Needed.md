# Add a Live Observability Dashboard to Your ASP.NET Core App in 5 Minutes — No External Services Needed
*No Grafana. No Datadog. No YAML. Just visibility.*

You've shipped your ASP.NET Core API. It's live. And then your product manager pings you: *"Something seems slow — can you check?"*

You open the logs. Wall of text. No correlation IDs. No timing. No idea which request threw that exception at 2 AM.

Sound familiar?

That's the problem **AsGuard** solves.

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*SBh92Rr_oUxJ3YxV6OKJBw.png)

## What Is AsGuard?

AsGuard is a lightweight NuGet package that plugs directly into your ASP.NET Core middleware pipeline and gives you:

- **Full HTTP request/response logging** with timing, method, status, and body capture
- **Exception tracking** with stack traces, severity, and trends — no Sentry account required
- **Host** `**ILogger**` **capture** — your existing `LogWarning` and `LogError` calls get stored and searchable
- **A built-in live dashboard** with dark/light mode, SSE-powered real-time updates, and filtering
- **APM trace timelines** — auto-tracks EF Core queries and `HttpClient` calls with Gantt charts
- **Alerting** for queue pressure, exception spikes, and persistence failures
- **Sensitive data masking** via `[AsGuardMasked]` attributes and configurable header redaction

The entire thing runs inside your app. No external services. No Docker compose file with six containers. No licensing tiers. Just a NuGet package and four lines of configuration.

## Getting It Running (For Real, Under 5 Minutes)

## Step 1 — Install the package

```c
dotnet add package AsGuard
```

## Step 2 — Configure in Program.cs

```c
using AsGuard.Extensions;
using AsGuard.Domain.RequestLogging;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRequestLogging(options =>
{
    options.DatabaseProvider = LoggingDatabaseProvider.Sqlite;
    options.ConnectionString = "Data Source=asguard.db";
    options.DashboardRoute = "/logs";
    options.DashboardUsername = builder.Configuration["AsGuard:Username"] ?? "admin";
    options.DashboardPassword = builder.Configuration["AsGuard:Password"] ?? "change-me";
    options.EnableExceptionLogging = true;
    options.CaptureHostLogs = true;
    options.LogResponseBody = true;
    options.LogRequestBody = true;
});
var app = builder.Build();
```

## Step 3 — Register the middleware

The placement matters. Add `UseRequestLogging()` after HTTPS/CORS but *before* auth and your endpoints:

```c
app.UseHttpsRedirection();
app.UseExceptionHandler();
app.UseRequestLogging(); // ← right here

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

## Step 4 — Hit the dashboard

Run your app, open a browser, and navigate to:

```c
https://localhost:<port>/logs
```

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*v-V8prJ67ViOcjK-G7qrvw.png)

AsGuard Dashboard

Enter your credentials and you'll see a live dashboard with every request your app has handled. That's it.

## The Middleware Order Actually Matters

One gotcha worth knowing upfront: where you place `UseRequestLogging()` in the pipeline determines what gets captured.

```c
[HTTP Request In]
       │
       ▼
┌──────────────────────┐
│ Exception Handler    │  ← register first so AsGuard can intercept exceptions
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│ HTTPS / CORS / HSTS  │
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│  UseRequestLogging() │  ← HERE: correlation IDs generated, body stream opened
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│  Auth & Authorization│  ← 401s and 403s will still be logged
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│  Your Endpoints      │
└──────────────────────┘
```

Placing it after auth means you lose logs for unauthorized requests. Placing it before exception handlers means unhandled exceptions won't have their HTTP context correctly linked. The order shown above gets you everything.

## Choosing a Database

AsGuard supports four providers out of the box:

- **SQLite** — zero setup, file-based; perfect for local dev, small apps, and quick demos
- **SQL Server** — the natural fit for production on Azure or Windows-hosted workloads
- **PostgreSQL** — ideal for Linux environments and cloud-native stacks
- **In-Memory** — no persistence, no config; great for integration tests and ephemeral environments

Switching is one line:

```c
// PostgreSQL
options.DatabaseProvider = LoggingDatabaseProvider.PostgreSql;
options.ConnectionString = "Host=localhost;Database=asguard;Username=postgres;Password=pass";
// In-Memory (no persistence, great for tests)
options.DatabaseProvider = LoggingDatabaseProvider.InMemory;
options.MaxInMemoryEntries = 10000;
```

No migrations to run manually. AsGuard auto-creates its tables on startup.

## Masking Sensitive Data

If you capture request bodies (and you should, for debugging), you don't want passwords showing up in your log viewer. AsGuard gives you two ways to handle this.

**Per-property attribute:**

```c
public class LoginRequest
{
    public string Username { get; set; }
    [AsGuardMasked]
    public string Password { get; set; } // stored as "[REDACTED]"
}
```

**Global key list in config:**

```c
options.SensitiveBodyKeys.Add("creditCardNumber");
options.SensitiveBodyKeys.Add("ssn");
options.SensitiveBodyKeys.Add("token");
```

This runs at the middleware level before anything hits the database — you're never storing what you shouldn't.

## What You'll Actually Use It For

## Debugging slow requests in development

The request log view shows method, path, status code, and duration for every call. You can filter by status, search by path, and click into any row to see the full request/response bodies, headers, and the correlation ID chain.

## Tracking down production exceptions

Exception logging captures the full stack trace, the HTTP context it occurred in, the severity, and when it happened. The dashboard includes trend charts so you can see if your error rate just spiked after a deploy.

## Capturing ILogger output

With `CaptureHostLogs = true`, any `LogWarning` or `LogError` your existing services emit gets stored alongside the HTTP logs. You don't need to change a single line of your existing logging code — AsGuard just intercepts the host `ILogger` pipeline.

## APM for EF Core and HttpClient

AsGuard auto-instruments EF Core queries and outbound `HttpClient` calls, showing them as Gantt-style trace timelines per request. If one endpoint is slow because it's firing 12 database queries, you'll see exactly which ones and how long each took.

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*4gUep3Khoz0YrPQYcybigw.png)

Request Details — APM

## Seeing what's live, right now

The dashboard uses Server-Sent Events for real-time updates — no page refresh, no polling interval to configure. While you're load testing or manually poking an endpoint, the request log updates in front of you.

## Production Notes

A few things to know before deploying:

**Change the default credentials.** AsGuard actively rejects the literal string `"admin"` as a username. Store credentials in environment variables or user secrets:

```c
# environment variable approach
ASGUARD__USERNAME=your_secure_user
ASGUARD__PASSWORD=your_secure_password
```

**The write path is non-blocking.** AsGuard uses a queue-based architecture — logs are written to an in-process channel and flushed asynchronously. Your API response times are not affected by database write latency.

**Set up retention policies.** For high-traffic apps, configure auto-cleanup so the log table doesn't grow unbounded:

```c
options.RetentionDays = 30;
```

**SQLite is fine for low-to-medium traffic.** For anything handling hundreds of requests per second in production, switch to PostgreSQL or SQL Server.

## The Bigger Picture

Most.NET teams reach for Sentry for exceptions, Datadog or Azure Monitor for metrics, and a custom Kibana setup for logs. That's three services, three SDKs, three dashboards, and three monthly bills.

AsGuard doesn't replace that stack for large-scale production systems. But for the majority of apps — internal tools, SaaS products under 100k requests/day, staging environments, side projects — it gives you 80% of the observability value at 0% of the infrastructure overhead.

Install it in your existing app today and you'll have a searchable, live view of every request and exception by the time your coffee's ready.

**GitHub:** [mahmood-alsarraj/asguard](https://github.com/mahmood-alsarraj/asguard)

**NuGet:** `dotnet add package AsGuard`

*If this saved you a debugging session, give the repo a star — it helps.*

