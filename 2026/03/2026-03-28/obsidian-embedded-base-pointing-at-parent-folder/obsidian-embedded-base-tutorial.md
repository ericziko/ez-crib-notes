---
title: 🤖❓ Obsidian Embedded Base Pointing at Parent Folder
created: 2026-03-28T00:00:00
modified: 2026-03-28T00:00:00
tags:
  - obsidian
  - bases
  - templates
  - workflow
uid: 28b2cdd8-fa86-43f1-a17c-85eff0e83923
---

# 🤖❓ Obsidian: Embedded Base That Dynamically Filters by Parent Folder

## 🤖💡 Overview

Obsidian **Bases** lets you embed a live database-style table directly inside a note. Combined with a **New Note template**, you can make the embedded base automatically filter for all files that live in the same folder as the note — without hardcoding any paths.

This is particularly useful for **index / MOC (Map of Content) notes**: drop one into any folder and it instantly shows a table of everything else in that folder.

---

## 📋 Prerequisites

| Requirement | Notes |
|---|---|
| Obsidian ≥ 1.8 | Bases is a core feature from 1.8 onward |
| **Bases** core plugin | Enable in *Settings → Core Plugins → Bases* |
| **Templates** core plugin (or Templater) | To apply the template on new note creation |

---

## 🗂️ Understanding the Key Expression: `this.file.folder`

Inside any embedded base, Obsidian exposes a special `this` object that refers to the **note the base is embedded in**.

| Expression | Returns |
|---|---|
| `this.file.name` | File name of the host note (no extension) |
| `this.file.path` | Full vault-relative path, e.g. `Projects/Alpha/index.md` |
| `this.file.folder` | Parent folder path, e.g. `Projects/Alpha` |

So `file.folder = this.file.folder` means *"only show files whose folder matches the folder this note is in"*.

---

## 🛠️ Step-by-Step Tutorial

### Step 1 — Enable Required Plugins

1. Open *Settings* (`Ctrl/Cmd + ,`)
2. Go to **Core Plugins**
3. Toggle **ON**: `Bases` and `Templates`
4. In **Templates** settings, set your **Template folder location** (e.g. `_templates`)

---

### Step 2 — Create the Template Note

Create a new note inside your templates folder, e.g. `_templates/folder-index.md`.

Paste the following content:

````markdown
---
title: "{{title}}"
created: {{date}}T{{time}}
modified: {{date}}T{{time}}
tags:
  - index
---

# 📁 {{title}}

> Index of all notes in this folder.

```base
filters:
  - field: file.folder
    operator: "="
    value: "{{this.file.folder}}"
sort:
  - field: file.mtime
    order: desc
fields:
  - field: file.name
    alias: Note
  - field: file.mtime
    alias: Modified
  - field: tags
    alias: Tags
```
````

> **Key line:** `value: "{{this.file.folder}}"` — when the base renders, Obsidian evaluates `this.file.folder` at runtime against the *host* note, not the template. This makes it dynamic.

---

### Step 3 — Apply the Template to a New Note

#### Using the built-in Templates plugin

1. Create a new note in any folder (e.g. `Projects/Alpha/index.md`)
2. Open the Command Palette (`Ctrl/Cmd + P`)
3. Run **Templates: Insert template**
4. Select `folder-index`

The base will immediately render, showing all files where `file.folder = Projects/Alpha`.

#### Using Templater (recommended for automation)

If you use the **Templater** community plugin you can trigger the template automatically when a note named `index` is created:

```js
// In Templater settings → Folder Templates
// Folder: (any)   Template: folder-index
```

Templater also gives you richer date/time tokens (`<% tp.date.now("YYYY-MM-DDTHH:mm:ss") %>`), but the base filter expression itself is identical.

---

### Step 4 — Customise the Base

Open the base in **Edit** mode (click the pencil icon on the embedded base) to tweak columns, sorting, or add more filters.

#### 🔍 Show only specific file types in the folder

```yaml
filters:
  - field: file.folder
    operator: "="
    value: "{{this.file.folder}}"
  - field: file.extension
    operator: "="
    value: "md"
```

#### 🏷️ Show only notes with a specific tag

```yaml
filters:
  - field: file.folder
    operator: "="
    value: "{{this.file.folder}}"
  - field: tags
    operator: contains
    value: "project"
```

#### 📅 Sort by creation date

```yaml
sort:
  - field: file.ctime
    order: asc
```

---

## 🔄 How the Dynamic Filtering Works at Runtime

```
Template saved with literal text: {{this.file.folder}}
           │
           ▼ (template inserted into note)
Note at: Projects/Alpha/index.md
           │
           ▼ (Bases evaluates expressions on render)
this.file.folder  →  "Projects/Alpha"
           │
           ▼ (filter applied)
Shows only files where file.folder = "Projects/Alpha"
```

The expression is **not** substituted at insert time by the Templates plugin — it remains as the literal text `{{this.file.folder}}` in the note source. Obsidian Bases evaluates `this.*` expressions live each time the base renders, which is what makes it dynamic.

---

## 💡 Tips & Gotchas

| Tip | Detail |
|---|---|
| **Nested folders are excluded** | The filter matches the *exact* folder string. Files in `Projects/Alpha/Sub` will not appear in an `Alpha` index. Use `operator: "startsWith"` if you want recursive. |
| **The index note itself appears** | The host note's folder matches, so it will list itself. Add a second filter: `file.name != this.file.name` to exclude it. |
| **Templater vs Templates** | The built-in Templates plugin substitutes `{{date}}` etc. at insert time. `{{this.file.folder}}` is a Bases expression, not a template token — leave it as-is and Bases handles it. |
| **Base won't render in Reading mode** | Make sure Bases core plugin is enabled; embedded bases render in both Reading and Live Preview modes. |

---

## ✅ Complete Template (Copy-Paste Ready)

````markdown
---
title: "{{title}}"
created: {{date}}T{{time}}
modified: {{date}}T{{time}}
tags:
  - index
---

# 📁 {{title}}

```base
filters:
  - field: file.folder
    operator: "="
    value: "{{this.file.folder}}"
  - field: file.name
    operator: "!="
    value: "{{this.file.name}}"
sort:
  - field: file.mtime
    order: desc
fields:
  - field: file.name
    alias: Note
  - field: file.mtime
    alias: Modified
  - field: tags
    alias: Tags
```
````
