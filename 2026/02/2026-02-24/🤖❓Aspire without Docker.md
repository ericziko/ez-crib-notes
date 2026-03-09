---
uid: ea397c9c-2f35-4eb1-984a-9a4affd0e415
title: "🤖❓Aspire without Docker "
aliases:
  - "🤖❓Aspire without Docker "
linter-yaml-title-alias: "🤖❓Aspire without Docker "
created: 2026-02-24T20:21:13
modified: 2026-02-24T20:27:16
---

# 🤖❓Aspire without Docker 

I'm using Aspire, and I'm not allowed to use Docker or containers in my local dev environment at work. Is it still possible to use Aspire as my app orchestrator and launch multiple container-like processes and coordinate them without Docker?

## 🤖💡 Yep — Aspire can still be your dev-time "orchestrator" without Docker, as long as you understand what you're swapping out
- Your own services (your .NET projects) already run as normal host processes when launched by the Aspire AppHost.
- Non-.NET things (Node, Python, CLIs, etc.) can also be launched as normal host processes via AddExecutable.  ￼
- The main thing you lose without Docker is Aspire's ability to auto-provision containerized dependencies (Redis/Postgres/etc.) from images. For those, you point Aspire at an existing instance instead (local install, VM, shared dev DB, cloud dev resource, etc.).

What you can orchestrate without Docker

### Multiple "container-like" processes (but they're just processes)

Aspire can start and manage external processes with AddExecutable (and still wire up env vars / dependencies).  ￼

Conceptually:

```cs
var builder = DistributedApplication.CreateBuilder(args);
var api = builder.AddProject<Projects.ApiService>("api");

var frontend = builder.AddExecutable("frontend", "npm", workingDirectory: "../frontend", "run", "dev")
                      .WithEnvironment("API_BASE_URL", api.GetEndpoint("https")); // example pattern

builder.Build().Run();
```

So you still get one command to launch the whole mini-universe, plus Aspire's coordination features (references, waits, env injection, dashboard, etc.).

## Coordinating dependencies without starting them

If you can't run Redis/Postgres in containers, you can model them as connection strings / external parameters and have Aspire pass them to the right services.

Aspire explicitly supports using AddConnectionString and reading the value from the AppHost config (ConnectionStrings) when running locally.  ￼

Example from the docs:

```cs
var redis = builder.AddConnectionString("redis");
builder.AddProject<Projects.WebApplication>("api")
       .WithReference(redis)
       .WaitFor(redis);
```

…and you provide the actual value in the AppHost appsettings.json under ConnectionStrings.  ￼

That means your "Redis" can be:
- a locally installed Redis service,
- a VM on your network,
- a managed cloud Redis instance,
- anything reachable by a connection string.

The practical tradeoffs (aka "what breaks without Docker")
- If you use integrations that spin up containers automatically (common for dev DBs/caches), those specific resources won't be able to start unless you replace them with:
- AddConnectionString / parameters (external dependency), or
- AddExecutable (you start a local binary/service yourself), or
- a real shared dev environment you can reach.
- You don't get container isolation (filesystem, networks, fixed image versions) — you're coordinating host processes, so you'll care more about port conflicts, installed versions, and "works on my machine" drift.

## A good "no-Docker Aspire" pattern
- Use **Aspire AppHost** to launch all your services + any extra local tools via **AddExecutable**. 
- Treat infra dependencies as **external resources** via AddConnectionString / parameters (read from config/secrets). 
- Keep the same *logical* wiring (WithReference, WaitFor) so your app composition stays consistent even if the backing runtime differs.

So: **Aspire absolutely still works as the conductor** — you're just telling it "don't summon Docker dragons; my dependencies already exist somewhere, and some helpers are plain old processes."
