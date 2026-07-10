---
title: Harness Deployment Without Health Checks - Kubernetes Jobs
created: 2026-04-06
modified: 2026-04-06
tags:
  - harness
  - kubernetes
  - openshift
  - ci-cd
  - batch-jobs
  - etl
uid: 297713a6-8bf7-4543-acdc-02accba9c900
---

# Harness Deployment Without Health Checks: Kubernetes Jobs Tutorial

## Problem Statement

When deploying a container that runs a CLI tool for a short duration (batch job, ETL, etc.), Harness requires health checks and readiness checks to verify deployment success. However, **batch jobs don't have persistent endpoints** to check—they run, complete their work, and exit.

This tutorial explains how to configure Harness to deploy Kubernetes **Jobs** instead of **Deployments**, which eliminates the need for health check endpoints entirely.

---

## Part 1: Understanding Kubernetes Workload Types

### **Deployment**
- **Purpose**: Long-lived, persistent applications
- **Pod lifetime**: Pods run indefinitely
- **Restart policy**: Automatically recreates failed pods
- **Use case**: Web servers, APIs, microservices
- **Health checks**: ✅ **REQUIRED** (liveness & readiness probes)
- **Success criteria**: Pod stays running

**Example: Web Server**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: my-api
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: app
        livenessProbe:      # Is the app still alive?
          httpGet:
            path: /health
            port: 8080
        readinessProbe:     # Is the app ready for traffic?
          httpGet:
            path: /ready
            port: 8080
```

---

### **Job** (For Your Use Case)
- **Purpose**: Run a task to completion, then stop
- **Pod lifetime**: Pod exits when work is done (exit code 0 = success)
- **Restart policy**: Retries on failure (configurable), but not indefinite
- **Use case**: Batch processing, ETL, data migrations, backups, one-time tasks
- **Health checks**: ❌ **NOT REQUIRED** (Job monitors exit code instead)
- **Success criteria**: Exit code 0

**Example: ETL Job**
```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: etl-transform
spec:
  backoffLimit: 3  # Retry 3 times if it fails
  template:
    spec:
      containers:
      - name: etl
        image: my-etl:latest
        command: ["./MyEtlTool"]
      restartPolicy: Never  # Don't restart on exit
```

When the container exits, the Job is marked complete (success or failed).

---

### **CronJob** (For Scheduled Jobs)
- **Purpose**: Run a Job on a schedule
- **Pod lifetime**: Same as Job (exits when done)
- **Restart policy**: Creates a new Job at specified times
- **Use case**: Scheduled ETL, scheduled reports, periodic cleanup
- **Health checks**: ❌ **NOT REQUIRED**
- **Schedule**: Cron syntax (e.g., `0 2 * * *` = daily at 2 AM)

**Example: Nightly ETL**
```yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: nightly-etl
spec:
  schedule: "0 2 * * *"  # Every night at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: etl
            image: my-etl:latest
            command: ["./MyEtlTool"]
          restartPolicy: Never
```

---

## Part 2: Comparison Table

| Aspect | Deployment | Job | CronJob |
|--------|-----------|-----|---------|
| Container exits normally? | ❌ (pod restarts) | ✅ (expected) | ✅ (expected) |
| Health checks required? | ✅ YES | ❌ **NO** | ❌ **NO** |
| Harness waits for completion? | ❌ (pod always running) | ✅ (waits for exit) | ✅ (schedules jobs) |
| Use case | Long-lived services | Batch/ETL tasks | Scheduled batch tasks |
| Health check method | HTTP/TCP probe | Exit code monitoring | Exit code monitoring |

---

## Part 3: How Harness Monitors Jobs (No Health Checks Needed)

When you configure a Kubernetes Job in Harness with `skipSteadyStateCheck: false`, Harness automatically monitors:

1. **Kubernetes Job Status** - Watches the Job's `.status` field
2. **Events** - Monitors Kubernetes events for progress
3. **Exit Code** - Considers exit code 0 as success, non-zero as failure
4. **Completion Time** - Waits for `.status.completionTime` to be set

**Under the hood, Harness runs:**
```bash
kubectl get events --namespace=default --watch-only
kubectl get jobs etl-transform --namespace=default --output=jsonpath='{.status}'
```

**Desired final state:**
```json
{
  "conditions": [{"type": "Complete", "status": "True"}],
  "succeeded": 1,
  "completionTime": "2026-04-06T14:32:00Z"
}
```

**No HTTP endpoint needed—it's purely job status monitoring.**

---

## Part 4: Setting Up Harness for Kubernetes Jobs

### Step 1: Create the Job Manifest

Create a file named `etl-job.yaml` in your repository:

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: etl-transform-<+execution.correlationId>
  namespace: <+infra.namespace>
  labels:
    app: etl-transform
    environment: <+env.name>
spec:
  backoffLimit: 2  # Retry 2 times on failure
  ttlSecondsAfterFinished: 3600  # Clean up job after 1 hour
  template:
    metadata:
      labels:
        app: etl-transform
    spec:
      containers:
      - name: etl-tool
        image: <+artifacts.primary.image>:<+artifacts.primary.tag>
        imagePullPolicy: Always
        env:
        - name: ENVIRONMENT
          value: <+env.name>
        - name: LOG_LEVEL
          value: INFO
        # Add any other environment variables your ETL needs here
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
      restartPolicy: Never  # Important: don't restart the container
      serviceAccountName: etl-sa  # Optional: create if you need specific RBAC
```

**Key Explanations:**
- **`<+execution.correlationId>`**: Unique ID per deployment (avoid name collisions)
- **`backoffLimit: 2`**: Job retries up to 2 times before failing
- **`ttlSecondsAfterFinished`**: Automatically deletes the Job resource after completion
- **`restartPolicy: Never`**: Pod exits cleanly; Kubernetes doesn't restart it
- **`serviceAccountName`**: Optional RBAC service account if your ETL needs cluster permissions

---

### Step 2: Configure Harness Service Definition

In Harness, navigate to **Services** and create a new Kubernetes service.

**Service Configuration:**
```yaml
service:
  name: etl-transform
  identifier: etl_transform
  serviceDefinition:
    type: Kubernetes
    spec:
      manifests:
        - manifest:
            identifier: etl-job
            type: K8sManifest
            spec:
              store:
                type: Github  # or Harness, GitLab, Bitbucket, etc.
                spec:
                  connectorRef: account.your-github-connector
                  gitFetchType: Branch
                  paths:
                    - manifests/etl-job.yaml
                  repoName: your-repo
                  branch: main
              skipResourceVersioning: false
```

**Manifest Store Options:**
- **Harness**: Store YAML files directly in Harness
- **Github**: Reference files from your GitHub repo
- **GitLab**: Reference files from your GitLab repo
- **Bitbucket**: Reference files from your Bitbucket repo
- **Local**: For testing, include the YAML in your pipeline

---

### Step 3: Configure the Harness Pipeline Deployment Step

In your **Deployment** stage, add the **K8sApply** step:

```yaml
steps:
  - step:
      type: K8sApply
      name: Deploy ETL Job
      identifier: deploy_etl_job
      spec:
        filePaths:
          - manifests/etl-job.yaml
        skipDryRun: false  # Validate manifest first
        skipSteadyStateCheck: false  # Wait for job completion
      timeout: 30m  # Adjust based on your ETL runtime (max expected duration)
      failureStrategies:
        - onFailure:
            errors:
              - AllErrors
            action:
              type: Retry
              spec:
                retryCount: 1
                retryIntervals:
                  - 5m
```

**Parameter Explanation:**
- **`filePaths`**: Path to your Job manifest
- **`skipDryRun: false`**: Harness validates the manifest before applying
- **`skipSteadyStateCheck: false`**: ✅ **CRITICAL** - Tells Harness to monitor the Job until completion
- **`timeout: 30m`**: How long Harness will wait for the Job to complete (adjust to your ETL's max runtime)

---

### Step 4: Configure Environment & Infrastructure

Set up your **Environment** and **Infrastructure Definition** to point to your OpenShift cluster:

**Environment Configuration:**
```yaml
environment:
  name: Dev
  identifier: dev
  type: Non-Production
  deploymentType: Kubernetes
```

**Infrastructure Definition:**
```yaml
infrastructureDefinition:
  name: OpenShift Dev
  identifier: openshift_dev
  type: KubernetesCluster
  spec:
    connectorRef: account.your-openshift-connector
    namespace: default  # Or your target namespace
```

Ensure your Harness **Kubernetes Connector** points to your OpenShift cluster.

---

## Part 5: Complete Pipeline Example

Here's a full Harness pipeline deploying a Kubernetes Job:

```yaml
pipeline:
  name: Deploy ETL Job
  identifier: deploy_etl_job_pipeline
  projectIdentifier: Default
  orgIdentifier: default
  tags: {}
  stages:
    - stage:
        name: Deploy
        identifier: deploy_stage
        type: Deployment
        spec:
          deploymentType: Kubernetes
          service:
            serviceRef: etl_transform
            serviceInputs:
              serviceDefinition:
                type: Kubernetes
                spec:
                  artifacts:
                    primary:
                      primaryArtifactRef: <+input>
                      sources: <+input>
          environment:
            environmentRef: dev
            deployToAll: false
            infrastructureDefinitions:
              - identifier: openshift_dev
          execution:
            steps:
              - step:
                  name: Deploy ETL Job
                  identifier: deploy_etl_job
                  type: K8sApply
                  spec:
                    filePaths:
                      - manifests/etl-job.yaml
                    skipDryRun: false
                    skipSteadyStateCheck: false
                  timeout: 30m
                  failureStrategies:
                    - onFailure:
                        errors:
                          - AllErrors
                        action:
                          type: Retry
                          spec:
                            retryCount: 1
            rollbackSteps: []
          failureStrategies:
            - onFailure:
                errors:
                  - AllErrors
                action:
                  type: StageRollback
```

---

## Part 6: Persistent Debug Version (For Development)

To keep the container running indefinitely for debugging in your dev OpenShift environment, convert it to a **Deployment** with a loop:

Create `etl-debug-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: etl-debug
  namespace: default
  labels:
    app: etl-debug
    environment: dev
spec:
  replicas: 1
  selector:
    matchLabels:
      app: etl-debug
  template:
    metadata:
      labels:
        app: etl-debug
    spec:
      containers:
      - name: etl-tool
        image: your-etl-image:latest
        imagePullPolicy: Always
        command: ["/bin/sh"]
        args:
          - -c
          - |
            while true; do
              echo "=== Running ETL at $(date) ==="
              /app/MyEtlTool
              EXIT_CODE=$?
              echo "=== ETL exited with code $EXIT_CODE at $(date) ==="
              echo "Sleeping 300 seconds before next run..."
              sleep 300
            done
        env:
        - name: ENVIRONMENT
          value: dev
        - name: LOG_LEVEL
          value: DEBUG
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "2Gi"
            cpu: "1000m"
        # Health check for Kubernetes (required for Deployment)
        livenessProbe:
          exec:
            command:
            - /bin/sh
            - -c
            - |
              # Simple check: if process is still running, pod is alive
              ps aux | grep -v grep | grep MyEtlTool > /dev/null || exit 0
          initialDelaySeconds: 30
          periodSeconds: 60
          failureThreshold: 3
```

**Deploy to OpenShift:**
```bash
# Using kubectl
kubectl apply -f etl-debug-deployment.yaml

# Using oc (OpenShift CLI)
oc apply -f etl-debug-deployment.yaml
```

**Interact with the Pod:**
```bash
# View logs
kubectl logs -f deployment/etl-debug

# SSH into the pod
kubectl exec -it <pod-name> -- /bin/sh

# Watch pod status
kubectl get pods -w -l app=etl-debug

# Describe pod for troubleshooting
kubectl describe pod <pod-name>
```

---

## Part 7: Troubleshooting

### Job Status Commands

```bash
# Get job status
kubectl get jobs -n default
kubectl get job etl-transform-abc123 -n default -o yaml

# View job events
kubectl describe job etl-transform-abc123 -n default

# View pod logs from the job
kubectl logs -n default <pod-name>

# Check if job succeeded
kubectl get job etl-transform-abc123 -n default -o jsonpath='{.status.succeeded}'
```

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Job hangs indefinitely | `skipSteadyStateCheck: true` | Set to `false` in K8sApply step |
| Pod keeps restarting | `restartPolicy: Always` | Set to `Never` in Job spec |
| Harness timeout | Job takes longer than timeout | Increase `timeout` in K8sApply step |
| Job fails with exit code 1 | Application error | Check logs: `kubectl logs <pod-name>` |
| Pod can't pull image | Bad image reference | Verify image exists in registry |

---

## Part 8: Summary Checklist

Before deploying, ensure you have:

- [ ] **Job Manifest** (`etl-job.yaml`)
  - [ ] `restartPolicy: Never`
  - [ ] No `livenessProbe` or `readinessProbe`
  - [ ] `backoffLimit` set appropriately
  - [ ] Container exits with code 0 on success

- [ ] **Harness Service Definition**
  - [ ] Configured to point to your manifest
  - [ ] Store connector (GitHub, GitLab, etc.) is working

- [ ] **Harness Pipeline**
  - [ ] **K8sApply** step configured
  - [ ] `skipSteadyStateCheck: false`
  - [ ] `timeout` is longer than your max ETL runtime

- [ ] **Environment & Infrastructure**
  - [ ] Kubernetes connector points to OpenShift
  - [ ] Namespace is correct
  - [ ] Service account has necessary RBAC (if needed)

---

## Part 9: Key Takeaways

1. **Jobs are for batch work** - Use Job for CLI tools, ETL, migrations; use Deployment for web services
2. **No health checks needed** - Jobs succeed/fail based on exit code, not HTTP probes
3. **Harness monitors automatically** - Set `skipSteadyStateCheck: false` and Harness handles the rest
4. **Timeout is critical** - Set it longer than your expected job duration
5. **For debugging** - Use a Deployment with a loop to keep the container running

---

## Additional Resources

- [Kubernetes Job Documentation](https://kubernetes.io/docs/concepts/workloads/controllers/job/)
- [Kubernetes CronJob Documentation](https://kubernetes.io/docs/concepts/workloads/controllers/cron-jobs/)
- [Harness Kubernetes Job Deployment](https://developer.harness.io/docs/continuous-delivery/deploy-srv-diff-platforms/kubernetes/kubernetes-executions/run-kubernetes-jobs)
- [Harness K8sApply Step Reference](https://developer.harness.io/docs/continuous-delivery/deploy-srv-diff-platforms/kubernetes/kubernetes-executions/deploy-manifests-using-apply-step)
- [OpenShift with Harness](https://developer.harness.io/docs/continuous-delivery/deploy-srv-diff-platforms/kubernetes/cd-k8s-ref/using-open-shift-with-harness-kubernetes)
