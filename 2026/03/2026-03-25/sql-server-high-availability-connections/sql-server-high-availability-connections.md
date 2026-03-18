---
uid: a1b2c3d4-e5f6-7890-abcd-ef1234567890
title: SQL Server Always On Availability Groups — C# Connection Guide
created: 2026-03-25T00:00:00Z
modified: 2026-03-25T00:00:00Z
tags:
  - sql-server
  - always-on
  - availability-groups
  - csharp
  - connection-strings
  - high-availability
---

# 🤖❓ SQL Server Always On Availability Groups — C# Connection Guide

## 🤖💡 Overview

Switching from a direct SQL Server connection to an **Always On Availability Group (AG)** listener
is mostly a connection string change — but there are critical gotchas that will bite you if you
miss them.

---

## 📡 Connection String: Direct vs Availability Group Listener

### Direct connection (before)
```
Server=MY-SQL-SERVER\INSTANCE;Database=MyDb;Integrated Security=true;
```

### Availability Group Listener (after)
```
Server=MY-AG-LISTENER,1433;Database=MyDb;Integrated Security=true;
MultiSubnetFailover=True;
```

The listener is a **virtual network name (VNN)** or **distributed network name (DNN)** — it floats
between nodes. You connect to the listener, not to a specific server.

---

## 🔑 Critical Connection String Parameters

### `MultiSubnetFailover=True` ⚠️ MUST HAVE

```
MultiSubnetFailover=True
```

**What it does:** Tells the driver to attempt connections to **all IP addresses** for the listener
in parallel rather than sequentially. In a multi-subnet AG (e.g., primary in Sydney, DR in
Melbourne), without this flag, failover can take **20–40 seconds** instead of **2–5 seconds**.

**Rule of thumb:** Always set this when connecting to an AG listener. It is safe to set even on
single-subnet AGs — it doesn't hurt anything.

```csharp
// In appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=MY-AG-LISTENER,1433;Database=MyDb;Integrated Security=true;MultiSubnetFailover=True;"
}
```

---

### `ApplicationIntent=ReadOnly` — For Read Replicas

```
ApplicationIntent=ReadOnly
```

Always On AGs support **readable secondary replicas**. If you set this, the listener routes your
connection to a secondary (read-only) replica automatically.

```csharp
// Read-write connection (primary)
"ReadWriteConnection": "Server=MY-AG-LISTENER,1433;Database=MyDb;MultiSubnetFailover=True;Integrated Security=true;"

// Read-only connection (secondary replica)
"ReadOnlyConnection": "Server=MY-AG-LISTENER,1433;Database=MyDb;MultiSubnetFailover=True;ApplicationIntent=ReadOnly;Integrated Security=true;"
```

> ⚠️ **Gotcha:** If no readable secondary is configured, `ReadOnly` connections fall back to the
> primary. This is safe but means you're not actually offloading reads until a secondary is
> configured and set as readable in the AG configuration.

---

## 🪤 Gotchas & Common Pitfalls

### 1. 🪤 Hard-coded server names in code

Search your codebase for any hard-coded server names. These will bypass the listener entirely and
connect directly to a node — meaning failover won't work for those connections.

```csharp
// BAD — bypasses the listener
var conn = new SqlConnection("Server=MY-PRIMARY-NODE;...");

// GOOD — always use the listener
var conn = new SqlConnection(config.GetConnectionString("DefaultConnection"));
```

---

### 2. 🪤 `ConnectRetryCount` and `ConnectRetryInterval`

After a failover, the primary is temporarily unavailable. Your client will get transient errors.
Configure retry settings:

```
Server=MY-AG-LISTENER,1433;Database=MyDb;MultiSubnetFailover=True;
ConnectRetryCount=3;ConnectRetryInterval=10;Integrated Security=true;
```

Better still, combine with **Polly** for retry logic on transient SQL errors:

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null  // uses EF Core's default transient error list
        )
    )
);
```

---

### 3. 🪤 Connection Timeout — increase it for failover scenarios

Default is 15 seconds, which may be too short during failover. Consider increasing:

```
Connect Timeout=30;
```

---

### 4. 🪤 `Encrypt` and `TrustServerCertificate` — changed defaults in newer drivers

In **Microsoft.Data.SqlClient 4.0+**, `Encrypt=True` is the **default** (was `False` in older
System.Data.SqlClient). If your AG nodes don't have trusted TLS certificates, connections will
fail.

```
// If you don't have proper certs (dev/test only — not for prod):
Encrypt=False;

// OR if using self-signed certs (dev/test only):
TrustServerCertificate=True;
```

> ⚠️ Always use proper certificates in production.

---

### 5. 🪤 Named Instances vs Default Instance on the Listener

AG Listeners **do not support named instances**. The listener always uses a port (default `1433`).
If your old connection used `SERVER\INSTANCENAME`, you cannot replicate that with the listener —
use `SERVER,PORT` instead.

```
// OLD — named instance (cannot use with AG listener)
Server=MY-SERVER\SQLEXPRESS;

// NEW — port-based (correct for AG listener)
Server=MY-AG-LISTENER,1433;
```

---

### 6. 🪤 Read-Only Routing requires correct AG configuration

`ApplicationIntent=ReadOnly` only routes to a secondary if:

1. The secondary replica is configured as **readable** in the AG.
2. A **read-only routing list** is configured on the primary replica.
3. The listener is used (not a direct node connection).

Check with your DBA that these are set up before assuming reads are going to secondaries.

---

### 7. 🪤 Distributed Transaction Coordinator (MSDTC) and AGs

`TransactionScope` with `Enlist=True` (the default) uses MSDTC. This does **not work reliably**
with AGs across subnets. If you use distributed transactions, review this with your DBA.

```
// Disable auto-enlistment if you don't need distributed transactions
Enlist=False;
```

---

## 🏗️ EF Core Configuration Pattern

```csharp
// Program.cs / Startup.cs
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ReadWrite"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        }
    )
);

// Optional: separate read-only context
services.AddDbContext<ReadOnlyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ReadOnly"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)
    )
    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
);
```

```json
// appsettings.json
{
  "ConnectionStrings": {
    "ReadWrite":  "Server=MY-AG-LISTENER,1433;Database=MyDb;Integrated Security=true;MultiSubnetFailover=True;Connect Timeout=30;",
    "ReadOnly":   "Server=MY-AG-LISTENER,1433;Database=MyDb;Integrated Security=true;MultiSubnetFailover=True;ApplicationIntent=ReadOnly;Connect Timeout=30;"
  }
}
```

---

## ✅ Migration Checklist

| Item | Done |
|------|------|
| Replace server name with AG listener VNN/DNN in all connection strings | ☐ |
| Add `MultiSubnetFailover=True` | ☐ |
| Remove named instance (`\INSTANCENAME`), use port instead | ☐ |
| Increase `Connect Timeout` to 30s | ☐ |
| Add `EnableRetryOnFailure` in EF Core (or Polly for raw SqlConnection) | ☐ |
| Audit code for hard-coded server names | ☐ |
| Check `Encrypt`/`TrustServerCertificate` settings against driver version | ☐ |
| Decide on `ReadOnly` routing and set up separate connection string if needed | ☐ |
| Confirm AG read-only routing is configured (DBA task) | ☐ |
| Test failover manually (pull the primary node) and measure reconnect time | ☐ |

---

## 🔍 Quick Reference: Key Connection String Keywords

| Keyword | Purpose | Recommended Value |
|---------|---------|-------------------|
| `MultiSubnetFailover` | Parallel IP probing on failover | `True` |
| `ApplicationIntent` | Route to primary or readable secondary | `ReadWrite` or `ReadOnly` |
| `Connect Timeout` | How long to wait for initial connection | `30` |
| `ConnectRetryCount` | Auto-retry attempts after drop | `3` |
| `ConnectRetryInterval` | Seconds between retries | `10` |
| `Encrypt` | TLS encryption | `True` (prod) |
| `Enlist` | Auto-enlist in ambient transactions | `False` if not using MSDTC |
