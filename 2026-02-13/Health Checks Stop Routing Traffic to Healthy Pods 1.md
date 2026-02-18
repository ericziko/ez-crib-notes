---
title: 'Health Checks Stop Routing Traffic to "Healthy" Pods 1'
source: https://medium.com/write-a-catalyst/net-10-health-checks-stop-routing-traffic-to-healthy-pods-05b4461773ac
author:
  - "[[Abe Jaber]]"
published: 2026-01-19
created: 2026-02-13T00:00:00
description: Stop routing traffic to dying pods. Split /health/live vs /health/ready, add timeboxed DB/Redis/downstream checks, map Degraded, and avoid health checks DDoSing prod.
tags:
  - clippings
uid: bc460dfd-b78d-45c3-9acb-7ad0e0589502
aliases:
  - 'Health Checks Stop Routing Traffic to "Healthy" Pods 1'
linter-yaml-title-alias: 'Health Checks Stop Routing Traffic to "Healthy" Pods 1'
modified: 2026-02-13T22:23:22
---

#  Health Checks Stop Routing Traffic to "Healthy" Pods 1

![](https://miro.medium.com/v2/resize:fit:640/format:webp/0*t2sQB0Yj0V2YJWQG)

Photo by Agence Olloweb on Unsplash

**Your** `**/health**` **endpoint says OK while your DB is on fire? it's because you're checking the wrong thing.**

This is how the worst outages stay alive:

- Real requests are failing (timeouts, 500s, DB pool exhaustion)
- But your load balancer / Kubernetes keeps routing traffic to the same dying pods
- Because your health check is basically… **"can I return HTTP 200?"**

Health checks *aren't* for you to feel good. They're for orchestrators and load balancers to make routing and restart decisions.

By the end of this post you'll have:

- `**/health/live**` (liveness): *"process is alive"*
- `**/health/ready**` (readiness): *"safe to receive traffic"*
- Dependency checks with **timeouts**, **degraded states**, and **anti-DDoS rules** so your probes don't become the outage.

## The core misunderstanding: ping ≠ ready

## Liveness protects your process

Kubernetes uses **liveness** probes to decide when to restart a container (deadlocks, stuck process, unrecoverable state).

## Readiness protects your users

Kubernetes uses **readiness** probes to decide whether the pod should receive traffic. When a pod is **not ready**, it gets removed from Service load balancers.

If you only have one `/health` endpoint, you usually end up with one of these two disasters:

1. You include DB checks in `/health` → transient DB issue → pods flap and restart → **restart storms**
2. You only check "app responds" → DB is dead → pods stay "healthy" → **traffic keeps flowing to broken instances**

## What a real readiness check should validate (without getting expensive)

Readiness should answer one question:

> ***Can this instance serve real traffic right now?***

For most APIs, that usually means *minimal* checks like:

- DB connectivity (fast, cheap)
- Cache connectivity (optional, if your API *needs* it)
- One critical downstream (only if your API can't function without it)

Rules that keep it production-safe:

- **Every dependency check needs a timeout** (probes that hang become their own incident)
- **Keep checks cheap** (no big queries, no full table scans, no "warm up the world")
- **Fail readiness fast** when critical dependencies are broken
- **Don't fail liveness** just because a dependency is temporarily unhealthy (avoid restart loops)

## split endpoints the right way (Minimal API /.NET 10)

This is the "two endpoint" pattern Microsoft shows:

- readiness runs tagged checks
- liveness runs **no checks** and returns 200 if the process is alive

## 1) Register health checks (tag readiness dependencies)

```rb
using Microsoft.Extensions.Diagnostics.HealthChecks;
var builder = WebApplication.CreateBuilder(args);
// Core health checks
var hc = builder.Services.AddHealthChecks();
// Tag your dependency checks as "ready"
hc.AddDbContextCheck<AppDbContext>(
    name: "db",
    failureStatus: HealthStatus.Unhealthy,
    tags: new[] { "ready" });
// Example: a cheap Redis check via IDistributedCache (optional)
hc.AddCheck<RedisPingHealthCheck>(
    name: "redis",
    failureStatus: HealthStatus.Degraded,
    tags: new[] { "ready" });
// Example: a critical downstream ping (optional)
hc.AddCheck<DownstreamHttpHealthCheck>(
    name: "payments",
    failureStatus: HealthStatus.Degraded,
    tags: new[] { "ready" });
var app = builder.Build();
```

**Why** `**AddDbContextCheck**` **is solid:** by default it uses `CanConnectAsync(CancellationToken)` to test DB connectivity (cheap + direct).

## 2) Map /health/live and /health/ready

```rb
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    // readiness: dependency-aware
    Predicate = check => check.Tags.Contains("ready"),
    // treat degraded as "not ready" if your API can't function properly
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    // liveness: exclude all checks (always 200 if process can respond)
    Predicate = _ => false
});
```

This exact split (ready tag + live predicate false) is a recommended pattern in the ASP.NET Core health checks guidance.

## Dependency checks with timeouts (so probes don't hang)

Health checks are **just code**. If you write a check that can hang, your probe can hang too.

Here are two lightweight "timeout-safe" custom checks you can copy/paste.

## Redis ping check (IDistributedCache)

```rb
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
public sealed class RedisPingHealthCheck : IHealthCheck
{
    private readonly IDistributedCache _cache;
    public RedisPingHealthCheck(IDistributedCache cache) => _cache = cache;
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(300));
        try
        {
            // Cheap: "does the cache respond?" (null is fine)
            await _cache.GetAsync("__health", cts.Token);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Redis health check timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Redis check failed: {ex.Message}");
        }
    }
}
```

## Downstream HTTP check (timeboxed)

```rb
using System.Net.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
public sealed class DownstreamHttpHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    public DownstreamHttpHealthCheck(IHttpClientFactory httpClientFactory)
        => _httpClientFactory = httpClientFactory;
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(500));
        try
        {
            var client = _httpClientFactory.CreateClient("payments-probe");
            using var resp = await client.GetAsync("/health", cts.Token);
            return resp.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Degraded($"Payments returned {(int)resp.StatusCode}");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Degraded("Payments probe timed out.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded($"Payments probe failed: {ex.Message}");
        }
    }
}
```

## The production traps (this is where incidents are born)

## Trap 1: Health checks that DDoS your DB

Your orchestrator might hit readiness every few seconds *per pod*.

If your readiness check runs a heavy query, congratulations: you built a **DB load generator**.

Keep DB checks cheap:

- use connectivity (`CanConnectAsync`)
- or `SELECT 1` style checks
- never scan tables
- never "warm up caches" inside a probe

ASP.NET Core health checks are meant to validate infrastructure and routing decisions and they aren't a free performance test.

## Trap 2: Checks that hang (no timeouts)

A hung probe under load can pile up:

- thread usage
- outbound calls
- DB connections

Timebox everything.

## Trap 3: "Healthy" while you're saturated

A DB might be reachable, but the pool is exhausted and every real request times out.

If you've lived this, you can add a saturation signal:

- queue depth
- DB pool wait time
- thread pool starvation metrics
- dependency latency threshold

(Keep it fast. Don't do expensive diagnostics in the probe path.)

## Trap 4: One instance is dying, the system is fine

This is *exactly* why readiness exists:

- remove the bad instance from rotation
- keep the rest serving
- avoid taking the whole system down

Kubernetes explicitly uses readiness to decide which pods receive traffic.

## Practical rules: when to fail readiness (and when NOT to)

Fail **readiness** when:

- DB connectivity check fails quickly
- Redis/cache is required and unavailable
- critical downstream is timing out or consistently failing

Do **not** fail **liveness** just because:

- a dependency is temporarily down
- you're degraded but still capable of recovery
- you want to "self-heal" by restarting (that usually makes it worse)

Kubernetes warns that incorrect liveness probes can cause cascading failures (restarts under load, failed requests, extra load on remaining pods).

## Bonus: keep health endpoints from being publicly abused

If your `/health/ready` hits dependencies, you probably don't want random internet traffic spamming it.

ASP.NET Core lets you lock down health endpoints with:

- host/port restrictions (`RequireHost`)
- auth (`RequireAuthorization`)

Also: by default, the middleware suppresses caching headers to prevent caches from storing probe responses (you *usually* want that). If you ever need to control caching behavior, there's `AllowCachingResponses`.

## Prove it's working (what "good" looks like)

Before:

- users get errors, but pods stay "healthy"
- rollouts send traffic too early
- incidents linger because traffic keeps hitting broken nodes

After:

- readiness flips fast when dependencies are dead
- broken pods get removed from rotation quickly
- deployments stabilize
- recovery is faster because the system stops feeding traffic into dying instances

What to watch:

- readiness failures count
- error rate during dependency incidents
- time-to-recovery (how quickly traffic stops hitting broken pods)

## CTA

Go check your repo right now:

- Do you have only one `/health` endpoint?
- Does it just return 200 because the app can respond?

Split it into:

- `/health/live` (no dependency checks)
- `/health/ready` (timeboxed dependency checks + sane status mapping)

Then come back and comment your worst **"health check said OK"** outage story, I'll reply with how I'd model readiness for that system.

[![Write A Catalyst](https://miro.medium.com/v2/resize:fill:96:96/1*KCHN5TM3Ga2PqZHA4hNbaw.png)](https://medium.com/write-a-catalyst?source=post_page---post_publication_info--05b4461773ac---------------------------------------)

[![Write A Catalyst](https://miro.medium.com/v2/resize:fill:128:128/1*KCHN5TM3Ga2PqZHA4hNbaw.png)](https://medium.com/write-a-catalyst?source=post_page---post_publication_info--05b4461773ac---------------------------------------)

[Last published 8 hours ago](https://medium.com/write-a-catalyst/the-best-remote-job-boards-all-use-ai-now-meet-the-new-establishment-8270fea4d496?source=post_page---post_publication_info--05b4461773ac---------------------------------------)

Write A Catalyst and Build it into Existence.

Senior Software Developer C#/NET | Blockchain user and contributor | Free thinker

## More from Abe Jaber and Write A Catalyst

## Recommended from Medium

[

See more recommendations

](<https://medium.com/?source=post_page---read_next_recirc--05b4461773ac--------------------------------------->)w
