---
uid: bcf99ea6-764b-461f-ad57-15f1b2cfc4b4
title: 🤖❓ Summary of json tools `jid`, `jq`, `fx`
created: '2026-05-28T00:13:12+00:13'
modified: '2026-05-28T00:15:06+00:15'
---

# 🤖❓ Summary of json tools `jid`, `jq`, `fx`

| **Tool** | **Purpose**           |
| -------- | --------------------- |
| jid      | discover structure    |
| jq       | extract/filter        |
| fx       | interactively inspect |

Please give me a summary of each one of the above tools, what they do, what I would use them for, and get me GitHub links to each one of their repositories.

# 🤖💡Robot Reply# 🤖❓ Summary of JSON Tools: `jid`, `jq`, `fx`

---

## `jid` — JSON Incremental Digger
🔗 **GitHub:** <https://github.com/simeji/jid>

### What it is
An **interactive, incremental JSON explorer** that lets you drill into JSON structure using tab completion and fuzzy filtering in real time.

### What it does
- Accepts JSON via stdin or file
- Provides an interactive TUI (terminal UI) where you type a query and see results update live
- Supports **tab completion** to explore keys
- Outputs a valid `jq` query when you're done — so you can copy it directly into a script

### What you'd use it for
- **You don't know the structure** of a JSON blob and want to explore it
- Quickly finding the right `jq` path without trial and error
- Treat it as a **discovery tool** — use it first, then hand the query off to `jq`

### Example

```bash
curl -s https://api.example.com/data | jid
# Interactively drill down, then copy the resulting jq query
```

---

## `jq` — JSON Query/Filter
🔗 **GitHub:** <https://github.com/jqlang/jq>

### What it is
The **industry-standard CLI tool** for parsing, filtering, transforming, and extracting data from JSON. Think of it as `sed`/`awk` but for JSON.

### What it does
- Filter and extract specific fields
- Transform and reshape JSON structures
- Perform arithmetic, string manipulation, conditionals
- Convert JSON to other formats (CSV, plain text, etc.)
- Scriptable and composable — works perfectly in shell pipelines

### What you'd use it for
- **Extracting a specific field** from an API response
- **Transforming** JSON from one shape to another
- Scripting and automation pipelines
- Any situation where you know (or have discovered via `jid`) the structure

### Example

```bash
# Extract all names from an array of objects
curl -s https://api.example.com/users | jq '[.[] | .name]'

# Get a nested value
cat data.json | jq '.users[0].address.city'

# Filter where condition is true
cat data.json | jq '.users[] | select(.age > 30)'
```

---

## `fx` — JSON Viewer / Interactive Processor
🔗 **GitHub:** <https://github.com/antonmedv/fx>

### What it is
A **dual-mode JSON tool** — it works as both an interactive TUI browser AND a command-line processor using JavaScript expressions.

### What it does
- **Interactive mode:** A scrollable, collapsible tree view of JSON in your terminal
- **Processing mode:** Apply JavaScript functions to transform/filter JSON inline
- Supports mouse interaction in the TUI
- Much more approachable than `jq` syntax for those familiar with JavaScript

### What you'd use it for
- **Visually inspecting** large or deeply nested JSON in a readable, collapsible format
- Quick transformations using familiar **JavaScript syntax** instead of `jq`'s DSL
- Situations where `jq` feels too cryptic and you want JS-style access
- Replacing `cat file.json | python -m json.tool` with something far more powerful

### Example

```bash
# Interactive browsing
cat data.json | fx

# JavaScript-style processing (no jq DSL needed)
cat data.json | fx '.users.filter(u => u.age > 30)'

# Access nested field
cat data.json | fx '.users[0].name'
```

---

## 🧭 When to Use Which

| **Scenario**                              | **Use**  |
| ----------------------------------------- | -------- |
| I don't know the JSON structure           | `jid`    |
| I need to script/automate JSON extraction | `jq`     |
| I want to visually browse JSON            | `fx`     |
| I know JS but not `jq` syntax             | `fx`     |
| I need powerful, composable pipelines     | `jq`     |
| I want to find the right `jq` query       | `jid` → `jq` |

### Typical Workflow

```bash
# 1. Discover structure
curl -s https://api.example.com/data | jid

# 2. Use the output query in jq for scripting
curl -s https://api.example.com/data | jq '.results[].id'

# 3. Or browse visually with fx when needed
curl -s https://api.example.com/data | fx
```
