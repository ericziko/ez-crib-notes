---
title: 🤖❓ GitHub co-pilot prompts for PR code quality reviews
created: 2026-02-02T12:13:57
modified: 2026-02-02T12:23:41
uid: 6f874cdd-44b6-4cf0-bf56-7cc7ae8d44ef
---

# 🤖❓ GitHub co-pilot prompts for PR code quality reviews

## Summary 
Below are a bunch of AI suggestions for GitHub Copilot instructions

- [👤 Basic suggestion](#-basic-suggestion)
- [🤖❓Can you add specific instructions for reviewing pull requests to gitHub Copilot instructions?](#can-you-add-specific-instructions-for-reviewing-pull-requests-to-github-copilot-instructions)
	- [Where Copilot actually reads instructions](#where-copilot-actually-reads-instructions)
	- [What to put in it (PR-review specific)](#what-to-put-in-it-pr-review-specific)
	- [How this actually shows up in PRs](#how-this-actually-shows-up-in-prs)
	- [A subtle but powerful trick](#a-subtle-but-powerful-trick)
	- [Limitations (because reality exists)](#limitations-because-reality-exists)
	- [The deeper idea](#the-deeper-idea)
	- [🤖❓Legacy code containment | "This repo is cursed, be careful" mode](#legacy-code-containment--this-repo-is-cursed-be-careful-mode)

### 👤 Basic suggestion

```
1) What bugs do you see?
2) What security issues do you see?
3) What performance issues do you see?
4) What architectural issues do you see?
5) What code smells do you see?
6) What tests can we write that would cover the most critical testing gaps?
```

### 🤖❓Can you add specific instructions for reviewing pull requests to gitHub Copilot instructions?

Yes — and this is one of the *most* under-used powers of GitHub Copilot. You can absolutely give it **explicit pull-request review instructions**, and it will behave like a tireless, mildly judgmental senior engineer who never forgets a checklist.

Let's unpack how this works and how to do it *well*, not just loudly.

#### Where Copilot actually reads instructions

Copilot pulls "how should I behave?" guidance from a few places, but for **PR reviews** the sweet spot is a repository-level instruction file.

##### The magic file

Create this file in your repo:

```
.github/copilot-instructions.md
```

Anything in here becomes *ambient law* for Copilot: it applies when generating code, reviewing diffs, and commenting on pull requests.

This works with [**GitHub Copilot**](chatgpt://generic-entity?number=0) in PR review mode, not just autocomplete.

#### What to put in it (PR-review specific)

Copilot responds best to **clear norms + explicit constraints**. Think of this as training a new team member who is brilliant but literal.

Here's a solid, battle-tested example you can drop in and adapt:

```markdown
# Pull Request Review Instructions

When reviewing pull requests, follow these rules:

## Review Focus
- Prioritize correctness, clarity, and maintainability over cleverness.
- Look for hidden coupling, shared mutable state, and unintended side effects.
- Flag potential performance issues (N+1 queries, unnecessary allocations, blocking I/O).
- Identify security risks (input validation, secrets, auth boundaries, logging of sensitive data).
- Check error handling paths and failure modes, not just the happy path.

## Architecture & Design
- Ensure changes align with existing architectural patterns.
- Avoid introducing new abstractions unless they clearly reduce complexity.
- Call out violations of layering, CQRS boundaries, or domain encapsulation.
- Prefer explicit dependencies over service locators or static access.

## Tests
- Verify that new behavior is covered by appropriate tests.
- Flag brittle tests, over-mocking, or tests that assert implementation details.
- Ensure tests are isolated and do not rely on shared state or execution order.

## Style & Readability
- Prefer readable, boring code over dense or “clever” implementations.
- Recommend simplifications when logic can be expressed more clearly.
- Call out misleading names, long methods, or unclear intent.

## Comments
- Be concise and constructive.
- Explain *why* something is risky or unclear, not just *what* to change.
- Suggest concrete improvements when possible.
- If something is acceptable but debatable, label it as a “nit” or “suggestion”.

## What Not to Do
- Do not restate what the code obviously does.
- Do not request stylistic changes already enforced by linters or formatters.
- Do not approve changes that introduce technical debt without justification.
```

This turns Copilot from "rubber-stamp robot" into "helpful but slightly suspicious reviewer."

#### How this actually shows up in PRs

With these instructions in place, Copilot will:

- Leave **inline comments** that reference design and risk, not trivia
- Ask for tests when behavior changes
- Call out architectural drift
- Avoid bikeshedding formatting
- Use calmer language instead of alarmist nonsense

It won't replace a human reviewer — but it *will* catch the stuff humans miss at 4:57 PM.

#### A subtle but powerful trick

Copilot is very sensitive to **negative constraints**.

Adding sections like *"What Not to Do"* dramatically improves signal-to-noise. Without that, it tends to narrate the diff like it's reading bedtime stories to itself.

#### Limitations (because reality exists)

- Copilot won't enforce these rules — it only **advises**
- It can miss deep domain invariants unless you describe them
- It works best when PRs are small and scoped
- It mirrors your repo's existing quality — garbage in, politely phrased garbage out

Still, as a first-pass reviewer, it's shockingly effective.

#### The deeper idea

You're not configuring a tool — you're **encoding institutional memory**. Every time someone opens a PR, Copilot reenacts your best reviewer on their best day, minus caffeine dependency and mood swings.

If you want, the next step is tailoring this for:

- .NET + EF Core
- SQL-heavy repos
- Security-sensitive systems
- Legacy code containment
- "This repo is cursed, be careful" mode

Those get *very* interesting.

#### 🤖❓Legacy code containment | "This repo is cursed, be careful" mode 

Absolutely. For "legacy code containment" and "this repo is cursed" mode, you want Copilot to behave like a hazmat-suited archaeologist: **minimize blast radius, preserve behavior, prove equivalence, and leave warning signs for the next adventurer.**

Below are two drop-in sections you can paste into .github/copilot-instructions.md. They're designed to bias Copilot toward *safe, incremental PR review* and away from "refactor the universe" impulses.

##### Legacy code containment mode

```markdown
## Legacy Code Containment Mode (Review Rules)

Assume this codebase contains fragile behavior and undocumented dependencies.

### Primary Objective
- Preserve existing behavior unless the PR explicitly changes requirements.
- Prefer the smallest change that achieves the goal.

### Containment & Blast Radius
- Minimize the diff: avoid unrelated refactors, renames, formatting churn, and broad reorganizations.
- Avoid changing public APIs, shared utilities, base classes, or global configuration unless necessary.
- Avoid cross-cutting changes (shared helpers, common libraries, “cleanup” PRs) unless explicitly requested.

### Risk Hotspots (be extra suspicious)
- Shared mutable state, statics, singletons, caches, globals
- Time-dependent logic, threading/concurrency, async/await boundaries
- Serialization/deserialization, schema changes, parsing/formatting
- Reflection, dynamic invocation, dependency injection registration
- Error handling, retries, circuit breakers, fallbacks
- Logging/telemetry that might leak sensitive data or trigger noisy alerts

### Behavioral Preservation
- Flag changes that might alter:
  - ordering (iteration order, sorting, “first match wins” logic)
  - timing (timeouts, retries, async vs sync behavior)
  - numeric behavior (rounding, integer division, culture/locale issues)
  - null/empty semantics and default values
- Prefer additive changes over modifications when possible (add a new method/branch rather than altering a fragile path).

### Tests & Evidence Required
- Require evidence that behavior is preserved:
  - golden/master tests, snapshot tests, characterization tests, or targeted integration tests
  - explicit reproduction steps for the bug being fixed
- If tests are absent or difficult, request:
  - a minimal “characterization test” that locks current behavior before changing it
  - or concrete before/after examples (inputs/outputs, logs, screenshots)

### Guidance for Review Comments
- Label high-risk concerns explicitly: "high risk", "medium risk", "nit".
- When suggesting improvements, provide a safer alternative with minimal scope.
```

##### "This repo is cursed, be careful" mode

This one is more opinionated. It assumes:

- build/test might be flaky
- behavior might be emergent
- "obvious improvements" can awaken ancient spirits

```markdown
## “Cursed Repo” Mode (Extreme Caution Review)

Treat the repository as hazardous and unpredictable. Default to caution.

### Sacred Rules
- Do not recommend “cleanup”, “modernization”, or “while I’m here” refactors.
- Avoid changes that are hard to roll back.
- Prefer a boring fix over an elegant one.

### Diff Hygiene
- If the PR mixes functional changes with formatting/refactors, request splitting into separate PRs.
- Minimize file touches: only edit files necessary for the change.
- Avoid large-scale renames/moves because they destroy `git blame` and hide behavior changes.

### Stabilize Before Improve
- If code is confusing, request comments or small clarifying helpers rather than rewrites.
- If behavior is unclear, ask for a characterization test rather than speculation.
- Don’t assume “dead code” is dead. Ask for evidence before removal.

### Operational Safety
- Call out changes that could affect production stability:
  - config defaults, env vars, feature flags
  - logging volume (cost), log formats consumed downstream
  - retry loops, polling intervals, timeouts
  - database queries and migrations
- Prefer feature flags or gradual rollout patterns for risky behavior changes.

### Failure Mode Thinking
- For any non-trivial change, consider:
  - What happens when dependencies fail?
  - What happens under partial data?
  - What happens under concurrency?
  - What happens when the input is malformed?
- Request explicit handling or tests for at least one realistic failure mode.

### Review Voice & Tone
- Be direct and pragmatic.
- Avoid speculative redesign. Focus on preventing regressions.
- If recommending anything beyond minimal scope, label it "optional / future work" and don’t block approval on it.
```

##### A tiny add-on that makes both modes work better

If you add this, Copilot gets *way* better at not inventing "safe" assumptions:

```markdown
## Assumptions Policy
- If behavior is not proven by tests or documentation, treat it as unknown.
- Ask for evidence (tests, reproduction steps, or examples) instead of assuming intent.
- Prefer preserving behavior over interpreting intent.
```

These instructions push Copilot to act less like "enthusiastic intern with a refactor hammer" and more like "cautious maintainer trying not to break the spell that keeps prod alive."

If you later want to go full ritual circle, the next upgrade is adding a **risk rubric** (approve/soft-block/hard-block) so Copilot consistently categorizes findings by severity and rollout risk.
