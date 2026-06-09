---
title: Markdown Preview in Classic Vim
created: 2026-04-06T00:00:00
modified: 2026-04-06T00:00:00
tags:
  - vim
  - markdown
  - preview
  - plugins
uid: 8bb6ed28-ea13-423e-aef6-342f4ba0b706
---

# 📄 Markdown Preview in Classic Vim

> How to get rendered Markdown preview without leaving Vim, using only tools that work in terminal Vim (no Neovim required).

---

## 🗺️ The Options at a Glance

| Approach | How it works | Best for |
|---|---|---|
| **iamcco/markdown-preview.nvim** | Browser preview via WebSocket | Rich rendering, tables, diagrams |
| **instant-markdown/vim-instant-markdown** | Browser preview via a Node daemon | Simple setup |
| **previm/previm** | Browser preview via static HTML | Offline use |
| **Terminal render with `glow`** | Renders inline in a split terminal | Pure terminal workflow |
| **Terminal render with `pandoc` + `bat`** | Convert + pipe to pager | Lightweight, no browser |

---

## 🥇 Option 1 — `iamcco/markdown-preview.nvim` (works in classic Vim too)

Despite the `.nvim` in the name, this plugin supports Vim 8+ as well as Neovim.

### How it works

- Starts a local HTTP server inside Vim
- Opens a browser tab that connects over WebSocket
- The preview updates live as you type — no manual refresh needed

### Install

**With vim-plug:**
```vim
Plug 'iamcco/markdown-preview.nvim', { 'do': 'cd app && yarn install' }
```

**With Vundle:**
```vim
Plugin 'iamcco/markdown-preview.nvim'
" Then run: cd ~/.vim/bundle/markdown-preview.nvim/app && yarn install
```

**Requirements:** Node.js + yarn (or npm).

```bash
brew install node
npm install -g yarn
```

### Configuration

```vim
" Auto-start preview when opening a markdown file (default: 0)
let g:mkdp_auto_start = 0

" Auto-close preview when leaving the markdown buffer (default: 1)
let g:mkdp_auto_close = 1

" Refresh only on save or leaving insert mode (reduces flicker on slow machines)
let g:mkdp_refresh_slow = 0

" Use a custom browser (default: system default)
" let g:mkdp_browser = 'firefox'

" Preview server port (0 = random available port)
let g:mkdp_port = ''

" Page title format in the browser tab
let g:mkdp_page_title = '「${name}」'

" Mappings
nnoremap <leader>mp :MarkdownPreview<CR>
nnoremap <leader>ms :MarkdownPreviewStop<CR>
nnoremap <leader>mt :MarkdownPreviewToggle<CR>
```

### Commands

| Command | Action |
|---|---|
| `:MarkdownPreview` | Open preview in browser |
| `:MarkdownPreviewStop` | Stop the preview server |
| `:MarkdownPreviewToggle` | Toggle preview on/off |

---

## 🥈 Option 2 — `instant-markdown/vim-instant-markdown`

Simpler setup than `markdown-preview.nvim`. Uses a small Node daemon that serves a browser page.

### Install

```bash
npm install -g instant-markdown-d
```

```vim
" vim-plug
Plug 'instant-markdown/vim-instant-markdown', {'for': 'markdown', 'do': 'yarn install'}
```

### Configuration

```vim
" Only preview on explicit command, not automatically on open
let g:instant_markdown_autostart = 0

" Slow down refresh rate (good for large files)
let g:instant_markdown_slow = 1

" Allow external resources (images from the web)
let g:instant_markdown_allow_external_content = 1

" Open preview
nnoremap <leader>mp :InstantMarkdownPreview<CR>

" Stop preview
nnoremap <leader>ms :InstantMarkdownStop<CR>
```

### Commands

| Command | Action |
|---|---|
| `:InstantMarkdownPreview` | Open browser preview |
| `:InstantMarkdownStop` | Kill the daemon |

---

## 🥉 Option 3 — `previm/previm` (offline-friendly)

Generates a static HTML file and opens it in the browser. No running daemon — good for offline use or restricted environments.

### Install

```vim
Plug 'previm/previm'
Plug 'tyru/open-browser.vim'   " dependency — opens URLs in the browser
```

### Configuration

```vim
" Optionally pin to a specific browser
" let g:previm_open_cmd = 'open -a Firefox'

" Auto-refresh when the file changes (default: 1)
let g:previm_enable_realtime = 1

nnoremap <leader>mp :PrevimOpen<CR>
```

### Commands

| Command | Action |
|---|---|
| `:PrevimOpen` | Render and open in browser |

---

## 🖥️ Option 4 — Terminal Preview with `glow` (no browser)

`glow` is a CLI tool that renders Markdown beautifully in the terminal using colour and Unicode. Pipe the current buffer through it in a split — no browser needed.

### Install

```bash
brew install glow
```

### Vim integration

```vim
" Open a terminal split below showing glow-rendered preview of the current file
nnoremap <leader>mp :below terminal glow %<CR>
```

Or for a manual refresh workflow using a mapping that writes then renders:

```vim
nnoremap <leader>mp :write \| below terminal glow %<CR>
```

> **Note:** The preview is a static snapshot — it does not update as you type. Re-run the mapping to refresh.

### Persistent side-by-side layout

For a side-by-side split that stays open:

```vim
" Open vertical glow preview
nnoremap <leader>mp :vsplit \| terminal glow %<CR>
```

---

## 🧪 Option 5 — `pandoc` + `bat` in a Split (lightweight)

For a zero-plugin approach using tools you likely already have:

```vim
" Convert markdown to plain text and display in a horizontal split
nnoremap <leader>mp :write \| below split \| terminal pandoc -t plain % \| bat --style=plain<CR>
```

Or render to HTML and open in the default browser:

```vim
nnoremap <leader>mb :write \| execute '!pandoc % -o /tmp/vim-preview.html && open /tmp/vim-preview.html'<CR>
```

**Requirements:**
```bash
brew install pandoc
brew install bat        " optional — for syntax-highlighted pager
```

---

## 🔧 Recommended vimrc Block

Add this to your vimrc (works alongside `suggested-vimrc.md`):

```vim
" ---------- Markdown Preview ----------
" Pick ONE of the plugin options above. This example uses markdown-preview.nvim.

let g:mkdp_auto_start  = 0
let g:mkdp_auto_close  = 1
let g:mkdp_refresh_slow = 0

augroup MarkdownPreviewMappings
  autocmd!
  autocmd FileType markdown nnoremap <buffer> <leader>mp :MarkdownPreviewToggle<CR>
  autocmd FileType markdown nnoremap <buffer> <leader>ms :MarkdownPreviewStop<CR>
augroup END
```

Using `<buffer>` makes the mappings local to markdown files only — they won't shadow `<leader>mp` in other filetypes.

---

## 🆚 Comparison

| | `markdown-preview.nvim` | `vim-instant-markdown` | `previm` | `glow` | `pandoc` |
|---|---|---|---|---|---|
| **Live update** | ✅ WebSocket | ✅ WebSocket | 🔄 Polling | ❌ Manual | ❌ Manual |
| **Browser required** | ✅ | ✅ | ✅ | ❌ | Optional |
| **Offline** | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Tables / diagrams** | ✅ | ✅ | ✅ | ⚠️ Limited | ⚠️ Limited |
| **Install complexity** | Node + yarn | Node | Simple | `brew` only | `brew` only |
| **Vim 8 support** | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 💡 Recommendation

- **Best overall experience:** `iamcco/markdown-preview.nvim` — live browser preview, good table/image support, Vim 8 compatible.
- **Pure terminal, no browser:** `glow` — one `brew install`, zero plugins, renders beautifully in the terminal.
- **Offline + simple:** `previm` — no daemon, no Node version headaches.
