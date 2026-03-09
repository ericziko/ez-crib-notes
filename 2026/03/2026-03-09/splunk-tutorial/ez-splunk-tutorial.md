---
title: Splunk Tutorial — C# Memory Leaks & OpenShift/Kubernetes Errors
created: 2026-03-09
modified: 2026-03-09
tags:
  - splunk
  - spl
  - csharp
  - dotnet
  - kubernetes
  - openshift
  - memory-leak
  - observability
---

# 🔍 Splunk Tutorial — C# Memory Leaks & OpenShift / Kubernetes Errors

> **Stack assumptions for this guide:**
> - Logging: **Serilog**, **Microsoft.Extensions.Logging**, **OpenTelemetry**
> - Log format: **Structured JSON** (fields like `level`, `message`, `exception.type`, `app`, etc.)
> - Targets: memory leak detection + OpenShift/Kubernetes error hunting

---

## 🤖💡 Splunk SPL Fundamentals (Quick Reference)

| Concept | Syntax |
|---|---|
| Time range | `earliest=-1h latest=now` |
| Field filter | `\| where level="Error"` |
| Extract JSON field | `\| spath input=_raw path=exception.type` |
| Aggregate | `\| stats count by app, host` |
| Trend over time | `\| timechart span=5m count by app` |
| Top N | `\| top 10 exception.type` |
| Rename field | `\| rename exception.type as exType` |
| Regex extract | `\| rex field=_raw "Memory: (?<mem_mb>\d+) MB"` |

> **Tip:** With structured JSON logs from Serilog/OTEL, use `spath` once to parse all fields, or configure your Splunk index to auto-extract JSON at index time (far more efficient).

---

## 🤖💡 Part 1 — Hunting Memory Leaks in C# Applications

### 🧠 Why memory leaks show up in Splunk

When running C# apps on Kubernetes/OpenShift, memory pressure manifests across several log sources:

1. **Application logs** — `OutOfMemoryException`, GC warnings, custom health metrics
2. **Kubernetes events** — `OOMKilled` pod restarts
3. **OTEL metrics** — process memory and GC generation counters
4. **Liveness/readiness probe failures** — indirect signal of resource exhaustion

---

### 💡 Tip 1 — Catch `OutOfMemoryException` directly

```spl
index=your_app_index sourcetype=json
  (exception.type="System.OutOfMemoryException"
   OR message="*OutOfMemoryException*"
   OR exception="*OutOfMemoryException*")
| spath
| stats count by app, host, exception.type
| sort -count
```

**Why:** Serilog and MEL both serialize exception type into a structured field. With OTEL, look for `exception.type` in the span event attributes.

---

### 💡 Tip 2 — Detect GC Gen2 pressure via OTEL metrics logs

```spl
index=otel_metrics sourcetype=json
  metric_name="process.runtime.dotnet.gc.collections.count"
  attributes.generation="gen2"
| spath
| timechart span=5m max(value) as gen2_collections by service.name
```

**Why:** Rising Gen2 collection counts are a classic signal of heap pressure. A spike followed by a crash = memory leak pattern.

---

### 💡 Tip 3 — Track process memory growth over time

```spl
index=otel_metrics sourcetype=json
  metric_name="process.runtime.dotnet.gc.heap.size"
| spath
| timechart span=10m max(value) as heap_bytes by service.name
```

**Why:** A steadily climbing heap size (sawtooth pattern gets wider over time with no full drops) is the classic leak signature.

---

### 💡 Tip 4 — Correlate OOMKilled pod restarts with app logs

```spl
index=k8s_events sourcetype=json
  reason="OOMKilling"
| spath
| rename involvedObject.name as pod_name, involvedObject.namespace as namespace
| eval oom_time=_time
| table oom_time, namespace, pod_name, message
| join pod_name [
    search index=your_app_index sourcetype=json
    | spath
    | stats latest(_time) as last_log_time by kubernetes.pod.name
    | rename kubernetes.pod.name as pod_name
]
| eval lag_seconds=oom_time - last_log_time
| table oom_time, namespace, pod_name, last_log_time, lag_seconds
```

**Why:** This join lets you see which app log was the last before an OOM kill — pointing you to the hot path.

---

### 💡 Tip 5 — Find pods restarting frequently (OOMKilled pattern)

```spl
index=k8s_events sourcetype=json
  reason="OOMKilling"
| spath
| timechart span=1h count by involvedObject.name
| sort -count
```

**Why:** Pods that restart on a schedule (e.g., every 6 hours) with OOMKill are the clearest leak indicator — memory fills up on a predictable cycle.

---

### 💡 Tip 6 — Watch for `Finalizer` thread saturation (Serilog diagnostic logs)

```spl
index=your_app_index sourcetype=json
  message="*Finalizer*" OR message="*finalizer queue*" OR message="*GC.WaitForPendingFinalizers*"
| spath
| stats count by app, host
| where count > 10
```

**Why:** A backed-up finalizer queue is a strong signal that objects with finalizers are being created faster than they can be cleaned up — a common unmanaged resource leak pattern in C#.

---

### 💡 Tip 7 — Alert on memory usage approaching pod limits

```spl
index=otel_metrics sourcetype=json
  metric_name="process.runtime.dotnet.gc.heap.size"
| spath
| eval heap_mb = value / 1024 / 1024
| stats max(heap_mb) as peak_heap_mb by service.name, host
| where peak_heap_mb > 800
| sort -peak_heap_mb
```

> Adjust `800` to match ~80% of your pod's memory limit. Use this as a Splunk Alert to get ahead of OOMKills.

---

### 💡 Tip 8 — Find large object heap (LOH) pressure indicators

```spl
index=otel_metrics sourcetype=json
  metric_name="process.runtime.dotnet.gc.committed_memory.size"
| spath
| eval committed_mb = value / 1024 / 1024
| timechart span=5m max(committed_mb) as committed_mb by service.name
```

**Why:** Committed memory growing beyond heap size indicates LOH or native allocations — common in apps that process large byte arrays (e.g., file upload, image processing).

---

### 💡 Tip 9 — Spot the "last 100 errors before OOM crash"

```spl
index=your_app_index sourcetype=json level="Error"
| spath
| where app="your-service-name"
| sort _time desc
| head 100
| table _time, level, message, exception.type, exception.message
```

**Why:** Memory leak crashes rarely come out of nowhere. The 100 errors before the crash often contain the root cause — connection pool exhaustion, failed disposes, retry storms, etc.

---

### 💡 Tip 10 — Build a memory leak triage dashboard query

```spl
index=your_app_index OR index=otel_metrics OR index=k8s_events sourcetype=json
  (metric_name="process.runtime.dotnet.gc.heap.size"
   OR exception.type="System.OutOfMemoryException"
   OR reason="OOMKilling")
| spath
| eval signal=case(
    isnotnull(exception.type), "OOM Exception",
    isnotnull(reason),         "OOMKill Event",
    isnotnull(metric_name),    "Heap Metric"
  )
| eval service=coalesce(app, 'service.name', 'involvedObject.name')
| timechart span=15m count by signal
```

**Why:** A single panel that overlays all three signals lets you see the sequence — heap grows, then exceptions, then OOMKill.

---

## 🤖💡 Part 2 — 10 Practical Splunk Queries for OpenShift / Kubernetes Errors

> Queries are provided in both **namespace-scoped** and **cluster-wide** variants.

---

### 🔴 Query 1 — Pod crash loops (CrashLoopBackOff)

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json
  reason="BackOff" message="*CrashLoopBackOff*"
| spath
| stats count by involvedObject.namespace, involvedObject.name
| sort -count
```

**Namespaced:**
```spl
index=k8s_events sourcetype=json
  reason="BackOff" message="*CrashLoopBackOff*"
  involvedObject.namespace="your-namespace"
| spath
| timechart span=15m count by involvedObject.name
```

---

### 🔴 Query 2 — OOMKilled pods

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json reason="OOMKilling"
| spath
| stats count as oom_kills by involvedObject.namespace, involvedObject.name
| sort -oom_kills
```

**Namespaced:**
```spl
index=k8s_events sourcetype=json
  reason="OOMKilling"
  involvedObject.namespace="your-namespace"
| spath
| timechart span=1h count by involvedObject.name
```

---

### 🔴 Query 3 — Image pull failures (ImagePullBackOff)

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json
  (reason="Failed" OR reason="BackOff")
  message="*ImagePullBackOff*" OR message="*ErrImagePull*"
| spath
| stats count by involvedObject.namespace, involvedObject.name, message
| sort -count
```

**Namespaced:**
```spl
index=k8s_events sourcetype=json
  involvedObject.namespace="your-namespace"
  message="*ImagePullBackOff*"
| spath
| table _time, involvedObject.name, message
```

---

### 🔴 Query 4 — Node pressure and resource exhaustion events

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json
  (reason="NodeHasDiskPressure"
   OR reason="NodeHasMemoryPressure"
   OR reason="NodeHasPIDPressure"
   OR reason="Evicted")
| spath
| stats count by reason, involvedObject.name
| sort -count
```

---

### 🔴 Query 5 — Liveness/readiness probe failures

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json
  reason="Unhealthy"
  (message="*Liveness probe*" OR message="*Readiness probe*")
| spath
| stats count by involvedObject.namespace, involvedObject.name, message
| sort -count
```

**Namespaced:**
```spl
index=k8s_events sourcetype=json
  reason="Unhealthy"
  involvedObject.namespace="your-namespace"
| spath
| timechart span=10m count by involvedObject.name
```

---

### 🔴 Query 6 — Application-level error rate by service

**Cluster-wide (all apps logging to Splunk):**
```spl
index=your_app_index sourcetype=json level="Error"
| spath
| timechart span=5m count by kubernetes.namespace
```

**Namespaced:**
```spl
index=your_app_index sourcetype=json
  level="Error"
  kubernetes.namespace="your-namespace"
| spath
| timechart span=5m count by kubernetes.pod.name
```

---

### 🔴 Query 7 — 5xx errors from OpenShift router / ingress

**Cluster-wide:**
```spl
index=openshift_router sourcetype=json
  status>=500
| spath
| stats count as error_count by status, upstream_addr, request
| sort -error_count
| head 20
```

**Namespaced (filter by host/route):**
```spl
index=openshift_router sourcetype=json
  status>=500
  host="*.your-namespace.svc*"
| spath
| timechart span=5m count by status
```

---

### 🔴 Query 8 — Pod scheduling failures (Insufficient resources)

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json
  reason="FailedScheduling"
  (message="*Insufficient memory*"
   OR message="*Insufficient cpu*"
   OR message="*No nodes are available*")
| spath
| stats count by message
| sort -count
```

---

### 🔴 Query 9 — Volume mount / PVC failures

**Cluster-wide:**
```spl
index=k8s_events sourcetype=json
  (reason="FailedMount"
   OR reason="FailedAttachVolume"
   OR reason="ProvisioningFailed")
| spath
| stats count by involvedObject.namespace, reason, message
| sort -count
```

**Namespaced:**
```spl
index=k8s_events sourcetype=json
  reason="FailedMount"
  involvedObject.namespace="your-namespace"
| spath
| table _time, involvedObject.name, message
| sort -_time
```

---

### 🔴 Query 10 — Top error patterns across the cluster (anomaly detection)

**Cluster-wide — surface the most frequent error messages:**
```spl
index=your_app_index OR index=k8s_events sourcetype=json
  (level="Error" OR level="Fatal" OR reason="Failed" OR reason="BackOff")
| spath
| eval error_signal=coalesce(message, 'exception.message')
| rex field=error_signal mode=sed "s/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/UUID/g"
| rex field=error_signal mode=sed "s/\b\d+\b/N/g"
| stats count by error_signal
| sort -count
| head 20
```

**Why the regex replacements?** UUIDs and numbers make each error look unique. Normalising them groups the same error pattern together, making the top-20 actually useful.

---

## 🤖💡 Part 3 — Splunk Workflow Tips for .NET on Kubernetes

### Structure your searches for speed

```
1. Always specify index= and sourcetype= first
2. Filter with raw-text terms (quoted strings) before | spath or | where
3. Use | spath only once per search — it parses all JSON fields
4. Use | fields to drop columns you don't need early in the pipeline
```

### Useful Serilog field names in Splunk (structured JSON)

| Serilog property | Splunk field (after spath) |
|---|---|
| `@timestamp` | `_time` (auto-mapped) |
| `@l` / `level` | `level` |
| `@m` / `message` | `message` |
| `@x` (exception) | `exception`, `exception.type`, `exception.message` |
| `SourceContext` | `SourceContext` |
| `RequestId` | `RequestId` |
| App name | `app` or `service.name` (OTEL) |

### Useful OpenTelemetry metric names for .NET memory

| Metric | Meaning |
|---|---|
| `process.runtime.dotnet.gc.heap.size` | Total managed heap size |
| `process.runtime.dotnet.gc.committed_memory.size` | Committed (reserved) memory |
| `process.runtime.dotnet.gc.collections.count` | GC collections by generation |
| `process.runtime.dotnet.gc.objects.size` | Live objects on heap |
| `process.runtime.dotnet.gc.allocations.size` | Total bytes allocated since start |

---

## 🤖❓ Questions I Have For You

To make this guide even more tailored, consider:

1. **Which Splunk indexes** are your app logs, OTEL metrics, and K8s events going into? (The queries above use placeholder names like `your_app_index` — we can update them.)
2. **Do you have OpenTelemetry Collector** shipping metrics to Splunk, or are GC metrics logged as structured log events?
3. **What memory limits** are your pods configured with? (Helps calibrate the alert threshold in Tip 7.)
4. **Are you using OpenShift-specific logging** (Cluster Logging Operator / Loki) or shipping directly to Splunk via a Fluentd/Vector forwarder?
5. **Do you want Splunk Alert definitions** (trigger conditions, throttle settings) for any of these queries?

---

*Generated: 2026-03-09 | Tags: `splunk` `spl` `csharp` `kubernetes` `openshift` `memory-leak` `observability`*
