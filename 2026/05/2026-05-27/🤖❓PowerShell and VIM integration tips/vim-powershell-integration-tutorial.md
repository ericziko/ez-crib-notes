---
uid: 182d47bc-a536-4e16-a55f-6259a1dad1ca
title: "Vim + PowerShell on Windows — A Practical Integration Tutorial"
created: 2026-05-28T00:48:20Z
modified: 2026-05-28T00:48:20Z
tags:
  - vim
  - powershell
  - pwsh7
  - windows
  - quickfix
  - nerd-fonts
  - tutorial
---

# 🤖💡 Vim + PowerShell on Windows — A Practical Integration Tutorial

> A task-oriented companion to [`🤖❓PowerShell and VIM integration tips.md`](./🤖❓PowerShell%20and%20VIM%20integration%20tips.md).
> Targets **classic Vim** (vim.org build, 8.2+/9.x, `+terminal`) and **PowerShell 7** (`pwsh`) on Windows.

This guide is organized around *what you want to do*, not around the original question list. Skim the cheat sheet at the end first if you just want the commands.

---

## 🧭 Conventions used here

| Symbol | Meaning |
| --- | --- |
| `pwsh` | PowerShell 7+ (`C:\Program Files\PowerShell\7\pwsh.exe`). `powershell` is Windows PowerShell 5.1 — avoid it for this guide. |
| `:cmd` | A Vim **Ex command** (typed after `:`). |
| `<leader>` | Your Vim leader key (`\` by default; many set it to `Space`). |
| `~/_vimrc` | Your Vim config. On Windows classic Vim this is `%USERPROFILE%\_vimrc` (or `$HOME\.vimrc`). |
| `$PROFILE` | Your PowerShell profile. Run `notepad $PROFILE` (or `vim $PROFILE`) to edit. |

> **Check your build first.** Run `:version` in Vim and confirm you see `+terminal`, `+job`, `+channel`, `+clipboard`, and (for GVim true-color) `+gui`. The official Windows "huge" build has all of these.

---

## ⚙️ 1. Setup — make PowerShell 7 Vim's shell

By default Vim on Windows shells out to `cmd.exe`. You can point it at `pwsh` so that `:!`, `:r !`, `system()`, `:make`, and `:terminal` all use PowerShell 7.

> ⚠️ **Read this before you do it.** Setting `'shell'` to `pwsh` *globally* breaks plugins that assume a POSIX `sh` or `cmd.exe` (notably **fzf**, **fugitive**, and anything calling `system('...')` with POSIX syntax). You have two sane strategies:
>
> 1. **Global pwsh** (best if you barely use those plugins) — set it and fix breakage as it appears.
> 2. **Keep `shell=cmd`, call pwsh explicitly** in `makeprg`, mappings, and filters (most robust). The rest of this guide works either way.

### The robust global recipe (handles UTF-8 correctly)

Use the `let &option = ...` form instead of `set option=...`. It avoids Vim's painful backslash-escaping of spaces and lets you force UTF-8 in/out, which is what fixes garbled output and broken quickfix parsing.

```vim
" ~/_vimrc — PowerShell 7 as Vim's shell (UTF-8 clean)
if has('win32') && executable('pwsh')
  set shell=pwsh
  set shellquote=
  set shellxquote=
  " -NoProfile keeps startup fast & predictable; force UTF-8 console encoding.
  let &shellcmdflag = '-NoLogo -NoProfile -ExecutionPolicy RemoteSigned -Command ' .
        \ '[Console]::InputEncoding=[Console]::OutputEncoding=[Text.UTF8Encoding]::new();' .
        \ '$PSDefaultParameterValues[''Out-File:Encoding'']=''utf8'';'
  " Tee output to the temp file (%s) AND the screen for :make / :! ... | capture.
  let &shellredir = '2>&1 | %%{ "$_" } | Out-File %s; exit $LastExitCode'
  let &shellpipe  = '2>&1 | %%{ "$_" } | Tee-Object %s; exit $LastExitCode'
endif
```

Why each line matters:

- **`shellquote` / `shellxquote` emptied** — PowerShell does its own quoting; Vim's cmd-style quoting corrupts arguments.
- **`%%{ "$_" }`** — the doubled `%` escapes Vim's `%` (current file). `%{ "$_" }` re-emits each line as a clean string so redirection captures text, not objects.
- **`exit $LastExitCode`** — lets `:make` know whether the build/lint failed.
- **`Out-File`/`Tee-Object` with UTF-8** — without this, accented chars and box-drawing glyphs become mojibake in the quickfix list.

### Keep-cmd-as-shell alternative

If you'd rather not risk plugin breakage, leave `'shell'` alone and just invoke pwsh where you need it:

```vim
" Run the current file in pwsh, blocking, see output in a pager-style buffer
nnoremap <leader>r :!pwsh -NoProfile -File "%"<CR>
```

---

## 🎯 2. Pipe PowerShell output into the Quickfix list

The **quickfix list** is Vim's jump-to-location list: each entry has a file, line, column, and message, and you walk them with `:cnext`/`:cprev`. It is the single most valuable Vim/PowerShell integration — it turns "lint/test/build output" into clickable navigation.

### 2a. The pieces

| Command | What it does |
| --- | --- |
| `:cexpr {expr}` | Parse a string/list (e.g. `system(...)`) into quickfix **and jump** to first entry. |
| `:cgetexpr {expr}` | Same, but **don't** jump. |
| `:cbuffer` | Parse the **current buffer's** lines into quickfix. |
| `:cfile {file}` | Parse a **file** into quickfix. |
| `:cexpr systemlist('…')` | Run a shell command and parse its lines. |
| `:make` | Run `'makeprg'`, capture output via `'shellpipe'`, parse with `'errorformat'`. |
| `:copen` / `:cclose` | Open/close the quickfix window. |
| `:cnext` `:cprev` `:cfirst` `:clast` | Navigate entries. |

The magic glue is **`'errorformat'`** — a scanf-like pattern that tells Vim how to read file/line/col/message out of each text line. Key tokens: `%f` file, `%l` line, `%c` column, `%m` message, `%t` type (`e`/`w`/`i`), `%*[^:]` "skip non-colons".

### 2b. PSScriptAnalyzer → quickfix (the classic loop)

Make PSScriptAnalyzer emit one `file:line:col: severity: message` line per finding, then teach Vim to parse it.

**Reusable emitter** — drop this function in `$PROFILE`:

```powershell
# $PROFILE — emit PSScriptAnalyzer results in a Vim-friendly grep format
function Invoke-PSSAVim {
    param([string]$Path = '.')
    Invoke-ScriptAnalyzer -Path $Path -Recurse |
        ForEach-Object {
            '{0}:{1}:{2}: {3}: {4}' -f `
                $_.ScriptPath, $_.Line, $_.Column, $_.Severity, $_.Message
        }
}
Set-Alias pssa Invoke-PSSAVim
```

**Wire it into Vim** (`~/_vimrc`):

```vim
" PSScriptAnalyzer as the 'make' program for *.ps1
augroup ps1_make
  autocmd!
  autocmd FileType ps1 setlocal
        \ makeprg=pwsh\ -NoProfile\ -Command\ \"Invoke-PSSAVim\ -Path\ '%'\"
        \ errorformat=%f:%l:%c:\ %t%*[^:]:\ %m,%f:%l:%c:\ %m
augroup END
```

Now the loop is:

```
:make          " run PSScriptAnalyzer on the current file
:copen         " see all findings
]q / [q        " jump next/prev (see mappings in §6) — or :cnext / :cprev
```

> The first `errorformat` branch maps `%t` from `Error`/`Warning`/`Information` to Vim's `e`/`w`/`i` because they start with distinct letters — Vim only reads the first char for `%t`.

### 2c. One-shot capture without `:make`

```vim
" Lint the whole repo into quickfix, don't jump
:cgetexpr systemlist('pwsh -NoProfile -Command "Invoke-PSSAVim -Path ."')
:copen
```

### 2d. Pester test failures → quickfix

```powershell
# $PROFILE — Pester failures as file:line: message
function Invoke-PesterVim {
    $r = Invoke-Pester -PassThru -Output None
    foreach ($t in $r.Failed) {
        '{0}:{1}: {2}' -f `
            $t.ErrorRecord.TargetObject.File,
            $t.ErrorRecord.TargetObject.Line,
            $t.ErrorRecord.DisplayErrorMessage
    }
}
```

```vim
:cgetexpr systemlist('pwsh -NoProfile -Command "Invoke-PesterVim"')
:copen
```

### 2e. From the PowerShell side: launch Vim *into* quickfix

`vim -q {errorfile}` starts Vim already in quickfix mode:

```powershell
Invoke-PSSAVim -Path . | Out-File -Encoding utf8 errors.txt
vim -q errors.txt        # opens at the first finding
```

Or do it in one shot with a here-string and process substitution-style temp file:

```powershell
function Edit-LintErrors {
    $tmp = New-TemporaryFile
    Invoke-PSSAVim -Path . | Out-File -Encoding utf8 $tmp
    vim -q $tmp
    Remove-Item $tmp
}
```

> Generic rule: **any** tool that prints `file:line:col: message` (ripgrep with `--vimgrep`, `dotnet build`, `tsc`, ...) can feed `:cexpr`/`vim -q` with the right `errorformat`. `:grep`/`:vimgrep` populate quickfix too.

---

## 🛠️ 3. Use Vim as a PowerShell development environment

Three layers, lightest to heaviest:

### 3a. Syntax, indent, folding (zero servers)

- Vim ships basic `ps1` syntax. For *good* highlighting + indent install **[PProvost/vim-ps1](https://github.com/PProvost/vim-ps1)**.
- Force the filetype when needed: `:setf ps1` (or it's auto by `.ps1`/`.psm1`/`.psd1`).

### 3b. Linting + fix-on-save with ALE (async, no LSP required)

**[dense-analysis/ale](https://github.com/dense-analysis/ale)** runs PSScriptAnalyzer in the background and shows findings in the gutter as you type:

```vim
" ~/_vimrc — ALE for PowerShell
let g:ale_linters = { 'ps1': ['psscriptanalyzer'] }
let g:ale_fixers  = { 'ps1': ['powershell_formatter'] }   " uses Invoke-Formatter
let g:ale_fix_on_save = 1
let g:ale_ps1_psscriptanalyzer_executable = 'pwsh'
```

### 3c. Full IDE: LSP via PowerShellEditorServices

The same engine behind the VS Code PowerShell extension — completion, hover, go-to-definition, rename, signature help — is **[PowerShell/PowerShellEditorServices](https://github.com/PowerShell/PowerShellEditorServices)** (PSES). Wire it to classic Vim with **[prabirshrestha/vim-lsp](https://github.com/prabirshrestha/vim-lsp)** + **[mattn/vim-lsp-settings](https://github.com/mattn/vim-lsp-settings)**:

```vim
" ~/_vimrc — minimal vim-lsp wiring
function! s:on_lsp_buffer_enabled() abort
  setlocal omnifunc=lsp#complete
  nmap <buffer> gd <plug>(lsp-definition)
  nmap <buffer> gr <plug>(lsp-references)
  nmap <buffer> K  <plug>(lsp-hover)
  nmap <buffer> <leader>rn <plug>(lsp-rename)
endfunction
augroup lsp_install
  autocmd!
  autocmd User lsp_buffer_enabled call s:on_lsp_buffer_enabled()
augroup END
```

Then inside a `.ps1` buffer run **`:LspInstallServer`** (vim-lsp-settings auto-installs `powershell-es`). Completion is `<C-x><C-o>` (omni) unless you add an async completion plugin (asyncomplete.vim).

> Prefer batteries-included and on Vim 9? **[yegappan/lsp](https://github.com/yegappan/lsp)** is a pure-Vim9 LSP client that also drives PSES.

### 3d. Run / debug from the editor

```vim
" Run current file in a split terminal (PowerShell 7), reuse the same window
nnoremap <leader>x :term ++rows=15 pwsh -NoProfile -File "%"<CR>
" Run current *selection* (visual) by writing it to pwsh stdin — see §5
```

Vim has no native PowerShell debugger; for breakpoint debugging stay in VS Code. For everything else (edit → lint → run → test) the loop above is fast and entirely in Vim.

---

## 🖥️ 4. Terminal-in-Vim, Nerd Fonts & emoji

### 4a. `:terminal` basics

```vim
:term pwsh                 " open a PowerShell 7 terminal in a split
:term ++close pwsh         " auto-close the window when pwsh exits
:vert term pwsh            " vertical split
```

- **Terminal-Normal mode:** `<C-w>N` (or `<C-w>:`) to scroll/copy with Vim motions; `i` to return to typing.
- **Switch windows from terminal:** `<C-w>` then `h/j/k/l`.
- Make it open `pwsh` by default: `set shell=pwsh` (see §1) so bare `:term` launches PowerShell 7.

### 4b. Encoding — the foundation for glyphs

Glyphs only render if **both** Vim and the host agree on UTF-8:

```vim
" ~/_vimrc
set encoding=utf-8
set fileencoding=utf-8
scriptencoding utf-8
set termguicolors          " 24-bit color in :terminal / modern terminals
```

```powershell
# $PROFILE — make pwsh speak UTF-8
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding =
    [System.Text.UTF8Encoding]::new()
$PSDefaultParameterValues['*:Encoding'] = 'utf8'
```

### 4c. Where the font actually lives (this trips everyone up)

| You're running… | Font is controlled by… | Setting |
| --- | --- | --- |
| **GVim** | Vim itself | `set guifont=...` |
| **Console Vim** inside Windows Terminal | **Windows Terminal**, *not* Vim | WT profile → Appearance → Font face |
| **Console Vim** in conhost | conhost properties | Right-click title bar → Properties → Font |

For GVim with a [Nerd Font](https://www.nerdfonts.com/) (download a patched font like *JetBrainsMono Nerd Font*):

```vim
" Windows GVim — note the colon-h size syntax and escaped spaces
set guifont=JetBrainsMono\ NFM:h11
" Fallback chain (Vim tries each until one exists):
set guifont=JetBrainsMono\ NFM:h11,Cascadia\ Code\ PL:h11,Consolas:h11
```

For **console Vim**, set the Nerd Font in your **Windows Terminal profile** for the PowerShell 7 profile — Vim has no say.

### 4d. Glyph *width* — the difference between aligned and garbled

Nerd Font icons and many emoji are "ambiguous/double width." Two options govern this:

```vim
set ambiwidth=single   " treat ambiguous-width chars as 1 cell (usual best for Nerd Fonts)
" set ambiwidth=double " try this if box-drawing/icons overlap text
set emoji              " (default on) emoji counted as width 2 — matches most terminals
" set noemoji          " if your terminal renders emoji as width 1 and columns drift
```

> Rule of thumb: if **icons overlap the next character**, flip `ambiwidth`. If **emoji push the cursor too far / not far enough**, flip `emoji`. The "correct" pair depends on your terminal's own width tables, so test with a line like `  ` and a few emoji.

### 4e. Statusline & file icons

- **[vim-airline/vim-airline](https://github.com/vim-airline/vim-airline)** + `let g:airline_powerline_fonts = 1` gives the powerline separators (needs a Nerd/Powerline font).
- **[ryanoasis/vim-devicons](https://github.com/ryanoasis/vim-devicons)** adds filetype glyphs in NERDTree/airline/fzf (load it **last**; needs a Nerd Font).

---

## 🔁 5. Round-trip text between Vim and PowerShell

This is where Vim shines: treat any PowerShell command as a **text filter**.

### 5a. PowerShell → Vim (read output in)

```vim
:r !pwsh -NoProfile -c "Get-Process | Sort-Object CPU -Descending | Select -First 10"
"   ^ inserts command output below the cursor
:0r !pwsh -NoProfile -c "Get-Date"   " insert at top of file
```

From the **PowerShell side**, pipe straight into a new Vim buffer with the `-` stdin trick:

```powershell
Get-Process | Out-String | vim -          # '-' = read stdin into a [No Name] buffer
Get-ChildItem | vim -                      # browse a dir listing in Vim
```

### 5b. Vim → PowerShell → Vim (filter in place)

The `!` filter operator sends lines to a command's **stdin** and **replaces** them with stdout. In PowerShell, the incoming text is the `$input` enumerator.

```vim
" Filter the WHOLE buffer through a pwsh pipeline
:%!pwsh -NoProfile -c "$input | Sort-Object"

" Pretty-print the buffer as JSON
:%!pwsh -NoProfile -c "$input | ConvertFrom-Json | ConvertTo-Json -Depth 10"

" Filter a VISUAL selection (Vim inserts :'<,'> for you when you press ! in visual mode)
:'<,'>!pwsh -NoProfile -c "$input | Where-Object { $_ -match 'ERROR' }"

" Normal-mode shortcuts:
"   !!         filter the current line
"   !ap        filter a paragraph (operator + 'ap' text object)
```

### 5c. Vim → PowerShell (send without replacing)

```vim
:w !pwsh -NoProfile -c "$input | Measure-Object -Line -Word"
"   writes buffer to the command's stdin; output shown in a pager, buffer untouched
:'<,'>w !pwsh -NoProfile -c "$input | Set-Clipboard"   " copy selection via PS
```

### 5d. Vimscript helpers for scripted round-trips

```vim
" Capture command output as a string / list inside mappings & functions
let g:branch = system('pwsh -NoProfile -c "git rev-parse --abbrev-ref HEAD"')
let g:files  = systemlist('pwsh -NoProfile -c "Get-ChildItem -Name *.ps1"')
```

### 5e. PowerShell using Vim as `$EDITOR` (blocking edits)

Many tools (git, `Edit-...` helpers) shell out to an editor and **wait**. GVim forks by default, so pass `-f` (no-fork) / use console `vim`:

```powershell
# $PROFILE
$env:EDITOR = 'vim'              # console vim blocks naturally
$env:GIT_EDITOR = 'vim'
# If you insist on GVim:  $env:EDITOR = 'gvim -f'

# Edit a variable's contents through Vim and read it back
function Edit-Text {
    param([Parameter(ValueFromPipeline)] [string[]] $InputObject)
    $tmp = New-TemporaryFile
    $input | Set-Content -Encoding utf8 $tmp
    vim $tmp                      # blocks until you :wq
    Get-Content -Raw $tmp
    Remove-Item $tmp
}
# usage:  $cleaned = Get-Content big.log | Edit-Text
```

### 5f. Clipboard register (skip temp files)

With `+clipboard` in `:version`, `"+y`/`"+p` use the Windows clipboard. Pair with `Get-Clipboard`/`Set-Clipboard`:

```vim
"+yy            " yank current line to Windows clipboard
:'<,'>w !pwsh -NoProfile -c "$input | Set-Clipboard"
```

---

## 🚀 6. Fast everyday workflows

### 6a. Quickfix navigation mappings (steal these)

```vim
" ~/_vimrc — tpope-style quickfix hops
nnoremap ]q :cnext<CR>
nnoremap [q :cprev<CR>
nnoremap ]Q :clast<CR>
nnoremap [Q :cfirst<CR>
nnoremap <leader>q :copen<CR>
nnoremap <leader>m :make<CR>
```

### 6b. The lint → fix → re-lint inner loop

```
:w            " save
:make         " PSScriptAnalyzer (or :ALELint if using ALE)
]q ]q ]q      " walk findings, fix as you go
:make         " confirm clean (empty quickfix = green)
```

### 6c. Send code to a live REPL with vim-slime

**[jpalardy/vim-slime](https://github.com/jpalardy/vim-slime)** ships a selection/paragraph to a running REPL — perfect for iterating on a pipeline without leaving the editor. With Vim's built-in terminal:

```vim
let g:slime_target = "vimterminal"
let g:slime_vimterminal_cmd = "pwsh -NoProfile"
" Then: open a script, C-c C-c to send the paragraph to the pwsh terminal
```

Workflow: split a `:term pwsh`, write a snippet in your buffer, `C-c C-c` to execute it in the live session, tweak, repeat. The session keeps state (variables, modules) between sends.

### 6d. Fuzzy-find files & commands with fzf

**[junegunn/fzf](https://github.com/junegunn/fzf)** + **[fzf.vim](https://github.com/junegunn/fzf.vim)**: `:Files`, `:Rg` (ripgrep), `:Buffers`, `:History`. 

> ⚠️ fzf shells out and expects `cmd`/`sh`, so if you set `shell=pwsh` globally (§1) and fzf misbehaves, scope cmd back just for it: `let $FZF_DEFAULT_COMMAND = 'rg --files'` and/or temporarily `set shell=cmd.exe` around fzf, or use the keep-cmd strategy.

### 6e. Scratch-buffer REPL pattern (no plugins)

```vim
" Open a throwaway buffer, paste a pipeline, then run it through pwsh in place:
:enew | setlocal buftype=nofile
" ...type/paste a pipeline...
:%!pwsh -NoProfile -c "$input | Invoke-Expression"   " see results replace the buffer
```

### 6f. PSReadLine "edit-in-Vim" at the prompt

PSReadLine can hand the *current command line* to your editor. Add to `$PROFILE`:

```powershell
Set-PSReadLineOption -EditMode Vi          # optional: vi keybindings at the prompt
# In Vi command mode press 'v' to edit the current command in $env:EDITOR (Vim)
```

This gives you full Vim editing for gnarly one-liners, then runs them on `:wq`.

---

## 🔌 7. Recommended plugins & downloads

Install with your manager of choice — **[vim-plug](https://github.com/junegunn/vim-plug)** is the simplest:

```vim
call plug#begin('~/vimfiles/plugged')   " ~/.vim/plugged on *nix
  Plug 'PProvost/vim-ps1'                " PowerShell syntax/indent
  Plug 'dense-analysis/ale'              " async lint/fix (PSScriptAnalyzer)
  Plug 'prabirshrestha/vim-lsp'          " LSP client (classic Vim 8+)
  Plug 'mattn/vim-lsp-settings'          " auto-installs powershell-es
  Plug 'prabirshrestha/asyncomplete.vim' " async completion popup
  Plug 'prabirshrestha/asyncomplete-lsp.vim'
  Plug 'jpalardy/vim-slime'              " send code to a REPL/terminal
  Plug 'junegunn/fzf', { 'do': { -> fzf#install() } }
  Plug 'junegunn/fzf.vim'                " :Files :Rg :Buffers
  Plug 'tpope/vim-fugitive'              " git (NOTE: prefers shell=cmd/sh)
  Plug 'vim-airline/vim-airline'         " statusline (powerline fonts)
  Plug 'ryanoasis/vim-devicons'          " filetype glyphs (load LAST)
call plug#end()
```

**Links worth bookmarking**

| Thing | Link |
| --- | --- |
| PowerShell syntax/indent | https://github.com/PProvost/vim-ps1 |
| ALE (lint/fix) | https://github.com/dense-analysis/ale |
| vim-lsp / settings | https://github.com/prabirshrestha/vim-lsp · https://github.com/mattn/vim-lsp-settings |
| Pure-Vim9 LSP client | https://github.com/yegappan/lsp |
| PowerShellEditorServices (the LSP server) | https://github.com/PowerShell/PowerShellEditorServices |
| vim-slime (REPL send) | https://github.com/jpalardy/vim-slime |
| fzf / fzf.vim | https://github.com/junegunn/fzf · https://github.com/junegunn/fzf.vim |
| Nerd Fonts | https://www.nerdfonts.com/ |
| vim-devicons | https://github.com/ryanoasis/vim-devicons |
| vim-airline | https://github.com/vim-airline/vim-airline |
| PSScriptAnalyzer | https://github.com/PowerShell/PSScriptAnalyzer |
| Pester | https://pester.dev/ |
| vim-plug (manager) | https://github.com/junegunn/vim-plug |

> Don't install all of these on day one. Start with **vim-ps1 + ALE**, add **vim-lsp** when you want completion, add **vim-slime + fzf** when the inner loop feels slow.

---

## 📋 8. Cheat sheet

### Quickfix

| Action | Command |
| --- | --- |
| Run linter/build | `:make` |
| Capture cmd → quickfix (jump) | `:cexpr systemlist('pwsh -c "…"')` |
| Capture cmd → quickfix (no jump) | `:cgetexpr systemlist('pwsh -c "…"')` |
| Current buffer → quickfix | `:cbuffer` |
| File → quickfix | `:cfile errors.txt` |
| Open/close list | `:copen` / `:cclose` |
| Next / prev / first / last | `:cnext` `:cprev` `:cfirst` `:clast` |
| Launch Vim into a list (from pwsh) | `vim -q errors.txt` |

### Text round-trip

| Action | Command |
| --- | --- |
| Read cmd output into buffer | `:r !pwsh -c "…"` |
| Pipe pwsh output into a new buffer | `… | vim -` (PowerShell side) |
| Filter whole buffer | `:%!pwsh -c "$input | …"` |
| Filter selection | `:'<,'>!pwsh -c "$input | …"` |
| Filter current line / paragraph | `!!` / `!ap` |
| Send to cmd, keep buffer | `:w !pwsh -c "$input | …"` |
| Yank to Windows clipboard | `"+yy` |

### Terminal & shell

| Action | Command |
| --- | --- |
| Open pwsh terminal | `:term pwsh` (or `:vert term pwsh`) |
| Auto-close on exit | `:term ++close pwsh` |
| Terminal → copy/scroll mode | `<C-w>N` |
| Back to typing | `i` |
| Run current file | `:!pwsh -NoProfile -File "%"` |

### Glyphs / fonts

| Symptom | Knob |
| --- | --- |
| Garbled accents/box chars | `set encoding=utf-8` + pwsh UTF-8 (`§4b`) |
| Need Nerd Font in GVim | `set guifont=JetBrainsMono\ NFM:h11` |
| Need Nerd Font in console Vim | set it in **Windows Terminal** profile |
| Icons overlap text | toggle `set ambiwidth=single`/`double` |
| Emoji columns drift | toggle `set emoji`/`noemoji` |
| Want 24-bit color | `set termguicolors` |

### `$PROFILE` snippets to keep

```powershell
# UTF-8 everywhere
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [Text.UTF8Encoding]::new()
$env:EDITOR = 'vim'; $env:GIT_EDITOR = 'vim'
Set-Alias pssa Invoke-PSSAVim          # PSScriptAnalyzer → file:line:col format (§2b)
```

---

## ✅ TL;DR starting point

1. Add the **UTF-8 `$PROFILE` block** (§4b) and the **`Invoke-PSSAVim` emitter** (§2b).
2. Add **`encoding=utf-8`, `termguicolors`**, and the **`ps1` `makeprg`/`errorformat`** to `~/_vimrc` (§2b/§4b).
3. Install **vim-ps1 + ALE** (§3, §7).
4. Learn the loop: `:make` → `:copen` → `]q` (§6b), and the filter `:%!pwsh -c "$input | …"` (§5b).
5. Add **vim-lsp** for completion and **vim-slime + fzf** once you want more speed.

Everything else in this guide is incremental polish on top of those five steps.
