---
title: ".NET 10 Background Services: The Complete Production Setup That Stops Silent Failures"
source: https://jber595.medium.com/net-10-background-services-the-complete-production-setup-that-stops-silent-failures-65bfa99e1a77
author:
  - "[[Abe Jaber]]"
published: 2026-04-29
created: 2026-06-15
description: More
tags:
  - clippings
uid: 01KV5YVG2VMB6Y62ZTJPGKZPER
---
# _NET 10 Background Services The Complete Production Setup That Stops Silent Failures
![](https://miro.medium.com/v2/resize:fit:1400/format:webp/0*j4YI2Iybcylmb1jL)

Photo by Namroud Gorguis on Unsplash

A background service that polls every 30 seconds. You deployed it Friday afternoon. Monday morning you realize it stopped running 18 hours ago.

No exception in your logs. No alert. The health check is still green. The pod is fine. Your jobs just stopped, silently, and nobody noticed.

This is not a hypothetical. This is what happens when you copy a BackgroundService tutorial from Microsoft Learn and ship it without the five settings that turn it into something production can trust.

This article is the complete production template. Five silent failures. Five fixes. One copy-paste setup at the end that handles all of them.

> ***🚨 HIRING: Remote Tech Talent****  
> 💰 $50–$120/hr | 🔥 Multiple Roles*
> 
> *Frontend • Backend • Full Stack • Mobile • AI/ML • DevOps  
> *[*👉* ***Apply Here***](https://optimhire.com/?ref_code=codetodeploy)

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*pkQFf0NUnzoFbVEVQCF5WQ.png)

## Failure 1: The exception behavior you set to “be safe” is the one that hides bugs

In.NET 6 and later, the default `BackgroundServiceExceptionBehavior` is `StopHost`. An unhandled exception in your background service crashes the entire host. That sounds bad, but it is correct behavior. The exception gets logged. Kubernetes sees the pod fail. Your alerts fire. You find out within minutes.

The wrong move, which a lot of teams make, is to “fix” this by switching to `Ignore`:

```c
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = 
        BackgroundServiceExceptionBehavior.Ignore;
});
```

Now the exception is logged once and your service quietly stops running. The host stays up. The web side of your app keeps serving requests. Your background loop is dead. No alerts.

**The fix:** Leave the default `StopHost`. Handle expected exceptions inside your service with try-catch and explicit recovery logic. Let the unexpected ones crash the host so your platform restarts the pod.

```c
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = 
        BackgroundServiceExceptionBehavior.StopHost; // explicit
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

Set it explicitly even though it matches the default. New developers reading your Program.cs need to see the choice was deliberate.

## Failure 2: DbContext from a singleton, slowly corrupting itself

`AddHostedService<T>()` registers your service as a singleton. The DI container creates one instance for the lifetime of the application. If you inject `AppDbContext` directly into the constructor, you get one DbContext that lives for the entire application lifetime.

DbContext is not thread-safe. Its change tracker accumulates entities forever. Connections get held open. Concurrent operations corrupt each other. The first hour looks fine. By Friday afternoon, queries are slow and SaveChanges throws inconsistency errors.

```c
public class OrderProcessor : BackgroundService
{
    private readonly AppDbContext _db; // ❌ singleton DbContext

    public OrderProcessor(AppDbContext db) => _db = db;
}
```

**The fix: create a DI scope per work iteration**

Inject `IServiceScopeFactory` instead. Create a scope inside your loop. Resolve the scoped services from that scope. Dispose the scope when the iteration ends.

```c
public class OrderProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await ProcessBatchAsync(db, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

The scope ends when `using` exits. The DbContext gets disposed. Connections return to the pool. The change tracker resets. Next iteration gets a fresh DbContext.

## Failure 3: Shutdown that hangs and gets SIGKILLed mid-transaction

Kubernetes sends SIGTERM when it wants your pod to stop..NET’s `ShutdownTimeout` defaults to 30 seconds. After that, SIGKILL. No cleanup. No graceful exit.

Your background service receives a CancellationToken in `ExecuteAsync`. Most tutorials show the right loop pattern but skip the part where you actually pass the token down through every async call.

```c
// ❌ Token checked at the top, ignored everywhere else
while (!stoppingToken.IsCancellationRequested)
{
    var orders = await db.Orders.ToListAsync(); // no token passed
    foreach (var order in orders)
    {
        await ProcessAsync(order); // no token passed
    }
    await Task.Delay(TimeSpan.FromSeconds(30)); // no token passed
}
```

When SIGTERM arrives, the token gets cancelled. The loop’s top-level check fires, but the in-flight `ToListAsync` keeps running. The foreach finishes processing whatever it started. The Task.Delay completes its full 30 seconds. By the time the loop ends, you've burned most of the shutdown budget.

**The fix: token everywhere, OperationCanceledException caught explicitly**

```c
while (!stoppingToken.IsCancellationRequested)
{
    try
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orders = await db.Orders
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync(stoppingToken);

        foreach (var order in orders)
        {
            stoppingToken.ThrowIfCancellationRequested();
            await ProcessAsync(order, stoppingToken);
        }
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
    }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
    {
        break; // expected during shutdown
    }
}
```

The token reaches every awaited call. When it cancels, every operation throws `OperationCanceledException` immediately. The `when` filter only catches the cancellation if shutdown was actually requested, so unrelated cancellations still surface as bugs.

## Failure 4: The exception swallow that becomes an infinite-failure loop

You’re worried about Failure 1 (the host stopping on exceptions). So you wrap your work in a catch-all and keep the loop running:

```c
catch (Exception ex)
{
    _logger.LogError(ex, "Processing failed");
    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
}
```

Now your service hits the same broken state every 30 seconds, logs the same error, and never recovers. CPU burns. Logs flood. Your alerting system gets desensitized to the noise. Real failures hide inside the spam.

**The fix: exponential backoff with a failure threshold**

```c
private int _consecutiveFailures = 0;

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await ProcessBatchAsync(db, stoppingToken);
            _consecutiveFailures = 0;
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            break;
        }
        catch (Exception ex)
        {
            _consecutiveFailures++;
            _logger.LogError(ex,
                "Order processing failed (attempt {Failures}). Backing off.",
                _consecutiveFailures);
            if (_consecutiveFailures >= 5)
            {
                _logger.LogCritical(
                    "Order processor failed {Failures} times in a row. Stopping host.",
                    _consecutiveFailures);
                throw;
            }
            var backoffSeconds = Math.Min(60, Math.Pow(2, _consecutiveFailures));
            await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), stoppingToken);
        }
    }
}
```

Two seconds, four seconds, eight, sixteen, thirty-two. After five consecutive failures, rethrow. The host stops. Kubernetes restarts the pod. You get the alert. A successful run resets the counter.

This pattern handles transient failures (DB hiccup, downstream timeout) without letting permanent failures (broken connection string, schema drift) burn your CPU forever.

## Failure 5: A health check that lies about whether your service is alive

Your background service has been dead for 6 hours. Your `/health` endpoint returns 200. Why? Because the health check pipeline knows nothing about your background service. It only knows that the web host is alive.

Kubernetes never restarts the pod. Your monitoring stays green. Your jobs stop running. You find out from a customer.

**The fix: track last successful run, expose it through IHealthCheck**

Step 1, a singleton state holder:

```c
public class OrderProcessorState
{
    public DateTimeOffset? LastSuccessfulRun { get; set; }
}
```

Register it as a singleton:

```c
builder.Services.AddSingleton<OrderProcessorState>();
```

Step 2, update it from your background service after every successful run:

```c
public class OrderProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderProcessorState _state;
    private readonly ILogger<OrderProcessor> _logger;
    // constructor injects all three

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ... loop body
        await ProcessBatchAsync(db, stoppingToken);
        _state.LastSuccessfulRun = DateTimeOffset.UtcNow;
        // ...
    }
}
```

Step 3, the health check that reads it:

```c
public class OrderProcessorHealthCheck : IHealthCheck
{
    private readonly OrderProcessorState _state;
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(5);

  public OrderProcessorHealthCheck(OrderProcessorState state) 
        => _state = state;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
      {
        if (_state.LastSuccessfulRun is null)
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("Order processor has not run yet."));
        }
        var staleness = DateTimeOffset.UtcNow - _state.LastSuccessfulRun.Value;
        return staleness > StaleThreshold
            ? Task.FromResult(HealthCheckResult.Unhealthy(
                $"Last run was {staleness.TotalMinutes:F0} minutes ago."))
            : Task.FromResult(HealthCheckResult.Healthy());
    }
}
```

Step 4, register both:

```c
builder.Services.AddHealthChecks()
    .AddCheck<OrderProcessorHealthCheck>("order-processor");
```

Now `/health` returns Unhealthy when your background service hasn't run in 5 minutes. Kubernetes restarts the pod. Your alerts fire. The lying health check is gone.

## The complete production template

Drop this into any new background service. Five fixes baked in.

```c
public class OrderProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderProcessorState _state;
    private readonly ILogger<OrderProcessor> _logger;
    private int _consecutiveFailures = 0;

  public OrderProcessor(
        IServiceScopeFactory scopeFactory,
        OrderProcessorState state,
        ILogger<OrderProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _state = state;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order processor started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ProcessBatchAsync(db, stoppingToken);
                _state.LastSuccessfulRun = DateTimeOffset.UtcNow;
                _consecutiveFailures = 0;
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _logger.LogError(ex,
                    "Order processing failed (attempt {Failures}). Backing off.",
                    _consecutiveFailures);
                if (_consecutiveFailures >= 5)
                {
                    _logger.LogCritical(
                        "Order processor failed {Failures} times in a row. Stopping host.",
                        _consecutiveFailures);
                    throw;
                }
                var backoffSeconds = Math.Min(60, Math.Pow(2, _consecutiveFailures));
                await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), stoppingToken);
            }
        }
        _logger.LogInformation("Order processor stopped.");
    }
    private static async Task ProcessBatchAsync(
        AppDbContext db, CancellationToken ct)
    {
        // your batch logic here
        await Task.CompletedTask;
    }
}
```

Program.cs registration:

```c
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = 
        BackgroundServiceExceptionBehavior.StopHost;
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<OrderProcessorState>();
builder.Services.AddHostedService<OrderProcessor>();
builder.Services.AddHealthChecks()
    .AddCheck<OrderProcessorHealthCheck>("order-processor");
```

## The 5-point audit for existing background services

Run this against every BackgroundService in your codebase right now:

1. Is `BackgroundServiceExceptionBehavior` set to `Ignore`? If yes, change it to `StopHost`.
2. Are scoped services injected directly into the constructor? If yes, switch to `IServiceScopeFactory`.
3. Is the CancellationToken passed to every awaited call inside the loop? If not, add it.
4. Is there a catch-all without a failure threshold and exponential backoff? If yes, add both.
5. Does any health check report on whether the service has actually run recently? If not, add the state-tracking pattern.

Each fix takes 5 to 15 minutes. The combined effect is a service that fails loudly when it should and recovers quietly when it can.

Follow me on Medium. I publish.NET production fixes every week.

## Thank you for being a part of the community

*Before you go:*

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*d9QTaaaxboQP_gKSLedW_w.png)

👉 Be sure to **clap** and **follow** the writer ️👏 **️️**

👉 Follow us: [**Linkedin**](https://www.linkedin.com/in/bhumika-ch-3784391b9/) | [**Medium**](https://medium.com/codetodeploy)

👉 CodeToDeploy Tech Community is live on Discord — [**Join now!**](https://discord.gg/ZpwhHq6D)

**Disclosure:** This post includes affiliate and partnership links.