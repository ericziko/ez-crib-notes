---
title: Research Sources for Lazygit Custom Diff Views
created: 2026-03-28T00:00:00Z
modified: 2026-03-28T00:00:00Z
---

# Research Sources: Adding Custom Diff Views to Lazygit

This document lists all official and authoritative sources consulted when creating the comprehensive tutorial on custom diff views in Lazygit.

## Official Lazygit Documentation

1. **Custom Pagers Official Documentation**
   - URL: https://github.com/jesseduffield/lazygit/blob/master/docs/Custom_Pagers.md
   - Content: Complete reference for configuring custom pagers, examples with Delta, ydiff, Diff-So-Fancy, and Difftastic
   - Key info: Multiple pagers array syntax, colorArg settings, external diff command usage, Windows workarounds

2. **Configuration Reference (Config.md)**
   - URL: https://github.com/jesseduffield/lazygit/blob/master/docs/Config.md
   - Content: Comprehensive config.yml reference including all git paging options
   - Key info: diffContextSize, ignoreWhitespaceInDiffView, renameSimilarityThreshold, pagers array structure

## External Diff Tool Documentation

3. **Difftastic Manual**
   - URL: https://difftastic.wilfred.me.uk/git.html
   - Content: How to use Difftastic with git and external diff configurations
   - Key info: Semantic/structural diff capabilities, command-line options, integration with git

4. **Delta GitHub Repository**
   - URL: https://github.com/dandavison/delta
   - Content: Full documentation of Delta diff tool features
   - Key info: Syntax highlighting, line numbers, hyperlinks, side-by-side viewing, theme options

5. **Diff-So-Fancy GitHub Repository**
   - URL: https://github.com/so-fancy/diff-so-fancy
   - Content: Documentation for Diff-So-Fancy pager tool
   - Key info: Installation methods, features, configuration

6. **ydiff GitHub Repository**
   - URL: https://github.com/ymattw/ydiff
   - Content: ydiff tool documentation and usage
   - Key info: Side-by-side diff viewing, command-line options, column width handling

## Community Resources and Examples

7. **Better diffs in Lazygit with delta - Lorenzo Bettini**
   - URL: https://www.lorenzobettini.it/2025/06/better-diffs-in-lazygit-with-delta/
   - Content: Practical examples of Delta configuration with Lazygit
   - Key info: Real-world setup examples, integration tips

8. **Lazygit GitHub Issues and Discussions**
   - Issue #3941: "How to enable Split Diff view?"
   - Issue #2659: "Improved diff UX"
   - Issue #190: "Integrating diff-so-fancy as a diff viewer?"
   - Issue #2337: "[Windows] git diff is not shown using delta as pager in lazygit"
   - Content: Community discussions, implementation details, troubleshooting
   - Key info: Real user problems, solutions, configuration examples

9. **Get lazy with lazygit - DEV Community**
   - URL: https://dev.to/tahsinature/get-lazy-with-lazygit-4h37
   - Content: Tutorial on Lazygit usage and configuration
   - Key info: Practical examples, user workflows

## Documentation Aggregators

10. **Sourcegraph - Lazygit Config.md**
    - URL: https://sourcegraph.com/r/github.com/jesseduffield/lazygit/-/blob/docs/Config.md
    - Content: Indexed version of official Config.md
    - Key info: Searchable reference of all configuration options

11. **Fossies - Lazygit Custom_Pagers.md**
    - URL: https://fossies.org/linux/lazygit/docs/Custom_Pagers.md
    - Content: Archived version of official Custom_Pagers documentation
    - Key info: Historical reference, complete examples

---

## Key Findings Summary

### Core Concepts
- Lazygit supports multiple custom pagers via a `pagers` array in the git configuration
- Pagers are cycled through using the `|` key in the diff view
- Two approaches: pagers (post-processing) and externalDiffCommand (direct access to files)

### Popular Tools
- **Delta**: Recommended for most users; offers syntax highlighting, line numbers, hyperlinks
- **Diff-So-Fancy**: Elegant formatting with file change indicators
- **ydiff**: Side-by-side viewing of diffs
- **Difftastic**: Semantic/structural diff understanding
- **Raw (cat)**: Fallback for unprocessed diffs

### Configuration Details
- Config file location: `~/.config/lazygit/config.yml` (Linux/macOS) or `%APPDATA%\lazygit\config.yml` (Windows)
- YAML format required; indentation critical
- colorArg controls `--color=always` flag (typically `always` for pagers, `never` for tools managing own colors)
- Template variable available: `{{columnWidth}}` for dynamic width adjustment

### Platform Notes
- Linux/macOS: Full native support
- Windows: Requires PowerShell workaround script (limitations with rename detection)

---

## How to Use These Sources

When implementing custom diff views in Lazygit:

1. **Start** with the official Custom_Pagers.md documentation
2. **Reference** the Config.md for all available options
3. **Install** tools from their respective GitHub repositories
4. **Test** configurations using examples from community resources
5. **Troubleshoot** using GitHub issues for specific problems

All sources were consulted on 2026-03-28 and represent the most current information available for Lazygit custom diff view configuration.
