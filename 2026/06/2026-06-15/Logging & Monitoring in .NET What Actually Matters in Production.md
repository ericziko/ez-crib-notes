---
title: Logging & Monitoring in .NET What Actually Matters in Production
source: https://medium.com/real-world-net/logging-monitoring-in-net-what-actually-matters-in-production-a22c55dc918c
author:
  - "[[Kerim Kara]]"
published: 2026-05-17
created: 2026-06-15T08:09:00-07:00
description: Featured
tags:
  - clippings
uid: 01KV5XD9YV1J2JWXNR52F9KBC3
modified: 2026-06-15T08:35:18-07:00
---

# Logging & Monitoring in .NET What Actually Matters in Production
When a.NET application runs in production, everything changes. What worked fine during development can quickly become a problem. Bugs appear that you cannot reproduce. Performance issues affect real users. Errors happen in places you did not expect. At this point, debugging with breakpoints is no longer possible. You need a different approach. This is where logging and monitoring become critical.

Many teams treat logging as an afterthought. They add a few log statements, maybe log errors, and move on. Monitoring is often limited to checking if the application is still running. This approach works for small projects, but it fails in real production environments.

In modern applications, especially cloud-based and distributed systems, observability is a core requirement. You need to understand what your system is doing at all times. You need to detect problems early, investigate them quickly, and fix them before users are affected.

Logging and monitoring are the foundation of this visibility.

Logging helps you understand *what happened*. It records events, errors, and important actions inside your application. Monitoring helps you understand *how your system behaves over time*. It shows trends, performance metrics, and system health.

But not all logging and monitoring strategies are effective. Logging everything can create noise and increase costs. Logging too little makes debugging impossible. Using the wrong tools or patterns leads to confusion instead of clarity.

This article focuses on what actually matters in production. It is not about theory or complex setups. It is about practical decisions that improve reliability and make your system easier to operate.

You will learn:

- How to design meaningful logs
- Why structured logging is essential
- How to choose correct log levels
- How to trace requests across services
- Why centralized logging is critical
- How monitoring and metrics complement logs
- How to use health checks and distributed tracing
- And the most common mistakes to avoid

All examples are based on real-world.NET practices using simple and clear code.

If you build or maintain.NET applications, this guide will help you move from basic logging to production-ready observability.

Because in production, the question is not *if* something will go wrong.  
The real question is: **Will you be able to understand and fix it quickly?**

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*p7GyB50p3pwsSsxySCW9dQ.png)

## 1\. Why Logging is Not Optional in Production

When you move a.NET application from development to production, logging stops being a "nice to have" and becomes essential. In development, you can debug step by step, inspect variables, and quickly find issues. In production, you lose that visibility. Logging becomes your main window into what is happening inside your system.

A common mistake is to log too little or too much. If you log too little, you cannot diagnose problems. If you log too much, your logs become noisy and expensive to store. The goal is to log meaningful events that help you understand system behavior and failures.

Good logging answers key questions:

- What happened?
- When did it happen?
- Why did it happen?
- What was the impact?

In.NET, logging is built into the framework via `Microsoft.Extensions.Logging`. This gives you a structured and flexible way to log messages. Instead of writing plain text logs, you should prefer structured logging. This allows logs to be searched and analyzed easily in tools like Seq, Elasticsearch, or Azure Monitor.

Another important point is log levels. You should use them correctly:

- **Information**: Normal operations
- **Warning**: Something unexpected but not critical
- **Error**: A failure occurred
- **Debug/Trace**: Detailed internal information

Avoid logging sensitive data like passwords, tokens, or personal user information. This is a common production mistake and can lead to security issues.

Also, logs should always include context. For example, a user ID, request ID, or correlation ID. Without context, logs are much harder to use.

Here is a simple example using.NET logging:

```cs
using Microsoft.Extensions.Logging;

public class OrderService
{
    private readonly ILogger<OrderService> _logger;
    public OrderService(ILogger<OrderService> logger)
    {
        _logger = logger;
    }
    public void CreateOrder(int userId, decimal amount)
    {
        _logger.LogInformation("Creating order for user {UserId} with amount {Amount}", userId, amount);
        try
        {
            // Simulate order creation
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be greater than zero");
            }
            _logger.LogInformation("Order created successfully for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while creating order for user {UserId}", userId);
            throw;
        }
    }
}
```

This example shows structured logging with placeholders. This is much better than string concatenation.

In production, logging is not just about errors. It is about understanding your system. If you design your logging carefully from the start, debugging production issues becomes much easier.

## 2\. Structured Logging vs Plain Text Logging

Many developers start with simple text logs. For example:  
"User created successfully" or "Error occurred". While this works at a small scale, it becomes a problem in production systems.

Plain text logs are hard to search, filter, and analyze. If you want to find all logs for a specific user or request, you need to rely on string matching, which is inefficient and unreliable.

Structured logging solves this problem. Instead of storing logs as simple strings, structured logging stores data as key-value pairs. This makes logs machine-readable and easy to query.

In.NET, structured logging is supported out of the box. When you use placeholders like `{UserId}`, the logging system captures them as structured data.

For example:

- Plain text: "User 123 created an order"
- Structured: UserId=123, Event=OrderCreated

With structured logs, you can easily filter logs by UserId or event type.

Another advantage is integration with modern observability tools like:

- Elasticsearch + Kibana
- Seq
- Azure Application Insights

These tools can index structured data and allow powerful queries.

Let's compare bad vs good logging:

**Bad logging:**

```cs
_logger.LogInformation("User " + userId + " created an order of " + amount);
```

**Good structured logging:**

```cs
_logger.LogInformation("User {UserId} created an order of {Amount}", userId, amount);
```

Now let's see a more complete example using Serilog, a popular logging library in.NET:

```cs
using Serilog;

class Program
{
    static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        int userId = 123;
        decimal amount = 50.5m;
        Log.Information("User {UserId} created an order with amount {Amount}", userId, amount);
        Log.CloseAndFlush();
    }
}
```

Serilog makes structured logging even more powerful by supporting JSON output. This is very useful when logs are sent to centralized systems.

Another important concept is consistency. Your log messages should follow a consistent pattern across the application. For example:

- Always include identifiers (UserId, OrderId)
- Use consistent naming

Structured logging also improves performance when analyzing logs. Instead of parsing text, systems can directly use structured fields.

In production systems, structured logging is not optional anymore. It is the standard approach. It allows better debugging, monitoring, and analysis.

If you are still using plain text logs, switching to structured logging will significantly improve your ability to operate and maintain your application.

## 3\. Choosing the Right Log Levels

Choosing the correct log level is one of the most important decisions in logging. Many production issues come from misuse of log levels. Either everything is logged as "Information" or everything is logged as "Error".

Log levels help you control what gets recorded and what gets ignored. They also help reduce noise and improve performance.

In.NET, the main log levels are:

- Trace
- Debug
- Information
- Warning
- Error
- Critical

Each level has a purpose.

**Trace / Debug**  
These are used for detailed internal information. You typically enable them only during development or troubleshooting. In production, they are often disabled because they generate too much data.

**Information**  
This is for normal operations. For example, starting a process, completing a request, or important business events.

**Warning**  
This indicates something unexpected, but the system can still continue. For example, a retry operation or a fallback mechanism.

**Error**  
This indicates a failure in the application. The operation did not complete successfully.

**Critical**  
This is for severe failures that may stop the entire system.

A common mistake is logging everything as an error. This creates alert fatigue. If everything is an error, nothing is important.

Here is an example of proper log level usage:

```cs
public class PaymentService
{
    private readonly ILogger<PaymentService> _logger;
    
    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }
    public void ProcessPayment(int userId, decimal amount)
    {
        _logger.LogInformation("Starting payment for user {UserId}", userId);
        try
        {
            if (amount <= 0)
            {
                _logger.LogWarning("Invalid payment amount {Amount} for user {UserId}", amount, userId);
                return;
            }
            // Simulate payment
            bool paymentSuccess = true;
            if (!paymentSuccess)
            {
                _logger.LogError("Payment failed for user {UserId}", userId);
                return;
            }
            _logger.LogInformation("Payment completed for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error while processing payment for user {UserId}", userId);
            throw;
        }
    }
}
```

Another important concept is configuring log levels per environment. For example:

- Development: Debug level enabled
- Production: Information and above

In `appsettings.json`:

```cs
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

This helps reduce noise from framework logs while keeping your application logs useful.

Proper use of log levels improves clarity, reduces cost, and makes monitoring more effective. It ensures that important issues are visible and not hidden in noise.

## 4\. Correlation IDs and Request Tracing

In modern applications, especially microservices, a single user request can pass through many services. Without a way to track that request, debugging becomes very difficult.

This is where correlation IDs come in.

A correlation ID is a unique identifier that is attached to a request and passed through all services. Every log entry related to that request includes this ID. This allows you to trace the full journey of the request.

In ASP.NET Core, you can implement this using middleware.

Here is a simple example:

```cs
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string HeaderName = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task Invoke(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = correlationId;
        }
        context.Response.Headers[HeaderName] = correlationId;
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId.ToString()
        }))
        {
            logger.LogInformation("Handling request with CorrelationId {CorrelationId}", correlationId);
            await _next(context);
            logger.LogInformation("Finished handling request with CorrelationId {CorrelationId}", correlationId);
        }
    }
}
```

You then register the middleware:

```cs
app.UseMiddleware<CorrelationIdMiddleware>();
```

Now, every log inside that request scope will include the correlation ID.

This is extremely useful when:

- Debugging distributed systems
- Investigating production issues
- Tracking performance bottlenecks

Correlation IDs are also used with distributed tracing systems like:

- OpenTelemetry
- Jaeger
- Zipkin

These tools provide a visual representation of request flow across services.

Another best practice is to include the correlation ID in responses. This allows clients to report issues with a specific ID.

For example:

```cs
X-Correlation-ID: abc-123
```

If a user reports a problem, you can search logs using this ID.

Without correlation IDs, logs are isolated messages. With correlation IDs, logs become a connected story.

In production systems, this is not optional. It is essential for observability.

## 5\. Centralized Logging: Why Local Logs Are Not Enough

Logging to a file or console is fine during development. But in production, especially in cloud or container environments, local logs are not enough.

Applications can run on multiple servers or containers. If logs are stored locally, you need to access each machine separately. This is inefficient and often impossible.

Centralized logging solves this problem by collecting logs from all services into one place.

Benefits of centralized logging:

- Single place to search logs
- Easier debugging
- Better monitoring and alerting
- Long-term storage and analysis

Popular tools include:

- Elasticsearch + Kibana (ELK stack)
- Seq
- Azure Monitor
- Datadog

In.NET, you can integrate centralized logging using providers like Serilog.

Here is an example using Serilog with Elasticsearch:

```cs
using Serilog;
using Serilog.Sinks.Elasticsearch;

var builder = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "myapp-logs-{0:yyyy.MM.dd}"
    })
    .CreateLogger();
Log.Logger = builder;
try
{
    Log.Information("Application started");
    // Simulate work
    Log.Information("Processing data...");
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred");
}
finally
{
    Log.CloseAndFlush();
}
```

Now logs are sent to Elasticsearch and can be visualized in Kibana.

Another important concept is log retention. Logs can grow quickly, so you need policies to:

- Delete old logs
- Archive important logs

Also, centralized logging enables alerting. For example:

- Send alert if error rate increases
- Detect unusual patterns

Without centralized logging, production debugging becomes slow and painful.

Modern applications must treat logs as a first-class system. Centralized logging is a key part of that system.

## 6\. Monitoring vs Logging: Understanding the Difference

Many developers confuse logging with monitoring. While they are related, they serve different purposes in production systems.

Logging is about recording events. Monitoring is about observing system health and performance over time.

Logs are detailed and event-based. Monitoring is aggregated and metric-based.

For example:

- A log might say: "User 123 failed to login"
- Monitoring might show: "Login failure rate increased by 20%"

Logs help you investigate issues after they happen. Monitoring helps you detect issues as they happen.

A healthy production system needs both.

Monitoring usually focuses on metrics such as:

- CPU usage
- Memory usage
- Request rate
- Error rate
- Response time

In.NET, you can use tools like:

- Application Insights
- Prometheus + Grafana
- OpenTelemetry

Let's look at a simple example using.NET metrics with `System.Diagnostics.Metrics`:

```cs
using System.Diagnostics.Metrics;

public class OrderMetrics
{
    private static readonly Meter Meter = new("MyApp.Orders");
    private static readonly Counter<int> OrderCounter = Meter.CreateCounter<int>("orders.created");
    public void RecordOrder()
    {
        OrderCounter.Add(1);
    }
}
```

This example tracks how many orders are created.

You can then export these metrics using OpenTelemetry:

```cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("MyApp.Orders");
        metrics.AddConsoleExporter();
    });
```

Monitoring systems collect these metrics and visualize them in dashboards.

Another important concept is alerting. Monitoring tools can trigger alerts when thresholds are exceeded. For example:

- Error rate > 5%
- Response time > 2 seconds

Logs alone cannot do this efficiently.

In production, logs answer "why", while monitoring answers "when" and "how often".

You should design your system with both:

- Logs for debugging
- Metrics for visibility
- Alerts for action

Without monitoring, you only know about problems after users report them. With monitoring, you can detect and fix issues before users notice.

## 7\. Health Checks and Readiness Probes

In production systems, especially in cloud environments like Kubernetes, your application must report its health status.

Health checks allow external systems to know if your application is running correctly.

There are two main types:

- **Liveness checks**: Is the app running?
- **Readiness checks**: Is the app ready to handle requests?

In ASP.NET Core, health checks are built-in and easy to use.

Here is a basic example:

```cs
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health");
app.Run();
```

This creates a simple endpoint:

```cs
GET /health
```

It returns a status like:

- Healthy
- Unhealthy

But real applications need deeper checks. For example:

- Database connection
- External API availability
- Cache status

Here is an example with a database check:

```cs
builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: "YourConnectionString",
        name: "sql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy
    );
```

You can also create custom health checks:

```cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class ApiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool apiAvailable = true; // simulate check
        if (apiAvailable)
        {
            return Task.FromResult(HealthCheckResult.Healthy("API is working"));
        }
        return Task.FromResult(HealthCheckResult.Unhealthy("API is down"));
    }
}
```

Register it:

```cs
builder.Services.AddHealthChecks()
    .AddCheck<ApiHealthCheck>("external_api");
```

Health checks are used by:

- Load balancers
- Kubernetes
- Monitoring systems

If your app is unhealthy, traffic can be stopped automatically.

Another important point: do not make health checks too slow. They should be fast and lightweight.

Health checks are critical for:

- High availability
- Auto-scaling
- Self-healing systems

Without them, your system cannot react properly to failures.

## 8\. Distributed Tracing with OpenTelemetry

As systems grow, especially with microservices, understanding request flow becomes harder. A single request might go through multiple services, databases, and APIs.

Distributed tracing solves this problem.

It allows you to see:

- Where time is spent
- Which service caused an error
- How requests flow through the system

OpenTelemetry is the modern standard for distributed tracing.

In.NET, it integrates easily.

Here is a simple setup:

```cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });
```

This automatically tracks:

- Incoming HTTP requests
- Outgoing HTTP calls

Each request is represented as a trace, made up of spans.

You can also create custom spans:

```cs
using System.Diagnostics;

public class PaymentService
{
    private static readonly ActivitySource ActivitySource = new("MyApp.Payment");
    public void ProcessPayment()
    {
        using var activity = ActivitySource.StartActivity("ProcessPayment");
        activity?.SetTag("payment.method", "credit_card");
        // Simulate work
        Thread.Sleep(100);
    }
}
```

This adds detailed tracing information.

Tracing tools like:

- Jaeger
- Zipkin
- Azure Monitor

can visualize this data.

You get a timeline view showing:

- Total request time
- Each step in the process

This is extremely useful for performance tuning.

Another key benefit is automatic correlation. Logs, metrics, and traces can be linked together.

For example:

- A slow request → find trace → find related logs

In modern production systems, distributed tracing is no longer optional.

It provides deep visibility that logs alone cannot offer.

## 9\. Log Enrichment and Contextual Data

Basic logs are useful, but enriched logs are powerful.

Log enrichment means adding extra context to every log entry.

This context can include:

- User ID
- Request ID
- Environment (Production, Staging)
- Machine name
- Application version

Without this context, logs are harder to analyze.

In.NET, especially with Serilog, enrichment is very easy.

Here is an example:

```cs
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "MyApp")
    .Enrich.WithProperty("Environment", "Production")
    .WriteTo.Console()
    .CreateLogger();
Log.Information("Application started");
```

You can also enrich logs dynamically:

```cs
using (LogContext.PushProperty("UserId", 123))
{
    Log.Information("User performed an action");
}
```

Now every log inside that scope includes `UserId`.

Another useful enrichment is machine information:

```cs
.Enrich.WithMachineName()
.Enrich.WithThreadId()
```

This helps when debugging distributed systems.

Let's see a full example:

```cs
Log.Logger = new LoggerConfiguration()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("AppVersion", "1.0.0")
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Processing request");
```

In centralized logging systems, enriched logs allow powerful queries like:

- Find all errors for UserId = 123
- Find logs from a specific server

Another important concept is consistency. Use the same property names across your application.

For example:

- Always use `UserId`, not `User_Id` or `user`

Log enrichment makes logs more useful without changing log messages.

In production, enriched logs are essential for:

- Debugging
- Monitoring
- Security auditing

## 10\. Common Mistakes in Production Logging

Even experienced developers make mistakes with logging in production.

Let's look at the most common ones.

**1\. Logging Too Much**  
Excessive logging increases cost and noise. It makes it harder to find important information.

**2\. Logging Too Little**  
Missing logs make debugging almost impossible.

**3\. Logging Sensitive Data**  
Never log:

- Passwords
- Tokens
- Personal data

This is a serious security risk.

**4\. Using Wrong Log Levels**  
Everything should not be an error.

**5\. No Correlation IDs**  
Without correlation, logs are isolated and hard to trace.

**6\. No Centralized Logging**  
Local logs are not enough in distributed systems.

**7\. Ignoring Performance Impact**  
Logging can affect performance if not configured properly.

Here is an example of bad logging:

```cs
_logger.LogInformation("User login: " + username + " Password: " + password);
```

This is dangerous because it logs sensitive data.

Here is a better approach:

```cs
_logger.LogInformation("User {Username} attempted login", username);
```

Another mistake is not handling exceptions properly:

```cs
try
{
    DoWork();
}
catch
{
    // ignored
}
```

This hides errors completely.

Correct approach:

```cs
try
{
    DoWork();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error while doing work");
    throw;
}
```

Another issue is inconsistent logging:

```cs
_logger.LogInformation("User created");
_logger.LogInformation("Created user successfully");
```

These logs are unclear and inconsistent.

Better:

```cs
_logger.LogInformation("User {UserId} created successfully", userId);
```

Consistency matters.

Finally, not testing logging is a mistake. You should verify:

- Logs are generated correctly
- Important events are logged
- Sensitive data is not logged

Production logging is not just about writing logs. It is about designing a system that provides visibility, safety, and performance.

Avoiding these mistakes will significantly improve your system reliability.
