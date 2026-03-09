---
title: 🤖❓Xunit2 Parallelism
created: 2026-01-30T17:27:25
modified: 2026-01-30T17:33:39
---

# 🤖❓Xunit2 Parallelism
can you tell me about the parallel options in the execution of tests in Xunit 2 - especially when it comes to shared state such as databases, environment, variables, and speeding up execution where possible and avoiding shared state where necessary

xUnit.net 2's parallelism is powerful, but it's basically a "concurrency knife": it will cut your test time in half *or* cut you in half, depending on how much shared state you've secretly been petting.

Here's the mental model and the knobs you get, with a focus on shared state (DBs, env vars, globals) and the "go fast without going flaky" game.

## How xUnit 2 parallelism actually works

### 1) Test methods can run in parallel within a test class

By default, xUnit 2 will run test *methods* in the same class **sequentially** (important!) and run **different test classes in parallel** (also important).

So the default is basically:

- **Inside a class:** one-at-a-time
- **Across classes:** parallel

This is already a decent compromise: a class is often where people keep shared fixtures/state.

### 2) xUnit's unit of parallelization is the test collection

xUnit groups test classes into "collections". Classes in **different** collections can run in parallel. Classes in the **same** collection will not run in parallel with each other.

If you do nothing, xUnit uses a default behavior that effectively puts each test class into its own collection (which enables class-level parallelism).

## The knobs you can turn

### A) Turn parallelization on/off globally

At the assembly level (usually a file like `AssemblyInfo.cs` or any `.cs` file in the test project):

```cs
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
```

Use this when:

- You have unavoidable global shared state (process-wide env vars, single shared LocalDB instance name, global static config, etc.)
- You're stabilizing a flaky suite and want to re-enable parallelism later in a controlled way.

### B) Control how many threads xUnit uses

```cs
using Xunit;
[assembly: CollectionBehavior(MaxParallelThreads = 4)]
```

Notes:
- "More threads" is not always faster (DB-bound tests often hit contention and get slower).
- If your tests are I/O heavy (DB, HTTP, disk), modest parallelism (like 2–8) is usually the sweet spot.

You can also use:

```cs
[assembly: CollectionBehavior(MaxParallelThreads = 0)]
```

0 means "let xUnit pick" (it uses an internal heuristic).

### C) Use collections to  serialize specific sets of tests (shared state containment)

If a bunch of tests share a DB, or mutate environment variables, put them in the same collection so they don't step on each other.

```cs
using Xunit;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // no code here; this is just metadata
}

[Collection("Database collection")]
public class UserRepositoryTests
{
    // runs sequentially w.r.t. any other class in "Database collection"
}
```

**Key idea:** everything in "Database collection" will be mutually exclusive with everything else in that same collection.

### **D) You can also opt out per class**

If you just want "this class must never run in parallel with others" you still do it via collections (xUnit 2 doesn't have a "non-parallel class" attribute the way some frameworks do). You make a collection and put only that class (and any related ones) in it.

## **Shared state: what's safe vs what's a trap**

### **✅ Usually safe to parallelize**

- Pure unit tests (no I/O, no static state)
- Tests using isolated resources per test (unique temp folder, unique DB name, unique port)
- Read-only database queries against a shared DB (still risky if anything writes)

### **☠️ Usually not safe (process-wide shared state)**

- Environment.SetEnvironmentVariable(...) (mutates process env)
- Modifying static singletons / service locators / caches
- Changing current directory, culture, time zone (process/thread-level, can leak)
- Shared LocalDB / shared SQL database name with schema/data changes
- Global configuration objects (static options, ConfigurationManager, etc.)

If you must touch these, either:

1. isolate per test (preferred), or
2. serialize those tests into a collection, or
3. disable parallelization globally (last resort).

## Patterns that keep parallelism AND prevent flakiness

### Pattern 1: "One DB per test class" (fast-ish, stable)

- Use IClassFixture`<T>` to create a database once per class
- Tests in the class run sequentially by default, so internal ordering issues are reduced
- Different classes can still run in parallel **if each class has its own DB name**

This is the typical "integration suite" baseline.

### Pattern 2: "One DB per test" (slowest, most isolated)

- Great when tests do heavy mutation and you want maximum parallel safety
- Can be sped up with template DBs / restore-from-backup / file copy (LocalDB MDF clone) rather than full schema rebuild.

### Pattern 3: "Shared DB, but transaction-per-test + rollback" (fast, can be tricky)

- Each test opens a transaction and rolls back
- Watch out for:
    - code that opens its own connections outside the transaction scope
    - background jobs
    - triggers / side effects
    - snapshot/isolation semantics

This can be very fast when it works, but it's not always "honest isolation".

### Pattern 4: "Shared DB, Respawn-style reset"

- After each test, reset tables to a known baseline
- Helps keep a single DB per suite while keeping tests independent
- Still needs serialization if multiple tests hit the same DB at once unless each test uses its own schema/database or you coordinate resets.

## Practical guidance: how to decide your parallel strategy

### Step 1: Split tests into two universes

1. **Pure / isolated tests** → let them fly in parallel
2. **Shared-state integration tests** → quarantine them into collections

### Step 2: For integration tests, pick the isolation level

- If you can afford it: **unique DB per class** is the best balance
- If you need speed: **template DB copy** or **transaction/Respawn**
- If you can't avoid global state: **serialize collection** or **disable parallelization**

### Step 3: Tune MaxParallelThreads

- Start with something conservative like 4
- Measure runtime
- If DB contention is high, *lowering* threads can speed things up (counterintuitive but real)

## A concrete "shared DB" quarantine example

Let's say you have a set of tests that all talk to the same SQLLocalDB database instance name and mutate schema/data.

You do:

```cs
[CollectionDefinition("Shared LocalDB")]
public class SharedLocalDbCollection : ICollectionFixture<SharedLocalDbFixture> { }

[Collection("Shared LocalDB")]
public class OrdersIntegrationTests { }

[Collection("Shared LocalDB")]
public class CustomersIntegrationTests { }
```

Now those classes will not run at the same time, even if the rest of your suite is parallel.

## Speed tips that don't involve gambling with shared state

- **Prefer per-class DB** over per-test DB
- **Create schema once** then reset data (Respawn/transactions) instead of recreating schema every test
- **Avoid static caches** in test code unless you control them tightly
- **Use unique resource naming**: DB name, temp folder, queue name, etc. (GUID suffixes are boring but mighty)
- **Keep your "quarantined collections" small** so the rest of the suite can still parallelize

## Common gotchas that make people think "xUnit parallelism is broken"

- A fixture that uses a static connection string / static database name across classes
- Tests that mutate Environment.* and don't restore it reliably
- A singleton DI container shared across test classes
- Parallel tests fighting over the same temp directory
- "Helpful" optimization like reusing the same LocalDB across the entire test project

If you want to go deeper next, the highest-leverage move is to decide your database isolation strategy (per test, per class, per collection) and then wire that into IClassFixture / ICollectionFixture so the rules are enforced by structure, not discipline. That's how you get *fast* and *boring* (boring is the dream).
