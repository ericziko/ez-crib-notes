" ------------------------------------------------
" Begin Install of Vundle Plugin
" See https://github.com/VundleVim/Vundle.vim"
" ------------------------------------------------
set encoding=utf-8
set nocompatible              " be iMproved, required
set hidden
filetype off                  " required
" Installed VIM Plug
" https://github.com/junegunn/vim-plug
call plug#begin()

" List your plugins here
Plug 'tpope/vim-fugitive'
Plug 'tpope/vim-sensible'
Plug 'ryanoasis/vim-devicons'
Plug 'vim-airline/vim-airline'
Plug 'vim-airline/vim-airline-themes'
Plug 'morhetz/gruvbox'
Plug 'dracula/vim'
Plug 'arcticicestudio/nord-vim'
Plug 'rbong/vim-flog'
Plug 'scrooloose/nerdtree'
call plug#end()


set number
let mapleader = " "
let maplocalleader = ","
nnoremap <leader>gs :Git<CR>
nnoremap <leader>gd :Git diff<CR>
nnoremap <leader>gb :Git blame<CR>
nnoremap <leader>q :quit<CR>

" I-beam in Insert mode, block in Normal mode (and others).
" Works in terminals that support cursor-shape escape sequences and in GUI Vim.

if exists('&guicursor')
	highlight Cursor guifg=white guibg=black
highlight iCursor guifg=white guibg=steelblue
	set guicursor=n-v-c:block-Cursor
	set guicursor+=i:ver100-iCursor
	set guicursor+=n-v-c:blinkon0
	set guicursor+=i:blinkwait10
endif
set termguicolors
  " Terminal: Shape control  
  let &t_SI = "\e[6 q"  " Insert: beam  
  let &t_EI = "\e[2 q"  " Normal: block  
  let &t_VE = "\e[1 q"  " Visual: reverse block (terminal-specific)  
 
  " Terminal: Color control  
  autocmd InsertEnter * silent !echo -ne "\e]12;blue\a"  
  autocmd InsertLeave * silent !echo -ne "\e]12;white\a"  
  " autocmd VisualEnter * silent !echo -ne "\e]12;red\a"  
  " autocmd VisualLeave * silent !echo -ne "\e]12;white\a" 
  "
inoremap jk <Esc>


nnoremap <SPACE> <Nop>
let mapleader = " "
let maplocaleader = "\\"


inoremap <esc> <nop>

" ⭐  
nnoremap <leader>ev :vsplit $MYVIMRC<cr>
nnoremap <leader>sv :source $MYVIMRC<cr>

iabbrev rq 🤖❓
iabbrev ra 🤖💡


" Remap change window keys
" Move between windows with Ctrl-h/j/k/l
nnoremap <C-h> <C-w>h
nnoremap <C-j> <C-w>j
nnoremap <C-k> <C-w>k
nnoremap <C-l> <C-w>l

" Window commands under <Leader>w...
nnoremap <Leader>wh <C-w>v
nnoremap <Leader>wj <C-w>j
nnoremap <Leader>wk <C-w>k
nnoremap <Leader>wl <C-w>l
nnoremap <Leader>wv <C-w>v   " vertical split
nnoremap <Leader>ws <C-w>s   " horizontal split
nnoremap <Leader>wc <C-w>c   " close window
nnoremap <Leader>wo <C-w>o   " only this window

nnoremap <leader>bn :bnext<CR>
nnoremap <leader>bp :bprevious<CR>
nnoremap <leader>bb :buffers<CR>:buffer<Space>


" Command Palette (Vim equivalent of VS Code Cmd+Shift+P)
nnoremap <silent> <leader>p :Commands<CR>'

" Find files (like Ctrl+P)
nnoremap <silent> <leader>f :Files<CR>

" Search text in project (like Ctrl+Shift+F)
nnoremap <silent> <leader>g :Rg<CR>

" Open buffers (like tab switcher)
nnoremap <silent> <leader>b :Buffers<CR>

" Search help tags
nnoremap <silent> <leader>h :Helptags<CR>

" EZ
" The following is for easymotion in VSCode
"
" map <Leader> <Plug>(easymotion-w)
"
" See: https://github.com/easymotion/vim-easymotion
" not sure if I like it yet, so I am commenting it out for now
"

" Remap semi-colen -> colen because it makes it is easier to hit without
" having to type shift
nnoremap ; :

" Setup yank to work with clipboard on Macintosh
set clipboard=unnamed

" Setup mappings for Fuzzbox.vim
" https://github.com/vim-fuzzbox/fuzzbox.vim?tab=readme-ov-file
let g:fuzzbox_devicons = 1
nnoremap <silent> <leader>fb :FuzzyBuffers<CR>
nnoremap <silent> <leader>fc :FuzzyCommands<CR>
nnoremap <silent> <leader>ff :FuzzyFiles<CR>
nnoremap <silent> <leader>fg :FuzzyGrep<CR>
nnoremap <silent> <leader>fh :FuzzyHelp<CR>
nnoremap <silent> <leader>fi :FuzzyInBuffer<CR>
nnoremap <silent> <leader>fm :FuzzyMru<CR>
nnoremap <silent> <leader>fp :FuzzyPrevious<CR>
nnoremap <silent> <leader>fq :FuzzyQuickfix<CR>
nnoremap <silent> <leader>fr :FuzzyMruCwd<CR>


" The following two lines make vertical splits much nicer
set fillchars=vert:│
highlight VertSplit cterm=NONE ctermbg=NONE guibg=NONE


" For vim-airline
" See https://github.com/vim-airline/vim-airlinehttps://github.com/vim-airline/vim-airline
" let g:airline#extensions#tabline#enabled = 1
" let g:airline#extensions#tabline#left_sep = ' '
" let g:airline#extensions#tabline#left_alt_sep = '|'

" Nerd Tree Mappings
" See https://github.com/preservim/nerdtree
" nnoremap <leader>n :NERDTreeFocus<CR>
nnoremap <leader>1 :NERDTreeToggle<CR>

nnoremap <leader>, :e $MYVIMRC<CR>

augroup myvimrc_reload
  autocmd!
  autocmd BufWritePost $MYVIMRC source $MYVIMRC | echom "vimrc reloaded"
augroup END



" If another buffer tries to replace NERDTree, put it in the other window, and bring back NERDTree.
autocmd BufEnter * if winnr() == winnr('h') && bufname('#') =~ 'NERD_tree_\d\+' && bufname('%') !~ 'NERD_tree_\d\+' && winnr('$') > 1 |
    \ let buf=bufnr() | buffer# | execute "normal! \<C-W>w" | execute 'buffer'.buf | endif
