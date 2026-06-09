---
title: ASP.NET Health Checks with Aspire and OpenShift
created: 2026-05-09
modified: 2026-05-09
tags:
  - aspnet-core
  - health-checks
  - aspire
  - openshift
  - observability
  - kubernetes
uid: f7c9226b-c829-495d-a4dd-0bbf7ab7db4e
---

# ASP.NET Health Checks Tutorial: Aspire & OpenShift Setup

A comprehensive guide to implementing health check endpoints in ASP.NET Core applications targeting OpenShift with .NET Aspire orchestration.

## Table of Contents

1. [Overview](#overview)
2. [Quick Start](#quick-start)
3. [Core Health Checks Setup](#core-health-checks-setup)
4. [SQL Server Health Checks](#sql-server-health-checks)
5. [Aspire Integration](#aspire-integration)
6. [OpenShift Deployment](#openshift-deployment)
7. [Tips & Best Practices](#tips--best-practices)
8. [Common Gotchas](#common-gotchas)
9. [Troubleshooting](#troubleshooting)

---

## Overview

Health checks are critical for container orchestration platforms like OpenShift. They enable Kubernetes/OpenShift to:
- **Detect unhealthy pods** and restart them automatically
- **Route traffic** only to healthy instances
- **Manage rolling deployments** safely
- **Monitor application readiness** before accepting traffic

This guide covers setting up health checks in ASP.NET Core with:
- **Readiness probe** (`/health`) - Is the app ready for traffic?
- **Liveness probe** (`/alive`) - Is the app still running?
- **SQL Server connectivity** - Is the database reachable?

---

## Quick Start

### 1. Install NuGet Packages

```bash
dotnet add package Microsoft.Extensions.Diagnostics.HealthChecks
dotnet add package AspNetCore.HealthChecks.SqlServer
```

### 2. Configure in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add health checks with SQL Server check
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]);

var app = builder.Build();

// Map health check endpoints
var healthChecks = app.MapGroup("");
healthChecks.MapHealthChecks("/health");
healthChecks.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});

app.Run();
```

### 3. Add Connection String to appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Integrated Security=true;"
  }
}
```

### 4. Configure in Aspire AppHost (Program.cs)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health");

builder.Build().Run();
```

---

## Core Health Checks Setup

### Understanding Health Check Endpoints

ASP.NET Core provides flexible health check configuration with two common patterns:

#### Pattern 1: Single Endpoint (Simple)

```csharp
app.MapHealthChecks("/health");
```

Returns 200 OK if all checks pass, 503 Service Unavailable otherwise.

#### Pattern 2: Separate Readiness & Liveness (Recommended)

```csharp
// Readiness: All checks must pass
app.MapHealthChecks("/health");

// Liveness: Only "live" tagged checks
app.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});
```

**Why use two endpoints?**
- **Readiness** (`/health`): Wait until app is fully initialized before routing traffic
- **Liveness** (`/alive`): Quick check that app process is responsive (no slow checks)

### Adding Custom Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddCheck("startup", async context =>
    {
        // Custom check - verify critical initialization
        var isReady = await MyInitializationService.IsReadyAsync();
        return isReady 
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Startup not complete");
    });
```

### Health Check Response Format

Default response (JSON):
```json
{
  "status": "Healthy",
  "checks": {
    "self": {
      "status": "Healthy"
    },
    "MyDatabase": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    }
  },
  "totalDuration": "00:00:00.0234567"
}
```

---

## SQL Server Health Checks

### Why SQL Server Checks Matter

SQL Server connectivity is often the critical path for readiness. A health check ensures:
- Database credentials are valid
- Network connectivity exists
- Database is accessible and responding

### Setup with AspNetCore.HealthChecks.SqlServer

#### Installation

```bash
dotnet add package AspNetCore.HealthChecks.SqlServer
```

#### Basic Configuration

```csharp
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);
```

**Connection String Formats:**

```csharp
// SQL Server (Windows Auth)
"Server=(local);Database=MyApp;Integrated Security=true;"

// SQL Server (SQL Auth)
"Server=sql-server.example.com;Database=MyApp;User Id=sa;Password=MyPassword;"

// Azure SQL
"Server=tcp:myserver.database.windows.net,1433;Initial Catalog=MyDb;Persist Security Info=False;User ID=sqladmin;Password=MyPassword;Encrypt=True;Connection Timeout=30;"

// From environment variable (Aspire/Container)
Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING") 
  ?? "Server=localhost;Database=MyApp;Integrated Security=true;"
```

#### Advanced Configuration with Timeout & Query

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" },
        timeout: TimeSpan.FromSeconds(5), // Critical for OpenShift
        configure: setup =>
        {
            // Optional: Custom query instead of SELECT 1
            setup.CommandText = "SELECT 1";
        }
    );
```

### 🎯 Critical: Set Appropriate Timeouts

```csharp
// DO THIS - Set explicit timeout
.AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(5))

// NOT THIS - Uses default 100ms timeout (too short!)
.AddSqlServer(connectionString)
```

**Timeout Behavior:**
- OpenShift waits for a response within `initialDelaySeconds` + `timeoutSeconds`
- If health check hangs, pod becomes unresponsive
- Recommended: **3-5 second timeout** for database checks

### Testing SQL Server Health Check

```bash
# From inside pod/container
curl http://localhost:5000/health
curl http://localhost:5000/alive

# Check response status
curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health
# Should return: 200 (healthy) or 503 (unhealthy)
```

---

## Aspire Integration

### What Aspire Does with Health Checks

.NET Aspire uses health checks for:
1. **Service Discovery** - Waiting for dependent services to be ready
2. **Startup Orchestration** - Starting services in correct order
3. **Development Loop** - Automatic restart on health check failure
4. **Deployment Automation** - Container readiness/liveness probes

### Configuring Health Checks in Aspire

#### Step 1: Configure Health Checks in Project

Your ASP.NET service (Program.cs):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]);

var app = builder.Build();

var healthChecks = app.MapGroup("");
healthChecks.MapHealthChecks("/health");
healthChecks.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});

app.Run();
```

#### Step 2: Declare Health Check in Aspire AppHost

```csharp
// In AppHost/Program.cs

var builder = DistributedApplication.CreateBuilder(args);

// Add your project with health check
var api = builder
    .AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health");  // <-- This is key

// Database reference
var db = builder
    .AddSqlServer("sql")
    .AddDatabase("mydb");

// Link database to API
api.WithReference(db);

var app = builder.Build();
app.Run();
```

#### Step 3: Configure Startup Ordering with WaitFor

```csharp
var db = builder
    .AddSqlServer("sql")
    .AddDatabase("mydb");

var api = builder
    .AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(db)
    .WaitFor(db);  // <-- Wait for database health before starting API

var web = builder
    .AddProject<Projects.MyWeb>("web")
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api);  // <-- Wait for API health before starting web

builder.Build().Run();
```

**Important:** `WaitFor()` uses the health check endpoint to determine readiness!

### Advanced: Custom Health Check Policies

```csharp
builder.Services.AddRequestTimeouts(timeouts =>
    timeouts.AddPolicy("HealthChecks", TimeSpan.FromSeconds(5)));

builder.Services.AddOutputCache(configureOptions: static caching =>
    caching.AddPolicy("HealthChecks",
        build: static policy => policy.Expire(TimeSpan.FromSeconds(10))));

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

// In endpoints
var healthChecks = app.MapGroup("");
healthChecks
    .CacheOutput("HealthChecks")
    .WithRequestTimeout("HealthChecks")
    .MapHealthChecks("/health");
```

---

## OpenShift Deployment

### OpenShift Health Check Concepts

OpenShift/Kubernetes uses **probes** to manage pod lifecycle:

| Probe | Purpose | Endpoint | Timeout |
|-------|---------|----------|---------|
| **Startup Probe** | Is app initializing? (runs once) | `/health` | 30s default |
| **Readiness Probe** | Ready for traffic? (continuous) | `/health` | 1s default |
| **Liveness Probe** | Still alive? (continuous) | `/alive` | 1s default |

### Deployment Manifest Example

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    metadata:
      labels:
        app: myapp
    spec:
      containers:
      - name: myapp
        image: myregistry.azurecr.io/myapp:latest
        ports:
        - containerPort: 8080
          name: http
        env:
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: connectionstring
        
        # Startup Probe: Wait for app to initialize (runs once)
        startupProbe:
          httpGet:
            path: /health
            port: http
          failureThreshold: 30  # 30 * 10s = 300s total timeout
          periodSeconds: 10
        
        # Readiness Probe: Is app ready for traffic?
        readinessProbe:
          httpGet:
            path: /health
            port: http
          initialDelaySeconds: 5
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        
        # Liveness Probe: Is app still responsive?
        livenessProbe:
          httpGet:
            path: /alive
            port: http
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 3
          failureThreshold: 3
```

### OpenShift-Specific: Container Runtime Liveness Probe

If using OpenShift's built-in container health checks (without explicit probes):

```yaml
livenessProbe:
  exec:
    command:
    - /bin/sh
    - -c
    - curl -f http://localhost:8080/alive || exit 1
  initialDelaySeconds: 30
  periodSeconds: 10
  timeoutSeconds: 3
```

### Deploying with Aspire to OpenShift

If using Aspire's deployment tools:

```bash
# Generate manifests
dotnet publish --self-contained false

# Deploy to OpenShift
oc apply -f ./manifests
```

Or with Aspire deployment extension:

```csharp
// In AppHost Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health");

builder
    .AddExternalHttpEndpoints() // For OpenShift ingress
    .Build()
    .Run();
```

---

## Tips & Best Practices

### 1. Use Separate Readiness and Liveness Endpoints

✅ **DO:**
```csharp
// /health: All checks (db, cache, etc)
app.MapHealthChecks("/health");

// /alive: Fast checks only (self check)
app.MapHealthChecks("/alive", new() 
{ 
    Predicate = r => r.Tags.Contains("live")
});
```

❌ **DON'T:**
```csharp
// Single endpoint for everything - risks killing pod during DB issues
app.MapHealthChecks("/health");
```

### 2. Always Set Explicit Timeouts

✅ **DO:**
```csharp
.AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(5))
```

❌ **DON'T:**
```csharp
.AddSqlServer(connectionString) // Uses 100ms default - too short!
```

### 3. Tag Health Checks for Granular Control

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live", "startup"])
    .AddSqlServer(connectionString, tags: new[] { "db", "readiness" });

// Filter by tag in endpoint
app.MapHealthChecks("/health", new()
{
    Predicate = r => !r.Tags.Contains("internal")
});
```

### 4. Make Liveness Checks Fast

❌ **DON'T** do database queries in liveness probe:
```csharp
// This will kill the pod if DB is slow!
app.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);  // Added to /alive - BAD
```

✅ **DO** keep liveness checks lightweight:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

app.MapHealthChecks("/alive", new()
{
    Predicate = r => r.Tags.Contains("live")
});
```

### 5. Handle Database Connection Pooling

```csharp
// Connection strings that work well with OpenShift
.AddSqlServer(
    connectionString: connectionString,
    timeout: TimeSpan.FromSeconds(5),
    configure: setup =>
    {
        // Don't pool in health checks
        setup.ConnectionString += ";Connection Pooling=false;";
    }
);
```

### 6. Use Environment-Specific Configuration

```csharp
var connectionString = builder.Environment.IsProduction()
    ? builder.Configuration["ConnectionStrings:Production"]
    : builder.Configuration["ConnectionStrings:Development"];

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);
```

### 7. Configure Caching for Health Endpoints (Production)

```csharp
builder.Services.AddOutputCache(caching =>
    caching.AddPolicy("HealthChecks",
        policy => policy.Expire(TimeSpan.FromSeconds(10))));

app.MapHealthChecks("/health")
    .CacheOutput("HealthChecks");
```

### 8. Monitor Health Check Performance

Add logging:
```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddCheck("custom", async context =>
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await MyCheck();
        sw.Stop();
        
        if (sw.ElapsedMilliseconds > 1000)
            _logger.LogWarning($"Health check took {sw.ElapsedMilliseconds}ms");
        
        return result;
    });
```

---

## Common Gotchas

### ❌ Gotcha 1: Missing AddHealthChecks()

```csharp
// This throws - MapHealthChecks requires AddHealthChecks
app.MapHealthChecks("/health");

// Should be:
builder.Services.AddHealthChecks();
app.MapHealthChecks("/health");
```

### ❌ Gotcha 2: SQL Server Check with Wrong Port

OpenShift containers often use non-standard SQL Server ports:

```csharp
// ❌ Assumes SQL Server on default 1433
.AddSqlServer("Server=sqlserver;Database=MyApp;...")

// ✅ Specify port explicitly
.AddSqlServer("Server=sqlserver,1433;Database=MyApp;...")

// ✅ From environment variable
var server = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "localhost";
var port = Environment.GetEnvironmentVariable("SQL_PORT") ?? "1433";
var connectionString = $"Server={server},{port};Database=MyApp;...";
.AddSqlServer(connectionString)
```

### ❌ Gotcha 3: Health Checks Block Startup

If health checks fail during startup, the app may never become ready:

```csharp
// ❌ Required database check - blocks startup if DB unavailable
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

// ✅ Make checks optional or have fallback
builder.Services.AddHealthChecks()
    .AddCheck("optional-db", async context =>
    {
        try
        {
            return await CheckDatabaseAsync();
        }
        catch
        {
            return HealthCheckResult.Degraded("DB unavailable");
        }
    });
```

### ❌ Gotcha 4: Timeout Too Short for Database

```csharp
// ❌ 100ms timeout - often fails with transient issues
.AddSqlServer(connectionString)

// ✅ 5s timeout - allows for network jitter
.AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(5))
```

### ❌ Gotcha 5: Including Slow Checks in Liveness

```csharp
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, tags: new[] { "live" })  // ❌ BAD
    .AddCheck("cache", CheckCacheAsync, tags: new[] { "live" });

// ✅ Only self-check in liveness
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddSqlServer(connectionString);  // Readiness only
```

### ❌ Gotcha 6: Aspire Health Check Not Working

```csharp
// ❌ Health check endpoint not exposed
var api = builder.AddProject<Projects.MyApi>("api");

// ✅ Declare health check
var api = builder
    .AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health");
```

### ❌ Gotcha 7: Connection String Injection Fails

```yaml
# ❌ Wrong - connection string not passed
env:
- name: ConnectionStrings__DefaultConnection
  value: "Server=..."
```

```csharp
// ✅ Application must read from correct env var
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
```

---

## Troubleshooting

### Issue: Health Check Endpoint Returns 503 (Unhealthy)

**Check 1: Verify endpoint is callable**
```bash
kubectl exec -it pod-name -- curl http://localhost:8080/health
```

**Check 2: Look at logs**
```bash
kubectl logs pod-name | grep -i health
```

**Check 3: Test SQL Server connection**
```bash
# From pod
echo "Server=sqlserver,1433;Database=MyApp;User Id=sa;Password=..." | /opt/mssql-tools/bin/sqlcmd
```

**Check 4: Verify timeout is sufficient**
```csharp
// Increase timeout
.AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(10))
```

### Issue: Pod Keeps Restarting (Liveness Probe Failing)

**Symptom:** CrashLoopBackOff status

**Solution:**
- Liveness probe likely running heavy checks (database, etc.)
- Move all heavy checks to readiness only
- Keep `/alive` for lightweight checks only

```csharp
// ✅ Fix
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

// Don't add database to liveness
.AddSqlServer(connectionString);  // Goes to /health only
```

### Issue: Health Check Takes Too Long

**Symptoms:** Startup timeout, frequent restarts

**Solutions:**

1. Check database timeout:
```csharp
.AddSqlServer(connectionString, timeout: TimeSpan.FromSeconds(5))
```

2. Add startup probe to give more time:
```yaml
startupProbe:
  httpGet:
    path: /health
    port: http
  failureThreshold: 60  # 60 * 5s = 300s total
  periodSeconds: 5
```

3. Cache health check results (production):
```csharp
builder.Services.AddOutputCache(c => 
    c.AddPolicy("health", p => p.Expire(TimeSpan.FromSeconds(10))));

app.MapHealthChecks("/health").CacheOutput("health");
```

### Issue: Health Check Works Locally, Fails in Container

**Common causes:**

1. **Connection string is localhost**
```csharp
// ❌ Works locally, fails in container
.AddSqlServer("Server=localhost;Database=MyApp;...")

// ✅ Use environment variable
var server = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "localhost";
.AddSqlServer($"Server={server};Database=MyApp;...")
```

2. **Network connectivity**
```bash
# Test from pod
kubectl exec -it pod-name -- curl -v http://sqlserver:1433
```

3. **Credentials in appsettings.json**
```csharp
// ✅ Use secrets
var password = builder.Configuration["DatabasePassword"];
var connectionString = $"Server=sqlserver;Database=MyApp;User Id=sa;Password={password};";
.AddSqlServer(connectionString)
```

### Debug: Detailed Health Check Logging

```csharp
builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddConsole();
});

builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddCheck("debug", async context =>
    {
        _logger.LogInformation("Health check running at {time}", DateTime.UtcNow);
        return HealthCheckResult.Healthy();
    });
```

---

## Complete Example: Production-Ready Setup

### Program.cs

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"])
    .AddSqlServer(
        connectionString: builder.Configuration["ConnectionStrings:DefaultConnection"],
        name: "database",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "ready" },
        timeout: TimeSpan.FromSeconds(5)
    );

builder.Services.AddOutputCache(caching =>
    caching.AddPolicy("HealthChecks", 
        policy => policy.Expire(TimeSpan.FromSeconds(10))));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Health check endpoints
var healthChecks = app.MapGroup("")
    .WithOpenApi()
    .WithName("Health");

healthChecks
    .MapHealthChecks("/health")
    .CacheOutput("HealthChecks")
    .WithName("Readiness")
    .WithOpenApi();

healthChecks
    .MapHealthChecks("/alive", new()
    {
        Predicate = r => r.Tags.Contains("live")
    })
    .WithName("Liveness")
    .WithOpenApi();

app.Run();
```

### AppHost Program.cs (Aspire)

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var sql = builder
    .AddSqlServer("sql", port: 1433)
    .AddDatabase("mydb");

var api = builder
    .AddProject<Projects.MyApi>("api")
    .WithHttpHealthCheck("/health")
    .WithReference(sql)
    .WaitFor(sql);

var app = builder
    .AddProject<Projects.MyWeb>("web")
    .WithHttpHealthCheck("/health")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
```

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MyApp;Integrated Security=true;"
  },
  "AllowedHosts": "*"
}
```

### appsettings.Production.json (OpenShift)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=${SQL_SERVER};Database=${SQL_DATABASE};User Id=${SQL_USER};Password=${SQL_PASSWORD};"
  }
}
```

---

## Summary Checklist

- [ ] Installed `AspNetCore.HealthChecks.SqlServer`
- [ ] Added `AddHealthChecks()` in DI container
- [ ] Mapped `/health` endpoint (readiness)
- [ ] Mapped `/alive` endpoint (liveness)
- [ ] Added SQL Server check with **explicit timeout** (3-5s)
- [ ] Configured connection string from environment
- [ ] Tagged health checks appropriately
- [ ] Tested locally: `curl http://localhost:5000/health`
- [ ] Declared health check in Aspire AppHost
- [ ] Created OpenShift Deployment manifest with probes
- [ ] Tested in OpenShift: `oc logs` and `oc get pods`
- [ ] Verified liveness check doesn't include database
- [ ] Added output caching for production
- [ ] Documented health check endpoints in API docs

---

## References

- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [.NET Aspire Health Checks](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/health-checks)
- [Kubernetes Probes Documentation](https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/)
- [AspNetCore.HealthChecks.SqlServer](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)

