---
uid: d327b41a-c5c6-4a97-b3f8-e8b4ea726103
---
--
uid: 06577f1f-4a02-41d6-8ad5-fe2c4df57e58
title: 🤖❓ **SQL Server High CPU Usage During Maintenance**
created: 2026-03-13T13:46:40
modified: 2026-03-13T13:49:49
---

# 🤖❓ **SQL Server High CPU Usage During Maintenance**

## **Overall Summary**

Reported high CPU usage issues on a SQL Server running inside an OpenShift container during a Sunday night maintenance window when the server goes offline as part of a high availability group.


## Table of Contents 
- [**Key Points**](<#key-points>)
- [**Open Questions**](<#open-questions>)
- [**1. Form a working theory first**](<#1-form-a-working-theory-first>)
- [**2. Start with the app-side symptoms**](<#2-start-with-the-app-side-symptoms>)
	- [**Metrics to line up on a timeline**](<#metrics-to-line-up-on-a-timeline>)
	- [**What to correlate**](<#what-to-correlate>)
- [**3. Hunt these exception types in logs**](<#3-hunt-these-exception-types-in-logs>)
- [**4. Audit all DB entry points in the code**](<#4-audit-all-db-entry-points-in-the-code>)
	- [**A. Disposal is guaranteed**](<#a-disposal-is-guaranteed>)
- [**5. Find retry logic and interrogate it like a detective**](<#5-find-retry-logic-and-interrogate-it-like-a-detective>)
- [**6. Inspect async code for multiplication effects**](<#6-inspect-async-code-for-multiplication-effects>)
- [**7. Check for sync-over-async**](<#7-check-for-sync-over-async>)
- [**8. Review OpenShift probes very carefully**](<#8-review-openshift-probes-very-carefully>)
- [**9. Inspect BackgroundService and hosted workers**](<#9-inspect-backgroundservice-and-hosted-workers>)
- [**10. Check connection pool behavior**](<#10-check-connection-pool-behavior>)
- [**11. EF Core / Dapper specific checks**](<#11-ef-core--dapper-specific-checks>)
	- [**If using EF Core**](<#if-using-ef-core>)
	- [**If using Dapper / raw ADO.NET**](<#if-using-dapper--raw-adonet>)
- [**12. Look at logging volume itself**](<#12-look-at-logging-volume-itself>)
- [**13. OpenShift-specific things to inspect**](<#13-openshift-specific-things-to-inspect>)
- [**14. A concrete code smell checklist**](<#14-a-concrete-code-smell-checklist>)
- [**15. What to capture in one outage window**](<#15-what-to-capture-in-one-outage-window>)
	- [**From logs**](<#from-logs>)
	- [**From metrics**](<#from-metrics>)
	- [**From platform**](<#from-platform>)
- [**16. What I'd fix first even before perfect proof**](<#16-what-id-fix-first-even-before-perfect-proof>)
- [**17. Tiny example of a healthier failure path**](<#17-tiny-example-of-a-healthier-failure-path>)
- [**18. My strongest suspicion ranking**](<#18-my-strongest-suspicion-ranking>)
- [**19. Practical next step**](<#19-practical-next-step>)

### **Key Points**

- The SQL Server is part of a high availability group running inside OpenShift
- High CPU usage occurs during the maintenance window when the SQL Server goes offline

### **Open Questions**

- What could be causing the high CPU usage during the maintenance window?

Absolutely. Here's a focused **.NET + OpenShift + SQL Server AG outage investigation checklist** aimed at your exact gremlin nest: **SQL goes offline during maintenance, app CPU spikes, and you suspect bad connection cleanup plus async failure storms**.

### **1. Form a working theory first**

The leading theory is:

**When SQL becomes unavailable, your app's failure path is burning CPU** rather than real work doing so.

That usually means one or more of these:

- retry loops are too aggressive
- async calls are multiplying during failure
- exceptions are being thrown and logged at absurd volume
- health checks or background workers keep hammering SQL
- connection pool is exhausted or thrashing
- blocked threads and thread-pool growth create secondary chaos

So the job is not "find high CPU."

The job is "find what code path becomes a panic squirrel when SQL disappears."

---

### **2. Start with the app-side symptoms**

During the maintenance window, collect these from the pod/container:

#### **Metrics to line up on a timeline**

- container CPU
- container memory
- pod restarts
- request rate
- error rate
- thread count if exposed
- connection-related exception count
- health check failures
- SQL dependency duration/failure rate
- queue/backlog depth if you have message consumers

#### **What to correlate**

You want a timeline like:

- **T0** SQL node/failover/maintenance starts
- **T0 + seconds** connection/login/timeout errors begin
- **T0 + seconds/minutes** retries spike
- **T0 + seconds/minutes** CPU spikes
- **T0 + seconds/minutes** health checks fail / readiness changes / restarts occur

If those line up, the culprit is almost certainly your failure-handling path.

---

### **3. Hunt these exception types in logs**

Search the outage window for bursts of:

- SqlException
- timeout expired
- transport-level error
- network-related or instance-specific error
- connection pool timeout
- OperationCanceledException
- TaskCanceledException
- broken circuit / retry exhausted if using Polly
- login failed / handshake / TCP provider errors
- AG failover related disconnects

The important thing is not just *which* exceptions happen — it's:

- how many per minute
- from which code path
- whether the same message repeats like a machine gun

If you see the same DB failure logged tens of thousands of times, there's your goblin.

---

### **4. Audit all DB entry points in the code**

Do a code search for:

- new SqlConnection(
- Open( / OpenAsync(
- ExecuteReader
- ExecuteNonQuery
- ExecuteScalar
- BeginTransaction
- repository methods
- Dapper / EF Core command execution
- homegrown DAL wrappers

For each path, verify:

#### **A. Disposal is guaranteed**

You want:

```
await using var conn = new SqlConnection(cs);
await conn.OpenAsync(ct);
```

and similarly for:

- commands
- readers
- transactions

Red flags:

- connection created outside using / await using
- connection returned from helper methods without clear ownership
- reader left open
- transaction not disposed on exception
- connection stored on a service field and reused across calls
- singleton service holding connection state

That last one is particularly cursed.

---

### **5. Find retry logic and interrogate it like a detective**

Search for:

- Polly
- WaitAndRetry
- RetryAsync
- while
- for (;;)
- Task.Delay
- custom "retry" helpers
- transient fault handling wrappers

What good looks like:

- finite retry count
- exponential backoff
- jitter
- cancellation support
- retries only for genuinely transient failures
- circuit breaker or some stop-hammering mechanism

What bad looks like:

```
while (true)
{
    try
    {
        await CallDatabase();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Retrying");
    }
}
```

That is not resilience. That is a CPU trebuchet.

Also look for retries layered on retries:

- SQL client retry
- Polly retry
- repository retry
- background service retry
- caller retry

Stack enough of those and one outage becomes a carnival.

---

### **6. Inspect async code for multiplication effects**

Search for these patterns:

- Task.Run
- _ = SomeAsyncMethod()
- fire-and-forget work
- Parallel.ForEachAsync
- unbounded Task.WhenAll
- channels/queues with many consumers
- timers that can overlap
- background loops without locks or guards

You're looking for this shape of bug:

1. SQL call fails
2. retry logic kicks in
3. multiple callers do the same at once
4. each one logs exceptions
5. background jobs also keep trying
6. health checks pile on
7. thread pool expands and CPU rises

Particularly nasty:

- a timer firing every few seconds even when prior execution hasn't finished
- scheduled refresh jobs stacking up during outage
- startup warmers or cache loaders that retry forever
- consumers that immediately nack/requeue and retry

---

### **7. Check for sync-over-async**

Search for:

- .Result
- .Wait()
- GetAwaiter().GetResult()

These are classic thread-pool misery generators when dependencies fail.

A common failure pattern:

- async DB operation blocks synchronously
- threads get stuck
- thread pool creates more threads
- more work tries to execute
- CPU rises from scheduling/context switching/exception churn

This does not always present as "everything blocked." Sometimes it presents as "why is CPU high while nothing useful is happening?"

---

### **8. Review OpenShift probes very carefully**

This is a huge one.

Inspect:

- liveness probe
- readiness probe
- startup probe

Questions:

- Do they hit the database?
- How often?
- Are they implemented in-process?
- Do they run expensive checks?
- Does failure cause restart loops?

Best practice:

- **readiness** can reflect dependency health more carefully
- **liveness** should usually not depend on transient DB reachability unless the process is truly unrecoverable
- probes should fail fast and not create load storms

A bad pattern is:

- probe endpoint checks SQL every few seconds
- SQL goes offline
- every pod hammers SQL
- probes fail
- pods restart
- startup path also hammers SQL
- now you have an outage amplification machine

That's enterprise-grade chaos confetti.

---

### **9. Inspect BackgroundService and hosted workers**

Search all:

- BackgroundService
- IHostedService
- timers
- schedulers
- queue consumers
- cache refresh jobs

Audit each one for:

- infinite loops without backoff
- catch + continue with no delay
- DB dependency inside polling loops
- cancellation token ignored
- overlapping executions
- logging every failure at error level on each iteration

Healthy loop:

```
while (!stoppingToken.IsCancellationRequested)
{
    try
    {
        await DoWork(stoppingToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Background work failed");
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
    }
}
```

Unhealthy loop:

```
while (!stoppingToken.IsCancellationRequested)
{
    try
    {
        await DoWork(stoppingToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "failed");
    }
}
```

That second one is a tiny reactor core.

---

### **10. Check connection pool behavior**

Look for signs of:

- pool exhaustion
- pooled connections becoming invalid after failover/outage
- callers timing out waiting for a pooled connection
- too many concurrent opens under failure

Questions to answer:

- Are connections short-lived per operation?
- Is one long transaction or reader holding resources?
- Are failed calls properly disposing connections?
- Is max pool size appropriate?
- Are callers stampeding to open new connections during outage?

Important nuance:

not closing connections properly is bad, but in modern .NET the bigger real-world monster is often **not disposal alone**, but **too many callers concurrently trying to recover badly**.

So don't stop at "missing using." That may be only one head of the hydra.

---

### **11. EF Core / Dapper specific checks**

#### **If using EF Core**

Inspect:

- DbContext lifetime
- retry execution strategy
- long-running tracked contexts
- shared DbContext across threads
- async query calls properly awaited

Red flags:

- singleton service using a scoped DbContext
- manual connection opens left hanging
- retry strategies wrapped again in Polly

#### **If using Dapper / raw ADO.NET**

Inspect:

- explicit connection ownership
- readers disposed
- command timeout settings
- command reuse across threads
- manual transaction handling

Dapper is fast and lovely, but it also happily lets you build a precision-engineered foot-cannon if ownership is sloppy.

---

### **12. Look at logging volume itself**

Logging can absolutely become part of the CPU spike.

Check:

- same exception logged repeatedly in tight loops
- large exception serialization
- synchronous or heavy enrichers
- logging to stdout at extreme volume in containers
- structured logging with giant payloads or object graphs
- duplicate logs at multiple layers for same failure

During outage, the app may be spending serious CPU just formatting and shipping misery.

A classic smell:

- repository logs error
- service logs error
- controller logs error
- middleware logs error
- background worker logs error
- probe logs error

One failed query becomes five flaming log entries.

---

### **13. OpenShift-specific things to inspect**

From pod/container behavior, check:

- did the pod restart during the event?
- were there OOMs or only CPU spikes?
- did readiness go false?
- did liveness kill the pod?
- were multiple replicas all affected at once?
- did HPA scale more pods, causing more DB pressure?
- did sidecars or service mesh retries amplify the issue?
- was DNS resolution or SQL listener resolution slow during AG failover?

Also inspect whether the app is using:

- SQL listener name correctly
- proper multi-subnet failover settings where relevant
- sane connection timeout / command timeout values

If AG failover or listener re-resolution is slow and the app retries too aggressively, you get the same pattern: self-inflicted CPU jazz.

---

### **14. A concrete code smell checklist**

Search for and flag every occurrence of:

- catch (Exception) with no delay before retry
- while(true) around DB calls
- _ = SomeDbMethodAsync()
- .Result / .Wait()
- Task.Run wrapping DB calls
- Timer callbacks using async lambdas badly
- shared SqlConnection
- shared DbContext
- background loops without Task.Delay
- health checks that open SQL connections every probe
- nested retry policies
- broad retries on non-transient exceptions
- cancellation token ignored
- logging inside tight retry loops
- retrying inside HTTP middleware for every request
- periodic jobs with no single-flight guard

That list catches an amazing amount of nonsense.

---

### **15. What to capture in one outage window**

For the next Sunday maintenance window, try to gather:

#### **From logs**

- exception counts grouped by type
- top failing message templates
- stack traces for first occurrence of each major exception
- number of retries attempted
- background service logs
- health check logs

#### **From metrics**

- CPU by pod
- restarts
- request rate
- thread count if available
- GC activity if available
- dependency failure count
- average/percentile DB call latency before full failure
- readiness/liveness transitions

#### **From platform**

- pod describe/events
- HPA events
- SQL AG maintenance/failover timestamps
- any network or DNS anomalies

The key is correlation. One graph without timing is just decorative suffering.

---

### **16. What I'd fix first even before perfect proof**

These are high-value hardening moves:

1. **Ensure every DB object is disposed correctly**
    
    await using where appropriate.
    
2. **Put bounded exponential backoff with jitter around real transient operations**
    
    Not infinite retries.
    
3. **Add a circuit breaker or temporary suppression**
    
    So when SQL is clearly down, callers stop dogpiling.
    
4. **Make background loops back off on failure**
    Always.
    
5. **Remove DB dependency from liveness probes**
    Or at least make probe behavior very conservative.
    
6. **Kill fire-and-forget DB work**
    Await it or explicitly manage it.
7. **Remove sync-over-async**
    
    That old demon still collects souls.
    
8. **Deduplicate logging**
    
    Log once per failure path, not five times per layer.

---

### **17. Tiny example of a healthier failure path**

```
public async Task<T> ExecuteWithSqlResilience<T>(
    Func<CancellationToken, Task<T>> action,
    CancellationToken cancellationToken)
{
    const int maxAttempts = 4;
    var delay = TimeSpan.FromSeconds(1);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await action(cancellationToken);
        }
        catch (SqlException ex) when (attempt < maxAttempts)
        {
            _logger.LogWarning(ex,
                "SQL operation failed on attempt {Attempt} of {MaxAttempts}",
                attempt, maxAttempts);

            var jitterMs = Random.Shared.Next(100, 500);
            await Task.Delay(delay + TimeSpan.FromMilliseconds(jitterMs), cancellationToken);
            delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
        }
    }

    // last attempt, let the real exception escape
    return await action(cancellationToken);
}
```

Even this is incomplete for all scenarios, but it's vastly less feral than hot-loop retries.

---

### **18. My strongest suspicion ranking**

Based on what you described, I would investigate in this order:

1. **background services or health checks repeatedly hitting SQL during outage**
2. **aggressive retry loops without backoff**
3. **async fan-out / overlapping work during failure**
4. **logging storm**
5. **connection disposal / pool exhaustion**
6. **sync-over-async thread-pool trouble**

That ordering matches the weird pattern of "DB offline causes app CPU spike."

---

### **19. Practical next step**

Do one focused grep/code review pass over:

- health checks
- BackgroundService
- retry helpers / Polly policies
- every SqlConnection open site
- every .Result / .Wait()
- every catch around DB calls

That usually finds the monster faster than staring at dashboards and whispering at Kubernetes.

I can also turn this into a **copy-paste incident runbook** for your team, with sections for **code review checks, Splunk query ideas, and OpenShift commands**.
