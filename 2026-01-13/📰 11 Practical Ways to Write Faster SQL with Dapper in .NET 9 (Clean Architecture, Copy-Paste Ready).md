---
title: 📰 11 Practical Ways to Write Faster SQL with Dapper in .NET 9 (Clean Architecture, Copy-Paste Ready)
source: https://medium.com/@maged_/11-practical-ways-to-write-faster-sql-with-dapper-in-net-9-clean-architecture-copy-paste-ready-824126fd9b9c
author:
  - "[[Adam]]"
published: 2025-12-24
created: 2026-01-07
description: "11 Practical Ways to Write Faster SQL with Dapper in .NET 9 (Clean Architecture, Copy-Paste Ready) If you want the full source code, join our community: Here EF Core is fantastic for modeling. But …"
tags:
  - clippings
updated: 2026-01-07T22:37
uid: e15a64fc-d060-42dc-b628-2f145446995e
---

# 📰 11 Practical Ways to Write Faster SQL with Dapper in .NET 9 (Clean Architecture, Copy-Paste Ready)

![](<_resources/📰 11 Practical Ways to Write Faster SQL with Dapper in .NET 9 (Clean Architecture, Copy-Paste Ready)/bfa5369edf9db0784e7d3590fc3bc171_MD5.webp>)

## If you want the full source code, join our community: Here

EF Core is fantastic for modeling. But when the PM says "cut latency by 40%," you don't argue — you reach for **Dapper**. It's the lightweight micro-ORM that gives you **raw ADO.NET speed** with **nice mapping**. Think: EF for productivity, Dapper for hot paths.

This guide shows **where Dapper lives in Clean Architecture**, how to **shape queries for speed**, and the **11 patterns** that actually move the needle. Elder-brother tone, minimal drama, code you can ship today.

# Where Dapper Fits in Clean Architecture (zero coupling)

```c
src/
├─ YourApp.Domain/               // Entities, value objects, no Dapper refs
├─ YourApp.Application/          // Ports (interfaces), DTOs, CQRS queries/commands
├─ YourApp.Infrastructure/       // Dapper implementations, SQL, TypeHandlers
└─ YourApp.Web/                  // Minimal APIs / Controllers, DI
```

- **Application** defines interfaces (e.g., `IUserReadRepository`).
- **Infrastructure** implements them with Dapper (SQL lives here).
- **Web** wires DI.
- **Domain** stays blissfully ignorant.

# Install (keep it lean)

```c
dotnet add src/YourApp.Infrastructure package Dapper
dotnet add src/YourApp.Infrastructure package Microsoft.Data.SqlClient     # SQL Server
# or
dotnet add src/YourApp.Infrastructure package Npgsql                       # PostgreSQL
```

# A Minimal, Fast Dapper Setup

`Infrastructure/Data/DbConnectionFactory.cs`

```c
using System.Data;
using Microsoft.Data.SqlClient; // or Npgsql
using Microsoft.Extensions.Configuration;
namespace YourApp.Infrastructure.Data;
public interface IDbConnectionFactory
{
    IDbConnection Create();
}
public sealed class SqlConnectionFactory(IConfiguration config) : IDbConnectionFactory
{
    private readonly string _cs = config.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Missing connection string 'Default'.");
    public IDbConnection Create() => new SqlConnection(_cs); // pooling handled by provider
}
```

`Application/Users/IUserReadRepository.cs`

```c
namespace YourApp.Application.Users;
public interface IUserReadRepository
{
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<UserDto>> SearchAsync(string? emailLike, int page, int size);
}
public sealed record UserDto(Guid Id, string Email, string DisplayName, bool IsActive);
```

`Infrastructure/Users/UserReadRepository.cs`

```c
using System.Data;
using Dapper;
using YourApp.Application.Users;
using YourApp.Infrastructure.Data;
namespace YourApp.Infrastructure.Users;
public sealed class UserReadRepository(IDbConnectionFactory factory) : IUserReadRepository
{
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        const string sql = """
            SELECT TOP 1 Id, Email, DisplayName, IsActive
            FROM dbo.Users WITH (NOLOCK)
            WHERE Id = @Id
        """;
        using var conn = factory.Create();
        return await conn.QueryFirstOrDefaultAsync<UserDto>(sql, new { Id = id });
    }
    public async Task<IReadOnlyList<UserDto>> SearchAsync(string? emailLike, int page, int size)
    {
        const string sql = """
            SELECT Id, Email, DisplayName, IsActive
            FROM dbo.Users WITH (NOLOCK)
            WHERE (@Email IS NULL OR Email LIKE '%' + @Email + '%')
            ORDER BY Id
            OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY
        """;
        using var conn = factory.Create();
        var items = await conn.QueryAsync<UserDto>(
            sql,
            new { Email = emailLike, Offset = (page - 1) * size, Size = size });
        return items.AsList();
    }
}
```

`Web/Program.cs` DI

```c
using YourApp.Infrastructure.Data;
using YourApp.Application.Users;
using YourApp.Infrastructure.Users;
builder.Services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IUserReadRepository, UserReadRepository>();
```

> *Yes,* `***using var conn***` ***per method*** *is fine:.NET providers use* ***connection pooling*** *by default. Opening/closing is cheap.*

# The 11 Speed Patterns (that actually help)

# 1) Project Only What You Need (stop SELECT \*)

Less bytes = less time. Map directly to DTOs.

```c
SELECT Id, Email, DisplayName, IsActive FROM dbo.Users WHERE Id = @Id
```

Typical gains: **10–30%** vs wide `SELECT *` on mid-sized tables.

# 2) Use Parameters Always (plan reuse + no injection)

Dapper auto-parameterizes:

```c
await conn.QueryAsync<UserDto>("SELECT ... WHERE Email = @Email", new { Email = email });
```

This hits cached execution plans and keeps the DBA calm.

# 3) Buffered vs. Unbuffered

- `QueryAsync<T>` (default **buffered**) reads all rows into memory before returning.
- `Query<T>(..., buffered: false)` streams records.  
	Use **unbuffered** for large result sets to cut peak memory.

```c
await foreach (var u in conn.QueryUnbufferedAsync<UserDto>(sql, args)) { /* stream */ }
// helper
public static class DapperStream
{
    public static async IAsyncEnumerable<T> QueryUnbufferedAsync<T>(
        this IDbConnection conn, string sql, object? args = null)
    {
        foreach (var row in await conn.QueryAsync<T>(sql, args, buffered: false))
            yield return row;
    }
}
```

# 4) QueryMultiple for Round-Trip Reduction

Bundle related reads to cut network latency.

```c
const string sql = """
  SELECT * FROM dbo.Users WHERE Id = @Id;
  SELECT * FROM dbo.Orders WHERE UserId = @Id ORDER BY CreatedAt DESC;
""";
using var grid = await conn.QueryMultipleAsync(sql, new { Id = userId });
var user   = await grid.ReadFirstOrDefaultAsync<UserDto>();
var orders = (await grid.ReadAsync<OrderDto>()).AsList();
```

On high-latency networks, combining 2–3 queries often saves **20–50 ms**.

# 5) Multi-Mapping for Joins (without N+1)

Join once, map twice.

```c
const string sql = """
SELECT u.Id, u.Email, o.Id, o.Total, o.CreatedAt
FROM dbo.Users u
LEFT JOIN dbo.Orders o ON o.UserId = u.Id
WHERE u.Id = @Id
ORDER BY o.CreatedAt DESC
""";
var lookup = new Dictionary<Guid, UserWithOrders>();
await conn.QueryAsync<UserWithOrders, OrderDto, UserWithOrders>(
    sql,
    (u, o) =>
    {
        if (!lookup.TryGetValue(u.Id, out var agg))
            lookup[u.Id] = agg = new(u.Id, u.Email, new());
        if (o is not null) agg.Orders.Add(o);
        return agg;
    },
    new { Id = userId },
    splitOn: "Id" // first column of the second object
);
var result = lookup.Values.SingleOrDefault();
public sealed record UserWithOrders(Guid Id, string Email, List<OrderDto> Orders);
```

Avoid the classic **N+1** (100 orders → 101 queries). One query, one trip.

# 6) Proper Index-First Thinking

Fast Dapper can't fix slow SQL. Ensure:

- Predicates use **indexed columns**.
- Use **covering indexes** for read-heavy endpoints (include select list).
- Keep an eye on **seek vs scan** in the plan.  
	Rule of thumb: add one focused index per latency-critical endpoint; it's often a **30–90%** drop in read times.

# 7) Batched Writes > Loops

Replace 100 round-trips with one.

```c
// SQL Server TVP example
CREATE TYPE dbo.IdAmount AS TABLE (Id UNIQUEIDENTIFIER, Amount DECIMAL(18,2));
const string sql = "UPDATE dbo.Orders SET Amount = t.Amount FROM dbo.Orders o JOIN @Data t ON t.Id = o.Id";
var tvp = new DataTable();
tvp.Columns.Add("Id", typeof(Guid));
tvp.Columns.Add("Amount", typeof(decimal));
foreach (var x in updates) tvp.Rows.Add(x.Id, x.Amount);
var p = new DynamicParameters();
p.Add("@Data", tvp.AsTableValuedParameter("dbo.IdAmount"));
await conn.ExecuteAsync(sql, p);
```

For Postgres, use `**COPY**`, **unnest arrays**, or `**NpgsqlBinaryImporter**`.

# 8) Transactions Around Sets

One transaction per batch keeps it safe and faster.

```c
using var tx = conn.BeginTransaction();
await conn.ExecuteAsync(sql1, args1, tx);
await conn.ExecuteAsync(sql2, args2, tx);
tx.Commit();
```

Fewer log flushes, fewer locks held over time.

# 9) Command Timeout & "Fast Fail"

Default timeouts are generous (30s). Tighten for hot endpoints:

```c
await conn.QueryAsync<UserDto>(sql, args, commandTimeout: 3); // seconds
```

If it times out, you *want* to know before your users do.

# 10) TypeHandlers for Odd Types

Map custom types cleanly once.

```c
using Dapper;
using System.Data;
public sealed class UtcDateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
        => parameter.Value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public override DateTime Parse(object value)
        => DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
}
// At startup
SqlMapper.AddTypeHandler(new UtcDateTimeHandler());
```

No more drifting `DateTimeKind`.

# 11) Measure Like an Adult (Stopwatch + p95)

Don't guess. Measure.

```c
var sw = System.Diagnostics.Stopwatch.StartNew();
var rows = await repo.SearchAsync("alex", 1, 50);
sw.Stop();
logger.LogInformation("Search took {ms} ms for {count} rows", sw.ElapsedMilliseconds, rows.Count);
```

Track **p95** over a day. Typical teams see **2–5× faster** reads when moving simple queries from EF to Dapper; complex projections vary.

# A Focused Example: High-Throughput "Get Orders" Query

`Application/Orders/IOrderReadRepository.cs`

```c
public interface IOrderReadRepository
{
    Task<IReadOnlyList<OrderSummary>> GetRecentForUserAsync(Guid userId, int take);
}
public sealed record OrderSummary(Guid Id, decimal Total, DateTime CreatedAt, string Status);
```

`Infrastructure/Orders/OrderReadRepository.cs`

```c
using Dapper;
using YourApp.Application.Orders;
using YourApp.Infrastructure.Data;
public sealed class OrderReadRepository(IDbConnectionFactory factory) : IOrderReadRepository
{
    private const string Sql = """
        SELECT TOP (@Take)
               o.Id, o.Total, o.CreatedAtUtc AS CreatedAt, o.Status
        FROM dbo.Orders o WITH (NOLOCK)
        WHERE o.UserId = @UserId
        ORDER BY o.CreatedAtUtc DESC
    """;
    public async Task<IReadOnlyList<OrderSummary>> GetRecentForUserAsync(Guid userId, int take)
    {
        using var conn = factory.Create();
        var rows = await conn.QueryAsync<OrderSummary>(Sql, new { UserId = userId, Take = take }, commandTimeout: 2);
        return rows.AsList();
    }
}
```

`Web/Program.cs` endpoint

```c
api.MapGet("/users/{id:guid}/orders/recent", async (Guid id, int take, IOrderReadRepository repo) =>
{
    take = Math.Clamp(take, 1, 100);
    var items = await repo.GetRecentForUserAsync(id, take);
    return Results.Ok(items);
});
```

# EF Core + Dapper: The Hybrid That Wins

- Keep **EF Core** for writes, aggregates, and business logic heavy flows.
- Use **Dapper** for read-most, latency-sensitive endpoints and reporting.
- This combo often reduces API p95 by **25–60%** with minimal churn.

# Safety & Pitfalls (read this twice)

1. **NOLOCK** can return dirty/duplicate rows — fine for dashboards, not for money.
2. **String concatenation** in SQL = injection. Always param.
3. **Alias your columns** to match DTOs; Dapper maps by name.
4. **Fix the N+1** — prefer joins, multi-mapping, or server-side `IN` filters.
5. **Timeouts**: set per query; add retries only for safe, idempotent reads.
6. **Connection leaks**: always `using var conn`.
7. **Large result sets**: go unbuffered or paginate.

# Quick Benchmark Template (DIY)

```c
var warmup = await repo.GetRecentForUserAsync(userId, 50); // JIT + plan warm
var times = new List<long>(100);
for (int i = 0; i < 100; i++)
{
    var sw = Stopwatch.StartNew();
    _ = await repo.GetRecentForUserAsync(userId, 50);
    sw.Stop();
    times.Add(sw.ElapsedMilliseconds);
}
var p95 = times.OrderBy(x => x).Skip((int)(times.Count * 0.95)).First();
Console.WriteLine($"p95 = {p95} ms");
```

Use the same pattern to compare your EF query vs Dapper on **production-like data**.

# Copy-Paste Checklist

- `IDbConnectionFactory` with pooling (SqlClient/Npgsql).
- Interfaces in **Application**, Dapper impls in **Infrastructure**.
- Parameterized SQL; select only needed columns.
- Multi-mapping / QueryMultiple where it saves trips.
- Batching for writes, transactions for sets.
- Timeouts, logging, p95 tracking.
- Optional TypeHandlers (UTC, enums).

# Parting Shot

Dapper is the "manual transmission" of data access: more control, more speed, fewer surprises. Use it for the **hot 10%** of your endpoints and keep EF Core where it shines. Your users will feel the difference — and your graphs will look prettier.
