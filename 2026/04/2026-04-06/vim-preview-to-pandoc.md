---
uid: 20b4a1a2-8628-4641-96e2-0659f404775a
---

# vim-preview-to-pandoc

```vim
function! RenderMarkdownOnly() abort
  if &filetype !=# 'markdown' || empty(expand('%:p'))
    return
  endif

  let l:infile = expand('%:p')
  let l:outfile = tempname() . '.html'

  call system('pandoc ' . shellescape(l:infile) . ' -s -o ' . shellescape(l:outfile))
endfunction

augroup markdown_preview
  autocmd!
  autocmd BufWritePost *.md call RenderMarkdownOnly()
augroup END
```


