---
created: 2026-05-08T21:05:00+21:05
modified: 2026-05-08T21:05:17+21:05
uid: 65835158-e261-4046-a57f-7f25587612bd
---
# 🤖❓Tutorial on interrogating OpenShift with oc utility
- I am a `c#` developer who is just coming up to speed with OpenShift
- Please write me a detailed tutorial on commands that I can run with the `oc` utility to interrogate the state and configuration of my cluster
- I get the "gist" of Kubernetes / OpenShift - but I am still very new to it and would like to be able to develop a mental map of the configuration of my teams cluster
  by navigating it with the `oc` utility


> [!ai] Chat-GPT

# 🤖 OpenShift Interrogation Guide for C# Developers

Welcome! I'll guide you through exploring your OpenShift cluster using `oc`. Think of it like using Visual Studio's debugger or Fiddler—we're just inspecting running systems.

---

## 📋 Table of Contents
1. [Basics & Context](<#basics--context>)
2. [Exploring the Cluster Structure](<#exploring-the-cluster-structure>)
3. [Investigating Applications](<#investigating-applications>)
4. [Debugging & Troubleshooting](<#debugging--troubleshooting>)
5. [Configuration Deep Dives](<#configuration-deep-dives>)
6. [Practical Scenarios](<#practical-scenarios>)

---

## Basics & Context

### First: Know Where You Are

```bash
# Check your current context (which cluster you're connected to)
oc context

# List all configured contexts
oc config get-contexts

# Switch to a different context
oc config use-context <context-name>

# Get cluster info
oc cluster-info
```

**C# Developer Analogy**: Think of `context` like your Visual Studio project configuration—you can have Debug, Release, different machines, etc.

### Your Current Namespace

```bash
# See which namespace you're currently in
oc project

# List all namespaces in the cluster
oc get namespaces

# Switch to a different namespace
oc project <namespace-name>

# Quick namespace check
oc config current-context  # Shows context@namespace
```

**Key Concept**: Namespaces are like organizational folders. Your team probably works in one, maybe `default`, `development`, `production`, etc.

---

## Exploring the Cluster Structure

### 1. The Big Picture: What's Running?

```bash
# List all resource types available in your cluster
oc api-resources

# Get a summary of everything in your current namespace
oc get all
```

**Output Explained**:
```
NAME                                    READY   STATUS    RESTARTS   AGE
pod/myapp-deployment-7d4f5c6b9f-abc12   1/1     Running   0          2d

NAME                    TYPE        CLUSTER-IP      EXTERNAL-IP   PORT(S)
service/myapp-service   ClusterIP   172.30.100.50   <none>        8080/TCP

NAME                                      READY   UP-TO-DATE   AVAILABLE   AGE
deployment.apps/myapp-deployment          1/1     1            1           2d

NAME                                                DESIRED   CURRENT   READY   AGE
replicaset.apps/myapp-deployment-7d4f5c6b9f        1         1         1       2d
```

This is like looking at your running IIS instances, process monitor, and service status all at once.

### 2. Deep Dive: Pods (Your Running Containers)

```bash
# List all pods in current namespace
oc get pods

# More detailed pod information
oc get pods -o wide
# Shows: node assignment, internal IPs, restart counts

# Get extremely detailed info about a specific pod
oc describe pod <pod-name>

# Get the YAML/JSON definition of a pod
oc get pod <pod-name> -o yaml
oc get pod <pod-name> -o json

# List pods with custom columns
oc get pods -o custom-columns=NAME:.metadata.name,STATUS:.status.phase,NODE:.spec.nodeName

# List pods across ALL namespaces
oc get pods --all-namespaces
oc get pods -A  # Short form
```

**C# Analogy**: Pods are like running processes in your app. If you have a deployment with 3 replicas, you have 3 pods.

### 3. Understanding Deployments (Your Application Blueprint)

```bash
# List deployments
oc get deployments

# Detailed deployment info
oc describe deployment <deployment-name>

# See deployment YAML (this is YOUR APPLICATION DEFINITION)
oc get deployment <deployment-name> -o yaml

# Check rollout history
oc rollout history deployment/<deployment-name>

# See detailed revision info
oc rollout history deployment/<deployment-name> --revision=2
```

**Key Insight**: The deployment YAML defines:
- How many replicas (like setting IIS app pool recycling/instances)
- Which container image to use
- Resource requests/limits (like memory/CPU quotas)
- Environment variables
- Volumes/storage

### 4. Services (Your Load Balancers)

```bash
# List services
oc get services
oc get svc  # Short form

# Get service details
oc describe service <service-name>

# View service YAML
oc get service <service-name> -o yaml

# Check which pods a service targets
oc get endpoints <service-name>
```

**C# Developer Note**: Services are like your IIS URL Rewrite rules or an API Gateway—they route traffic to your pods. There are different types:

```yaml
# ClusterIP - internal only (default)
# NodePort - exposes on all nodes on a specific port
# LoadBalancer - uses cloud provider's load balancer
# ExternalName - DNS alias
```

### 5. Routes (OpenShift-Specific Web Access)

```bash
# List routes (OpenShift's way of exposing apps to the internet)
oc get routes

# Get route details
oc describe route <route-name>

# View route YAML
oc get route <route-name> -o yaml

# Find the URL for your app
oc get routes --no-headers | awk '{print $2}'
```

**C# Analogy**: Routes are like your domain/certificate configuration in IIS. They map public URLs to internal services.

---

## Investigating Applications

### See Your Application's Full Stack

```bash
# Imagine your app is called "myapp"

# 1. Find the deployment
oc get deployments | grep myapp
# Output: myapp-deployment

# 2. See what it's supposed to run
oc describe deployment myapp-deployment

# 3. Check if pods are actually running
oc get pods | grep myapp
# Output: myapp-deployment-7d4f5c6b9f-abc12, etc.

# 4. See the service (how traffic gets to it)
oc get services | grep myapp
# Output: myapp-service

# 5. See the route (the public URL)
oc get routes | grep myapp
# Output: myapp-route

# 6. Get the actual hostname
oc get route myapp-route -o jsonpath='{.spec.host}'
# Output: myapp-myproject.apps.example.com
```

### Container Images

```bash
# See what image your deployment is running
oc get deployment myapp-deployment -o jsonpath='{.spec.template.spec.containers[0].image}'
# Output: docker.io/myregistry/myapp:v2.1.0

# View all containers in a pod
oc get pod <pod-name> -o jsonpath='{.spec.containers[*].image}'
```

---

## Debugging & Troubleshooting

### 1. Pod Logs (Like Application Event Viewer)

```bash
# Get logs from a pod
oc logs <pod-name>

# Last 50 lines
oc logs <pod-name> --tail=50

# Follow logs in real-time (like tail -f)
oc logs <pod-name> -f

# Logs from a specific container (if pod has multiple)
oc logs <pod-name> -c <container-name>

# Logs from all pods in a deployment
oc logs -f deployment/<deployment-name>

# Previous logs (if pod crashed and restarted)
oc logs <pod-name> --previous

# Logs with timestamps
oc logs <pod-name> --timestamps=true
```

**Pro Tip**: If your C# app isn't starting, logs are your first stop:
```bash
oc logs -f deployment/myapp-deployment
# Watch for startup errors
```

### 2. Exec into a Container (Like Remote Desktop into a Pod)

```bash
# Open a shell in a running container
oc exec -it <pod-name> -- /bin/bash
# or for Alpine: /bin/sh

# Run a single command
oc exec <pod-name> -- dotnet --info

# If pod has multiple containers, specify which:
oc exec -it <pod-name> -c <container-name> -- /bin/bash

# Check environment variables
oc exec <pod-name> -- env | sort

# Check what's listening on ports
oc exec <pod-name> -- netstat -tuln
```

### 3. Describe (Your Debugging Dashboard)

```bash
# See everything about a pod
oc describe pod <pod-name>

# This shows:
# - Current state
# - Resources requested/limits
# - Recent events (CrashLoopBackOff? Image pull failed? etc.)
# - Environment variables
# - Mounted volumes
# - IP address
```

**Example output to interpret**:
```
Events:
  Type     Reason            Age    Message
  ----     ------            ----   -------
  Normal   Scheduled         5m     Successfully assigned pod to node
  Normal   Pulling           5m     Pulling image "myimage:v1"
  Normal   Pulled            4m     Successfully pulled image
  Normal   Created           4m     Created container
  Normal   Started           4m     Started container
  Warning  Unhealthy         2m     Liveness probe failed: HTTP probe failed
  Warning  BackOff           1m     Back-off restarting failed container
```

### 4. Event Debugging

```bash
# See cluster events (like Windows Event Viewer for your cluster)
oc get events

# For a specific namespace
oc get events --namespace=<namespace>

# Watch events live
oc get events -w

# Events for a specific resource
oc describe pod <pod-name>  # includes events at bottom
```

---

## Configuration Deep Dives

### 1. Environment Variables

```bash
# See environment variables passed to your app
oc get deployment <deployment-name> -o jsonpath='{.spec.template.spec.containers[0].env}'

# More readable format
oc get deployment <deployment-name> -o yaml | grep -A 20 "env:"

# Check if using ConfigMaps
oc get configmaps

# View a ConfigMap
oc get configmap <configmap-name> -o yaml

# Check if using Secrets
oc get secrets

# View a Secret (values are base64 encoded)
oc get secret <secret-name> -o yaml
```

### 2. Resource Limits & Requests

```bash
# See CPU/Memory requests and limits
oc get pods -o custom-columns=NAME:.metadata.name,CPU_REQ:.spec.containers[0].resources.requests.cpu,MEM_REQ:.spec.containers[0].resources.requests.memory,CPU_LIM:.spec.containers[0].resources.limits.cpu,MEM_LIM:.spec.containers[0].resources.limits.memory

# Or just describe the deployment
oc describe deployment <deployment-name> | grep -A 5 "Limits\|Requests"
```

**C# Context**: Like setting max memory for your app pool—prevents runaway apps from crashing nodes.

### 3. Node Information

```bash
# List nodes (the VMs running your containers)
oc get nodes

# Detailed node info
oc describe node <node-name>

# Node resource usage
oc top nodes

# Pods on a specific node
oc get pods --field-selector spec.nodeName=<node-name> -A
```

### 4. Storage & Volumes

```bash
# List persistent volumes (storage)
oc get pv

# List persistent volume claims (what apps ask for)
oc get pvc

# Details about storage
oc describe pvc <pvc-name>

# Check what storage classes are available
oc get storageclass
```

---

## Practical Scenarios

### Scenario 1: "My App Isn't Working. Where Do I Start?"

```bash
# 1. Check the route exists
oc get routes
# Note the URL

# 2. Check if pods are running
oc get pods
# Status should be "Running", not "CrashLoopBackOff" or "Pending"

# 3. If not running, describe to see why
oc describe pod <pod-name>
# Look at "Events" section at the bottom

# 4. Check logs
oc logs -f deployment/<deployment-name>
# Look for startup errors (exception stack traces, port already in use, etc.)

# 5. If using environment variables, check they're set
oc exec <pod-name> -- env | grep -i MY_VAR

# 6. Check service is routing correctly
oc get endpoints <service-name>
# Should show IPs matching your pods

# 7. Try to hit it internally
oc exec <pod-name> -- curl http://myapp-service:8080/health
```

### Scenario 2: "How Do I Know What Version Is Running?"

```bash
# 1. See what image is deployed
oc get deployment myapp -o jsonpath='{.spec.template.spec.containers[0].image}'
# Output: docker.io/myregistry/myapp:v2.1.0

# 2. Check pod label (sometimes version is in labels)
oc get pods --show-labels | grep myapp

# 3. Check if deployment has recent changes
oc rollout history deployment/myapp

# 4. See detailed pod template
oc get deployment myapp -o yaml | grep -A 30 "spec:"
```

### Scenario 3: "How Many Replicas Do I Have? Are They Healthy?"

```bash
# Simple view
oc get deployments
# Output columns: NAME, READY, UP-TO-DATE, AVAILABLE

# Pod-level view
oc get pods

# Detailed
oc describe deployment myapp
# Shows "Replicas: 3 desired | 3 updated | 3 total | 3 available"

# Watch scaling in real-time
oc get pods -w
```

### Scenario 4: "Check My ConfigMap (App Settings)"

```bash
# List all configs
oc get configmaps

# View specific config
oc get configmap appsettings -o yaml
# Or
oc describe configmap appsettings

# See if it's mounted in your pod
oc get pod <pod-name> -o yaml | grep -A 5 "configMap"

# Verify the app sees it
oc exec <pod-name> -- cat /etc/config/appsettings.json
```

### Scenario 5: "Debug Network Connectivity"

```bash
# Check DNS resolution
oc exec <pod-name> -- nslookup myapp-service
oc exec <pod-name> -- nslookup myapp-service.default.svc.cluster.local

# Check if service port is accessible
oc exec <pod-name> -- curl http://myapp-service:8080/

# Check service endpoints
oc get endpoints myapp-service

# Check routes
oc get routes
oc get route myapp-route -o yaml
```

### Scenario 6: "My App Needs More Replicas"

```bash
# Current state
oc get deployments

# Scale up
oc scale deployment myapp --replicas=5

# Watch it scale
oc get pods -w

# Verify
oc get deployment myapp
# Should now show "5 available"
```

---

## 🎯 Mental Model Checklist

Use this to build your mental map:

```
CLUSTER
  └─ NODES (VMs)
      └─ PODS (Containers)
          ├─ Deployment (blueprint for pods)
          ├─ Service (internal networking)
          ├─ Route (external networking)
          ├─ ConfigMap (settings)
          ├─ Secret (passwords)
          └─ Storage (volumes)
```

**Key Commands by Use Case**:

| Goal | Command |
|------|---------|
| See what's running | `oc get all` |
| Troubleshoot app | `oc logs -f deployment/APP` |
| Check configuration | `oc get deployment APP -o yaml` |
| Debug inside container | `oc exec -it POD -- /bin/bash` |
| See recent errors | `oc describe pod POD` |
| Check networking | `oc get services,routes` |
| Monitor resources | `oc top pods` |

---

## 📚 Advanced Inspection Commands

```bash
# JSONPath queries (like JQ for structured data)
oc get pods -o jsonpath='{.items[*].metadata.name}'

# Custom output formats
oc get pods -o wide
oc get pods -o json | jq '.'

# Watch for changes
oc get pods -w

# Sort results
oc get pods --sort-by=.metadata.creationTimestamp

# Filter by labels
oc get pods -l app=myapp

# Filter by field
oc get pods --field-selector=status.phase=Running
```

---

## 🔗 Next Steps

Once comfortable with these commands:
1. Learn to read YAML (that's where the real config lives)
2. Understand Operators & CustomResourceDefinitions (CRDs)
3. Explore Helm (package manager for Kubernetes)
4. Learn to write your own deployment YAML files

Good luck! The `oc` utility is your friend. 🚀
