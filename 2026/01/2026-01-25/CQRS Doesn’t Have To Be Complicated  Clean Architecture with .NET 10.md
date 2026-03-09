---
title: CQRS Doesn't Have To Be Complicated  Clean Architecture with .NET 10
source: https://medium.com/codetodeploy/cqrs-doesnt-have-to-be-complicated-clean-architecture-with-net-10-f418f051e3c6
author:
  - "[[Mori]]"
published: 2025-12-14
created: 2026-01-08
description: "CQRS Doesn’t Have To Be Complicated | Clean Architecture with .NET 10 Part 1 — Killing the Myth: CQRS Is Simpler Than You Think 🚀 Top Remote Tech Jobs — $50–$120/hr 🔥 Multiple Roles …"
tags:
  - clippings
updated: 2026-01-08T00:16
aliases:
  - CQRS Doesn't Have To Be Complicated  Clean Architecture with .NET 10
linter-yaml-title-alias: CQRS Doesn't Have To Be Complicated  Clean Architecture with .NET 10
---

# CQRS Doesn't Have To Be Complicated  Clean Architecture with .NET 10
[Sitemap](https://medium.com/sitemap/sitemap.xml)## [CodeToDeploy](https://medium.com/codetodeploy?source=post_page---publication_nav-c8b549b355f4-f418f051e3c6---------------------------------------)

[![CodeToDeploy](https://miro.medium.com/v2/resize:fill:76:76/1*UWhS0KcM5TWFNTMWebBy7Q.png)](https://medium.com/codetodeploy?source=post_page---post_publication_sidebar-c8b549b355f4-f418f051e3c6---------------------------------------)

The First Publication That Blends Tech Insights + Real Job Opportunities

![](<../../../../../../Clippings/_resources/CQRS Doesn’t Have To Be Complicated  Clean Architecture with .NET 10/40957482cc096e05177d16c0e0f3eea6_MD5.webp>)

## 🚀 Top Remote Tech Jobs — $50–$120/hr

🔥 *Multiple Roles Open*  
Hiring E **xperienced Talent (3+ years)** Only.

- Frontend / Backend / Full Stack
- Mobile (iOS/Android)
- AI / ML
- DevOps & Cloud

⏳ **Opportunities Fill FAST — Early Applicants Get Priority!**  
👉 [**Apply Here**](https://app.usebraintrust.com/r/code6/)

*"CQRS has a reputation problem.  
Not because it's hard — but because people overengineer it."*

If you've ever searched for CQRS tutorials, you probably found:

- 10 layers of abstractions
- 20 folders with unclear responsibilities
- "Event Sourcing" forced into everything
- Complex messaging infrastructure

And you probably thought:

## "This feels like overkill."

You were right.

**CQRS is not complicated.**  
People make it complicated.

This 3-part series will show you how to implement **real CQRS inside Clean Architecture using.NET 10**, in a way that is:

✅ Simple  
✅ Testable  
✅ Fast  
✅ Scalable  
✅ Production-ready

No magic.  
No ceremony.  
No unnecessary abstractions.

# ✅ What You'll Build in This Series

By the end, you'll have:

- A real **Clean Architecture solution**
- Commands and Queries done properly
- No overengineering
- No fake patterns
- A scalable structure
- .NET 10 compatible code
- MediatR done right
- Production-ready examples

# 🧩 PART 1 — Understanding CQRS Without the Nonsense

In this part, you'll learn:

1. What CQRS actually is (and isn't)
2. Why people overcomplicate it
3. How it fits naturally into Clean Architecture
4. How to structure a real project
5. Create your first Command & Query

# 1\. What CQRS Really Is (In One Sentence)

CQRS means:

## Commands change state. Queries read state. They are not mixed

That's it.

Not "event sourcing".  
Not "message brokers".  
Not "complex projections".

Just a clean separation.

Example:

```c
CreateOrderCommand → writes
GetOrderByIdQuery   → reads
```

# 2\. What CQRS Is NOT

CQRS is **not**:

❌ Microservices  
❌ Event Sourcing  
❌ RabbitMQ  
❌ Kafka  
❌ Redis Streams

Those can be added later, but they are **not CQRS**.

CQRS is just separation of reads and writes.

# 3\. Why Clean Architecture and CQRS Fit Naturally

Clean Architecture gives structure.

CQRS gives flow.

When combined:

```c
Domain → core business rules
Application → Commands + Queries
Infrastructure → DB, external services
API → thin controllers
```

Each layer has a **single responsibility**.

# 4\. Project Setup (.NET 10 + Clean Structure)

Create solution:

```c
dotnet new sln -n SimpleCqrs
mkdir src
cd src
dotnet new classlib -n SimpleCqrs.Domain
dotnet new classlib -n SimpleCqrs.Application
dotnet new classlib -n SimpleCqrs.Infrastructure
dotnet new webapi -n SimpleCqrs.Api
cd ..
dotnet sln add src/**/*
```

Add references:

```c
cd src/SimpleCqrs.Application
dotnet add reference ../SimpleCqrs.Domain
cd ../SimpleCqrs.Infrastructure
dotnet add reference ../SimpleCqrs.Application
dotnet add reference ../SimpleCqrs.Domain
cd ../SimpleCqrs.Api
dotnet add reference ../SimpleCqrs.Application
dotnet add reference ../SimpleCqrs.Infrastructure
```

# 5\. Domain Layer (Business Rules Only)

Create entity:

`Domain/Entities/TaskItem.cs`

```c
namespace SimpleCqrs.Domain.Entities;
public class TaskItem
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public bool IsCompleted { get; private set; }
    private TaskItem() { }
    public TaskItem(string title)
    {
        Id = Guid.NewGuid();
        Title = title;
        IsCompleted = false;
    }
    public void Complete()
    {
        IsCompleted = true;
    }
}
```

✅ No EF  
✅ No JSON  
✅ No Controller logic

# 6\. Application Layer Structure

Create folders:

```c
Application/
  /Commands
  /Queries
  /DTOs
  /Interfaces
```

# 7\. Your First Command

`Application/Commands/CreateTask/CreateTaskCommand.cs`

```c
using MediatR;
public record CreateTaskCommand(string Title) : IRequest<Guid>;
```

Command Handler:

```c
using MediatR;
using SimpleCqrs.Application.Interfaces;
using SimpleCqrs.Domain.Entities;
public class CreateTaskHandler : IRequestHandler<CreateTaskCommand, Guid>
{
    private readonly ITaskRepository _repo;
    public CreateTaskHandler(ITaskRepository repo)
    {
        _repo = repo;
    }
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var task = new TaskItem(request.Title);
        await _repo.AddAsync(task);
        return task.Id;
    }
}
```

# 8\. Your First Query

DTO:

```c
public record TaskDto(Guid Id, string Title, bool IsCompleted);
```

Query:

```c
using MediatR;
public record GetTaskByIdQuery(Guid Id) : IRequest<TaskDto?>;
```

Handler:

```c
using MediatR;
using SimpleCqrs.Application.Interfaces;
public class GetTaskByIdHandler : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    private readonly ITaskReadRepository _repo;
    public GetTaskByIdHandler(ITaskReadRepository repo)
    {
        _repo = repo;
    }
    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken ct)
    {
        return await _repo.GetByIdAsync(request.Id);
    }
}
```

# 9\. Infrastructure Interfaces (Keep It Simple)

```c
public interface ITaskRepository
{
    Task AddAsync(TaskItem task);
}
public interface ITaskReadRepository
{
    Task<TaskDto?> GetByIdAsync(Guid id);
}
```

✅ Separate read and write models  
✅ No overengineering

# ✅ PART 1 SUMMARY

You now have:

✅ Clear CQRS mental model  
✅ Simple Clean Architecture structure  
✅ A real Command  
✅ A real Query  
✅ No complexity  
✅ No ceremony  
✅ No bloat

CQRS is now **simple and logical**.

# 👉 PART 2 PREVIEW

In **Part 2**, you'll build:

✅ EF Core implementation  
✅ Migrations  
✅ API Controllers  
✅ Validation pipeline  
✅ Error handling  
✅ Real-world project layout

# CQRS Doesn't Have To Be Complicated | Clean Architecture with.NET 10

## Part 2 — Simple EF Core, Clean Persistence, Minimal APIs, and Real CQRS in Action

![](<../../../../../../Clippings/_resources/CQRS Doesn’t Have To Be Complicated  Clean Architecture with .NET 10/6d8005874b2044af42f5b94d67edd012_MD5.webp>)

## "CQRS only feels heavy when your infrastructure is heavy."

In **Part 1**, you built:

✅ Clean Architecture structure  
✅ Domain model  
✅ Commands & Queries  
✅ No overengineering

Now in **Part 2**, you'll make everything **real and executable**:

You will build:

✅ EF Core persistence  
✅ Real repositories  
✅ Database migrations  
✅ Clean CQRS APIs  
✅ Validation behavior  
✅ Minimal APIs & controllers

# 🔥 PART 2 ROADMAP

We'll cover:

1. EF Core integration the *clean* way
2. Write model vs read model separation
3. Real repository implementations
4. MediatR wiring
5. Migrations
6. Validation pipeline
7. Minimal API endpoints

# 1\. Install Required Packages

Inside **Infrastructure** project:

```c
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Inside **API** project:

```c
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation.AspNetCore
```

# 2\. Create DbContext

`Infrastructure/Persistence/AppDbContext.cs`

```c
using Microsoft.EntityFrameworkCore;
using SimpleCqrs.Domain.Entities;
namespace SimpleCqrs.Infrastructure.Persistence;
public class AppDbContext : DbContext
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
```

# 3\. Write Model Repository Implementation

`Infrastructure/Repositories/TaskRepository.cs`

```c
using SimpleCqrs.Application.Interfaces;
using SimpleCqrs.Domain.Entities;
using SimpleCqrs.Infrastructure.Persistence;
public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _db;
    public TaskRepository(AppDbContext db)
    {
        _db = db;
    }
    public async Task AddAsync(TaskItem task)
    {
        await _db.Tasks.AddAsync(task);
        await _db.SaveChangesAsync();
    }
}
```

# 4\. Read Model Repository (Projections Only)

```c
using Microsoft.EntityFrameworkCore;
using SimpleCqrs.Application.Interfaces;
using SimpleCqrs.Application.DTOs;
using SimpleCqrs.Infrastructure.Persistence;
public class TaskReadRepository : ITaskReadRepository
{
    private readonly AppDbContext _db;
    public TaskReadRepository(AppDbContext db)
    {
        _db = db;
    }
    public async Task<TaskDto?> GetByIdAsync(Guid id)
    {
        return await _db.Tasks
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TaskDto(x.Id, x.Title, x.IsCompleted))
            .FirstOrDefaultAsync();
    }
}
```

✅ Clean projection  
✅ No entity leaking  
✅ Fast SQL

# 5\. Register Services in Program.cs

```c
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskReadRepository, TaskReadRepository>();
builder.Services.AddMediatR(typeof(CreateTaskCommand).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskCommand>();
```

# 6\. Database Migrations

Run:

```c
dotnet ef migrations add InitialCreate -p ../SimpleCqrs.Infrastructure -s ../SimpleCqrs.Api
dotnet ef database update -p ../SimpleCqrs.Infrastructure -s ../SimpleCqrs.Api
```

Your table:

```c
Tasks
 - Id
 - Title
 - IsCompleted
```

# 7\. Validation (Without Complexity)

Install FluentValidation:

```c
dotnet add package FluentValidation
```

Validator:

```c
using FluentValidation;
public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

Pipeline Behavior:

```c
using MediatR;
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();
        if (failures.Any())
            throw new ValidationException(failures);
        return await next();
    }
}
```

Register:

```c
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

# 8\. Minimal API Endpoints (CQRS without controllers)

```c
app.MapPost("/tasks", async (CreateTaskCommand cmd, IMediator mediator) =>
{
    var id = await mediator.Send(cmd);
    return Results.Ok(id);
});
app.MapGet("/tasks/{id}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetTaskByIdQuery(id));
    return result is null ? Results.NotFound() : Results.Ok(result);
});
```

✅ Clean  
✅ Minimal  
✅ No bloated controllers

# ✅ Part 2 Summary

You now have:

✅ Real database persistence  
✅ Separate write/read repositories  
✅ CQRS handlers fully functional  
✅ Minimal APIs  
✅ Validation pipeline  
✅ EF migrations

CQRS is still **clean and simple**.

# 👉 PART 3 PREVIEW

In Part 3, we'll build:

✅ Domain events  
✅ Logging pipeline  
✅ Transaction behavior  
✅ Performance best practices  
✅ Testing strategy  
✅ Pro Tips  
✅ Final conclusion

# CQRS Doesn't Have To Be Complicated | Clean Architecture with.NET 10

## 🔥 Part 3 — Domain Events, Logging Pipelines, Transactions, Performance & Production-Ready CQRS

![](<../../../../../../Clippings/_resources/CQRS Doesn’t Have To Be Complicated  Clean Architecture with .NET 10/3954c9d508ac97ba31336d4cabb3f0cc_MD5.webp>)

## "Simple CQRS is good. Production-ready CQRS is great."

In **Part 1**, you understood CQRS without the nonsense.  
In **Part 2**, you built a real working system with EF Core, minimal APIs, and validation.

Now in **Part 3**, we'll make this a **real-world, enterprise-grade CQRS system** — without making it complicated.

This is where CQRS becomes **powerful**, not complex.

# 🔥 PART 3 ROADMAP

You will implement:

✅ Domain Events (simple, not overengineered)  
✅ Logging Pipeline (cross-cutting concern)  
✅ Transaction Pipeline  
✅ Performance best practices  
✅ Testing strategy  
✅ Production structure  
✅ Pro Tips  
✅ Final Conclusion

# 1️⃣ Simple Domain Events (Without Overengineering)

Domain events let **your domain talk** without coupling it to infrastructure.

# Create a base event

**Domain/Events/IDomainEvent.cs**

```c
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
```

**Domain/Events/TaskCreatedEvent.cs**

```c
public class TaskCreatedEvent : IDomainEvent
{
    public Guid TaskId { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
public TaskCreatedEvent(Guid taskId)
    {
        TaskId = taskId;
    }
}
```

# 2️⃣ Raise Events Inside the Entity

Update your Domain entity:

```c
private readonly List<IDomainEvent> _domainEvents = new();
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
private void AddDomainEvent(IDomainEvent domainEvent)
{
    _domainEvents.Add(domainEvent);
}
public TaskItem(string title)
{
    Id = Guid.NewGuid();
    Title = title;
    IsCompleted = false;
    AddDomainEvent(new TaskCreatedEvent(Id));
}
```

No magic.  
Just plain, clean code.

# 3️⃣ Dispatch Domain Events Automatically

Create dispatcher:

**Application/Interfaces/IDomainEventDispatcher.cs**

```c
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
}
```

Implementation (Infrastructure):

```c
using MediatR;
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }
    public Task DispatchAsync(IDomainEvent domainEvent)
    {
        return _mediator.Publish(domainEvent);
    }
}
```

# 4️⃣ Hook Events into EF Core SaveChanges

Modify your **AppDbContext**:

```c
public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    var entities = ChangeTracker
        .Entries<TaskItem>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();
var events = entities.SelectMany(e => e.DomainEvents).ToList();
    var result = await base.SaveChangesAsync(cancellationToken);
    foreach (var domainEvent in events)
        await _dispatcher.DispatchAsync(domainEvent);
    return result;
}
```

✅ Still clean  
✅ Still simple  
✅ No complex infrastructure

# 5️⃣ Logging Pipeline (Cross-Cutting Without Ugly Code)

CQRS shines when you move cross-cutting concerns **out of handlers**.

Create behavior:

```c
using MediatR;
using Microsoft.Extensions.Logging;
public class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

Register in DI:

```c
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

# 6️⃣ Transaction Pipeline (No Transaction Code in Handlers)

Instead of this:

❌ `using var tx = ...` in handlers

We centralize it:

```c
public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly AppDbContext _db;
public TransactionBehavior(AppDbContext db)
    {
        _db = db;
    }
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(cancellationToken);
        var response = await next();
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
```

Register:

```c
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
```

✅ Every command becomes atomic  
✅ No duplicated transaction logic

# 7️⃣ Performance Best Practices (Simple Wins)

# Queries

Always:

```c
.AsNoTracking()
.Select(...)
.FirstOrDefaultAsync()
```

# Commands

- Keep handlers small
- Avoid multiple `SaveChanges()`
- Prefer bulk operations when possible

# Indexing

Add database indexes:

```c
CREATE INDEX IX_Tasks_Title ON Tasks(Title);
```

# Async Everywhere

Never:

```c
.Result
.Wait()
```

Always:

```c
await
```

# 8️⃣ Testing Strategy (That's Actually Practical)

# Unit Tests → Handlers

Test business logic without EF:

```c
[Fact]
public async Task CreateTask_ShouldReturnId()
{
    // Arrange mocks
    // Act handler
    // Assert outcome
}
```

# Integration Tests → Infrastructure

Spin up real DB (or SQLite in-memory):

```c
builder.UseSqlite("DataSource=:memory:");
```

CQRS makes this **very easy**.

# 9️⃣ Real-World Folder Structure

Your final structure:

```c
/Domain
   /Entities
   /Events
/Application
   /Commands
   /Queries
   /Behaviors
   /Interfaces
/Infrastructure
   /Persistence
   /Repositories
/API
   /Endpoints
```

Simple. Scalable. Maintainable.

# 🔥 PRO TIPS (Senior-Level CQRS Advice)

✅ CQRS is about clarity, not ceremony  
✅ Don't mix reads & writes  
✅ Keep handlers small  
✅ Use pipeline behaviors for everything cross-cutting  
✅ Use domain events only when business requires it  
✅ Avoid event sourcing unless you truly need it  
✅ Make queries projection-based for performance  
✅ Keep your API layer thin

# ✅ Conclusion

You now understand **real CQRS**.

Not the blog-theory version.  
Not the overengineered version.

You built:

✅ Simple and clean Commands  
✅ Simple and clean Queries  
✅ EF Core persistence  
✅ Validation & logging pipelines  
✅ Domain events  
✅ Transaction management  
✅ Production-grade architecture

CQRS turns out to be **simple when done right**.

# 🎯 Final Thoughts

CQRS is not about:

❌ More layers  
❌ More complexity  
❌ More buzzwords

It's about:

✅ Clarity  
✅ Separation  
✅ Intent  
✅ Maintainability

When combined with **Clean Architecture and.NET 10**, CQRS becomes a **powerful but simple tool** that scales with your system without destroying your sanity.

# Thank you for being a part of the community

*Before you go:*

![](<../../../../../../Clippings/_resources/CQRS Doesn’t Have To Be Complicated  Clean Architecture with .NET 10/9d04c7acb314be0ef9e2e579d9c91a98_MD5.webp>)

👉 Be sure to **clap** and **follow** the writer ️👏 **️️**

👉 Follow us: [**X**](https://x.com/Bhuwanchet67277) | [**Medium**](https://medium.com/codetodeploy)

👉 CodeToDeploy Tech Community is live on Discord — [**Join now!**](https://discord.gg/ZpwhHq6D)

👉 **Follow our publication,** [**CodeToDeploy**](https://medium.com/codetodeploy)

**Note:** This Post may contain affiliate links.

[![CodeToDeploy](<../../../../../../Clippings/_resources/CQRS Doesn’t Have To Be Complicated  Clean Architecture with .NET 10/0e4f77ae928c89e8c33630972bf7f707_MD5.png>)](https://medium.com/codetodeploy?source=post_page---post_publication_info--f418f051e3c6---------------------------------------)

[![CodeToDeploy](<../../../../../../Clippings/_resources/CQRS Doesn’t Have To Be Complicated  Clean Architecture with .NET 10/dc8ea158cac8ac678c078c314a11f53b_MD5.png>)](https://medium.com/codetodeploy?source=post_page---post_publication_info--f418f051e3c6---------------------------------------)

[Last published 5 hours ago](https://medium.com/codetodeploy/from-electrical-engineering-to-ai-hamayl-shahs-journey-to-simplifying-complex-tech-9a8420ed987a?source=post_page---post_publication_info--f418f051e3c6---------------------------------------)

The First Publication That Blends Tech Insights + Real Job Opportunities

✨ Finding life lessons in lines of code. I write about debugging our thoughts and refactoring our habits for a better life. Let's grow together.

# More from Mori and CodeToDeploy

# Recommended from Medium

[

See more recommendations

](<https://medium.com/?source=post_page---read_next_recirc--f418f051e3c6--------------------------------------->)w
