---
title: Complete Observability with OpenTelemetry in .NET 10 A Practical and Universal Guide
source: https://vitorafgomes.medium.com/complete-observability-with-opentelemetry-in-net-10-a-practical-and-universal-guide-c9dda9edaace
author:
  - "[[Vitor Gomes]]"
published: 2025-11-15
created: 2026-02-13T00:00:00
description: "Complete Observability with OpenTelemetry in .NET 10: A Practical and Universal Guide Introduction Observability has become a fundamental pillar in modern application development. With the increasing …"
tags:
  - clippings
uid: 29fff47a-10c9-47f5-a945-883908709b4f
modified: 2026-02-13T22:15:54
---

# Complete Observability with OpenTelemetry in .NET 10 A Practical and Universal Guide

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*Nw16daJvTYboGZLWN7xFCQ.png)

**Introduction**

Observability has become a fundamental pillar in modern application development. With the increasing complexity of systems, having complete visibility into application behavior in production is no longer a luxury it's a necessity.

In this article, I'll share a complete and **reusable** observability implementation using **OpenTelemetry** in.NET 10, covering the **four fundamental pillars**: **Logs**, **Metrics**, **Traces** (Distributed Tracing), and **Health Checks**.

💡 **Important**: Although the examples come from a multi-tenant identity microservice, **this implementation works for ANY type of.NET project**:

\- REST and gRPC APIs  
\- Microservices and monoliths  
\- Workers and Background Services  
\- Web Applications (Blazor, Razor Pages, MVC)  
\- Console Applications  
\- Azure Functions and AWS Lambda

The code is **generic and adaptable** you only need to adjust the health checks for your specific dependencies!

**What is OpenTelemetry?**

OpenTelemetry is an **open-source and vendor-neutral** framework (CNCF) that provides a standardized set of APIs, libraries, and tools to collect application telemetry. It unifies observability instrumentation, allowing you to:

\- **Collect data** consistently across any language/framework  
\- **Process and export** to multiple backends (Grafana, Azure, AWS, Datadog, etc.)  
\- **Avoid vendor lock-in** while maintaining total portability  
\- **Instrument ONCE**, use on any platform

**Why OpenTelemetry?**

\- Industry standard (CNCF)  
\- Native support in.NET since version 6  
\- A single library for logs, metrics, and traces  
\- Works in any environment (on-premises, cloud, hybrid)  
\- Massive community and constant evolution

**Solution Architecture (Applicable to Any Project)**

This implementation follows a cloud-native architecture with standard components that work for **any type of.NET application**.

**Telemetry Flow**:

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*H0EowWx84O87cchc2dZSbg.png)

**Implementation in.NET 10**

1\. Base Configuration

OpenTelemetry configuration is centralized in an extension class that configures all three observability pillars:

```c
public static class OpenTelemetryConfigurationExtensions
{
    public static TBuilder AddOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();
        builder.Services.AddServiceDiscovery();

        // Resilience configuration for HTTP clients
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.Retry.MaxRetryAttempts = 3;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(90);
            });

            http.AddServiceDiscovery();
        });

        return builder;
    }
}
```

**2.** Resource Configuration

The Resource defines attributes that identify your service in the observability system:

```c
private static ResourceBuilder ConfigureResourceOpenTelemetry(ApplicationSettings settings)
{
    var serviceName = string.IsNullOrWhiteSpace(settings?.ServiceName)
        ? "*.*.Identity.Tenant"
        : settings.ServiceName;

    var serviceNamespace = string.IsNullOrWhiteSpace(settings?.ServiceNamespace)
        ? "*.*"
        : settings.ServiceNamespace;

    var serviceVersion = string.IsNullOrWhiteSpace(settings?.ServiceVersion)
        ? "1.0.0"
        : settings.ServiceVersion;

    return ResourceBuilder
        .CreateDefault()
        .AddService(
            serviceName,
            serviceNamespace,
            serviceVersion,
            settings?.AutoGenerateServiceInstanceId ?? true,
            settings?.ServiceInstanceId
        );
}
```

**The Four Pillars of Observability**

**Pillar 1**: Logs Detailed Context with Structured Logging

Logs provide contextual information about specific events in the application.

**Configuration**:

```c
builder.Logging.AddOpenTelemetry(logging =>
{
    logging
        .SetResourceBuilder(ConfigureResourceOpenTelemetry(applicationSettings))
        .AddConsoleExporter();

    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;

    // Custom processor to enrich exception logs
    logging.AddProcessor(new ExceptionLoggingProcessor());
});
```

**Custom Exception Processor**:

One of the differentiators of our implementation is the ExceptionLoggingProcessor, which automatically enriches exception logs with structured information:

```c
public sealed class ExceptionLoggingProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord logRecord)
    {
        if (logRecord.Exception == null) return;

        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("exception.type", logRecord.Exception.GetType().FullName),
            new("exception.message", logRecord.Exception.Message),
            new("exception.stacktrace", logRecord.Exception.StackTrace),
            new("exception.source", logRecord.Exception.Source)
        };

        // Process inner exceptions (up to 5 levels)
        var innerException = logRecord.Exception.InnerException;
        var depth = 1;
        while (innerException != null && depth <= 5)
        {
            attributes.Add(new($"exception.inner.{depth}.type",
                innerException.GetType().FullName));
            attributes.Add(new($"exception.inner.{depth}.message",
                innerException.Message));
            attributes.Add(new($"exception.inner.{depth}.stacktrace",
                innerException.StackTrace));

            innerException = innerException.InnerException;
            depth++;
        }

        // Add custom exception data
        if (logRecord.Exception.Data?.Count > 0)
        {
            foreach (var key in logRecord.Exception.Data.Keys)
            {
                if (key != null)
                {
                    var value = logRecord.Exception.Data[key];
                    if (value != null)
                    {
                        attributes.Add(new($"exception.data.{key}",
                            value.ToString()));
                    }
                }
            }
        }

        logRecord.Attributes = logRecord.Attributes?
            .Concat(attributes) ?? attributes;
    }
}
```

**Benefits**:

\- Easily searchable structured logs  
\- Complete exception context including inner exceptions  
\- Preserved custom metadata  
\- Automatic correlation with traces

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*RQ5ZeG1NUY9EeB5Y6Ixkfw.png)

**Pillar 2**: Metrics Performance and Health Indicators

Metrics provide quantitative data about system behavior over time.

**Configuration**:

```c
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ConfigureResourceOpenTelemetry(applicationSettings))
            .AddAspNetCoreInstrumentation()      // HTTP metrics
            .AddHttpClientInstrumentation()      // HTTP client metrics
            .AddRuntimeInstrumentation()         // .NET runtime metrics
            .AddMeter("MongoDB.Driver.Core")     // MongoDB metrics
            .AddBuiltInMeters()                  // Built-in metrics
            .AddConsoleExporter();
    });
```

**Metrics Collected**:

**HTTP**:

\- Request rate, latency, status codes, throughput

**Runtime**:

\- GC collections, thread pool, memory allocation

**Database**:

\- Active connections, query duration, deadlocks

**Dependencies**:

\- Status of MongoDB, PostgreSQL, RabbitMQ, Keycloak

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*JMoNV-KfdbAxdyh1276erQ.png)

**Pillar 3**: Traces Distributed Tracing

Traces allow you to visualize the complete path of a request through multiple services.

**Configuration**:

```c
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(applicationSettings.ServiceName)
            .AddSource("MongoDB.Driver.Core")
            .SetResourceBuilder(ConfigureResourceOpenTelemetry(applicationSettings))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });
```

**Benefits:**

\- End-to-end latency visualization  
\- Bottleneck identification  
\- Production problem debugging  
\- Service dependency analysis

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*HdWHdm8X-jCIvog9uiZzhw.png)

**Pillar 4**: Health Checks The Forgotten Pillar

In addition to the three classic OpenTelemetry pillars (Logs, Metrics, and Traces), we implement \*\*Health Checks\*\* as a fundamental component of our observability strategy. Health checks provide \*\*proactive visibility\*\* into system and dependency health.

**Health Checks Architecture**:

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*Ar8ihzE_reytGF5JRz2cAw.png)

**Why Are Health Checks Essential?**

**1**. **Early Detection**: Identifies problems before they affect users  
2\. **Auto-Recovery**: Kubernetes can restart pods automatically  
3\. **Service Mesh Ready**: Integration with Istio, Linkerd for circuit breaking  
4\. **Holistic Observability**: Complements metrics and traces with dependency status  
5\. **SLA Tracking**: Provides data for uptime and availability calculation

**Complete Implementation**

**Health Checks Base Configuration**:

```c
private static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    builder.Services.AddHealthChecks()
        // ========================================
        // Kubernetes Probes
        // ========================================

        // Liveness Probe: "Is the application running?"
        // If it fails, Kubernetes RESTARTS the pod
        .AddCheck("self",
            () => HealthCheckResult.Healthy("Application is running"),
            tags: ["live"])

        // Readiness Probe: "Is the application ready to receive traffic?"
        // If it fails, Kubernetes REMOVES the pod from Service
        .AddCheck("readiness",
            () => HealthCheckResult.Healthy("Application is ready"),
            tags: ["ready"])

        // ========================================
        // Critical Dependencies (Database)
        // ========================================

        // PostgreSQL - Main database
        .Add(new HealthCheckRegistration(
            name: "postgres",
            factory: sp => new PostgresSqlHealthCheck(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<PostgresSqlHealthCheck>>(),
                sp),
            failureStatus: HealthStatus.Degraded,  // Degraded allows app to continue
            tags: ["dependency", "critical", "database"],
            timeout: TimeSpan.FromSeconds(15)))

        // MongoDB - Analytics database
        .Add(new HealthCheckRegistration(
            name: "mongodb",
            factory: sp => new MongoDbHealthCheck(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<MongoDbHealthCheck>>(),
                sp),
            failureStatus: HealthStatus.Degraded,
            tags: ["dependency", "critical", "database"],
            timeout: TimeSpan.FromSeconds(15)))

        // ========================================
        // External Dependencies
        // ========================================

        // Keycloak - External authentication service
        .Add(new HealthCheckRegistration(
            name: "keycloak",
            factory: sp => new KeycloakHealthCheck(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<IHttpClientFactory>()),
            failureStatus: HealthStatus.Degraded,
            tags: ["dependency", "external", "authentication"],
            timeout: TimeSpan.FromSeconds(18)))

        // RabbitMQ - Messaging infrastructure
        .Add(new HealthCheckRegistration(
            name: "rabbitmq",
            factory: sp => new RabbitMqHealthCheck(
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<RabbitMqHealthCheck>>(),
                sp),
            failureStatus: HealthStatus.Degraded,
            tags: ["dependency", "messaging"],
            timeout: TimeSpan.FromSeconds(15)));

    // ========================================
    // Health Check Publisher (Periodic Checks)
    // ========================================

    builder.Services.Configure<HealthCheckPublisherOptions>(options =>
    {
        options.Delay = TimeSpan.FromSeconds(5);   // Wait 5s after startup
        options.Period = TimeSpan.FromSeconds(30); // Execute every 30s
    });

    return builder;
}
```

**Custom Health Check Implementation**

Let's see how to implement a custom health check for PostgreSQL:

```c
public class PostgresSqlHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PostgresSqlHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PostgresSqlHealthCheck(
        IConfiguration configuration,
        ILogger<PostgresSqlHealthCheck> logger,
        IServiceProvider serviceProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get DbContext via DI
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            // Execute simple query to check connectivity
            var canConnect = await dbContext.Database
                .CanConnectAsync(cancellationToken);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to PostgreSQL database",
                    data: new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow,
                        ["database"] = "PostgreSQL"
                    });
            }

            // Check for pending migrations
            var pendingMigrations = await dbContext.Database
                .GetPendingMigrationsAsync(cancellationToken);

            if (pendingMigrations.Any())
            {
                return HealthCheckResult.Degraded(
                    "Database has pending migrations",
                    data: new Dictionary<string, object>
                    {
                        ["pendingMigrations"] = pendingMigrations.Count(),
                        ["migrations"] = string.Join(", ", pendingMigrations)
                    });
            }

            // Test real query to validate permissions
            var recordCount = await dbContext.Tenants
                .CountAsync(cancellationToken);

            return HealthCheckResult.Healthy(
                "PostgreSQL connection is healthy",
                data: new Dictionary<string, object>
                {
                    ["connectionString"] = MaskConnectionString(
                        dbContext.Database.GetConnectionString()),
                    ["recordCount"] = recordCount,
                    ["responseTime"] = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PostgreSQL health check failed: {Message}", ex.Message);

            return HealthCheckResult.Unhealthy(
                "PostgreSQL health check failed",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["error"] = ex.Message,
                    ["timestamp"] = DateTime.UtcNow
                });
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        // Mask password in connection string for security
        return Regex.Replace(
            connectionString ?? string.Empty,
            @"Password=([^;]+)",
            "Password=***",
            RegexOptions.IgnoreCase);
    }
}
```

**Custom Endpoints with Response Writer**

Our implementation provides detailed endpoints with structured JSON:

```c
public static WebApplication MapDefaultEndpoints(this WebApplication app)
{
    var healthCheckOptions = new HealthCheckOptions
    {
        Predicate = _ => true,  // All dependencies
        ResponseWriter = WriteHealthCheckResponse,
        ResultStatusCodes = new Dictionary<HealthStatus, int>
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,     
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    };

    var liveOptions = new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("live"),
        ResponseWriter = WriteHealthCheckResponse,
        ResultStatusCodes = new Dictionary<HealthStatus, int>
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    };

    var readyOptions = new HealthCheckOptions
    {
        Predicate = r => r.Tags.Contains("ready"),
        ResponseWriter = WriteHealthCheckResponse,
        ResultStatusCodes = new Dictionary<HealthStatus, int>
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        }
    };

    // Map endpoints to both /health and /healthz (K8s compatibility)
    MapDetailedHealthEndpoints(app, "/health",
        healthCheckOptions, liveOptions, readyOptions);
    MapDetailedHealthEndpoints(app, "/healthz",
        healthCheckOptions, liveOptions, readyOptions);

    return app;
}
```

> **Important Note**: Degraded returns 200 OK because the service can still process requests, just with reduced functionality (e.g., cache unavailable, but main database OK).

**Custom Response Writer**:

```c
private static async Task WriteHealthCheckResponse(
    HttpContext context,
    HealthReport report)
{
    context.Response.ContentType = "application/json";

    var response = new
    {
        Status = report.Status.ToString(),
        Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        TotalDuration = $"{report.TotalDuration.TotalMilliseconds}ms",
        Results = report.Entries.Select(e => new
        {
            Name = e.Key,
            Status = e.Value.Status.ToString(),
            Duration = $"{e.Value.Duration.TotalMilliseconds}ms",
            Description = e.Value.Description,
            Error = e.Value.Exception?.Message,
            Data = e.Value.Data?.Count > 0 ? e.Value.Data : null,
            Tags = e.Value.Tags?.ToList()
        }).ToList(),
        Summary = new
        {
            Total = report.Entries.Count,
            Healthy = report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
            Degraded = report.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
            Unhealthy = report.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
        }
    };

    await context.Response.WriteAsync(
        JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
}
```

**Available Endpoints**:

```c
# General status of all dependencies
GET /health
GET /healthz

# Liveness probe - Kubernetes uses to know if it should restart the pod
GET /health/live
GET /healthz/live

# Readiness probe - Kubernetes uses to know if it can send traffic
GET /health/ready
GET /healthz/ready
```

**Structured JSON Response**

**Scenario 1**: Everything Healthy

```c
{
  "status": "Healthy",
  "timestamp": "2025-11-15T10:30:45.123Z",
  "totalDuration": "245.67ms",
  "results": [
    {
      "name": "self",
      "status": "Healthy",
      "duration": "0.12ms",
      "description": "Application is running",
      "tags": ["live"]
    },
    {
      "name": "readiness",
      "status": "Healthy",
      "duration": "0.08ms",
      "description": "Application is ready",
      "tags": ["ready"]
    },
    {
      "name": "postgres",
      "status": "Healthy",
      "duration": "45.23ms",
      "description": "PostgreSQL connection is healthy",
      "data": {
        "connectionString": "Host=10.0.1.10;Database=my_database;Password=***",        "recordCount": 1247,
        "responseTime": "2025-11-15T10:30:45.089Z"
      },
      "tags": ["dependency", "critical", "database"]
    },
    {
      "name": "mongodb",
      "status": "Healthy",
      "duration": "32.45ms",
      "description": "MongoDB connection is healthy",
      "data": {
        "cluster": "mongodb://10.0.1.20:27017",
        "serverVersion": "7.0.14",
        "replicaSet": "rs0"
      },
      "tags": ["dependency", "critical", "database"]
    },
    {
      "name": "keycloak",
      "status": "Healthy",
      "duration": "78.12ms",
      "description": "Keycloak is reachable",
      "data": {
        "url": "https://keycloak.example.com",
        "realm": "*-*"
      },
      "tags": ["dependency", "external", "authentication"]
    },
    {
      "name": "rabbitmq",
      "status": "Healthy",
      "duration": "56.34ms",
      "description": "RabbitMQ connection is healthy",
      "data": {
        "host": "110.0.1.20",
        "virtualHost": "/",
        "isConnected": true
      },
      "tags": ["dependency", "messaging"]
    }
  ],
  "summary": {
    "total": 6,
    "healthy": 6,
    "degraded": 0,
    "unhealthy": 0
  }
}
```

**Scenario 2**: PostgreSQL Degraded (Pending Migrations)

```c
{
  "status": "Degraded",
  "timestamp": "2025-11-15T10:35:22.456Z",
  "totalDuration": "312.89ms",
  "results": [
    {
      "name": "postgres",
      "status": "Degraded",
      "duration": "67.89ms",
      "description": "Database has pending migrations",
      "data": {
        "pendingMigrations": 2,
        "migrations": "20251115_AddTenantMetadata, 20251115_UpdateUserSchema"
      },
      "tags": ["dependency", "critical", "database"]
    }
  ],
  "summary": {
    "total": 6,
    "healthy": 5,
    "degraded": 1,
    "unhealthy": 0
  }
}
```

**Scenario 3**: Keycloak Unavailable (Unhealthy)

```c
{
  "status": "Unhealthy",
  "timestamp": "2025-11-15T10:40:15.789Z",
  "totalDuration": "18234.56ms",
  "results": [
    {
      "name": "keycloak",
      "status": "Unhealthy",
      "duration": "18000.00ms",
      "description": "Keycloak health check failed",
      "error": "A task was canceled.",
      "data": {
        "error": "The operation was canceled.",
        "timestamp": "2025-11-15T10:40:15.789Z"
      },
      "tags": ["dependency", "external", "authentication"]
    }
  ],
  "summary": {
    "total": 6,
    "healthy": 5,
    "degraded": 0,
    "unhealthy": 1
  }
}
```

**Kubernetes Integration**

**Deployment.yaml**:

```c
apiVersion: apps/v1
kind: Deployment
metadata:
  name: *-*-identity-tenant
spec:
  replicas: 2
  template:
    spec:
      containers:
      - name: api
        image: *-*-identity-tenant:latest
        ports:
        - containerPort: 8080
          name: http

        # ========================================
        # Liveness Probe
        # ========================================
        # Kubernetes restarts the pod if it fails
        livenessProbe:
          httpGet:
            path: /health/live
            port: http
          initialDelaySeconds: 120  # Wait 2min after startup
          periodSeconds: 30         # Check every 30s
          timeoutSeconds: 10        # 10s timeout
          failureThreshold: 5       # Restart after 5 consecutive failures
          successThreshold: 1       # 1 success = healthy again

        # ========================================
        # Readiness Probe
        # ========================================
        # Kubernetes removes from Service if it fails
        readinessProbe:
          httpGet:
            path: /health/ready
            port: http
          initialDelaySeconds: 90   # Wait 90s after startup
          periodSeconds: 15         # Check every 15s
          timeoutSeconds: 10        # 10s timeout
          failureThreshold: 5       # Remove after 5 failures
          successThreshold: 1       # 1 success = ready again

        # ========================================
        # Startup Probe (Optional - .NET 10)
        # ========================================
        # Protects liveness during slow startup
        startupProbe:
          httpGet:
            path: /health/live
            port: http
          initialDelaySeconds: 0
          periodSeconds: 10
          timeoutSeconds: 10
          failureThreshold: 30      # 30 attempts * 10s = 5min max startup
          successThreshold: 1
```

**Prometheus Integration (Service Monitor)**:

```c
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: *-*-identity-tenant
  labels:
    release: kps  # Label for Prometheus Operator discovery
spec:
  selector:
    matchLabels:
      app: *-*-identity-tenant
  endpoints:
  - port: http
    path: /metrics
    interval: 30s
    scrapeTimeout: 10s

    # Custom health check metrics
    metricRelabelings:
    - sourceLabels: [__name__]
      regex: 'healthcheck_.*'
      action: keep
```

**Exported Health Check Metrics**:

ASP.NET Core automatically exports health check metrics:

```c
# Status of each health check (0=Unhealthy, 0.5=Degraded, 1=Healthy)
aspnetcore_healthcheck_status{name="postgres"} 1
aspnetcore_healthcheck_status{name="mongodb"} 1
aspnetcore_healthcheck_status{name="keycloak"} 1
aspnetcore_healthcheck_status{name="rabbitmq"} 1

# Duration of each health check in seconds
aspnetcore_healthcheck_duration_seconds{name="postgres"} 0.045
aspnetcore_healthcheck_duration_seconds{name="mongodb"} 0.032
aspnetcore_healthcheck_duration_seconds{name="keycloak"} 0.078
aspnetcore_healthcheck_duration_seconds{name="rabbitmq"} 0.056

# Overall application status
aspnetcore_healthcheck_status{name="self"} 1
```

**Health Check-Based Alerts**

**Prometheus Alert Rules**:

```c
groups:
- name: health_checks
  interval: 30s
  rules:

  # Alert when PostgreSQL is unhealthy
  - alert: PostgreSQLUnhealthy
    expr: aspnetcore_healthcheck_status{name="postgres"} == 0
    for: 2m
    labels:
      severity: critical
      component: database
    annotations:
      summary: "PostgreSQL health check failing"
      description: "PostgreSQL has been unhealthy for 2 minutes"

  # Alert when any dependency is degraded
  - alert: DependencyDegraded
    expr: aspnetcore_healthcheck_status < 1 and aspnetcore_healthcheck_status > 0
    for: 5m
    labels:
      severity: warning
    annotations:
      summary: "{{ $labels.name }} is degraded"
      description: "Dependency {{ $labels.name }} has been degraded for 5 minutes"

  # Alert when health check is slow
  - alert: HealthCheckSlow
    expr: aspnetcore_healthcheck_duration_seconds > 5
    for: 2m
    labels:
      severity: warning
    annotations:
      summary: "Health check {{ $labels.name }} is slow"
      description: "Health check taking {{ $value }}s (threshold: 5s)"
```

**OTLP Export to Grafana Stack**

Telemetry export uses the OTLP (OpenTelemetry Protocol) to send data to Grafana Alloy (collector):

```c
private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    var useOtlpExporter = !string.IsNullOrWhiteSpace(
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

    if (useOtlpExporter)
    {
        // Export logs to Loki
        builder.Services.Configure<OpenTelemetryLoggerOptions>(
            logging => logging.AddOtlpExporter());

        // Export metrics to Prometheus
        builder.Services.ConfigureOpenTelemetryMeterProvider(
            metrics => metrics.AddOtlpExporter());

        // Export traces to Tempo
        builder.Services.ConfigureOpenTelemetryTracerProvider(
            tracing => tracing.AddOtlpExporter());
    }

    return builder;
}
```

**Kubernetes Configuration**

**Helm Values (values.yaml)**:

```c
configMap:
  enabled: true
  data:
    # Grafana Alloy endpoint in cluster
    OTEL_EXPORTER_OTLP_ENDPOINT: "http://alloy.monitoring.svc.cluster.local:4317"

    # Service identification
    ServiceName: "*.*.Identity.Tenant"
    ServiceNamespace: "*.*.Identity.Tenant.Api"
    ServiceVersion: "1.0.0"
    AutoGenerateServiceInstanceId: "false"
    ServiceInstanceId: "*.*+.Identity.Tenant"

# Service Monitor for Prometheus Operator
monitoring:
  enabled: true
  serviceMonitor:
    enabled: true
    interval: 30s
    scrapeTimeout: 10s
    path: /metrics
    labels:
      release: kps  # Label for Prometheus discovery
```

**Local Configuration (Development)**

**launchSettings.json**:

```c
{
  "profiles": {
    "http": {
      "environmentVariables": {
        "OTEL_EXPORTER_OTLP_ENDPOINT": "localhost:17011",
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Practical Example: Telemetry Correlation**

Let's see how the four pillars work together in a real scenario.

**Scenario**: Keycloak Authentication Failure

1. Trace shows:

```c
[Trace ID: abc123]
  ├─ HTTP POST /api/v1/users/login (200ms)
  │   ├─ Keycloak.ValidateToken (150ms)  ❌ ERROR
  │   └─ Database.SaveAuditLog (45ms)    ✓
```

**2**. Structured log captures:

```c
{
  "timestamp": "2025-11-15T10:30:45.123Z",
  "level": "Error",
  "traceId": "abc123",
  "spanId": "def456",
  "service.name": "*.*.Identity.Tenant",
  "exception.type": "Keycloak.AuthenticationException",
  "exception.message": "Invalid token signature",
  "exception.stacktrace": "...",
  "http.method": "POST",
  "http.route": "/api/v1/users/login",
  "http.status_code": 401
}
```

**3**. Metrics alert:

```c
http_request_duration_seconds{route="/api/v1/users/login"} > SLA
keycloak_health_check{status="Degraded"}
http_requests_total{status="401"} spike detected
```

**4**. Health Check confirms:

```c
{
  "name": "keycloak",
  "status": "Unhealthy",
  "error": "Connection timeout"
}
```

With this correlation, you can:

\- **Identify the problem** through metrics  
\- **Find the root cause** via traces  
\- **Diagnose details** in structured logs  
\- **Confirm status** with health checks  
\- Everything connected by **Trace ID**!

**Implementation Benefits**

**1**. Production Debugging

\- Trace IDs allow following requests end-to-end  
\- Structured logs facilitate complex searches  
\- Complete stack traces with context

**2**. Performance Optimization

\- Bottleneck identification via traces  
\- Latency metrics per endpoint  
\- Usage pattern analysis

**3**. Proactive Alerting

\- Health checks detect dependency failures  
\- Metrics trigger alerts before impacting users  
\- Automatic SLO/SLA tracking

**4**. Compliance and Auditing

\- All events traceable  
\- Structured logs facilitate compliance  
\- Configurable retention by data type

**Implemented Best Practices**

\- **Vendor Neutra** l: OTLP allows changing backends without code changes  
\- **Structured Logging**: Logs always in structured format (JSON)  
\- **Correlation IDs**: TraceId/SpanId in all logs  
\- **Contextual Information**: Custom processors enrich telemetry  
\- **Sampling**: Configurable to control production volume  
\- **Resource Attributes**: Clear service/instance identification  
\- **Health Checks**: Kubernetes-ready probes  
\- **Graceful Degradation**: Console exporter as fallback

**Results**

After implementing complete observability with OpenTelemetry:

**MTTD (Mean Time To Detect)**: 80% reduction (from 30min to 6min)  
**MTTR (Mean Time To Resolve)**: 65% reduction (from 2h to 42min)  
**SLA Compliance**: Increase from 95% to 99.5%  
**Debug Efficiency**: Issue resolution 3x faster  
**Cost Optimization**: Slow query identification saved 40% of DB resources

**Conclusion**

OpenTelemetry transformed our ability to understand and debug system behavior in production. Standardization through a vendor-neutral framework gave us flexibility without sacrificing functionality.

The key to success was treating observability as a \*\*first-class requirement\*\* from the project's start, not as a "nice to have" added later.

If you're building.NET applications, especially in cloud-nativegsmart environments with Kubernetes, OpenTelemetry is a solid choice that will pay dividends in productivity and reliability.

**Resources**

\- **OpenTelemetry.NET**: [https://opentelemetry.io/docs/languages/net/](https://opentelemetry.io/docs/languages/net/)  
\- **Grafana Stack**: [https://grafana.com/oss/](https://grafana.com/oss/)  
\- **Health Checks**: [https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
