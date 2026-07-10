---
uid: c6777c5b-46b8-4f76-9af9-0c257cd8adc5
---
# 🤖❓PowerShell-performance-profiling-tips

## 🤖❓Prompt
I have a PowerShell profile that loads a bunch of different things, and it's a bit slow.

- What kind of profiling options are available to me in PowerShell, third-party modules or otherwise? 
- How can I start to figure out what's slow without having to add log statements all over the place?
- Please suggest any third-party PowerShell modules that might help out.
- Please write me a detailed tutorial of how I can figure this out.

### 🤖💡PowerShell Profile Performance Profiling

- [🤖💡PowerShell Profile Performance Profiling](<#powershell-profile-performance-profiling>)
	- [Quick Overview of Your Options](<#quick-overview-of-your-options>)
	- [Part 1 — Built-in PowerShell Tools](<#part-1--built-in-powershell-tools>)
	- [Part 2 — Third-Party Modules](<#part-2--third-party-modules>)
	- [Part 3 — Systematic Investigation Tutorial](<#part-3--systematic-investigation-tutorial>)
	- [Part 4 — Common Profile Bottlenecks Cheat Sheet](<#part-4--common-profile-bottlenecks-cheat-sheet>)
	- [TL;DR — Fastest Path to Answers](<#tldr--fastest-path-to-answers>)

#### Quick Overview of Your Options

| Method                   | Effort | Granularity           | Best For                       |
| ------------------------ | ------ | --------------------- | ------------------------------ |
| `Measure-Command`        | Low    | Whole blocks          | Quick timing of known suspects |
| `Get-Date` deltas        | Low    | Manual checkpoints    | Simple before/after            |
| Script Block tracing     | Medium | Line-by-line          | Finding unknown bottlenecks    |
| PSProfiler module        | Low    | Automatic line timing | Profile-specific analysis      |
| Custom transcript timing | Medium | Checkpoint-based      | Long-running profiles          |
| .NET `Stopwatch`         | Low    | Sub-millisecond       | High-precision timing          |

---

#### Part 1 — Built-in PowerShell Tools

##### 1.1 `Measure-Command` — The Obvious Starting Point

The simplest tool. Wraps any block and returns a `TimeSpan`.

```powershell
# Time your entire profile loading
Measure-Command { . $PROFILE }

# Time a specific suspected culprit
Measure-Command {
    Import-Module PSReadLine
}

# Time multiple things and compare them
$results = @{
    PSReadLine  = (Measure-Command { Import-Module PSReadLine }).TotalMilliseconds
    Az          = (Measure-Command { Import-Module Az }).TotalMilliseconds
    Posh-Git    = (Measure-Command { Import-Module posh-git }).TotalMilliseconds
}

$results.GetEnumerator() | Sort-Object Value -Descending |
    Format-Table @{L='Module';E={$_.Key}}, @{L='ms';E={[math]::Round($_.Value, 2)}}
```

> ⚠️ **Limitation:** `Measure-Command` swallows output. Use `-OutVariable` or redirect if you need to see what the block produces.

---

##### 1.2 .NET `Stopwatch` — High-Precision Timing

Better than `Get-Date` deltas because it uses a high-resolution timer and has no date formatting overhead.

```powershell
$sw = [System.Diagnostics.Stopwatch]::StartNew()

# --- your code here ---
Import-Module PSReadLine

$sw.Stop()
Write-Host "Elapsed: $($sw.Elapsed.TotalMilliseconds) ms"

# Lap timing pattern — checkpoint multiple stages
$sw = [System.Diagnostics.Stopwatch]::StartNew()

Import-Module PSReadLine
Write-Host "After PSReadLine: $($sw.ElapsedMilliseconds) ms"

Import-Module posh-git
Write-Host "After posh-git: $($sw.ElapsedMilliseconds) ms"

Set-PoshPrompt -Theme agnoster
Write-Host "After prompt theme: $($sw.ElapsedMilliseconds) ms"

$sw.Stop()
```

---

##### 1.3 Script Block Tracing with `Set-PSDebug` and `Trace-Command`

These are **built-in** and require **zero code changes** to your profile.

```powershell
# WARNING: Very verbose. Redirect to a file.
# Traces every line executed with timestamps.

Set-PSDebug -Trace 2   # 2 = trace lines + variable assignments

# Then dot-source your profile
. $PROFILE

Set-PSDebug -Trace 0   # Turn it off again
```

`Trace-Command` is more targeted — it traces specific PowerShell subsystems:

```powershell
# See all available trace sources
Get-TraceSource | Sort-Object Name | Format-Table Name, Description

# Trace module loading specifically
Trace-Command -Name Modules -Expression { Import-Module posh-git } -PSHost

# Trace command discovery (finds slow path lookups)
Trace-Command -Name CommandDiscovery -Expression {
    . $PROFILE
} -FilePath "$env:TEMP\profile-trace.log"

# Useful trace sources for profile debugging:
# Modules           — module load/unload events
# CommandDiscovery  — how commands are found
# TypeConversion    — type system overhead
# ParameterBinding  — parameter resolution
```

---

##### 1.4 `$PROFILE` Checkpoint Timing Pattern — No Extra Modules

Add this **temporarily** to your profile to get a timing breakdown without restructuring anything:

```powershell
# ── Paste this at the TOP of your profile ──────────────────────────────────
$_profileTimer = [System.Diagnostics.Stopwatch]::StartNew()
$_profileLast  = 0

function _ProfileCheckpoint ([string]$label) {
    $now   = $_profileTimer.ElapsedMilliseconds
    $delta = $now - $_profileLast
    $_profileLast = $now
    # Only report things that took more than 10ms — reduce noise
    if ($delta -gt 10) {
        Write-Host ("[profile] {0,6} ms  (+{1,5} ms)  {2}" -f $now, $delta, $label) `
            -ForegroundColor DarkGray
    }
}
# ───────────────────────────────────────────────────────────────────────────

# Now sprinkle _ProfileCheckpoint calls after logical sections:
Import-Module PSReadLine
_ProfileCheckpoint "PSReadLine"

Import-Module posh-git
_ProfileCheckpoint "posh-git"

oh-my-posh init pwsh --config "$env:POSH_THEMES_PATH\agnoster.omp.json" | Invoke-Expression
_ProfileCheckpoint "oh-my-posh"

# Source your custom functions file
. "$PSScriptRoot\functions.ps1"
_ProfileCheckpoint "functions.ps1"

# ── Paste this at the BOTTOM of your profile ───────────────────────────────
$_profileTimer.Stop()
Write-Host ("[profile] Total: $($_profileTimer.ElapsedMilliseconds) ms") -ForegroundColor Cyan
Remove-Item Function:\_ProfileCheckpoint
Remove-Variable _profileTimer, _profileLast
# ───────────────────────────────────────────────────────────────────────────
```

**Example output:**

```
[profile]    312 ms  (+  312 ms)  PSReadLine
[profile]    891 ms  (+  579 ms)  posh-git        ← 🚨 this is your problem
[profile]    934 ms  (+   43 ms)  oh-my-posh
[profile]    947 ms  (+   13 ms)  functions.ps1
[profile] Total: 947 ms
```

---

#### Part 2 — Third-Party Modules

##### 2.1 PSProfiler ⭐ — Purpose-Built for This Exact Problem

This is the most useful tool for your situation. It gives you **per-line timing** with no manual instrumentation.

```powershell
# Install
Install-Module PSProfiler -Scope CurrentUser

# Usage — profiles any script file line by line
# The -Top parameter shows only the N slowest lines
Measure-Script -Path $PROFILE -Top 10
```

**Example output:**

```
Line  Hits    Total Ms  Avg Ms  Command
----  ----    --------  ------  -------
  47     1    1423.11  1423.11  Import-Module Az.Accounts
  12     1     891.44   891.44  Import-Module posh-git
  89     1     203.17   203.17  $env:PATH = Get-PathFromRegistry
  23     1      44.22    44.22  oh-my-posh init pwsh | Invoke-Expression
```

> 💡 `PSProfiler` uses PowerShell's **AST (Abstract Syntax Tree)** to instrument your script automatically. You don't add any code — it reads the file directly.

```powershell
# Profile a specific function instead of a whole file
Measure-Script -ScriptBlock {
    . $PROFILE
}

# Save results to CSV for analysis
Measure-Script -Path $PROFILE |
    Export-Csv -Path "$env:TEMP\profile-results.csv" -NoTypeInformation
```

---

##### 2.2 Chronometer

An older alternative to PSProfiler with similar per-line approach:

```powershell
Install-Module Chronometer -Scope CurrentUser

# Reads your script and instruments it automatically
Get-Chronometer -Path $PROFILE | Format-Table -AutoSize
```

---

##### 2.3 ProfilePal — Profile Management Helper

Not a profiler in the timing sense, but helps you manage **multiple profile configurations** so you can easily toggle sections on/off during debugging:

```powershell
Install-Module ProfilePal -Scope CurrentUser
```

---

#### Part 3 — Systematic Investigation Tutorial

##### Step 1 — Establish a Baseline

Before touching anything, measure raw load time consistently:

```powershell
# Start a fresh PowerShell with NO profile loaded
pwsh -NoProfile

# Then time loading your profile from scratch
1..5 | ForEach-Object {
    (Measure-Command { . $PROFILE }).TotalMilliseconds
} | Measure-Object -Average -Minimum -Maximum | Format-List
```

> Run it **5 times** — first run is often slower due to disk caching. Use the average of runs 2–5 as your real baseline.

---

##### Step 2 — Automated Line-Level Profiling with PSProfiler

```powershell
# Install if needed
Install-Module PSProfiler -Scope CurrentUser -Force

# Get your worst offenders immediately
$results = Measure-Script -Path $PROFILE -Top 20

# Show the results sorted by total time
$results | Sort-Object TotalMilliseconds -Descending | Format-Table -AutoSize

# Find anything over 100ms
$results | Where-Object TotalMilliseconds -gt 100
```

---

##### Step 3 — Categorize What You Find

After PSProfiler identifies slow lines, they'll typically fall into these categories:

```powershell
# ── Category 1: Module imports ─────────────────────────────────
# Slow because modules search the filesystem for manifests

# Diagnose: is the module even used interactively?
# Fix A: Lazy-load it
function Get-AzContext {
    Import-Module Az.Accounts -Force
    & (Get-Module Az.Accounts) { Get-AzContext @args } @args
}

# Fix B: Use -UseWindowsPowerShell for compatibility modules
Import-Module SomeOldModule -UseWindowsPowerShell

# Fix C: Import only the submodule you actually need
Import-Module Az.Accounts    # instead of: Import-Module Az


# ── Category 2: Network calls ──────────────────────────────────
# Common culprits: version checkers, telemetry, git status on load

# Diagnose:
Trace-Command -Name * -Expression { . $PROFILE } -FilePath "$env:TEMP\trace.log"
Select-String "DNS\|HTTP\|TCP\|socket" "$env:TEMP\trace.log"

# Fix: Wrap in a background job
Start-ThreadJob -ScriptBlock {
    # whatever was doing a network call
    $result = Invoke-RestMethod "https://api.example.com/check"
    # write result somewhere your profile can use it later
} | Out-Null


# ── Category 3: Slow $env:PATH or registry reads ───────────────
# Fix: Cache results to a temp file, invalidate daily
$cachePath = "$env:TEMP\my-path-cache.txt"
$cacheAge  = if (Test-Path $cachePath) {
    (Get-Date) - (Get-Item $cachePath).LastWriteTime
} else { [TimeSpan]::MaxValue }

if ($cacheAge.TotalHours -gt 24) {
    $computedPath = Get-MyExpensivePath   # your slow function
    $computedPath | Set-Content $cachePath
} else {
    $computedPath = Get-Content $cachePath
}


# ── Category 4: Slow `oh-my-posh` / prompt themes ─────────────
# Diagnose prompt render time:
Measure-Command { & ([ScriptBlock]::Create((oh-my-posh init pwsh --config $env:POSH_THEMES_PATH\theme.omp.json))) }

# Fix: Switch to a theme without expensive segments
# Slow segments: git status, kubectl context, azure subscription, node version
# Check your theme JSON and remove segments you don't need


# ── Category 5: `Update-TypeData` / `Update-FormatData` ────────
# These are surprisingly slow if called on every profile load
# Fix: Only add if not already present
if (-not (Get-TypeData -TypeName 'MyCustomType' -ErrorAction SilentlyContinue)) {
    Update-TypeData -TypeName 'MyCustomType' -MemberType ScriptProperty ...
}
```

---

##### Step 4 — Binary Search for Unknown Slow Sections

If you can't easily add checkpoints, use binary search to isolate the problem:

```powershell
# Make a copy of your profile
Copy-Item $PROFILE "$env:TEMP\profile-test.ps1"

# Comment out the BOTTOM HALF and time it
# If fast → problem is in bottom half → restore bottom, comment top half
# Repeat until you've isolated the slow block to ~10 lines
Measure-Command { . "$env:TEMP\profile-test.ps1" }
```

This is tedious but **works on any profile** without any tooling.

---

##### Step 5 — Apply Lazy Loading Pattern

The most impactful fix for module-heavy profiles:

```powershell
# Instead of loading everything on startup, load on first use
# Pattern: create a proxy function that imports and then calls

function Enable-LazyModule {
    param(
        [string]$ModuleName,
        [string[]]$ExportedCommands
    )

    foreach ($cmd in $ExportedCommands) {
        $cmdName = $cmd   # capture for closure
        $modName = $ModuleName

        Set-Item -Path "Function:\$cmdName" -Value {
            # Remove this placeholder
            Remove-Item -Path "Function:\$cmdName" -ErrorAction SilentlyContinue

            # Load the real module
            Import-Module $modName -Global

            # Call the real command with original arguments
            & $cmdName @args
        }.GetNewClosure()
    }
}

# Usage in your profile — fast to set up, module loads on first actual use
Enable-LazyModule -ModuleName 'Az.Accounts' -ExportedCommands @(
    'Connect-AzAccount',
    'Get-AzContext',
    'Set-AzContext'
)

Enable-LazyModule -ModuleName 'SqlServer' -ExportedCommands @(
    'Invoke-Sqlcmd',
    'Get-SqlDatabase'
)
```

---

##### Step 6 — Verify Your Improvements

```powershell
# After each change, re-run PSProfiler to confirm improvement
Measure-Script -Path $PROFILE -Top 10

# And re-run the raw timing baseline
1..5 | ForEach-Object {
    (Measure-Command { . $PROFILE }).TotalMilliseconds
} | Measure-Object -Average | Select-Object -ExpandProperty Average
```

---

#### Part 4 — Common Profile Bottlenecks Cheat Sheet

```
Symptom                         Likely Cause                Fix
──────────────────────────────  ──────────────────────────  ──────────────────────────────
>500ms on Import-Module Az      Full Az module loaded        Import only Az.Accounts
Slow on every new terminal      posh-git scanning large repo POSH_GIT_ENABLED=false or scope
Hangs 2–3 seconds occasionally  DNS lookup / version check   Wrap in Start-ThreadJob
Prompt renders slowly           oh-my-posh complex theme     Remove git/cloud segments
Fast locally, slow on servers   Module path is a UNC share   Copy modules locally
Slow after Windows Update       Assembly cache invalidated   Ngen queue, wait it out
>100ms for simple functions     Dot-sourcing large .ps1      Split files, load selectively
```

---

#### TL;DR — Fastest Path to Answers

```powershell
# 1. Install PSProfiler
Install-Module PSProfiler -Scope CurrentUser

# 2. Run it — get your answer in 30 seconds
Measure-Script -Path $PROFILE -Top 10

# 3. Fix the top offenders with lazy loading or submodule imports

# 4. Verify improvement
```
1..3 | % { (Measure-Command { . $PROFILE }).TotalMilliseconds }
