---
uid: 1425e51e-fef8-4f98-8fc6-d2acb1825a8b
created: '2026-05-28T18:59:34+18:59'
modified: '2026-05-28T19:08:15+19:08'
title: "🤖❓Git view branch merge cli queries "
aliases:
  - "🤖❓Git view branch merge cli queries "
linter-yaml-title-alias: "🤖❓Git view branch merge cli queries "
---

# 🤖❓Git view branch merge cli queries

##  🤖❓ Prompt
From the GitHub Full request page. I can see

```text
User xxx Merged X commits into targetBranchName from sourceBranchName
```

- I would like to learn how to use the Git command line to explore exactly what happened above.
- I am used to using SmartGit as a graphical interface to Git - and I need to get better at using Git from the command line
- Please give me a list of all the relevant `git` command line invocations / queries, and use cases for running each invocation for which I can use to explore exactly what that means and to understand it from a CLI perspective.
- Please ask me any questions if you need clarity.

### 🤖💡Git CLI Commands for Exploring Branch Merges

- [What You're Looking At](<#what-youre-looking-at>)
- [The Commands](<#the-commands>)
- [Quick Reference Cheat Sheet](<#quick-reference-cheat-sheet>)
- [Suggested Exploration Order](<#suggested-exploration-order>)

#### What You're Looking At

The GitHub PR message means:

```
git merge sourceBranchName  # was run while targetBranchName was checked out
```

---

#### The Commands

##### 1. View Branch Structure / "Where Did Things Come From"

```bash
# Show a visual graph of commits across branches
git log --oneline --graph --all

# Same but more focused on just the two branches in question
git log --oneline --graph targetBranchName sourceBranchName

# Limit to recent history so it's not overwhelming
git log --oneline --graph --all -20
```

**Use case:** See the fork point, the parallel work, and the merge commit visually in the terminal. This is the closest CLI equivalent to SmartGit's branch graph view.

---

##### 2. Find the Merge Commit Itself

```bash
# List merge commits on the target branch (commits with 2+ parents)
git log --oneline --merges targetBranchName

# Show full detail of a specific merge commit
git show <merge-commit-hash>

# Show just the metadata (who, when, message) without the diff
git show --no-patch <merge-commit-hash>
```

**Use case:** Identify *exactly* when the merge happened, who did it, and what the merge commit message says.

---

##### 3. See What Commits Were Brought In

```bash
# Show commits that came FROM sourceBranchName but were NOT on targetBranchName before merge
git log targetBranchName..sourceBranchName

# Same but oneline for brevity
git log --oneline targetBranchName..sourceBranchName

# If the source branch has been deleted, use the merge commit hash (^1 = first parent, ^2 = second parent)
git log --oneline <merge-commit-hash>^1..<merge-commit-hash>^2
```

**Use case:** Answer *"what work did this PR actually contain?"* — lists every commit that rode in on the source branch.

---

##### 4. See What Files Were Changed

```bash
# Files changed by the merge commit itself
git diff <merge-commit-hash>^1 <merge-commit-hash>

# All files changed across all commits in the source branch
git diff --name-only targetBranchName...sourceBranchName
#         ^^^ three dots is intentional - finds the common ancestor

# With change summary (lines added/removed per file)
git diff --stat targetBranchName...sourceBranchName
```

**Use case:** Answer *"what files did this PR touch?"*

> **Two-dot vs Three-dot**
> - `A..B` → changes from A to B directly
> - `A...B` → changes from the **common ancestor** of A and B, up to B

---

##### 5. See the Actual Code Diff

```bash
# Full diff of everything the source branch changed (relative to common ancestor)
git diff targetBranchName...sourceBranchName

# Diff of a specific file only
git diff targetBranchName...sourceBranchName -- path/to/file.txt

# Diff introduced by one specific commit from the branch
git show <commit-hash>
```

**Use case:** Read the actual line-by-line changes, equivalent to the *"Files Changed"* tab in a GitHub PR.

---

##### 6. Understand the Merge Commit's Parents

```bash
# Show both parents of a merge commit
git log --pretty=format:"%H %P" -1 <merge-commit-hash>
#                                       ^^^ %P = parent hashes

# More readable
git cat-file -p <merge-commit-hash>
```

**Use case:** A merge commit has **two parents** — parent 1 is where `targetBranch` was, parent 2 is the tip of `sourceBranch`. This shows you both.

```
parent1 (targetBranch tip before merge)
         \
          M  ← merge commit
         /
parent2 (sourceBranch tip = last PR commit)
```

---

##### 7. Check Where Branches Currently Point

```bash
# List all branches and their latest commit
git branch -v

# Include remote branches
git branch -av

# Show what commit a specific branch points to
git rev-parse targetBranchName
git rev-parse sourceBranchName  # may be gone if deleted after merge
```

**Use case:** Confirm which commit each branch tip currently references.

---

##### 8. Find the Common Ancestor (Fork Point)

```bash
# Find where the two branches diverged
git merge-base targetBranchName sourceBranchName

# See what that commit actually was
git show $(git merge-base targetBranchName sourceBranchName)
```

**Use case:** Answer *"what was the state of the code when the PR branch was created?"*

---

##### 9. Check for Merge Conflicts That Were Resolved

```bash
# Show the merge commit with conflict context (if it was a non-fast-forward merge)
git show <merge-commit-hash>

# Check if the merge was a fast-forward (no merge commit) vs a true merge commit
git log --oneline --merges targetBranchName -5
```

**Use case:** If the merge commit diff shows changes *beyond* what the source branch added, those were likely conflict resolutions.

---

#### Quick Reference Cheat Sheet

| Goal | Command |
|---|---|
| Visual branch graph | `git log --oneline --graph --all` |
| Find the merge commit | `git log --oneline --merges targetBranch` |
| Commits brought in by PR | `git log --oneline main..featureBranch` |
| Files changed in PR | `git diff --name-only main...featureBranch` |
| Full code diff of PR | `git diff main...featureBranch` |
| Fork point | `git merge-base main featureBranch` |
| Inspect merge commit parents | `git cat-file -p <hash>` |

---

#### Suggested Exploration Order

```bash
# Step 1 - get your bearings
git log --oneline --graph --all -30

# Step 2 - find the merge commit hash (call it $M)
git log --oneline --merges targetBranchName -5

# Step 3 - see what came in
git log --oneline $M^1..$M^2

# Step 4 - see what changed
git diff --stat $M^1 $M
```
