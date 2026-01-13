---
title: 📰💥 You're Using HttpClient Wrong — And It's Costing You in Production
source: https://blog.yaseerarafat.com/youre-using-httpclient-wrong-and-it-s-costing-you-in-production-16a0325cb2d2
author:
  - "[[Yaseer Arafat]]"
published: 2025-07-20
created: 2026-01-07
description: 💥 You’re Using HttpClient Wrong — And It’s Costing You in Production Ever wondered why your high-performance .NET application occasionally grinds to a halt with inexplicable connection …
tags:
  - clippings
updated: 2026-01-07T23:08
---

# 📰💥 You're Using HttpClient Wrong — And It's Costing You in Production

![](<_resources/📰💥 You're Using HttpClient Wrong — And It's Costing You in Production/a0e6c2b331ff7a18c50dd7c97f13109b_MD5.webp>)

Ever wondered why your high-performance.NET application occasionally grinds to a halt with inexplicable connection errors? You're not alone. The default `HttpClient` might be a hidden trap, silently causing socket exhaustion and crippling your app's stability. In this post, we'll expose this critical issue and show you how `IHttpClientFactory` and Refit provide the definitive solution, transforming your network communication for good.

## 🚨 Why This Article Exists

It was a calm Sunday morning — until customer complaints flooded in.  
Latency skyrocketed. API calls hung. The cloud bill doubled overnight.  
The culprit? A tiny line of code creating a new `HttpClient` \*every single request.*

This post will save you \**hours of downtime**, **cloud costs**, and maybe your job.

![](<_resources/💥 You're Using HttpClient Wrong — And It's Costing You in Production/31b8e47b2756605243a82d531bfebe03_MD5.webp>)

Improper HttpClient use is a silent killer. The right DI pattern not only fixes performance but future-proofs your API calls.

## 🧠 The Silent Killer: new HttpClient()

Every time you write:

```c
var client = new HttpClient();
```

…you're \*quietly* leaking sockets. It looks fine — until it silently kills your app.

Sockets stay open longer than you think, threads pile up, and under load, your app crashes without warning — leaving your users frustrated and your team scrambling.

Let's see how to fix that.

### ✅ The Right Way: IHttpClientFactory + Dependency Injection

.NET Core fixed this with `IHttpClientFactory`.

```c
builder.Services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});
```

Inject and use it safely:

```c
public class MyService
{
    private readonly HttpClient _client;

    public MyService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("MyApi");
    }

    // Use _client safely here
}
```

Benefits:

- Socket reuse and pooling
- DNS refresh handling
- Configurable retries & policies

Now that you know the problem, here's the proven fix that saved me and countless others.

![](<_resources/📰💥 You're Using HttpClient Wrong — And It's Costing You in Production/afcd04616493fe33cb14aeea3ffb882a_MD5.webp>)

Don't let rogue HttpClient calls burn your ports. Manage it like a pro with IHttpClientFactory.

## 🤖 Simplify with Refit — Type-Safe REST Calls

Refit turns interfaces into HTTP calls, making your code cleaner and tests easier.

```c
public interface IWeatherApi
{
    [Get("/weather/today")]
    Task<WeatherDto> GetTodayAsync();
}
```

Register with DI:

```c
builder.Services.AddRefitClient<IWeatherApi>()
       .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"));
```

Inject & call:

```c
public class WeatherController
{
    private readonly IWeatherApi _api;

    public WeatherController(IWeatherApi api)
    {
        _api = api;
    }

    public async Task<IActionResult> Get()
    {
        var weather = await _api.GetTodayAsync();
        return Ok(weather);
    }
}
```

Say goodbye to messy, fragile code. Welcome confidence and clean tests.

## 🔐 Add Authorization & API Keys with a Handler

Create a handler to inject tokens and keys automatically:

```c
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenService _tokenService;

    public AuthHeaderHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        request.Headers.Add("X-API-Key", "your-api-key");

        return await base.SendAsync(request, cancellationToken);
    }
}
```

Register everything:

```c
builder.Services.AddTransient<AuthHeaderHandler>();

builder.Services
    .AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddHttpMessageHandler<AuthHeaderHandler>();
```

![](<_resources/📰💥 You're Using HttpClient Wrong — And It's Costing You in Production/ebd23c421fcdab422fb730ae62d3788d_MD5.webp>)

Token handler flow: fetching and injecting bearer tokens and API keys

## 🔍 Why This Matters

- Auto-refresh tokens without polluting your service code
- Securely add API keys every request
- Keep your clients clean, reusable, and secure

## ⚠️ Anti-Patterns to Avoid

❌ \**Creating a new** `**HttpClient**` **instance every request**  
→ ✅ Use `IHttpClientFactory` or Refit typed clients instead

❌ \**Using a static** `**HttpClient**` **instance for the app's lifetime**  
→ ✅ Use typed clients with Dependency Injection and message handlers

❌ \**Hardcoding retry logic manually**  
→ ✅ Use Polly policies via `AddPolicyHandler` for resilience

❌ \**Manually handling JSON serialization/deserialization**  
→ ✅ Let Refit handle it automatically with `System.Text.Json`

## 🔥 Real Impact

A fintech startup I consulted had random crashes during peak hours.

They used 20+ `new HttpClient()` calls all over.

After switching to `AddHttpClient()` + Refit + auth handlers, latency dropped 70% and stability hit 100%.

## 🚀 Final Takeaway

Still writing `new HttpClient()`?  
You're on borrowed time.

Use typed clients, DI, auth handlers, and Refit to ship rock-solid, scalable.NET apps.

## 📌 Bonus Box — Starter Template

```c
builder.Services.AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddHttpMessageHandler<AuthHeaderHandler>()
    .AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    => HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

This snippet demonstrates a robust way to configure an API client using `IHttpClientFactory` and Refit, with added resilience:

- `AddRefitClient<IMyApi>()`: Registers a Refit client for the `IMyApi` interface, leveraging `IHttpClientFactory` for proper `HttpClient` management.
- `ConfigureHttpClient(...)`: Sets the base URL for API requests, ensuring all calls to `IMyApi` go to `[https://api.example.com](https://api.example.com./)`[.](https://api.example.com./)
- `AddHttpMessageHandler<AuthHeaderHandler>()`: Injects a custom message handler (e.g., `AuthHeaderHandler`) to automatically add authentication headers to every outgoing request.
- `AddPolicyHandler(GetRetryPolicy())`: Integrates a retry policy using Polly, which automatically retries transient HTTP errors up to 3 times with exponential back-off, significantly improving API call reliability.
