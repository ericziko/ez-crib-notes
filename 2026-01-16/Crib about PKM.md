---
created: 2026-01-16T02:35
updated: 2026-01-16T02:38
uid: 70569b9c-c036-40db-9fc1-11d4384600d9
---
# Crib about PKM

## 2\. 🧠 PKM (The Note-Taking App That’s Actually a Database)

This one surprised me most. 🧠 PKM feels like a GUI-first app. It’s Markdown notes with a nice interface. But underneath? It’s just files in a folder. And it has a localhost API.

I needed a knowledge base for our team that could version-control, support full-text search, and integrate with our tooling. Confluence felt too heavy. Notion was too locked-in. Plain Markdown files were too unstructured.

🧠 PKM ended up being perfect, but not how you’d expect.

We run 🧠 PKM headless on a server. No GUI, just the API exposed via localhost. Our CI pipeline writes documentation directly to the vault. Engineers write architectural decision records (ADRs) as Markdown, push to Git, and a webhook updates 🧠 PKM. The search and graph features still work — they’re just accessed via API.

Here’s how we query it:

```c
curl -X POST http://localhost:27123/search \
  -H "Content-Type: application/json" \
  -d '{"query": "authentication", "contextLength": 100}'
```

Returns JSON with matches, backlinks, and context. We built a Slack bot that queries this. Someone asks “how do we handle rate limiting?” and the bot searches the knowledge base, pulls relevant ADRs, and posts them inline. Engineers actually use it because it’s faster than searching Confluence.

The controversial take here: ==🧠 PKM is a better knowledge base backend than Confluence.== Confluence is designed for end users. 🧠 PKM is designed for files and APIs. The GUI is optional. ==We let engineers edit locally with 🧠 PKM installed, changes sync via Git==, and the headless instance indexes everything.

==Cost? Zero, because 🧠 PKM is free for commercial use== if you self-host. Confluence Enterprise was $15k/year for our team size.

### Why Markdown + API Beats Web-Based Knowledge Bases

Here’s what took me too long to realize: web-based knowledge bases have an incentive to lock you in. They want you editing in their interface, stuck on their platform. But our workflow is Git-native. PRs, reviews, version history — that’s where we live.

==🧠 PKM doesn’t care. It reads files. It has an API. It doesn’t try to own your workflow. That’s rare in this space, and frankly, undervalued.==