---
uid: 3105f9d1-5c76-483c-993a-828c5c71be14
title: "Git Internal Data Structures: A Complete Visual Guide"
created: 2026-02-25T13:14:28
modified: 2026-02-25T13:54:51
aliases:
  - "Git Internal Data Structures: A Complete Visual Guide"
linter-yaml-title-alias: "Git Internal Data Structures: A Complete Visual Guide"
---

# Git Internal Data Structures: A Complete Visual Guide

> **Goal:** Build a visual mental model of Git's object graph so you can navigate it confidently with `git cat-file`, CLI tools, and Vim-Fugitive.

---

## Table of Contents

1. [The Core Idea: A Content-Addressable Object Store](<#1-the-core-idea>)
2. [The Four Object Types](<#2-the-four-object-types>)
3. [How Objects Relate to Each Other](<#3-how-objects-relate>)
4. [A Concrete Example: Files → Objects](<#4-concrete-example>)
5. [The .git Directory on Disk](<#5-the-git-directory>)
6. [Refs, Branches, Tags, and HEAD](<#6-refs-branches-tags-head>)
7. [The Three Trees: Working Dir, Index, Repository](<#7-the-three-trees>)
8. [The Full Picture](<#8-the-full-picture>)
9. [CLI Cheat Sheet: Querying Objects](<#9-cli-cheat-sheet>)
10. [Vim-Fugitive Navigation Guide](<#10-vim-fugitive-navigation>)

---

## 1. The Core Idea

Git is, at its heart, a **content-addressable key-value store** wrapped in a version control system. Every piece of data stored in Git is:

1. Compressed with zlib
2. Given a **SHA-1 hash** of its content (40 hex characters)
3. Stored at `.git/objects/<first-2-chars>/<remaining-38-chars>`

The hash **is** the address. If you know the content, you know the address. If the content changes by one byte, the hash changes completely.

```
Content → SHA-1 hash → File path on disk
"blob 5\0hello" → a9993e364706816aba3e25717850c26c9cd0d89d → .git/objects/a9/993e...
```

This is why Git is **immutable** — objects are never modified, only new ones are created.

---

## 2. The Four Object Types

### 2.1 Blob — "File Contents"

A blob stores the **raw content of a file**. It knows nothing about filenames, paths, or permissions. Two files with identical content share one blob.

```
Format on disk:
blob <content-length>\0<content>
```

### 2.2 Tree — "Directory Listing"

A tree stores a **directory snapshot**. It is a list of entries, each containing:
- A **mode** (file permissions: `100644` regular file, `100755` executable, `040000` directory, `120000` symlink)
- A **name** (filename or subdirectory name)
- A **SHA-1** pointing to a blob (for files) or another tree (for subdirectories)

```
Format: mode SP name NUL sha1-bytes
040000 tree a1b2c3d4...  src
100644 blob e5f6a7b8...  README.md
100755 blob 9c0d1e2f...  run.sh
```

### 2.3 Commit — "A Snapshot in Time"

A commit stores:
- A pointer to a **root tree** (the full directory snapshot)
- Zero or more **parent commit SHA-1s** (zero = initial commit, two = merge)
- **Author** and **Committer** (name, email, timestamp, timezone)
- A **commit message**

```
Format:
tree <tree-sha>
parent <parent-sha>       ← zero or more of these
author Name <email> timestamp tz
committer Name <email> timestamp tz

Commit message here
```

### 2.4 Tag (Annotated) — "A Named, Signed Snapshot"

An annotated tag is a **first-class object** (unlike lightweight tags which are just refs). It stores:
- The **object it points to** (usually a commit SHA-1)
- The **type** of the object it points to (`commit`, `tree`, `blob`)
- A **tagger** (name, email, timestamp)
- A **tag message** (and optionally a GPG signature)

```
Format:
object <sha>
type commit
tag v1.0.0
tagger Name <email> timestamp tz

Tag message here
-----BEGIN PGP SIGNATURE-----   ← optional
...
```

---

## 3. How Objects Relate

### 3.1 The Object Type Hierarchy

```mermaid
graph TD
    TAG["🏷️ Tag Object<br/><i>type: tag</i><br/>points to a commit"]
    COMMIT["📸 Commit Object<br/><i>type: commit</i><br/>author, message, tree ptr"]
    TREE["📁 Tree Object<br/><i>type: tree</i><br/>list of mode+name+sha entries"]
    BLOB["📄 Blob Object<br/><i>type: blob</i><br/>raw file bytes"]
    SUBTREE["📁 Sub-Tree Object<br/><i>type: tree</i><br/>(nested directory)"]

    TAG -->|"object (SHA)"| COMMIT
    COMMIT -->|"tree (SHA)"| TREE
    TREE -->|"blob entry"| BLOB
    TREE -->|"tree entry"| SUBTREE
    SUBTREE -->|"blob entry"| BLOB

    style TAG fill:#f9d71c,stroke:#c9a800,color:#333
    style COMMIT fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style TREE fill:#7dcc7d,stroke:#4a994a,color:#fff
    style BLOB fill:#ff9d6e,stroke:#d4622a,color:#fff
    style SUBTREE fill:#7dcc7d,stroke:#4a994a,color:#fff
```

### 3.2 How a Commit Chain Looks (History)

```mermaid
graph RL
    C3["📸 Commit C3<br/>SHA: a1b2c3<br/>'Add feature X'"]
    C2["📸 Commit C2<br/>SHA: d4e5f6<br/>'Fix typo'"]
    C1["📸 Commit C1<br/>SHA: g7h8i9<br/>'Initial commit'"]

    T3["📁 Tree 3<br/>SHA: j0k1l2"]
    T2["📁 Tree 2<br/>SHA: m3n4o5"]
    T1["📁 Tree 1<br/>SHA: p6q7r8"]

    B_readme["📄 Blob: README.md<br/>SHA: s9t0u1"]
    B_main_v1["📄 Blob: main.py v1<br/>SHA: v2w3x4"]
    B_main_v2["📄 Blob: main.py v2<br/>SHA: y5z6a7"]
    B_main_v3["📄 Blob: main.py v3<br/>SHA: b8c9d0"]

    C3 -->|parent| C2
    C2 -->|parent| C1

    C3 -->|tree| T3
    C2 -->|tree| T2
    C1 -->|tree| T1

    T1 -->|"100644 README.md"| B_readme
    T1 -->|"100644 main.py"| B_main_v1

    T2 -->|"100644 README.md"| B_readme
    T2 -->|"100644 main.py"| B_main_v2

    T3 -->|"100644 README.md"| B_readme
    T3 -->|"100644 main.py"| B_main_v3

    style C3 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style C2 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style C1 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style T3 fill:#7dcc7d,stroke:#4a994a,color:#fff
    style T2 fill:#7dcc7d,stroke:#4a994a,color:#fff
    style T1 fill:#7dcc7d,stroke:#4a994a,color:#fff
    style B_readme fill:#ff9d6e,stroke:#d4622a,color:#fff
    style B_main_v1 fill:#ff9d6e,stroke:#d4622a,color:#fff
    style B_main_v2 fill:#ff9d6e,stroke:#d4622a,color:#fff
    style B_main_v3 fill:#ff9d6e,stroke:#d4622a,color:#fff
```

> **Key insight:** `README.md` never changed across the three commits, so all three trees point to the **same blob SHA**. Git deduplicates automatically — no copying.

### 3.3 A Merge Commit (Two Parents)

```mermaid
graph RL
    M["📸 Merge Commit<br/>SHA: merge1<br/>'Merge feature into main'"]
    F2["📸 Feature Tip<br/>SHA: feat2<br/>'Feature: step 2'"]
    F1["📸 Feature Start<br/>SHA: feat1<br/>'Feature: step 1'"]
    BASE["📸 Common Ancestor<br/>SHA: base1<br/>'Before feature'"]

    M -->|"parent 1"| BASE
    M -->|"parent 2"| F2
    F2 -->|parent| F1
    F1 -->|parent| BASE

    style M fill:#c084fc,stroke:#7e22ce,color:#fff
    style F2 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style F1 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style BASE fill:#6db5fe,stroke:#3a8fd4,color:#fff
```

---

## 4. A Concrete Example: Files → Objects

### 4.1 The Project Layout

Suppose you have this project:

```
my-project/
├── README.md
├── src/
│   ├── main.py
│   └── utils.py
└── tests/
    └── test_main.py
```

### 4.2 The Full Object Graph for One Commit

```mermaid
graph TD
    HEAD["🔖 HEAD<br/>→ refs/heads/main"]
    MAIN["🌿 refs/heads/main<br/>→ SHA: abc123"]

    COMMIT["📸 Commit abc123<br/>author: Eric<br/>'Initial commit'<br/>tree: def456"]

    ROOT_TREE["📁 Root Tree def456<br/>─────────────────<br/>040000 tree  src/ → ghi789<br/>040000 tree  tests/ → jkl012<br/>100644 blob  README.md → mno345"]

    SRC_TREE["📁 src/ Tree ghi789<br/>─────────────────<br/>100644 blob  main.py → pqr678<br/>100644 blob  utils.py → stu901"]

    TESTS_TREE["📁 tests/ Tree jkl012<br/>─────────────────<br/>100644 blob  test_main.py → vwx234"]

    BLOB_README["📄 Blob mno345<br/>'# My Project<br/>...'"]
    BLOB_MAIN["📄 Blob pqr678<br/>'def main():<br/>    ...'"]
    BLOB_UTILS["📄 Blob stu901<br/>'def helper():<br/>    ...'"]
    BLOB_TEST["📄 Blob vwx234<br/>'import main<br/>...'"]

    HEAD --> MAIN
    MAIN --> COMMIT
    COMMIT -->|tree| ROOT_TREE
    ROOT_TREE -->|"040000 src/"| SRC_TREE
    ROOT_TREE -->|"040000 tests/"| TESTS_TREE
    ROOT_TREE -->|"100644 README.md"| BLOB_README
    SRC_TREE -->|"100644 main.py"| BLOB_MAIN
    SRC_TREE -->|"100644 utils.py"| BLOB_UTILS
    TESTS_TREE -->|"100644 test_main.py"| BLOB_TEST

    style HEAD fill:#f9d71c,stroke:#c9a800,color:#333
    style MAIN fill:#f9d71c,stroke:#c9a800,color:#333
    style COMMIT fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style ROOT_TREE fill:#7dcc7d,stroke:#4a994a,color:#fff
    style SRC_TREE fill:#7dcc7d,stroke:#4a994a,color:#fff
    style TESTS_TREE fill:#7dcc7d,stroke:#4a994a,color:#fff
    style BLOB_README fill:#ff9d6e,stroke:#d4622a,color:#fff
    style BLOB_MAIN fill:#ff9d6e,stroke:#d4622a,color:#fff
    style BLOB_UTILS fill:#ff9d6e,stroke:#d4622a,color:#fff
    style BLOB_TEST fill:#ff9d6e,stroke:#d4622a,color:#fff
```

---

## 5. The .git Directory on Disk

### 5.1 Directory Structure

```mermaid
graph TD
    GIT[".git/"]

    GIT --> HEAD_FILE["HEAD<br/>'ref: refs/heads/main'"]
    GIT --> CONFIG["config<br/>(repo settings)"]
    GIT --> INDEX["index<br/>(staging area / index)"]
    GIT --> OBJECTS[".git/objects/"]
    GIT --> REFS[".git/refs/"]
    GIT --> LOGS[".git/logs/"]
    GIT --> PACKED[".git/packed-refs<br/>(compressed refs)"]

    OBJECTS --> OBJ_LOOSE["Loose Objects<br/>ab/cdef1234..."]
    OBJECTS --> OBJ_PACK["pack/<br/>*.pack + *.idx"]
    OBJECTS --> OBJ_INFO["info/"]

    REFS --> REFS_HEADS[".git/refs/heads/<br/>(local branches)"]
    REFS --> REFS_TAGS[".git/refs/tags/<br/>(tags)"]
    REFS --> REFS_REMOTE[".git/refs/remotes/<br/>(remote tracking)"]

    REFS_HEADS --> MAIN_FILE["main<br/>'abc123...'"]
    REFS_HEADS --> DEV_FILE["dev<br/>'def456...'"]

    LOGS --> LOG_HEAD[".git/logs/HEAD<br/>(reflog)"]
    LOGS --> LOG_REFS[".git/logs/refs/<br/>(branch reflogs)"]

    style GIT fill:#e8e8e8,stroke:#888,color:#333
    style OBJECTS fill:#fff3cd,stroke:#d4a017,color:#333
    style REFS fill:#d1ecf1,stroke:#0c5460,color:#333
    style LOGS fill:#d4edda,stroke:#155724,color:#333
    style OBJ_PACK fill:#f8d7da,stroke:#721c24,color:#333
```

### 5.2 How an Object SHA Maps to a File Path

```mermaid
flowchart LR
    SHA["SHA-1:<br/>abc123def456..."]
    DIR[".git/objects/ab/"]
    FILE["c123def456...<br/>(38 chars)"]
    CONTENT["zlib-compressed:<br/>'commit 200\\0tree...\<br/>'"]

    SHA -->|"first 2 chars = directory"| DIR
    SHA -->|"remaining 38 chars = filename"| FILE
    DIR --> FILE
    FILE -->|"decompress"| CONTENT

    style SHA fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style DIR fill:#7dcc7d,stroke:#4a994a,color:#fff
    style FILE fill:#ff9d6e,stroke:#d4622a,color:#fff
    style CONTENT fill:#e8e8e8,stroke:#888,color:#333
```

### 5.3 Object Wire Format (What's Inside Each File)

| Object Type | Stored As |
|-------------|-----------|
| Blob | `blob <length>\0<raw bytes>` |
| Tree | `tree <length>\0<mode> <name>\0<20-byte-sha>...` (repeating) |
| Commit | `commit <length>\0tree <sha><br/>parent <sha><br/>author...<br/>committer...<br/><br/><message>` |
| Tag | `tag <length>\0object <sha><br/>type <type><br/>tag <name><br/>tagger...<br/><br/><message>` |

Pack files (`.git/objects/pack/`) are created by `git gc` and bundle many objects together for efficiency. The `.idx` file provides an offset table into the `.pack` file.

---

## 6. Refs, Branches, Tags, and HEAD

### 6.1 The Ref System

A **ref** is just a file containing a 40-character SHA-1 (or a symbolic ref pointing to another ref). That's it.

```mermaid
graph TD
    subgraph "Symbolic Refs (text files with 'ref: path')"
        HEAD_SYM["HEAD<br/>→ ref: refs/heads/main"]
        ORIG_HEAD["ORIG_HEAD<br/>→ abc123..."]
        MERGE_HEAD["MERGE_HEAD<br/>→ (during merge)"]
    end

    subgraph "Branch Refs (.git/refs/heads/)"
        MAIN_REF["main<br/>→ abc123..."]
        DEV_REF["dev<br/>→ def456..."]
        FEAT_REF["feature/login<br/>→ ghi789..."]
    end

    subgraph "Tag Refs (.git/refs/tags/)"
        TAG_LIGHT["v1.0<br/>→ abc123... (lightweight: points to commit)"]
        TAG_ANN["v2.0<br/>→ jkl012... (annotated: points to tag object)"]
    end

    subgraph "Remote Refs (.git/refs/remotes/)"
        ORIGIN_MAIN["origin/main<br/>→ mno345..."]
    end

    subgraph "Commits"
        C_ABC["📸 Commit abc123"]
        C_DEF["📸 Commit def456"]
        C_GHI["📸 Commit ghi789"]
        C_MNO["📸 Commit mno345"]
    end

    subgraph "Tag Object"
        TAG_OBJ["🏷️ Tag Object jkl012<br/>points to → abc123"]
    end

    HEAD_SYM --> MAIN_REF
    MAIN_REF --> C_ABC
    DEV_REF --> C_DEF
    FEAT_REF --> C_GHI
    TAG_LIGHT --> C_ABC
    TAG_ANN --> TAG_OBJ
    TAG_OBJ --> C_ABC
    ORIGIN_MAIN --> C_MNO

    style HEAD_SYM fill:#f9d71c,stroke:#c9a800,color:#333
    style MAIN_REF fill:#c084fc,stroke:#7e22ce,color:#fff
    style DEV_REF fill:#c084fc,stroke:#7e22ce,color:#fff
    style FEAT_REF fill:#c084fc,stroke:#7e22ce,color:#fff
    style TAG_LIGHT fill:#f9d71c,stroke:#c9a800,color:#333
    style TAG_ANN fill:#f9d71c,stroke:#c9a800,color:#333
    style TAG_OBJ fill:#f9d71c,stroke:#c9a800,color:#333
    style ORIGIN_MAIN fill:#fb923c,stroke:#c2410c,color:#fff
    style C_ABC fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style C_DEF fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style C_GHI fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style C_MNO fill:#6db5fe,stroke:#3a8fd4,color:#fff
```

### 6.2 Detached HEAD State

```mermaid
graph LR
    subgraph "Normal: HEAD → branch → commit"
        H1["HEAD"] -->|"symbolic ref"| B1["refs/heads/main"]
        B1 --> CM1["📸 Commit abc123"]
    end

    subgraph "Detached: HEAD → commit directly"
        H2["HEAD"] -->|"direct SHA"| CM2["📸 Commit def456<br/>(no branch points here)"]
    end

    style H1 fill:#f9d71c,stroke:#c9a800,color:#333
    style H2 fill:#f87171,stroke:#dc2626,color:#fff
    style B1 fill:#c084fc,stroke:#7e22ce,color:#fff
    style CM1 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style CM2 fill:#6db5fe,stroke:#3a8fd4,color:#fff
```

> **Warning:** In detached HEAD, new commits you make won't be reachable after you switch branches unless you create a branch first.

---

## 7. The Three Trees

Git manages three "trees" (not tree objects, but tree-structured data structures):

```mermaid
graph LR
    subgraph "1. Working Directory"
        WD["Your actual files<br/>on disk<br/><br/>Editable freely<br/>Not tracked by Git<br/>until staged"]
    end

    subgraph "2. Index / Staging Area"
        IDX[".git/index<br/><br/>A binary file:<br/>mode + sha + stage + name<br/>for every tracked file<br/><br/>The 'next commit'"]
    end

    subgraph "3. Repository (HEAD)"
        REPO["The current commit's<br/>tree, as read from<br/>the object store<br/><br/>The 'last commit'"]
    end

    WD -->|"git add"| IDX
    IDX -->|"git commit"| REPO
    REPO -->|"git checkout .<br/>git restore ."| WD
    REPO -->|"git reset HEAD"| IDX

    style WD fill:#ff9d6e,stroke:#d4622a,color:#fff
    style IDX fill:#f9d71c,stroke:#c9a800,color:#333
    style REPO fill:#6db5fe,stroke:#3a8fd4,color:#fff
```

### 7.1 What git status Actually Does

```mermaid
flowchart TD
    A["git status"] --> B{"Compare<br/>HEAD tree<br/>vs Index"}
    B -->|"Different"| C["Staged changes<br/>(green)"]
    B -->|"Same"| D["Nothing staged"]
    A --> E{"Compare<br/>Index<br/>vs Working Dir"}
    E -->|"Different"| F["Unstaged changes<br/>(red)"]
    E -->|"Same"| G["Working tree clean"]
    A --> H{"Files in Working Dir<br/>not in Index?"}
    H -->|"Yes"| I["Untracked files<br/>(red ??)"]

    style A fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style C fill:#7dcc7d,stroke:#4a994a,color:#fff
    style F fill:#f87171,stroke:#dc2626,color:#fff
    style I fill:#f87171,stroke:#dc2626,color:#fff
```

---

## 8. The Full Picture

```mermaid
graph TB
    subgraph DISK["💾 Disk: .git/"]
        subgraph OBJECTS_BOX[".git/objects/"]
            BL1["📄 Blob<br/>file contents"]
            BL2["📄 Blob<br/>file contents"]
            BL3["📄 Blob<br/>file contents"]
            TR1["📁 Tree<br/>dir listing"]
            TR2["📁 Tree<br/>dir listing"]
            CM1["📸 Commit<br/>snapshot ptr"]
            CM2["📸 Commit<br/>snapshot ptr"]
            TG1["🏷️ Tag object"]
        end
        subgraph REFS_BOX[".git/refs/"]
            BR["refs/heads/main → SHA"]
            TAG_R["refs/tags/v1 → SHA"]
            REM["refs/remotes/origin/main → SHA"]
        end
        HEAD_F["HEAD → ref: refs/heads/main"]
        IDX_F["index (staging area)"]
    end

    subgraph WORK["📂 Working Directory"]
        F1["README.md"]
        F2["src/main.py"]
        F3["src/utils.py"]
    end

    CM2 -->|parent| CM1
    CM2 -->|tree| TR1
    CM1 -->|tree| TR2
    TR1 -->|blob| BL1
    TR1 -->|blob| BL2
    TR2 -->|blob| BL1
    TR2 -->|blob| BL3
    TG1 -->|object| CM2
    BR --> CM2
    TAG_R --> TG1
    HEAD_F --> BR

    IDX_F -.->|"git add"| BL1
    IDX_F -.->|"git add"| BL2
    WORK -.->|"git add"| IDX_F
    CM2 -.->|"git checkout"| WORK

    style DISK fill:#f0f0f0,stroke:#999
    style OBJECTS_BOX fill:#fff3cd,stroke:#d4a017
    style REFS_BOX fill:#d1ecf1,stroke:#0c5460
    style WORK fill:#d4edda,stroke:#155724
    style BL1 fill:#ff9d6e,stroke:#d4622a,color:#fff
    style BL2 fill:#ff9d6e,stroke:#d4622a,color:#fff
    style BL3 fill:#ff9d6e,stroke:#d4622a,color:#fff
    style TR1 fill:#7dcc7d,stroke:#4a994a,color:#fff
    style TR2 fill:#7dcc7d,stroke:#4a994a,color:#fff
    style CM1 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style CM2 fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style TG1 fill:#f9d71c,stroke:#c9a800,color:#333
```

---

## 9. CLI Cheat Sheet: Querying Objects

### 9.1 The Swiss Army Knife: `git cat-file`

`git cat-file` is the **primary tool** for inspecting raw Git objects.

| Command | What it shows | Example |
|---------|--------------|---------|
| `git cat-file -t <sha>` | **Type** of object | `blob`, `tree`, `commit`, `tag` |
| `git cat-file -s <sha>` | **Size** in bytes | `1234` |
| `git cat-file -p <sha>` | **Pretty-print** contents | (see below) |
| `git cat-file --batch` | Read many SHAs from stdin | `echo sha \| git cat-file --batch` |
| `git cat-file --batch-all-objects --batch-check` | List ALL objects | Type+size of every object |

```bash
# Inspect a commit
git cat-file -p HEAD
# tree a1b2c3d4...
# parent e5f6a7b8...
# author Eric Ziko <e@z.com> 1708000000 +0200
# committer Eric Ziko <e@z.com> 1708000000 +0200
#
# My commit message

# Inspect a tree (root tree of HEAD)
git cat-file -p HEAD^{tree}
# 040000 tree a1b2...  src
# 100644 blob c3d4...  README.md

# Inspect a blob
git cat-file -p HEAD:README.md
# (raw file contents)

# Inspect a nested path
git cat-file -p HEAD:src/main.py
```

### 9.2 Listing Tree Contents: `git ls-tree`

```bash
# List the root tree of HEAD
git ls-tree HEAD

# List recursively (all files, all levels)
git ls-tree -r HEAD

# List with full object names
git ls-tree -r --name-only HEAD

# List a subdirectory
git ls-tree HEAD src/

# List a specific commit's tree
git ls-tree abc123def
```

Output format: `<mode> <type> <sha>\t<path>`

```
100644 blob a1b2c3d4e5f6...  README.md
040000 tree f7e8d9c0b1a2...  src
```

### 9.3 Walking History: `git log`

```bash
# Graph view with branches
git log --oneline --graph --all --decorate

# Show what changed in each commit
git log --stat

# Show diffs inline
git log -p

# Show specific file history
git log --follow -- src/main.py

# Show commits touching a string
git log -S "function_name" --all

# Show merge commits only
git log --merges --oneline

# Show commits between two refs
git log main..feature-branch

# Show ancestry: commits reachable from A but not B
git log A ^B

# Reverse: oldest first
git log --reverse

# Pretty format: hash, author, date, subject
git log --format="%h %an %ar %s"
```

### 9.4 Inspecting Refs

```bash
# Show what HEAD points to
cat .git/HEAD                     # symbolic: ref: refs/heads/main
git symbolic-ref HEAD             # refs/heads/main
git rev-parse HEAD                # full SHA

# Show all refs
git for-each-ref
git for-each-ref --format="%(refname) %(objectname:short) %(subject)"

# Show all branches (local + remote)
git branch -avv

# Show all tags with messages
git tag -n

# Show what a ref resolves to
git rev-parse main
git rev-parse v1.0^{}             # dereference tag to commit
git rev-parse HEAD~3              # 3 commits before HEAD
git rev-parse HEAD^2              # second parent (merge commit)
```

### 9.5 Ref Arithmetic (Revision Syntax)

| Syntax | Meaning |
|--------|---------|
| `HEAD` | Current commit |
| `HEAD^` or `HEAD~1` | Parent commit |
| `HEAD^^` or `HEAD~2` | Grandparent commit |
| `HEAD~N` | N commits back |
| `HEAD^2` | Second parent (merge commit) |
| `main@{3}` | Where main was 3 moves ago (reflog) |
| `main@{yesterday}` | Where main was yesterday |
| `v1.0^{}` | Dereference tag to the commit |
| `abc123:path/to/file` | Blob at path in commit |
| `HEAD^{tree}` | Root tree of HEAD commit |

### 9.6 Diffing Objects

```bash
# Diff working dir vs index (unstaged changes)
git diff

# Diff index vs HEAD (staged changes, "what will be committed")
git diff --cached
git diff --staged

# Diff working dir vs HEAD (all changes)
git diff HEAD

# Diff two commits
git diff abc123 def456

# Diff specific file between commits
git diff HEAD~3 HEAD -- src/main.py

# Diff two branches
git diff main..feature

# Diff just the names of changed files
git diff --name-only main..feature

# Show stats only
git diff --stat main..feature
```

### 9.7 Finding Objects

```bash
# Find object SHA for a file in the index
git ls-files -s src/main.py
# 100644 a1b2c3d4... 0  src/main.py

# Find all objects of a type
git cat-file --batch-all-objects --batch-check | grep '^.\{40\} blob'

# Find dangling (unreferenced) objects
git fsck --unreachable

# Find the blob SHA for a file at HEAD
git rev-parse HEAD:src/main.py

# Find commits that modified a file
git log --all --full-history -- path/to/file

# Find what introduced a bug (bisect)
git bisect start
git bisect bad HEAD
git bisect good v1.0
git bisect run ./test.sh    # automate
```

### 9.8 Inspecting the Index

```bash
# List all files in the staging area
git ls-files

# List with stage number, sha, and path
git ls-files -s

# List untracked files
git ls-files --others --exclude-standard

# Show diff between index and working dir
git diff

# Show diff between HEAD and index
git diff --cached
```

### 9.9 Object Plumbing Commands Quick Reference

```bash
# Hash a file (compute its blob SHA without storing)
git hash-object path/to/file

# Store a blob manually
echo "hello" | git hash-object --stdin -w

# Manually create a tree from the index
git write-tree

# Manually create a commit
git commit-tree <tree-sha> -p <parent-sha> -m "message"

# Update a ref
git update-ref refs/heads/my-branch <sha>

# Show reflog (local history of ref movements)
git reflog
git reflog show main
```

---

## 10. Vim-Fugitive Navigation Guide

Vim-Fugitive (by tpope) gives you a **Git-aware buffer layer** inside Vim. The key insight is that Fugitive lets you navigate the Git object graph using Vim's native movement keys.

### 10.1 The Fugitive Mental Model

```mermaid
graph TD
    STATUS[":Git (Fugitive Status)<br/>:G<br/><br/>Your entry point.<br/>Shows working dir vs index vs HEAD."]

    DIFF[":Gdiffsplit<br/>:Gvdiffsplit<br/><br/>Side-by-side diff view<br/>of a file (index vs working)"]

    LOG[":Git log<br/>:Gclog<br/><br/>Commit history in<br/>the quickfix list"]

    BLAME[":Git blame<br/>:G blame<br/><br/>Annotate file with<br/>commit info per line"]

    TREE["Tree Browse<br/>:Gedit HEAD^{tree}<br/>Browse any tree object"]

    OBJECT["Object Buffer<br/>:Gedit <sha><br/>View raw object content"]

    COMMIT_BUF["Commit Buffer<br/>:Gedit HEAD<br/>or press Enter on<br/>a commit SHA"]

    STATUS -->|"- to stage/unstage<br/>= to inline diff<br/>Enter to open"| DIFF
    STATUS -->|"cc to commit<br/>ca to amend"| COMMIT_BUF
    STATUS -->|"cL or :Gclog"| LOG
    LOG -->|"Enter on SHA<br/>or :Gedit SHA"| COMMIT_BUF
    COMMIT_BUF -->|"Enter on tree SHA"| TREE
    COMMIT_BUF -->|"Enter on blob SHA"| OBJECT
    TREE -->|"Enter on entry"| OBJECT
    OBJECT -->|"press Enter on<br/>blob SHA in tree"| OBJECT

    style STATUS fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style DIFF fill:#7dcc7d,stroke:#4a994a,color:#fff
    style LOG fill:#c084fc,stroke:#7e22ce,color:#fff
    style BLAME fill:#f9d71c,stroke:#c9a800,color:#333
    style TREE fill:#7dcc7d,stroke:#4a994a,color:#fff
    style OBJECT fill:#ff9d6e,stroke:#d4622a,color:#fff
    style COMMIT_BUF fill:#6db5fe,stroke:#3a8fd4,color:#fff
```

### 10.2 Core Fugitive Commands

| Command | Description |
|---------|-------------|
| `:Git` or `:G` | Open Fugitive status window |
| `:Gedit <object>` | Open any Git object (SHA, HEAD, HEAD:path) |
| `:Gread <object>` | Read object into current buffer |
| `:Gdiffsplit` | Diff current file (working dir vs index) |
| `:Gvdiffsplit` | Same, vertical split |
| `:Git log` | Run `git log` in a buffer |
| `:Gclog` | Load commit history into quickfix list |
| `:Git blame` | Blame current file |
| `:Git difftool` | Open diffs in quickfix |

### 10.3 Navigating the Status Window (`:G`)

```
:G  (opens status buffer)

┌─────────────────────────────────────────┐
│ Head: main                              │
│ Push: origin/main                       │
│                                         │
│ Staged Changes (2)                      │
│   M  src/main.py                        │
│   A  src/newfile.py                     │
│                                         │
│ Unstaged Changes (1)                    │
│   M  README.md                          │
│                                         │
│ Untracked Files (1)                     │
│   ?  scratch.py                         │
└─────────────────────────────────────────┘
```

| Key | Action |
|-----|--------|
| `s` | **Stage** file under cursor (or selection) |
| `u` | **Unstage** file under cursor |
| `-` | Toggle stage/unstage |
| `=` | Toggle inline diff for file under cursor |
| `>` / `<` | Expand / collapse inline diff |
| `Enter` | Open file in new split |
| `o` | Open file in horizontal split |
| `v` | Open file in vertical split |
| `t` | Open file in new tab |
| `p` | **Patch** — stage individual hunks interactively |
| `cc` | **Commit** staged changes |
| `ca` | Amend previous commit |
| `ce` | Amend without editing message |
| `cw` | Reword last commit message |
| `coo` | Checkout file (discard working dir changes) |
| `czz` | **Stash** |
| `czp` | Stash pop |
| `g?` | Help for all bindings |
| `q` | Close status window |

### 10.4 Navigating Commit History

```bash
# Open the git log in a scrollable buffer
:Git log --oneline --graph

# Load commits for CURRENT FILE into quickfix list (navigate with :cn, :cp)
:Gclog

# Load ALL commits into quickfix
:Gclog --all

# Load commits for a specific file
:Gclog -- %     # % = current file path
```

In the quickfix list (`:copen`):

| Key | Action |
|-----|--------|
| `Enter` | Jump to that commit |
| `:cn` | Next commit |
| `:cp` | Previous commit |

### 10.5 Navigating Objects — The Core Skill

This is where Fugitive shines for exploring the object graph. Use `:Gedit` as your navigation verb:

```vim
" Open a commit object
:Gedit HEAD
:Gedit abc123

" Open the root tree of HEAD
:Gedit HEAD^{tree}
:Gedit HEAD:

" Open a specific directory's tree
:Gedit HEAD:src/

" Open a specific file blob
:Gedit HEAD:src/main.py
:Gedit HEAD~3:src/main.py    " 3 commits ago

" Open a tag object
:Gedit v1.0
```

Once you're **inside a commit buffer** (`:Gedit HEAD`), the content looks like:

```
tree abc123def456...
parent 789ghi012jkl...
author Eric Ziko ...
committer Eric Ziko ...

My commit message
```

You can **press `Enter` on any SHA** and Fugitive will open that object. This lets you drill down:

```
Commit → (Enter on tree SHA) → Tree → (Enter on file entry) → Blob
```

### 10.6 Object Navigation Flow in Fugitive

```mermaid
flowchart TD
    START["Start:<br/>:Gedit HEAD<br/>or :Git log"]

    COMMIT_VIEW["Commit Buffer<br/>Shows: tree, parent(s),<br/>author, message"]

    TREE_VIEW["Tree Buffer<br/>Shows: mode type sha name<br/>for each entry"]

    BLOB_VIEW["Blob Buffer<br/>(file contents at<br/>that point in history)"]

    PARENT["Parent Commit Buffer<br/>(older commit)"]

    BACK["Press C-o<br/>(vim jumplist back)"]

    START --> COMMIT_VIEW
    COMMIT_VIEW -->|"Enter on tree SHA"| TREE_VIEW
    COMMIT_VIEW -->|"Enter on parent SHA"| PARENT
    PARENT -->|"Enter on tree SHA"| TREE_VIEW
    TREE_VIEW -->|"Enter on blob entry"| BLOB_VIEW
    TREE_VIEW -->|"Enter on tree entry"| TREE_VIEW
    BLOB_VIEW --> BACK
    TREE_VIEW --> BACK
    COMMIT_VIEW --> BACK
    BACK --> COMMIT_VIEW

    style START fill:#f9d71c,stroke:#c9a800,color:#333
    style COMMIT_VIEW fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style TREE_VIEW fill:#7dcc7d,stroke:#4a994a,color:#fff
    style BLOB_VIEW fill:#ff9d6e,stroke:#d4622a,color:#fff
    style PARENT fill:#6db5fe,stroke:#3a8fd4,color:#fff
    style BACK fill:#e8e8e8,stroke:#888,color:#333
```

> **Key tip:** Use Vim's jump list (`Ctrl-O` to go back, `Ctrl-I` to go forward) to navigate the object graph like browser history.

### 10.7 Git Blame Navigation

```vim
" Open blame for current file
:Git blame
" or
:G blame
```

In the blame view:

| Key | Action |
|-----|--------|
| `Enter` or `o` | Open the commit that introduced this line |
| `O` | Open commit in new tab |
| `p` | Preview commit in preview window |
| `~` | Go to the version of the file before this commit |
| `C` | Navigate to parent commit |
| `g?` | Show all bindings |
| `q` | Close blame |

### 10.8 Diff Navigation (`:Gdiffsplit`)

```vim
" Diff current file (working dir vs index)
:Gdiffsplit
:Gvdiffsplit    " vertical

" Diff current file against HEAD (working dir vs HEAD)
:Gdiffsplit HEAD

" Diff current file at two specific commits
:Gdiffsplit main..feature    " not quite right for files, use:
:Gedit main:% | Gvdiffsplit feature:%
```

In diff view, use standard Vim diff navigation:

| Key | Action |
|-----|--------|
| `]c` | Next change |
| `[c` | Previous change |
| `do` | Diff obtain (get change from other window) |
| `dp` | Diff put (send change to other window) |
| `:diffupdate` | Refresh diff |

### 10.9 Fugitive + Quickfix Workflow

```vim
" Search for a string across all commits (like git log -S)
:Git log -S "my_function" --all
" Then open the quickfix-style list and navigate

" See all commits that touched a file
:Gclog -- path/to/file.py

" Navigate quickfix
:copen      " open list
:cn         " next item
:cp         " previous item
:cc 5       " jump to item 5
```

### 10.10 Complete Fugitive Mental Map

```mermaid
mindmap
  root(("🔱 Fugitive<br/>Entry Points"))
    (":G / :Git")
      ["Status window"]
      ["Stage: s or -"]
      ["Unstage: u"]
      ["Inline diff: ="]
      ["Patch stage: p"]
      ["Commit: cc"]
      ["Amend: ca"]
    (":Gedit SHA")
      ["Browse any object"]
      ["Enter to drill in"]
      ["Ctrl-O to go back"]
      ["Commit → Tree → Blob"]
    (":Gclog")
      ["History → quickfix"]
      [":cn / :cp to navigate"]
      ["Enter to open commit"]
    (":Git blame / :G blame")
      ["Annotate file"]
      ["Enter → commit"]
      ["~ → parent version"]
    (":Gdiffsplit")
      ["Side-by-side diff"]
      ["]c / [c to jump hunks"]
      ["do / dp to resolve"]
```

---

## Quick Reference Card

```
┌──────────────────────────────────────────────────────────────┐
│                  GIT OBJECT QUICK REFERENCE                  │
├─────────────┬────────────────────────────────────────────────┤
│ Object Type │ CLI to Inspect                                 │
├─────────────┼────────────────────────────────────────────────┤
│ Any object  │ git cat-file -t <sha>   (type)                 │
│             │ git cat-file -p <sha>   (contents)             │
├─────────────┼────────────────────────────────────────────────┤
│ Commit      │ git cat-file -p HEAD                           │
│             │ git log --oneline --graph --all                │
│             │ git show HEAD                                  │
├─────────────┼────────────────────────────────────────────────┤
│ Tree        │ git ls-tree HEAD                               │
│             │ git ls-tree -r HEAD   (recursive)              │
│             │ git cat-file -p HEAD^{tree}                    │
├─────────────┼────────────────────────────────────────────────┤
│ Blob        │ git cat-file -p HEAD:path/to/file              │
│             │ git show HEAD:path/to/file                     │
├─────────────┼────────────────────────────────────────────────┤
│ Tag         │ git cat-file -p v1.0                           │
│             │ git tag -n   (list with messages)              │
├─────────────┼────────────────────────────────────────────────┤
│ Refs        │ git for-each-ref                               │
│             │ git rev-parse HEAD                             │
│             │ git symbolic-ref HEAD                          │
├─────────────┼────────────────────────────────────────────────┤
│ Index       │ git ls-files -s                                │
│             │ git diff --cached                              │
└─────────────┴────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────┐
│              FUGITIVE NAVIGATION QUICK REFERENCE             │
├──────────────────────────┬───────────────────────────────────┤
│ :G                       │ Status window                     │
│ :Gedit HEAD              │ Open current commit               │
│ :Gedit HEAD^{tree}       │ Open root tree of HEAD            │
│ :Gedit HEAD:src/         │ Open src/ tree                    │
│ :Gedit HEAD:src/file.py  │ Open file at HEAD                 │
│ :Gedit HEAD~3:src/f.py   │ Open file 3 commits ago           │
│ :Gclog                   │ History → quickfix                │
│ :Git blame               │ Blame current file                │
│ :Gdiffsplit              │ Diff current file                 │
│ Enter (on SHA in buffer) │ Drill into that object            │
│ Ctrl-O                   │ Go back (jump list)               │
│ Ctrl-I                   │ Go forward (jump list)            │
└──────────────────────────┴───────────────────────────────────┘
```

---

## Key Insights to Internalize

1. **Everything is a SHA.** Commits, trees, blobs, and tags are all just SHA-addressed objects in `.git/objects/`. A branch is just a file containing a SHA.
2. **A commit is a snapshot, not a diff.** Git stores full trees, not deltas. The "diff" you see with `git show` is computed on the fly by comparing a commit's tree to its parent's tree.
3. **Content deduplication is automatic.** Identical file contents = identical SHA = stored once. Unchanged files cost nothing in new commits.
4. **Branches are just 41-byte files.** `cat .git/refs/heads/main` will show you a SHA. Moving a branch is just overwriting that file.
5. **HEAD is the "you are here" marker.** It either points to a branch ref (normal) or directly to a SHA (detached HEAD).
6. **The index is the "next commit".** `git add` updates it. `git commit` turns it into a tree object and wraps it in a commit object.
7. **Fugitive's superpower is `Enter`.** When viewing any Git object buffer, pressing `Enter` on a SHA navigates into that object. Use `Ctrl-O` to backtrack. This gives you a browser-like traversal of the object graph.
