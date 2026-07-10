---
uid: 01KV13JNRJ3PA0CHF99SF2AZP5
title: 🤖❓How do I delete a service in OpenShift
created: 2026-06-13T11:23:17-07:00
modified: 2026-06-13T11:24:03-07:00
tags:
  - para/resources/open-shift
---

#para/resources/open-shift 

***

# 🤖❓How do I delete a service in OpenShift

## 🤖💡To delete a Service in OpenShift

```bash
oc delete svc <service-name>
```

Example:

```bash
oc delete svc orders-api
```

or equivalently:

```bash
oc delete service orders-api
```

---

### First, see what services exist

```bash
oc get svc
```

Example:

```
NAME          TYPE        CLUSTER-IP
orders-api    ClusterIP   172.30.12.45
postgres      ClusterIP   172.30.88.91
```

Then:

```bash
oc delete svc orders-api
```

---

### Delete multiple services

```bash
oc delete svc svc1 svc2 svc3
```

Or all services in a namespace:

```bash
oc delete svc --all
```

Be careful with that one.

---

### Delete a Service in another namespace

```bash
oc delete svc orders-api -n myproject
```

---

### See what created the Service first

In many environments, the Service is managed by a Deployment, Helm chart, Operator, Argo CD application, or GitOps pipeline. If you manually delete it, it may immediately come back.

Check ownership:

```bash
oc get svc orders-api -o yaml
```

Look for:

```yaml
ownerReferences:
```

or labels like:

```yaml
app.kubernetes.io/managed-by: Helm
argocd.argoproj.io/instance: ...
```

If those exist, the Service is probably being reconciled by another controller.

---

### Dry-run what would be deleted

```bash
oc delete svc orders-api --dry-run=client
```

---

### Verify deletion

```bash
oc get svc
```

or:

```bash
oc get svc orders-api
```

You should see:

```
Error from server (NotFound): services "orders-api" not found
```

## 🤖💬 Deleting a service is often not the right thing to remove the workload
Tell me what kind of app it is 
If you're trying to clean up an application, tell me whether it's a Deployment, DeploymentConfig, Helm release, Operator-managed app, or Argo CD app, because deleting the Service alone is often not the right thing to remove the workload.

### 👤💬 I'm not sure how to answer that question. Could you give me some more details and context?

### 🤖💡 Great question — let me explain why I asked, and how you can figure out the answer yourself

#### Why this matters

In OpenShift, a **Service** is rarely a standalone thing. It's usually *created and managed by something else*. If you delete just the Service but leave the manager running, the manager will often **recreate the Service automatically** within seconds — making your deletion look like it "didn't work."

So before deleting, it helps to know **who owns the Service**.

---

#### The common "owners" of a Service

Here are the main things that create/manage Services in OpenShift, and how to recognize them:

##### 1. **Deployment** (standard Kubernetes)
The most common modern workload type.

```bash
oc get deployments
```

If you see your app listed here, it's probably a Deployment.

##### 2. **DeploymentConfig** (older, OpenShift-specific)
Legacy OpenShift workload type. Still common in older clusters.

```bash
oc get deploymentconfigs
```

or the short form:

```bash
oc get dc
```

##### 3. **Helm release**
A package manager for Kubernetes apps.

```bash
oc get all -l app.kubernetes.io/managed-by=Helm
```

or list Helm releases directly (if `helm` is installed):

```bash
helm list
```

##### 4. **Operator-managed**
An Operator is a controller that manages custom resources.

```bash
oc get csv          # ClusterServiceVersions = installed operators
```

##### 5. **Argo CD / GitOps**
If your org uses GitOps, Argo CD continuously syncs the cluster to match Git.

```bash
oc get applications -A
```

---

#### The easiest way to just *find out*

Instead of guessing, **inspect the Service itself** and let it tell you:

```bash
oc get svc <service-name> -o yaml
```

Look for two things:

**A. `ownerReferences`** (near the top, under `metadata`):

```yaml
metadata:
  ownerReferences:
  - apiVersion: apps/v1
    kind: Deployment       # ← this tells you the owner
    name: orders-api
```

**B. Labels** that hint at the manager:

```yaml
metadata:
  labels:
    app.kubernetes.io/managed-by: Helm
    argocd.argoproj.io/instance: orders-app
```

---

#### Quick decision guide

| What you see | What it means | How to properly remove the app |
|---|---|---|
| `kind: Deployment` in ownerReferences | Standard Deployment | `oc delete deployment <name>` |
| `kind: DeploymentConfig` | Legacy OpenShift | `oc delete dc <name>` |
| `managed-by: Helm` label | Helm release | `helm uninstall <release>` |
| `argocd.argoproj.io/...` label | Argo CD app | Delete in Argo CD / remove from Git |
| An Operator CRD owner | Operator-managed | Delete the custom resource, not the Service |
| **Nothing** (no owner, no labels) | Manually created | `oc delete svc <name>` is safe |

---

#### What I'd suggest right now

Run this and paste me the output (you can redact names):

```bash
oc get svc <service-name> -o yaml
```

Then I can tell you:
1. **Who owns the Service**
2. **Whether deleting it alone will work** or just bounce back
3. **The correct command** to remove the whole app cleanly

If the `ownerReferences` and management labels are both empty, then good news — it's standalone, and a simple `oc delete svc <name>` is exactly the right move. 👍
