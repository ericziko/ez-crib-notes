---
uid: 084ce584-a99d-4a5a-8442-92ac08bb8773
---
# 🤖❓Write me a tutorial on how to use VIMVinegar

## 🤖💡Robot Reply
### VIM Vinegar Tutorial 🥒

#### What is vim-vinegar?

**vim-vinegar** is a plugin by Tim Pope that enhances netrw (Vim's built-in file explorer) to make file navigation simpler and more intuitive. It's designed to be a lightweight alternative to full file explorers like NERDTree.

---

#### Installation

##### Using vim-plug

```vim
" Add to your .vimrc or init.vim
Plug 'tpope/vim-vinegar'
```

Then run:

```
:PlugInstall
```

##### Using Vundle

```vim
Plugin 'tpope/vim-vinegar'
```

##### Using Packer (Neovim)

```lua
use 'tpope/vim-vinegar'
```

---

#### Core Concept

vim-vinegar's philosophy is simple:

> **The file explorer should be a drawer, not a permanent fixture**

Instead of a persistent sidebar, you open, navigate, and close the explorer quickly.

---

#### Basic Usage

##### Opening the File Explorer

| Key | Action |
|-----|--------|
| `-` | Open netrw in the **current file's directory** |
| `-` (again) | Go **up** a directory |

```
" Just press minus (-) from any buffer!
-
```

This is the **killer feature** — one key opens the explorer relative to your current file.

---

##### Navigating in netrw

| Key       | Action                       |
| --------- | ---------------------------- |
| `Enter`   | Open file or enter directory |
| `-`       | Go up one directory          |
| `q`       | Close netrw / go back        |
| `j` / `k` | Move up and down             |

---

##### Opening Files

| Key     | Action                   |
| ------- | ------------------------ |
| `Enter` | Open in current window   |
| `v`     | Open in vertical split   |
| `o`     | Open in horizontal split |
| `t`     | Open in new tab          |
| `p`     | Preview file             |

---

##### File Operations

| Key  | Action                          |
| ---- | ------------------------------- |
| `d`  | Create a **directory**          |
| `%`  | Create a **new file**           |
| `R`  | **Rename** file or directory    |
| `D`  | **Delete** file or directory    |
| `m`  | **Mark** a file                 |
| `mx` | Execute command on marked files |

###### Creating a new file example

```
1. Press - to open netrw
2. Navigate to desired directory
3. Press % 
4. Type filename (e.g., newfile.js)
5. Press Enter
```

---

##### Dotfiles Toggle

vim-vinegar makes it easy to show/hide hidden files:

| Key  | Action                                        |
| ---- | --------------------------------------------- |
| `gh` | Toggle **hidden files** (dotfiles) visibility |

```
" Press gh to see .env, .gitignore, etc.
gh
```

---

##### The `~` Shortcut

Press `~` to quickly jump to your **home directory** in netrw:

```
~   " Jumps to ~/
```

---

#### Practical Workflow Examples

##### Example 1: Edit a nearby file

```
" You're editing src/components/Button.js
" You want to open src/utils/helpers.js

1. Press -          " Opens src/components/
2. Press -          " Goes up to src/
3. Navigate to utils/
4. Press Enter      " Opens utils/
5. Select helpers.js
6. Press Enter      " Opens the file!
```

---

##### Example 2: Create a new file in current directory

```
1. Press -          " Open explorer
2. Press %          " Prompt appears: Enter filename
3. Type index.css
4. Press Enter      " File created and opened!
```

---

##### Example 3: Open a file in a split

```
1. Press -          " Open explorer
2. Navigate to file
3. Press v          " Opens in vertical split
```

---

#### Configuration Tips

Add these to your `.vimrc` for a better experience:

```vim
" Disable banner in netrw (cleaner look)
let g:netrw_banner = 0

" Set default view (0=thin, 1=long, 2=wide, 3=tree)
let g:netrw_liststyle = 3

" Open files in the previous window
let g:netrw_browse_split = 4

" Set netrw window size (percentage)
let g:netrw_winsize = 25

" Hide certain files from view
let g:netrw_list_hide = '.*\.swp$,.*\.pyc'
```

---

#### vim-vinegar vs NERDTree

| Feature          | vim-vinegar    | NERDTree    |
| ---------------- | -------------- | ----------- |
| Weight           | 🪶 Lightweight | 🏋️ Heavier |
| Always visible   | ❌ No           | ✅ Yes       |
| Quick navigation | ✅ Excellent    | ✅ Good      |
| Built on         | netrw          | Custom      |
| Learning curve   | 🟢 Low         | 🟡 Medium   |

---

#### Pro Tips 💡

```
1. Use - frequently — it's faster than typing :e ./path/to/file

2. Combine with CTRL-^ to quickly alternate between 
   the explorer and your last file

3. Use . on a file in netrw to pre-populate the 
   command line with that file's path

4. Press I to toggle the help banner if you forget keys
```

---

#### Quick Reference Card

```
-       → Open explorer (current dir)
-       → Go up a directory  
Enter   → Open file/directory
v       → Vertical split
o       → Horizontal split
t       → New tab
%       → New file
d       → New directory
R       → Rename
D       → Delete
gh      → Toggle dotfiles
~       → Go home
q       → Quit explorer
```

---

#### Summary

vim-vinegar enhances netrw with one simple but powerful idea:

> **Press `-` and you're immediately where you need to be.**

It's the perfect tool for developers who want fast file navigation without the overhead of a full sidebar explorer. Start with just the `-` key, and the rest will become natural over time! 🎯

