---
uid: c39e6f54-376e-482b-9112-7d841b2249f4
title: 🤖❓Git merge conflict resolution strategies
created: '2026-05-28T00:15:27+00:15'
modified: '2026-05-28T00:20:45+00:20'
---

# 🤖❓Git merge conflict resolution strategies

In GitHub, in the web, when I try to merge to a certain branch, it says there's going to be conflicts that I have to figure out from the command line. How do I figure out what those conflicts are going to be?

# 🤖💡Robot Reply# Git Merge Conflict Resolution Strategies

## The Core Workflow

When GitHub says "resolve conflicts on command line", here's what to do:

```bash
# 1. Make sure you're on the branch you want to merge INTO
git checkout main                    # or whatever target branch

# 2. Fetch latest changes
git fetch origin

# 3. Attempt the merge - this REVEALS the conflicts
git merge origin/your-feature-branch
```

---

## Reading the Conflict Output

```bash
Auto-merging src/config.js
CONFLICT (content): Merge conflict in src/config.js
Auto-merging README.md
CONFLICT (content): Merge conflict in README.md
Automatic merge failed; fix conflicts and then commit the result.
```

**That output tells you exactly which files conflict.**

---

## See All Conflicted Files at Once

```bash
git status
# Look for:  "both modified: src/config.js"

git diff --name-only --diff-filter=U
# Lists ONLY conflicted files, cleanly
```

---

## What a Conflict Looks Like Inside the File

```javascript
const timeout = 
<<<<<<< HEAD
  5000                          // your current branch version
=======
  3000                          // incoming branch version
>>>>>>> origin/feature-branch
```

 `<<<<<<< HEAD`        - Start of YOUR current branch's version
 ` ======= `            - Divider between the two versions|
 `>>>>>>> branch-name` -  End of INCOMING branch's version

---

## Preview Conflicts WITHOUT Committing to the Merge

```bash
# See what WOULD conflict before you start
git merge --no-commit --no-ff origin/feature-branch

# Inspect the conflicts
git diff

# Then ABORT cleanly - nothing is changed
git merge --abort
```

> **This is the safest way to "look before you leap"**

---

## Resolving Each Conflict

```bash
# Open each conflicted file and edit it manually
# Delete the markers and keep what you want, then:

git add src/config.js             # mark as resolved
git add README.md

git commit                        # complete the merge
```

### Or use a merge tool

```bash
git mergetool                     # opens configured visual tool
# Common tools: vimdiff, VS Code, IntelliJ, kdiff3
```

### Configure VS Code as your merge tool

```bash
git config --global merge.tool vscode
git config --global mergetool.vscode.cmd 'code --wait $MERGED'
```

---

## The "Accept All Theirs/Ours" Shortcut

When you know one side is entirely correct:

```bash
# Keep ONLY your current branch version of a file
git checkout --ours src/config.js

# Keep ONLY the incoming branch version
git checkout --theirs src/config.js

git add src/config.js
```

---

## If You Want to Bail Out Entirely

```bash
git merge --abort       # returns everything to pre-merge state
```

---

## Quick Reference Cheatsheet

```
git merge origin/branch     → triggers the merge, shows conflicts
git status                  → lists conflicted files
git diff                    → shows conflict markers in files
git merge --abort           → escape hatch, undo everything
git add <file>              → mark a conflict as resolved
git commit                  → finalize the merge
```

---

## Pro Tip: See the Diff Before Merging

```bash
# See exactly what changed between the two branches
git diff main...origin/feature-branch

# See only which FILES differ
git diff main...origin/feature-branch --name-only
```

This lets you **anticipate** conflicts before even starting the merge.
