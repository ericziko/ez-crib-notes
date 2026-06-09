---
title: 🤖❓ Git Rebase updateRefs — The Complete Guide
created: 2026-03-27T00:00:00
modified: 2026-03-27T00:00:00
tags:
  - git
  - rebase
  - workflow
  - stacked-branches
uid: a1672206-ce25-40cc-aed6-972d6bae0293
---

# 🤖❓ Git `[rebase] updateRefs = true` — The Complete Guide

## 🤖💡 What Problem Does This Solve?

Modern git workflows often involve **stacked branches** — a chain of feature branches where each one builds on top of the previous. This is common when:

- A feature depends on another in-progress feature
- You split a large PR into reviewable chunks
- You work on related sub-tasks in sequence

The classic pain point: **when you rebase the bottom of the stack, the branches above it all get orphaned.** `updateRefs = true` fixes this automatically.

---

## 📐 Understanding Stacked Branches

### 🌳 The Initial State

Imagine you are building a feature in three layers:

```
main
  └── feature/auth        (PR #1 — adds login)
        └── feature/profile  (PR #2 — uses login)
              └── feature/dashboard (PR #3 — uses profile)
```

```mermaid
gitGraph
   commit id: "A" tag: "main"
   commit id: "B"
   branch feature/auth
   commit id: "C - Add auth middleware"
   commit id: "D - Add login endpoint"
   branch feature/profile
   commit id: "E - Add profile model"
   commit id: "F - Add profile endpoint"
   branch feature/dashboard
   commit id: "G - Add dashboard view"
   commit id: "H - Wire up dashboard data"
```

Each branch is **stacked** — `feature/profile` branches off `feature/auth`, and `feature/dashboard` branches off `feature/profile`.

---

## 😱 The Problem: Rebasing Without `updateRefs`

### Scenario: Main gets new commits, you need to rebase

New commits land on `main` (e.g., a hotfix or merged PR). You need to bring `feature/auth` up to date.

```mermaid
gitGraph
   commit id: "A"
   commit id: "B" tag: "old main"
   commit id: "X - hotfix merged" tag: "main"
   branch feature/auth
   commit id: "C - Add auth middleware"
   commit id: "D - Add login endpoint"
   branch feature/profile
   commit id: "E - Add profile model"
   commit id: "F - Add profile endpoint"
   branch feature/dashboard
   commit id: "G - Add dashboard view"
   commit id: "H - Wire up dashboard data"
```

You run:

```bash
git checkout feature/auth
git rebase main
```

### 🔴 What Happens Without `updateRefs`

Git replays `feature/auth`'s commits onto `main`, creating **new commits** `C'` and `D'`.

```mermaid
flowchart LR
    subgraph "Before rebase"
        A --> B
        B --> X["X (main)"]
        B --> C
        C --> D["D (feature/auth)"]
        D --> E
        E --> F["F (feature/profile)"]
        F --> G
        G --> H["H (feature/dashboard)"]
    end
```

```mermaid
flowchart LR
    subgraph "After: git rebase main (NO updateRefs)"
        A2[A] --> B2[B]
        B2 --> X2["X (main)"]
        X2 --> C2["C' - Add auth middleware"]
        C2 --> D2["D' (feature/auth) ✅ updated"]
        B2 --> C3["C (OLD - orphaned)"]
        C3 --> D3["D (OLD - orphaned)"]
        D3 --> E2["E"]
        E2 --> F2["F (feature/profile) ❌ still here!"]
        F2 --> G2["G"]
        G2 --> H2["H (feature/dashboard) ❌ still here!"]
    end
```

**The disaster:**
- `feature/auth` now correctly points to `D'` (new commit on top of `main`)
- `feature/profile` still points to `F`, which is built on the **old** `D` commit
- `feature/dashboard` still points to `H`, built on the **old** `F`
- Your entire stack is now **disconnected and inconsistent**

To fix this, you would have to manually rebase each branch:

```bash
git checkout feature/profile
git rebase feature/auth   # painful

git checkout feature/dashboard
git rebase feature/profile  # painful again
```

Each rebase risks conflicts that you have to resolve one by one, even if the actual code changes are trivial.

---

## ✅ The Solution: `updateRefs = true`

### Configure It

Add this to your global or local `.gitconfig`:

```ini
[rebase]
    updateRefs = true
```

Or enable it per-command:

```bash
git rebase --update-refs main
```

### 🟢 What Happens With `updateRefs`

When you rebase `feature/auth` onto `main`, git **detects all intermediate branch refs** that are in the chain and **automatically updates them all**.

```mermaid
flowchart TD
    subgraph "After: git rebase main (WITH updateRefs)"
        A --> B
        B --> X["X (main)"]
        X --> C2["C' - Add auth middleware"]
        C2 --> D2["D' (feature/auth) ✅ updated"]
        D2 --> E2["E' - Add profile model"]
        E2 --> F2["F' (feature/profile) ✅ auto-updated!"]
        F2 --> G2["G' - Add dashboard view"]
        G2 --> H2["H' (feature/dashboard) ✅ auto-updated!"]
    end
```

**All three branches are automatically replayed** onto the new base. The entire stack moves together.

---

## 🔬 How Git Detects Intermediate Refs

Git is clever about this. When you run the rebase, it:

1. Walks the commit history from the current branch tip back toward the base
2. At each commit, checks: **does any branch ref point here?**
3. If yes, records it as an "intermediate ref to update"
4. After replaying all commits, moves each recorded ref to its corresponding new commit

```mermaid
sequenceDiagram
    participant User
    participant Git
    participant RefDB as "Ref Database"

    User->>Git: git rebase main
    Git->>RefDB: Walk commits from HEAD to main
    RefDB-->>Git: Found: feature/profile at commit F
    RefDB-->>Git: Found: feature/dashboard at commit H
    Git->>Git: Replay C, D → C', D'
    Git->>Git: Replay E, F → E', F'
    Git->>RefDB: Update feature/profile → F'
    Git->>Git: Replay G, H → G', H'
    Git->>RefDB: Update feature/dashboard → H'
    Git->>RefDB: Update feature/auth → D'
    Git-->>User: Successfully rebased. Updated 3 refs.
```

---

## 📊 Side-by-Side Comparison

| Scenario | Without `updateRefs` | With `updateRefs = true` |
|---|---|---|
| Rebase bottom of stack | Only bottom branch moves | **Entire stack moves** |
| Intermediate branches | Left pointing at orphaned commits | Automatically updated |
| Manual cleanup required | Yes — rebase each branch | No |
| Risk of conflicts | Multiplied (one per branch) | Handled in one pass |
| Stack consistency | Broken | Preserved |

---

## 🗂️ Full Worked Example

### Setup

```bash
# Start from main
git checkout main

# Create stacked branches
git checkout -b feature/auth
echo "auth code" > auth.ts && git add . && git commit -m "Add auth middleware"
echo "login" > login.ts && git add . && git commit -m "Add login endpoint"

git checkout -b feature/profile   # branches from feature/auth
echo "profile" > profile.ts && git add . && git commit -m "Add profile model"
echo "profile api" >> profile.ts && git add . && git commit -m "Add profile endpoint"

git checkout -b feature/dashboard  # branches from feature/profile
echo "dashboard" > dashboard.ts && git add . && git commit -m "Add dashboard view"
echo "wired" >> dashboard.ts && git add . && git commit -m "Wire up dashboard data"
```

### Initial State

```mermaid
gitGraph
   commit id: "main-1" tag: "main"
   branch feature/auth
   commit id: "auth-C"
   commit id: "auth-D"
   branch feature/profile
   commit id: "prof-E"
   commit id: "prof-F"
   branch feature/dashboard
   commit id: "dash-G"
   commit id: "dash-H"
```

### A new commit lands on main

```bash
git checkout main
echo "hotfix" > hotfix.ts && git add . && git commit -m "Critical hotfix"
```

### Rebase with updateRefs

```bash
git checkout feature/auth
git rebase --update-refs main
# or, if configured globally, just:
# git rebase main
```

### Output you'll see

```
Successfully rebased and updated refs/heads/feature/auth.
Updated refs/heads/feature/profile (was abc1234).
Updated refs/heads/feature/dashboard (was def5678).
```

### Final State

```mermaid
gitGraph
   commit id: "main-1"
   commit id: "hotfix" tag: "main"
   branch feature/auth
   commit id: "auth-C'"
   commit id: "auth-D'"
   branch feature/profile
   commit id: "prof-E'"
   commit id: "prof-F'"
   branch feature/dashboard
   commit id: "dash-G'"
   commit id: "dash-H'"
```

All three branches now sit cleanly on top of the hotfix. Stack is preserved. No manual intervention needed.

---

## ⚠️ Caveats and Edge Cases

### 🔁 Force Push Required

Because all the commits in the stack are rewritten (they get new SHA hashes), you must force-push any branches that have already been pushed to a remote:

```bash
git push --force-with-lease origin feature/auth feature/profile feature/dashboard
```

> `--force-with-lease` is safer than `--force`: it refuses to push if the remote has commits you haven't seen, protecting against accidentally overwriting teammates' work.

### 👥 Shared Branches

If teammates are working on the same branches, rewriting history with `--update-refs` will cause divergence for them. This workflow is best for:

- **Solo stacks** where only you work on those branches
- **PR-per-branch** workflows where branches aren't shared directly

### 🏷️ Only Detects Refs in the Ancestry

Git only updates refs that sit **between the current HEAD and the rebase target**. It won't touch branches that branch off the stack sideways or below the rebase base.

```mermaid
flowchart TD
    MAIN["main (rebase target)"]
    AUTH["feature/auth ✅ updated"]
    PROFILE["feature/profile ✅ updated"]
    DASH["feature/dashboard ✅ updated"]
    SIDEBAR["feature/auth-experiment ❌ NOT updated\n(branches off auth but is not\nin the direct ancestry chain\nof the branch being rebased)"]

    MAIN --> AUTH
    AUTH --> PROFILE
    AUTH --> SIDEBAR
    PROFILE --> DASH
```

---

## 🛠️ Configuration Reference

### Global (applies to all repos)

```bash
git config --global rebase.updateRefs true
```

This writes to `~/.gitconfig`:

```ini
[rebase]
    updateRefs = true
```

### Per-Repo

```bash
git config rebase.updateRefs true
```

Writes to `.git/config` in the repo.

### Per-Command (without saving config)

```bash
git rebase --update-refs <base>
git rebase --update-refs main
git rebase --update-refs origin/main
```

### Disable Per-Command Even If Globally Enabled

```bash
git rebase --no-update-refs main
```

---

## 🧩 How This Fits Into a Stacked PR Workflow

`updateRefs` pairs naturally with tools like [gh](https://cli.github.com/) for managing stacked PRs:

```mermaid
flowchart TD
    A["Create stacked branches\nfeature/auth → feature/profile → feature/dashboard"] --> B

    B["Open PRs\nPR #1: main ← feature/auth\nPR #2: feature/auth ← feature/profile\nPR #3: feature/profile ← feature/dashboard"] --> C

    C["New commits land on main\n(review feedback, hotfixes, merges)"] --> D

    D["git checkout feature/auth\ngit rebase --update-refs main"] --> E

    E["All branches auto-updated ✅"] --> F

    F["git push --force-with-lease origin\nfeature/auth feature/profile feature/dashboard"] --> G

    G["All PRs updated\nStack stays coherent 🎉"]
```

---

## 🔑 Key Takeaways

- `[rebase] updateRefs = true` makes git **automatically move all intermediate branch refs** when you rebase a stack
- Without it, rebasing the base of a stack leaves all upper branches **pointing at orphaned commits**
- With it, a **single rebase command** keeps your entire stack coherent
- You still need to **force-push** all affected branches to the remote
- Best suited for **solo stacked branch workflows** — be careful on shared branches
- Available since **Git 2.38** (October 2022)

---

## 📚 Further Reading

- `man git-rebase` — search for `--update-refs`
- `git help config` — search for `rebase.updateRefs`
- [GitHub Blog: Stacked Pull Requests](https://github.blog) — workflows that benefit from this feature
