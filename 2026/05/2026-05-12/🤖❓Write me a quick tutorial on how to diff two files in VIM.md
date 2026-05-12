
# 🤖❓Write me a quick tutorial on how to diff two files in VIM 
- I am talking about classic VIM - not neovim
- If there are any VIM plugins that would help with this workflow - please feel free to suggest them
- How I open and diff two different files while I am in VIM
- How I open vim into a diff view from the command line

# 🤖💡Robot Reply# Diffing Two Files in VIM

## From the Command Line (vimdiff)

The easiest way to start a diff session is before you even open VIM:

```bash
vimdiff file1.txt file2.txt
```

Or equivalently:

```bash
vim -d file1.txt file2.txt
```

You can diff **up to 4 files** at once:

```bash
vimdiff file1.txt file2.txt file3.txt
```

---

## Opening a Diff From Inside VIM

If you already have a file open in VIM and want to diff it against another file:

```vim
:vertical diffsplit other_file.txt
```

Or horizontally (less common, harder to read):

```vim
:diffsplit other_file.txt
```

You can also open a second file in a split first and then turn on diff mode manually:

```vim
:vsplit other_file.txt
```

Then in **each window** run:

```vim
:diffthis
```

To turn diff mode **off** in the current window:

```vim
:diffoff
```

To turn it off in **all windows**:

```vim
:diffoff!
```

---

## Navigating the Diff

| Command | Action |
|---|---|
| `]c` | Jump to **next** change |
| `[c` | Jump to **previous** change |
| `do` | **D**iff **o**btain — pull change into current window |
| `dp` | **D**iff **p**ut — push change to the other window |
| `zo` | Open a fold |
| `zc` | Close a fold |
| `zR` | Open **all** folds |

After making changes you can refresh the diff view with:

```vim
:diffupdate
```

---

## Useful VIM Settings for Diff Mode

Worth putting these in your `.vimrc`:

```vim
" Ignore whitespace differences
set diffopt+=iwhite

" Always use vertical splits for diffs
set diffopt+=vertical

" Show context lines around changes (default is 6)
set diffopt+=context:3
```

---

## Helpful Plugins

### [vim-fugitive](https://github.com/tpope/vim-fugitive)

> The gold standard for Git + VIM integration

- Run `:Gdiffsplit` to diff the current file against its last committed Git version
- Gives you a full 3-way merge conflict resolution workflow
- Pairs well with vimdiff for resolving merge conflicts

```bash
# In your .vimrc (with vim-plug for example)
Plug 'tpope/vim-fugitive'
```

---

### [vim-signify](https://github.com/mhinz/vim-signify) *(lighter alternative)*
- Shows Git diff **signs in the gutter** (`+`, `-`, `~`) as you edit
- Not a full diff view but great for staying aware of changes inline
- Lighter weight than fugitive if you just want gutter indicators

---

## Quick Reference Card

```
# From terminal
vimdiff a.txt b.txt
vim -d a.txt b.txt

# From inside vim
:vertical diffsplit b.txt
:diffthis          ← on each window if opened manually
:diffoff           ← turn off diff in current window
:diffupdate        ← refresh after edits

# Navigation
]c  →  next change
[c  →  prev change
do  →  obtain (pull into here)
dp  →  put (push to other side)
```

---

**My recommendation:** Use `vimdiff` from the command line for quick one-off diffs, and install **vim-fugitive** if you work with Git — the `:Gdiffsplit` workflow alone is worth it.
