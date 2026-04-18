---
title: OpenShift on Mac — Bootstrap & Deploy .NET to CRC
created: 2026-04-17
modified: 2026-04-17
tags:
  - openshift
  - crc
  - kubernetes
  - dotnet
  - macos
  - orbstack
  - tutorial
  - s2i
---

# 🛳️ OpenShift on Mac — Bootstrap & Deploy a .NET App to Local CRC

> **Goal:** Stand up OpenShift Local (CRC) on macOS, build a small ASP.NET Core service with OrbStack, deploy it to the local cluster, and expose it via a Route — all offline-friendly, no cloud costs.
>
> **Audience:** A .NET engineer who already knows containers but is new to OpenShift's opinions (Routes, ImageStreams, BuildConfigs, SCCs).

---

## 🗺️ Action Plan (at a glance)

| Phase | Outcome | Est. time |
|---|---|---|
| 1. Prereqs | CRC, OrbStack, `oc`, `dotnet` verified | 10 min |
| 2. CRC bootstrap | Cluster running, `oc` logged in | 40–60 min (first run) |
| 3. Orient to OpenShift | Project created, registry reachable | 15 min |
| 4. Build a .NET minimal API | Hello app + `/healthz` | 15 min |
| 5. Deploy via S2I (no Dockerfile) | App reachable via Route | 15 min |
| 6. Deploy via OrbStack-built image | Push to internal registry + Deployment | 20 min |
| 7. Add liveness/readiness + ConfigMap | Production-ish manifest | 20 min |
| 8. Teardown / cost control | Clean shutdown, free RAM | 5 min |

---

## 🧰 Phase 1 — Prerequisites

### ✅ What you should already have

```bash
crc version              # CRC itself
orb version              # OrbStack
dotnet --list-sdks       # .NET SDK (prefer 9.0 or latest LTS)
```

### 🔧 What you probably still need

```bash
# oc CLI (OpenShift client) — get it via brew or from crc itself
brew install openshift-cli

# Optional but recommended
brew install stern        # multi-pod log tailing
brew install k9s          # TUI for cluster navigation
brew install yq           # YAML surgery
```

### 🔑 Pull secret

You need a Red Hat pull secret (free, requires a Red Hat account):

1. Visit <https://console.redhat.com/openshift/create/local>
2. Download `pull-secret.txt`
3. Save to `~/.crc/pull-secret.txt` (or any path you remember)

---

## 🚀 Phase 2 — Bootstrap CRC

### 🤖💡 Pick a preset

CRC supports three presets. For .NET work, **`openshift`** (full OpenShift) is what you want:

```bash
crc config set preset openshift
```

### 🧮 Size it right for .NET

Default 4 CPU / 10.5 GB RAM is tight once you add a .NET app + builder pod. Bump it:

```bash
crc config set cpus 6
crc config set memory 16384          # 16 GB
crc config set disk-size 60          # 60 GB
crc config set kubeadmin-password "change-me-local"   # optional, predictable password
crc config set consent-telemetry no  # optional
```

> **Why so much RAM?** S2I builds, the internal registry, router, monitoring stack, plus your .NET app's build + runtime pods. 10 GB will OOM.

### 🏁 Run setup & start

```bash
crc setup                          # one-time: installs the bundle, configures host
crc start -p ~/.crc/pull-secret.txt
```

Go make coffee. First boot is ~35 min on Apple Silicon. Subsequent starts are ~3 min.

### 🔐 Log in

```bash
eval $(crc oc-env)                 # puts the bundled oc on your PATH
crc console --credentials          # prints kubeadmin + developer creds

# Developer persona for everyday work
oc login -u developer -p developer https://api.crc.testing:6443

# Admin persona when you need it
oc login -u kubeadmin -p <password-from-above> https://api.crc.testing:6443
```

Web console: `crc console` opens <https://console-openshift-console.apps-crc.testing>.

---

## 🧭 Phase 3 — Orient Yourself to OpenShift

### 🤖💡 K8s concepts → OpenShift equivalents

| Kubernetes | OpenShift | Notes |
|---|---|---|
| Namespace | **Project** | Same API object, plus quotas/limits/SCCs bound by default |
| Ingress | **Route** | Route existed first; Ingress is also supported |
| — | **ImageStream** | Versioned, mutable pointer to a container image |
| — | **BuildConfig** | Declarative build pipeline (S2I, Docker, or custom) |
| PodSecurityStandards | **SCC** (SecurityContextConstraints) | Much stricter defaults — random UID, no root |
| — | **DeploymentConfig** | Legacy; prefer `Deployment` for new work |

> 💡 **The one that bites .NET devs:** SCCs mean your container will run as a random high UID. Base images from Microsoft (`mcr.microsoft.com/dotnet/aspnet`) need tweaks (writable tmp, non-1000 UID). Red Hat's `registry.access.redhat.com/ubi8/dotnet-*` S2I images are already SCC-compliant — prefer them locally.

### 📁 Create a project

```bash
oc new-project hello-dotnet --display-name="Hello .NET on CRC"
```

### 📦 Confirm the internal registry

On modern CRC it's exposed by default at:

```
default-route-openshift-image-registry.apps-crc.testing
```

Quick sanity check:

```bash
oc registry info                   # prints the internal registry URL
oc get co image-registry           # ClusterOperator should be Available=True
```

---

## 🛠️ Phase 4 — A Tiny .NET Minimal API

```bash
mkdir hello-openshift && cd hello-openshift
dotnet new web -n HelloOpenShift -o .
```

Replace `Program.cs` with something probeable:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => new {
    message = "Hello, OpenShift!",
    host    = Environment.MachineName,
    now     = DateTimeOffset.UtcNow
});

app.MapHealthChecks("/healthz");
app.MapHealthChecks("/readyz");

// Kestrel defaults to :8080 when ASPNETCORE_URLS is set — set it in container env
app.Run();
```

Local smoke test (optional):

```bash
ASPNETCORE_URLS=http://localhost:8080 dotnet run
curl http://localhost:8080/healthz
```

> 🔎 **Why port 8080?** OpenShift's non-root SCC forbids binding ports <1024. `8080` is the community convention for non-root web apps.

---

## 🪄 Phase 5 — Fastest Path: S2I (No Dockerfile)

Source-to-Image lets OpenShift build directly from a Git repo (or local dir) using a language-specific builder image. No Dockerfile required.

### Option A — from a local directory

```bash
# From your hello-openshift folder:
oc new-app \
  --name=hello-dotnet \
  --image-stream=openshift/dotnet:9.0-ubi8 \
  --binary=true

# Kick off the build from local source
oc start-build hello-dotnet --from-dir=. --follow
```

> ⚠️ If `oc get is -n openshift | grep dotnet` returns nothing, import the stream: <br>
> `oc apply -f https://raw.githubusercontent.com/redhat-developer/s2i-dotnetcore/main/dotnet_imagestreams.json -n openshift`

### Option B — from a Git repo

```bash
oc new-app \
  openshift/dotnet:9.0-ubi8~https://github.com/<you>/hello-openshift.git \
  --name=hello-dotnet \
  --env DOTNET_STARTUP_PROJECT=HelloOpenShift.csproj
```

### 🌐 Expose it

```bash
oc expose svc/hello-dotnet
oc get route hello-dotnet -o jsonpath='{.spec.host}{"\n"}'
# Visit https://hello-dotnet-hello-dotnet.apps-crc.testing
```

### 🐛 When a build fails

```bash
oc logs -f bc/hello-dotnet          # build logs
oc logs -f deploy/hello-dotnet      # runtime logs
oc describe pod -l app=hello-dotnet # events + SCC denials
```

---

## 🐳 Phase 6 — Bring-Your-Own Image (OrbStack route)

Use OrbStack when you want a Dockerfile-based pipeline, matching what you'd run in CI.

### 1. Dockerfile (multi-stage, SCC-friendly)

```dockerfile
# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY HelloOpenShift.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080
# Write to /tmp works on any SCC; avoid /app writes at runtime
USER 1001
EXPOSE 8080
ENTRYPOINT ["dotnet", "HelloOpenShift.dll"]
```

### 2. Build with OrbStack

```bash
# OrbStack provides the docker CLI — build multi-arch if you like, or native arm64
docker build -t hello-dotnet:0.1 .
```

### 3. Push to the CRC internal registry

```bash
# Login to the internal registry
oc registry login

# Tag for the registry + project
REGISTRY=default-route-openshift-image-registry.apps-crc.testing
docker tag hello-dotnet:0.1 $REGISTRY/hello-dotnet/hello-dotnet:0.1
docker push $REGISTRY/hello-dotnet/hello-dotnet:0.1
```

> 💡 If `docker push` 401s, run `oc registry login --skip-check=false` and confirm your kubeconfig context is CRC.

### 4. Deploy it

`deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hello-dotnet
  labels: { app: hello-dotnet }
spec:
  replicas: 1
  selector:
    matchLabels: { app: hello-dotnet }
  template:
    metadata:
      labels: { app: hello-dotnet }
    spec:
      containers:
        - name: app
          image: image-registry.openshift-image-registry.svc:5000/hello-dotnet/hello-dotnet:0.1
          ports: [{ containerPort: 8080 }]
          readinessProbe:
            httpGet: { path: /readyz, port: 8080 }
            initialDelaySeconds: 3
            periodSeconds: 5
          livenessProbe:
            httpGet: { path: /healthz, port: 8080 }
            initialDelaySeconds: 15
            periodSeconds: 10
          resources:
            requests: { cpu: 100m, memory: 128Mi }
            limits:   { cpu: 500m, memory: 512Mi }
---
apiVersion: v1
kind: Service
metadata:
  name: hello-dotnet
spec:
  selector: { app: hello-dotnet }
  ports:
    - name: http
      port: 8080
      targetPort: 8080
---
apiVersion: route.openshift.io/v1
kind: Route
metadata:
  name: hello-dotnet
spec:
  to:
    kind: Service
    name: hello-dotnet
  port:
    targetPort: http
  tls:
    termination: edge
    insecureEdgeTerminationPolicy: Redirect
```

```bash
oc apply -f deployment.yaml
oc get route hello-dotnet -o jsonpath='{.spec.host}{"\n"}'
```

> 🧠 **Why the in-cluster registry hostname (`image-registry.openshift-image-registry.svc:5000`)?** That's the service DNS nodes use to pull. The external `default-route-...` URL is only for `docker push` from your laptop.

---

## 🧪 Phase 7 — Tighten It Up

### ConfigMap + env wiring

```bash
oc create configmap hello-dotnet-cfg --from-literal=Greeting__Name=CRC
oc set env deploy/hello-dotnet --from=configmap/hello-dotnet-cfg
```

Read it in code (standard `IConfiguration` — underscores map to colons):

```csharp
var name = builder.Configuration["Greeting:Name"] ?? "World";
app.MapGet("/hello", () => $"Hello, {name}!");
```

### 🔭 Observability quickies

```bash
# Live logs across replicas
stern -l app=hello-dotnet

# Pod-level events and SCC denials
oc describe pod -l app=hello-dotnet | less

# Port-forward for local curl if a Route is flaky
oc port-forward svc/hello-dotnet 8080:8080
```

### 🔐 Secrets (don't use ConfigMaps for these)

```bash
oc create secret generic hello-dotnet-conn --from-literal=ConnectionStrings__Default='Server=...;'
oc set env deploy/hello-dotnet --from=secret/hello-dotnet-conn
```

### 🧯 SCC gotchas cheat sheet

| Symptom | Likely cause | Fix |
|---|---|---|
| `CrashLoopBackOff`, log shows `Permission denied` writing `/app` | Non-root UID can't write image dir | Use `/tmp` or a `emptyDir` volume |
| `container has runAsNonRoot and image has non-numeric user` | Microsoft base image declares `USER app` by name | Add `USER 1001` (numeric) in Dockerfile |
| Port binding denied for :80 / :443 | SCC forbids privileged ports | Bind `:8080`, use Route/TLS termination |
| Build fails `chown: ...: Operation not permitted` | Dockerfile assumes root | Use UBI-based S2I, or stop chowning |

---

## 🧹 Phase 8 — Teardown & Daily Habits

```bash
# Pause the cluster, keep state
crc stop

# Nuke everything
crc delete
crc cleanup

# Free up even more: stop OrbStack when not needed
orb stop
```

### 💤 Daily workflow that doesn't melt your laptop

1. `crc start` in the morning (≈3 min warm boot).
2. Keep the CRC VM at 16 GB; raise CPU only while building.
3. `crc stop` before sleep — `crc delete` is only for "oops" recovery.
4. Use OrbStack for fast iteration (`docker build + push`); let S2I handle "this time I want the whole pipeline in-cluster."

---

## 🧩 Where to Go Next

- **Helm / Kustomize**: Swap the raw YAML in Phase 6 for a chart. OpenShift happily installs community charts (`oc new-project; helm install ...`).
- **Tekton Pipelines**: Red Hat's CI-in-cluster — replace S2I with a pipeline that lints, tests, scans, deploys.
- **OpenShift GitOps (Argo CD)**: Put the YAML from Phase 6 in Git, let Argo reconcile.
- **Aspire → OpenShift**: .NET Aspire's manifest can be translated to K8s manifests; good follow-up doc.
- **OpenTelemetry Collector on CRC**: deploy the collector, point your app's OTLP exporter at it, and round-trip traces locally.

---

## 🤖💬❓ Clarifying Questions for a Follow-Up Pass

If you want me to extend this tutorial, I need to know:

1. **Which .NET version are you targeting?** I defaulted to 9.0; let me know if it should be 8 LTS or 10-preview.
2. **Do you want Aspire-first or plain ASP.NET-first examples?** Aspire changes the deploy story significantly (manifests + AppHost).
3. **GitHub or local-only?** Phase 5 Option B assumes a remote repo — is there a repo you'd like wired in?
4. **Data tier?** Want a follow-up deploying SQL Server or Postgres alongside, with a StatefulSet + PVC?
5. **Are you on Apple Silicon or Intel?** ARM64 is fully supported by current CRC, but a couple of the UBI images still lag; I can call out specifics if needed.
6. **CRC vs. MicroShift?** CRC = full OpenShift; MicroShift = single-node, resource-light. If your laptop is tight on RAM, MicroShift on OrbStack might be a better fit — happy to write that variant.

---

## 📎 Quick Reference — Commands You'll Use Daily

```bash
# Cluster lifecycle
crc start -p ~/.crc/pull-secret.txt
crc stop
crc status
crc console --credentials

# Context
eval $(crc oc-env)
oc whoami
oc project hello-dotnet

# Build/deploy loop (S2I)
oc start-build hello-dotnet --from-dir=. --follow
oc logs -f deploy/hello-dotnet

# Build/deploy loop (OrbStack)
docker build -t $REGISTRY/hello-dotnet/hello-dotnet:$(git rev-parse --short HEAD) .
docker push  $REGISTRY/hello-dotnet/hello-dotnet:$(git rev-parse --short HEAD)
oc set image deploy/hello-dotnet app=image-registry.openshift-image-registry.svc:5000/hello-dotnet/hello-dotnet:$(git rev-parse --short HEAD)

# Investigate
oc get pods,routes,svc
oc describe pod -l app=hello-dotnet
stern -l app=hello-dotnet
```
