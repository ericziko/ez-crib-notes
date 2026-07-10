---
title: Obsidian Windows Freeze Troubleshooting Guide
created: 2026-04-01T00:00:00
modified: 2026-04-01T00:00:00
tags:
  - obsidian
  - troubleshooting
  - windows
  - performance
uid: 6682fc85-e68b-4b9b-a7da-753015e95c4f
---

# 🔍 Obsidian Windows Freeze Troubleshooting Guide

Obsidian is an Electron/Chromium app, which means freezes can stem from plugin JavaScript, renderer process memory issues, file system watchers, or GPU rendering problems. Work through these sections methodically.

---

## 🩺 Step 1 — Establish a Baseline (Safe Mode)

Before changing anything, confirm whether the freezes are plugin-related.

1. Open Obsidian → **Settings → Community Plugins**
2. Toggle **Safe Mode ON** (disables all community plugins)
3. Use Obsidian normally for a day or two

| Result | Meaning |
|--------|---------|
| Freezes stop | A community plugin is the culprit — proceed to Step 2 |
| Freezes continue | Core Obsidian or system issue — skip to Step 4 |

---

## 🔌 Step 2 — Identify the Offending Plugin (Binary Search)

If Safe Mode fixes it, isolate the plugin with a binary search approach:

1. Re-enable **half** your plugins
2. Test for a day
3. If freeze returns → the culprit is in that half; if not → it's in the other half
4. Repeat until you find the single offending plugin

### 🔎 High-Suspect Plugins (known to be resource-heavy)

These plugins are common freeze culprits due to continuous background work:

- **Dataview** — constantly re-indexes vault; large vaults can cause significant lag
- **Obsidian Git** — file system polling + git operations can block the renderer
- **Templater** — runs JS on file open/create events
- **Periodic Notes / Calendar** — can trigger on every file change
- **Omnisearch / Smart Search** — maintains background search indexes
- **Sync-related plugins** — iCloud, OneDrive, or third-party sync combined with Obsidian's own watcher causes double-watch conflicts on Windows

---

## ⚙️ Step 3 — Plugin-Specific Fixes

### Dataview
```
Settings → Dataview → Refresh Interval
```
- Increase **Automatic View Refreshing** interval (e.g., from 500ms → 5000ms)
- Disable **Enable JavaScript Queries** if not needed

### Obsidian Git
- Set **Auto Pull/Push** intervals longer (e.g., every 30min instead of 5min)
- Disable **Pull on startup**

### General for all plugins
- Check each plugin's GitHub issues page for "freeze", "hang", or "Windows" reports

---

## 🖥️ Step 4 — Chromium/Electron Renderer Issues

Since Obsidian runs on Chromium, GPU and renderer processes can cause freezes.

### 4a — Disable Hardware Acceleration

1. Close Obsidian
2. Navigate to your Obsidian config folder:
   ```
   %APPDATA%\obsidian\
   ```
3. Open (or create) `obsidian.json` and add:
   ```json
   {
     "hardwareAcceleration": false
   }
   ```
4. Restart Obsidian

> This forces software rendering. If freezes stop, your GPU driver or integrated graphics is the issue.

### 4b — Check the Chromium DevTools Console

1. In Obsidian, press `Ctrl+Shift+I` to open DevTools
2. Go to **Console** tab — look for errors or warnings during a freeze
3. Go to **Memory** tab → take a heap snapshot before and after a freeze session
4. Look for memory growing unboundedly (memory leak in a plugin)

### 4c — Check the Renderer Process

In DevTools → **Performance** tab:
1. Click **Record**
2. Trigger or wait for a freeze
3. Stop recording
4. Look for long **yellow (scripting)** or **purple (rendering)** blocks — these identify what's consuming time

---

## 📁 Step 5 — File System & Vault Issues

Windows file system watchers are a known Electron pain point.

### 5a — Vault Size Check

Large vaults (10,000+ files) stress the watcher. Check your vault:
```powershell
# Run in PowerShell from your vault root
(Get-ChildItem -Recurse | Measure-Object).Count
```

If >10,000 files:
- Move archived/old notes out of the active vault
- Use **Excluded Files** setting to exclude large attachment folders

### 5b — Antivirus Exclusion

Windows Defender (or other AV) scanning every `.md` file write is a very common freeze cause:

1. Open **Windows Security → Virus & Threat Protection → Manage Settings**
2. Add exclusions for:
   - Your vault folder path
   - `%APPDATA%\obsidian\`
   - The Obsidian executable: `%LOCALAPPDATA%\Obsidian\Obsidian.exe`

### 5c — Sync Software Conflicts

If your vault is in **OneDrive**, **Dropbox**, or **Google Drive**:
- These sync clients watch the same files as Obsidian → race conditions and locks
- Consider moving vault to a **local-only** folder and syncing via Obsidian Sync or Git instead
- At minimum, pause sync and test if freezes stop

---

## 🔄 Step 6 — Use obsidian-cli for Diagnostics

Obsidian's new [obsidian-cli](https://obsidian.md/cli) provides command-line access. Useful diagnostic commands:

```bash
# List installed plugins and their status
obsidian plugin list

# Check vault info
obsidian vault info

# Open vault with specific flags for testing
obsidian open --vault "MyVault" --safe-mode
```

> Check the CLI docs for the latest available diagnostic commands — the tool was recently released and commands may evolve.

---

## 📊 Step 7 — System-Level Diagnostics

If the problem persists, collect system data during a freeze.

### 7a — Task Manager During Freeze

When Obsidian freezes, immediately open Task Manager (`Ctrl+Shift+Esc`):
- Check **CPU** column — is Obsidian or a helper process spiking?
- Check **Memory** — is it growing over time?
- Look for `Obsidian.exe`, `Obsidian Helper (Renderer).exe`, `Obsidian Helper (GPU).exe`

### 7b — Event Viewer

After a freeze/crash:
1. Open **Event Viewer** → **Windows Logs → Application**
2. Filter by Source: `Application Error` or look for Obsidian entries
3. Note any `.js` stack traces — these point to plugin code

### 7c — Process Monitor (Sysinternals)

For deep file system investigation:
1. Download [Process Monitor](https://learn.microsoft.com/en-us/sysinternals/downloads/procmon) from Sysinternals
2. Filter by `Process Name = Obsidian.exe`
3. Look for excessive file I/O or any `ACCESS DENIED` errors during freezes

---

## 🔧 Step 8 — Obsidian Settings Tweaks

### Reduce Editor Overhead
- **Settings → Editor → Live Preview** → switch to **Source mode** temporarily to test (Live Preview renders markdown continuously)
- Disable **Spell check** if enabled (can be slow on large files)
- Disable **Backlinks in document** (recalculates on every edit)

### Reduce Startup Load
- **Settings → Files & Links → Detect all file extensions** → OFF (reduces initial scan)
- Disable **Reopen last closed tab on startup**

---

## 🆙 Step 9 — Update or Reinstall

1. **Update Obsidian**: Check **Settings → About** for updates — many freeze bugs are fixed in patch releases
2. **Update plugins**: **Settings → Community Plugins → Check for updates**
3. **Update GPU drivers**: Outdated drivers cause Chromium rendering freezes
4. **Reinstall Obsidian**: If all else fails, fully uninstall, delete `%APPDATA%\obsidian\`, and reinstall fresh

---

## 📋 Diagnostic Checklist Summary

| Step | Action | Time to Test |
|------|--------|-------------|
| 1 | Safe Mode | 1-2 days |
| 2 | Binary search plugins | 1-2 days per half |
| 4a | Disable hardware acceleration | 1-2 days |
| 5b | AV exclusion | Immediate |
| 5c | Disable sync | Immediate |
| 7a | Task Manager snapshot | During next freeze |
| 8 | Editor/startup tweaks | 1 day |
| 9 | Update everything | Immediate |

---

## 💡 Most Likely Culprits (Ranked)

1. **Antivirus scanning vault files** — most common Windows-specific cause
2. **Dataview plugin** on a large vault
3. **Sync software conflict** (OneDrive/Dropbox + Obsidian watcher)
4. **GPU driver / hardware acceleration** bug
5. **Obsidian Git** with aggressive poll intervals
