" ------------------------------------------------
" Begin Install of Vundle Plugin
" See https://github.com/VundleVim/Vundle.vim"
" ------------------------------------------------
set encoding=UTF-8

" Installed VIM Plug
" https://github.com/junegunn/vim-plug
call plug#begin()

" List your plugins here
Plug 'tpope/vim-sensible'
Plug 'ryanoasis/vim-devicons'
Plug 'vim-airline/vim-airline'
Plug 'vim-airline/vim-airline-themes'
call plug#end()

set nocompatible              " be iMproved, required
filetype off                  " required

" set the runtime path to include Vundle and initialize
set rtp+=~/.vim/bundle/Vundle.vim
call vundle#begin()
" alternatively, pass a path where Vundle should install plugins
"call vundle#begin('~/some/path/here')

" let Vundle manage Vundle, required
Plugin 'VundleVim/Vundle.vim'

" The following are examples of different formats supported.
" Keep Plugin commands between vundle#begin/end.
" plugin on GitHub repo
Plugin 'tpope/vim-fugitive'
" plugin from http://vim-scripts.org/vim/scripts.html
" Plugin 'L9'
" Git plugin not hosted on GitHub
Plugin 'git://git.wincent.com/command-t.git'
" git repos on your local machine (i.e. when working on your own plugin)
Plugin 'file:///home/gmarik/path/to/plugin'
" The sparkup vim script is in a subdirectory of this repo called vim.
" Pass the path to set the runtimepath properly.
Plugin 'rstacruz/sparkup', {'rtp': 'vim/'}
" Install L9 and avoid a Naming conflict if you've already installed a
" different version somewhere else.
" Plugin 'ascenator/L9', {'name': 'newL9'}
" All of your Plugins must be added before the following line
" Install plugin to get command pallate like functionality
Plugin 'junegunn/fzf'
Plugin 'junegunn/fzf.vim'
Plugin 'tpope/vim-sensible'
Plugin 'ryanoasis/vim-devicons'
call vundle#end()            " required
filetype plugin indent on    " required
" To ignore plugin indent changes, instead use:
"filetype plugin on
"
" Brief help
" :PluginList       - lists configured plugins
" :PluginInstall    - installs plugins; append `!` to update or just :PluginUpdate
" :PluginSearch foo - searches for foo; append `!` to refresh local cache
" :PluginClean      - confirms removal of unused plugins; append `!` to auto-approve removal
"
" see :h vundle for more details or wiki for FAQ
" Put your non-Plugin stuff after this line
" ------------------------------------------------
" End Install of Vundle Plugin
" ------------------------------------------------

" Install Nerd Tree 
" https://vimawesome.com/plugin/nerdtree-red
Plugin 'scrooloose/nerdtree'

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



