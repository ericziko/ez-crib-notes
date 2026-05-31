---
uid: db74295f-b18a-467c-b485-c0748ef7fe54
---
# Claude_Models_Complete_Guide

**Updated: May 2026**  
A comprehensive reference for understanding Anthropic's Claude models, their capabilities, and when to use each one.

---

## Table of Contents

1. [Quick Start: Model Selection Flowchart](#quick-start-model-selection-flowchart)
2. [The Model Tiers](#the-model-tiers)
3. [Detailed Model Profiles](#detailed-model-profiles)
4. [Capability Comparison Matrix](#capability-comparison-matrix)
5. [Use Case Guide](#use-case-guide)
6. [Advanced Features Explained](#advanced-features-explained)
7. [Performance & Cost Tradeoffs](#performance--cost-tradeoffs)
8. [Migration Path (Older Models)](#migration-path-older-models)

---

## Quick Start: Model Selection Flowchart

```
START: What do you need?

├─ "I want the absolute best"
│  └─> CLAUDE OPUS 4.7 (Frontier)
│      Best for: Complex reasoning, research, long coding tasks
│
├─ "I want fast AND smart (default choice)"
│  └─> CLAUDE SONNET 4.6 (High-Performance)
│      Best for: Agents, most production use, coding
│
├─ "I want SPEED for basic tasks"
│  └─> CLAUDE HAIKU 4.5 (Fast)
│      Best for: Simple Q&A, classification, high volume
│
├─ "I'm building a coding agent or taking extended reasoning"
│  └─> Consider CLAUDE OPUS 4.7 with reasoning_effort
│      (Extended thinking modes available)
│
└─ "I have specific security/compliance needs"
   └─> Contact Anthropic for specialized models
```

**TLDR:** Use **Sonnet 4.6** for most things. Use **Opus 4.7** when you need maximum intelligence. Use **Haiku 4.5** when you need speed.

---

## The Model Tiers

Claude models are organized into three capability tiers plus experimental variants:

### Tier 1: Frontier Models (Maximum Intelligence)

**Claude Opus 4.7** (`claude-opus-4-7`) — Latest, most capable
- **Released:** February 2026
- **Best For:** Complex research, long-form code generation, multi-step reasoning, architectural design
- **Key Strength:** Maximum intelligence for the hardest problems
- **Speed:** Moderate (slower than Sonnet, but faster than you'd expect)
- **Context Window:** Up to 200K tokens input, 16K tokens output
- **Reasoning Modes:** All (enabled, adaptive thinking; clear thinking; effort levels: low-max)
- **Special:** Fastest inference among Frontier models

**Claude Opus 4.6** (`claude-opus-4-6`) — Previous frontier
- **Released:** October 2025
- **Status:** Still excellent, slightly older than 4.7
- **Use When:** 4.7 not available or you're on a specific version lock
- **Same capabilities as 4.7** with marginal performance differences

**Claude Opus 4.5** (`claude-opus-4-5`) — Stable frontier variant
- **Released:** Months before 4.6
- **Status:** Still highly capable, proven in production
- **Context Window:** 200K input, 4K output
- **All Frontier Features:** Thinking, effort control, everything

**Claude Opus 4.1** (`claude-opus-4-1`) — Legacy frontier
- **Status:** Deprecated. Use 4.7 instead.
- **Reason:** 4.7 is cheaper and faster

**Claude Opus 4.0** (`claude-opus-4-0`) — Original frontier
- **Status:** Deprecated. Use 4.7 instead.

---

### Tier 2: High-Performance Models (Speed + Intelligence Balance)

**Claude Sonnet 4.6** (`claude-sonnet-4-6`) — **RECOMMENDED DEFAULT**
- **Released:** February 2026 (same release as Opus 4.7)
- **Best For:** Production agents, API endpoints, general-purpose coding, customer-facing features
- **Speed:** Fast (~2-3x faster than Opus 4.7)
- **Cost:** ~50% the price of Opus
- **Context Window:** 200K input, 4K output
- **Intelligence:** ~95% of Opus capability for most tasks
- **Reasoning Modes:** All supported (thinking, effort levels)
- **When to Choose:** Default for everything unless you need maximum intelligence
- **Real-World Example:** If Opus solves a problem in 10 seconds with $0.10 cost, Sonnet solves it in 4 seconds for $0.05

**Claude Sonnet 4.5** (`claude-sonnet-4-5`) — Previous high-performance
- **Released:** September 2025
- **Status:** Still excellent, slightly slower than 4.6
- **Thinking Support:** Yes (both enabled and adaptive modes)
- **Use When:** 4.6 not available

**Claude Sonnet 4.0** (`claude-sonnet-4-0`) — Legacy high-performance
- **Released:** Earlier
- **Status:** Deprecated. Use 4.6 instead.
- **Thinking Support:** Yes (extended thinking available)

---

### Tier 3: Fast Models (Maximum Speed)

**Claude Haiku 4.5** (`claude-haiku-4-5`) — **FASTEST MODEL AVAILABLE**
- **Released:** October 2025
- **Best For:** Simple questions, classification, text processing, high-volume APIs, chat
- **Speed:** ~5-10x faster than Sonnet, near-instant for simple tasks
- **Cost:** ~10% the price of Opus
- **Context Window:** 200K input, 4K output
- **Intelligence:** Solid for basic tasks, can handle moderate complexity
- **Reasoning Modes:** All supported (yes, even thinking modes!)
- **Real-World Example:** Process 1000 customer support questions/second at $1 total cost
- **Surprising Fact:** Haiku 4.5 is remarkably capable despite being the fastest

**Claude Haiku 3** (`claude-3-haiku-20240307`) — Legacy fast model
- **Released:** March 2024
- **Status:** Deprecated. Use Haiku 4.5 instead.
- **Only use if:** Specific version pinning required

---

### Experimental: Frontier Research Models

**Claude Mythos (Preview)** (`claude-mythos-preview`)
- **Status:** Experimental, available for beta testing
- **Focus:** Strongest in coding and cybersecurity
- **New Architecture:** Different from the Opus/Sonnet/Haiku line
- **Use Case:** If you're doing cutting-edge security research or extreme coding challenges
- **Warning:** Preview API may change; expect breaking updates
- **Availability:** Limited beta access

---

## Detailed Model Profiles

### Claude Opus 4.7 (Frontier Intelligence)

**When to Use**
- ✅ Research papers and literature reviews
- ✅ Complex multi-step reasoning (10+ steps)
- ✅ Architectural design and system planning
- ✅ Legal/financial/medical analysis
- ✅ Advanced code generation (novel algorithms)
- ✅ When accuracy matters more than speed
- ❌ NOT for simple Q&A (use Haiku)
- ❌ NOT for high-volume APIs (use Sonnet)

**Key Characteristics**

| Aspect | Details |
|--------|---------|
| **Reasoning Ability** | Best-in-class reasoning, excellent at finding subtle connections |
| **Code Quality** | Generates production-ready code, handles edge cases |
| **Language** | Superior multilingual capability |
| **Factual Accuracy** | Highest accuracy for knowledge-based questions |
| **Coding Contests** | Can solve complex algorithmic problems |
| **Planning Ability** | Excellent multi-step planning and strategic thinking |

**Available Capabilities**

| Feature | Support |
|---------|---------|
| Thinking (enabled) | ✅ Yes |
| Thinking (adaptive) | ✅ Yes |
| Extended thinking | ✅ Yes |
| Effort levels (low/medium/high/xhigh/max) | ✅ All levels |
| Clear thinking | ✅ Yes |
| Clear tool uses | ✅ Yes |
| Compact mode | ✅ Yes |
| Batch processing | ✅ Yes |
| Image input | ✅ Yes |
| PDF input | ✅ Yes |
| Structured outputs | ✅ Yes |
| Citations | ✅ Yes |
| Code execution | ✅ Yes |

**Performance Profile**

```
Reasoning Quality:   ████████████████████ (10/10)
Speed:              ███████░░░░░░░░░░░░░ (3.5/10)
Cost Efficiency:    ██░░░░░░░░░░░░░░░░░░ (1/10)
Multilingual:       ████████████████████ (10/10)
Reliability:        ████████████████████ (10/10)
```

**Example Prompts**
- "Design a distributed transaction system for a global e-commerce platform"
- "Analyze this academic paper and identify methodological limitations"
- "Generate a complete authentication system with security best practices"

---

### Claude Sonnet 4.6 (High-Performance, Recommended Default)

**When to Use (DEFAULT CHOICE)**
- ✅ Production APIs and microservices
- ✅ Customer-facing chatbots
- ✅ Agentic workflows (multiple tool calls)
- ✅ Content generation (blogs, emails, marketing)
- ✅ Code review and refactoring
- ✅ Data analysis and SQL generation
- ✅ Moderate complexity tasks
- ❌ NOT for maximum intelligence needs (use Opus)

**Key Characteristics**

| Aspect | Details |
|--------|---------|
| **Sweet Spot** | Best balance of speed, cost, and capability |
| **Speed** | Typically returns in 1-3 seconds |
| **Reliability** | Rock-solid for production workloads |
| **Code Quality** | Excellent for most programming tasks |
| **Consistency** | Very predictable behavior |

**Available Capabilities**

| Feature | Support |
|---------|---------|
| Thinking (enabled) | ✅ Yes |
| Thinking (adaptive) | ✅ Yes |
| Effort levels | ✅ All levels |
| Clear thinking | ✅ Yes |
| Clear tool uses | ✅ Yes |
| Compact mode | ✅ Yes |
| Batch processing | ✅ Yes |
| Image input | ✅ Yes |
| PDF input | ✅ Yes |
| Structured outputs | ✅ Yes |
| Citations | ✅ Yes |
| Code execution | ✅ Yes |

**Performance Profile**

```
Reasoning Quality:   ███████████████████░ (9/10)
Speed:              ██████████░░░░░░░░░░ (5.5/10)
Cost Efficiency:    ██████████░░░░░░░░░░ (5.5/10)
Production Ready:   ████████████████████ (10/10)
Agentic Capability: ████████████████████ (10/10)
```

**Example Use Cases**
```python
# Perfect for production APIs
response = client.messages.create(
    model="claude-sonnet-4-6",
    messages=[{"role": "user", "content": "Convert this CSV to SQL INSERT statements"}]
)

# Ideal for agents with tool use
response = client.messages.create(
    model="claude-sonnet-4-6",
    max_tokens=2048,
    tools=[...],  # Excellent at multi-step tool orchestration
    messages=[...]
)
```

---

### Claude Haiku 4.5 (Speed Champion)

**When to Use**
- ✅ High-volume APIs (1000+ req/sec)
- ✅ Simple classification tasks
- ✅ Chatbot responses for well-defined domains
- ✅ Text processing (summarization, tagging)
- ✅ Customer support triage
- ✅ Real-time applications
- ✅ Budget-constrained scenarios
- ❌ NOT for complex reasoning (use Opus)
- ❌ NOT when high accuracy needed (use Sonnet)

**Key Characteristics**

| Aspect | Details |
|---------|---------|
| **Speed** | Typically <500ms, even <100ms for simple tasks |
| **Cost** | Cheapest available model |
| **Capability** | Surprisingly capable for basic-to-moderate tasks |
| **Latency** | Lowest latency option |
| **Consistency** | Good for templated/pattern-based tasks |

**Available Capabilities**

| Feature | Support |
|---------|---------|
| Thinking (enabled) | ✅ Yes |
| Thinking (adaptive) | ✅ Yes |
| Effort levels | ✅ Low/medium (high/xhigh available but slower) |
| Batch processing | ✅ Yes |
| Image input | ✅ Yes |
| PDF input | ✅ Yes |
| Structured outputs | ✅ Yes |
| Citations | ✅ Yes |
| Code execution | ✅ Yes |

**Performance Profile**

```
Speed:              ████████████████████ (10/10)
Cost Efficiency:    ████████████████████ (10/10)
Reasoning Quality:  ███████░░░░░░░░░░░░░ (3.5/10)
Good For Simple:    ████████████████████ (10/10)
```

**Example Use Cases**
```python
# Perfect for high-volume classification
for email in inbox:
    response = client.messages.create(
        model="claude-haiku-4-5",  # <100ms response
        messages=[{
            "role": "user",
            "content": f"Classify email as spam/important/read-later: {email.subject}"
        }]
    )

# Real-time chat responses
response = client.messages.create(
    model="claude-haiku-4-5",
    messages=[user_messages],
    max_tokens=500  # Limited output for speed
)
```

---

## Capability Comparison Matrix

### Core Capabilities

| Capability | Opus 4.7 | Sonnet 4.6 | Haiku 4.5 | Notes |
|------------|----------|-----------|-----------|-------|
| **Reasoning Quality** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐☆ | ⭐⭐⭐☆☆ | Opus best for complex reasoning |
| **Speed** | Medium | Fast | Very Fast | Haiku is ~5-10x faster |
| **Cost per 1M tokens** | $15 (input) | $3 (input) | $0.80 (input) | Haiku ~18x cheaper |
| **Context Window** | 200K tokens | 200K tokens | 200K tokens | All identical |
| **Max Output** | 16K tokens | 4K tokens | 4K tokens | Opus can output more |
| **Thinking Support** | ✅ Full | ✅ Full | ✅ Full | All models support thinking |
| **Effort Control** | ✅ All (low-max) | ✅ All (low-max) | ✅ Low-medium | Haiku limited on high effort |
| **Tool Use** | ✅ Excellent | ✅ Excellent | ✅ Good | All models support tools |

### Input/Output Capabilities

| Feature | Opus 4.7 | Sonnet 4.6 | Haiku 4.5 |
|---------|----------|-----------|-----------|
| Text input | ✅ | ✅ | ✅ |
| Image input | ✅ | ✅ | ✅ |
| PDF input | ✅ | ✅ | ✅ |
| Structured output | ✅ | ✅ | ✅ |
| Video input | ❌ | ❌ | ❌ |
| Audio input | ❌ | ❌ | ❌ |

### Advanced Features

| Feature | Opus 4.7 | Sonnet 4.6 | Haiku 4.5 | What It Does |
|---------|----------|-----------|-----------|-------------|
| Thinking (enabled) | ✅ | ✅ | ✅ | Extended internal reasoning |
| Thinking (adaptive) | ✅ | ✅ | ✅ | Auto-detect when to think |
| Clear thinking | ✅ | ✅ | ✅ | Show internal reasoning |
| Clear tool uses | ✅ | ✅ | ✅ | Transparency for tool calls |
| Compact mode | ✅ | ✅ | ✅ | Context compression |
| Citations | ✅ | ✅ | ✅ | Generate citations for sources |
| Code execution | ✅ | ✅ | ✅ | Run code sandboxed |
| Batch API | ✅ | ✅ | ✅ | Process many requests efficiently |

---

## Use Case Guide

### By Task Type

#### Research & Analysis
| Task | Recommended | Alternative | Reason |
|------|-------------|-------------|--------|
| Literature review | Opus 4.7 | Sonnet 4.6 | Needs nuanced understanding |
| Data analysis | Opus 4.7 / Sonnet | Haiku | Complexity varies |
| Competitive analysis | Sonnet 4.6 | Opus 4.7 | Good balance of cost/capability |
| Fact-checking | Opus 4.7 | Sonnet 4.6 | Accuracy > speed |

#### Code Generation & Development
| Task | Recommended | Alternative | Reason |
|------|-------------|-------------|--------|
| Simple script | Haiku 4.5 | Sonnet | Fast enough, cheap |
| Feature implementation | Sonnet 4.6 | Opus 4.7 | Balanced approach |
| Architectural design | Opus 4.7 | Sonnet 4.6 | Needs best reasoning |
| Bug fixing | Sonnet 4.6 | Haiku 4.5 | Usually moderate complexity |
| Code review | Sonnet 4.6 | Opus 4.7 | Balanced cost/quality |
| Performance optimization | Opus 4.7 | Sonnet 4.6 | Complex tradeoff analysis |

#### Customer-Facing Applications
| Task | Recommended | Alternative | Reason |
|------|-------------|-------------|--------|
| Chatbot responses | Sonnet 4.6 | Haiku 4.5 | Balance of quality & speed |
| Support ticket triage | Haiku 4.5 | Sonnet 4.6 | Classification task, low latency |
| Email response drafting | Sonnet 4.6 | Haiku 4.5 | Needs nuance, still fast |
| FAQ generation | Sonnet 4.6 | Opus 4.7 | One-time generation task |
| Personalized recommendations | Haiku 4.5 | Sonnet 4.6 | Simple matching task |

#### Content Creation
| Task | Recommended | Alternative | Reason |
|------|-------------|-------------|--------|
| Blog post writing | Sonnet 4.6 | Opus 4.7 | Good enough, faster |
| Marketing copy | Haiku 4.5 | Sonnet 4.6 | Template-based, high volume |
| Long-form content | Opus 4.7 | Sonnet 4.6 | Needs sophistication |
| Email templates | Haiku 4.5 | Sonnet 4.6 | Simple structure |
| Product descriptions | Sonnet 4.6 | Haiku 4.5 | Moderate complexity |

#### Data Processing
| Task | Recommended | Alternative | Reason |
|------|-------------|-------------|--------|
| CSV transformation | Haiku 4.5 | Sonnet 4.6 | Pattern matching, repetitive |
| JSON schema generation | Sonnet 4.6 | Haiku 4.5 | Usually straightforward |
| Text classification | Haiku 4.5 | Sonnet 4.6 | Simple categories |
| Sentiment analysis | Haiku 4.5 | Sonnet 4.6 | Standard classification |
| Entity extraction | Haiku 4.5 | Sonnet 4.6 | Pattern-based task |

#### Real-Time & High-Volume
| Task | Recommended | Cost Example |
|------|-------------|--------------|
| 1000 req/sec API | Haiku 4.5 | ~$0.22/1M requests |
| Streaming responses | Sonnet 4.6 | ~$1.32/1M requests |
| Bulk processing | Batch API | Cheapest option (50% discount) |
| Real-time chat | Sonnet 4.6 | ~$1.32/1M requests |

---

## Advanced Features Explained

### Thinking Modes (Extended Reasoning)

**What It Does:** The model allocates extra compute to "think through" a problem before responding.

**Two Types:**

1. **Thinking: enabled** - Model explicitly reasons through before answering
   ```python
   response = client.messages.create(
       model="claude-opus-4-7",
       max_tokens=2000,  # For thinking + response
       thinking={
           "type": "enabled",
           "budget_tokens": 1024  # Allocate compute to thinking
       },
       messages=[...]
   )
   ```
   - See the thinking process in the response
   - Useful for: Complex problems, debugging, planning
   - Cost: Counts against token limit but improves accuracy

2. **Thinking: adaptive** - Model automatically decides when to think
   ```python
   response = client.messages.create(
       model="claude-sonnet-4-6",
       thinking={
           "type": "adaptive"  # Auto-decide when useful
       },
       messages=[...]
   )
   ```
   - Best for: General use, let model choose
   - More efficient: Only thinks when needed

**When to Use Thinking**
- ✅ Complex logic problems
- ✅ Math problems requiring work
- ✅ Debugging tricky code
- ✅ Strategic planning
- ❌ NOT for simple Q&A (wastes tokens)
- ❌ NOT for classification (unnecessary)

---

### Effort Control (reasoning_effort)

**What It Does:** Control how much reasoning compute is allocated.

**Levels (Low → Max):**

| Level | Description | Best For | Speed Impact |
|-------|-------------|----------|--------------|
| **low** | Quick answers, minimum reasoning | Simple classification | Fastest |
| **medium** | Balanced reasoning and speed | General use (default) | Fast |
| **high** | More reasoning, still reasonably fast | Complex problems | Slower |
| **xhigh** | Heavy reasoning, deeper analysis | Very complex problems | Much slower |
| **max** | Maximum reasoning capability | Hardest problems only | Slowest |

**Available by Model:**

```
Opus 4.7:   ✅ All levels (low, medium, high, xhigh, max)
Sonnet 4.6: ✅ All levels (low, medium, high, xhigh, max)
Haiku 4.5:  ✅ low, medium (high+ slower but available)
```

**Example Usage**

```python
# For a straightforward question - use low effort
response = client.messages.create(
    model="claude-sonnet-4-6",
    reasoning_effort="low",  # Quick answer
    messages=[{"role": "user", "content": "What's 2+2?"}]
)

# For complex analysis - use max effort
response = client.messages.create(
    model="claude-opus-4-7",
    reasoning_effort="max",  # Deep thinking
    messages=[{"role": "user", "content": "Analyze this algorithm for correctness..."}]
)
```

---

### Clear Thinking (Context Management)

**What It Does:** Shows you the model's internal reasoning and thought process.

**How It Works:**
```python
response = client.messages.create(
    model="claude-opus-4-7",
    max_tokens=3000,
    thinking={
        "type": "enabled",
        "budget_tokens": 2000
    },
    messages=[...],
    betas=["interleaved-thinking-2025-05-14"]  # Beta feature
)

# Response includes thinking blocks showing the reasoning
for block in response.content:
    if block.type == "thinking":
        print("Model's reasoning:", block.thinking)
    elif block.type == "text":
        print("Final answer:", block.text)
```

**Benefits:**
- Transparency into why the model chose an answer
- Helpful for debugging errors
- Useful for understanding complex reasoning
- Good for building trust in critical applications

---

### Context Management Strategies

**Available Strategies:**

1. **clear_thinking** - Show reasoning transparently
2. **clear_tool_uses** - Show tool calls transparently  
3. **compact** - Compress context efficiently

**Automatic Context Management:**
```python
# Let model manage its own context
response = client.messages.create(
    model="claude-opus-4-7",
    context_management="auto",  # Automatic optimization
    messages=[...]
)
```

---

### Structured Outputs (JSON Mode)

**What It Does:** Forces model to always return valid JSON.

**Use Case:**
```python
from typing import Literal

class SentimentAnalysis(BaseModel):
    sentiment: Literal["positive", "negative", "neutral"]
    confidence: float
    reasoning: str

response = client.messages.create(
    model="claude-sonnet-4-6",
    messages=[...],
    response_format=SentimentAnalysis  # Always returns valid JSON
)
```

**Benefits:**
- Guaranteed parseable output
- No "fixing" malformed JSON
- Type safety
- Good for downstream processing

---

## Performance & Cost Tradeoffs

### Cost Comparison (Per 1M Input Tokens)

```
Haiku 4.5:    $0.80      ███░░░░░░░░░░░░░░░░░ (Cheapest)
Sonnet 4.6:   $3.00      ███████░░░░░░░░░░░░░ (5x more than Haiku)
Opus 4.7:     $15.00     █████████████████░░░ (19x more than Haiku)
```

### Speed Comparison (Relative Throughput)

```
Haiku 4.5:    Baseline (1x)        ████████████████████
Sonnet 4.6:   ~0.3-0.5x            ██████████░░░░░░░░░░
Opus 4.7:     ~0.2-0.3x            ██████░░░░░░░░░░░░░░
```

### Real-World Cost Examples

**Scenario: Processing 1 Million Customer Support Emails**

| Model | Time | Cost | Total Cost |
|-------|------|------|-----------|
| Haiku 4.5 | ~2-3 hours | $0.80 | ✅ $0.80 |
| Sonnet 4.6 | ~5-8 hours | $3.00 | $3.00 |
| Opus 4.7 | ~10-15 hours | $15.00 | $15.00 |

**Decision:** Haiku 4.5 at 18x cheaper for email classification

---

**Scenario: Building a Code Generation API**

| Metric | Haiku | Sonnet | Opus |
|--------|-------|--------|------|
| Monthly volume | 100K requests | 100K requests | 100K requests |
| Avg tokens/request | 500 in, 200 out | 500 in, 500 out | 500 in, 1000 out |
| Monthly input cost | $40 | $150 | $750 |
| Monthly output cost | $16 | $150 | $1500 |
| **Total/month** | **$56** | **$300** | **$2250** |
| Avg latency | 50ms | 500ms | 1500ms |

**Decision:** Sonnet 4.6 for balance of quality and latency

---

### When Cost Doesn't Matter

These scenarios justify Opus 4.7 despite high cost:

- **One-time research** (literature review, strategic analysis)
- **High-stakes decisions** (medical, legal, financial analysis)
- **Complex problem-solving** (novel algorithms, architecture design)
- **Where accuracy = revenue** (if 1% better accuracy = $10K+ savings)

---

## Migration Path (Older Models)

### If You're Using Claude 3.5 Sonnet

**Action:** Migrate to Claude Sonnet 4.6
- ✅ 40% faster
- ✅ Better reasoning
- ✅ Same context window
- ✅ Drop-in replacement (same API)

```python
# Before
model="claude-3-5-sonnet-20241022"

# After
model="claude-sonnet-4-6"
```

### If You're Using Claude 3 Opus

**Action:** Migrate to Claude Opus 4.7
- ✅ 3-5x faster
- ✅ Much better reasoning
- ✅ Same capabilities
- ✅ Drop-in replacement

### If You're Using Claude 3 Haiku

**Action:** Migrate to Claude Haiku 4.5
- ✅ 4x faster
- ✅ Much better at complex tasks
- ✅ Still the cheapest
- ✅ Drop-in replacement

**Cost Savings from Migration:**
- Keeping Haiku 3 costs same but gets worse performance
- Claude 3 → Haiku 4.5: Same speed, better quality, same cost
- Claude 3 → Sonnet 4.6: 3-5x faster, better quality, slightly higher cost

---

## Quick Reference: Model Capabilities by Number

### Supported Across All Models

✅ Text input  
✅ Image input (PNG, JPEG, GIF, WEBP, etc.)  
✅ PDF documents  
✅ Tool/function calling  
✅ Batch processing  
✅ Structured outputs (JSON)  
✅ Citations  
✅ Code execution (sandboxed)  

### Thinking Support Across Models

All models support:
- ✅ Thinking type: enabled
- ✅ Thinking type: adaptive
- ⚠️ Max output: Opus 4.7 (16K) > Sonnet/Haiku (4K)

### Effort Levels by Model

```
Opus 4.7:   low, medium, high, xhigh, max  (5 levels)
Sonnet 4.6: low, medium, high, xhigh, max  (5 levels)
Haiku 4.5:  low, medium, high*, xhigh*, max*  (all available, slower on high+)
```

---

## Summary: Which Model to Choose?

### Decision Tree

```
Question 1: What's your priority?

  ├─ "Accuracy/Quality is everything"
  │  └─ Use OPUS 4.7
  │
  ├─ "Speed AND Quality are both important"
  │  └─ Use SONNET 4.6 ⭐ (DEFAULT ANSWER)
  │
  └─ "Cost is the main constraint"
     └─ Use HAIKU 4.5

Additional Questions:

Question 2: Is this for high-volume API?
  ├─ Yes (1000+ req/sec) → Use HAIKU 4.5 (definitely)
  └─ No → Continue to Q1

Question 3: How complex is the task?
  ├─ Simple (classification, Q&A) → HAIKU 4.5
  ├─ Moderate (coding, analysis) → SONNET 4.6 ⭐
  └─ Complex (research, architecture) → OPUS 4.7

Question 4: Do you need extended thinking?
  ├─ Yes, show thinking process → OPUS 4.7 + thinking:enabled
  ├─ Yes, auto-detect when → ANY MODEL + thinking:adaptive
  └─ No → Choose by complexity

Question 5: Budget constraints?
  ├─ Unlimited → OPUS 4.7
  ├─ Moderate → SONNET 4.6 ⭐
  └─ Tight → HAIKU 4.5
```

---

## Final Recommendations by Role

### Software Engineer Building an App
- **Default:** Sonnet 4.6
- **For complex logic:** Opus 4.7
- **For simple features:** Haiku 4.5
- **When unsure:** Sonnet 4.6

### Data Scientist / Analyst
- **Exploration/analysis:** Opus 4.7
- **Production pipelines:** Sonnet 4.6
- **Bulk processing:** Haiku 4.5

### Product Manager
- **Content generation:** Sonnet 4.6
- **High-volume customer features:** Haiku 4.5
- **Strategic analysis:** Opus 4.7

### Customer Support Teams
- **Ticket triage:** Haiku 4.5
- **Detailed responses:** Sonnet 4.6
- **Complex cases:** Opus 4.7

### DevOps / SRE
- **Log analysis:** Haiku 4.5 (fast, cheap)
- **Alert response:** Sonnet 4.6 (balanced)
- **Incident analysis:** Opus 4.7 (thorough)

### Startup / Cost-Conscious
- **Everything initially:** Haiku 4.5 (prove value)
- **Switch to Sonnet when scaling:** Better quality at acceptable cost
- **Only use Opus for critical tasks:** Justify the expense

---

## More Information

**Official Documentation:** https://platform.claude.com/docs/models

**API Reference:** https://platform.claude.com/docs/en/api/messages

**Pricing Details:** https://www.anthropic.com/pricing

**Model Capabilities API:** Use `GET /models` endpoint to programmatically query model capabilities

---

**Document Version:** 1.0 (May 2026)  
**Last Updated:** May 31, 2026  
**Claude Models Covered:** Opus 4.7, Opus 4.6, Sonnet 4.6, Haiku 4.5, plus legacy variants
