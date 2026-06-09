---
title: Git Config — Pull, Rebase & Merge Options Explained
created: 2026-03-27
modified: 2026-03-27
tags:
  - git
  - config
  - rebase
  - merge
  - workflow
uid: ac43c652-e4e0-494e-b169-efd18483a50e
---

# 🤖❓ Git Config — Pull, Rebase & Merge Options Explained

---

## 🤖💡 Your Current Config Explained

```ini
[pull]
    rebase = true
    ff = only
[rebase]
    autoStash = true
    updateRefs = true
```

### `[pull] rebase = true`

When you run `git pull`, instead of creating a merge commit, Git will **rebase your local commits on top of the fetched upstream commits**.

| Without this | With this |
|---|---|
| `git pull` = fetch + merge → merge commit | `git pull` = fetch + rebase → linear history |

**Why it matters:** Keeps history clean and linear. No "Merge branch 'main' of..." noise commits.

---

### `[pull] ff = only`

Forces `git pull` to only succeed if it can **fast-forward** — i.e. your local branch has no diverging commits.

- If you have local commits that diverged, the pull **fails** rather than auto-merging or rebasing.
- This is a safety net: it makes Git stop and say *"something is different here, deal with it explicitly"*.

> **Note:** This setting and `rebase = true` together can create tension. `ff = only` prevents the pull if there are diverging commits, but `rebase = true` would handle them. In practice, `rebase = true` takes precedence for the pull mechanism — `ff = only` is more of a guardrail for cases where rebasing isn't appropriate (e.g. when the remote has force-pushed and you need to notice).

---

### `[rebase] autoStash = true`

Before a rebase begins, Git automatically runs `git stash` on any uncommitted changes, then `git stash pop` after the rebase completes.

Without this, `git rebase` (and by extension `git pull --rebase`) would refuse to run if you had a dirty working tree.

**Practical effect:** You can `git pull` even mid-work without committing first.

---

### `[rebase] updateRefs = true`

When rebasing a stack of branches (branch A branched from B, B branched from C), this option **automatically updates the intermediate branch pointers** as each commit is replayed.

Without it, rebasing the bottom branch leaves the branches above it dangling at old commits. With it, Git keeps the whole stack in sync.

**Most useful when:** You work with stacked PRs or dependent feature branches.

---

## 🤖💡 Overriding Per-Repository

Yes — absolutely. Git config has three layers, and **local always wins**:

| Layer | File | Scope |
|---|---|---|
| System | `/etc/gitconfig` | All users on the machine |
| Global | `~/.gitconfig` | Your user, all repos |
| Local | `.git/config` | This repo only |

### How to override in a specific repo

```bash
# Inside the repo
git config --local pull.rebase false       # Use merge instead of rebase for this repo
git config --local pull.ff false           # Allow non-fast-forward merges
git config --local rebase.autoStash false  # Disable auto-stash here
```

Or edit `.git/config` directly:

```ini
[pull]
    rebase = false
    ff = false
```

---

## 🤖💡 Other Pragmatic Choices

### 🔀 Merge vs Rebase — the core tradeoff

| | Merge | Rebase |
|---|---|---|
| History shape | Non-linear (shows branching) | Linear (clean, easy to read) |
| Commit SHAs | Preserved | Rewritten |
| Collaboration safety | Safe on shared branches | **Dangerous on shared branches** — never rebase what others have |
| Bisect/blame | Can be harder to follow | Easier |

**Rule of thumb:** Rebase your *local* work before pushing. Never rebase commits that are already on a shared/remote branch.

---

### 🔧 Other useful global config options

```ini
[merge]
    ff = false          # Always create a merge commit even for fast-forwards
    tool = vimdiff      # or vscode, kdiff3, etc.

[diff]
    tool = vimdiff

[rerere]
    enabled = true      # Re-use Recorded Resolution — remembers how you resolved
                        # a conflict and auto-applies it if the same conflict appears again
                        # Great when you rebase long-running branches repeatedly

[branch]
    autosetuprebase = always   # New branches automatically track with rebase instead of merge
                               # (works with `pull.rebase = true`)

[push]
    default = current          # Push the current branch to a remote branch of the same name
    autoSetupRemote = true     # Auto-creates the upstream tracking branch on first push
                               # (no need for -u origin HEAD)
```

---

### 🏷️ `branch.autosetuprebase = always`

Pairs with `pull.rebase = true`. When you create a new branch that tracks a remote, it is automatically configured to use rebase on pull rather than merge. Without this, new branches default to merge even if your global `pull.rebase = true` is set.

---

### 🧠 `rerere.enabled = true` — the hidden gem

Short for **Re-use Recorded Resolution**. When you resolve a merge/rebase conflict, Git records the resolution. If the exact same conflict appears again (common when rebasing a long-lived branch onto an updated main), Git applies the saved resolution automatically.

Highly recommended if you frequently rebase feature branches.

---

### 📋 Recommended pragmatic global config

```ini
[pull]
    rebase = true

[rebase]
    autoStash = true
    updateRefs = true

[push]
    default = current
    autoSetupRemote = true

[branch]
    autosetuprebase = always

[rerere]
    enabled = true

[merge]
    tool = vscode        # or your preferred diff tool
```

Drop `pull.ff = only` unless you specifically want the safety check — it can get in the way when working with `rebase = true`.

---

## 🤖💡 Switching to a Merge-Based Workflow

### 🔧 Global config change

Edit `~/.gitconfig`:

```ini
[pull]
    rebase = false   # or remove the line entirely
    ff = false       # allow merge commits on pull

[merge]
    ff = false       # always create a merge commit, even when fast-forward is possible
```

Or via CLI:

```bash
git config --global pull.rebase false
git config --global pull.ff false
git config --global merge.ff false
```

### 🔧 Per-repo override only

```bash
git config --local pull.rebase false
git config --local pull.ff false
```

---

### 📊 What changes day-to-day

| Action | Rebase workflow | Merge workflow |
|---|---|---|
| `git pull` | Rebases your commits on top of upstream | Creates a merge commit |
| `git merge feature` | You'd rebase instead | Creates a merge commit |
| History shape | Linear | Branchy (shows what happened when) |
| Conflict resolution | Once per commit being replayed | Once total |

---

### 💡 What to keep from the rebase config

`rebase.autoStash = true` is still useful in a merge workflow — it lets `git pull` run even with a dirty working tree without needing to commit first.
