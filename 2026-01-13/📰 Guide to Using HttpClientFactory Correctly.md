---
title: 📰 Guide to Using HttpClientFactory Correctly
source: https://medium.com/@jordansrowles/jordans-net-guide-to-using-httpclientfactory-correctly-9f371ece9c88
author:
  - "[[Jordan Rowles]]"
published: 2025-12-20
created: 2026-01-07
description: "Jordan’s .NET Guide to Using HttpClientFactory Correctly Socket exhaustion (often encountered as SocketException: Cannot assign requested address) is one of those issues you don’t really catch on …"
tags:
  - clippings
updated: 2026-01-07T22:17
uid: 60ede37c-cf92-4d2f-8853-dd93c33cedd7
---

# 📰 Guide to Using HttpClientFactory Correctly

Socket exhaustion (often encountered as `SocketException: Cannot assign requested address`) is one of those issues you don't really catch on your local machine, but can easily show itself in production. It works fine in development, works fine in staging with a light load test, and then production traffic hits and suddenly you're bleeding out sockets until the OS says "no more" and your app falls over.

The fix to using HttpClient, is HttpClientFactory. So I'll very quickly go over it in this article.

```csharp
Socket Exhaustion and TIME_WAIT
    The Classic Anti-Pattern
    Diagnosing Socket Exhaustion in the Wild
    Why this wasn't a problem before?
Naive Fix: Singleton HttpClient Without Configuration
    The Correct Way to Singleton a HttpClient
Better Fix: HttpClientFactory
    Internals
    Why 2 Minutes?
    Basic Setup
    Named Clients
    Typed Clients
```

## Socket Exhaustion and TIME\_WAIT

So, I'll jump right into the low stuff, and look at what happens at the protocol level when you create and dispose an `HttpClient` object. Understanding that is helpful in knowing why the standard pattern of scoping `HttpClient` in a using block fails.

When you create a HttpClient object and make a request, the OS opens a TCP socket. That socket has two endpoints: your local address (IP + port), and a remote address (IP + port).

The local port that's chosen is from the ephemeral port range. On Windows (by default) that's 49152 to 65535, giving you about 16,384 ports. On Linux it's typically bigger at around 32768 to 60999, giving 28,000 ports.

When you dispose it, the underlying socket *doesn't* close, it goes into a `TIME_WAIT` state, which is a part of the TCP spec (RFC 9293). TIME\_WAIT is there to ensure any stray packets from the old connection don't accidentally get delivered to a new connection (if it happens to use the same port).

`TIME_WAIT` 's duration is 2–4 minutes (OS dependent). On Windows it's 4 minutes (240 seconds) by default. On Linux it's 60 seconds, and technically can't be changed. Changing `tcp_fin_timeout` doesn't actually control `TIME_WAIT`, it manages `FIN-WAIT-2`. `TIME_WAIT` is baked into the TCP stack.

Still, seconds are still way to slow for computers, and can cause some issues if we ignore it. While the socket is in TIME\_WAIT, it's still consuming an entry in the local port space. If you're creating and disposing HttpClient instances rapidly, say like 100 requests per second, you're tearing through 100 ports per second.

Doing the maths for Windows, that's 163 seconds (< 3 minutes) before port exhaustion.

### The Classic Anti-Pattern

Here's an example of some code that can cause this

```csharp
public async Task<string> GetDataAsync(string url)
{
    using var client = new HttpClient();
    return await client.GetStringAsync(url);
}
```

Seems reasonable, no? You create the object, the use it, and then dispose of it. For most types in.NET that implement `IDisposable`, this pattern is exactly what you're suppose to do (the `using`). `HttpClient` is special, in that the disposable pattern will hurt you here.

Every call to this method create a new `HttpClient` object, which creates a new `HttpClientHandler` object, which in turn creates a new `SocketsHttpHandler`, which opens the TCP connection. When I dispose the object at the end (method return), the connection enters `TIME_WAIT`, and that port is locked up for at least a minute.

### Diagnosing Socket Exhaustion in the Wild

So how do we tell if we're encountering this issue? Obviously firstly would be an exception of some kind, like so

```csharp
System.Net.Http.HttpRequestException: Cannot assign requested address
---> System.Net.Sockets.SocketException: Cannot assign requested address
```

On Windows we can list sockets with `netstat`

```csharp
netstat -ano | findstr TIME_WAIT | measure
```

On Linux,

```csharp
ss -tan | grep TIME-WAIT | wc -l
```

If you see thousands of connections in `TIME_WAIT`, you've got a problem. On a healthy system, you might have a few dozen. On a system with socket exhaustion, you'll have thousands, and they'll all be to the same remote endpoint.

You'll see output that looks kind of like this

```csharp
TIME-WAIT  0      0      192.168.1.100:54321   93.184.216.34:443
TIME-WAIT  0      0      192.168.1.100:54322   93.184.216.34:443
TIME-WAIT  0      0      192.168.1.100:54323   93.184.216.34:443
...thousands more...
```

Notice the local ports incrementing? That'll be your app burning through the ephemeral port range.

To check the ephemeral port range on Window, and Linux (respectfully), is

```csharp
netsh int ipv4 show dynamicport tcp
```

```csharp
cat /proc/sys/net/ipv4/ip_local_port_range
```

### Why This Wasn't a Problem Before?

If you wasn't encountering this issue, but now are, there could be a few reasons

1. Load, you could be handling more traffic now (10 /s vs 100 /s)
2. Microservices making more outbound HTTP calls than it's used to, every service-to-service call would be a new socket
3. Container port ranges can be more restricted
4. Cloud load balancers keeping connections open longer

## Naive Fix: Singleton HttpClient Without Configuration

The obvious fix seems to be, just create HttpClient as a singleton and share the instance across all requests, like so

```csharp
public class MyService
{
    private static readonly HttpClient _client = new HttpClient();

    public async Task<string> GetDataAsync(string url)
    {
        return await _client.GetStringAsync(url);
    }
}
```

This actually does solve the socket exhaustion issue, because we're only opening one connection (or a small pool of them), and they stay open, so we have no WAIT\_STATE buildup.

But now a new bug comes out, the app cannot resolve the name `api.example.com/v1`. The APIs IP address has changed (this can happen with something like failover, or a DNS-based load balancing). Requests are being sent to a "stale" IP address because the singleton (that has never been disposed and recreated), still points to the wrong host.

Welcome to the DNS resolution issue.

### Naive Fix: The Correct Way to Singleton a HttpClient

To prevent this, we can configure `HttpClient` 's pooled connection lifetime, like so

```csharp
public class MyService
{
    private static readonly HttpClient _client = new HttpClient(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2)
    });

    public async Task<string> GetDataAsync(string url)
    {
        return await _client.GetStringAsync(url);
    }
}
```

Setting the `PooledConnectionLifetime` tells the handler: "After this duration, close the pooled connections and create new ones".

When a connection is recreated, a new connection is made, meaning that another DNS resolution happens again, so it pick up the IP change.

This pattern works fine for simpler applications where you don't need things like

- Multiple named/typed client instances with different configurations
- DI integration for better testability
- Per-client handler pipelines
- Centralised handler lifetime management

This is where `HttpClientFactory` steps up as the better choice. To spoil the story, simply put, the factory does the handler rotation for us, and integrates cleanly with ASP.NET Core's DI system, giving near endless amounts of configuration options (if your using the options pattern to populate your IConfiguration).

## Better Fix: HttpClientFactory

Introduced in.NET Core 2.1, specifically designed to solve both these problems (socket exhaustion and the DNS resolution).

The problem isn't HttpClient itself, but the underlying HttpMessageHandler. The handler is what manages the actual socket connections and DNS caching.

### Internals

`HttpClientFactory` maintains the pool of `HttpMessageHandler` instances. When you call `CreateClient()`, the factory

1. Creates a lightweight HttpClient wrapper
2. Assigns it a handler from the pool of connections
3. Tracks the handlers age, and rotates out after a configurable amount of time (default is 2 minutes)

The handlers are shared across multiple instances of HttpClient. So you could be creating 1000 HttpClient objects, but they might all be sharing just a few handlers under the hood. This already solves the socket exhaustion without us doing anything, giving us the benefit of a singleton but without the DNS issue.

Here's a simplified metal model of what the factory is doing,

```csharp
public class HttpClientFactory
{
    private readonly ConcurrentDictionary<string, HandlerEntry> _handlers = new();

    public HttpClient CreateClient(string name)
    {
        var entry = _handlers.GetOrAdd(name, _ => new HandlerEntry
        {
            Handler = new SocketsHttpHandler(),
            CreatedAt = DateTime.UtcNow
        });

        // Rotate handlers every 2 minutes
        if (DateTime.UtcNow - entry.CreatedAt > TimeSpan.FromMinutes(2))
        {
            var newEntry = new HandlerEntry
            {
                Handler = new SocketsHttpHandler(),
                CreatedAt = DateTime.UtcNow
            };
            _handlers[name] = newEntry;
            
            // Old handler gets disposed after active requests finish
            // (this is simplified. tge real implementation is more complex)
        }

        return new HttpClient(entry.Handler, disposeHandler: false);
    }

    private class HandlerEntry
    {
        public HttpMessageHandler Handler { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
```

The [real one](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Http/src/DefaultHttpClientFactory.cs), is of course more sophisticated, but that gives a rough idea of what's happening.

The key feature is the `disposeHandler: false`. When you dispose the HttpClient, the handler isn't disposed. The factory owns the handler life cycle, not the client.

### Why 2 Minutes?

A balance between DNS update latency and connection reuse efficiency. If we rotate handlers too frequently (like every 10 seconds), we're constantly creating and disposing objects, which defeats the purpose of connection pooling. If we rotate too slow, we're slow to pickup DNS changes.

That said, it's configurable for a reason. Go nuts.

### Basic Setup With The Factory

First, we register the HttpClientFactory in the DI container, like so

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
var app = builder.Build();
```

That registers everything, the ` IHttpClientFactory`, `IHttpMessageHandlerFactory`, and all the internal plumbing.

Then inject where it's needed

```csharp
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<string> GetDataAsync(string url)
    {
        var client = _httpClientFactory.CreateClient();
        return await client.GetStringAsync(url);
    }
}
```

This is already miles better than the anti-patterns. The factory manages the handler lifecycle, so you get socket reuse and DNS rotation. You can create as many `HttpClient` instances as you want without worrying about socket exhaustion or stale DNS.

But there's a more ergonomic way to do this if you're calling multiple APIs with different configurations.

### Named Clients

If you're calling different APIs with different configurations (base URLs, headers, timeouts), you can register named clients. This keeps configuration centralized and avoids repeating yourself all over the place

```csharp
builder.Services.AddHttpClient("GitHub", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("Internal", client =>
{
    client.BaseAddress = new Uri("https://internal.mycompany.com/api/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient("SlowExternal", client =>
{
    client.BaseAddress = new Uri("https://slow-api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(120); // Long timeout for slow APIs
});
```

Then we can just retrieve them by name when we need them

```csharp
public class MyService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MyService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Repository> GetGitHubRepoAsync(string owner, string repo)
    {
        var client = _httpClientFactory.CreateClient("GitHub");
        var json = await client.GetStringAsync($"repos/{owner}/{repo}");
        return JsonSerializer.Deserialize<Repository>(json);
    }

    public async Task<string> GetInternalDataAsync(string endpoint)
    {
        var client = _httpClientFactory.CreateClient("Internal");
        return await client.GetStringAsync(endpoint);
    }
}
```

Each named client gets its own handler pool. So "GitHub" and "Internal" don't share handlers, which makes sense, they're going to different base URLs, so they need separate connection pools anyway.

This approach is clean and maintainable. All your HTTP configuration lives in one place (your startup/DI registration), not scattered across service classes.

### Typed Clients

For anything beyond trivial use cases, typed clients are my preferred approach. Instead of dealing with `IHttpClientFactory` directly, you encapsulate all HTTP logic in a dedicated service class, and the factory injects a pre-configured `HttpClient` for you.

Here's an example of what it would look like,

```csharp
public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Repository> GetRepositoryAsync(string owner, string repo)
    {
        try
        {
            var response = await _httpClient.GetStringAsync($"repos/{owner}/{repo}");
            return JsonSerializer.Deserialize<Repository>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Repository {Owner}/{Repo} not found", owner, repo);
            return null;
        }
    }

    public async Task<IEnumerable<Repository>> GetUserRepositoriesAsync(string username)
    {
        var response = await _httpClient.GetStringAsync($"users/{username}/repos");
        return JsonSerializer.Deserialize<IEnumerable<Repository>>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<User> GetUserAsync(string username)
    {
        var response = await _httpClient.GetStringAsync($"users/{username}");
        return JsonSerializer.Deserialize<User>(response, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
}

public record Repository(string Name, string Description, int StargazersCount, string HtmlUrl);
public record User(string Login, string Name, int PublicRepos, int Followers);
```

Register it with a typed client, like so

```csharp
builder.Services.AddHttpClient<GitHubService>(client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "MyApp/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

Now you can inject `GitHubService` directly into your controllers or other services,

```csharp
[ApiController]
[Route("api/[controller]")]
public class GitHubController : ControllerBase
{
    private readonly GitHubService _githubService;

    public GitHubController(GitHubService githubService)
    {
        _githubService = githubService;
    }

    [HttpGet("repo/{owner}/{name}")]
    public async Task<IActionResult> GetRepository(string owner, string name)
    {
        var repo = await _githubService.GetRepositoryAsync(owner, name);
        if (repo == null)
        {
            return NotFound();
        }
        return Ok(repo);
    }

    [HttpGet("user/{username}")]
    public async Task<IActionResult> GetUser(string username)
    {
        var user = await _githubService.GetUserAsync(username);
        return Ok(user);
    }
}
```

This approach is clean, testable, and hides all the `HttpClient` plumbing behind a domain-specific interface. Your controller doesn't care that `GitHubService` uses HTTP under the hood,it just calls methods and gets results.

Makes testing easier too: you can mock `GitHubService` without worrying about HTTP details.

The factory will inject a configured `HttpClient` into `GitHubService` for each request. The `HttpClient` itself is transient (created for each service instance), but the underlying handler is pooled and managed by the factory. So you get the best of both worlds, clean service interfaces and efficient connection management.
