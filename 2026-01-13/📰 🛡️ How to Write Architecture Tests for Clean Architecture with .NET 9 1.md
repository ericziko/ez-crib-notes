---
title: 📰 🛡️ How to Write Architecture Tests for Clean Architecture with .NET 9 1
source: https://medium.com/@mariammaurice/%EF%B8%8F-how-to-write-architecture-tests-for-clean-architecture-with-net-9-6da97ae8ca64
author:
  - "[[Mori]]"
published: 2025-10-14
created: 2026-01-08
description: 🛡️ How to Write Architecture Tests for Clean Architecture with .NET 9 Maintaining a Clean Architecture isn’t just about writing layered code; it’s about ensuring that those layers stay …
tags:
  - clippings
uid: b251c032-7858-4b71-bf82-9d5aab0d8945
updated: 2026-01-08T00:23
---

# 📰 🛡️ How to Write Architecture Tests for Clean Architecture with .NET 9 1

![](<_resources/📰 🛡️ How to Write Architecture Tests for Clean Architecture with .NET 9 1/8fea2b34f5ea3549fa070ce34b7c99d8_MD5.webp>)

> *Maintaining a Clean Architecture isn't just about writing layered code; it's about ensuring that those layers stay clean, that dependencies remain correct, and that architectural decisions survive over time. Architecture tests are your guardrails.*

## Table of Contents

1. Introduction: Why Architecture Tests Matter
2. Clean Architecture &.NET 9 — Layers & Dependency Rules
3. Types of Tests in Clean Architecture
4. ==Tools & Libraries (NetArchTest, ArchUnitNET, others)==
5. Designing Architecture Tests — What to Enforce
6. Sample Project Setup (.NET 9)
7. Writing Architecture Tests: Real Code Examples
8. Running Architecture Tests in CI/CD
9. Trade-offs, Pitfalls & Best Practices
10. Summary & Final Thoughts

## 1\. Introduction: Why Architecture Tests Matter

Software begins clean, neat, structured. But over time, with many contributors, shifting requirements, deadlines, technical debt, it tends to drift.

Problems that creep in:

- Inner layers (e.g. Domain) accidentally depend on outer layers (e.g. Infrastructure or Presentation).
- Repositories or ORM/file storage code leaks into domain layer.
- Business logic gets mixed with controllers or UI.
- Naming conventions break, dependencies are tangled.

Architecture tests are automated checks that codify your architectural rules (layering, dependency directions, naming, visibility) and enforce them continuously. So, when someone accidentally introduces a violation, the test suite fails, the issue is caught early, not after many merges or in production.

## 2\. Clean Architecture &.NET 9 — Layers & Dependency Rules

Before writing tests, you need a clear definition of your architecture. In most Clean Architecture / Onion / Hexagonal styles, your projects/layers look like:

- **Domain** (Core business logic, Entities, Value Objects, Domain Events)
- **Application** (Use Cases, Interfaces / Ports)
- **Infrastructure** (EF Core / persistence, external services, file/queue etc.)
- **Presentation / API / UI** (Controllers, Endpoints, GraphQL, etc.)

Dependency Rules (typical):

- Domain → *nothing else* (no references to Application, Infrastructure, or Presentation)
- Application → Domain, but *not* Infrastructure or Presentation (if following strict selection)
- Infrastructure → Application & Domain, but not vice versa
- Presentation → Application → Domain
- Naming conventions (e.g. repository classes end with "Repository", domain events end with "Event" or "DomainEvent", etc.)
- Classes in Domain often should be sealed / immutable (depends on your style)

Document your rules clearly. This becomes the baseline for your architecture tests

## 3\. Types of Tests in Clean Architecture

Let's classify tests and see where architecture tests fit:

![](<_resources/🛡️ How to Write Architecture Tests for Clean Architecture with .NET 9 1/f5396fdd9e27d790b8628a222426be46_MD5.webp>)

Architecture tests reduce drift, help keep the shape of your system correct as it grows, especially by many developers.

## 4\. Tools & Libraries

Here are tools that help write architecture tests in.NET:

- **NetArchTest.Rules** — very popular, expressive, allows you to assert dependencies, types, etc. [Code Maze+1](https://code-maze.com/csharp-architecture-tests-with-netarchtest-rules/?utm_source=chatgpt.com)
- **ArchUnitNET** — inspired by Java's ArchUnit, also provides type / dependency inspection. (Not always used as widely but very useful)
- Reflection / custom code — for bespoke rules if the existing tools don't cover your need.

When using NetArchTest, typical pattern: you load an assembly, select types with filters (namespace, class vs interface, name patterns), then assert certain dependency or naming rules. [EzzyLearning.net+1](https://www.ezzylearning.net/tutorial/maintain-clean-architecture-rules-with-architecture-tests?utm_source=chatgpt.com)

## 5\. Designing Architecture Tests — What to Enforce

Here are common rules / constraints to encode as architecture tests:

1. **Layering Constraints**
- Domain must not reference Application, Infrastructure, Presentation
- Application must not reference Infrastructure (depending on style)
- Presentation must depend only on Application (and possibly some Domain abstractions)

**2\. Dependency Direction**

- If Application depends on Infrastructure via interfaces only, ensure implementation details are in Infrastructure layer

**3\. Naming Conventions**

- Classes in certain directories / namespaces must follow naming — e.g. repository classes end with *Repository*
- Domain Events named *SomethingEvent* or *SomethingDomainEvent*

**4\. Visibility / Mutability Constraints**

- Domain classes being sealed (if your style calls for immutability)
- Entities with private parameterless constructors (for ORM)
- Avoid public setters in Entities / Value Objects unless required

**5\. Enforcing Interfaces / Abstractions**

- Infrastructure implementations must implement interfaces defined in Application / Domain
- No direct usage of concrete infrastructure classes in Application / Domain

**6\. Layer Boundaries in Namespaces / Projects**

- Ensuring namespaces / projects reflect layers and dependencies match

**7\. Avoid Certain Types in Domain**

- For example, no references to EF Core (DbContext, or EF attributes) in Domain layer

**8\. Ensure Dependency Injection Use**

- Controllers / Presentation should get dependencies via interfaces, not concrete types (can be tested superficially)

You should pick a subset you care about and that your team agrees on.

## 6\. Sample Project Setup (.NET 9)

To illustrate, let's assume a sample project structure:

```c
/src
   /Domain         → Domain project (Class Library)  
       Entities, ValueObjects, DomainEvents, Interfaces  
   /Application    → Use Cases, DTOs, Interfaces  
   /Infrastructure → EF Core, Repositories, external services  
   /WebApi         → ASP.NET Core Web API, Controllers  
/tests
   /ArchitectureTests → separate test project for architecture tests  
   /DomainTests       → unit tests for domain logic  
   /ApplicationTests  → unit tests for use case logic  
   /IntegrationTests  → tests involving database, etc.
```

Dependencies:

- Domain project has no dependencies on Application, Infrastructure, WebApi
- Application references Domain
- Infrastructure references Application and Domain
- WebApi references Application

Also, might include a shared Kernel or common abstractions project depending.

## 7\. Writing Architecture Tests: Real Code Examples

Here are concrete examples using **NetArchTest.Rules** in. NET 9.

## 7.1 Setup

In your test solution, add a project called `MyApp.ArchitectureTests` (or similar). Add reference to test projects, and add NuGet:

```c
dotnet add MyApp.ArchitectureTests package NetArchTest.Rules
dotnet add MyApp.ArchitectureTests package FluentAssertions  # for assertions, optional
```

You'll need to reference the assemblies you want to test, e.g.:

```c
<ItemGroup>
  <ProjectReference Include="..\src\Domain\Domain.csproj" />
  <ProjectReference Include="..\src\Application\Application.csproj" />
  <ProjectReference Include="..\src\Infrastructure\Infrastructure.csproj" />
  <ProjectReference Include="..\src\WebApi\WebApi.csproj" />
</ItemGroup>
```

## 7.2 Example: Domain Does Not Depend On Other Layers

```c
using NetArchTest.Rules;
using System.Reflection;
using Xunit;
using FluentAssertions;
public class LayeringTests
{
    private readonly Assembly _domainAssembly = typeof(SomeDomainEntity).Assembly;
    private readonly Assembly _applicationAssembly = typeof(SomeApplicationService).Assembly;
    private readonly Assembly _infrastructureAssembly = typeof(SomeRepositoryImplementation).Assembly;
    [Fact]
    public void Domain_Layer_Should_Not_Have_Dependency_On_Application_Or_Infrastructure()
    {
        // Arrange
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn(_applicationAssembly.GetName().Name)
            .AndNot()
            .HaveDependencyOn(_infrastructureAssembly.GetName().Name)
            .GetResult();
        // Assert
        result.IsSuccessful.Should().BeTrue("Domain layer should not depend on Application or Infrastructure layers");
    }
}
```

## 7.3 Example: Application Layer Should Not Depend on Infrastructure Layer Directly

```c
[Fact]
public void Application_Layer_Should_Not_Have_Dependency_On_Infrastructure()
{
    var result = Types.InAssembly(_applicationAssembly)
        .ShouldNot()
        .HaveDependencyOn(_infrastructureAssembly.GetName().Name)
        .GetResult();
result.IsSuccessful.Should().BeTrue("Application layer should should not depend on Infrastructure directly");
}
```

## 7.4 Example: Repository Naming Convention

```c
[Fact]
public void Repository_Implementations_Should_End_With_Repository()
{
    var infraTypes = Types
        .InAssembly(_infrastructureAssembly)
        .That()
        .ResideInNamespace("MyApp.Infrastructure.Repositories")
        .And()
        .AreClasses()
        .GetTypes();
foreach(var t in infraTypes)
    {
        t.Name.Should().EndWith("Repository", $"Class {t.Name} should be named with *Repository suffix");
    }
}
```

Alternatively using NetArchTest:

```c
[Fact]
public void RepositoryClasses_Should_Have_NameEndingWith_Repository()
{
    var result = Types.InAssembly(_infrastructureAssembly)
        .That()
        .ResideInNamespace("MyApp.Infrastructure.Repositories")
        .And()
        .AreClasses()
        .Should()
        .HaveNameEndingWith("Repository")
        .GetResult();
result.IsSuccessful.Should().BeTrue();
}
```

## 7.5 Example: Domain Events Should Be Sealed / Immutable

```c
[Fact]
public void DomainEvents_Should_Be_Sealed()
{
    var domainEvents = Types.InAssembly(_domainAssembly)
        .That()
        .HaveNameEndingWith("Event")
        .And()
        .AreClasses()
        .GetResult();
foreach(var t in domainEvents.Types)
    {
        t.IsSealed.Should().BeTrue($"{t.FullName} should be sealed to enforce immutability / design style");
    }
}
```

## 7.6 Example: No Public Setter in Domain Entities

You might want to enforce that domain entity properties do not have public setters (depending on your style):

```c
[Fact]
public void Domain_Entities_Should_Not_Have_Public_Setters()
{
    var entityTypes = Types.InAssembly(_domainAssembly)
        .That()
        .ResideInNamespace("MyApp.Domain.Entities")
        .GetTypes();
foreach(var t in entityTypes)
    {
        var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod != null && p.SetMethod.IsPublic);
        props.Should().BeEmpty($"{t.Name} should not expose public setters on its properties");
    }
}
```

## 7.7 Example: Controllers Should Use Application Layer, Not Domain Directly

```c
[Fact]
public void Controllers_Should_Not_Depend_On_Domain_Entities_Directly()
{
    var webApiAssembly = typeof(SomeController).Assembly;
    var domainAssemblyName = _domainAssembly.GetName().Name;
var result = Types.InAssembly(webApiAssembly)
        .That()
        .ResideInNamespace("MyApp.WebApi.Controllers")
        .ShouldNot()
        .HaveDependencyOn(domainAssemblyName)
        .GetResult();
    result.IsSuccessful.Should().BeTrue("Controllers should go through Application layer (DTOs, interfaces), not depend directly on domain types");
}
```

## 8\. Running Architecture Tests in CI/CD

To make architecture tests effective:

- Add them to your build pipeline (GitHub Actions, Azure DevOps, etc.). If they fail, block the merge.
- Make them fast! Because they're structural, they generally run quickly (reflection-based, no DB).
- Keep naming and dependency rules stable; changing them should be a conscious decision. When you change architectural decisions, update the tests accordingly.
- Include architecture test coverage in your code quality metrics.

Sample GitHub Actions snippet:

```c
name: CI
on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]
jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET 9
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.x'
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Run unit tests
        run: dotnet test --no-restore --verbosity normal
      - name: Run architecture tests
        run: dotnet test --filter Category=Architecture --no-restore --verbosity normal
```

You may tag your architecture test project or tests with custom categories (e.g. `[Trait("Category","Architecture")]`) so you can separately run or filter them.

## 9\. Trade-offs, Pitfalls & Best Practices

Writing architecture tests adds value, but also comes with caveats.

## Trade-offs / Costs

- Upfront work: you need to define rules, write tests, maintain them when architecture evolves.
- False positives / brittle rules: Overly strict naming or visibility rules might irritate developers or block valid design changes.
- Maintenance overhead: When styles or decisions change, many tests might need updating.

## Common Pitfalls

- Writing too many rules too early; some rules are premature optimization.
- Overconstraining minor parts of the system causing friction.
- Not keeping the tests fast; if architecture tests are slow or flaky, people will disable or ignore them.
- Mixing behavior tests with architecture tests (keep separation).

## Best Practices

- Start with a small core set of architectural rules (layer dependencies, no domain → external).
- Use tools like NetArchTest to express rules clearly.
- Tag architecture tests so they are clearly differentiated.
- Run them often (CI).
- Document your architectural decisions; keep the rules versioned.
- When architecture changes legitimately, update tests and communicate the change.
- Let the team agree on conventions (naming, layering) so that tests reflect shared understanding, not arbitrary rules.

## 10\. Summary & Final Thoughts

Architecture tests are a powerful tool to preserve the integrity of a Clean Architecture codebase over time. They codify design constraints, guard against architectural erosion, and help new team members obey the rules without having to read all the documentation.

Here are the key takeaways:

- Clean Architecture is about layering & dependency direction; architecture tests enforce that.
- Use tools like NetArchTest.Rules in.NET 9 to write reflection-based tests over assemblies.
- Pick the rules that matter for your project (layering, naming, visibility, dependencies).
- Integrate architecture tests into CI/CD.
- Expect that architecture evolves; the tests and rules will need maintenance, not just code.

If you build architecture tests early (or integrate them soon), they pay off massively in reducing technical debt and keeping structure clean as the team grows.

## ✨ Key Takeaways

- ✅ Architecture tests ensure your code structure stays clean.
- 🔄 Tools like **NetArchTest** make them easy to write and maintain.
- 🧠 Focus on *boundaries*, not every detail.
- 🚀 Add them to your CI pipeline for real protection.
- 💪 Let automation guard your architectural intent.

✨ Finding life lessons in lines of code. I write about debugging our thoughts and refactoring our habits for a better life. Let's grow together.
