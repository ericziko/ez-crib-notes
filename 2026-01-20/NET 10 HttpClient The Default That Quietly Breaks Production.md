---
title: NET 10 HttpClient The Default That Quietly Breaks Production
source: "https://medium.com/codetodeploy/net-10-httpclient-the-default-that-quietly-breaks-production-9da38e11ea96"
author:
  - "[[Abe Jaber]]"
published: 2026-01-17
created: 2026-01-20T00:00:00
description: "Stop “random” p99 spikes with a safe-by-default .NET 10 HttpClient setup: real timeouts + CancellationToken propagation, DNS-safe pooled connections, and resilience without retry storms."
tags:
  - "clippings"
  - 1
  - 2
  - 3
modified: 2026-01-20T22:34:38
---

# NET 10 HttpClient The Default That Quietly Breaks Production

A Publication Where Tech People Learn, Build, And Grow. Follow To Join Our 500k+ Monthly Readers

"Everything was fine… until one downstream got slow and your API started timing out for *everything*."

If you've shipped backend APIs long enough, you've seen this movie:

- One dependency gets slow (payments, identity, internal service)
- Your requests **wait**
- While they wait, concurrency piles up
- The server starts queueing
- **p99 goes vertical**
- You get a 5xx/timeout storm that *looks random* but isn't

The villain is rarely "HttpClient is bad."

It's **defaults that are too polite** + missing guardrails.

Let's name the big one.

## The default that hurts: HttpClient waits 100 seconds

Most APIs should never wait this long for a downstream call:

```c
using var http = new HttpClient();
Console.WriteLine(http.Timeout); // 00:01:40 (100 seconds)
```

The default `HttpClient.Timeout` is **100 seconds**.

In API-world, 100 seconds is basically infinite.

Because "waiting" isn't free. Long waits keep connections in play, keep memory alive, keep work in-flight, and under load turn your service into its own queue.

Timeouts aren't "failures."  
Timeouts are **budgets**.  
A fast failure is often a reliability win.

So here are the 3 guardrails that prevent the classic incident:  
**(1) real timeouts + cancellation propagation**  
**(2) DNS-safe pooled connections**  
**(3) resilience without retry storms**

## Guardrail #1: Timeouts you can trust (cancellation propagation is non-negotiable)

The docs say two things most teams miss:

1. The same `HttpClient.Timeout` applies to all requests on that client.
2. You can set per-request timeouts using a `CancellationTokenSource`, and **the shorter timeout wins**.

That's huge.

It means your "budget" can be **per dependency** or even **per call**, and you can keep it consistent and measurable.

## The mistake: setting a timeout but not passing CancellationToken

If you don't propagate cancellation, you get **zombie work**:

- the request is already "over"
- but your downstream calls keep running
- your DB calls keep running
- your system stays hot and stressed

## The correct pattern (copy/paste mental model)

**Endpoint** `CancellationToken` **→ pass into every downstream call.**

Minimal API example:

```cs
app.MapGet("/checkout/status", async (HttpClient http, CancellationToken ct) =>
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(2)); // per-call budget
    
    using var resp = await http.GetAsync("https://payments.internal/status", cts.Token);
    resp.EnsureSuccessStatusCode();
    return Results.Ok(await resp.Content.ReadAsStringAsync(ct));
});
```

A few "senior" notes:

- Use `CreateLinkedTokenSource(ct)` so request cancellation (client disconnected, server timeout, etc.) cancels the downstream call too.
- Use `CancelAfter(...)` for per-call budgets; keep them realistic by endpoint type (fast reads vs heavy endpoints).
- Don't set everything to 200ms and call it "performance." That's just self-inflicted outages.

## Guardrail #2: The Kubernetes/DNS trap pooled connections live forever unless you rotate them

Here's the subtle one that bites in Kubernetes, service discovery, and "pods move" environments:

**HttpClient only resolves DNS when a connection is created. It doesn't track DNS TTL.**

If DNS entries change regularly, your client won't respect those updates… unless you rotate connections.

Also: by default, `SocketsHttpHandler.PooledConnectionLifetime` is **InfiniteTimeSpan**.

So you can end up with "stale endpoints" behavior when things shift.

## The fix: set PooledConnectionLifetime

Microsoft 's own guidance: limit connection lifetime so DNS gets re-queried when connections are replaced.

Example:

```cs
builder.Services.AddHttpClient("Payments", client =>
{
    client.BaseAddress = new Uri("https://payments.internal");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
});
```

What value should you choose?

- Docs use **15 minutes** as an illustration and say you should pick based on expected DNS/network changes.
- In high-churn environments (pods scaling/rotating), many teams start around **2–5 minutes** and tune.

This single line prevents a whole class of "it works… until it doesn't" issues.

## Guardrail #3: Resilience without retry-storming your dependencies

Now let's talk about the "helpful" thing that causes incidents: **retries**.

Retries can be good.  
Retries can also multiply load during an outage.

So the "adult rules" are:

- Retry **only idempotent** operations (GET, safe PUTs if designed that way)
- Keep an **overall timeout budget**
- Circuit-break when the dependency is unhealthy
- (Optionally) rate-limit outbound calls to avoid crushing a struggling downstream

In.NET 10, Microsoft's guidance is to use `Microsoft.Extensions.Http.Resilience` for HttpClient resilience (Polly-backed).

And they explicitly warn: **avoid stacking multiple resilience handlers randomly** use one, or use `AddResilienceHandler` to build a custom pipeline.

## Copy/paste: add the standard resilience handler

```cs
using Microsoft.Extensions.Http.Resilience;
builder.Services.AddHttpClient("Payments", client =>
{
    client.BaseAddress = new Uri("https://payments.internal");
})
.AddStandardResilienceHandler();
```

That gives you sensible defaults without Polly spaghetti.

## Make it "production safe" with explicit budgets

Most teams want to make the budget explicit so it's not magic.

```cs
using Microsoft.Extensions.Http.Resilience;
builder.Services.AddHttpClient("Payments", client =>
{
    client.BaseAddress = new Uri("https://payments.internal");
    client.Timeout = TimeSpan.FromSeconds(10); // keep >= total request budget
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
})
.AddStandardResilienceHandler(options =>
{
    // Overall budget (includes retries)
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(3);
    // Per-attempt budget (each try)
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
    // This matters: don't retry unsafe HTTP methods
    options.Retry.DisableForUnsafeHttpMethods();
});
```

You can keep it simple:

- overall timeout (total budget)
- attempt timeout
- safe retries only
- breaker defaults from standard handler

That's usually enough to stop "one slow dependency melts the whole API."

## Static HttpClient vs IHttpClientFactory: what I'd do in a real API

Microsoft summarizes recommended lifetime management like this:

- Use **long-lived static/singleton HttpClient** with `PooledConnectionLifetime` set, **or**
- Use **short-lived HttpClient from** `**IHttpClientFactory**` (typed clients recommended).

If you want a practical, team-friendly default:

✅ **Typed clients per dependency**  
`PaymentsClient`, `IdentityClient`, `CatalogClient`  
Each gets its own budgets, handler config, and resilience rules.

## Important cookie warning (easy to miss)

If your app requires cookies, Microsoft recommends avoiding `IHttpClientFactory` because handler pooling can share `CookieContainer` objects and potentially leak cookies between unrelated parts of the app (and cookies can be lost when the handler is recycled).

Most backend service-to-service calls don't use cookies, so typed clients via the factory are usually perfect.

## The screenshot block: a "safe-by-default" HttpClient baseline

If you only copy one thing from this post, copy this.

```cs
using Microsoft.Extensions.Http.Resilience;
builder.Services.AddHttpClient<PaymentsClient>(client =>
{
    client.BaseAddress = new Uri("https://payments.internal");
    // Default client timeout (acts as a safety net).
    // Per-call CTS timeouts can still be shorter (shorter wins).
    client.Timeout = TimeSpan.FromSeconds(10);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Rotate pooled connections to respect DNS / endpoint changes.
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
})
.AddStandardResilienceHandler(options =>
{
    // Keep the *total* budget bounded (including retries).
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(3);
    // Each attempt should be short.
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(1);
    // Avoid retrying unsafe methods by default.
    options.Retry.DisableForUnsafeHttpMethods();
});
public sealed class PaymentsClient(HttpClient http)
{
    public async Task<string> GetStatusAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(2)); // per-call budget
        using var resp = await http.GetAsync("/status", cts.Token);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync(ct);
    }
}
```

This gives you:

- budgets (client + total + per-attempt + per-call)
- cancellation propagation
- DNS-safe connection rotation
- resilience without retry storms

## Prove it in 15 minutes: measure before/after

Don't ship this on faith. Prove it.

**Before:**

- p95 / p99 latency
- dependency duration (payments, identity, internal services)
- timeout count
- 5xx rate during a downstream slowdown
- "in-flight request" pressure under load

**Test idea:**

- simulate a slow downstream (add a 2–5s delay in a dev/stage dependency)
- run a 5-minute load test
- watch p99 behavior before vs after guardrails

What you want:

- p99 stops going vertical
- timeouts fail fast instead of piling up
- the service recovers predictably when the downstream returns

## Ship-ready checklist

- Set sane budgets (client timeout + per-call CTS where needed)
- Propagate `CancellationToken` everywhere (no zombie work)
- Set `PooledConnectionLifetime` so DNS changes are respected
- Add resilience *once* (don't stack handlers randomly)
- Retry only idempotent calls; keep total budget bounded
- Measure p95/p99 + dependency duration before/after
