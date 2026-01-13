---
title: 📰 Serilog Tips and Tricks Structured Console Logging with Formatting
source: https://medium.com/@junaidulhaq723/serilog-tips-and-tricks-structured-console-logging-with-formatting-7f576075806a
author:
  - "[[Junaid Ul Haq]]"
published: 2025-08-26
created: 2026-01-07
description: "Serilog Tips and Tricks: Structured Console Logging with Formatting Serilog is one of the most powerful and flexible logging libraries in .NET. Developers can shape logs almost any way they want — …"
tags:
  - clippings
updated: 2026-01-07T22:54
---

# 📰 Serilog Tips and Tricks Structured Console Logging with Formatting

![](<_resources/📰 Serilog Tips and Tricks Structured Console Logging with Formatting/46885cda9ba5a58db16998839cae34fc_MD5.webp>)

Serilog is one of the most powerful and flexible logging libraries in.NET. Developers can shape logs almost any way they want — from plain text to structured JSON — and choose where those logs end up.

This post focuses on a simple but powerful tip: **structured console logging using custom formatting**.

## Why Console Logging Matters

When running in containers, applications usually rely on **console logs**, which are then collected by tools like **AWS CloudWatch** or other monitoring systems.

With Serilog, you don't need `Console.WriteLine()` anymore. Just use the **console sink**, and every log will automatically be written to the console.

Better yet, you can use **multiple sinks** at once:

- Locally, your logs can go to a **file sink**.
- In containers, the same app will log to the **console sink**.
- If you run your API as a console app locally, you'll still see logs in the console.

This flexibility makes Serilog incredibly useful across different environments.

## Why I Avoid JsonFormatter

Serilog ships with a built-in `JsonFormatter` for structured logging. It works, but in my experience, it adds **a lot of extra fields** that I don't need — which makes logs bigger and less efficient.

Instead, I prefer to use a **custom** `**outputTemplate**`. This way, I keep my logs structured in JSON format but with full control over which fields are included.

Here's an example:

```c
"WriteTo": [
  {
    "Name": "Console",
    "Args": {
      "outputTemplate": "{{\"@t\":\"{Timestamp:o}\",\"@l\":\"{Level}\",\"Message\":\"{Message:lj}\",\"Path\":\"{RequestPath}\",\"App\":\"{Application}\",\"trace_id\":\"{TraceId}\",\"user_id\":\"{user_id}\",\"RequestId\":\"{RequestId}\",\"ElapsedMs\":{ElapsedMs}}}{NewLine}{Exception}"
    }
  }
]
```

With this approach:

- I only log the fields I care about.
- I avoid bloated metadata.
- I keep my logs clean and efficient.

## What the Logs Look Like (Before vs. After)

Here's a quick comparison of what Serilog logs look like with the default `JsonFormatter` versus my custom `outputTemplate`.

## Default JsonFormatter Output

```c
{
  "Timestamp": "2025-08-23T10:15:42.234Z",
  "Level": "Information",
  "MessageTemplate": "Slow request detected: {Method} {RequestPath} took {ElapsedMs}ms with status {StatusCode}",
  "RenderedMessage": "Slow request detected: GET /api/orders took 123ms with status 200",
  "Properties": {
    "Method": "GET",
    "RequestPath": "/api/orders",
    "ElapsedMs": 123,
    "StatusCode": 200,
    "SourceContext": "Microsoft.AspNetCore.Hosting.Diagnostics",
    "ConnectionId": "0HMP9K3R1Q...",
    "TraceId": "abcd1234efgh5678",
    "SpanId": "ijkl9012mnop3456",
    "ParentId": "qrst7890uvwx1234",
    "MachineName": "MyLaptop",
    "ThreadId": 12
  }
}
```

This is **verbose** and includes a lot of extra metadata I don't always need.

## Custom OutputTemplate

```c
{
  "@t": "2025-08-23T10:15:42.2340000Z",
  "@l": "Information",
  "Message": "Slow request detected: GET /api/orders took 123ms with status 200",
  "Path": "/api/orders",
  "App": "OrderService",
  "trace_id": "abcd1234efgh5678",
  "user_id": "42",
  "RequestId": "rq-7890",
  "ElapsedMs": 123
}
```

This is **much cleaner** and focused only on the fields I care about:

- `ElapsedMs` appears as a proper JSON property (not just part of the message).
- No unnecessary internal fields.
- The log is shorter, easier to read, and more efficient to process in monitoring tools.

## Reducing Noise in Logs

By default,.NET logging can generate a lot of internal fields — for example, through `ActivityTrackingOptions` and `IncludeScopes`.

These might be useful in some cases, but if you don't need them, they **hurt performance** and make logs noisy.

Here's how you can turn them off:

```c
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  },
  //"ActivityTrackingOptions": "None",
  "IncludeScopes": false
}
```

When you actually need more context, you can enable enrichers selectively:

```c
"Enrich": [ "FromLogContext", "WithSpan" ]
```

This way, you get only the data that's relevant.

## Adding Custom Fields

Sometimes you need to log your own **custom properties** (e.g., request time, user IDs, or status codes) as proper JSON fields — not just as text inside the message.

Here's an example from my project:

```c
_logger.LogWarning(
    "Slow request detected: {@Method} {@RequestPath} took {@ElapsedMs}ms with status {@StatusCode}",
    context.Request.Method,
    context.Request.Path.Value,
    sw.Elapsed.TotalMilliseconds,
    context.Response.StatusCode);
```

In this case:

- `ElapsedMs` is **not** a built-in Serilog property.
- But since it's included in the log message as a placeholder, it becomes a **structured field**.
- Because my `outputTemplate` includes `ElapsedMs`, it shows up as a JSON property.

This means I can query `ElapsedMs` directly in CloudWatch or any log aggregator — super useful for observability and monitoring.

## Key Takeaways

- Use `outputTemplate` instead of the built-in `JsonFormatter` for **lean, efficient, and controlled structured logs**.
- Combine multiple sinks so your app logs correctly in **local, IIS, or container environments**.
- Disable unnecessary fields (`ActivityTrackingOptions`, `IncludeScopes`) to improve **performance and clarity**.
- Enrich logs with your own **custom fields** to capture meaningful data.
