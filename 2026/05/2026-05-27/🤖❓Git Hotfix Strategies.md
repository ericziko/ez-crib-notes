---
uid: b1fce8f7-1693-4ff7-b3d8-62fa70c1c387
created: '2026-05-28T00:21:42+00:21'
modified: '2026-05-28T00:29:18+00:29'
title: 🤖❓Git Hotfix Strategies
---

# 🤖❓Git Hotfix Strategies

## Question
Because of my team's poor Git source control policies, I have a hotfix on one branch that I have to apply to several branches. What are some strategies that I could use to do this without shooting myself in the foot?

- [🤖💡The Core Problem](<#the-core-problem>)
	- [Strategy 1: `git cherry-pick` (Most Common)](<#strategy-1-git-cherry-pick-most-common>)
	- [Strategy 2: `git merge` (When Branches Share History)](<#strategy-2-git-merge-when-branches-share-history>)
	- [Strategy 3: `git rebase` (Clean Linear History)](<#strategy-3-git-rebase-clean-linear-history>)
	- [Strategy 4: Patch File (Nuclear Offline Option)](<#strategy-4-patch-file-nuclear-offline-option>)
	- [Strategy 5: Automation Script (When You Have Many Branches)](<#strategy-5-automation-script-when-you-have-many-branches>)
	- [Decision Guide](<#decision-guide>)
	- [🛡️ Best Practices to Avoid Shooting Yourself](<#-best-practices-to-avoid-shooting-yourself>)
	- [Long-Term Advice](<#long-term-advice>)
- [🤖❓Is there a way to cherry-pick a range of commits?](<#is-there-a-way-to-cherry-pick-a-range-of-commits>)
	- [🤖💡Cherry-Picking a Range of Commits](<#cherry-picking-a-range-of-commits>)

### 🤖💡The Core Problem
You need to propagate a fix across multiple branches without creating merge conflicts, duplicate commits, or history chaos.

---

#### Strategy 1: `git cherry-pick` (Most Common)
Pick specific commits and apply them to other branches.

```bash
# Note the commit hash of your hotfix
git log --oneline feature-branch
# abc1234 Fix critical bug

# Apply it to each target branch
git checkout main
git cherry-pick abc1234

git checkout release/v2
git cherry-pick abc1234

git checkout release/v1
git cherry-pick abc1234
```

##### ✅ Pros
- Surgical precision, only moves what you want
- Simple to understand
- Each branch gets its own copy of the commit

##### ⚠️ Pitfalls
- Cherry-picked commits get **new hashes**, so Git treats them as different commits
- Can cause duplicate commits if branches are later merged together
- Does **not** handle dependencies well if your fix spans multiple commits

---

#### Strategy 2: `git merge` (When Branches Share History)
Better when the hotfix branch has a **common ancestor** with targets.

```bash
# Create hotfix branch from the common base
git checkout -b hotfix/critical-bug main

# Make your fix
git commit -m "Fix critical bug"

# Merge into each target
git checkout main && git merge hotfix/critical-bug
git checkout release/v2 && git merge hotfix/critical-bug
git checkout develop && git merge hotfix/critical-bug

# Clean up
git branch -d hotfix/critical-bug
```

##### ✅ Pros
- Preserves full history
- Git recognizes the same commit, avoiding duplicates on later merges
- Handles multi-commit fixes cleanly

##### ⚠️ Pitfalls
- Creates merge commits (noisy history)
- Can drag in **unwanted changes** if branches have diverged significantly

---

#### Strategy 3: `git rebase` (Clean Linear History)
Replay commits on top of target branches.

```bash
git checkout hotfix/critical-bug
git rebase main

git checkout main
git merge --ff-only hotfix/critical-bug  # Fast-forward, no merge commit
```

##### ✅ Pros
- Clean, linear history
- No merge commit noise

##### ⚠️ Pitfalls
- **Never rebase shared/public branches** — rewrites history
- Risky if others are working on the same branches

---

#### Strategy 4: Patch File (Nuclear Offline Option)
Export the fix as a patch and apply it anywhere.

```bash
# Export your fix as a patch file
git format-patch -1 abc1234 --stdout > hotfix.patch

# Apply to any branch, anywhere
git checkout release/v1
git apply hotfix.patch

# OR apply as a commit (preserves author/message)
git am hotfix.patch
```

##### ✅ Pros
- Works across repos, forks, or air-gapped environments
- No branch dependency required

##### ⚠️ Pitfalls
- Can fail with conflicts like any other apply method
- Manual process, easy to miss a branch

---

#### Strategy 5: Automation Script (When You Have Many Branches)

```bash
#!/bin/bash
HOTFIX_COMMIT="abc1234"
BRANCHES=("main" "release/v1" "release/v2" "develop")

for branch in "${BRANCHES[@]}"; do
    echo "Applying hotfix to $branch..."
    git checkout "$branch" || { echo "FAILED: checkout $branch"; continue; }
    git cherry-pick "$HOTFIX_COMMIT" || { echo "FAILED: cherry-pick on $branch"; break; }
    git push origin "$branch"
done
```

> ⚠️ **Test this on non-critical branches first**

---

#### Decision Guide

```
Is your fix a single commit?
├── YES → cherry-pick ✅
└── NO (multiple commits)?
    ├── Branches share common history? → merge ✅
    └── Need clean history + private branches? → rebase ✅

Many branches to update?
└── Wrap cherry-pick in a script ✅

Working across repos or offline?
└── patch file ✅
```

---

#### 🛡️ Best Practices to Avoid Shooting Yourself

| Practice | Why |
|---|---|
| **Always branch before applying** | Never hotfix directly on `main` |
| **Dry-run with `--no-commit`** | `git cherry-pick --no-commit abc1234` lets you inspect before committing |
| **Check for conflicts immediately** | `git status` after every apply |
| **Tag your hotfix** | `git tag hotfix/critical-bug-2024` for traceability |
| **Document the fix in commit message** | Future you will thank present you |
| **Communicate with your team** | Force-pushes after rebase will ruin someone's day |

---

#### Long-Term Advice
The root cause is likely **no clear branching strategy**. Consider adopting:
- **GitFlow** — structured release/hotfix branches
- **Trunk-Based Development** — fewer long-lived branches
- **Branch protection rules** on GitHub/GitLab to enforce the policy

The best hotfix strategy is one you rarely have to use.

### 🤖❓Is there a way to cherry-pick a range of commits?

I saw something like this somewhere once.

```sh
git switch target-branch
git pull --ff-only

git cherry-pick -x <first-hotfix-commit>^..<last-hotfix-commit>
```

#### 🤖💡Cherry-Picking a Range of Commits

Yes, that syntax is real and it works. A few things to unpack:

##### The Range Syntax

```bash
git cherry-pick A^..B
```

This means **"from A to B, inclusive"** — the `^` steps back one commit *before* A, so A itself is included in the range.

```bash
# Without the caret — EXCLUDES the first commit
git cherry-pick A..B    # applies commits after A, up to and including B

# With the caret — INCLUDES the first commit
git cherry-pick A^..B   # applies A through B inclusive
```

> This is the same `^` and `..` syntax used in `git log` and `git diff` — it's consistent across Git commands.

---

##### The `-x` Flag

```bash
git cherry-pick -x A^..B
```

The `-x` appends a note to each commit message:

```
Fix null pointer in payment handler

(cherry picked from commit abc1234)
```

###### ✅ Why this is useful
- Creates an **audit trail** linking the cherry-picked commit back to its origin
- Helps teammates (and future you) understand where a commit came from
- Makes `git log` on the target branch self-documenting

###### ⚠️ When to skip it
- On private/local branches where the noise isn't worth it
- When you're about to squash the commits anyway

---

##### Practical Example

```bash
# Your hotfix commits on the hotfix branch
git log --oneline hotfix/payment-bug
# d4e5f6 Add regression test for null case
# c3d4e5 Guard against null customer object  
# b2c3d4 Fix null pointer in payment handler  ← first
# a1b2c3 Unrelated older commit

# Apply just the hotfix range to main
git switch main
git pull --ff-only
git cherry-pick -x b2c3d4^..d4e5f6
```

---

##### What Can Go Wrong

| Problem | Cause | Fix |
|---|---|---|
| Range applies in wrong order | Commits listed newest-first in log | Double-check with `git log --reverse` |
| Conflict mid-range | Diverged history | Resolve, then `git cherry-pick --continue` |
| Wrong commits included | Off-by-one with `^` | Preview first with `git log A^..B --oneline` |

---

##### Preview Before You Commit

```bash
# See exactly what you're about to apply — no side effects
git log --oneline b2c3d4^..d4e5f6
```

Do this first. It costs nothing and saves a lot of pain.
