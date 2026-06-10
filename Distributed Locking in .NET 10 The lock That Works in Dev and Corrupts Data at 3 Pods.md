
# Distributed Locking in .NET 10 

> The lock That Works in Dev and Corrupts Data at 3 Pods
Your inventory count goes negative at 9pm on a Friday. Not by a lot. Minus three on one SKU, minus one on two others. Enough that the warehouse export fails its validation and someone gets paged.

You check the code that decrements inventory. It is wrapped in a `lock`. A clean, textbook `lock (_inventoryLock)` around the read-check-write. You wrote it carefully. You code-reviewed it. It is, by every rule you learned, thread-safe.

It is thread-safe. It is not pod-safe. And three weeks ago someone scaled the deployment to three replicas.

The `lock` keyword protects threads inside one process. The moment you run more than one instance, it protects nothing. This article covers three locking traps that pass every test in dev and corrupt data the moment you scale out, and what actually works instead.

## Why the lock keyword stops working

A `lock` statement works by acquiring a mutual-exclusion monitor on an object reference. That object lives on the managed heap. The heap belongs to one process.

When you run one instance of your app, there is one heap, one `_inventoryLock` object, one monitor. Every thread that wants the critical section queues on that single monitor. The guarantee holds.

When you run three pods, there are three processes, three heaps, three separate `_inventoryLock` objects that happen to share a variable name. Three monitors. A thread in pod A and a thread in pod B both acquire "the" lock at the same instant, because they are not the same lock. They never were. They just looked identical in the source.

That is the whole bug. Everything below is a variation of it.

## Trap 1: The lock that looks thread-safe

Here is the code that shipped:

```c
public sealed class InventoryService
{
    private static readonly object _inventoryLock = new();
    private readonly InventoryRepository _repo;

    public async Task<bool> TryReserveAsync(int sku, int quantity)
    {
        lock (_inventoryLock)
        {
            var current = _repo.GetStock(sku);
            if (current < quantity)
                return false;
            _repo.SetStock(sku, current - quantity);
            return true;
        }
    }
}
```

Read this in a code review and it looks correct. The check and the write are inside the lock. No other thread can read stock between your check and your decrement. Textbook.

It passes every unit test. It passes integration tests, because the test host is one process. It passes the load test someone ran locally, because that load test hammered one process. CI is green. The reviewer approves it. Nobody did anything wrong by the rules they were taught.

Then the deployment is scaled to `replicas: 3` for a traffic event, and the lock quietly becomes three locks. Pod A reads stock of 2 for a SKU. Pod B reads stock of 2 for the same SKU at the same moment, because Pod B's monitor has nothing to do with Pod A's. Both pass the `current < quantity` check. Both write. The SKU that had 2 units just sold 4. Inventory goes negative.

At 200 requests per second against a hot SKU, this is not a rare edge case you might hit. The overlap window is every moment two pods touch the same key, which at that traffic is constant. You will lose this race on a schedule.

The `lock` keyword did its job perfectly. Its job was never cross-process safety. It never claimed to be.

## Trap 2: The database-as-lock race

A team that discovers Trap 1 usually reaches for the database next. The database is shared across all pods, so a lock in the database should be shared too. The homemade version looks like this:

```c
public async Task<bool> TryReserveAsync(int sku, int quantity)
{
    var locks = await _db.QueryAsync(
        "SELECT IsLocked FROM ResourceLocks WHERE ResourceKey = @key",
        new { key = $"sku:{sku}" });

  if (locks.Single().IsLocked)
        return false;

    await _db.ExecuteAsync(
        "UPDATE ResourceLocks SET IsLocked = 1 WHERE ResourceKey = @key",
        new { key = $"sku:{sku}" });
    // ... do the reservation, then set IsLocked = 0
}
```

The lock now lives in a place all three pods can see. Problem solved.

It is not solved. This is Trap 1 again, moved one layer down. There is a window between the `SELECT` that reads `IsLocked` and the `UPDATE` that sets it. Pod A reads `IsLocked = 0`. Before Pod A runs its `UPDATE`, Pod B runs its `SELECT` and also reads `IsLocked = 0`. Both pods believe they hold the lock. Both proceed.

This is the check-then-act race, and the gap between the two statements is small, maybe two to ten milliseconds. People look at two milliseconds and assume it is too narrow to matter. At any real concurrency it is not narrow at all. It is hit constantly. A two-millisecond window under steady multi-pod traffic is a race you lose continuously, not occasionally.

Homemade database locks are where good intentions go to cause incidents. If you are writing `SELECT IsLocked`, stop. The fix is not a better `SELECT`. The fix is making acquisition a single atomic operation, so there is no window between checking and taking.

## Trap 3: The lock with no expiry

The third trap shows up after a team adopts a real distributed lock. They bring in Redis and use it the obvious way:

```c
// Acquire
await _redis.StringSetAsync($"lock:sku:{sku}", "held", when: When.NotExists);

// ... do the work ...
// Release
await _redis.KeyDeleteAsync($"lock:sku:{sku}");
```

`When.NotExists` maps to Redis `SET NX`. It is atomic. Only one pod can set the key when it does not exist, so only one pod gets the lock. Trap 1 and Trap 2 are both genuinely fixed. The acquire is a single atomic operation with no check-then-act window.

Then a pod acquires the lock, starts the work, and dies. Out-of-memory kill. A node drain. A bad deploy. Kubernetes does what it is supposed to do and reschedules the pod within seconds. But the `KeyDeleteAsync` that releases the lock never ran, because the process that was going to run it no longer exists.

The Redis key is still there. It says the lock is held. It will say that forever, because the only pod that knew to delete it is gone. Every other pod now calls `SET NX`, sees the key exists, and backs off. Nothing is slow. Everything is simply blocked, indefinitely, behind a lock whose owner no longer exists. One OOM-killed pod converted a fifty-millisecond critical section into an unbounded outage.

A distributed lock with no expiry is a deadlock waiting for a crash. And in production, something always crashes.

## The production-safe setup

A correct distributed lock needs three properties. Acquisition must be atomic. The lock must carry a time-to-live so a dead holder cannot block forever. And release must verify ownership, so a pod cannot delete a lock that has already expired and been taken by someone else.

Redis gives you all three if you use it correctly.

```c
public sealed class RedisLock
{
    private readonly IDatabase _redis;

    public RedisLock(IConnectionMultiplexer mux) => _redis = mux.GetDatabase();
    
    public async Task<string?> AcquireAsync(string key, TimeSpan ttl)
    {
        var token = Guid.NewGuid().ToString("N");
        // SET key token NX PX <ttl> - atomic acquire WITH expiry
        bool acquired = await _redis.StringSetAsync(
            $"lock:{key}",
            token,
            expiry: ttl,
            when: When.NotExists);
        return acquired ? token : null;
    }
    private const string ReleaseScript = """
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end
        """;
    public async Task ReleaseAsync(string key, string token)
    {
        // Compare-and-delete: only delete the lock if WE still hold it
        await _redis.ScriptEvaluateAsync(
            ReleaseScript,
            new RedisKey[] { $"lock:{key}" },
            new RedisValue[] { token });
    }
}
```

Two things in this code matter more than the rest.

The acquire sets the value and the expiry in one operation. If the pod dies, the TTL expires the lock on its own. No process needs to be alive to clean up.

The release runs a Lua script that checks the token before deleting. Without that check, here is the failure: Pod A acquires the lock with a 30-second TTL, runs slow, and the TTL expires while Pod A is still working. Pod B acquires the now-free lock. Pod A finishes and calls release, which blindly deletes the key, deleting Pod B's lock. Now Pod C can acquire it too. The compare-and-delete makes release safe by deleting only if the stored token is still yours.

If you do not have Redis, SQL Server gives you `sp_getapplock`, an application-level lock scoped to a session or transaction, which is a reasonable alternative when your database is the only shared infrastructure you have.

One honest caveat, because it is the part most articles skip. A lock with a TTL can expire while you are still working. If your critical section can run longer than the TTL, two pods can legitimately both believe they hold the lock during the overlap. The lock reduces the probability of collision. It does not make the work itself safe. If correctness depends on it, the work inside the lock must also be idempotent, so that doing it twice produces the same result as doing it once. A distributed lock is a strong optimization. It is not a substitute for idempotency.

## How to check if you have this problem right now

There is one question that tells you whether you are exposed.

Does your service run more than one replica, instance, or pod?

If yes, search the codebase for `lock (`, `SemaphoreSlim`, and `Interlocked`. For each hit, ask one thing: does the code inside touch state that is shared outside this process, a database row, a cache entry, an external balance, a file, a third-party resource? Every hit where the answer is yes is a candidate for the exact bug in this article. It passes your tests. It is corrupting data anyway.

## The checklist

- `lock`, `SemaphoreSlim`, and `Interlocked` protect one process only. Never use them to guard state shared across instances.
- A distributed lock acquire must be atomic and carry a TTL. In Redis that is `SET key token NX PX`.
- Release must be compare-and-delete with a unique token, never a blind `DEL`, or you will delete a lock someone else now holds.
- If the critical section can outlive the TTL, the work inside it must be idempotent. The lock is an optimization, not a guarantee.
- Whenever replica count goes above one, grep for in-process locks around shared external state and treat every hit as a bug until proven otherwise.

The `lock` keyword is a promise. It promises that only one thread runs this code at a time. Read the promise carefully. It says thread. It never said pod. Production is always more than one pod.
