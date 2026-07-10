---
title: "The Only Middleware Pipeline I Trust in .NET 10 (After 4 Incidents)"
source: "https://jber595.medium.com/the-only-middleware-pipeline-i-trust-in-net-10-after-4-incidents-8cb559f94337"
author:
  - "[[Abe Jaber]]"
published: 2026-05-03
created: 2026-06-15
description: "More"
tags:
  - "clippings"
---
# The Only Middleware Pipeline I Trust in .NET 10 (After 4 Incidents)
![](https://miro.medium.com/v2/resize:fit:1400/format:webp/0*Hut75SOucGt_IG1q)

Photo by Quinten de Graaf on Unsplash

Two years ago a senior dev on our team moved `UseAuthentication()` one line in Program.cs. The deploy went out Tuesday afternoon. Wednesday morning we noticed authenticated users were randomly getting 401 responses. Not all of them. About 30%.

It took 4 hours to find. The fix was moving one line back.

After three more incidents like that, I stopped trusting “best guess” middleware orders. This is the complete pipeline I now use on every.NET 10 project, organized into 5 zones, with the production incident behind every rule.

> ***🚨 High-Paying Tech Roles Available****  
> 💰 $3K–$10K/Month Remote & Onsite Opportunities  
> ⚡ No long applications — just submit your profile in minutes  
> 🔎 Get matched with active hiring companies*  
> [**👉 Start Application (60 Seconds)**](https://optimhire.com/?ref_code=codetodeploy)

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*pkQFf0NUnzoFbVEVQCF5WQ.png)

## The 5 zones (mental model first, code at the end)

Most middleware ordering articles list 12 to 15 components and tell you to put them in a specific sequence. Memorizing that sequence is fragile. The moment you add a new middleware, you have to look up where it goes.

A better model: every middleware belongs to one of 5 zones. Zones run in order. Within a zone, components have a small number of well-defined positions.

The zones are:

1. **Outer wrapper** (catches everything)
2. **Cheap rejections** (fail fast before expensive work)
3. **Static and cached responses** (skip the rest of the pipeline when possible)
4. **Identity and access** (the order that breaks if you swap two lines)
5. **Endpoint layer** (the actual destination)

Once you place a new middleware into the right zone, the order inside that zone is usually obvious. Let me walk through each zone with the production incident that taught me the rule.

## Zone 1: The outer wrapper

```c
app.UseExceptionHandler("/error");
app.UseHsts();
app.UseHttpsRedirection();
```

`UseExceptionHandler` is the first line after `app = builder.Build()`. Always. It wraps every other middleware in a try-catch. Any exception thrown by middleware below it gets caught and converted to a clean error response. Any exception thrown by middleware above it leaks raw to the client.

`UseHsts` and `UseHttpsRedirection` enforce HTTPS at the protocol level before any work happens. HSTS sets the response header. HttpsRedirection issues the redirect for HTTP requests.

## Incident 1: UseExceptionHandler in the wrong place

We had a custom exception middleware that converted exceptions to ProblemDetails responses. It was registered after `UseRouting`. A bug in our forwarded headers configuration started throwing during request setup, which happens before routing.

The exception bubbled up past our handler. ASP.NET Core’s default behavior took over and sent the raw stack trace as the response body. In production. To real customers. For 20 minutes before we noticed.

The fix was making `UseExceptionHandler` the first line. Now nothing can throw "above" the handler.

## Zone 2: Cheap rejections

```c
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseRequestTimeouts();
```

This zone exists to reject requests we don’t want to spend CPU on. Rate-limited traffic, timed-out requests, and connections from sources we trust through the proxy chain.

`UseForwardedHeaders` has to come first because rate limiting reads the client IP. If forwarded headers haven't been processed, the rate limiter sees the load balancer's IP for every request, not the actual client. Every user behind the same proxy looks like one user.

`UseRequestTimeouts` is a.NET 10 feature that sets the deadline early so downstream work can be cancelled cleanly when it expires.

## Incident 2: Rate limiter blocking legitimate users behind corporate proxies

A customer support ticket came in. Users at a large enterprise customer kept hitting our rate limit. We checked the rate limit config. It was set to 100 requests per minute per IP. The enterprise customer had maybe 200 employees, all behind one corporate egress IP.

The rate limiter saw 200 employees as one IP. They burned through 100 requests in seconds. The rest of the day they were locked out.

The deeper bug: `UseForwardedHeaders` was registered, but it was below `UseRateLimiter` in the pipeline. The rate limiter ran first, saw the proxy's IP, and made decisions based on that. Forwarded headers got processed afterward, too late to matter.

Fix: `UseForwardedHeaders` at the top of zone 2. Now the rate limiter sees the real client IP from the X-Forwarded-For header chain.

## Zone 3: Static and cached responses

```c
app.UseResponseCompression();
app.MapStaticAssets();
```

This zone short-circuits the pipeline for content that doesn’t need authentication or business logic. Static files, compressed assets, and cached responses.

`UseResponseCompression` runs before static files so static responses get compressed..NET 10 ships `MapStaticAssets`, an optimized replacement for `UseStaticFiles` that handles fingerprinting and compression at build time.

Output caching also fits this concept, but it has a placement decision that depends on whether you cache anonymous content or per-user content. I cover that in zone 4 below because it interacts with authentication.

## Why static files come before authentication

A common mistake is putting `UseStaticFiles` after authentication. The result: every request for a CSS file, an image, a favicon goes through your full auth pipeline. JWT validation runs. Database lookups for user claims happen. All to serve a 2KB image that nobody needs to be authenticated to see.

Putting static files in zone 3, before zone 4, means anonymous static content skips the auth work entirely.

## Zone 4: Identity and access (the zone that breaks if you swap two lines)

```c
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseOutputCache();
```

This is the zone where order is non-negotiable. Every line here exists in a specific position because the line below it depends on something the line above it produced.

- `UseRouting` matches the request to an endpoint. CORS and auth need to know what endpoint they're protecting.
- `UseCors` runs before authentication because CORS preflight `OPTIONS` requests are sent without credentials. If auth runs first, preflight requests get rejected as unauthenticated.
- `UseAuthentication` populates `HttpContext.User`. Authorization reads from `HttpContext.User`.
- `UseAuthorization` enforces the policies that depend on the populated user.
- `UseAntiforgery` validates the antiforgery token against the authenticated identity. It needs the user to exist.

Output caching comes after authentication when you want per-user cached responses (the cache key includes the user identity). If your cached responses are anonymous and identical for everyone, you could move `UseOutputCache` into zone 3, but you lose the ability to cache per-user content. I default to "after auth" because the per-user case is more common in production APIs.

## Incident 3: UseAuthorization before UseAuthentication

This is the one I opened the article with. A senior dev moved `UseAuthentication()` one line. Specifically, they swapped these two:

```c
// What got committed
app.UseAuthorization();
app.UseAuthentication();
```

`UseAuthorization` ran first. It checked `HttpContext.User`. The user property was the default anonymous `ClaimsPrincipal` because `UseAuthentication` hadn't run yet. Authorization rejected the request as unauthenticated.

The reason it was only 30% of requests: some endpoints used the `[Authorize]` attribute with policies that bypassed pipeline-level authorization in certain cases. Other endpoints relied entirely on the pipeline. The behavior was inconsistent enough that we initially thought it was a token issue, then a claim issue, then a database issue.

Fix: `UseAuthentication` first. Always. Even if your IDE wants to alphabetize them, even if it looks ugly, the order is fixed.

## Incident 4: UseCors below UseAuthentication

A frontend deployment went out for our SPA. The next day, every API call from the SPA was failing with 401 errors during the preflight check. Our frontend devs blamed the API team. The API team blamed the CORS configuration.

The CORS config was correct. The preflight `OPTIONS` request, which browsers send before any cross-origin call with credentials, was being rejected by `UseAuthentication` because preflight requests don't carry the authentication header.

`UseCors` was below `UseAuthentication` in the pipeline. So preflight `OPTIONS` requests hit auth first, got rejected, and never reached the CORS handler that would have responded correctly.

Fix: `UseCors` above `UseAuthentication`. Preflight requests now get a proper CORS response before authentication has a chance to reject them.

## Zone 5: Endpoint layer

```c
app.UseMiddleware<RequestEnrichmentMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
```

Custom middleware sits closest to the endpoint because by this point in the pipeline, the request has been routed, the user is authenticated, and authorization has passed. Custom middleware that needs the matched route, the user identity, or tenant context belongs here.

Health checks live in this zone but typically need `.AllowAnonymous()` because external monitoring systems probe `/health` without credentials. If you put health checks before zone 4, they bypass everything cleanly. If you put them in zone 5 with `.AllowAnonymous()`, they still bypass auth but live alongside the rest of your endpoint mapping for clarity.

## The complete production pipeline

Drop this into any new.NET 10 project. The order is the same every time.

```c
var app = builder.Build();

// ZONE 1: Outer wrapper. Catches everything.
app.UseExceptionHandler("/error");
app.UseHsts();
app.UseHttpsRedirection();

// ZONE 2: Cheap rejections. Fail fast before expensive work.
app.UseForwardedHeaders();
app.UseRateLimiter();
app.UseRequestTimeouts();

// ZONE 3: Static and cached responses. Skip the pipeline when possible.
app.UseResponseCompression();
app.MapStaticAssets();

// ZONE 4: Identity and access. Order is non-negotiable.
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseOutputCache();

// ZONE 5: Custom middleware and endpoints.
app.UseMiddleware<RequestEnrichmentMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();
```

When a new middleware comes along (a new logging library, a new feature flag system, a new tenancy resolver), I figure out which of the 5 zones it belongs to first. The order within a zone is then constrained by which middleware in that zone produces context the new one needs.

## The 5-point audit for your existing Program.cs

Open your Program.cs right now. Run through these five checks.

1. Is `UseExceptionHandler` the first line after `app = builder.Build()`? If anything is above it, exceptions from those middleware components will leak raw to clients.
2. Is `UseForwardedHeaders` above every middleware that reads client IP or request scheme? If not, your rate limiter, your logging, and your audit trail are all seeing the proxy IP, not the real client.
3. Is `UseCors` above `UseAuthentication`? If not, browser preflight requests for cross-origin calls will fail intermittently in ways that are hard to reproduce.
4. Is `UseAuthentication` above `UseAuthorization`? If not, authorization is checking an empty `ClaimsPrincipal` and your `[Authorize]` attributes will reject authenticated users.
5. Is `UseOutputCache` placed deliberately? After auth for per-user caching, before auth for anonymous public content. Pick one based on what you cache, not by accident.

Each fix takes 30 seconds. The collective effect is a pipeline you can defend in code review and a Program.cs that won’t surprise you at 2am.

Follow me on Medium. I publish.NET production fixes every week.

The 4 hours we spent debugging that random 401 incident were the most expensive 4 hours of the quarter. The fix was moving one line. Every time someone on the team adds a new middleware now, they ask which zone it belongs in before they add it.

## Thank you for being a part of the community

*Before you go:*

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*d9QTaaaxboQP_gKSLedW_w.png)

👉 Be sure to **clap** and **follow** the writer ️👏 **️️**

👉 Follow us: [**Linkedin**](https://www.linkedin.com/in/bhumika-ch-3784391b9/) | [**Medium**](https://medium.com/codetodeploy)

👉 CodeToDeploy Tech Community is live on Discord — [**Join now!**](https://discord.gg/ZpwhHq6D)

**Disclosure:** This post includes affiliate and partnership links.