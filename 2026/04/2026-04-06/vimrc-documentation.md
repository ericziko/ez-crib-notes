---
title: Vim Configuration Documentation
created: 2026-04-06T00:00:00
modified: 2026-04-06T00:00:00
tags:
  - vim
  - configuration
  - editor
  - reference
---

# 📝 Vim Configuration Documentation

> Detailed reference for the settings and mappings in `suggested-vimrc.md`.

---

## 🏗️ Core

```vim
set nocompatible
syntax on
filetype plugin indent on
let mapleader = " "
```

| Setting | What it does |
|---|---|
| `set nocompatible` | Disables Vi-compatibility mode. Enables all modern Vim features. Must come first. |
| `syntax on` | Enables syntax highlighting using built-in language rules. |
| `filetype plugin indent on` | Three things in one: enables filetype detection, loads filetype-specific plugins (e.g. `ftplugin/markdown.vim`), and applies filetype-specific indentation rules. |
| `let mapleader = " "` | Sets `<Space>` as the leader key — the prefix for all `<leader>` mappings below. Space is ergonomic and rarely conflicts with native Vim commands. |

---

## 🖥️ UI

```vim
set number
set relativenumber
set ruler
set showcmd
set wildmenu
set hidden
set scrolloff=5
set signcolumn=yes
```

| Setting | What it does |
|---|---|
| `set number` | Shows absolute line numbers in the left gutter. |
| `set relativenumber` | Shows relative line numbers above and below the cursor. Combined with `number`, the current line shows its absolute number while surrounding lines show their distance — perfect for `5j`, `12k`-style jumps. |
| `set ruler` | Displays the cursor position (line, column) in the status line. |
| `set showcmd` | Shows partial commands in the bottom-right as you type them (e.g., `2d` before completing `2dw`). |
| `set wildmenu` | Enables enhanced tab-completion in command mode. Pressing `<Tab>` after `:e` shows a navigable menu of files. |
| `set hidden` | Allows switching buffers without saving first. Unsaved changes are kept in memory. Essential for multi-file workflows. |
| `set scrolloff=5` | Keeps 5 lines visible above and below the cursor when scrolling. Prevents the cursor from hitting the edge of the screen. |
| `set signcolumn=yes` | Always shows the sign column (the narrow gutter left of line numbers). Prevents layout jumping when signs appear (e.g., git diff marks, LSP diagnostics). |

---

## ✏️ Editing

```vim
set backspace=indent,eol,start
set clipboard=unnamedplus
set mouse=a
```

| Setting | What it does |
|---|---|
| `set backspace=indent,eol,start` | Makes `<Backspace>` work intuitively: can delete auto-indentation, cross line breaks, and delete before the insert-mode start point. Without this, backspace is often blocked in those positions. |
| `set clipboard=unnamedplus` | Ties Vim's unnamed register to the system clipboard (`+`). Yank/paste (`y`/`p`) operates on the OS clipboard directly — no need for `"+y`. On macOS use `set clipboard=unnamed` if `unnamedplus` doesn't work. |
| `set mouse=a` | Enables mouse support in all modes: click to position cursor, scroll, resize splits, and select visual ranges. |

---

## 🔍 Search

```vim
set ignorecase
set smartcase
set incsearch
set hlsearch

nnoremap <Esc><Esc> :nohlsearch<CR>
```

| Setting / Mapping | What it does |
|---|---|
| `set ignorecase` | Search is case-insensitive by default (`/foo` matches `Foo`, `FOO`). |
| `set smartcase` | Overrides `ignorecase` when the search pattern contains an uppercase letter. Typing `/Foo` searches case-sensitively; `/foo` searches case-insensitively. |
| `set incsearch` | Highlights matches incrementally as you type the search pattern, before pressing `<Enter>`. |
| `set hlsearch` | Highlights all matches of the last search pattern. |
| `<Esc><Esc>` → `:nohlsearch` | Pressing Escape twice clears the search highlight without changing the search history. |

---

## ↔️ Indentation

```vim
set expandtab
set shiftwidth=4
set softtabstop=4
set tabstop=4
set smartindent
```

| Setting | What it does |
|---|---|
| `set expandtab` | Inserts spaces instead of a real tab character when `<Tab>` is pressed. |
| `set shiftwidth=4` | Width of an indentation level when using `>>`, `<<`, or `=`. |
| `set softtabstop=4` | How many spaces `<Tab>` and `<Backspace>` feel like in insert mode. With `expandtab`, pressing `<Tab>` inserts 4 spaces; `<Backspace>` removes 4 spaces if they look like a tab stop. |
| `set tabstop=4` | How wide a literal tab character (`\t`) appears visually. |
| `set smartindent` | Automatically indents new lines based on C-like syntax rules (e.g., after `{`). For most filetypes, `filetype plugin indent on` provides better indentation — `smartindent` fills the gaps. |

---

## 🪟 Splits

```vim
set splitbelow
set splitright
```

| Setting | What it does |
|---|---|
| `set splitbelow` | New horizontal splits (`:split`, `<C-w>s`) open below the current window instead of above. |
| `set splitright` | New vertical splits (`:vsplit`, `<C-w>v`) open to the right instead of the left. |

Together these make split behaviour match the mental model most people have: new panes appear below or to the right.

---

## ↩️ Persistent Undo

```vim
if has('persistent_undo')
  set undofile
endif
```

| Setting | What it does |
|---|---|
| `set undofile` | Writes undo history to a file on disk. After closing and reopening a file, you can still undo changes from a previous session. The undo file is stored alongside the edited file by default (or in `undodir` if set). The `has('persistent_undo')` guard ensures this only runs if Vim was compiled with the feature. |

---

## 🚫 Kill Comment Continuation

```vim
augroup NoCommentContinuation
  autocmd!
  autocmd BufEnter * setlocal formatoptions-=r formatoptions-=o formatoptions-=c
augroup END
```

**Problem being solved:** By default, Vim (and many filetype plugins) automatically insert a comment leader (`//`, `*`, `#`) at the start of the next line when you press `<Enter>` in insert mode (`r`) or `o`/`O` in normal mode (`o`). This is usually annoying.

| Flag removed | Behaviour disabled |
|---|---|
| `r` | Stop auto-inserting comment leader on `<Enter>` in insert mode. |
| `o` | Stop auto-inserting comment leader when pressing `o` or `O`. |
| `c` | Stop auto-wrapping comments with `textwidth`. |

The `augroup` + `autocmd!` pattern clears previous definitions before adding the new one, preventing duplicate autocmds on config reload.

---

## 💾 Quick Save

```vim
nnoremap <leader>w :write<CR>
```

`<Space>w` saves the current buffer. Faster than `:w<Enter>`.

---

## 🪟 Window Navigation

```vim
nnoremap <C-h> <C-w>h
nnoremap <C-j> <C-w>j
nnoremap <C-k> <C-w>k
nnoremap <C-l> <C-w>l
```

Replaces the native two-key chord (`<C-w>h` etc.) with single `Ctrl+direction` presses to move between split windows. Matches the muscle memory of tmux pane navigation.

| Mapping | Moves to |
|---|---|
| `<C-h>` | Left window |
| `<C-j>` | Window below |
| `<C-k>` | Window above |
| `<C-l>` | Right window |

> **Note:** `<C-l>` natively redraws the screen. This mapping overrides that, but `:redraw!` still works if needed.

---

## ⚡ Quickfix Navigation

```vim
nnoremap ]q :cnext<CR>
nnoremap [q :cprev<CR>
nnoremap <leader>co :copen<CR>
nnoremap <leader>cc :cclose<CR>
```

The quickfix list is populated by grep, `make`, LSP diagnostics, and other tools.

| Mapping | Action |
|---|---|
| `]q` | Jump to next quickfix entry |
| `[q` | Jump to previous quickfix entry |
| `<Space>co` | Open the quickfix window |
| `<Space>cc` | Close the quickfix window |

The `]`/`[` prefix convention for "next/previous" matches Tim Pope's `vim-unimpaired` plugin style.

---

## 🔎 Grep with ripgrep

```vim
if executable('rg')
  set grepprg=rg\ --vimgrep\ --no-heading\ --smart-case
  set grepformat=%f:%l:%c:%m
endif

nnoremap <leader>rg :grep<Space>
```

| Setting | What it does |
|---|---|
| `set grepprg=rg ...` | Replaces Vim's default `grep` with `rg` (ripgrep). `--vimgrep` outputs `file:line:col:match` format. `--no-heading` keeps output flat. `--smart-case` mirrors the search settings above. |
| `set grepformat=%f:%l:%c:%m` | Tells Vim how to parse the output: `%f` = filename, `%l` = line, `%c` = column, `%m` = message/match text. Results populate the quickfix list. |
| `<Space>rg` | Opens a `:grep ` prompt ready to type a search term. Results land in the quickfix list; use `]q`/`[q` to navigate. |

**Workflow example:**
```
<Space>rg TodoItem<Enter>   → populates quickfix with all matches
]q / [q                     → jump between results
<Space>co                   → open quickfix window to see all at once
```

---

## 📁 fd Integration

```vim
command! -nargs=1 Find execute 'cexpr system("fd --type f " . shellescape(<q-args>))' | copen
```

Defines a `:Find` command that uses `fd` to search for files by name and loads the results into the quickfix list.

**Usage:**
```vim
:Find vimrc          " finds all files whose name contains 'vimrc'
:Find *.md           " finds all markdown files
```

`shellescape()` safely quotes the argument to prevent shell injection. Results open in the quickfix window automatically.

---

## 📋 Open All Quickfix Files

```vim
command! Qargs execute 'args ' . join(map(getqflist(), 'bufname(v:val.bufnr)'))
nnoremap <leader>qa :Qargs<CR>
```

`:Qargs` / `<Space>qa` loads every file currently in the quickfix list into the argument list (`args`). This enables bulk operations:

```vim
<Space>rg TODO<Enter>    " find all TODOs → quickfix
<Space>qa                " load all those files into arglist
:argdo %s/TODO/FIXME/g   " substitute across all of them
:argdo write             " save all
```

---

## 📄 Markdown

```vim
autocmd FileType markdown setlocal wrap linebreak nolist
autocmd FileType markdown setlocal spell

nnoremap ]] /^\s*#<CR>
nnoremap [[ ?^\s*#<CR>
```

| Setting / Mapping | What it does |
|---|---|
| `wrap` | Enables line wrapping for long lines (doesn't insert newlines, just wraps visually). |
| `linebreak` | Wraps at word boundaries instead of mid-word. |
| `nolist` | Hides the `listchars` markers (tabs, trailing spaces) which clutter prose writing. |
| `spell` | Enables spell checking. Use `]s` / `[s` to jump between misspelled words; `z=` to see suggestions. |
| `]]` | Jump forward to the next markdown heading (`# ...`). |
| `[[` | Jump backward to the previous markdown heading. |

> **Note:** `]]`/`[[` are buffer-global mappings here, not filetype-local. They only make sense in markdown files but will affect all buffers. Consider wrapping them in `autocmd FileType markdown` if this is a concern.

---

## 🌿 Git (Fugitive)

```vim
nnoremap <leader>gs :Git<CR>
nnoremap <leader>gl :Git log --oneline --graph --decorate --all<CR>
```

These mappings assume the `vim-fugitive` plugin is installed.

| Mapping | Action |
|---|---|
| `<Space>gs` | Opens the Fugitive status window (equivalent to `git status` + interactive staging). Press `-` to stage/unstage, `cc` to commit. |
| `<Space>gl` | Opens a compact, decorated, graph-format git log for all branches. |

---

## 🔄 Reload Config

```vim
nnoremap <leader>vr :source $MYVIMRC<CR>
nnoremap <leader>ve :edit $MYVIMRC<CR>
```

| Mapping | Action |
|---|---|
| `<Space>vr` | Re-sources (reloads) the vimrc without restarting Vim. Changes to settings take effect immediately. |
| `<Space>ve` | Opens the vimrc file for editing in the current window. |

`$MYVIMRC` is a Vim built-in variable that always points to the active vimrc file path.

---

## 🗺️ Quick Reference Card

| Category | Mapping | Action |
|---|---|---|
| **Save** | `<Space>w` | Write buffer |
| **Config** | `<Space>ve` | Edit vimrc |
| **Config** | `<Space>vr` | Reload vimrc |
| **Windows** | `<C-h/j/k/l>` | Navigate splits |
| **Quickfix** | `]q` / `[q` | Next / prev entry |
| **Quickfix** | `<Space>co` | Open quickfix |
| **Quickfix** | `<Space>cc` | Close quickfix |
| **Quickfix** | `<Space>qa` | Load QF files into arglist |
| **Search** | `<Space>rg` | Grep with ripgrep |
| **Search** | `<Esc><Esc>` | Clear search highlight |
| **Files** | `:Find <name>` | Find files with fd |
| **Git** | `<Space>gs` | Git status (fugitive) |
| **Git** | `<Space>gl` | Git log graph |
| **Markdown** | `]]` / `[[` | Next / prev heading |

---

## 🔌 External Tool Dependencies

| Tool | Used for | Install |
|---|---|---|
| `rg` (ripgrep) | `:grep` / `<Space>rg` | `brew install ripgrep` |
| `fd` | `:Find` command | `brew install fd` |
| `vim-fugitive` | `<Space>gs`, `<Space>gl` | Plugin manager of choice |
