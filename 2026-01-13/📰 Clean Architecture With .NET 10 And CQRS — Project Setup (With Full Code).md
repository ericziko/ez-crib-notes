---
title: 📰 Clean Architecture With .NET 10 And CQRS — Project Setup (With Full Code)
source: https://medium.com/codetodeploy/clean-architecture-with-net-10-and-cqrs-project-setup-with-full-code-cd4c0020e9c2
author:
  - "[[Mori]]"
published: 2025-12-09
created: 2026-01-07
description: Clean Architecture With .NET 10 And CQRS — Project Setup (With Full Code) Part 1 — Why Clean Architecture + CQRS Wins & Bootstrapping a Real-World Solution 🚀 Top Remote Tech Jobs — …
tags:
  - clippings
updated: 2026-01-07T23:04
---

# 📰 Clean Architecture With .NET 10 And CQRS — Project Setup (With Full Code)

![](<_resources/📰 Clean Architecture With .NET 10 And CQRS — Project Setup (With Full Code)/c65825385ae5e5390ea15a0d370f2803_MD5.webp>)

## "Most projects don't fail because of bad code. They fail because of bad structure."

If you've built even a few.NET backend systems, you've probably felt this pain:

- Controllers getting fat
- Business logic leaking everywhere
- DbContext injected into UI
- Copy-paste queries
- No clear boundaries
- Zero testability

This series fixes that.

This is not a toy tutorial.  
This is how **real production systems** are structured.

In this 3-Part series you'll build:

✅ Clean Architecture solution  
✅ CQRS implemented correctly  
✅.NET 10 setup  
✅ EF Core  
✅ MediatR  
✅ Validation  
✅ Folder structure that scales to **100+ features**  
✅ Real code (not pseudo-code)

# PART 1 — Foundation & Project Setup

This part covers:

1. Why Clean Architecture + CQRS matters
2. Why.NET 10 changes the game
3. Folder structure used by real teams
4. Creating the solution
5. Wiring MediatR
6. Initial CQRS pipeline
7. Dependency injection the right way

# 1\. What Clean Architecture Actually Is

Clean Architecture forces your application to respect **boundaries**.

Think of it like this:

```c
Domain       →  Core business rules
Application  →  Use cases / orchestration
Infrastructure → DB, Email, Redis, External APIs
API          →  HTTP layer only
```

**Rule:**  
Inner layers can't depend on outer layers.

✅ Domain knows nothing about EF Core  
✅ Application knows nothing about ASP.NET  
✅ Infrastructure depends on Application  
✅ API depends on Application only

This is the foundation.

# 2\. What CQRS Actually Is (In Practice)

CQRS =  
**Command Query Responsibility Segregation**

Meaning:

- **Commands change state**
- **Queries only read data**

They are NOT mixed.

Example:

```c
CreateOrderCommand  → writes
GetOrderByIdQuery   → reads
```

Each has:

- Its own model
- Its own handler
- Its own validation
- Its own projection

You do **not** return domain entities.

# 3\. Creating the Solution (.NET 10)

```c
dotnet new sln -n CleanArchitectureDemo
```

Now create projects:

```c
dotnet new classlib -n CleanArchitectureDemo.Domain
dotnet new classlib -n CleanArchitectureDemo.Application
dotnet new classlib -n CleanArchitectureDemo.Infrastructure
dotnet new webapi   -n CleanArchitectureDemo.Api
```

Add to solution:

```c
dotnet sln add **/*.csproj
```

Add references:

```c
dotnet add CleanArchitectureDemo.Application reference CleanArchitectureDemo.Domain
dotnet add CleanArchitectureDemo.Infrastructure reference CleanArchitectureDemo.Application
dotnet add CleanArchitectureDemo.Api reference CleanArchitectureDemo.Application
dotnet add CleanArchitectureDemo.Api reference CleanArchitectureDemo.Infrastructure
```

# 4\. Folder Structure (Battle-Tested)

Inside **Application**:

```c
/Application
  /Common
  /Abstractions
  /Behaviors
  /Commands
  /Queries
  /DTOs
  /Interfaces
```

Inside **Domain**:

```c
/Domain
  /Entities
  /ValueObjects
  /Events
```

Inside **Infrastructure**:

```c
/Infrastructure
  /Persistence
  /Messaging
  /Services
```

# 5\. Install Core Packages

In `Application` project:

```c
dotnet add package MediatR
dotnet add package FluentValidation
```

In `Infrastructure`:

```c
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

In `API`:

```c
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
```

# 6\. Domain Layer (Pure Business Objects)

`Domain/Entities/User.cs`

```c
namespace CleanArchitectureDemo.Domain.Entities;
public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    private User() {}
    public User(string email)
    {
        Id = Guid.NewGuid();
        Email = email;
    }
}
```

No EF  
No controllers  
No JSON

Pure domain.

# 7\. Application Layer (CQRS Starts)

# Command

`Application/Commands/CreateUser/CreateUserCommand.cs`

```c
using MediatR;
public record CreateUserCommand(string Email) : IRequest<Guid>;
```

# Handler

```c
public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IAppDbContext _db;
public CreateUserHandler(IAppDbContext db)
    {
        _db = db;
    }
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var user = new User(request.Email);
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return user.Id;
    }
}
```

# 8\. Persistence Abstraction

`Application/Interfaces/IAppDbContext.cs`

```c
using Microsoft.EntityFrameworkCore;
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

# 9\. Infrastructure Layer: EF Core Setup

`Infrastructure/Persistence/AppDbContext.cs`

```c
using Microsoft.EntityFrameworkCore;
public class AppDbContext : DbContext, IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }
}
```

# 10\. API Layer — Clean Setup

`Program.cs`

```c
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddScoped<IAppDbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(typeof(CreateUserCommand).Assembly));
```

# Clean Architecture With.NET 10 And CQRS — Project Setup (With Full Code)

## Part 2 — Queries, Validation, Pipelines, Transactions, and Real-World CQRS Patterns

## "Most teams think they're doing CQRS. In reality, they're just doing CRUD with fancy names."

In **Part 1**, you built:

✅ A real Clean Architecture structure  
✅ Proper layer separation  
✅ MediatR setup  
✅ Command handling  
✅ EF Core in Infrastructure  
✅ Thin API layer

# 👉In Part 2, we go deeper and more realistic

You will build:

✅ Real **Query models & DTO projections**  
✅ **Validation pipeline** with FluentValidation  
✅ **Transaction pipeline behavior**  
✅ Centralized **exception handling pattern**  
✅ Real-world mapping strategies  
✅ CQRS the way senior engineers build it

# 🔥 PART 2 ROADMAP

This part will cover:

1. Why Queries are NOT Entities
2. DTO Projections (the right way)
3. FluentValidation in CQRS
4. Validation Pipeline Behavior
5. Transaction Pipeline Behavior
6. Exception Handling Behavior
7. Real-world Query Examples
8. Performance patterns

# 1️⃣ Why Queries Should NEVER Return Entities

Your query should NOT return:

```c
User
```

Because:

- It couples your API to your Domain
- It exposes internal structure
- It breaks schema evolution
- It kills performance flexibility

Instead, you return **Projections / DTOs**.

# 2️⃣ Create DTOs (Read Models)

`Application/DTOs/UserDto.cs`

```c
namespace CleanArchitectureDemo.Application.DTOs;
public record UserDto(
    Guid Id,
    string Email
);
```

# 3️⃣ Create Queries (CQRS Read Side)

`Application/Queries/GetUserById/GetUserByIdQuery.cs`

```c
using MediatR;
using CleanArchitectureDemo.Application.DTOs;
public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;
```

# 4️⃣ Query Handler With Projection

```c
using Microsoft.EntityFrameworkCore;
public class GetUserByIdHandler 
    : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IAppDbContext _db;
    public GetUserByIdHandler(IAppDbContext db)
    {
        _db = db;
    }
    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        return await _db.Users
            .Where(x => x.Id == request.Id)
            .Select(x => new UserDto(x.Id, x.Email))
            .FirstOrDefaultAsync(ct);
    }
}
```

✅ No domain entity leakage  
✅ Pure projection  
✅ Very fast SQL

# 5️⃣ Add FluentValidation

Install in Application:

```c
dotnet add package FluentValidation.DependencyInjectionExtensions
```

# Create Validator

`Application/Commands/CreateUser/CreateUserValidator.cs`

```c
using FluentValidation;
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
```

# 6️⃣ Validation Pipeline Behavior

This applies automatically to every command/query.

`Application/Behaviors/ValidationBehavior.cs`

```c
using MediatR;
using FluentValidation;
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

# Register Pipelines

API `Program.cs`:

```c
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(typeof(CreateUserCommand).Assembly);
});
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

Now your CQRS layer **auto-validates**.

# 7️⃣ Transaction Pipeline Behavior

Instead of starting transactions inside handlers (❌),  
we centralize them.

`Application/Behaviors/TransactionBehavior.cs`

```c
using MediatR;
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
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        var response = await next();
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return response;
    }
}
```

Register:

```c
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
```

Now **every command is automatically atomic**.

# 8️⃣ Global Exception Handling Pattern

CQRS should NOT throw raw exceptions to the API.

Define:

```c
public class AppException : Exception
{
    public AppException(string message) : base(message) {}
}
```

Better error mapping from Domain → Application → API later.

API Exception Middleware:

```c
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Something went wrong"
        });
    });
});
```

# 9️⃣ API Endpoints (Thin Controllers)

`Controllers/UsersController.cs`

```c
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserCommand cmd)
    {
        var id = await _mediator.Send(cmd);
        return Ok(id);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id));
        if (user == null) return NotFound();
        return Ok(user);
    }
}
```

✅ No EF references  
✅ No business logic  
✅ Clean CQRS boundaries

# 10️⃣ Performance Patterns in CQRS

# ✅ AsNoTracking for queries

```c
_db.Users.AsNoTracking()
```

# ✅ Projection-first approach

# ✅ No navigation loading on read side

# ✅ Separate read models from write models

# ✅ Part 2 Summary

You now have:

✅ Query side fully implemented  
✅ DTO projections  
✅ Validation pipeline  
✅ Transaction pipeline  
✅ Exception handling  
✅ Thin API layer  
✅ Production-level CQRS flow

# 👉 NEXT — Part 3

In **Part 3**, we will build:

✅ Domain Events  
✅ Outbox Pattern  
✅ Integration Events  
✅ Background Workers  
✅ Event-driven CQRS  
✅ Clean Architecture at Scale  
✅ Pro Tips  
✅ Final Conclusion

# Clean Architecture With.NET 10 And CQRS — Project Setup (With Full Code)

Part 3 — Domain Events, Outbox Pattern, Integration Events, Background Workers, and Production-Ready Architecture

## "Clean code gets the applause. Clean architecture survives production."

In **Part 1**, we built the foundation.  
In **Part 2**, we added real CQRS mechanics.

# 👉In Part 3, we turn your system into something enterprise-grade, event-driven, and truly production-ready

This is where most tutorials stop.  
But this is where real systems begin.

# 🔥 PART 3 ROADMAP

Here's what you'll build:

✅ Domain Events  
✅ Event Dispatching  
✅ Outbox Pattern  
✅ Integration Events  
✅ Background Event Publisher  
✅ CQRS + Event-Driven Architecture  
✅ Clean Architecture at scale  
✅ Observability + logging structure  
✅ Production-ready patterns  
✅ Pro Tips  
✅ Conclusion & Final Thoughts

# 1\. Domain Events — The Missing Piece of Most CQRS Systems

Most systems do this:

```c
Save Entity → Return Response
```

Real systems need:

```c
Save Entity
→ Raise Domain Event
→ Other parts of system react
→ Integration Messages created
→ External world notified
```

# Why Domain Events?

Because:

- Business logic becomes expressive
- Side effects are handled cleanly
- You avoid coupling layers

# 2\. Implement Base Domain Event Infrastructure

# Domain layer

`Domain/Events/IDomainEvent.cs`

```c
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
```

`Domain/Events/BaseDomainEvent.cs`

```c
public abstract class BaseDomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; protected set; } = DateTime.UtcNow;
}
```

# 3\. Emit Events From Entities

`Domain/Entities/User.cs`

```c
private readonly List<IDomainEvent> _domainEvents = new();
public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
public void AddDomainEvent(IDomainEvent domainEvent)
{
    _domainEvents.Add(domainEvent);
}
public void ClearDomainEvents()
{
    _domainEvents.Clear();
}
```

Create event:

```c
public class UserCreatedEvent : BaseDomainEvent
{
    public Guid UserId { get; }
public UserCreatedEvent(Guid userId)
    {
        UserId = userId;
    }
}
```

Emit it:

```c
public User(string email)
{
    Id = Guid.NewGuid();
    Email = email;
AddDomainEvent(new UserCreatedEvent(Id));
}
```

# 4\. Domain Event Dispatcher

Application layer:

```c
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
}
```

Implementation using MediatR:

```c
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }
    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        await _mediator.Publish(domainEvent);
    }
}
```

Register in API:

```c
builder.Services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
```

# 5\. Hook Domain Events Into EF Core SaveChanges

Infrastructure layer:

```c
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var domainEntities = ChangeTracker
        .Entries<Entity>()
        .Where(x => x.Entity.DomainEvents.Any())
        .Select(x => x.Entity)
        .ToList();
var events = domainEntities
        .SelectMany(x => x.DomainEvents)
        .ToList();
    foreach (var entity in domainEntities)
        entity.ClearDomainEvents();
    var result = await base.SaveChangesAsync(cancellationToken);
    foreach (var domainEvent in events)
        await _dispatcher.DispatchAsync(domainEvent);
    return result;
}
```

Now domain events fire automatically.

# 6\. Why You Need the Outbox Pattern

Without Outbox:

```c
DB: Save Changes ✅
Message Bus: Fail ❌
→ Data inconsistency
→ Lost integration events
```

With Outbox:

```c
Transaction:
  Save Domain Entity
  Save Outbox Message
Commit
Background Worker:
  Reads Outbox
  Publishes Event
  Marks as Processed
```

This guarantees **reliability**.

# 7\. Outbox Schema

Infrastructure:

```c
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
}
```

Add to DbContext:

```c
public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
```

# 8\. Creating Outbox Messages from Domain Events

Handler:

```c
public class UserCreatedEventHandler : INotificationHandler<UserCreatedEvent>
{
    private readonly AppDbContext _db;
public UserCreatedEventHandler(AppDbContext db)
    {
        _db = db;
    }
    public async Task Handle(UserCreatedEvent notification, CancellationToken ct)
    {
        var outbox = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(UserCreatedEvent),
            Payload = JsonSerializer.Serialize(notification),
            OccurredOn = DateTime.UtcNow
        };
        _db.OutboxMessages.Add(outbox);
        await _db.SaveChangesAsync(ct);
    }
}
```

✅ The outbox entry is saved **inside the main transaction**.

# 9\. Background Worker to Publish Events

Infrastructure project:

```c
public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
public OutboxPublisherWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await db.OutboxMessages
                .Where(x => x.ProcessedOn == null)
                .Take(10)
                .ToListAsync(stoppingToken);
            foreach (var msg in pending)
            {
                // publish to message bus (RabbitMQ, Azure Service Bus, Kafka, etc.)
                msg.ProcessedOn = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

Register:

```c
builder.Services.AddHostedService<OutboxPublisherWorker>();
```

# 10\. Integration Events

These leave your service.

Example:

```c
public record UserCreatedIntegrationEvent(Guid UserId, string Email);
```

Mapper:

```c
var integrationEvent = new UserCreatedIntegrationEvent(userId, email);
```

Now your system is:

✔ Event-driven  
✔ Eventually consistent  
✔ Microservice-ready

# 11\. Large-Scale Architecture Structure

Real production layout:

```c
/Domain
/Application
/Infrastructure
/API
/Workers
/Integration
```

Each can be deployed independently.

# 12\. Observability Hooks (Logging + Metrics Friendly)

Add structured logs:

```c
_logger.LogInformation("Domain event raised {EventType}", nameof(UserCreatedEvent));
```

Add tracing hooks:

```c
Activity.Current?.AddTag("event.name", "UserCreated");
```

# 13\. Production CQRS Checklist

✔ No EF in API  
✔ No Entities returned to API  
✔ Commands wrap in transactions  
✔ Domain events emitted automatically  
✔ Outbox guarantees delivery  
✔ Background publishing worker  
✔ Validation pipelines  
✔ Exception pipelines

This is how senior teams build systems.

# 🔥 PRO TIPS (Real Enterprise Advice)

# Pro Tip 1

Never publish events directly from a handler.  
Always use **Outbox**.

# Pro Tip 2

Never let infrastructure leak into Domain.

# Pro Tip 3

Keep commands small and transactional.

# Pro Tip 4

Queries should be projection-only.

# Pro Tip 5

Always log Domain Events.

# Pro Tip 6

Use explicit naming:

- `CreateOrderCommand`
- `OrderCreatedEvent`
- `OrderCreatedIntegrationEvent`

# Pro Tip 7

Background workers should be stateless.

# ✅ Conclusion

You now have a **complete, real-world Clean Architecture + CQRS +.NET 10 system**.

Not theory.  
Not toy examples.  
Not blog-level architecture.

This is how **actual production systems** are built.

You now understand:

- Structural boundaries
- CQRS segregation
- Pipeline behaviors
- Transaction management
- Domain-driven patterns
- Domain events
- Outbox reliability
- Integration events
- Background workers
- Production safety

You are now operating at **senior-level architecture knowledge**.

# 🎯 Final Thoughts

Most developers never go beyond CRUD.

You just built:

✅ A scalable architecture  
✅ Decoupled domain logic  
✅ Resilient event system  
✅ Production-safe messaging  
✅ Clean CQRS pipelines  
✅ Infrastructure isolation

This is the kind of foundation that can carry products for **years**.
