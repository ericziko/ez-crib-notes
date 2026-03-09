---
uid: 80d38978-56c2-49d2-970d-77ee01b1aa2b
created: 2026-02-24T20:29:32
modified: 2026-02-24T20:38:27
title: 🤖❓How to kill Aspire
---

# 🤖❓How to kill Aspire
## Summary
I'm using .NET Aspire, and when I start the app host, I'd like to write some sort of a PowerShell script or something to kill that particular .NET process. How do I figure out which of all the .NET processes running on my machine is the one that's running the Aspire app host?

## 🤖💡

If you want something more deterministic, you can:
	- Match by full path to the DLL
	- Match by working directory
	- Match by port (via Get-NetTCPConnection and then correlate PID)

For example, killing by port:

```powershell
$port = 15888  # whatever Aspire dashboard or service port
$pid = (Get-NetTCPConnection -LocalPort $port).OwningProcess
Stop-Process -Id $pid -Force
```

That's extremely reliable if the port is stable.

Now let's think architecturally.

If your goal is to stop Aspire cleanly as part of automation, you might be better off:
	- Capturing the PID when you start it
	- Writing that PID to a file
	- Killing exactly that PID later

Example:

Start it:

```powershell
$process = Start-Process dotnet -ArgumentList "run --project MyAppHost" -PassThru
$process.Id | Out-File ".aspire.pid"
```

Stop it:

```powershell
$pid = Get-Content ".aspire.pid"
Stop-Process -Id $pid -Force
Remove-Item ".aspire.pid"
```

## If you want, we can also go deeper into how Aspire spawns child processes and how to shut down the entire tree instead of just the host

Cool — let's graduate from "kill a dotnet" to "kill the whole Aspire ecosystem you spawned."

There are really two worlds Aspire can be running in:
	1. Local processes (lots of dotnet.exe children)
	2. Containers (Docker / Podman / whatever) — the AppHost may be local, but services might not be.

I'll give you a solid "kill the process tree" approach for (1), plus a pragmatic note for (2).

Option A (simple + effective): kill the whole process tree with taskkill

Once you've identified the AppHost PID, Windows already has a built-in "take the kids too" hammer:

```powershell
taskkill /PID $pid /T /F
```

- /T = kill the entire tree (children, grandchildren…)
- /F = force

So the only remaining problem is reliably getting $pid.

Find the AppHost PID by command line

This is the best "process fingerprint" method:

```powershell
$appHostMatch = "MyAppHost"  # part of the project name or dll path

$host = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
  Where-Object { $_.CommandLine -like "*$appHostMatch*" } |
  Select-Object -First 1

$host | Select-Object ProcessId, CommandLine
```

Then:

```powershell
taskkill /PID $host.ProcessId /T /F
```

That's it. Dead host, dead children.

### Option B (most deterministic): capture the PID when you start AppHost

This is the "engineers don't guess" route.

Start AppHost and store its PID:

```powershell
$proc = Start-Process dotnet -ArgumentList "run --project .\MyAppHost\MyAppHost.csproj" -PassThru
$proc.Id | Set-Content .aspire-apphost.pid
```

Stop later (and kill the tree):

```powershell
$pid = [int](Get-Content .aspire-apphost.pid)
taskkill /PID $pid /T /F
Remove-Item .aspire-apphost.pid -ErrorAction SilentlyContinue
```

### Practical recommendation

For "one script to rule them all" on a dev box:
	1. Prefer PID capture at start (Option C)
	2. Stop with taskkill /T /F
	3. Optionally stop matching containers if you use Docker/Podman

⸻

If you tell me how you start the AppHost (VS run button vs dotnet run vs aspire run) and whether you see anything in docker ps, I can give you a final "drop-in script" that handles your exact setup (process tree + optional containers) with nice output and a safe dry-run mode.
