---
title: Adding Custom Diff Views to Lazygit
created: 2026-03-28T00:00:00Z
modified: 2026-03-28T00:00:00Z
tags:
  - lazygit
  - git
  - diff
  - configuration
  - tutorial
---

# Adding Custom Diff Views to Lazygit: A Comprehensive Guide

## Table of Contents

1. [What Are Diff Views in Lazygit?](#what-are-diff-views-in-lazygit)
2. [Configuration Fundamentals](#configuration-fundamentals)
3. [Setting Up Multiple Diff Views](#setting-up-multiple-diff-views)
4. [Popular Diff View Examples](#popular-diff-view-examples)
5. [Using Custom Diff Views in the UI](#using-custom-diff-views-in-the-ui)
6. [Advanced Configuration](#advanced-configuration)
7. [Tips and Best Practices](#tips-and-best-practices)
8. [Troubleshooting](#troubleshooting)

---

## What Are Diff Views in Lazygit?

In Lazygit, a **diff view** is a customizable way to display changes between commits, branches, or the working directory. By default, Lazygit uses standard `git diff` output, but you can enhance this experience by configuring custom "pagers"—tools that process and beautifully render your diffs.

### Why Use Custom Diff Views?

Custom diff views provide several benefits:

- **Enhanced Readability**: Tools like [Delta](https://github.com/dandavison/delta) add syntax highlighting and file decorations
- **Alternative Formats**: View diffs side-by-side, as word diffs, or with semantic understanding (e.g., [Difftastic](https://difftastic.wilfred.me.uk/))
- **Line Numbers**: Quickly navigate to specific lines
- **Hyperlinks**: Click line numbers to jump directly to your editor
- **Multiple Views**: Cycle between different pagers for different situations

### How It Works

When you view a diff in Lazygit, the output of `git diff` is piped through your configured pager. By default, this uses your system's standard pager (like `less`). With custom pagers, you can intercept this output and process it through powerful diff tools.

---

## Configuration Fundamentals

### Locating the Config File

Lazygit's configuration is stored in `config.yml`. To find it:

- **Linux/macOS**: `~/.config/lazygit/config.yml`
- **Windows**: `%APPDATA%\lazygit\config.yml`

You can also open the config directly in Lazygit by pressing `e` (edit config) from the status panel.

### YAML Syntax Overview

Lazygit uses YAML for configuration. Here's a quick primer on the relevant syntax:

```yaml
git:
  pagers:
    - pager: "command --flag value"
      colorArg: always
    - externalDiffCommand: "other-command --option"
```

**Key points:**
- Indentation matters in YAML (use spaces, not tabs)
- String values with special characters should be quoted
- The `pagers` array can contain multiple pager definitions
- Each pager is a separate list item (denoted by `-`)

---

## Setting Up Multiple Diff Views

### Basic Configuration Structure

The `git` section of your config contains a `pagers` array where each entry defines one diff view:

```yaml
git:
  pagers:
    - pager: "first-pager-command"
    - pager: "second-pager-command"
      colorArg: never
    - externalDiffCommand: "third-tool-command"
```

### Two Types of Diff Tools

Lazygit supports two approaches to custom diff tools:

#### 1. **Pagers** (Post-Processing Approach)

A pager receives the output of `git diff` and processes it. This works for tools like Delta and Diff-So-Fancy that can work as filters.

```yaml
git:
  pagers:
    - pager: "delta --dark --paging=never"
```

#### 2. **External Diff Commands** (Direct Approach)

Some tools need direct access to the files being compared (not just the diff output). These use `externalDiffCommand` instead.

```yaml
git:
  pagers:
    - externalDiffCommand: "difft --color=always"
```

### Understanding `colorArg`

The `colorArg` parameter controls whether Lazygit adds the `--color=always` flag to the `git diff` command:

- `always` (default): Adds `--color=always` to enable colors in diffs
- `never`: Disables color flags (some pagers manage color themselves)

**When to use each:**
- **`always`**: For pagers like Delta that expect colored input
- **`never`**: For pagers like ydiff that handle their own colors, or for tools that break with color codes

---

## Popular Diff View Examples

### Example 1: Delta with Dark Theme

[Delta](https://github.com/dandavison/delta) is a popular diff tool that adds syntax highlighting, file decorations, and enhanced navigation.

**Installation:**
```bash
# macOS
brew install delta

# Linux (using cargo)
cargo install git-delta

# On Windows, check the delta documentation for installation
```

**Basic Configuration:**
```yaml
git:
  pagers:
    - pager: "delta --dark --paging=never"
      colorArg: always
```

**With Line Numbers and Hyperlinks:**
```yaml
git:
  pagers:
    - pager: "delta --dark --paging=never --line-numbers --hyperlinks --hyperlinks-file-link-format='lazygit-edit://{path}:{line}'"
      colorArg: always
```

The hyperlinks option lets you click on line numbers in the diff to jump to that location in your editor.

**With Side-by-Side View:**
```yaml
git:
  pagers:
    - pager: "delta --dark --paging=never --side-by-side --line-numbers"
      colorArg: always
```

### Example 2: Diff-So-Fancy

[Diff-So-Fancy](https://github.com/so-fancy/diff-so-fancy) provides elegant diffs with file change indicators and attractive formatting.

**Installation:**
```bash
# macOS
brew install diff-so-fancy

# Linux (using npm)
npm install -g diff-so-fancy

# Or download from the GitHub repository
```

**Configuration:**
```yaml
git:
  pagers:
    - pager: "diff-so-fancy"
      colorArg: always
```

### Example 3: ydiff with Side-by-Side View

[ydiff](https://github.com/ymattw/ydiff) displays diffs in a side-by-side format, which is excellent for understanding complex changes.

**Installation:**
```bash
# macOS
brew install ydiff

# Linux (using pip)
pip install ydiff

# Or using npm
npm install -g ydiff
```

**Configuration:**
```yaml
gui:
  sidePanelWidth: 0.2  # Give more space for side-by-side view

git:
  pagers:
    - pager: "ydiff -p cat -s --wrap --width={{columnWidth}}"
      colorArg: never  # ydiff handles its own colors
```

The `{{columnWidth}}` placeholder is replaced automatically with the available column width in Lazygit.

### Example 4: Difftastic (Structural Diff)

[Difftastic](https://difftastic.wilfred.me.uk/) performs semantic-aware diffs, understanding the structure of your code (e.g., function definitions, blocks) rather than just lines.

**Installation:**
```bash
# macOS
brew install difftastic

# Linux (using cargo)
cargo install difftastic

# Or download pre-built binaries
```

**Configuration:**
Note: Difftastic requires `externalDiffCommand` because it needs access to the actual files, not just the diff output.

```yaml
git:
  pagers:
    - externalDiffCommand: "difft --color=always"
```

**With Custom Display Options:**
```yaml
git:
  pagers:
    - externalDiffCommand: "difft --color=always --display=inline"
```

### Example 5: Raw Unified Diff

Sometimes you want the raw, unprocessed `git diff` output. This is useful as a quick fallback:

```yaml
git:
  pagers:
    - pager: "cat"
      colorArg: always
```

This pipes the diff through `cat`, which simply outputs it without modification.

---

## Using Custom Diff Views in the UI

### Switching Between Diff Views

Once you've configured multiple pagers, you can cycle through them while viewing a diff:

**Press `|` (pipe character)** to switch to the next diff view in your `pagers` array.

This allows you to quickly toggle between your configured views without reopening the config file.

### Where Diffs Appear in Lazygit

Diffs are displayed when you:

- **Select a commit** in the log view (press `Space` to expand)
- **View changes in the working directory** (in the "Unstaged changes" section)
- **View staged changes** (in the "Staged changes" section)
- **Compare branches or commits** (using the diff mode)

### Practical Workflow Example

1. You're in Lazygit viewing a commit's changes
2. The default pager (first in your array) shows the diff with Delta syntax highlighting
3. You want to see the old line-by-line format—press `|`
4. The view switches to your second configured pager
5. You want to see actual file structure differences—press `|` again
6. Now Difftastic is rendering the diff with semantic understanding

---

## Advanced Configuration

### Complete Configuration Example

Here's a real-world configuration that includes multiple diff views:

```yaml
gui:
  sidePanelWidth: 0.25

git:
  pagers:
    # Primary view: Delta with modern syntax highlighting
    - pager: "delta --dark --paging=never --line-numbers --syntax-highlight=on"
      colorArg: always

    # Secondary view: Side-by-side comparison
    - pager: "ydiff -p cat -s --wrap --width={{columnWidth}}"
      colorArg: never

    # Tertiary view: Raw diff for debugging
    - pager: "cat"
      colorArg: always

    # Quaternary view: Semantic/structural diff
    - externalDiffCommand: "difft --color=always"

  # Other diff-related settings
  diffContextSize: 3  # Lines of context around each hunk
  ignoreWhitespaceInDiffView: false
```

### Template Variables

Lazygit provides placeholders that are replaced with values at runtime:

- `{{columnWidth}}`: The available column width in Lazygit (useful for tools like ydiff)

### Additional Git Config Options

While not strictly part of custom diff views, these related settings enhance your diff experience:

```yaml
git:
  # Show different number of context lines (default: 3)
  # Adjust with { and } keys in diff view
  diffContextSize: 3

  # Toggle whitespace handling with <c-w>
  ignoreWhitespaceInDiffView: false

  # Adjust rename detection sensitivity with ( and )
  renameSimilarityThreshold: 50  # percentage
```

### Using Git's External Diff Configuration

Instead of hardcoding the command in Lazygit, you can configure it in Git itself and tell Lazygit to use that:

**Step 1: Configure Git**
```bash
git config --global diff.external difft
```

**Step 2: Tell Lazygit to use it**
```yaml
git:
  pagers:
    - useExternalDiffGitConfig: true
```

This approach is useful if you want to use the same diff tool across all your tools (not just Lazygit).

---

## Tips and Best Practices

### 1. Order Your Pagers Strategically

Put your most-used diff view first, as it will be the default:

```yaml
git:
  pagers:
    # First: Your daily driver
    - pager: "delta --dark --paging=never"

    # Second: Alternative for specific situations
    - pager: "ydiff -p cat -s --wrap --width={{columnWidth}}"
```

### 2. Test Before Committing Configuration

After editing your config, test each diff view by:

1. Selecting a commit in the log
2. Pressing `|` to cycle through your configured pagers
3. Verifying each one renders correctly

### 3. Be Mindful of Performance

Some diff tools can be slow on large diffs. Consider:

- Using `--paging=never` for tools like Delta to avoid nested pagers
- Having a lightweight option (like `cat`) as a fallback for very large diffs

### 4. Use Comments in Config

YAML supports comments—use them to document your pagers:

```yaml
git:
  pagers:
    # Fast, syntax-highlighted diffs with line numbers
    - pager: "delta --dark --paging=never --line-numbers"
      colorArg: always

    # Side-by-side view for complex changes (needs more width)
    - pager: "ydiff -p cat -s --wrap --width={{columnWidth}}"
      colorArg: never

    # Raw output for debugging
    - pager: "cat"
      colorArg: always
```

### 5. Combine with Git Config

Set up your git config to align with Lazygit:

```bash
# Use your preferred diff tool globally
git config --global diff.tool delta

# Or set a custom pager for `git diff` command-line usage
git config --global core.pager "delta --dark"
```

### 6. Leverage Hyperlinks

If using Delta, enable hyperlinks to jump to files with one click:

```yaml
git:
  pagers:
    - pager: "delta --dark --paging=never --line-numbers --hyperlinks --hyperlinks-file-link-format='lazygit-edit://{path}:{line}'"
      colorArg: always
```

### 7. Consider Color Settings for Different Contexts

You might want different color settings for terminal theme vs. IDE:

```yaml
git:
  pagers:
    - pager: "delta --light --paging=never"
      colorArg: always  # For light terminals

    - pager: "delta --dark --paging=never"
      colorArg: always  # For dark terminals
```

---

## Troubleshooting

### Problem: Diff View Doesn't Render Properly

**Symptom:** Pager command runs but output looks broken or incomplete.

**Solutions:**
1. Check `colorArg` setting—try both `always` and `never`
2. Verify the pager command works from command line:
   ```bash
   git diff | your-pager-command
   ```
3. Ensure the tool is installed and in your PATH:
   ```bash
   which delta  # or whichever tool you're using
   ```

### Problem: Pressing `|` Doesn't Switch Views

**Symptom:** Cycling key doesn't respond or always shows the same view.

**Solutions:**
1. Verify you have multiple pagers configured (at least 2)
2. Make sure you're in a diff view (viewing a commit or file diff)
3. Check that your config.yml syntax is valid YAML:
   ```bash
   cat ~/.config/lazygit/config.yml  # Check for errors
   ```

### Problem: External Diff Command Doesn't Work

**Symptom:** Using `externalDiffCommand` shows an error or no output.

**Solutions:**
1. Test the command directly with actual files:
   ```bash
   difft file1.txt file2.txt
   ```
2. For `externalDiffCommand`, the tool receives file paths, not stdin—it must support this
3. Check permissions: the command might not be executable
4. Try with absolute path instead of relying on PATH:
   ```yaml
   - externalDiffCommand: "/usr/local/bin/difft --color=always"
   ```

### Problem: YAML Parse Error

**Symptom:** Lazygit won't start or shows "invalid YAML" error.

**Solutions:**
1. Check indentation—YAML requires consistent spaces (not tabs)
2. Verify quotes around strings with special characters:
   ```yaml
   # Wrong
   - pager: delta --dark --paging=never --hyperlinks-file-link-format="lazygit-edit://{path}:{line}"

   # Correct (escape internal quotes or use single quotes)
   - pager: "delta --dark --paging=never --hyperlinks-file-link-format='lazygit-edit://{path}:{line}'"
   ```
3. Use an online YAML validator to check syntax
4. Start with the minimal config and add complexity gradually

### Problem: Special Characters Breaking Commands

**Symptom:** Commands with pipes, quotes, or special characters fail.

**Solutions:**
1. Always quote the entire command string:
   ```yaml
   - pager: "command | another-command"
   ```
2. For quotes within the command, use different quote types:
   ```yaml
   - pager: "delta --hyperlinks-file-link-format='lazygit-edit://{path}:{line}'"
   ```
3. Escape sequences might be needed in some cases:
   ```yaml
   - pager: "command with \"quoted string\""
   ```

### Problem: Custom Pagers Work in Terminal but Not in Lazygit

**Symptom:** Running `git diff | pager-tool` in terminal works fine, but not in Lazygit.

**Solutions:**
1. Ensure the tool is in Lazygit's PATH (may differ from terminal PATH)
2. Try specifying the absolute path to the tool
3. Check that environment variables are set correctly for the tool
4. Some tools require terminal capabilities—Lazygit might provide different terminal settings

### Problem: Windows Pager Not Working

**Symptom:** Native pagers don't work on Windows.

**Solution:** Use the PowerShell workaround. Create a `lazygit-pager.ps1` script:

```powershell
#!/usr/bin/env pwsh

$old = $args[1].Replace('\', '/')
$new = $args[4].Replace('\', '/')
$path = $args[0]
git diff --no-index --no-ext-diff $old $new `
  | ForEach-Object { $_.Replace($old, $path).Replace($new, $path) } `
  | delta --width=$env:LAZYGIT_COLUMNS
```

Then configure:
```yaml
git:
  pagers:
    - externalDiffCommand: "C:/path/to/lazygit-pager.ps1"
```

**Limitation:** Renames are shown as modifications in this approach.

---

## Summary

Custom diff views in Lazygit dramatically improve your ability to understand code changes. By configuring multiple pagers, you get:

- **Flexibility**: Switch between tools based on the situation
- **Power**: Use specialized tools like Difftastic for semantic diffs
- **Comfort**: Syntax highlighting, line numbers, and hyperlinks
- **Efficiency**: View changes in the format that works best for you

Start with Delta (recommended for most users), add a side-by-side view with ydiff, and keep a raw view as a fallback. Customize from there based on your workflow.

Happy diffing!

---

## Resources and Further Reading

- [Official Lazygit Custom Pagers Documentation](https://github.com/jesseduffield/lazygit/blob/master/docs/Custom_Pagers.md)
- [Lazygit Configuration Reference](https://github.com/jesseduffield/lazygit/blob/master/docs/Config.md)
- [Delta GitHub Repository](https://github.com/dandavison/delta)
- [Difftastic Documentation](https://difftastic.wilfred.me.uk/)
- [Diff-So-Fancy GitHub Repository](https://github.com/so-fancy/diff-so-fancy)
- [ydiff GitHub Repository](https://github.com/ymattw/ydiff)
