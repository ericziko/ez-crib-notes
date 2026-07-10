---
title: .NET Aspire + Data Tier on OpenShift — Full Stack Deployment (CRC & MicroShift)
created: 2026-04-19
modified: 2026-04-19
tags:
  - openshift
  - aspire
  - dotnet
  - kubernetes
  - microshift
  - crc
  - postgres
  - sqlserver
  - macos
  - apple-silicon
  - tutorial
uid: 015e6090-4473-4568-8d2d-27189045b868
---

# 🚀 .NET Aspire + Data Tier on OpenShift — CRC & MicroShift Full-Stack Deployment

> **Goal:** Deploy a .NET 9.0 Aspire application with a persistent database (SQL Server or Postgres) to both OpenShift Local (CRC) and MicroShift, from both GitHub and local-only workflows, optimized for Apple Silicon.
>
> **Audience:** .NET engineers familiar with Aspire's orchestration model (`AppHost` + service manifests) and ready to bridge it to Kubernetes deployments.
>
> **What you'll learn:** Aspire → K8s manifest translation, StatefulSets for data persistence, Apple Silicon ARM64 considerations, and the operational trade-offs between CRC (full OpenShift) and MicroShift (lightweight).

---

## 🗺️ Action Plan (at a glance)

| Phase | Outcome | Est. time |
|---|---|---|
| 1. Choose your path | CRC or MicroShift; GitHub or local-only | 5 min |
| 2. Prereqs (path-specific) | Cluster + tooling ready for Apple Silicon | 15 min |
| 3. Aspire scaffold | AppHost + two services + database ref | 20 min |
| 4. Local Aspire run | Verify dashboard, dependencies wire correctly | 10 min |
| 5a. GitHub path: Sync repo + wire CI/CD | Actions → push manifests | 15 min |
| 5b. Local path: Manual manifest export | `dotnet run --project AppHost -- --output json` | 10 min |
| 6. Database setup (CRC-specific or MicroShift-specific) | StatefulSet + PVC + init scripts | 20 min |
| 7. Deploy services to cluster | Apply manifests, wire secrets | 15 min |
| 8. Route + troubleshoot | Expose Aspire frontend, verify data flow | 15 min |
| 9. Data persistence verification | Insert, restart pod, verify durability | 10 min |
| 10. Cleanup & cost control | Stop/pause cluster | 5 min |

**Total:** ~2 hours (first pass includes cluster bootstrap).

---

## Phase 1 — Your Deployment Path

### Decision Matrix

```
┌─────────────────────┬────────────────────────────────────────────┐
│ Cluster Choice      │ Best For                                   │
├─────────────────────┼────────────────────────────────────────────┤
│ CRC (OpenShift)     │ Full OpenShift features, advanced routing, │
│                     │ RBAC, quotas; ~16 GB RAM on Apple Silicon │
├─────────────────────┼────────────────────────────────────────────┤
│ MicroShift (OrbSt.) │ Minimal footprint (~4 GB), faster startup, │
│                     │ single-node, perfect for laptops; edge use │
└─────────────────────┴────────────────────────────────────────────┘

┌──────────────────────┬─────────────────────────────────────────────┐
│ Repo Choice          │ Best For                                    │
├──────────────────────┼─────────────────────────────────────────────┤
│ GitHub + CI/CD       │ Team workflows, automated deployments,      │
│                      │ branch-per-env; manifests always in sync    │
├──────────────────────┼─────────────────────────────────────────────┤
│ Local-only           │ Solo dev, fast iteration, no CI config,     │
│                      │ manifests on your disk, manual push         │
└──────────────────────┴─────────────────────────────────────────────┘
```

**Recommended:** Start with **CRC + Local-only** for simplicity. Once comfortable, upgrade to GitHub + MicroShift for production-like workflows.

---

## Phase 2 — Prerequisites (Path-Specific)

### ✅ Common (all paths)

```bash
# Verify your environment
dotnet --list-sdks           # Need 9.0+
dotnet workload list         # Should see aspire

# If missing Aspire workload:
dotnet workload restore

# Tools
brew install openshift-cli   # oc CLI
brew install yq              # YAML queries
```

### 🧿 CRC Path Setup

```bash
# CRC itself
crc version
crc config set preset openshift
crc config set cpus 6
crc config set memory 16384
crc config set disk-size 60
crc config set consent-telemetry no

# Pull secret (free Red Hat account)
# Visit https://console.redhat.com/openshift/create/local
# Save to ~/.crc/pull-secret.txt

# Start cluster
crc setup
crc start -p ~/.crc/pull-secret.txt

# After startup (≈35 min first time, ≈3 min warm)
eval $(crc oc-env)
oc login -u developer -p developer https://api.crc.testing:6443
oc new-project aspire-demo --display-name="Aspire Full Stack"
```

**Apple Silicon Notes:**
- CRC ARM64 support is complete; UBI images are current as of early 2025.
- No special ARM64 workarounds needed; all the images in this guide are multi-arch.

### 🧿 MicroShift Path Setup

MicroShift runs inside OrbStack as a lightweight single-node cluster.

```bash
# Install MicroShift via Homebrew (works on Apple Silicon M1/M2/M3)
brew tap redhat-developer/tap
brew install microshift

# Start it (runs in background)
microshift run &

# Wait ~20 seconds for startup, then set kubeconfig
export KUBECONFIG=~/.microshift/kubeconfig
oc login -u developer -p developer https://localhost:6443

# Create project
oc new-project aspire-demo --display-name="Aspire Full Stack"
```

**Apple Silicon MicroShift:**
- Native ARM64 build available; no emulation needed.
- Runs entirely inside OrbStack VM; much lighter than CRC.
- Default cgroup v2 + SELinux disabled → fewer SCC surprises.

---

## Phase 3 — Scaffold an Aspire Application

### Create the AppHost + Services

```bash
mkdir aspire-full-stack && cd aspire-full-stack

# Aspire project scaffold
dotnet new aspire-starter -n AspireFullStack -o .
```

This generates:
- `AspireAppHost/` — orchestration & manifest generation
- `AspireFullStack.AppHost/` — AppHost project (defines services)
- `AspireFullStack.ApiService/` — a .NET service
- `AspireFullStack.Web/` — frontend (or leave it out)
- `.sln` — solution file

### Modify AppHost to add a data service

Edit `AspireFullStack.AppHost/Program.cs`:

```csharp
using Aspire.Hosting;
using Aspire.Hosting.PostgreSQL;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database (Postgres path)
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var pgdb = postgres.AddDatabase("demo-db");

// OR add SQL Server (uncomment and comment Postgres above)
// var sqlserver = builder.AddSqlServer("sqlserver")
//     .WithEnvironment("MSSQL_SA_PASSWORD", "Dev@1234");
// var sqldb = sqlserver.AddDatabase("demo-db");

// API service wired to database
var apiService = builder.AddProject<Projects.AspireFullStack_ApiService>("api")
    .WithReference(pgdb)
    .WaitFor(pgdb);

// Web frontend (optional)
builder.AddProject<Projects.AspireFullStack_Web>("web")
    .WithReference(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### Update API service to use the database

Edit `AspireFullStack.ApiService/Program.cs`:

```csharp
using System.Data;
using Npgsql;  // For PostgreSQL
// OR using Microsoft.Data.SqlClient;  // For SQL Server

var builder = WebApplication.CreateBuilder(args);

// Database connection from Aspire
var connectionString = builder.Configuration.GetConnectionString("demo-db");

builder.Services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));
// OR: new SqlConnection(connectionString) for SQL Server

var app = builder.Build();

app.MapGet("/", async (IDbConnection db) => {
    using var cmd = db.CreateCommand();
    cmd.CommandText = "SELECT version();";  // Postgres; use @@version for SQL Server
    return await cmd.ExecuteScalarAsync();
});

app.MapHealthChecks("/healthz");
app.Run();
```

---

## Phase 4 — Local Aspire Dashboard Test

```bash
cd AspireAppHost

# Run the AppHost (starts dashboard + all services locally)
dotnet run

# Dashboard appears at http://localhost:18888
# You should see:
#   - postgres (or sqlserver) container running
#   - api service healthy
#   - web service healthy
#   - logs from each service
```

**Verify:** Open `http://localhost:8000` (or the assigned port) and confirm the API returns the database version.

Hit **Ctrl+C** to stop. All containers are torn down (data lost; local-only).

---

## Phase 5 — Export Manifests (Path-Specific)

### Option A — GitHub Path

1. **Create a GitHub repo** and push this folder:
   ```bash
   git init
   git add .
   git commit -m "Initial Aspire project"
   git remote add origin https://github.com/<you>/aspire-full-stack.git
   git push -u origin main
   ```

2. **Create `.github/workflows/deploy.yml`** (in the repo root):
   ```yaml
   name: Deploy to OpenShift
   on:
     push:
       branches: [main]
   jobs:
     deploy:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v4
         - uses: actions/setup-dotnet@v4
           with:
             dotnet-version: '9.0.x'
         
         - name: Generate Aspire manifests
           run: |
             cd AspireAppHost
             dotnet publish -c Release -o ./publish
             dotnet run --project AspireAppHost.csproj -- --output json > ../manifests.json
           env:
             ASPIRE_ALLOW_UNSECURED_TRANSPORT: "true"
         
         - name: Upload artifacts
           uses: actions/upload-artifact@v4
           with:
             name: manifests
             path: manifests.json
   ```

3. **Push & verify** — GitHub Actions runs; manifests are generated and available as artifacts.

### Option B — Local-only Path

```bash
cd AspireAppHost

# Export manifests as JSON (Aspire's Kubernetes manifest format)
dotnet run --project AspireAppHost.csproj -- --output json > ../aspire-manifests.json

# View the generated manifest structure
cat ../aspire-manifests.json | jq '.resources | length'
```

This creates a single JSON file with all service + database definitions. You'll translate these into standard K8s YAML next.

---

## Phase 6 — Database Setup (Cluster-Specific)

### CRC: StatefulSet + PVC for PostgreSQL

Create `postgres-statefulset.yaml`:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
spec:
  accessModes: [ "ReadWriteOnce" ]
  storageClassName: crc-csi-hostpath-sc  # Built-in CRC storage class
  resources:
    requests:
      storage: 10Gi
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels: { app: postgres }
  template:
    metadata:
      labels: { app: postgres }
    spec:
      containers:
      - name: postgres
        # ARM64 compatible image for Apple Silicon
        image: postgres:16-alpine
        ports: [{ containerPort: 5432 }]
        env:
        - name: POSTGRES_DB
          value: demo-db
        - name: POSTGRES_USER
          value: appuser
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: postgres-secret
              key: password
        volumeMounts:
        - name: data
          mountPath: /var/lib/postgresql/data
          subPath: postgres
        resources:
          requests: { cpu: 100m, memory: 256Mi }
          limits:   { cpu: 500m, memory: 512Mi }
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
spec:
  clusterIP: None  # Headless service for StatefulSet
  selector: { app: postgres }
  ports:
  - port: 5432
    targetPort: 5432
---
apiVersion: v1
kind: Secret
metadata:
  name: postgres-secret
type: Opaque
stringData:
  password: "dev-password-change-me"
```

Deploy it:
```bash
oc apply -f postgres-statefulset.yaml
oc get pvc,statefulset,pods -l app=postgres
```

### CRC: SQL Server Alternative

```yaml
# sql-server-statefulset.yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: sqlserver-pvc
spec:
  accessModes: [ "ReadWriteOnce" ]
  storageClassName: crc-csi-hostpath-sc
  resources:
    requests:
      storage: 20Gi  # SQL Server needs more space
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: sqlserver
spec:
  serviceName: sqlserver
  replicas: 1
  selector:
    matchLabels: { app: sqlserver }
  template:
    metadata:
      labels: { app: sqlserver }
    spec:
      containers:
      - name: sqlserver
        image: mcr.microsoft.com/mssql/server:2022-latest-ubuntu  # Supports ARM64
        ports: [{ containerPort: 1433 }]
        env:
        - name: MSSQL_SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sqlserver-secret
              key: sa-password
        - name: ACCEPT_EULA
          value: "Y"
        volumeMounts:
        - name: data
          mountPath: /var/opt/mssql
        resources:
          requests: { cpu: 500m, memory: 512Mi }
          limits:   { cpu: 2, memory: 2Gi }  # SQL Server needs more
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: sqlserver-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: sqlserver
spec:
  clusterIP: None
  selector: { app: sqlserver }
  ports:
  - port: 1433
    targetPort: 1433
---
apiVersion: v1
kind: Secret
metadata:
  name: sqlserver-secret
type: Opaque
stringData:
  sa-password: "Dev@1234Change"
```

### MicroShift: PostgreSQL (lighter footprint)

MicroShift typically doesn't have the `crc-csi-hostpath-sc` storage class. Use `local-path`:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
spec:
  accessModes: [ "ReadWriteOnce" ]
  storageClassName: local-path  # MicroShift default
  resources:
    requests:
      storage: 5Gi  # Smaller for test workloads
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels: { app: postgres }
  template:
    metadata:
      labels: { app: postgres }
    spec:
      containers:
      - name: postgres
        image: postgres:16-alpine
        ports: [{ containerPort: 5432 }]
        env:
        - name: POSTGRES_DB
          value: demo-db
        - name: POSTGRES_USER
          value: appuser
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: postgres-secret
              key: password
        volumeMounts:
        - name: data
          mountPath: /var/lib/postgresql/data
          subPath: postgres
        resources:
          requests: { cpu: 50m, memory: 128Mi }
          limits:   { cpu: 250m, memory: 256Mi }
      volumes:
      - name: data
        persistentVolumeClaim:
          claimName: postgres-pvc
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
spec:
  clusterIP: None
  selector: { app: postgres }
  ports:
  - port: 5432
    targetPort: 5432
---
apiVersion: v1
kind: Secret
metadata:
  name: postgres-secret
type: Opaque
stringData:
  password: "microshift-dev-pw"
```

Deploy:
```bash
oc apply -f postgres-statefulset.yaml
# Wait for the StatefulSet pod to be Running
oc rollout status statefulset/postgres
```

---

## Phase 7 — Deploy Aspire Services

### Translate Aspire manifests to K8s YAML

From the `aspire-manifests.json` (or workflow artifacts), manually create `api-deployment.yaml`:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: api-config
data:
  ConnectionStrings__demo-db: "Server=postgres,5432;User Id=appuser;Password=dev-password-change-me;Database=demo-db"
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: api
  labels: { app: api, version: v1 }
spec:
  replicas: 2
  selector:
    matchLabels: { app: api }
  template:
    metadata:
      labels: { app: api, version: v1 }
    spec:
      containers:
      - name: api
        image: image-registry.openshift-image-registry.svc:5000/aspire-demo/api:latest
        imagePullPolicy: Always
        ports: [{ containerPort: 8080 }]
        env:
        - name: ConnectionStrings__demo-db
          valueFrom:
            configMapKeyRef:
              name: api-config
              key: ConnectionStrings__demo-db
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        readinessProbe:
          httpGet: { path: /healthz, port: 8080 }
          initialDelaySeconds: 5
          periodSeconds: 10
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
  name: api
spec:
  selector: { app: api }
  ports:
  - name: http
    port: 80
    targetPort: 8080
---
apiVersion: route.openshift.io/v1
kind: Route
metadata:
  name: api
spec:
  to:
    kind: Service
    name: api
  port:
    targetPort: http
```

Deploy:
```bash
oc apply -f api-deployment.yaml
oc logs -f deploy/api
oc get route api
```

---

## Phase 8 — Build & Push Container Images

### Option A: OrbStack (recommended for iteration)

Build the API container:

```bash
# From the root of your Aspire project
cd AspireFullStack.ApiService

cat > Dockerfile <<'EOF'
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080
USER 1001
EXPOSE 8080
ENTRYPOINT ["dotnet", "AspireFullStack.ApiService.dll"]
EOF

docker build -t api:latest .

# Push to cluster registry
oc registry login
REGISTRY=$(oc registry info)
docker tag api:latest $REGISTRY/aspire-demo/api:latest
docker push $REGISTRY/aspire-demo/api:latest
```

### Option B: GitHub Container Registry + Actions

In your workflow, after manifest generation:

```yaml
- name: Log in to GitHub Container Registry
  uses: docker/login-action@v3
  with:
    registry: ghcr.io
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}

- name: Build and push API image
  uses: docker/build-push-action@v5
  with:
    context: ./AspireFullStack.ApiService
    push: true
    tags: ghcr.io/${{ github.repository }}/api:latest
```

Then, pull from GHCR into the cluster:

```bash
oc create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=<github-user> \
  --docker-password=<github-token> \
  --docker-email=<email>

oc secrets link default ghcr-secret --for=pull
```

Update `api-deployment.yaml` to use the GHCR image and reference the secret:

```yaml
spec:
  template:
    spec:
      imagePullSecrets:
      - name: ghcr-secret
      containers:
      - name: api
        image: ghcr.io/<you>/aspire-full-stack/api:latest
```

---

## Phase 9 — Verify Data Persistence

### Insert test data

```bash
# Port-forward to Postgres
oc port-forward svc/postgres 5432:5432 &

# Connect from your laptop
psql -h localhost -U appuser -d demo-db -W

# In psql:
CREATE TABLE items (id SERIAL PRIMARY KEY, name TEXT);
INSERT INTO items (name) VALUES ('Test Item 1');
SELECT * FROM items;
\q
```

### Call the API

```bash
API_HOST=$(oc get route api -o jsonpath='{.spec.host}')
curl https://$API_HOST/
```

### Kill the pod and restart

```bash
oc delete pod -l app=api
# New pod starts, pulls the same image
oc wait --for=condition=ready pod -l app=api --timeout=60s

# Verify data survived
oc port-forward svc/postgres 5432:5432 &
psql -h localhost -U appuser -d demo-db -W -c "SELECT * FROM items;"
```

**Expected:** Data persists. The PVC kept the database files across pod restarts.

---

## Phase 10 — Troubleshooting

### Pod won't start: ImagePullBackOff

```bash
oc describe pod -l app=api
# If image not found: push to the registry first (Phase 8)
# If auth fails: check imagePullSecrets
```

### Database connection timeout

```bash
# Check if Postgres pod is running
oc get pod postgres-0

# Verify the connection string in ConfigMap
oc get configmap api-config -o yaml

# Test connectivity from API pod
oc rsh deploy/api
# Inside pod:
curl http://postgres:5432  # Should timeout after 10s if Postgres is there
```

### StatefulSet never becomes ready

```bash
oc describe statefulset postgres
oc logs postgres-0
oc get pvc
# Check storage class exists: oc get storageclass
```

### Apple Silicon ARM64 image issues

Most images in this guide are multi-arch (`linux/amd64,linux/arm64`). If you hit an unsupported image:
- Postgres: `postgres:16-alpine` ✅ (native ARM64)
- SQL Server: `mcr.microsoft.com/mssql/server:2022-latest-ubuntu` ✅ (native ARM64)
- .NET Aspnet: `mcr.microsoft.com/dotnet/aspnet:9.0` ✅ (multi-arch)

If a Dockerfile build targets `FROM mcr.microsoft.com/dotnet/sdk:9.0`, ensure your machine's Docker/OrbStack is set to native ARM64 (not Rosetta). Verify:

```bash
docker run --rm mcr.microsoft.com/dotnet/sdk:9.0 uname -m
# Should print: aarch64 (not x86_64)
```

---

## Cluster-Specific Comparison

| Aspect | CRC | MicroShift |
|--------|-----|-----------|
| **Storage Class** | `crc-csi-hostpath-sc` | `local-path` |
| **Startup time** | ~35 min (first), ~3 min (warm) | ~20 sec |
| **RAM footprint** | 16 GB (recommended) | 4 GB |
| **Features** | Full OpenShift (RBAC, quotas, monitoring) | Lightweight K8s, no quotas |
| **Routes** | Full support (TLS, edge termination) | Supported via Ingress |
| **Console** | Full web console available | CLI-only |
| **Best for** | Full-featured testing, team demos | Solo dev, edge deployment, CI agents |

---

## Phase 10 — Cleanup

### CRC

```bash
crc stop               # Pause, keep state
crc delete             # Nuke everything
crc cleanup            # Reclaim disk
```

### MicroShift

```bash
# Stop the process (was run in background)
pkill -f microshift

# Clean state (optional)
rm -rf ~/.microshift
```

---

## 🔗 Next Steps

- **Helm Packaging:** Wrap your Aspire manifests in a Helm chart for reusability across environments.
- **Tekton Pipelines:** Add in-cluster build pipelines that compile, test, and deploy on every Git push.
- **Observability:** Deploy OpenTelemetry Collector on the cluster and point your Aspire services at it for traces.
- **Advanced Persistence:** Add Postgres replication (primary + replica) or SQL Server availability groups.
- **Multi-region:** Deploy to CRC + a cloud cluster (AKS, EKS, OCP), wire them via service mesh (Istio, Linkerd).
- **Aspire Dashboard in Cluster:** Run the Aspire dashboard as a service in K8s so your team can monitor all environments from one place.

---

## 📎 Quick Reference — Commands You'll Use Daily

### CRC Lifecycle
```bash
crc start -p ~/.crc/pull-secret.txt
crc stop
eval $(crc oc-env)
oc login -u developer -p developer https://api.crc.testing:6443
oc project aspire-demo
```

### MicroShift Lifecycle
```bash
microshift run &
export KUBECONFIG=~/.microshift/kubeconfig
oc login -u developer -p developer https://localhost:6443
oc project aspire-demo
```

### Database Operations
```bash
# Check StatefulSet
oc get statefulset postgres
oc logs postgres-0

# Port-forward for psql/sqlcmd
oc port-forward svc/postgres 5432:5432
psql -h localhost -U appuser -d demo-db -W

# Scale database (not recommended for StatefulSets, but possible)
oc scale statefulset postgres --replicas=2
```

### API Service Operations
```bash
# Deploy/update
oc apply -f api-deployment.yaml

# Check status
oc rollout status deploy/api
oc logs -f deploy/api

# Get the route
oc get route api -o jsonpath='{.spec.host}'

# Restart
oc rollout restart deploy/api
```

### Container Image Management
```bash
# Build locally
docker build -t api:v1 .

# Push to cluster registry
REGISTRY=$(oc registry info)
docker tag api:v1 $REGISTRY/aspire-demo/api:v1
docker push $REGISTRY/aspire-demo/api:v1

# Update deployment to use new image
oc set image deploy/api api=image-registry.openshift-image-registry.svc:5000/aspire-demo/api:v1
```

### Debugging
```bash
# Shell into a pod
oc rsh deploy/api

# Watch logs across replicas
stern -l app=api

# Full pod events
oc describe pod -l app=api

# Check storage
oc get pv,pvc
oc describe pvc postgres-pvc
```

---

## Appendix: Aspire Manifest → K8s Translation Reference

When you export Aspire manifests as JSON, they contain:
- **Services** (with resource limits, ports, env vars, probes)
- **Databases** (connection strings, storage volumes, init scripts)
- **Networks** (how services reference each other)

**Translation checklist:**

| Aspire | → | K8s |
|--------|---|-----|
| Service | Deployment + Service | Pod replica management + ClusterIP |
| Database StatefulSet | StatefulSet + PVC | Ordered, stable network identity + persistent storage |
| ConnectionString secret | Secret | Mounted as env vars or files |
| HealthChecks endpoint | readinessProbe + livenessProbe | Kubernetes pod health observation |
| Resource limits (CPU/RAM) | resources.limits | Pod scheduling + eviction |
| Image | container.image | OCI image pulled on pod start |

---

## 🤔 Common Questions

**Q: Can I use the same manifests on both CRC and MicroShift?**  
A: Mostly yes. Differences: storage class name (`crc-csi-hostpath-sc` vs. `local-path`), and MicroShift has no RBAC/quotas by default. Swap the StorageClass and both work.

**Q: Should I use SQL Server or Postgres?**  
A: Postgres is lighter (128 MB RAM at rest vs. 512 MB for SQL Server). Both support ARM64. Use SQL Server if you already know T-SQL or need SQL Server-specific features (always-on, replication). Otherwise, Postgres is simpler.

**Q: How do I do blue-green deploys?**  
A: Create two `Deployment` objects (`api-v1`, `api-v2`), then switch the `Service` selector between them. Or use Argo CD with GitOps workflows.

**Q: My pod won't pull the image. What's wrong?**  
A: Check: (1) image exists in the registry, (2) registry is reachable from the cluster, (3) imagePullSecrets are set if using private registries, (4) image architecture matches the node (`amd64` vs `arm64`).

**Q: Can I use Aspire Starter instead of Aspire Full Stack?**  
A: Yes, just skip the web project. Aspire Starter is smaller but has the same AppHost + service scaffold.

---

**Tutorial by:** Built for .NET 9.0 + Aspire + Kubernetes (CRC & MicroShift)  
**Last updated:** 2026-04-19  
**Apple Silicon tested:** ✅ M1/M2/M3 native ARM64
