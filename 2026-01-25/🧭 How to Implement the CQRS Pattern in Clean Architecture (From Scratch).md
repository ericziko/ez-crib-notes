---
title: 🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)
source: https://medium.com/codetodeploy/how-to-implement-the-cqrs-pattern-in-clean-architecture-from-scratch-cb6ccefed84a
author:
  - "[[Mori]]"
published: 2025-12-23
created: 2026-01-08
description: 🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch) A Story-Driven, Code-First Journey in .NET 10 Part 1 — When Everything Starts to Hurt 🚀 Crack FAANG & Top Startup …
tags:
  - clippings
updated: 2026-01-08T00:15
uid: 06f83d24-770c-499b-84d3-5b1bb763b99e
---

# 🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)
[Sitemap](https://medium.com/sitemap/sitemap.xml)## [CodeToDeploy](https://medium.com/codetodeploy?source=post_page---publication_nav-c8b549b355f4-cb6ccefed84a---------------------------------------)

[![CodeToDeploy](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/02983b2905fc80c74294b91979facd90_MD5.png>)](https://medium.com/codetodeploy?source=post_page---post_publication_sidebar-c8b549b355f4-cb6ccefed84a---------------------------------------)

The First Publication That Blends Tech Insights + Real Job Opportunities

## Part 1 — When Everything Starts to Hurt

![](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/8dc818e9ce2d64a250bd586bf433929f_MD5.webp>)

### 🚀 Crack FAANG & Top Startup Interviews

Train with **actual interview questions** asked by Google, Meta, Amazon, and fast-growing startups.  
✅ Company-specific question practice  
✅ Hands-on projects recruiters actually care about  
✅ Proven interview frameworks & hiring signals  
✅ Learn how **Top 10% Candidates Think And Answer**

📊 **90%+ of successful candidates master these exact patterns**  
🎯 Built for results — not endless tutorials  
👉 [**Start today at Educative**](https://www.educative.io/unlimited?aff=xkRD)

### Every system starts simple. And every system eventually becomes… confusing

This is the story of how a clean, innocent application slowly turns into a mess —  
and how **CQRS** becomes the turning point.

## 🧩 The Beginning: A Simple Application

Imagine this.

You're building a small system:

- Users
- Orders
- Products

Nothing fancy.

At first, everything lives happily together.

```rb
public class OrderService
{
    public OrderDto GetOrder(Guid id) { ... }
    public void CreateOrder(CreateOrderDto dto) { ... }
    public void CancelOrder(Guid id) { ... }
}
```

Life is good.

## ⚠️ The First Cracks Appear

Then business grows.

Suddenly:

- Queries become complex
- Writes need validation
- Reporting joins explode
- Performance starts dropping
- One change breaks five endpoints

Your service now looks like this:

```rb
public class OrderService
{
    public OrderDto GetOrder(Guid id) { ... }
    public List<OrderDto> SearchOrders(...) { ... }
    public OrderReportDto GetMonthlyReport(...) { ... }
public void CreateOrder(...) { ... }
    public void UpdateOrder(...) { ... }
    public void CancelOrder(...) { ... }
    public void RefundOrder(...) { ... }
}
```

This class **knows too much**.

## 🧠 The Real Problem (It's Not EF Core)

The problem is not:  
❌ Entity Framework  
❌ SQL  
❌ Performance

The real problem is this:

### You're mixing two very different responsibilities

## Reads ≠ Writes

- Reads want **speed**
- Writes want **consistency**
- Reads want **projections**
- Writes want **rules**

And you're forcing them to live together.

## 🔥 Enter CQRS (Command Query Responsibility Segregation)

CQRS is not a framework.  
It's not about microservices.  
It's not about complexity.

It's one simple rule:

### A method either changes state OR reads state — never both

## 🧱 CQRS in One Sentence

- **Commands**: change the system
- **Queries**: read from the system
- They never overlap

## 🏗️ Clean Architecture + CQRS (Why They Fit Perfectly)

Clean Architecture gives us **layers**.  
CQRS gives us **clarity inside the application layer**.

They complement each other naturally.

## 🧬 Final Architecture We're Building (End Goal)

By the end of Part 3, we'll have this:

```rb
src/
 ├── Domain
 │    ├── Entities
 │    ├── ValueObjects
 │    └── Rules
 │
 ├── Application
 │    ├── Commands
 │    │    ├── CreateOrder
 │    │    └── CancelOrder
 │    ├── Queries
 │    │    └── GetOrderById
 │    ├── Handlers
 │    └── Interfaces
 │
 ├── Infrastructure
 │    ├── Persistence
 │    └── Repositories
 │
 └── Api
      └── Endpoints
```

But we won't jump there yet.

We'll **build it step by step**, like a story.

## 🧱 Step 1: Start With the Domain (Always)

CQRS does NOT start with handlers.  
It starts with **business rules**.

## Order Entity (Pure Domain)

```rb
public class Order
{
    private readonly List<OrderItem> _items = new();
public Guid Id { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items;
    private Order() { }
    public Order(Guid id)
    {
        Id = id;
        Status = OrderStatus.Draft;
    }
    public void AddItem(ProductId productId, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                "Cannot modify a submitted order.");
        _items.Add(new OrderItem(productId, quantity));
    }
    public void Submit()
    {
        if (!_items.Any())
            throw new InvalidOperationException(
                "Cannot submit an empty order.");
        Status = OrderStatus.Submitted;
    }
}
```

No CQRS yet.  
Just **good modeling**.

## 🧠 Important Story Insight

CQRS **does not replace domain modeling**.

If your domain is weak, CQRS will:

- Add complexity
- Hide problems
- Make things worse

Strong domain first.  
CQRS second.

## 🧱 Step 2: Introduce the Application Layer

Now we reach the moment where most apps go wrong.

Instead of calling domain logic directly from controllers…

We introduce **Commands**.

## 🧾 First Command: CreateOrder

```rb
public record CreateOrderCommand(Guid OrderId);
```

Simple.  
No behavior.  
Just **intent**.

## 🎯 Command Handler

```rb
public class CreateOrderHandler
{
    private readonly IOrderRepository _repository;
public CreateOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }
    public async Task Handle(CreateOrderCommand command)
    {
        var order = new Order(command.OrderId);
        await _repository.AddAsync(order);
    }
}
```

Notice:

- No HTTP
- No EF Core
- No DTOs
- No controllers

Just **use case logic**.

## 🧩 Repository Abstraction

```rb
public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
}
```

This is where Clean Architecture protects us.

## 🔍 Now the Other Side: Queries

Queries do **not** use domain entities.

They use **read models**.

## 🧾 Query Model

```rb
public sealed class OrderDetailsDto
{
    public Guid Id { get; init; }
    public string Status { get; init; }
    public int ItemsCount { get; init; }
}
```

Flat.  
Simple.  
Optimized for reading.

## 🔎 Query Definition

```rb
public record GetOrderByIdQuery(Guid OrderId);
```

## 🎯 Query Handler

```rb
public class GetOrderByIdHandler
{
    private readonly IReadOnlyOrderRepository _repository;
public GetOrderByIdHandler(
        IReadOnlyOrderRepository repository)
    {
        _repository = repository;
    }
    public Task<OrderDetailsDto?> Handle(
        GetOrderByIdQuery query)
    {
        return _repository.GetByIdAsync(query.OrderId);
    }
}
```

## 🧠 Critical Difference (This Is CQRS)

![](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/b081cc1b78c558d474a1fde33de69d28_MD5.webp>)

## 🌪️ The Emotional Turning Point

At this moment, something magical happens:

- Command code becomes small
- Query code becomes fast
- Business rules become obvious
- Controllers become thin
- Changes stop breaking everything

This is the **moment developers fall in love with CQRS**.

## 🧱 Controllers Become Boring (Good Thing)

```rb
app.MapPost("/orders", async (
    CreateOrderCommand command,
    CreateOrderHandler handler) =>
{
    await handler.Handle(command);
    return Results.Created();
});
```

Controllers now:

- Accept input
- Call a handler
- Return a result

Nothing more.

## ⏭️ What's Coming in Part 2

In **Part 2**, the story continues:

🔥 Validation and failures  
🔥 MediatR integration  
🔥 Transaction boundaries  
🔥 Write vs Read databases  
🔥 Real EF Core implementations  
🔥 Testing CQRS flows

## 🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)

### A Story-Driven, Code-First Journey in.NET 10

## Part 2 — The Turning Point: Discipline, Boundaries, and Control

![](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/feb76d4004e956733bfcee960bd43e5a_MD5.webp>)

### CQRS doesn't save you immediately. It saves you the moment things get hard

In **Part 1**, we separated:

- **Intent** (commands)
- **Questions** (queries)
- **Rules** (domain)

Everything felt… calmer.

But then reality hits.

## 🌪️ The Moment Complexity Strikes Back

Your application grows.

Suddenly you need:

- Validation
- Transactions
- Persistence
- Consistency
- Performance
- Tests

This is where many CQRS attempts **collapse**.

Let's not let that happen.

## 1️⃣ Validation Belongs to Commands — Not Controllers

Most systems start like this:

```rb
if (string.IsNullOrEmpty(dto.Name))
    return BadRequest();
```

That's a **leak**.

Validation is part of the **use case**, not HTTP.

## Command With Intent (Still Clean)

```rb
public record SubmitOrderCommand(Guid OrderId);
```

## Command Validator

```rb
public class SubmitOrderValidator 
    : AbstractValidator<SubmitOrderCommand>
{
    public SubmitOrderValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty();
    }
}
```

## Validation Happens Before Handler

```rb
public class SubmitOrderHandler
{
    private readonly IOrderRepository _repository;
public SubmitOrderHandler(IOrderRepository repository)
    {
        _repository = repository;
    }
    public async Task Handle(
        SubmitOrderCommand command)
    {
        var order = await _repository
            .GetByIdAsync(command.OrderId)
            ?? throw new NotFoundException(
                "Order not found");
        order.Submit();
        await _repository.SaveAsync(order);
    }
}
```

Notice:  
✔ Handler assumes valid input  
✔ Domain enforces business rules  
✔ Errors bubble naturally

## 🧠 Story Insight

CQRS shines when:

### Commands fail loudly and early

That's not a bug — that's protection.

## 2️⃣ Introducing MediatR (Without Losing Control)

MediatR is **not CQRS**.  
It's just a **dispatcher**.

Used correctly, it removes glue code.  
Used incorrectly, it hides logic.

We'll use it **sparingly**.

## Command Definition

```rb
public record SubmitOrderCommand(Guid OrderId)
    : IRequest;
```

## Handler With MediatR

```rb
public class SubmitOrderHandler 
    : IRequestHandler<SubmitOrderCommand>
{
    private readonly IOrderRepository _repository;
public SubmitOrderHandler(
        IOrderRepository repository)
    {
        _repository = repository;
    }
    public async Task Handle(
        SubmitOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _repository
            .GetByIdAsync(request.OrderId)
            ?? throw new NotFoundException(
                "Order not found");
        order.Submit();
        await _repository.SaveAsync(order);
    }
}
```

Nothing magical.  
Still explicit.  
Still readable.

## 3️⃣ Transaction Boundaries (This Is Critical)

One command = **one transaction**.

Never this:

❌ Multiple handlers  
❌ Multiple SaveChanges  
❌ Partial updates

## Unit of Work via DbContext

```rb
public class OrdersDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
public async Task CommitAsync()
    {
        await SaveChangesAsync();
    }
}
```

## Repository Implementation

```rb
public class OrderRepository : IOrderRepository
{
    private readonly OrdersDbContext _context;
public OrderRepository(OrdersDbContext context)
    {
        _context = context;
    }
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    public async Task SaveAsync(Order order)
    {
        await _context.CommitAsync();
    }
}
```

The handler does **not** manage transactions.  
Infrastructure does.

## 4️⃣ Queries Need a Different Mindset

Commands protect invariants.  
Queries optimize **speed and shape**.

Never reuse domain entities for queries.

## Read-Only DbContext

```rb
public class OrdersReadDbContext : DbContext
{
    public DbSet<OrderReadModel> Orders =>
        Set<OrderReadModel>();
}
```

## Read Model (Flat & Fast)

```rb
public class OrderReadModel
{
    public Guid Id { get; set; }
    public string Status { get; set; } = default!;
    public int ItemsCount { get; set; }
}
```

## Query Handler

```rb
public class GetOrderByIdHandler
    : IRequestHandler<
        GetOrderByIdQuery,
        OrderDetailsDto?>
{
    private readonly OrdersReadDbContext _context;
public GetOrderByIdHandler(
        OrdersReadDbContext context)
    {
        _context = context;
    }
    public async Task<OrderDetailsDto?> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Where(o => o.Id == query.OrderId)
            .Select(o => new OrderDetailsDto
            {
                Id = o.Id,
                Status = o.Status,
                ItemsCount = o.ItemsCount
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

No domain.  
No rules.  
No side effects.

## 🧠 Story Insight

At this stage:

- Writes feel **safe**
- Reads feel **fast**
- Teams stop stepping on each other

This is when CQRS starts paying rent.

## 5️⃣ Controllers Become Translators Only

```rb
app.MapPost("/orders/{id}/submit",
    async (Guid id, IMediator mediator) =>
{
    await mediator.Send(
        new SubmitOrderCommand(id));
return Results.NoContent();
});
```

No logic.  
No validation.  
No try/catch.

## 6️⃣ Testing Becomes Easy (And Honest)

## Domain Test

```rb
[Fact]
public void Cannot_Submit_Empty_Order()
{
    var order = new Order(Guid.NewGuid());
Assert.Throws<InvalidOperationException>(
        () => order.Submit());
}
```

## Command Handler Test

```rb
[Fact]
public async Task Submit_Order_Changes_Status()
{
    var order = OrderFactory.WithItems();
    var repo = new FakeOrderRepository(order);
var handler = new SubmitOrderHandler(repo);
    await handler.Handle(
        new SubmitOrderCommand(order.Id),
        default);
    order.Status.Should().Be(OrderStatus.Submitted);
}
```

No mocks for EF.  
No HTTP.  
Just behavior.

## 7️⃣ The Emotional Shift

At this point in the story:

- Developers stop fearing change
- Bugs become localized
- Features don't break reports
- Queries stop slowing writes
- Refactoring becomes safe

CQRS is no longer "extra work".

It's **relief**.

## ⏭️ What's Coming in Part 3

In **Part 3**, we reach mastery:

🔥 Read/write separation at scale  
🔥 Event-driven extensions  
🔥 Handling eventual consistency  
🔥 When NOT to use CQRS  
🔥 Common traps & myths  
🔥 Full project structure  
🔥 Pro tips from production  
🔥 Final conclusion & mindset

## 🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)

### A Story-Driven, Code-First Journey in.NET 10

## Part 3 — Mastery: Events, Consistency, and Knowing When to Stop

![](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/0dbf0855a350ab5e7c933fa84d34f7f0_MD5.webp>)

### CQRS is not about architecture. It's about making change safe

In **Part 1**, we separated intent from questions.  
In **Part 2**, we enforced discipline and boundaries.

Now we face the hardest part:

**How does this system survive real life?**

## 1️⃣ The Moment Every Real System Reaches

Sooner or later, someone asks:

### "Why isn't my query updated immediately after the command?"

Congratulations.  
You've reached **eventual consistency**.

## 2️⃣ Understanding Eventual Consistency (Without Fear)

CQRS does **not** guarantee immediate read updates.

It guarantees:

- Correctness
- Isolation
- Scalability

The read side may lag **milliseconds or seconds** behind.

This is not a flaw — it's a **design choice**.

## 3️⃣ Introducing Domain Events (The Right Way)

Domain events describe **something that already happened**.

Not intentions.  
Not commands.

## Domain Event

```rb
public record OrderSubmittedDomainEvent(
    Guid OrderId);
```

## Raising the Event

```rb
public class Order
{
    private readonly List<object> _domainEvents = new();
public IReadOnlyCollection<object> DomainEvents
        => _domainEvents;
    public void Submit()
    {
        if (!Items.Any())
            throw new InvalidOperationException(
                "Order must have items");
        Status = OrderStatus.Submitted;
        _domainEvents.Add(
            new OrderSubmittedDomainEvent(Id));
    }
    public void ClearEvents() =>
        _domainEvents.Clear();
}
```

## 4️⃣ Dispatching Domain Events Safely

After saving the transaction:

```rb
public class DomainEventDispatcher
{
    private readonly IMediator _mediator;
public DomainEventDispatcher(
        IMediator mediator)
    {
        _mediator = mediator;
    }
    public async Task DispatchAsync(
        IEnumerable<object> events)
    {
        foreach (var domainEvent in events)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}
```

## Hook Into DbContext

```rb
public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    var entities = ChangeTracker
        .Entries<AggregateRoot>()
        .Select(e => e.Entity)
        .Where(e => e.DomainEvents.Any())
        .ToList();
var result = await base.SaveChangesAsync(
        cancellationToken);
    foreach (var entity in entities)
    {
        await _dispatcher.DispatchAsync(
            entity.DomainEvents);
        entity.ClearEvents();
    }
    return result;
}
```

## 5️⃣ Updating the Read Model via Events

Now the magic happens.

## Event Handler Updates Read Side

```rb
public class OrderSubmittedHandler
    : INotificationHandler<
        OrderSubmittedDomainEvent>
{
    private readonly OrdersReadDbContext _context;
public OrderSubmittedHandler(
        OrdersReadDbContext context)
    {
        _context = context;
    }
    public async Task Handle(
        OrderSubmittedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .FirstAsync(o => o.Id == notification.OrderId);
        order.Status = "Submitted";
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}
```

Now:  
✔ Writes stay clean  
✔ Reads stay fast  
✔ Systems stay decoupled

## 🧠 Story Insight

Events turn CQRS from **structure** into **movement**.

Nothing knows *who* listens.  
Nothing breaks when listeners change.

## 6️⃣ When NOT to Use CQRS (Important)

CQRS is **not free**.

Do **not** use it when:  
❌ CRUD screens  
❌ Admin panels  
❌ Small internal tools  
❌ One developer projects  
❌ No performance pressure

CQRS shines when:  
✅ Complex business rules  
✅ Multiple consumers  
✅ Heavy reads  
✅ Independent scaling  
✅ Long-term evolution

## 7️⃣ Common CQRS Traps (Avoid These)

## ❌ Over-splitting

One handler per field change = pain.

## ❌ Anemic domain

CQRS does not replace domain modeling.

## ❌ God events

Events should be specific, not generic.

## ❌ Reusing entities for reads

That defeats the purpose.

## 8️⃣ Final Project Structure (Reality-Ready)

```rb
src/
 ├── Domain/
 │   ├── Orders/
 │   │   ├── Order.cs
 │   │   ├── OrderItem.cs
 │   │   └── Events/
 │   └── Abstractions/
 ├── Application/
 │   ├── Commands/
 │   ├── Queries/
 │   ├── Handlers/
 │   ├── Validators/
 │   └── DTOs/
 ├── Infrastructure/
 │   ├── Persistence/
 │   │   ├── WriteDbContext.cs
 │   │   └── ReadDbContext.cs
 │   ├── Repositories/
 │   └── Events/
 ├── Api/
 │   └── Program.cs
 └── Tests/
```

This scales.  
This survives teams.  
This survives time.

## 9️⃣ Pro Tips From Production

🔹 Start simple — grow into CQRS  
🔹 Split reads only when needed  
🔹 Measure before optimizing  
🔹 Protect writes fiercely  
🔹 Treat events as contracts  
🔹 Document command intent  
🔹 Never hide business logic

## 🧠 Final Conclusion

CQRS is not about:

- Fancy diagrams
- Extra projects
- Trend chasing

It's about **respecting change**.

Change is inevitable.  
Chaos is optional.

## 🌱 Final Thoughts

If you're struggling with:

- Fear of refactoring
- Slow features
- Fragile code
- Accidental complexity

CQRS isn't a silver bullet —  
but used wisely, it gives you **room to breathe**.

Start small.  
Be intentional.  
Let the system evolve — **on your terms**.

## Thank you for being a part of the community

*Before you go:*

![](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/9d04c7acb314be0ef9e2e579d9c91a98_MD5.webp>)

👉 Be sure to **clap** and **follow** the writer ️👏 **️️**

👉 Follow us: [**X**](https://x.com/Bhuwanchet67277) | [**Medium**](https://medium.com/codetodeploy)

👉 CodeToDeploy Tech Community is live on Discord — [**Join now!**](https://discord.gg/ZpwhHq6D)

👉 **Follow our publication,** [**CodeToDeploy**](https://medium.com/codetodeploy)

**Note:** This Post may contain affiliate links.

[![CodeToDeploy](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/0e4f77ae928c89e8c33630972bf7f707_MD5.png>)](https://medium.com/codetodeploy?source=post_page---post_publication_info--cb6ccefed84a---------------------------------------)

[![CodeToDeploy](<_resources/🧭 How to Implement the CQRS Pattern in Clean Architecture (From Scratch)/dc8ea158cac8ac678c078c314a11f53b_MD5.png>)](https://medium.com/codetodeploy?source=post_page---post_publication_info--cb6ccefed84a---------------------------------------)

[Last published 5 hours ago](https://medium.com/codetodeploy/from-electrical-engineering-to-ai-hamayl-shahs-journey-to-simplifying-complex-tech-9a8420ed987a?source=post_page---post_publication_info--cb6ccefed84a---------------------------------------)

The First Publication That Blends Tech Insights + Real Job Opportunities

✨ Finding life lessons in lines of code. I write about debugging our thoughts and refactoring our habits for a better life. Let's grow together.

## More from Mori and CodeToDeploy

## Recommended from Medium

[

See more recommendations

](<https://medium.com/?source=post_page---read_next_recirc--cb6ccefed84a--------------------------------------->)w
