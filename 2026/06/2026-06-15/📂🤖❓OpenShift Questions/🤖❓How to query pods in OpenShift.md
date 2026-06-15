---
title: 🤖❓How to query pods in OpenShift
source: https://chatgpt.com/c/6a2b1fd9-3624-83e8-9ba4-634b791c95db
author:
published:
created: 2026-06-11T14:44:21-07:00
description: ChatGPT conversation with 4 messages
tags:
  - clippings
  - para/resources/open-shift
modified: 2026-06-13T11:26:13-07:00
uid: 01KTWAEGRQQTWTC499RE4DSR29
---

# 🤖❓How to query pods in OpenShift

## 🔗 References 
- [🤖❓How do I delete a service in OpenShift](</src/para/resources/📂🤖❓OpenShift Questions/🤖❓How do I delete a service in OpenShift.md>)

## 🤖❓How do I query what ports my pods are exposing?
I'm trying to figure out which ones have HTTPN points on them.

### 🤖💡In OpenShift/Kubernetes there are **three different things that people mean by "what port is this pod exposing?"**

If you're trying to find **HTTP endpoints**, you'll usually need to look at all three.

#### 1\. List declared container ports

For a specific pod:

```bash
oc get pod my-pod -o jsonpath='{range .spec.containers[*]}{.name}{"\n"}{range .ports[*]}  Port: {.containerPort}{"\n"}{end}{end}'
```

Or view the full YAML:

```bash
oc get pod my-pod -o yaml
```

Look for:

```yaml
containers:
- name: app
  ports:
  - containerPort: 8080
```

---

#### 2\. List ports for all pods in a namespace

```bash
oc get pods -o custom-columns=NAME:.metadata.name,PORTS:.spec.containers[*].ports[*].containerPort
```

##### Example output

```
NAME                PORTS
orders-api          8080
customer-api        8080
postgres            5432
redis               6379
```

---

#### 3\. Find Services exposing HTTP ports

This is often more useful than pod ports.

```bash
oc get svc
```

Or:

```bash
oc get svc -o wide
```

Example:

```
NAME          TYPE        PORT(S)
orders-api    ClusterIP   80/TCP
postgres      ClusterIP   5432/TCP
```

Detailed view:

```bash
oc describe svc orders-api
```

---

#### 4\. Find Routes (externally exposed HTTP endpoints)

OpenShift Routes are usually the easiest way to find web applications.

```bash
oc get routes
```

Or:

```bash
oc get route -A
```

Example:

```
NAME          HOST/PORT
orders-api    orders.apps.company.com
```

---

#### 5\. Find pods likely running HTTP servers

Search for common HTTP ports:

```bash
oc get pods -o json | jq -r '
.items[]
| .metadata.name as $name
| .spec.containers[]
| .ports[]?
| select(.containerPort == 80
      or .containerPort == 443
      or .containerPort == 8080
      or .containerPort == 8443)
| "\($name): \(.containerPort)"
'
```

---

#### 6\. Check what the container is actually listening on

Sometimes developers forget to declare `containerPort`.

Exec into the pod:

```bash
oc exec -it my-pod -- ss -tlnp
```

or

```bash
oc exec -it my-pod -- netstat -tlnp
```

Example:

```
LISTEN 0 128 *:8080
LISTEN 0 128 *:8443
```

This is the most authoritative answer.

---

#### 7\. Find readiness/liveness probes (great HTTP clue)

Many HTTP applications expose health endpoints.

```bash
oc get pod my-pod -o yaml
```

Look for:

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 8080

livenessProbe:
  httpGet:
    path: /actuator/health
    port: 8080
```

These are excellent indicators that the pod hosts an HTTP endpoint.

---

#### 8\. Find every HTTP probe in a namespace

```bash
oc get pods -o json | jq -r '
.items[]
| .metadata.name as $pod
| .spec.containers[]
| select(.readinessProbe.httpGet or .livenessProbe.httpGet)
| "\($pod)"
'
```

---

#### 9\. Get actual endpoint URLs from Routes

This is usually what developers really want:

```bash
oc get routes \
  -o custom-columns=NAME:.metadata.name,HOST:.spec.host
```

Then test:

```bash
curl https://orders.apps.company.com
```

---

##### My usual troubleshooting sequence

When I join a new OpenShift project, I typically run:

```bash
oc get pods
oc get svc
oc get routes
```

Then for anything interesting:

```bash
oc describe svc <service>
oc describe route <route>
oc get pod <pod> -o yaml
```

That usually lets me trace:

```
Route
  ↓
Service
  ↓
Pod
  ↓
Container Port
```

which is the complete HTTP request path through OpenShift.

---
