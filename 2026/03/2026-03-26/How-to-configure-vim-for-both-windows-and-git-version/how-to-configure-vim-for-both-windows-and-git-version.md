---
uid: 7f3a2c1d-4e8b-4f9a-b2c5-1d6e7f8a9b0c
title: How to Configure VIM for Both Windows and Git-for-Windows
created: 2026-03-26T00:00:00
modified: 2026-03-26T00:00:00
tags:
  - vim
  - windows
  - git-for-windows
  - vim-plug
  - configuration
---

# 🤖❓ How to Configure VIM for Both Windows and Git-for-Windows

## 📋 Table of Contents

1. [The Problem: Two VIMs, Two Worlds](#the-problem)
2. [Where Each VIM Looks for Config](#where-each-vim-looks)
3. [The Solution: A Single Shared Config Location](#the-solution)
4. [Setting Up the Shared Config](#setting-up-the-shared-config)
5. [Writing a Cross-Platform `.vimrc`](#writing-a-cross-platform-vimrc)
6. [Fixing VIM-PLUG Temp Directory Errors](#fixing-vim-plug-temp)
7. [VIM-PLUG Installation for Both Environments](#vim-plug-installation)
8. [FAQ](#faq)

---

## 🤖💡 The Problem: Two VIMs, Two Worlds {#the-problem}

When you have VIM on Windows you typically end up with two distinct installations:

| | **Windows VIM** | **Git-for-Windows VIM** |
|---|---|---|
| **Binary location** | `C:\Program Files\Vim\vim##\vim.exe` | `C:\Program Files\Git\usr\bin\vim.exe` |
| **Config file** | `$HOME\_vimrc` or `$HOME\vimfiles\` | `$HOME/.vimrc` or `$HOME/.vim/` |
| **`$HOME` resolves to** | `C:\Users\<you>` | `/c/Users/<you>` (MSYS2 POSIX path) |
| **Plugin directory** | `$HOME\vimfiles\` | `$HOME/.vim/` |
| **Path separator** | `\` | `/` |

The Git VIM runs inside an **MSYS2 POSIX emulation layer** — it thinks it is on Linux. Windows VIM runs natively and expects Windows paths.

---

## 🤖💡 Where Each VIM Looks for Config {#where-each-vim-looks}

### 🪟 Windows Native VIM

Run `:version` inside VIM and look for the line:

```
   system vimrc file: "$VIM/vimrc"
     user vimrc file: "$HOME\_vimrc"
 2nd user vimrc file: "$HOME/vimfiles/vimrc"
 3rd user vimrc file: "$VIM/_vimrc"
      user exrc file: "$HOME\_exrc"
```

Key points:
- Uses `_vimrc` (underscore, not dot) as the primary user config
- Uses `vimfiles\` as the plugin/runtime directory (equivalent of `.vim/`)
- `$HOME` = `C:\Users\<YourName>`
- `$VIM` = `C:\Program Files\Vim`

### 🐧 Git-for-Windows VIM

Run `:version` inside the Git Bash VIM:

```
   system vimrc file: "/etc/vimrc"
     user vimrc file: "$HOME/.vimrc"
 2nd user vimrc file: "~/.vim/vimrc"
      user exrc file: "$HOME/.exrc"
```

Key points:
- Uses `.vimrc` (dot prefix, Linux convention)
- Uses `.vim/` as the plugin/runtime directory
- `$HOME` = `/c/Users/<YourName>` in POSIX notation = `C:\Users\<YourName>` on disk

> 💡 **The key insight**: Both `$HOME` values resolve to the **same physical directory** on disk — `C:\Users\<YourName>`. The MSYS2 layer just presents it differently.

---

## 🤖💡 The Solution: A Single Shared Config Location {#the-solution}

Because both `$HOME` values point to the same folder (`C:\Users\<YourName>`), the trick is:

1. Create your actual config in a **single shared folder** (e.g., `C:\Users\<you>\.vim\`)
2. Create **symlinks or stub files** so each VIM finds its expected entry point and is redirected to the shared location
3. Use **conditional logic** in your `.vimrc` to handle path differences at runtime

**Architecture overview:**

```
C:\Users\<you>\
├── .vimrc          ← Real config (used by Git VIM directly)
├── _vimrc          ← Stub that sources .vimrc (used by Windows VIM)
├── .vim\           ← Real plugin/runtime dir (used by Git VIM directly)
│   ├── autoload\
│   │   └── plug.vim
│   └── plugged\
└── vimfiles\       ← Symlink OR stub pointing to .vim\ (used by Windows VIM)
```

---

## 🔧 Setting Up the Shared Config {#setting-up-the-shared-config}

### Step 1 — Create the canonical `.vim` directory

In **PowerShell** or **cmd**:

```powershell
mkdir "$env:USERPROFILE\.vim"
mkdir "$env:USERPROFILE\.vim\autoload"
mkdir "$env:USERPROFILE\.vim\plugged"
```

### Step 2 — Create the `_vimrc` stub for Windows VIM

Windows VIM loads `_vimrc` first. Make it simply source your real `.vimrc`:

```powershell
Set-Content -Path "$env:USERPROFILE\_vimrc" -Value 'source ~/.vimrc'
```

Or manually create `C:\Users\<you>\_vimrc` containing exactly:

```vim
source ~/.vimrc
```

> 💡 Windows VIM understands `~/` as `$HOME` so this path works correctly.

### Step 3 — Symlink `vimfiles` to `.vim` (recommended)

Windows VIM looks for its runtime files in `vimfiles\`. Create a directory symlink so it finds everything in `.vim\`.

Run this in an **elevated (Administrator) PowerShell**:

```powershell
# Remove vimfiles if it already exists (back it up first if needed)
# New-Item -ItemType SymbolicLink requires admin on Windows
New-Item -ItemType SymbolicLink `
  -Path "$env:USERPROFILE\vimfiles" `
  -Target "$env:USERPROFILE\.vim"
```

**Verify it worked:**

```powershell
Get-Item "$env:USERPROFILE\vimfiles" | Select-Object LinkType, Target
```

You should see `LinkType = SymbolicLink` and `Target = C:\Users\<you>\.vim`.

#### Alternative if you cannot run as Administrator

If you lack admin rights to create symlinks, add this to your `_vimrc` stub instead to tell Windows VIM to use `.vim\` as its runtime path:

```vim
" _vimrc - stub for Windows VIM (no symlink approach)
set runtimepath^=$HOME/.vim
set runtimepath+=$HOME/.vim/after
let &packpath = &runtimepath
source ~/.vimrc
```

---

## 📝 Writing a Cross-Platform `.vimrc` {#writing-a-cross-platform-vimrc}

Your real config lives at `C:\Users\<you>\.vimrc`. Use VIM's built-in variables to detect the environment and set paths accordingly.

```vim
" ============================================================
" ~/.vimrc — shared config for Windows VIM and Git-for-Windows VIM
" ============================================================

" ── Environment detection ───────────────────────────────────
let g:is_windows = has('win32') || has('win64')
let g:is_msys    = !g:is_windows && has('unix') && $MSYSTEM !=# ''

" ── Normalize $HOME to a consistent vim path ────────────────
" Both environments share the same physical $HOME on disk.
" On Windows VIM: $HOME = C:\Users\you
" On Git VIM:     $HOME = /c/Users/you  (POSIX)
" VIM's expand('~') handles this correctly in both cases.

" ── Temp / swap / backup directories ────────────────────────
" (See the dedicated section on temp dirs below)
let s:vimtmp = expand('~/.vim/tmp')
if !isdirectory(s:vimtmp)
  call mkdir(s:vimtmp, 'p')
endif

let &directory = s:vimtmp . '//'   " swap files
let &backupdir = s:vimtmp . '//'   " backup files
let &undodir   = s:vimtmp . '//'   " undo files

set backup
set undofile
set swapfile

" ── VIM-PLUG ────────────────────────────────────────────────
call plug#begin('~/.vim/plugged')

" Add your plugins here, e.g.:
" Plug 'tpope/vim-sensible'
" Plug 'preservim/nerdtree'

call plug#end()

" ── General settings ────────────────────────────────────────
set nocompatible
filetype plugin indent on
syntax enable

set number
set relativenumber
set tabstop=4
set shiftwidth=4
set expandtab
set encoding=utf-8

" ── Windows-specific font (only applies to gVIM) ────────────
if g:is_windows && has('gui_running')
  set guifont=Consolas:h11
endif
```

> 💡 Use `expand('~/.vim/...')` everywhere rather than hardcoding `C:\Users\...` or `/c/Users/...` — VIM resolves `~` correctly in both environments.

---

## 🚨 Fixing VIM-PLUG Temp Directory Errors {#fixing-vim-plug-temp}

### 🤖💬❓ What is the actual error?

The most common error looks like one of:

```
E303: Unable to open swap file for "...", recovery impossible
E484: Can't open file C:\Users\...\AppData\Local\Temp\...
```

Or during `PlugInstall`:

```
Error running job: Cannot write to temp file
```

### 🤖💡 Why this happens

VIM-PLUG runs `git clone` jobs asynchronously using VIM's `job_start()`. It writes temporary scripts to `$TMP` / `$TEMP`. This fails when:

1. `$TEMP` or `$TMP` env var points to a directory that doesn't exist
2. The path contains **spaces** (common — `C:\Users\First Last\AppData\...`)
3. The path uses characters that clash between Windows and MSYS2 environments
4. Permissions on the temp directory are restricted by enterprise policy

### 🔍 How to diagnose

In VIM, check what temp directory is being used:

```vim
:echo $TMP
:echo $TEMP
:echo tempname()
```

`tempname()` is what VIM actually uses when creating temp files. If it returns an empty string or a non-existent path, that is your problem.

### ✅ Fix 1 — Set a safe temp directory in your `.vimrc`

Add this **before** the `plug#begin()` call:

```vim
" ── Fix temp directory for VIM-PLUG ─────────────────────────
let s:vimtmp = expand('~/.vim/tmp')
if !isdirectory(s:vimtmp)
  call mkdir(s:vimtmp, 'p')
endif

" Override $TMP and $TEMP to use our known-good directory
let $TMP  = s:vimtmp
let $TEMP = s:vimtmp

" Also set VIM's own temp directory
set directory=$HOME/.vim/tmp//
```

> ⚠️ Set `$TMP`/`$TEMP` **before** `plug#begin()` is called — VIM-PLUG reads these at startup.

### ✅ Fix 2 — Ensure the directory exists before VIM starts

Create the directory from PowerShell so VIM never has to:

```powershell
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.vim\tmp"
```

### ✅ Fix 3 — Set system environment variables to a path without spaces

If enterprise policy prevents writing to `AppData\Local\Temp`, set permanent user-level env vars pointing somewhere clean:

```powershell
[System.Environment]::SetEnvironmentVariable('TMP',  "$env:USERPROFILE\.vim\tmp", 'User')
[System.Environment]::SetEnvironmentVariable('TEMP', "$env:USERPROFILE\.vim\tmp", 'User')
```

Restart your terminal after running this.

### ✅ Fix 4 — Git-for-Windows specific: MSYS2 temp collision

Git VIM may use `/tmp` (MSYS2 virtual path) while the Windows VIM uses `%TEMP%`. These can diverge. Force them to the same physical path by adding to your `.bashrc` or `.bash_profile` (in `C:\Users\<you>\.bashrc`):

```bash
export TMP="$HOME/.vim/tmp"
export TEMP="$HOME/.vim/tmp"
```

---

## 🔌 VIM-PLUG Installation for Both Environments {#vim-plug-installation}

VIM-PLUG needs `plug.vim` in `~/.vim/autoload/`. Since both VIMs share `~/.vim/`, you only need to install it once.

### Install via PowerShell (Windows native — works for both)

```powershell
$uri = 'https://raw.githubusercontent.com/junegunn/vim-plug/master/plug.vim'
$dest = "$env:USERPROFILE\.vim\autoload\plug.vim"

# Create autoload dir if needed
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.vim\autoload"

# Download plug.vim
Invoke-WebRequest -Uri $uri -OutFile $dest -UseBasicParsing
```

### Install via Git Bash

```bash
curl -fLo ~/.vim/autoload/plug.vim --create-dirs \
    https://raw.githubusercontent.com/junegunn/vim-plug/master/plug.vim
```

### Verify installation

Open VIM (either version) and run:

```vim
:PlugStatus
```

You should see your plugins listed. Run `:PlugInstall` to install them.

---

## ❓ FAQ {#faq}

### Q: Do I need to create both `_vimrc` and `.vimrc`?

**Yes.** Windows VIM loads `_vimrc` first; Git VIM loads `.vimrc`. The `_vimrc` is just a one-line stub that sources `.vimrc`. Git VIM ignores `_vimrc` entirely.

### Q: Why not just use a symlink from `_vimrc` → `.vimrc`?

You can, and it works. The stub `source` approach is simpler and doesn't require admin rights. If you have admin rights, a symlink is cleaner:

```powershell
# Admin PowerShell
New-Item -ItemType SymbolicLink -Path "$env:USERPROFILE\_vimrc" -Target "$env:USERPROFILE\.vimrc"
```

### Q: My plugins install but don't load in one of the VIMs

Check your `runtimepath`. In VIM run `:set rtp?`. Both VIMs should include `~/.vim` (or its Windows path equivalent). If Windows VIM shows only `vimfiles` and not `.vim`, either the symlink isn't working or you need the manual `runtimepath` override in your `_vimrc` stub (see Step 3 alternative above).

### Q: VIM-PLUG works in Git VIM but fails in Windows VIM with a Python/Ruby error

Windows VIM from `Program Files` may have been compiled with different feature flags than expected. Check:

```vim
:version
```

Look for `+python3`, `+job`, `+channel` in the feature list. VIM-PLUG's async install requires `+job` and `+channel`. If these are absent, your enterprise VIM build may be minimal. VIM-PLUG falls back to synchronous mode — add this before `plug#begin()`:

```vim
let g:plug_threads = 1  " force single-threaded (no async jobs)
```

### Q: How do I tell which VIM I'm running from inside VIM?

```vim
:echo v:progpath
```

This shows the full path to the running binary, e.g.:
- `C:\Program Files\Vim\vim91\vim.exe` — Windows VIM
- `/usr/bin/vim` — Git-for-Windows VIM

### Q: Can I have VIM-specific settings for each environment?

Yes — use conditional blocks in your shared `.vimrc`:

```vim
if has('win32') || has('win64')
  " Windows-only settings
  set shell=cmd.exe
elseif $MSYSTEM !=# ''
  " Git-for-Windows (MSYS2) settings
  set shell=/bin/bash
endif
```

### Q: Where does VIM read `$TMP`/`$TEMP` from?

VIM reads these **from the process environment** at startup. The order of precedence:

1. Whatever you set with `let $TMP = ...` in your `.vimrc` (overrides everything)
2. System/user environment variables set in Windows (`setx` or System Properties → Environment Variables)
3. MSYS2 inherited environment (for Git VIM)
4. Fallback to a Windows API temp path

Setting `let $TMP = ...` in `.vimrc` is the most reliable cross-environment fix.

---

## 📁 Final Directory Structure

After following this guide, your `C:\Users\<you>` folder should look like:

```
C:\Users\<you>\
├── .vimrc              ← Your real VIM config (canonical source of truth)
├── _vimrc              ← One-liner: source ~/.vimrc  (for Windows VIM)
├── .vim\               ← Canonical plugin/runtime directory
│   ├── autoload\
│   │   └── plug.vim    ← VIM-PLUG loader
│   ├── plugged\        ← Installed plugins land here
│   └── tmp\            ← Swap, backup, undo, and temp files
└── vimfiles\           ← Symlink → .vim\  (or handled via runtimepath in _vimrc)
```

Both VIMs now share a single config, a single plugin directory, and a reliable temp location. Changes to `.vimrc` take effect in both environments on next open.
