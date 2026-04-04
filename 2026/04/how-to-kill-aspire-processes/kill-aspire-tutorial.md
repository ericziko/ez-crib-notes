---
title: How to Kill .NET Aspire App Host Processes
created: 2026-04-04T00:00:00Z
modified: 2026-04-04T00:00:00Z
tags:
  - dotnet
  - aspire
  - powershell
  - devops
---

# 🤖❓ How to Kill .NET Aspire App Host Processes

## 🤖💡 The Problem

When you run a `.NET Aspire` app host, it spawns a tree of processes:

```
dotnet (AppHost)
  ├── dotnet (your API project)
  ├── dotnet (your worker service)
  └── ... (other services)
```

The dashboard, OTLP collector, and reverse proxy all run as part of this tree. Simply `Ctrl+C`-ing doesn't always clean up reliably — especially if launched from an IDE. You need a reliable way to find and kill the right process(es).

---

## 🤖💡 How to Identify the Aspire App Host Process

### 🔍 The Key Signal: Command Line Contains `AppHost`

The Aspire app host is a `dotnet` process whose command line references a DLL matching the pattern `*AppHost*.dll`. For example:

```
dotnet /home/user/repos/MyApp/src/MyApp.AppHost/bin/Debug/net9.0/MyApp.AppHost.dll
```

or when running via `dotnet run`:

```
dotnet run --project /home/user/repos/MyApp/src/MyApp.AppHost
```

### 🔍 Secondary Signals

| Signal | Details |
|--------|---------|
| **Dashboard port** | Aspire dashboard defaults to `http://localhost:18888` |
| **OTLP endpoint** | Typically `http://localhost:18889` |
| **Environment variable** | `ASPIRE_ALLOW_UNSECURED_TRANSPORT`, `DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` |
| **Process name** | Always `dotnet` (unless published as self-contained) |

---

## 🤖💡 Quick One-Liners (Before Using the Module)

### macOS / Linux

```bash
# Find the AppHost process
ps aux | grep -i 'AppHost' | grep -v grep

# Kill it by PID
kill <pid>

# Nuclear option — kill all dotnet processes (careful!)
pkill -f 'AppHost'
```

### PowerShell (Cross-Platform)

```powershell
# Find
Get-Process dotnet | Where-Object { (Get-ProcessArgs $_) -match 'AppHost' }

# Kill
Get-Process dotnet | Where-Object { (Get-ProcessArgs $_) -match 'AppHost' } | Stop-Process -Force
```

---

## 🤖💡 Using the AspireProcessManager Module

See [`aspire-process-manager.psm1`](./aspire-process-manager.psm1) in this directory.

### Import the Module

```powershell
Import-Module ./aspire-process-manager.psm1 -Force
```

### Find Aspire App Host Processes

```powershell
# Find any running Aspire AppHost
Find-AspireAppHost

# Find with a specific project name filter
Find-AspireAppHost -NameFilter "MyApp"

# Show verbose output including command line
Find-AspireAppHost -Verbose
```

### Stop Aspire App Host Processes

```powershell
# Stop gracefully (sends SIGTERM first, then SIGKILL after timeout)
Stop-AspireAppHost

# Stop all without confirmation prompt
Stop-AspireAppHost -Force

# Stop only if the name matches your project
Stop-AspireAppHost -NameFilter "MyApp"
```

### Get Detailed Info

```powershell
# Show process tree, ports, and command lines
Get-AspireProcessInfo
```

---

## 🤖💡 How Command Line Detection Works Per Platform

Getting a process's command line arguments is **not** exposed uniformly by PowerShell's `Get-Process`. Here's how the module handles each platform:

| Platform | Method |
|----------|--------|
| **macOS** | `ps -p <pid> -o args=` |
| **Linux** | Read `/proc/<pid>/cmdline` (null-byte delimited) |
| **Windows** | `Get-CimInstance Win32_Process -Filter "ProcessId = <pid>"` |

---

## 🤖💡 Dealing with Child Processes

Aspire AppHost spawns child processes for each service. When you kill the AppHost, the children **may or may not** be cleaned up depending on the OS. To be thorough:

```powershell
# Find all dotnet processes whose parent is the AppHost
Stop-AspireAppHost -IncludeChildren
```

The module's `Stop-AspireAppHost -IncludeChildren` flag walks the process tree and terminates children before the parent.

---

## 🤖💡 Troubleshooting

### "No Aspire process found"
- The AppHost may have been published as a self-contained executable — check for a process named after your project (e.g., `MyApp.AppHost`)
- Try `Find-AspireAppHost -ProcessName "*"` to search beyond just `dotnet`

### "Access denied" on Linux/macOS
- You may need `sudo` if the process was started by a different user or session

### Dashboard port already in use after restart
- A zombie process is holding the port. Use `Get-AspireProcessInfo` to list ports in use, then `Stop-AspireAppHost -Force`

---

## 🤖💡 Recommended Workflow

Add this to your `$PROFILE` so it's always available:

```powershell
Import-Module /path/to/aspire-process-manager.psm1

# Shortcut alias
Set-Alias -Name kaspire -Value Stop-AspireAppHost
```

Then from any terminal:

```powershell
kaspire          # kill the AppHost
kaspire -Force   # no prompts
```
