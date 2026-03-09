---
title: "Stop Calling BuildServiceProvider in .NET 10: The DI Mistake That Spawns Duplicate Singletons"
source: https://blog.stackademic.com/stop-calling-buildserviceprovider-in-net-10-the-di-mistake-that-spawns-duplicate-singletons-3131544f07d7
author:
  - "[[Abe Jaber]]"
published: 2026-02-08
created: 2026-02-17T00:00:00
description: BuildServiceProvider() creates a second DI container, duplicating “singletons” and causing weird prod bugs. Here’s the repro + copy/paste fixes that prevent it.
tags:
  - clippings
uid: a2c2f6de-5099-4648-b9f6-38ed4a772d44
modified: 2026-02-17T22:04:08
---
# Stop Calling BuildServiceProvider in .NET 10 The DI Mistake That Spawns Duplicate Singletons
[Sitemap](https://blog.stackademic.com/sitemap/sitemap.xml)## [Stackademic](https://blog.stackademic.com/?source=post_page---publication_nav-d1baaa8417a4-3131544f07d7---------------------------------------)

[![Stackademic](https://miro.medium.com/v2/resize:fill:76:76/1*U-kjsW7IZUobnoy1gAp1UQ.png)](https://blog.stackademic.com/?source=post_page---post_publication_sidebar-d1baaa8417a4-3131544f07d7---------------------------------------)

Stackademic is a learning hub for programmers, devs, coders, and engineers. Our goal is to democratize free coding education for the world.

![](https://miro.medium.com/v2/resize:fit:640/format:webp/0*IK9VzD3g2-tI-xd_)

Photo by benjamin lehman on Unsplash

It won’t fail fast. It’ll fail *weird;* duplicated background loops, inconsistent behavior, and memory that “slowly climbs for no reason.

We had a “singleton” that initialized twice.

Not in dev. Not during tests.

In production, after a few deploys… we started seeing **double warmups**, **duplicate timers**, and behavior that felt haunted:

- sometimes a cache looked warm
- sometimes it looked empty
- sometimes a background loop ran twice (so we did work twice)
- memory slowly climbed like a staircase

The code looked clean. No obvious leaks. No obvious bug.

The culprit was one line somebody added “just to grab a service during registration”:

```c
var sp = services.BuildServiceProvider();
```

That line doesn’t “grab a service”.

It **spawns a second DI universe** inside your app.

And your “singletons” stop being single.

## TL;DR (print this for code review)

- **Never** call `services.BuildServiceProvider()` inside app startup / registration.
- A **singleton is only singleton per container**. Build a second container → you get a second set of “singletons”.
- This causes **duplicated state**, **duplicated background work**, and often **memory/sockets that never get disposed**.
- Fix it with:
- `builder.Configuration` (not resolving config from DI)
- the **Options pattern** (`AddOptions().Bind()`)
- DI factory overloads (`sp => new(...)`)
- startup validation via **options validation** or a **hosted service**

## The villain: “I just need one service real quick”

This is the default assumption that creates incidents:

> *“DI is basically a bag. I can build it early, grab what I need, and keep registering stuff.”*

Nope.

When you call `BuildServiceProvider()`, you’re not “peeking”.

You are **constructing a container**. A real one. With real lifetimes. That can instantiate services.

## The mental model (two universes)

```c
Your app (what you THINK you have)
Host DI Container
      └── Singleton A (one instance)
      └── Singleton B (one instance)
      └── ...
What you ACTUALLY created
   Accidental Container (built early)
      └── Singleton A (instance #1)
      └── Singleton B (instance #1)
   Host DI Container (real one used by requests)
      └── Singleton A (instance #2)
      └── Singleton B (instance #2)
```

**Singleton means “one per container.”**  
If you build two containers, you get two worlds.

And that’s where “random” behavior comes from.

## Tiny repro (this will look familiar)

Here’s the exact crime scene pattern.

## ❌ Bad: building a provider during registration

```c
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<MySingleton>();
// ❌ "Just grab it real quick"
var earlyProvider = builder.Services.BuildServiceProvider();
_ = earlyProvider.GetRequiredService<MySingleton>();
// Later, the real host builds the real provider
var host = builder.Build();
// This resolves from the REAL container (creates another instance)
_ = host.Services.GetRequiredService<MySingleton>();
await host.RunAsync();
public sealed class MySingleton
{
    private static int _count;
    public MySingleton(ILogger<MySingleton> log)
    {
        var n = Interlocked.Increment(ref _count);
        log.LogWarning("MySingleton ctor called. Instance #{Instance}", n);
    }
}
```

**What you’ll see in logs:**

- `Instance #1` when you resolve from the accidental provider
- `Instance #2` when the host resolves from the real provider

That’s your “singleton” initializing twice.

Now imagine that singleton owns:

- a timer
- a `HttpClient` pipeline / handler
- a metrics exporter
- a queue consumer
- a cache dictionary that grows

Congrats. You just doubled it.

## Why this slowly kills production (and why it feels random)

This bug is nasty because it **doesn’t explode immediately**.

It creates *pressure*:

## 1) Duplicate singletons = inconsistent behavior

Any singleton that stores state can diverge:

- in-memory cache
- feature flag state
- counters / rate limiter buckets
- “only run once” guards
- circuit breaker state

Which one your code uses depends on which provider was used to resolve it.

## 2) Duplicate background work (the silent bill)

If a singleton starts loops/timers/long-running tasks:

- you do work twice
- you publish events twice
- you call dependencies twice
- p99 gets weird under load because extra background churn competes for CPU/threads

## 3) Leaks happen because the accidental provider isn’t tied to host lifetime

If you build a provider and never dispose it:

- background timers keep references alive
- sockets/handlers can stick around
- memory doesn’t drop when you expect

Even if you *do* dispose it, you still created a second universe (and may have already done expensive init twice).

## The 4 most common ways teams accidentally do this

## A) “I needed config, so I resolved IConfiguration”

You don’t need DI to read config. You already have it.

## B) “I needed IOptions<T> to register another service”

That’s a smell. Bind options properly instead of resolving them early.

## C) “I needed to log something during registration”

You can log later at runtime or use startup validation patterns.

## D) “I needed to run a startup check right there”

Use options validation or a hosted service that runs once at startup.

## The production-safe fixes (copy/paste)

## Fix #1: Need configuration? Use builder.Configuration

**❌ Bad**

```c
var sp = services.BuildServiceProvider();
var cfg = sp.GetRequiredService<IConfiguration>();
```

**✅ Good**

```c
var cfg = builder.Configuration;
var redisConn = cfg.GetConnectionString("Redis");
```

## Fix #2: Need typed settings? Use Options (don’t resolve early)

**✅ Good**

```c
builder.Services
    .AddOptions<RedisOptions>()
    .Bind(builder.Configuration.GetSection("Redis"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Redis:ConnectionString is required")
    .ValidateOnStart();

public sealed class RedisOptions
{
    public string ConnectionString { get; set; } = "";
}
```

Now you can inject `IOptions<RedisOptions>` anywhere *without building providers early*.

## Fix #3: Need another service while registering? Use factory overloads

This is the clean replacement for “let me build a provider and grab stuff”.

**✅ Good**

```c
builder.Services.AddSingleton<MyService>(sp =>
{
    var log = sp.GetRequiredService<ILogger<MyService>>();
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisOptions>>().Value;
    return new MyService(opts.ConnectionString, log);
});

public sealed class MyService(string redisConn, ILogger<MyService> log)
{
    // ...
}
```

Key point: **the host provider** invokes that lambda at the right time.

No second universe.

## Fix #4: Need startup checks? Use a hosted service (lifetime-safe)

**✅ Good**

```c
builder.Services.AddHostedService<StartupSmokeCheck>();
public sealed class StartupSmokeCheck : IHostedService
{
    private readonly ILogger<StartupSmokeCheck> _log;
    private readonly IServiceProvider _sp;
    public StartupSmokeCheck(ILogger<StartupSmokeCheck> log, IServiceProvider sp)
    {
        _log = log;
        _sp = sp;
    }
    public async Task StartAsync(CancellationToken ct)
    {
        _log.LogInformation("Startup checks...");
        using var scope = _sp.CreateScope();
        // Resolve scoped deps safely here
        var healthThing = scope.ServiceProvider.GetRequiredService<ISomething>();
        await healthThing.PingAsync(ct);
        _log.LogInformation("Startup checks OK.");
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

> This gives you the behavior people were trying to hack in with `BuildServiceProvider()` but correctly.

## “But I only used it once… is it still bad?”

Yes, because it’s the *worst kind of bug*:

- It works… until traffic, deploys, or timing makes the second universe matter.
- It creates drift and weirdness, not clean failures.
- It’s easy to forget it exists.

If you absolutely must build a provider (rare), you also must dispose it. But the real fix is: **don’t build it at all.**

## Copy/paste baseline for a clean.NET 10 API (no second container)

```c
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
// 1) Bind + validate options
builder.Services
    .AddOptions<MyOptions>()
    .Bind(builder.Configuration.GetSection("MyOptions"))
    .Validate(o => o.TimeoutMs is > 0 and < 10_000, "MyOptions:TimeoutMs must be 1..9999")
    .ValidateOnStart();
// 2) Register services using factory overloads
builder.Services.AddSingleton<MyClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<MyOptions>>().Value;
    var log  = sp.GetRequiredService<ILogger<MyClient>>();
    return new MyClient(TimeSpan.FromMilliseconds(opts.TimeoutMs), log);
});
// 3) Optional: startup checks using hosted service
builder.Services.AddHostedService<StartupSmokeCheck>();
var app = builder.Build();
app.MapGet("/ping", (MyClient client, CancellationToken ct) => client.PingAsync(ct));
app.Run();
public sealed class MyOptions
{
    public int TimeoutMs { get; set; } = 200;
}
public sealed class MyClient(TimeSpan timeout, ILogger<MyClient> log)
{
    public async Task<IResult> PingAsync(CancellationToken ct)
    {
        // pretend work
        await Task.Delay(10, ct);
        log.LogInformation("Ping OK with timeout {Timeout}", timeout);
        return Results.Ok(new { ok = true });
    }
}
```

## The screenshotable audit checklist

Run this on your repo today:

✅ **Search**

- `BuildServiceProvider(`
- `new ServiceCollection()` inside the app
- “temporary provider” comments

✅ **Look harder if you see**

- singletons that start timers/background loops
- “init” code that resolves services during registration
- duplicated telemetry/logging/metrics exporters

✅ **Rule**

- “No `BuildServiceProvider()` in startup. Ever.”

## CTA (the measurable move)

Do a 2-minute audit: **grep for** `**BuildServiceProvider**`.

If you find one, delete it and replace it with:

- `builder.Configuration`
- Options binding
- factory overloads (`sp => …`)
- startup hosted service (if you truly need checks)

Then watch what happens after deploy:

- fewer “mystery” behaviors
- less duplicated init
- memory trend flattens
- fewer “why is this running twice?” moments

If you want, paste the exact snippet you’re using `BuildServiceProvider()` for and I’ll rewrite it into the clean pattern without changing behavior.

[![Stackademic](https://miro.medium.com/v2/resize:fill:96:96/1*U-kjsW7IZUobnoy1gAp1UQ.png)](https://blog.stackademic.com/?source=post_page---post_publication_info--3131544f07d7---------------------------------------)

[![Stackademic](https://miro.medium.com/v2/resize:fill:128:128/1*U-kjsW7IZUobnoy1gAp1UQ.png)](https://blog.stackademic.com/?source=post_page---post_publication_info--3131544f07d7---------------------------------------)

[Last published 12 hours ago](https://blog.stackademic.com/junior-devs-write-god-classes-senior-devs-apply-these-5-solid-principles-in-spring-boot-d99fe7d6615f?source=post_page---post_publication_info--3131544f07d7---------------------------------------)

Stackademic is a learning hub for programmers, devs, coders, and engineers. Our goal is to democratize free coding education for the world.

Senior Software Developer C#/NET | Blockchain user and contributor | Free thinker

## More from Abe Jaber and Stackademic

## Recommended from Medium

[

See more recommendations

](https://medium.com/?source=post_page---read_next_recirc--3131544f07d7---------------------------------------)w