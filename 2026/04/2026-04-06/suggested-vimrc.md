# suggested-vimrc

## Suggested VIM

```vim
" ---------- Core ----------
set nocompatible
syntax on
filetype plugin indent on

let mapleader = " "

" ---------- UI ----------
set number
set relativenumber
set ruler
set showcmd
set wildmenu
set hidden
set scrolloff=5
set signcolumn=yes

" ---------- Editing ----------
set backspace=indent,eol,start
set clipboard=unnamedplus
set mouse=a

" ---------- Search ----------
set ignorecase
set smartcase
set incsearch
set hlsearch

" Clear search highlight quickly
nnoremap <Esc><Esc> :nohlsearch<CR>

" ---------- Indentation ----------
set expandtab
set shiftwidth=4
set softtabstop=4
set tabstop=4
set smartindent

" ---------- Splits ----------
set splitbelow
set splitright

" ---------- Undo ----------
if has('persistent_undo')
  set undofile
endif

" ---------- 🚫 Kill comment continuation ----------
augroup NoCommentContinuation
  autocmd!
  autocmd BufEnter * setlocal formatoptions-=r formatoptions-=o formatoptions-=c
augroup END

" ---------- Quick save ----------
nnoremap <leader>w :write<CR>

" ---------- Window navigation ----------
nnoremap <C-h> <C-w>h
nnoremap <C-j> <C-w>j
nnoremap <C-k> <C-w>k
nnoremap <C-l> <C-w>l

" ---------- Quickfix navigation ----------
nnoremap ]q :cnext<CR>
nnoremap [q :cprev<CR>
nnoremap <leader>co :copen<CR>
nnoremap <leader>cc :cclose<CR>

" ---------- Grep (ripgrep) ----------
if executable('rg')
  set grepprg=rg\ --vimgrep\ --no-heading\ --smart-case
  set grepformat=%f:%l:%c:%m
endif

" Run grep and open quickfix
nnoremap <leader>rg :grep<Space>

" ---------- fd integration ----------
command! -nargs=1 Find execute 'cexpr system("fd --type f " . shellescape(<q-args>))' | copen

" ---------- Open all quickfix files ----------
command! Qargs execute 'args ' . join(map(getqflist(), 'bufname(v:val.bufnr)'))
nnoremap <leader>qa :Qargs<CR>

" ---------- Markdown ----------
autocmd FileType markdown setlocal wrap linebreak nolist
autocmd FileType markdown setlocal spell

" Jump between headings
nnoremap ]] /^\s*#<CR>
nnoremap [[ ?^\s*#<CR>

" ---------- Git (works great with fugitive) ----------
nnoremap <leader>gs :Git<CR>
nnoremap <leader>gl :Git log --oneline --graph --decorate --all<CR>

" ---------- Reload config ----------
nnoremap <leader>vr :source $MYVIMRC<CR>

nnoremap <leader>ve :edit $MYVIMRC<CR>
```
