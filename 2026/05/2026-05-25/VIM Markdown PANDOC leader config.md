---
uid: 935cb4ee-5391-48b6-a609-5d2b5e07d1b0
---
# VIM Markdown PANDOC leader config


#para/resources/vim #para/resources/markdown 

## Alternatives to Plugins: Minimalist Workflows

If you prefer avoiding plugins, here are lightweight alternatives:

### 1\. Pandoc + Browser

Use `pandoc` (a universal document converter) to convert Markdown to HTML and open it in a browser:

```vim
nnoremap <leader>p :!pandoc % -o %:r.html && xdg-open %:r.html<CR>  " Linux
nnoremap <leader>p :!pandoc % -o %:r.html && open %:r.html<CR>      " macOS
```
