---
title: Git Fixup Commits - A Complete Tutorial
created: 2026-03-28
modified: 2026-03-28
tags:
  - git
  - fixup
  - commits
  - rebase
uid: faec0177-c551-4a2e-a380-de17db82e726
---

# Git Fixup Commits: A Complete Tutorial

## What Are Fixup Commits?

A **fixup commit** is a commit that fixes a bug or makes a small change to a *previous commit* without creating a new logical change in the history. Instead of amending that commit directly, you create a new commit marked as a "fixup" that will be automatically squashed into the original during a rebase.

### Why Use Fixup Commits?

```mermaid
graph TD
    A["❌ Problem: Direct Amendment"] -->|"git commit --amend"| B["Lost commit history<br/>Changes only in latest commit"]

    C["✅ Solution: Fixup Commits"] -->|"git commit --fixup"| D["Keeps commit separation<br/>Until intentionally squashed"]

    E["Benefits"] -->|"Cleaner history<br/>Easier to review<br/>Easier to bisect<br/>Flexible workflows"| F["Better collaboration"]
```

Think of it this way: fixup commits are **"work in progress"** markers that say *"this commit belongs with the one I'm fixing"* without destroying the original commit's history yet.

---

## Part 1: Creating Fixup Commits with `git commit --fixup`

### The Concept

```mermaid
graph LR
    A["Original Commit<br/>abc123"] -->|"You realize there's a bug"| B["Make a fix<br/>git add ."]
    B -->|"git commit --fixup abc123"| C["Fixup Commit Created<br/>fixup! abc123<br/>def456"]

    style A fill:#90EE90
    style C fill:#FFB6C1
```

### How It Works

When you run:
```bash
git commit --fixup <original-commit-hash>
```

Git automatically creates a new commit with a special message format:

```
fixup! <original commit message>
```

The fixup commit sits *after* the original in the history, waiting to be squashed later.

### Visual Example: Before and After

**Before Fixup Commit:**

```
commit abc123 - Feature: Add user authentication
    - Implement login endpoint
    - Add password hashing
    - (Bug: forgot SQL NULL check)
```

**After `git commit --fixup abc123`:**

```
commit abc123 - Feature: Add user authentication
    - Implement login endpoint
    - Add password hashing
    - (Bug: forgot SQL NULL check)

commit def456 - fixup! Feature: Add user authentication
    - Add missing NULL check in query
```

---

## Part 2: Understanding the Fixup Workflow

### Complete Workflow Diagram

```mermaid
graph TD
    A["1️⃣ Original Commit<br/>abc123: Feature X"] -->|"Work continues..."| B["2️⃣ Later: Bug Found<br/>in Feature X"]

    B -->|"git add .<br/>git commit --fixup abc123"| C["3️⃣ Fixup Commit Created<br/>def456: fixup! Feature X"]

    C -->|"git rebase -i HEAD~2<br/>--autosquash"| D["4️⃣ Rebase Squashes<br/>Fixup into Original"]

    D -->|"Result in history"| E["✅ Clean History<br/>abc123: Feature X<br/>with fix integrated"]

    style A fill:#90EE90
    style C fill:#FFB6C1
    style E fill:#87CEEB
```

### Step-by-Step Example

Let's say you have this history:

```
commit 3 - Add tests
commit 2 - Feature: User login
commit 1 - Setup project
```

**Step 1: Create a fixup commit**

```bash
# Find the bug in commit 2
git checkout HEAD~1  # Go back to commit 2
# ... fix the bug ...
git add .
git commit --fixup 2abcde  # Create fixup for commit 2
```

**Step 2: Your history now looks like:**

```mermaid
graph TB
    A["commit 1: Setup project"] --> B["commit 2: Feature: User login<br/>(has a bug)"]
    B --> C["commit 3: Add tests"]
    C --> D["commit 4: fixup! Feature: User login<br/>(the fix)"]

    style B fill:#FFB6C1
    style D fill:#FFB6C1
```

**Step 3: Use autosquash to merge fixup into original**

```bash
git rebase -i --autosquash HEAD~3
```

**Step 4: Result - clean history**

```mermaid
graph TB
    A["commit 1: Setup project"] --> B["commit 2: Feature: User login<br/>(bug fixed)"]
    B --> C["commit 3: Add tests"]

    style B fill:#90EE90
```

---

## Part 3: Git Rebase --Autosquash

### What is Autosquash?

`--autosquash` is a flag that automatically **reorders and marks fixup/squash commits** during an interactive rebase, so you don't have to manually edit the rebase plan.

### Without Autosquash (Manual Process)

```mermaid
graph TD
    A["Run: git rebase -i HEAD~2"] --> B["Interactive Editor Opens<br/>pick abc123 Feature X<br/>pick def456 fixup! Feature X"]

    B -->|"You must manually change<br/>pick to squash"| C["Editor shows:<br/>pick abc123 Feature X<br/>squash def456 fixup! Feature X"]

    C --> D["Save & close<br/>Rebase completes"]

    style B fill:#FFE4B5
    style C fill:#FFE4B5
```

### With Autosquash (Automatic Process)

```mermaid
graph TD
    A["Run: git rebase -i --autosquash HEAD~2"] --> B["Git Recognizes 'fixup!' Pattern<br/>Automatically reorders &<br/>marks for squashing"]

    B --> C["Interactive Editor Shows:<br/>pick abc123 Feature X<br/>fixup def456 fixup! Feature X<br/>(already set up!"]

    C --> D["Save & close<br/>Rebase completes"]

    style C fill:#90EE90
```

### Visual Comparison

```mermaid
graph LR
    subgraph manual["❌ Manual (Without --autosquash)"]
        M1["1. Run rebase"] --> M2["2. Read rebase plan"] --> M3["3. Manually edit<br/>pick→squash"] --> M4["4. Save & rebase"]
    end

    subgraph auto["✅ Automatic (With --autosquash)"]
        A1["1. Run rebase<br/>--autosquash"] --> A2["2. Git auto-detects<br/>fixup! commits"] --> A3["3. Automatically<br/>reordered & marked"] --> A4["4. Save & rebase"]
    end

    style manual fill:#FFE4B5
    style auto fill:#90EE90
```

### Before vs. After Autosquash

**Before (what git sees):**

```
def456 - fixup! Feature: User login
abc123 - Feature: User login
```

**After autosquash reorders (what interactive rebase shows):**

```
pick abc123 - Feature: User login
fixup def456 - fixup! Feature: User login
```

---

## Part 4: Manual Fixup Workflows (Without Autosquash)

Sometimes you want to manually control how commits are combined. Here's how:

### Scenario: Manually Squashing Multiple Commits

```mermaid
graph TB
    A["commit 1: Setup"] --> B["commit 2: Feature X<br/>(incomplete)"]
    B --> C["commit 3: Add Y"]
    C --> D["commit 4: Feature X fix #1"]
    D --> E["commit 5: Feature X fix #2"]

    style B fill:#FFB6C1
    style D fill:#FFB6C1
    style E fill:#FFB6C1
```

### Manual Rebase Process

```bash
# Start interactive rebase from 4 commits ago
git rebase -i HEAD~4
```

**Interactive Rebase Editor Opens:**

```
pick 2abcde Feature X (incomplete)
pick 3bcdef Add Y
pick 4cdefg Feature X fix #1
pick 5defgh Feature X fix #2
```

**You manually edit to:**

```
pick 2abcde Feature X (incomplete)
squash 4cdefg Feature X fix #1
squash 5defgh Feature X fix #2
pick 3bcdef Add Y
```

(Note: You move the fixups right after the original commit and change `pick` to `squash`)

**Result:**

```mermaid
graph TB
    A["commit 1: Setup"] --> B["commit 2: Feature X<br/>(with fixes integrated)"]
    B --> C["commit 3: Add Y"]

    style B fill:#90EE90
```

### Rebase Commands Explained

```mermaid
graph LR
    A["Rebase Commands"] -->|"pick"| B["Use this commit as-is"]
    A -->|"squash (s)"| C["Combine with previous<br/>Keep message"]
    A -->|"fixup (f)"| D["Combine with previous<br/>Discard message"]
    A -->|"reword (r)"| E["Edit commit message"]
    A -->|"drop (d)"| F["Remove commit entirely"]
```

---

## Part 5: Interactive Rebase Visual Walkthrough

### Complete Interactive Rebase State Machine

```mermaid
stateDiagram-v2
    [*] --> Editor: "git rebase -i HEAD~N"

    Editor --> Review: "Rebase plan opens<br/>List of commits with action"

    Review --> Decide: "Decide for each commit:<br/>pick? squash? reword?"

    Decide --> Edit: "Make changes to<br/>pick/squash/etc"

    Edit --> Save: "Save & exit editor"

    Save --> Process: "Git processes rebase<br/>Applies commits in order"

    Process --> Conflict{Merge<br/>Conflicts?}

    Conflict -->|Yes| Resolve: "Resolve conflicts<br/>git add .<br/>git rebase --continue"
    Conflict -->|No| Complete: "Rebase complete"

    Resolve --> Process

    Complete --> [*]
```

### Real Example: Step-by-Step

**Initial State (5 commits with fixup):**

```mermaid
graph TB
    A["abc123: Setup auth"] --> B["def456: Add login endpoint"]
    B --> C["ghi789: Add tests"]
    C --> D["jkl012: Fix NULL check<br/>fixup!"]
    D --> E["mno345: Update docs"]

    style D fill:#FFB6C1
```

**Command Executed:**

```bash
$ git rebase -i --autosquash HEAD~4
```

**Rebase Editor Opens (with autosquash applied):**

```
pick abc123 Setup auth
pick def456 Add login endpoint
fixup jkl012 Fix NULL check
pick ghi789 Add tests
pick mno345 Update docs
```

**You modify to reword one commit:**

```
pick abc123 Setup auth
pick def456 Add login endpoint
fixup jkl012 Fix NULL check
reword ghi789 Add tests
pick mno345 Update docs
```

**Save, then editor opens for reword:**

```
Add tests
# Edit the above line
```

**Change to:**

```
Add comprehensive unit tests for auth
```

**Save again, rebase continues and completes:**

```mermaid
graph TB
    A["abc123: Setup auth"] --> B["def456: Add login endpoint<br/>(with NULL check fix)"]
    B --> C["ghi789: Add comprehensive unit tests<br/>for auth"]
    C --> D["mno345: Update docs"]

    style B fill:#90EE90
    style C fill:#87CEEB
```

---

## Part 6: Practical Workflow Examples

### Workflow 1: Single Fixup Commit

```mermaid
graph LR
    A["1. Feature done<br/>abc123"] -->|"2. Deploy to staging<br/>QA finds bug"| B["3. Create fixup<br/>git commit --fixup abc123"]

    B -->|"4. Before merging to main<br/>git rebase -i --autosquash"| C["5. Clean history<br/>Bug integrated<br/>Push to main"]

    style A fill:#90EE90
    style B fill:#FFB6C1
    style C fill:#87CEEB
```

**Commands:**

```bash
# Step 1: Do your work, commit normally
git commit -m "Feature: Add password reset"  # abc123

# ... Later: Bug found

# Step 2: Fix the bug
git add .
git commit --fixup abc123  # Creates new commit

# Step 3: Before pushing, clean up
git rebase -i --autosquash HEAD~2

# Step 4: Push clean history
git push origin feature-branch
```

### Workflow 2: Multiple Fixups for Same Commit

```mermaid
graph TB
    A["Commit: Feature X<br/>abc123"]

    B["Day 1: Bug found<br/>Fix #1"] -->|"git commit --fixup abc123"| C["Fixup 1<br/>def456"]

    D["Day 2: Another bug<br/>Fix #2"] -->|"git commit --fixup abc123"| E["Fixup 2<br/>ghi789"]

    A --> B
    A --> D

    C --> F["Before merge:<br/>git rebase -i --autosquash"]
    E --> F

    F --> G["All fixups<br/>squashed into abc123"]

    style A fill:#90EE90
    style C fill:#FFB6C1
    style E fill:#FFB6C1
    style G fill:#87CEEB
```

**Commands:**

```bash
# Day 1
git commit --fixup abc123

# Day 2
git commit --fixup abc123

# Before merge - both fixups auto-squash
git rebase -i --autosquash HEAD~3

# Result: clean history with all fixes integrated
```

### Workflow 3: Staging Area with Fixup

```mermaid
graph LR
    A["Modified files<br/>from multiple commits"] -->|"Stage changes<br/>for each commit"| B["git add specific-files"]

    B -->|"Commit as fixup"| C["git commit --fixup &lt;hash&gt;"]

    C -->|"Rebase later"| D["All changes in right place"]

    style A fill:#FFE4B5
    style B fill:#FFE4B5
    style C fill:#FFB6C1
```

---

## Part 7: Best Practices & Tips

### ✅ DO

```mermaid
graph TD
    A["Best Practices"] -->|"✅"| B1["Use --autosquash<br/>Automates reordering<br/>Fewer manual steps"]
    A -->|"✅"| B2["Fixup before rebasing<br/>Keep work organized<br/>Easy to review"]
    A -->|"✅"| B3["Clear original commit messages<br/>Fixup targets them correctly"]
    A -->|"✅"| B4["Test after rebase<br/>Ensure no regressions"]

    style B1 fill:#90EE90
    style B2 fill:#90EE90
    style B3 fill:#90EE90
    style B4 fill:#90EE90
```

### ❌ DON'T

```mermaid
graph TD
    A["Avoid These"] -->|"❌"| B1["Rebase already-pushed commits<br/>Can confuse teammates"]
    A -->|"❌"| B2["Fixup on master/main<br/>Merge branches instead"]
    A -->|"❌"| B3["Fixup ancient commits<br/>Risk losing context"]
    A -->|"❌"| B4["Forget to test after rebase<br/>Bugs can hide during squashing"]

    style B1 fill:#FFB6C1
    style B2 fill:#FFB6C1
    style B3 fill:#FFB6C1
    style B4 fill:#FFB6C1
```

### Common Issues & Solutions

```mermaid
graph TD
    Problem1["❌ Fixup commit message<br/>doesn't match original"] -->|"Solution"| Sol1["Ensure message starts with<br/>exact original message"]

    Problem2["❌ Autosquash didn't work<br/>Fixup still marked 'pick'"] -->|"Solution"| Sol2["Check spelling of 'fixup!'<br/>Exact match required"]

    Problem3["❌ Merge conflicts during rebase"] -->|"Solution"| Sol3["Resolve conflicts<br/>git add .<br/>git rebase --continue"]

    Problem4["❌ Wrong commit squashed"] -->|"Solution"| Sol4["Abort & retry:<br/>git rebase --abort"]

    style Sol1 fill:#87CEEB
    style Sol2 fill:#87CEEB
    style Sol3 fill:#87CEEB
    style Sol4 fill:#87CEEB
```

---

## Part 8: Quick Reference

### Command Cheat Sheet

```bash
# Create a fixup commit (commits changes as fixup of original)
git commit --fixup <commit-hash>

# Interactive rebase with autosquash enabled
git rebase -i --autosquash HEAD~<number-of-commits>

# Shorthand for rebase (no -i needed if only squashing)
git rebase --autosquash HEAD~3

# If something goes wrong, abort
git rebase --abort

# Continue after resolving conflicts
git rebase --continue

# View original commit message for fixup reference
git log --oneline | grep "Feature"

# See what will be squashed
git log --oneline HEAD~5..HEAD
```

### Decision Tree: When to Use Fixup

```mermaid
graph TD
    A["Is there a mistake in<br/>a recent commit?"] -->|Yes| B["Is it the LAST commit?"]

    B -->|Yes| C["Just use<br/>git commit --amend"]
    B -->|No| D["Use fixup commit<br/>git commit --fixup &lt;hash&gt;"]

    A -->|No| E["Need to combine<br/>multiple commits?"]
    E -->|Yes| F["Use interactive rebase<br/>git rebase -i --autosquash"]
    E -->|No| G["Create new feature<br/>commit (normal flow)"]

    style C fill:#87CEEB
    style D fill:#90EE90
    style F fill:#90EE90
    style G fill:#87CEEB
```

---

## Summary

```mermaid
graph TB
    A["Git Fixup Commits"]

    A -->|"Core Concept"| B["Special commits that<br/>mark 'this fixes previous'<br/>Using 'fixup!' prefix"]

    A -->|"Creation"| C["git commit --fixup &lt;hash&gt;<br/>Creates marked commit"]

    A -->|"Integration"| D["git rebase -i --autosquash<br/>Auto-squashes into original"]

    A -->|"Benefits"| E["✅ Clean history<br/>✅ Easier reviews<br/>✅ Flexible workflow<br/>✅ Keeps original commit"]

    A -->|"When to Use"| F["Bug fixes in recent commits<br/>Code review feedback<br/>Testing iterations<br/>Before merging PR"]

    style A fill:#4A90E2
    style B fill:#90EE90
    style C fill:#90EE90
    style D fill:#90EE90
    style E fill:#FFD700
    style F fill:#FFD700
```

---

## Final Example: Complete Workflow

Let's put it all together:

```bash
# 1. You're working on a feature
git log --oneline
# 5 - docs: Update README
# 4 - feat: Add user validation
# 3 - feat: Add login endpoint  ← Found a bug here
# 2 - chore: Setup project
# 1 - Initial commit

# 2. You find a bug in commit 3 and fix it
git add .
git commit --fixup 3abc123  # Creates "fixup! feat: Add login endpoint"

# 3. Continue working normally (can create more fixups)
git add docs/
git commit -m "docs: Update README"

# 4. Before submitting PR, clean up history
git rebase -i --autosquash HEAD~3

# 5. Interactive editor shows:
# pick 3abc123 feat: Add login endpoint
# fixup 6def456 fixup! feat: Add login endpoint
# pick 5bcde12 docs: Update README

# 6. Save and close - rebase completes automatically

# 7. History is now clean:
git log --oneline
# 5 - docs: Update README
# 4 - feat: Add login endpoint (with bug fix integrated)
# 3 - feat: Add user validation
# 2 - chore: Setup project
# 1 - Initial commit

# 8. Push to your feature branch
git push -f origin feature-branch  # -f because history changed (safe on feature branches)
```

---

## Resources & Further Reading

- Git documentation: https://git-scm.com/docs/git-commit
- Interactive rebase guide: https://git-scm.com/book/en/v2/Git-Tools-Rewriting-History
- Autosquash in depth: https://git-scm.com/docs/git-rebase#Documentation/git-rebase.txt---autosquash

---

**Happy committing! 🚀**
