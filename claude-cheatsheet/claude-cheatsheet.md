---
uid: dbdb67a4-4bbf-40df-97ce-6a307301986d
---
# Claude CLI Cheatsheet

Quick reference guide for using Claude Code via the command line.

## Installation & Setup

```bash
# Install Claude Code CLI (macOS with Homebrew)
brew install anthropic/tools/claude-code

# Update to latest version
brew upgrade claude-code

# Verify installation
claude-code --version

# Initial setup with oh-my-claudecode
/oh-my-claudecode:omc-setup

# Configure MCP servers
/oh-my-claudecode:mcp-setup
```

---

## Core Commands

### Starting Sessions

```bash
# Start Claude Code in current directory
claude-code

# Start with specific directory
claude-code /path/to/project

# Start in fast mode (faster output, same model)
claude-code --fast

# Start with verbose output
claude-code --verbose

# Start with specific context
claude-code --context "Working on auth module"
```

### Getting Help

```bash
# General help
/help

# oh-my-claudecode help
/oh-my-claudecode:omc-help

# List available skills
/oh-my-claudecode:skill list

# Help with specific skill
/oh-my-claudecode:skill search <keyword>
```

---

## Execution Modes (OMC)

### Autopilot (Recommended Default)

**Fully autonomous execution from idea to working code.**

```bash
# Trigger with natural language
"autopilot: build a REST API for managing tasks"
"I want a React dashboard with real-time updates"
"build me a Python CLI tool that processes CSV files"

# Autopilot automatically:
# - Plans requirements
# - Spawns specialized agents
# - Executes tasks in parallel
# - Tests and verifies
# - Self-corrects until complete
```

### Ralph Mode (Persistence Loop)

**Keeps working until completion, with architectural verification.**

```bash
# Activate ralph
"ralph: refactor authentication system"
/oh-my-claudecode:ralph "Implement CQRS pattern"

# Ralph automatically:
# - Continues work until done
# - Verifies with architect
# - Self-corrects errors
# - Requires explicit /cancel to stop
```

### Ultrawork (Parallel Execution)

**Maximum parallelism for fast execution on independent tasks.**

```bash
# Activate ultrawork
/oh-my-claudecode:ultrawork
"ulw fix all TypeScript errors"

# Or with short keyword
"ulw build the payment module"
```

### Ultrapilot (Parallel Autopilot)

**Combines autopilot with ultrawork's parallelism (3-5x faster).**

```bash
/oh-my-claudecode:ultrapilot "build a full-stack notes app"
```

### Ecomode (Token Efficient)

**Optimized model routing to save tokens while maintaining quality.**

```bash
/oh-my-claudecode:ecomode "eco implement feature X"
"eco fix all errors"  # Explicit keyword
```

### Swarm (Coordinated Agents)

**Multiple agents working on shared task list.**

```bash
/oh-my-claudecode:swarm
# Spawns 3-5 coordinated agents on task list
```

### Pipeline (Sequential Chaining)

**Chain agents in sequence with data passing between them.**

```bash
/oh-my-claudecode:pipeline "research -> design -> implement -> test"
```

### Ultraqa (Quality Assurance Loop)

**Test, verify, fix, repeat until all tests pass.**

```bash
/oh-my-claudecode:ultraqa "improve test coverage to 90%"
```

### Plan / Planning Interview

**Strategic planning before execution.**

```bash
/oh-my-claudecode:plan "implement new authentication system"
/oh-my-claudecode:ralplan  # Plan with consensus iteration
```

---

## Specialized Skills

### Code Review & Analysis

```bash
# Comprehensive code review
/oh-my-claudecode:code-review

# Security vulnerability scan
/oh-my-claudecode:security-review

# Deep analysis and investigation
/oh-my-claudecode:analyze "why is this API slow?"

# Build error fixing
/oh-my-claudecode:build-fix
```

### Testing & Development

```bash
# Test-Driven Development (write tests first)
/oh-my-claudecode:tdd "add user authentication"

# Run tests repeatedly until passing
/oh-my-claudecode:ultraqa
```

### Search & Exploration

```bash
# Deep codebase search
/oh-my-claudecode:deepsearch "where is the auth middleware?"

# Codebase initialization with documentation
/oh-my-claudecode:deepinit

# Explore codebase structure
# (agents automatically use Explore when needed)
```

### Documentation & Notes

```bash
# Add to working memory (session only)
/oh-my-claudecode:note "API uses JWT tokens in Authorization header"

# Add to priority context (always loaded)
/oh-my-claudecode:note --priority "Project uses pnpm, not npm"

# Add permanent notes (never pruned)
/oh-my-claudecode:note --manual "Production DB: postgres.prod.internal"

# View all notes
/oh-my-claudecode:note --show

# Prune old entries (>7 days)
/oh-my-claudecode:note --prune

# Extract learning from conversation
/oh-my-claudecode:learner "extract test mocking patterns"
```

### Git & Commits

```bash
# Interactive commit (uses git-master skill silently)
# Simply use: git add <files> && git commit -m "message"
# Claude will optimize commit message quality

# View status and staged changes
git status
git diff --staged
```

### Configuration & Setup

```bash
# Configure execution defaults
/oh-my-claudecode:omc-setup

# Setup MCP servers (context7, Gmail, Google Calendar, etc.)
/oh-my-claudecode:mcp-setup

# Configure HUD status display
/oh-my-claudecode:hud setup
/oh-my-claudecode:hud "layout: compact"

# Manage local skills
/oh-my-claudecode:skill list
/oh-my-claudecode:skill add <skill-name>
/oh-my-claudecode:skill search <keyword>

# Diagnostic tools
/oh-my-claudecode:doctor         # Fix installation issues
/oh-my-claudecode:trace timeline # Show agent flow trace
/oh-my-claudecode:trace summary  # Show statistics
```

### Other Utilities

```bash
# Stop active execution mode
/oh-my-claudecode:cancel

# Force stop all background work
/oh-my-claudecode:cancel --force

# Configure notifications
/oh-my-claudecode:configure-notifications "discord"

# View oh-my-claudecode usage patterns
/oh-my-claudecode:learn-about-omc
```

---

## Task Management

### Creating Tasks

```bash
# Claude creates task lists automatically for complex work
# You can also manually create tasks:
```

In conversation, Claude will:
- Create task lists for multi-step work
- Show task IDs and status
- Update automatically as work progresses

### Common Patterns

```bash
# For multi-file changes
"update all TypeScript files to use strict mode"
# Creates tasks automatically

# For complex features
"add OAuth2 integration"
# Claude plans tasks, creates list, tracks progress

# Check progress
# Claude displays task status in responses
```

---

## File Operations

### Reading Files

```bash
# Claude uses Read tool automatically
"what's in package.json?"
"show me the authentication middleware"

# View with line numbers
"read src/auth.ts with line numbers"
```

### Creating/Editing Files

```bash
# Tell Claude directly
"create a new file for database migrations"
"update the error handler to log stack traces"
"add a new POST endpoint at /api/users"

# Claude uses Edit (minimal diff) or Write (full rewrite)
```

### Searching Codebase

```bash
# Natural language
"find all database queries"
"where is the JWT validation?"
"show me all async operations"

# Claude uses Grep automatically for best results
```

---

## Workflow Examples

### Building a New Feature (Autopilot)

```bash
# Start with high-level request
"autopilot: add a real-time notification system with WebSockets"

# Autopilot handles:
# 1. Planning & requirements
# 2. Parallel implementation
# 3. Testing
# 4. Verification
# → No manual intervention needed
```

### Refactoring Code (Ralph Mode)

```bash
# Start refactoring, ensure completion
"ralph: migrate from Redux to Zustand"

# Ralph ensures:
# - All changes implemented
# - Tests pass
# - Architect verifies
# - Requires /cancel when truly done
```

### Quick Fixes (Direct Commands)

```bash
# For small changes, just ask
"fix the TypeScript error in utils.ts"
"add error handling to the login route"
"update the README with new API docs"

# Claude makes the change directly
```

### Analysis & Planning

```bash
# For complex decisions
"plan: how should we implement real-time sync?"

# Claude:
# 1. Explores codebase
# 2. Asks clarifying questions
# 3. Presents options
# 4. Gets your approval
# 5. Then implements
```

---

## Magic Keywords (Optional Shortcuts)

| Keyword | Effect | Example |
|---------|--------|---------|
| `autopilot` | Full autonomous execution | "autopilot: build X" |
| `ralph` | Persistence mode | "ralph: refactor X" |
| `ulw` | Maximum parallelism | "ulw fix all errors" |
| `eco` | Token efficiency | "eco: implement X" |
| `plan` | Planning interview | "plan the new API" |
| `ralplan` | Planning with consensus | "ralplan this feature" |
| `fast` | Faster output (same model) | `/fast` toggle command |
| `/help` | Get help | `/help` |
| `/cancel` | Stop execution mode | `/cancel` |

---

## Context Management

### Persistent Memory (Across Sessions)

Memory files in `~/.claude/projects/` survive session compaction:

```bash
# User profile (role, preferences, knowledge)
~/.claude/projects/{project}/user.md

# Feedback (how to approach work)
~/.claude/projects/{project}/feedback_*.md

# Project context (goals, deadlines, architecture)
~/.claude/projects/{project}/project_*.md

# References (external systems)
~/.claude/projects/{project}/reference_*.md
```

### Session Memory (This Session Only)

```bash
# Notepad with three tiers
.omc/notepad.md

# Priority Context (always loaded)
# Working Memory (7-day auto-prune)
# MANUAL section (never pruned)
```

### Project Configuration

```bash
# Local Claude config
.claude/settings.json

# oh-my-claudecode state
.omc/state/*.json
.omc/config.json

# Local skills
.claude/agents/
```

---

## Tips & Tricks

### 1. Use Natural Language First

```bash
# Good
"add error handling to the payment endpoint"

# Also fine - Claude understands intent
"make the API more robust"
```

### 2. Provide Context

```bash
# Better
"add validation to the signup form - we need to check email format and password strength"

# Also works
"validate the signup form"
```

### 3. Combine Mode Keywords for Power

```bash
# Ultra-fast autonomous execution
"ulw autopilot: build admin dashboard"

# Token-efficient refactoring with persistence
"eco ralph: migrate to TypeScript"
```

### 4. Break Complex Tasks into Phases

```bash
# Instead of one giant request
"phase 1: plan the API structure
 phase 2: implement endpoints
 phase 3: add authentication
 phase 4: write tests"

# Or let autopilot handle it:
"autopilot: build a complete REST API with auth and tests"
```

### 5. Use `/cancel` When Done

```bash
# Ralph mode keeps going until you explicitly stop
"work done, all tests pass"
/oh-my-claudecode:cancel  # Actually stops the loop
```

### 6. Check Verification Evidence

Before accepting completion, Claude should show:
- Test results (✓ passing)
- Build output (✓ no errors)
- Code review findings
- Verification logs

### 7. Save Important Context

```bash
# Save architecture decisions
/oh-my-claudecode:note --priority "Using event-sourced CQRS for order processing"

# Save team info
/oh-my-claudecode:note --manual "Production DB: contact @devops on Slack"
```

### 8. Use Project-Specific CLAUDE.md

Create `.claude/CLAUDE.md` in project root for:
- Directory-specific rules
- Team conventions
- Required file formats
- Deployment procedures
- Custom skill definitions

---

## Common Scenarios

### Debugging a Production Issue

```bash
"debug: users can't log in - auth service returning 500 errors"

# Claude will:
# 1. Ask clarifying questions
# 2. Examine error handling
# 3. Identify root cause
# 4. Implement fix
# 5. Add tests
```

### Code Review Before Merge

```bash
/oh-my-claudecode:code-review

# Returns:
# - Security issues
# - Performance problems
# - Style violations
# - Maintainability concerns
```

### Security Audit

```bash
/oh-my-claudecode:security-review

# Checks for:
# - OWASP Top 10 vulnerabilities
# - Credential exposure
# - Unsafe patterns
# - Dependency vulnerabilities
```

### Performance Optimization

```bash
"identify performance bottlenecks and optimize:
 - database queries taking >100ms
 - API endpoints with high latency
 - memory leaks or inefficient algorithms"

# Claude profiles, analyzes, and fixes
```

---

## Settings & Configuration

### ~/.claude/settings.json

```json
{
  "defaultExecutionMode": "ultrawork",
  "codeReviewStrictness": "high",
  "autoCommit": false,
  "memoryCompaction": "7days"
}
```

### .claude/settings.json (Project-level)

```json
{
  "enforceStrictMode": true,
  "preferredLanguage": "typescript",
  "testFramework": "vitest",
  "lintConfig": "eslint"
}
```

---

## Troubleshooting

### Mode Not Activating

```bash
# Check active modes
/oh-my-claudecode:state status

# Force reset state
/oh-my-claudecode:cancel --force
```

### Sessions Ending Unexpectedly

```bash
# Likely in a mode that requires /cancel
/oh-my-claudecode:cancel

# Then restart
claude-code
```

### MCP Tools Not Working

```bash
# Setup MCP servers
/oh-my-claudecode:mcp-setup

# Or fix installation
/oh-my-claudecode:doctor
```

### Slow Performance

```bash
# Switch to ecomode to reduce context
"eco continue with the current task"

# Or use fast mode
claude-code --fast
```

---

## Reference

### Tool Availability by Mode

| Tool | Autopilot | Ralph | Ultrawork | Plan | Direct |
|------|-----------|-------|-----------|------|--------|
| Execute code | ✓ | ✓ | ✓ | ✗ | ✓ |
| Read files | ✓ | ✓ | ✓ | ✓ | ✓ |
| Create tasks | ✓ | ✓ | ✓ | ✓ | ✓ |
| Git operations | ✓ | ✓ | ✓ | ✗ | ✓ |
| Run tests | ✓ | ✓ | ✓ | ✗ | ✓ |

### Agent Tiers

```
TIER 1 (Haiku - Quick)
- Simple lookups, definitions, documentation

TIER 2 (Sonnet - Standard)
- Feature implementation, bug fixes, refactoring

TIER 3 (Opus - Complex)
- Architecture decisions, race conditions, full system design
```

Claude automatically routes based on task complexity.

### Keyboard Shortcuts

```
Ctrl+C        Cancel current operation / stop
Ctrl+L        Clear screen
Tab           Autocomplete
Enter         Submit prompt
Ctrl+R        Search command history (shell)
```

To customize: `claude-code --keybindings` or edit `~/.claude/keybindings.json`

---

## Resources

- **oh-my-claudecode**: Full orchestration framework
- **Context7 MCP**: Latest library docs (React, Django, etc.)
- **GitHub Issues**: Report bugs at https://github.com/anthropics/claude-code/issues
- **Feedback**: Same GitHub issues URL

---

**Last Updated:** 2026-04-05  
**Version:** Claude Code 4.5+
