---
title: 🤖❓Best Roslyn Analyzers
source: "https://chatgpt.com/share/6a2c4b49-08fc-83e8-88e2-45bec1734f9a"
author:
published:
created: 2026-06-12T11:09:54-07:00
description: "ChatGPT conversation with 2 messages"
tags:
  - "clippings"
  - para/resources/dotnet
modified: 2026-06-12T11:21:56-07:00
---

# 🤖❓Best Roslyn Analyzers

#para/resources/dotnet

## Summary

🤖❓ What are the best Roslyn analyzers to add to a .NET project in order to find issues of concurrency, memory leaks, etc.?

### 🤖💡 
I'll focus on analyzers you can actually add to a normal .NET repo, grouped by what they catch: correctness/concurrency, memory/resource lifetime, API design, security, and maintainability.

#### Best practical stack

```xml
<ItemGroup>
    <PackageReference Include="Meziantou.Analyzer" Version="*" PrivateAssets="all" />
    <PackageReference Include="Microsoft.VisualStudio.Threading.Analyzers" Version="*"
        PrivateAssets="all" />
    <PackageReference Include="AsyncFixer" Version="*" PrivateAssets="all" />
    <PackageReference Include="IDisposableAnalyzers" Version="*" PrivateAssets="all" />
    <PackageReference Include="SonarAnalyzer.CSharp" Version="*" PrivateAssets="all" />
    <PackageReference Include="Roslynator.Analyzers" Version="*" PrivateAssets="all" />
</ItemGroup>
```

#### Also turn on built-in .NET analyzers aggressively

```xml
<PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
</PropertyGroup>
```

#### My priority order

#### **Meziantou.Analyzer** 
Broad, high-signal rules for async, performance, cancellation tokens, resource usage, security, API misuse. It explicitly targets bugs, security issues, and best practices. [GitHub](https://github.com/meziantou/Meziantou.Analyzer?utm_source=chatgpt.com)

#### **Microsoft.VisualStudio.Threading.Analyzers** 
Best for async/concurrency issues: sync-over-async, deadlock-prone waits, bad `JoinableTaskFactory` usage, missing `ConfigureAwait` patterns.

#### **AsyncFixer** 
Focused async analyzer; useful for catching fire-and-forget, blocking waits, unawaited tasks, bad async patterns. A good async-analysis survey recommended AsyncFixer + VS Threading + Meziantou together. → [Cezary Piątek Blog](https://cezarypiatek.github.io/post/async-analyzers-summary)

#### **IDisposableAnalyzers** 
Best focused package for resource lifetime leaks: missing `Dispose`, incorrect ownership transfer, fields that should be disposed, `IDisposable` implementation mistakes.

#### **Built-in CA analyzers** 
Already included with modern .NET SDKs; CA rules cover quality, security, performance, design, and resource issues. 
Pay attention to:  `CA2000`, `CA2007`, `CA2012`, `CA2016`, `CA2202`, `CA2213`.

#### **SonarAnalyzer.CSharp** 
Broad code smell/security/bug analyzer. Good in CI, sometimes noisy.

#### **Roslynator.Analyzers** 
Useful cleanup/refactoring/best-practice rules, less specifically about concurrency or leaks.

For your stated goal — **concurrency + memory/resource leaks** — the core set is:

```xml
Meziantou.Analyzer
Microsoft.VisualStudio.Threading.Analyzers
AsyncFixer
IDisposableAnalyzers
SonarAnalyzer.CSharp
```

Use `.editorconfig` to ratchet severity gradually; don't turn everything into errors on day one.

<audio class="fixed start-0 bottom-0 hidden h-0 w-0" autoplay="" crossorigin="anonymous"></audio>
