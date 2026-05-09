---
title: graphify run notes — full-repo knowledge graph
created: '2026-05-09T00:00:00+00:00'
modified: '2026-05-09T00:00:00+00:00'
tags:
  - graphify
  - knowledge-graph
  - claude-code
  - tooling
  - runbook
---

# graphify run notes — full-repo knowledge graph

Runbook for the `/graphify .` invocation that produced `graphify-out/` on 2026-05-09, what worked, what didn't, and how to re-run the LLM-assisted semantic pass with a Gemini API key.

## What was produced

Outputs live in `graphify-out/`:

| File | What it is |
|------|-----------|
| `graph.html` | Interactive force-directed graph (open in any browser, no server) |
| `graph.json` | Raw graph data — nodes, edges, communities — for downstream tools |
| `GRAPH_REPORT.md` | Human-readable audit: god nodes, surprising edges, community labels, suggested questions |
| `manifest.json` | File list + hashes — used by `/graphify --update` to detect changes |
| `cost.json` | Cumulative LLM token spend across runs |
| `cache/ast/` | Per-file AST extraction cache (deterministic, free) |
| `cache/semantic/` | Per-file semantic extraction cache (LLM-driven, currently empty) |

Headline numbers: **899 nodes, 891 edges, 135 communities** from 189 .NET / PowerShell source files.

## What graphify did, step by step

1. **Detect** (`graphify.detect.detect`) walked the repo and classified 431 files: 189 code, 187 docs, 10 papers, 45 images. 48 sensitive-looking files were skipped automatically.
2. **Filtered** the corpus before paying for LLM calls:
   - Dropped 74 build artifacts under `bin/` and `obj/`.
   - Dropped all 45 "images" — every one was an empty-file placeholder under `Dapper.ETL.*Tests/bin/Debug/.../EmptyFiles/image/empty.{png,jpg,…}`. Sending them to a vision model would have been pure waste.
3. **AST extraction** (`graphify.extract.extract`) — deterministic, free. Parsed the 189 code files with tree-sitter and built **996 nodes / 1,479 edges** representing classes, methods, calls, and imports. Cached per-file in `graphify-out/cache/ast/`.
4. **Semantic extraction** — *this is the step that did NOT complete*. See [What didn't work](#what-didnt-work) below.
5. **Build + cluster** (`graphify.build`, `graphify.cluster`) — built a NetworkX graph, ran Louvain community detection (135 communities), scored each community's internal cohesion.
6. **Analyze** — found god nodes (high degree), surprising connections (high-betweenness bridges), and weakly-connected components.
7. **Label communities** — top 30 communities labeled by hand from sampled node names; the long tail (mostly singletons) auto-labeled by their most prominent node.
8. **Generate outputs** — `GRAPH_REPORT.md`, `graph.json`, `graph.html`, manifest, cost tracker.
9. **Benchmark** — claimed 21.1× token reduction vs. naively dumping the corpus into a prompt.

## What didn't work

Semantic extraction never ran. The plan was to dispatch ~15 Claude `general-purpose` subagents in parallel, each chunking 22 files of docs/code/papers and writing a `graphify-out/.graphify_chunk_NN.json` fragment. Every subagent hit the same wall:

> Write tool denied for `/Users/ericziko/-🏦gitHub/ericziko/ez-crib-notes/graphify-out/.graphify_chunk_NN.json` — and Bash heredoc was also denied.

This happened on the first dispatch. After the user explicitly granted write permission and we re-dispatched, the denials persisted — meaning the block is at the harness's subagent-permission layer, not the user-prompt layer. Each subagent had already done the LLM analysis (e.g. chunk 1 produced 26 nodes / 40 edges in memory) but couldn't persist a single byte.

The fallback: build the graph from AST data alone. That's what `graphify-out/` currently contains.

**What's missing from the AST-only graph:**
- All 187 markdown notes (the date-organized `2026/MM/...` folders).
- The 10 papers / cross-cutting tutorials (`PowerShell Background Processes Tutorial.md`, etc.).
- The `dapper-etl/docs/` architecture documents.
- All `semantically_similar_to`, `rationale_for`, `tagged_with`, `cites`, and `references` edges that link concepts across files.
- All hyperedges (3+ node groupings).

The graph you have is a code structure map of `dapper-etl` plus a sliver of the PowerShell helm-chart tooling. It's useful — but it's not the notes-aware knowledge graph the corpus is actually capable of producing.

## Re-running with Gemini (recommended)

Gemini bypasses the subagent-write problem entirely: graphify calls `extract_corpus_parallel(files, backend="gemini")` directly from the parent process, no subagents involved.

### 1. Get a key

Free-tier keys work and are sufficient for a corpus this size. Get one at:

- https://aistudio.google.com/apikey

(Google AI Studio → "Get API key" → "Create API key in new project".)

### 2. Install the Gemini extra

```bash
pipx inject graphifyy 'graphifyy[gemini]'
# or, if you installed via pip directly:
pip install 'graphifyy[gemini]'
```

The graphify binary on this machine lives in a pipx venv at `/Users/ericziko/.local/pipx/venvs/graphifyy/`, so `pipx inject` is the right command.

### 3. Export the key, then re-run

```bash
export GEMINI_API_KEY=AIza…              # or GOOGLE_API_KEY — graphify accepts either

cd ~/-🏦gitHub/ericziko/ez-crib-notes
# in Claude Code:
/graphify .
```

When the run starts, you should NOT see the `Tip: set GEMINI_API_KEY…` line. If you do, the env var didn't propagate into the Claude Code session — set it in `~/.zshrc` and start a fresh Claude Code session, or pass it via `claude` startup env.

### 4. What changes on the Gemini run

- Step 3 (semantic extraction) becomes a single-process Gemini batch call — no subagents, no permission walls.
- All 312 non-build files (code + docs + papers + Helm templates) get LLM analysis.
- Expect the graph to roughly **double in size** (~1,800–2,500 nodes, ~3,000–5,000 edges) once notes and rationale concepts are linked in.
- Expect the run to take 5–15 minutes depending on your Gemini tier.
- `cost.json` will start showing real input/output token totals (currently `0` because no LLM ran).

### 5. Optional: incremental updates

Once the first full Gemini run is cached, future runs only re-extract changed files:

```bash
# in Claude Code:
/graphify . --update
```

This compares `manifest.json` against the current filesystem and only sends new/modified files to Gemini.

## Alternative: skip Gemini, keep AST-only

If you don't want to set up Gemini, the AST-only graph is still useful for:
- Navigating `dapper-etl` test ↔ system-under-test pairings.
- Finding the god nodes in the test suite (the top of the list is dominated by `*Tests` classes — likely worth splitting).
- Spotting the PowerShell Helm-chart-tooling community (`Compare-FlatYaml`, `Get-HelmChartVariables`, etc.) as one self-contained cluster.
- Seeing the 25 weakly-connected nodes the report flags as "documentation gaps."

It is *not* useful for any question that involves a markdown note — those nodes don't exist in the graph yet.

## Why the subagent path failed (root cause)

Claude Code subagents inherit a stricter permission scope than the parent agent. The `Write` tool's path-based allowlist that the parent has does not propagate to children. Granting "Write to `graphify-out/`" via the runtime permission prompt only authorizes the parent — every subagent gets re-prompted (or, when the harness is in headless / batched mode, auto-denied).

Two possible structural fixes for next time:

1. **Allowlist subagent writes globally** in `.claude/settings.local.json`:
   ```json
   {
     "permissions": {
       "allow": ["Write(**/graphify-out/**)"]
     }
   }
   ```
   (Not yet verified — the harness's documented permission grammar may differ.)

2. **Use a backend that doesn't dispatch subagents.** Gemini is the supported escape hatch; the graphify CLI accepts `--model` and reads `GEMINI_API_KEY` / `GOOGLE_API_KEY` from the environment.

Option 2 is the recommended path and is what this runbook documents.

## Useful follow-up commands once the graph exists

```bash
# Ask a question with BFS over the graph
graphify query "how does the ETL orchestrator coordinate transactions?"

# Trace the shortest semantic path between two nodes
graphify path "EtlOrchestrator" "TransactionManager"

# Plain-language explanation of a single node and what it touches
graphify explain "ColumnMapper"

# Push the graph to a running Neo4j for Cypher exploration
graphify export neo4j --push bolt://localhost:7687 --user neo4j --password ...
```

All of these read `graph.json`, so they work whether the graph was built via AST-only or via Gemini.

## Files this run touched (under the repo)

- created: `graphify-out/` (entire directory)
- created: `2026/05/2026-05-09/graphify-run-notes.md` (this file)
- not touched: any source code, any existing note

## Suggested questions surfaced by the AST-only graph

From `GRAPH_REPORT.md` — questions worth chasing once you have time:

- Why does `IConfiguration` bridge *Connection & Logs Commands* → *Logs/Metrics/Dry Commands* → *Export Logs/Metrics Tests*? (betweenness 0.056 — high cross-community traffic)
- Why does `ILogger` bridge *RunEtl & SeedSource Commands* → *RecordingLogger* / *SerilogEtlLogger* / *Metrics Service*? (betweenness 0.048)
- What connects `TableCopyService`, `SerilogEtlLogger`, `StoredProcedureService` to the rest of the system? — 25 weakly-connected nodes detected, possibly missing edges or undocumented coupling.
- Should *ETL Core Services* be split into smaller, more focused modules? — community cohesion 0.06, low for a 41-node group.
