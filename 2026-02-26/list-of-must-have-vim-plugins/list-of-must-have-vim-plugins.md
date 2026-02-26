---
title: Must-Have VIM Plugins for .NET/C# Developers
created: 2026-02-26T00:00:00
modified: 2026-02-26T01:13:50
tags:
  - vim
  - plugins
  - dotnet
  - csharp
  - tooling
---

# list-of-must-have-vim-plugins

## 🤖💡 Overview

This guide covers the best plugins for Vim 8.0+ for a .NET/C# software developer. All plugins are installable with vim-plug. Categories are ordered by priority for day-to-day development work.

---

## Prerequisites: vim-plug Plugin Manager

Before installing any plugins, you need a plugin manager. **vim-plug** is the recommended choice: it is fast, minimal, and supports on-demand loading.

**GitHub:** [https://github.com/junegunn/vim-plug](https://github.com/junegunn/vim-plug)

### Install vim-plug (Unix/macOS)

```bash
curl -fLo ~/.vim/autoload/plug.vim --create-dirs \
    https://raw.githubusercontent.com/junegunn/vim-plug/master/plug.vim
```

### Basic `.vimrc` structure

```vim
call plug#begin('~/.vim/plugged')

" -- plugins go here --

call plug#end()
```

### Key vim-plug commands

| Command | Description |
|---------|-------------|
| `:PlugInstall` | Install all listed plugins |
| `:PlugUpdate` | Update all plugins |
| `:PlugClean` | Remove unlisted plugins |
| `:PlugStatus` | Check plugin status |

---

## Category 1: Fuzzy File Finding & Searching

Fuzzy finding is one of the highest-leverage productivity improvements in Vim. Being able to jump to any file or search all code by text without leaving the editor eliminates context switching.

---

### 🥇 Option A: fzf.vim (Recommended)

**GitHub:** [https://github.com/junegunn/fzf.vim](https://github.com/junegunn/fzf.vim)
**fzf core:** [https://github.com/junegunn/fzf](https://github.com/junegunn/fzf)

fzf.vim wraps the blazing-fast `fzf` command-line fuzzy finder with Vim commands. Combined with `ripgrep`, it provides near-instant fuzzy search across files and file contents.

#### Installation

```vim
Plug 'junegunn/fzf', { 'do': { -> fzf#install() } }
Plug 'junegunn/fzf.vim'
```

#### System dependencies (install first)

```bash
# macOS
brew install fzf ripgrep bat

# Ubuntu/Debian
sudo apt install fzf ripgrep bat
```

#### Key commands

| Command | Description |
|---------|-------------|
| `:Files` | Fuzzy search files in project |
| `:Rg` | Live ripgrep search across file contents |
| `:Buffers` | Fuzzy search open buffers |
| `:Lines` | Fuzzy search lines in current buffer |
| `:BLines` | Fuzzy search lines in all open buffers |
| `:History` | Recently opened files |
| `:GFiles` | Git-tracked files only |

#### Recommended configuration

```vim
" Map common fzf commands
nnoremap <C-p> :Files<CR>
nnoremap <Leader>f :Rg<CR>
nnoremap <Leader>b :Buffers<CR>
nnoremap <Leader>h :History<CR>

" Use ripgrep as the default fzf source
let $FZF_DEFAULT_COMMAND = 'rg --files --hidden --follow --glob "!.git/*"'
```

#### Pros
- Extremely fast - driven by a compiled binary
- Supports live `ripgrep` content search (`:Rg`)
- Preview window with `bat` syntax highlighting
- Actively maintained by the same author as fzf
- Works on Vim 8.0+ and Neovim

#### Cons
- Requires external binaries (`fzf`, `ripgrep`, optionally `bat`)
- The `fzf` binary must be installed separately (though the plugin handles it automatically via `fzf#install()`)

---

### Option B: ctrlp.vim

**GitHub:** [https://github.com/ctrlpvim/ctrlp.vim](https://github.com/ctrlpvim/ctrlp.vim)

Pure VimScript fuzzy finder with zero external dependencies.

#### Installation

```vim
Plug 'ctrlpvim/ctrlp.vim'
```

#### Key commands

| Command | Description |
|---------|-------------|
| `<C-p>` | Open CtrlP file finder |
| `<C-f>` | Switch between modes (files/buffers/MRU) |

#### Pros
- Zero dependencies - works out of the box
- Easy to get started

#### Cons
- Noticeably slower than fzf on large codebases
- No native live content search (only filename matching)
- Less actively developed

---

### Comparison Summary: Fuzzy Finders

| Feature | fzf.vim | ctrlp.vim |
|---------|---------|-----------|
| Speed on large projects | Excellent | Slow |
| Dependencies | fzf binary + ripgrep | None |
| Content search | Yes (`:Rg`) | No |
| Preview window | Yes | Limited |
| Vim 8.0+ support | Yes | Yes |
| Recommendation | **Use this** | Fallback only |

**Verdict:** Use `fzf.vim`. The dependency on the `fzf` binary is not a real cost - it installs automatically and the speed difference is enormous on any project with more than a few hundred files.

---

## Category 2: Code Completion & LSP Support

For a .NET/C# developer, getting IntelliSense-level completion in Vim requires connecting to the OmniSharp language server. There are two main approaches.

---

### 🥇 Option A: omnisharp-vim (Recommended for Vim 8.0+)

**GitHub:** [https://github.com/OmniSharp/omnisharp-vim](https://github.com/OmniSharp/omnisharp-vim)

The official OmniSharp plugin for Vim. Uses the OmniSharp-roslyn stdio server asynchronously with no Python dependency on Vim 8.0+.

#### Installation

```vim
Plug 'OmniSharp/omnisharp-vim'
```

The first time you open a `.cs` file, the plugin will ask permission to download the OmniSharp-roslyn server automatically.

#### Required configuration

```vim
" Use stdio server (required for Vim 8.0+)
let g:OmniSharp_server_stdio = 1

" Use roslyn-based server
let g:OmniSharp_server_use_net6 = 1

" Key bindings
augroup omnisharp_commands
  autocmd!
  autocmd FileType cs nmap <silent> <buffer> gd <Plug>(omnisharp_go_to_definition)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osfu <Plug>(omnisharp_find_usages)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osfi <Plug>(omnisharp_find_implementations)
  autocmd FileType cs nmap <silent> <buffer> K <Plug>(omnisharp_documentation)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osca <Plug>(omnisharp_code_actions)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osrn <Plug>(omnisharp_rename)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osf <Plug>(omnisharp_code_format)
augroup END
```

#### Key features
- Go to definition (`gd`)
- Find usages
- Find implementations
- Code actions (quick fixes / refactors)
- Rename symbol
- Code formatting
- Solution-aware (detects `.sln` files automatically)
- Integrates with ALE for diagnostics

#### Pros
- First-class C# support powered by Roslyn
- Async with no Python dependency in Vim 8.0+
- Automatically installs the server binary

#### Cons
- C#-specific (you need a separate approach for other languages)
- Requires .NET SDK installed on the machine

---

### Option B: vim-lsp + asyncomplete.vim (General-purpose LSP)

**GitHub vim-lsp:** [https://github.com/prabirshrestha/vim-lsp](https://github.com/prabirshrestha/vim-lsp)
**GitHub asyncomplete:** [https://github.com/prabirshrestha/asyncomplete.vim](https://github.com/prabirshrestha/asyncomplete.vim)
**GitHub asyncomplete-lsp:** [https://github.com/prabirshrestha/asyncomplete-lsp.vim](https://github.com/prabirshrestha/asyncomplete-lsp.vim)

A lightweight, pure-VimScript LSP client that works with any language server. Pairs with `asyncomplete.vim` for completion.

#### Installation

```vim
Plug 'prabirshrestha/vim-lsp'
Plug 'prabirshrestha/asyncomplete.vim'
Plug 'prabirshrestha/asyncomplete-lsp.vim'
```

#### Configuration for C# with OmniSharp

```vim
if executable('OmniSharp')
  au User lsp_setup call lsp#register_server({
    \ 'name': 'omnisharp',
    \ 'cmd': {server_info -> ['OmniSharp', '-lsp']},
    \ 'root_uri': {server_info -> lsp#utils#path_to_uri(lsp#utils#find_nearest_parent_file_directory(lsp#utils#get_buffer_path(), '*.sln'))},
    \ 'allowlist': ['cs'],
    \ })
endif

" Key bindings
nmap <silent> gd <plug>(lsp-definition)
nmap <silent> gr <plug>(lsp-references)
nmap <silent> K <plug>(lsp-hover)
nmap <silent> <leader>rn <plug>(lsp-rename)
nmap <silent> <leader>ca <plug>(lsp-code-action)
```

#### Pros
- Works with any LSP server (not just OmniSharp)
- Pure VimScript - no Node.js dependency
- Lightweight

#### Cons
- Requires manual server configuration per language
- Less integrated completion UI than coc.nvim

---

### Option C: coc.nvim (Feature-rich, VSCode-like)

**GitHub:** [https://github.com/neoclide/coc.nvim](https://github.com/neoclide/coc.nvim)

coc.nvim brings VS Code's extension ecosystem into Vim. Very powerful but heavier - requires Node.js.

#### Installation

```vim
Plug 'neoclide/coc.nvim', {'branch': 'release'}
```

Then inside Vim:
```
:CocInstall coc-omnisharp
```

#### Pros
- Extremely powerful - closest to VS Code experience
- Has extensions for almost every language
- Rich popup completion UI

#### Cons
- Requires Node.js runtime (heavy dependency)
- Has its own configuration file (`coc-settings.json`), separate from `.vimrc`
- Heavier memory/CPU footprint
- `coc-omnisharp` extension has had maintenance gaps

---

### Option D: ALE (Asynchronous Lint Engine)

**GitHub:** [https://github.com/dense-analysis/ale](https://github.com/dense-analysis/ale)

ALE provides asynchronous linting AND acts as a lightweight LSP client. Best used alongside `omnisharp-vim` for diagnostics, or as a standalone linter.

#### Installation

```vim
Plug 'dense-analysis/ale'
```

#### Configuration for C#

```vim
" ALE fixers and linters
let g:ale_linters = {
\   'cs': ['OmniSharp'],
\}

let g:ale_fixers = {
\   '*': ['remove_trailing_lines', 'trim_whitespace'],
\   'cs': ['uncrustify'],
\}

" Auto-fix on save
let g:ale_fix_on_save = 1

" Show errors in sign column
let g:ale_sign_error = '>'
let g:ale_sign_warning = '-'
```

#### Pros
- Works with Vim 8.0+
- Integrates naturally with `omnisharp-vim`
- Supports fixers (auto-format on save)
- Very actively maintained

#### Cons
- Not a full LSP client (limited to linting + basic fixes)

---

### LSP Comparison Summary

| Plugin | Approach | Node.js required | C# quality | Vim 8.0+ |
|--------|----------|-----------------|------------|----------|
| omnisharp-vim | Direct OmniSharp | No | Excellent | Yes |
| vim-lsp | Generic LSP | No | Good | Yes |
| coc.nvim | VSCode extensions | Yes | Excellent | Yes |
| ALE | Linting + light LSP | No | Good (with OmniSharp) | Yes |

**Verdict:** Use `omnisharp-vim` as the primary C# tool, with `ALE` alongside it for asynchronous diagnostics.

---

## Category 3: Code Commenting & Formatting

---

### 🥇 Option A: vim-commentary (Recommended)

**GitHub:** [https://github.com/tpope/vim-commentary](https://github.com/tpope/vim-commentary)

Minimalist comment toggling by Tim Pope. Under 100 lines of VimScript. Does one thing perfectly.

#### Installation

```vim
Plug 'tpope/vim-commentary'
```

#### Key commands

| Command | Description |
|---------|-------------|
| `gcc` | Toggle comment on current line |
| `gc` + motion | Toggle comment over a motion (e.g. `gc3j` = 3 lines) |
| `gc` in visual | Toggle comment on selection |
| `gcap` | Comment out a paragraph |

#### Configuration

Usually none needed. To set comment string for a filetype:
```vim
autocmd FileType cs setlocal commentstring=//\ %s
```

#### Pros
- Extremely lightweight
- Respects filetype comment characters automatically
- Works perfectly with `.` repeat (especially with `vim-repeat`)
- Actively maintained

#### Cons
- No block comment support (only line comments)
- No "comment and duplicate line" feature

---

### Option B: nerdcommenter

**GitHub:** [https://github.com/preservim/nerdcommenter](https://github.com/preservim/nerdcommenter)

Feature-rich commenting plugin with support for block comments, alignment options, and more.

#### Installation

```vim
Plug 'preservim/nerdcommenter'
```

#### Key commands

| Command | Description |
|---------|-------------|
| `<Leader>cc` | Comment current line |
| `<Leader>cu` | Uncomment current line |
| `<Leader>c<space>` | Toggle comment |
| `<Leader>cs` | Sexy block comment |

#### Pros
- Supports block-style comments (`/* */`)
- More configuration options
- Good for XML/HTML block commenting

#### Cons
- Heavier than vim-commentary
- More complex configuration

---

### Comparison: vim-commentary vs nerdcommenter

| Feature | vim-commentary | nerdcommenter |
|---------|---------------|---------------|
| Line weight | ~80 lines | Hundreds |
| Block comments | No | Yes |
| Default key map | `gc` (intuitive) | `<Leader>cc` |
| `.` repeat | Yes | Partial |
| Recommendation | **Use this** | If block comments needed |

**Verdict:** Use `vim-commentary` for C# - line comments (`//`) are what you need 95% of the time.

---

### Bonus: vim-surround (Formatting adjacent)

**GitHub:** [https://github.com/tpope/vim-surround](https://github.com/tpope/vim-surround)

Not strictly a "formatter" but an essential productivity plugin for wrapping and changing surrounding characters (quotes, brackets, tags).

#### Installation

```vim
Plug 'tpope/vim-surround'
Plug 'tpope/vim-repeat'  " Makes surround changes repeatable with .
```

#### Key commands

| Command | Description |
|---------|-------------|
| `cs"'` | Change surrounding `"` to `'` |
| `ds"` | Delete surrounding `"` |
| `ysiw"` | Add `"` around word |
| `yss(` | Surround entire line with `()` |

---

## Category 4: Git Integration

---

### 🥇 vim-fugitive (Essential)

**GitHub:** [https://github.com/tpope/vim-fugitive](https://github.com/tpope/vim-fugitive)

The definitive Git plugin for Vim. Often described as "so awesome, it should be illegal." Provides a full Git workflow without leaving Vim.

#### Installation

```vim
Plug 'tpope/vim-fugitive'
```

#### Key commands

| Command | Description |
|---------|-------------|
| `:Git` or `:G` | Run any git command |
| `:Git status` | Interactive staging view |
| `:Git commit` | Commit in current Vim buffer |
| `:Git diff` | Show diff in a buffer |
| `:Git log` | Browse log in a buffer |
| `:Git blame` | Inline blame annotation |
| `:GBrowse` | Open current file on GitHub |
| `:Gdiffsplit` | Side-by-side diff |
| `:Gread` | Read the git version of file into buffer |
| `:Gwrite` | Stage current file |

#### Recommended key mappings

```vim
nnoremap <Leader>gs :Git<CR>
nnoremap <Leader>gc :Git commit<CR>
nnoremap <Leader>gd :Gdiffsplit<CR>
nnoremap <Leader>gb :Git blame<CR>
nnoremap <Leader>gl :Git log<CR>
nnoremap <Leader>gp :Git push<CR>
```

#### Pros
- Full git workflow from within Vim
- Interactive staging (stage/unstage individual hunks in `:Git status`)
- Diff viewing, blame, log all inside Vim buffers
- Rebase interactively inside Vim
- Actively maintained by Tim Pope

#### Cons
- Learning curve for the interactive staging view
- `:GBrowse` requires the `vim-rhubarb` companion plugin for GitHub

---

### vim-gitgutter (Sign column indicators)

**GitHub:** [https://github.com/airblade/vim-gitgutter](https://github.com/airblade/vim-gitgutter)

Shows git diff markers in the sign column (the narrow column to the left of line numbers). Indicates added, modified, and removed lines in real time.

#### Installation

```vim
Plug 'airblade/vim-gitgutter'
```

#### Configuration

```vim
" Update markers faster (default is 4000ms)
set updatetime=300

" Key bindings for hunk navigation
nmap ]h <Plug>(GitGutterNextHunk)
nmap [h <Plug>(GitGutterPrevHunk)
nmap <Leader>hs <Plug>(GitGutterStageHunk)
nmap <Leader>hu <Plug>(GitGutterUndoHunk)
nmap <Leader>hp <Plug>(GitGutterPreviewHunk)
```

#### Key features
- Real-time diff markers: `+` (added), `~` (changed), `-` (removed)
- Stage/unstage/undo individual hunks without leaving Vim
- Jump between hunks with `]h` / `[h`
- Works with Vim 8.0+ async jobs

#### Pros
- Visual, always-on feedback on what has changed
- Lightweight and unintrusive
- Pairs naturally with vim-fugitive

#### Cons
- Sign column must be visible (it is by default in most setups)

---

### Optional: vim-rhubarb (GitHub browser integration)

**GitHub:** [https://github.com/tpope/vim-rhubarb](https://github.com/tpope/vim-rhubarb)

Companion plugin to vim-fugitive that enables `:GBrowse` to open files on GitHub.

#### Installation

```vim
Plug 'tpope/vim-rhubarb'
```

---

## Category 5: Dark Theme & Modern Statusline

---

### Colorscheme: vim-code-dark (Recommended for .NET Developers)

**GitHub:** [https://github.com/tomasiser/vim-code-dark](https://github.com/tomasiser/vim-code-dark)

Dark colorscheme for Vim inspired by the Dark+ theme from Visual Studio Code. Familiar to any developer coming from VS Code or Visual Studio. Includes first-class `vim-airline` integration.

#### Installation

```vim
Plug 'tomasiser/vim-code-dark'
```

#### Configuration

```vim
colorscheme codedark
set background=dark
```

#### Optional customizations (add before `colorscheme`)

```vim
" Use italics for comments
let g:codedark_italics = 1

" Use a transparent terminal background
let g:codedark_transparent = 1

" Modern dark colors (slightly different palette)
let g:codedark_modern = 1
```

---

### Alternative Colorscheme: gruvbox

**GitHub:** [https://github.com/morhetz/gruvbox](https://github.com/morhetz/gruvbox)

Retro groove color scheme. Warm, earthy tones with excellent contrast. One of the most popular Vim themes of all time.

#### Installation

```vim
Plug 'morhetz/gruvbox'
```

#### Configuration

```vim
set background=dark
colorscheme gruvbox
" Optional: medium (default), hard, or soft contrast
let g:gruvbox_contrast_dark = 'hard'
```

---

### Statusline: vim-airline (Recommended)

**GitHub:** [https://github.com/vim-airline/vim-airline](https://github.com/vim-airline/vim-airline)
**Themes:** [https://github.com/vim-airline/vim-airline-themes](https://github.com/vim-airline/vim-airline-themes)

A lean, fast statusline and tabline for Vim. Loads in under 1 millisecond. Integrates with fugitive, gitgutter, ale, omnisharp, and your chosen colorscheme.

#### Installation

```vim
Plug 'vim-airline/vim-airline'
Plug 'vim-airline/vim-airline-themes'
```

#### Configuration

```vim
" Enable powerline-style separators (requires a patched/Nerd font)
let g:airline_powerline_fonts = 1

" Theme - use codedark to match the colorscheme, or 'dark' for generic
let g:airline_theme = 'codedark'

" Enable tab line (shows open buffers at top)
let g:airline#extensions#tabline#enabled = 1
let g:airline#extensions#tabline#formatter = 'unique_tail'

" Show git branch (uses vim-fugitive automatically if installed)
let g:airline#extensions#fugitiveline#enabled = 1

" Show ALE warnings/errors in statusline
let g:airline#extensions#ale#enabled = 1
```

#### What it shows in the statusline
- Current git branch (via fugitive)
- Linting errors/warnings count (via ALE)
- File encoding, format, type
- Line/column position
- Mode indicator (NORMAL / INSERT / VISUAL)

#### Powerline fonts

For the arrow-style separators, install a Nerd Font:
```
https://www.nerdfonts.com/font-downloads
```
Then set your terminal to use the patched font. If you prefer not to install a font, use:
```vim
let g:airline_powerline_fonts = 0
```
The statusline still looks good without it.

---

### vim-airline vs Powerline comparison

| Feature | vim-airline | Powerline |
|---------|------------|-----------|
| Language | Pure VimScript | Python |
| Speed | < 1ms load time | Heavier |
| Dependencies | None (fonts optional) | Python required |
| Cross-app support | Vim only | Vim, tmux, zsh, bash |
| Plugin integrations | Extensive | Limited |
| Vim 8.0+ | Yes | Yes |
| Recommendation | **Use this** | Only if you need tmux integration |

**Verdict:** Use `vim-airline`. Powerline's Python dependency and heavier footprint are not worth it for Vim-only use.

---

## Category 6: File Explorer

While fuzzy finding (fzf.vim) largely replaces the need for a persistent file tree, having a sidebar file browser is still useful for exploring unfamiliar codebases.

---

### 🥇 NERDTree

**GitHub:** [https://github.com/preservim/nerdtree](https://github.com/preservim/nerdtree)

The classic Vim file system explorer. Shows a tree view sidebar of your project directory.

#### Installation

```vim
Plug 'preservim/nerdtree'
```

#### Configuration

```vim
" Toggle NERDTree with Ctrl+n
nnoremap <C-n> :NERDTreeToggle<CR>

" Show hidden files
let NERDTreeShowHidden = 1

" Close tree when opening a file
let NERDTreeQuitOnOpen = 1

" Auto-close vim if NERDTree is the only open window
autocmd BufEnter * if tabpagenr('$') == 1 && winnr('$') == 1 && exists('b:NERDTree') && b:NERDTree.isTabTree() | quit | endif
```

#### Pros
- Familiar to developers from VS Code
- Easy to navigate

#### Cons
- Can conflict with split window widths
- Heavyweight compared to alternatives

---

### Alternative: vim-vinegar (enhances built-in netrw)

**GitHub:** [https://github.com/tpope/vim-vinegar](https://github.com/tpope/vim-vinegar)

Enhances Vim's built-in `netrw` file browser to make it much more usable with a single key press.

#### Installation

```vim
Plug 'tpope/vim-vinegar'
```

Press `-` in any buffer to open the directory of the current file. Press `-` again to go up a level.

#### Pros
- Zero overhead - enhances what is already in Vim
- Very fast
- Non-intrusive (no persistent sidebar)

#### Cons
- Less visual than NERDTree

---

## Complete .vimrc Template

Below is a full working `.vimrc` combining all recommended plugins from this guide.

```vim
" ============================================================
" vim-plug setup
" ============================================================
call plug#begin('~/.vim/plugged')

" -- Fuzzy finding --
Plug 'junegunn/fzf', { 'do': { -> fzf#install() } }
Plug 'junegunn/fzf.vim'

" -- C# / .NET LSP & completion --
Plug 'OmniSharp/omnisharp-vim'
Plug 'dense-analysis/ale'

" -- Commenting --
Plug 'tpope/vim-commentary'

" -- Surrounding characters --
Plug 'tpope/vim-surround'
Plug 'tpope/vim-repeat'

" -- Git --
Plug 'tpope/vim-fugitive'
Plug 'tpope/vim-rhubarb'
Plug 'airblade/vim-gitgutter'

" -- Statusline --
Plug 'vim-airline/vim-airline'
Plug 'vim-airline/vim-airline-themes'

" -- Colorscheme --
Plug 'tomasiser/vim-code-dark'

" -- File explorer --
Plug 'preservim/nerdtree'

call plug#end()

" ============================================================
" General settings
" ============================================================
set nocompatible
syntax enable
filetype plugin indent on
set number
set relativenumber
set cursorline
set hlsearch
set incsearch
set ignorecase
set smartcase
set tabstop=4
set shiftwidth=4
set expandtab
set clipboard=unnamed
set updatetime=300
set signcolumn=yes
set encoding=utf-8
set laststatus=2

" ============================================================
" Colorscheme
" ============================================================
let g:codedark_italics = 1
colorscheme codedark
set background=dark

" ============================================================
" vim-airline
" ============================================================
let g:airline_powerline_fonts = 1
let g:airline_theme = 'codedark'
let g:airline#extensions#tabline#enabled = 1
let g:airline#extensions#tabline#formatter = 'unique_tail'
let g:airline#extensions#ale#enabled = 1

" ============================================================
" fzf.vim
" ============================================================
let $FZF_DEFAULT_COMMAND = 'rg --files --hidden --follow --glob "!.git/*"'
nnoremap <C-p> :Files<CR>
nnoremap <Leader>f :Rg<CR>
nnoremap <Leader>b :Buffers<CR>
nnoremap <Leader>h :History<CR>
nnoremap <Leader>gf :GFiles<CR>

" ============================================================
" OmniSharp
" ============================================================
let g:OmniSharp_server_stdio = 1
let g:OmniSharp_server_use_net6 = 1

augroup omnisharp_commands
  autocmd!
  autocmd FileType cs nmap <silent> <buffer> gd <Plug>(omnisharp_go_to_definition)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osu <Plug>(omnisharp_find_usages)
  autocmd FileType cs nmap <silent> <buffer> <Leader>osi <Plug>(omnisharp_find_implementations)
  autocmd FileType cs nmap <silent> <buffer> K <Plug>(omnisharp_documentation)
  autocmd FileType cs nmap <silent> <buffer> <Leader>oca <Plug>(omnisharp_code_actions)
  autocmd FileType cs nmap <silent> <buffer> <Leader>orn <Plug>(omnisharp_rename)
  autocmd FileType cs nmap <silent> <buffer> <Leader>of <Plug>(omnisharp_code_format)
augroup END

" ============================================================
" ALE
" ============================================================
let g:ale_linters = {
\   'cs': ['OmniSharp'],
\}
let g:ale_fixers = {
\   '*': ['remove_trailing_lines', 'trim_whitespace'],
\}
let g:ale_fix_on_save = 1
let g:ale_sign_error = '>>'
let g:ale_sign_warning = '--'

" ============================================================
" vim-gitgutter
" ============================================================
nmap ]h <Plug>(GitGutterNextHunk)
nmap [h <Plug>(GitGutterPrevHunk)
nmap <Leader>hs <Plug>(GitGutterStageHunk)
nmap <Leader>hu <Plug>(GitGutterUndoHunk)
nmap <Leader>hp <Plug>(GitGutterPreviewHunk)

" ============================================================
" vim-fugitive
" ============================================================
nnoremap <Leader>gs :Git<CR>
nnoremap <Leader>gc :Git commit<CR>
nnoremap <Leader>gd :Gdiffsplit<CR>
nnoremap <Leader>gb :Git blame<CR>
nnoremap <Leader>gl :Git log<CR>
nnoremap <Leader>gp :Git push<CR>

" ============================================================
" NERDTree
" ============================================================
nnoremap <C-n> :NERDTreeToggle<CR>
let NERDTreeShowHidden = 1
let NERDTreeQuitOnOpen = 1
```

---

## Quick Reference: All Plugins

| Plugin | Category | GitHub |
|--------|----------|--------|
| fzf + fzf.vim | Fuzzy finding | [junegunn/fzf](https://github.com/junegunn/fzf) / [junegunn/fzf.vim](https://github.com/junegunn/fzf.vim) |
| omnisharp-vim | C# LSP | [OmniSharp/omnisharp-vim](https://github.com/OmniSharp/omnisharp-vim) |
| ale | Linting/LSP | [dense-analysis/ale](https://github.com/dense-analysis/ale) |
| vim-commentary | Commenting | [tpope/vim-commentary](https://github.com/tpope/vim-commentary) |
| vim-surround | Text objects | [tpope/vim-surround](https://github.com/tpope/vim-surround) |
| vim-repeat | Repeat support | [tpope/vim-repeat](https://github.com/tpope/vim-repeat) |
| vim-fugitive | Git | [tpope/vim-fugitive](https://github.com/tpope/vim-fugitive) |
| vim-rhubarb | GitHub browser | [tpope/vim-rhubarb](https://github.com/tpope/vim-rhubarb) |
| vim-gitgutter | Git signs | [airblade/vim-gitgutter](https://github.com/airblade/vim-gitgutter) |
| vim-airline | Statusline | [vim-airline/vim-airline](https://github.com/vim-airline/vim-airline) |
| vim-airline-themes | Themes | [vim-airline/vim-airline-themes](https://github.com/vim-airline/vim-airline-themes) |
| vim-code-dark | Colorscheme | [tomasiser/vim-code-dark](https://github.com/tomasiser/vim-code-dark) |
| NERDTree | File explorer | [preservim/nerdtree](https://github.com/preservim/nerdtree) |

---

## Sources

- [Better fuzzy-finding in Vim - Source Diving](https://sourcediving.com/better-fuzzy-finding-in-vim-2f1e8597b3b9)
- [fzf.vim GitHub](https://github.com/junegunn/fzf.vim)
- [junegunn/fzf GitHub](https://github.com/junegunn/fzf)
- [OmniSharp/omnisharp-vim GitHub](https://github.com/OmniSharp/omnisharp-vim)
- [prabirshrestha/vim-lsp GitHub](https://github.com/prabirshrestha/vim-lsp)
- [prabirshrestha/asyncomplete.vim GitHub](https://github.com/prabirshrestha/asyncomplete.vim)
- [Comparison to other LSP ecosystems (coc, vim-lsp) - neovim wiki](https://github.com/neovim/nvim-lspconfig/wiki/Comparison-to-other-LSP-ecosystems-(CoC,-vim-lsp,-etc.))
- [dense-analysis/ale GitHub](https://github.com/dense-analysis/ale)
- [tpope/vim-commentary GitHub](https://github.com/tpope/vim-commentary)
- [preservim/nerdcommenter GitHub](https://github.com/preservim/nerdcommenter)
- [tpope/vim-fugitive GitHub](https://github.com/tpope/vim-fugitive)
- [airblade/vim-gitgutter GitHub](https://github.com/airblade/vim-gitgutter)
- [vim-airline/vim-airline GitHub](https://github.com/vim-airline/vim-airline)
- [vim-airline/vim-airline-themes GitHub](https://github.com/vim-airline/vim-airline-themes)
- [tomasiser/vim-code-dark GitHub](https://github.com/tomasiser/vim-code-dark)
- [morhetz/gruvbox GitHub](https://github.com/morhetz/gruvbox)
- [preservim/nerdtree GitHub](https://github.com/preservim/nerdtree)
- [tpope/vim-surround GitHub](https://github.com/tpope/vim-surround)
- [junegunn/vim-plug GitHub](https://github.com/junegunn/vim-plug)
- [Vim and Language Server Protocol - Vim From Scratch](https://www.vimfromscratch.com/articles/vim-and-language-server-protocol)
- [My Vim Configuration 2025 - Nickolas Kraus](https://nickolaskraus.io/posts/my-vim-configuration-2025/)
