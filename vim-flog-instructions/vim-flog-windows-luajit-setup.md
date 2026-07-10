---
title: vim-flog with LuaJIT on Windows — Setup & Configuration Guide
created: 2026-03-30
modified: 2026-03-30
tags:
  - vim
  - git
  - luajit
  - windows
  - vim-flog
uid: f8328585-2ef0-40c6-8637-f8fdcb8c9774
---

# 🛠️ vim-flog with LuaJIT on Windows — Setup & Configuration Guide

> **TL;DR:** Using vim-flog with LuaJIT on classic Windows Vim is technically possible but requires custom compilation and is not recommended for most users. The practical alternatives are Neovim (easiest) or Lua 5.1 with the official Vim installer (acceptable).

---

## 📋 Table of Contents

1. [What is vim-flog?](#what-is-vim-flog)
2. [How vim-flog Uses Lua/LuaJIT](#how-vim-flog-uses-lualuajit)
3. [Honest Assessment: Is This Possible?](#honest-assessment-is-this-possible)
4. [Option A — Recommended: Use Neovim on Windows](#option-a--recommended-use-neovim-on-windows)
5. [Option B — Acceptable: Lua 5.1 with Official Vim Windows](#option-b--acceptable-lua-51-with-official-vim-windows)
6. [Option C — Advanced: Compile Vim with LuaJIT on Windows](#option-c--advanced-compile-vim-with-luajit-on-windows)
7. [Installing vim-flog](#installing-vim-flog)
8. [Configuration & Usage](#configuration--usage)
9. [Troubleshooting](#troubleshooting)
10. [Decision Matrix](#decision-matrix)

---

## 🔍 What is vim-flog?

**vim-flog** is a fast, interactive Git branch/commit graph viewer for Vim and Neovim, created by `rbong`. It renders a visual Git log directly inside your editor.

**Key features:**
- Interactive commit graph (`:Flog`, `:Flogsplit`)
- Built on top of `vim-fugitive` for Git integration
- Uses Lua/LuaJIT for performance-critical graph rendering
- Works with both Vim 7.4.2204+ and Neovim

**GitHub:** https://github.com/rbong/vim-flog

---

## ⚙️ How vim-flog Uses Lua/LuaJIT

vim-flog offloads performance-sensitive graph traversal and rendering to Lua. The priority order is:

| Runtime | Support | Notes |
|---------|---------|-------|
| **LuaJIT 2.1** | ✅ Primary/Recommended | Best performance |
| **Lua 5.1** | ✅ Supported | Lower performance, but functional |
| No Lua | ❌ | Plugin will not work |

The Lua code handles complex graph operations; Vimscript handles the UI/UX layer.

---

## 🤔 Honest Assessment: Is This Possible?

**Short answer: Yes, but with significant caveats.**

| Path | Feasibility | Effort |
|------|-------------|--------|
| Neovim + vim-flog | ✅ Easy | ~15 minutes |
| Official Vim Windows + Lua 5.1 | ✅ Workable | ~30–60 minutes |
| Official Vim Windows + LuaJIT | ⚠️ Not possible (no pre-built) | N/A |
| Custom-compiled Vim + LuaJIT | ⚠️ Technically possible | Hours to days |

**Why LuaJIT + classic Vim on Windows is hard:**
- The official `vim-win32-installer` does **not** ship LuaJIT — only Lua 5.1.4
- Community builds (e.g. `lboulard/vim-win32-build`) also use Lua 5.1, not LuaJIT
- An open request to "ship LuaJIT binaries with the installer" (Issue [#182](https://github.com/vim/vim-win32-installer/issues/182)) remains unresolved as of 2026
- Compiling Vim with LuaJIT requires matching C runtime toolchains (MSVC vs MinGW), matching bitness (32-bit vs 64-bit), and non-trivial build knowledge

---

## ✅ Option A — Recommended: Use Neovim on Windows

Neovim ships with **LuaJIT 2.1 built in** — no extra setup required. This is the cleanest path to vim-flog + LuaJIT on Windows.

### Step 1: Install Neovim

Download the latest prebuilt release from GitHub:

```
https://github.com/neovim/neovim/releases/latest
```

Choose `nvim-win64.msi` (or `nvim-win64.zip` for portable install).

Run the installer or extract the zip to a directory of your choice (e.g. `C:\tools\nvim`).

Add to your **System PATH** if using the zip:
```
C:\tools\nvim\bin
```

Verify installation:
```powershell
nvim --version
# Look for: LuaJIT 2.1.x
```

### Step 2: Install a Plugin Manager

**lazy.nvim** (recommended for Neovim):

Create (or open) your Neovim init file:
```
%LOCALAPPDATA%\nvim\init.lua
```

Bootstrap lazy.nvim by adding this to the top of `init.lua`:
```lua
local lazypath = vim.fn.stdpath("data") .. "/lazy/lazy.nvim"
if not (vim.uv or vim.loop).fs_stat(lazypath) then
  local lazyrepo = "https://github.com/folke/lazy.nvim.git"
  local out = vim.fn.system({ "git", "clone", "--filter=blob:none", "--branch=stable", lazyrepo, lazypath })
  if vim.v.shell_error ~= 0 then
    vim.api.nvim_echo({ { "Failed to clone lazy.nvim:\n", "ErrorMsg" }, { out, "WarningMsg" }, { "\nPress any key to exit...", "ErrorMsg" } }, true, {})
    vim.fn.getchar()
    os.exit(1)
  end
end
vim.opt.rtp:prepend(lazypath)
```

### Step 3: Install vim-fugitive and vim-flog

Add to `init.lua` after the lazy.nvim bootstrap:
```lua
require("lazy").setup({
  {
    "tpope/vim-fugitive",
  },
  {
    "rbong/vim-flog",
    lazy = true,
    cmd = { "Flog", "Flogsplit", "Floggit" },
    dependencies = { "tpope/vim-fugitive" },
  },
})
```

### Step 4: Verify Lua/LuaJIT is Available

Inside Neovim:
```vim
:lua print(jit.version)
```

Expected output: `LuaJIT 2.1.xxxx`

You're done. Run `:Flog` inside a Git repository to see the commit graph.

---

## 🟡 Option B — Acceptable: Lua 5.1 with Official Vim Windows

This uses the official Vim Windows installer (which includes Lua 5.1) — no LuaJIT, but vim-flog will work.

### Step 1: Install Official Vim for Windows

Download from the official installer:
```
https://github.com/vim/vim-win32-installer/releases/latest
```

Choose `gvim_X.Y.Z_x64.exe` (64-bit recommended).

During installation, the Lua 5.1 support is bundled — no extra action needed.

Verify Lua is available inside Vim:
```vim
:echo has("lua")
" Should return: 1
```

### Step 2: Obtain the Lua 5.1 DLL (if missing)

If `:echo has("lua")` returns `0`, you need to provide the DLL manually.

Download the matching Lua 5.1 binaries from LuaBinaries:
```
https://luabinaries.sourceforge.net/
```

- For 64-bit Vim: download `lua-5.1.5_Win64_dll17_lib.zip`
- Extract `lua51.dll`
- Place it in your Vim install directory (e.g. `C:\Program Files\Vim\vim91\`)

Restart Vim and re-run `:echo has("lua")`.

### Step 3: Install vim-plug (Plugin Manager for Classic Vim)

Open PowerShell and run:
```powershell
iwr -useb https://raw.githubusercontent.com/junegunn/vim-plug/master/plug.vim |
    ni "$HOME/vimfiles/autoload/plug.vim" -Force
```

### Step 4: Configure `.vimrc`

Open or create `%USERPROFILE%\_vimrc` and add:
```vim
call plug#begin()
  Plug 'tpope/vim-fugitive'
  Plug 'rbong/vim-flog'
call plug#end()
```

Save and reload Vim, then run:
```vim
:PlugInstall
```

### Step 5: Verify

```vim
:echo has("lua")
" Should return: 1
```

Open a Git repo directory and run:
```vim
:Flog
```

> **Note:** Performance will be slightly lower than LuaJIT, but for most repositories this is not noticeable.

---

## 🔴 Option C — Advanced: Compile Vim with LuaJIT on Windows

> ⚠️ **Warning:** This path is complex, error-prone, and not recommended unless you have strong C/build-toolchain experience. The instructions below are a guide — expect troubleshooting.

### Prerequisites

- Visual Studio 2022 (with C++ build tools) or MinGW-w64
- Git for Windows
- MSYS2 (for MinGW builds)

### Step 1: Build LuaJIT for Windows

The LuaJIT DLL must be built with the **same toolchain** (MSVC or MinGW) and **same bitness** as Vim.

**Using MSYS2/MinGW:**
```bash
pacman -S mingw-w64-x86_64-gcc mingw-w64-x86_64-make git

git clone https://luajit.org/git/luajit.git
cd luajit
mingw32-make CFLAGS="-DLUAJIT_ENABLE_LUA52COMPAT"
```

Output: `src/luajit.exe` and `src/lua51.dll`

**Using MSVC (Visual Studio):**
```cmd
git clone https://luajit.org/git/luajit.git
cd luajit\src

# Open "x64 Native Tools Command Prompt for VS 2022"
msvcbuild.bat
```

Output: `luajit.exe` and `lua51.dll`

### Step 2: Place LuaJIT Files

Create a directory, e.g. `C:\LuaJIT\`:
```
C:\LuaJIT\
  luajit.exe
  lua51.dll
  include\
    lua.h
    lauxlib.h
    lualib.h
    luaconf.h
    luajit.h
```

The `include\` headers are needed to compile Vim. They are in the LuaJIT source under `src\`.

### Step 3: Obtain Vim Source

```cmd
git clone https://github.com/vim/vim.git
cd vim\src
```

### Step 4: Compile Vim with LuaJIT (MSVC)

Open "x64 Native Tools Command Prompt for VS 2022":
```cmd
cd vim\src

nmake -f Make_mvc.mak ^
  FEATURES=huge ^
  LUA=C:\LuaJIT ^
  LUA_VER=51 ^
  DYNAMIC_LUA=yes ^
  LUAJIT=yes ^
  GUI=yes
```

Key flags:
- `LUA=C:\LuaJIT` — path to your LuaJIT installation
- `LUA_VER=51` — LuaJIT uses the Lua 5.1 API
- `LUAJIT=yes` — tells the build system to use LuaJIT instead of standard Lua
- `DYNAMIC_LUA=yes` — link LuaJIT DLL dynamically

### Step 5: Verify the Build

```cmd
gvim --version | findstr Lua
```

Expected: `+lua/dyn`

Inside Vim:
```vim
:lua print(jit and jit.version or "No JIT")
```

Expected output: `LuaJIT 2.1.xxxx`

### Step 6: Install vim-flog

Follow the same steps as Option B (vim-plug + `.vimrc` configuration).

---

## 📦 Installing vim-flog

### With lazy.nvim (Neovim)

```lua
{
  "rbong/vim-flog",
  lazy = true,
  cmd = { "Flog", "Flogsplit", "Floggit" },
  dependencies = { "tpope/vim-fugitive" },
}
```

### With vim-plug (Classic Vim)

In `_vimrc`:
```vim
call plug#begin()
  Plug 'tpope/vim-fugitive'
  Plug 'rbong/vim-flog'
call plug#end()
```

Run `:PlugInstall` after saving.

### With Vundle (Classic Vim)

In `_vimrc`:
```vim
Plugin 'tpope/vim-fugitive'
Plugin 'rbong/vim-flog'
```

Run `:PluginInstall` after saving.

---

## 🎯 Configuration & Usage

### Basic Commands

| Command | Action |
|---------|--------|
| `:Flog` | Open commit graph (full window) |
| `:Flogsplit` | Open commit graph (split window) |
| `:Floggit` | Run a git command from flog |
| `:help flog` | Full built-in documentation |

### Key Bindings (inside Flog buffer)

| Key | Action |
|-----|--------|
| `<Enter>` | Open commit in preview |
| `o` | Open commit in split |
| `q` | Close flog window |
| `a` | Toggle all refs |
| `gb` | Show commits from current branch only |
| `<C-n>` | Jump to next commit |
| `<C-p>` | Jump to previous commit |

### Optional Configuration

Add to your `_vimrc` / `init.lua`:

**Vim:**
```vim
" Open flog in a vertical split by default
nmap <leader>gl :Flogsplit -format=%h\ [%ar]\ %s<CR>
```

**Neovim (Lua):**
```lua
vim.keymap.set("n", "<leader>gl", "<cmd>Flogsplit -format=%h\\ [%ar]\\ %s<CR>")
```

### Verifying Lua/LuaJIT is Active

Inside Vim:
```vim
:echo has("lua")
```

Inside Neovim:
```vim
:lua print(jit and jit.version or "Standard Lua (no JIT)")
```

---

## 🔧 Troubleshooting

### `:echo has("lua")` returns 0

- You are using a Vim build without Lua support
- Download the official `gvim_X.Y.Z_x64.exe` (the official installer includes Lua)
- Ensure `lua51.dll` is in your Vim directory or on your PATH

### Vim crashes on startup or when Lua is used

- DLL bitness mismatch: your `lua51.dll` is 32-bit but Vim is 64-bit (or vice versa)
- C runtime mismatch: DLL built with MinGW but Vim built with MSVC (or vice versa)
- Solution: rebuild or re-download a matching DLL

### `:Flog` shows errors about vim-fugitive

- Ensure `tpope/vim-fugitive` is installed and loaded before `vim-flog`
- Run `:PlugInstall` again and restart Vim

### LuaJIT build fails on Windows

- Ensure you are using matching toolchain (MSVC vs MinGW throughout)
- Try the MSYS2/MinGW path — it is generally more straightforward on Windows
- Check LuaJIT mailing list: https://luajit.org/list.html

### `:lua print(jit.version)` errors in Neovim

- Your Neovim installation may be corrupted or very old
- Reinstall Neovim from the official releases page

---

## 📊 Decision Matrix

| You want... | Use this path |
|-------------|---------------|
| LuaJIT + easiest setup | **Option A** — Neovim |
| Classic Vim + it just works | **Option B** — Official Vim + Lua 5.1 |
| Classic Vim + LuaJIT (no compromise) | **Option C** — Custom compile (advanced) |
| Maximum performance on Windows | **Option A** — Neovim |

---

## 📚 References

- [vim-flog GitHub](https://github.com/rbong/vim-flog)
- [vim-fugitive GitHub](https://github.com/tpope/vim-fugitive)
- [Neovim Releases](https://github.com/neovim/neovim/releases)
- [vim-win32-installer](https://github.com/vim/vim-win32-installer)
- [LuaJIT Installation Guide](https://luajit.org/install.html)
- [LuaBinaries (Lua 5.1 Windows DLLs)](https://luabinaries.sourceforge.net/)
- [vim-plug](https://github.com/junegunn/vim-plug)
- [lazy.nvim](https://github.com/folke/lazy.nvim)
- [Vim Windows Compilation Guide](https://vimdoc.sourceforge.net/howto/win32-compile/Vim-Compile-Win32-HOWTO/compiling.html)
- [Issue: Ship LuaJIT with vim-win32-installer #182](https://github.com/vim/vim-win32-installer/issues/182)
