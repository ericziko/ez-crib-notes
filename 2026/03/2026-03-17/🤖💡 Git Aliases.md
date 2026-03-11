---
uid: 6619b170-3ff9-44f3-ab3d-0a4550ccff66
created: 2026-03-17T15:50:54
modified: 2026-03-17T15:57:28
title: "🤖💡 Git Aliases "
aliases:
  - "🤖💡 Git Aliases "
linter-yaml-title-alias: "🤖💡 Git Aliases "
---

# 🤖💡 Git Aliases 

## Summary
A bunch of GIT aliases that the robot created for me.

### Pretty graph

```sh
git config --global alias.lg "log --graph --decorate --pretty=format:'%C(yellow)%h%Creset %C(cyan)%an%Creset %Cgreen(%cr)%Creset %s %C(auto)%d%Creset' --all"
```

#### How to use

```sh
git lg
```

### Other aliases

```sh
git config --global alias.all "log --oneline --graph --decorate --all"
git config --global alias.l "log --oneline"
git config --global alias.s "status --short --branch"
git config --global alias.lg "log --graph --decorate --pretty=format:'%C(yellow)%h%Creset %C(cyan)%an%Creset %Cgreen(%cr)%Creset %s %C(auto)%d%Creset' --all"
git config --global alias.last "log -1 HEAD --stat"
git config --global alias.br "branch"
git config --global alias.co "checkout"
git config --global alias.sw "switch"
git config --global alias.recent "for-each-ref --sort=-committerdate refs/heads --format='%(committerdate:relative) %(refname:short)'"
git config --global alias.d "diff"
git config --global alias.dc "diff --cached"
git config --global alias.ci "commit"
git config --global alias.ca "commit --amend"
git config --global alias.cm "commit -m"
git config --global alias.fixup "commit --fixup"
git config --global alias.st "stash"
git config --global alias.sl "stash list"
git config --global alias.sp "stash pop"
git config --global alias.f "fetch --all --prune"
git config --global alias.pullr "pull --rebase"
git config --global alias.unstage "restore --staged"
git config --global alias.discard "restore ."
git config --global alias.cleanall "clean -fd"
git config --global alias.aliases "config --get-regexp '^alias\\.'"
```

Two notes:

discard = restore . only restores tracked files in the current directory tree. For everything in the repo from anywhere, many people prefer:

```sh
discard = restore --worktree -- .
```

And cleanall = clean -fd removes untracked files and directories, but not ignored files. The more dangerous version is:

```
cleanall = clean -fdx
```

That one also removes ignored files like build outputs.

My top 8 from that set would be: all, lg, s, last, recent, dc, unstage, and f.

If you want, I can give you a second set aimed specifically at rebase/fixup/squash-heavy workflows.
