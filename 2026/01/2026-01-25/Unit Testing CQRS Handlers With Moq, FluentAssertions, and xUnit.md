---
title: Unit Testing CQRS Handlers With Moq, FluentAssertions, and xUnit
source: https://medium.com/codetodeploy/unit-testing-cqrs-handlers-with-moq-fluentassertions-and-xunit-4739550477c0
author:
  - "[[Mori]]"
published: 2026-01-23
created: 2026-01-25T00:00:00
description: Unit Testing CQRS Handlers With Moq, FluentAssertions, and xUnit A Story-Driven, No-Fluff Guide With Real Code (.NET 10) CodeToDeploy A Publication Where Tech People Learn, Build, And Grow. Follow To …
tags:
  - clippings
uid: 37407c6b-aa94-4168-9190-3437034f0768
modified: 2026-01-25T00:40:58
---
# Unit Testing CQRS Handlers With Moq, FluentAssertions, and xUnit
[Sitemap](https://medium.com/sitemap/sitemap.xml)## [CodeToDeploy](https://medium.com/codetodeploy?source=post_page---publication_nav-c8b549b355f4-4739550477c0---------------------------------------)## [CodeToDeploy](https://medium.com/codetodeploy?source=post_page-----4739550477c0---------------------------------------)

A Publication Where Tech People Learn, Build, And Grow. Follow To Join Our 500k+ Monthly Readers

medium.com

[View original](https://medium.com/codetodeploy?source=post_page-----4739550477c0---------------------------------------)

## Part 1 — “If your CQRS handlers aren’t unit tested, you don’t really know if your architecture works — you’re just hoping.”

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*ZTNk_9P--5wLuVhXYWKrWQ.jpeg)

This article is not about **what CQRS is**.  
It’s about **how to trust it**.

And trust only comes from **tests that are fast, readable, and meaningful**.

## The Story Begins: “Why Did This Break in Production?”

It was a small change.

A new field added to a command.  
A tiny validation tweak.  
Nothing touched the database schema.

And yet… production broke.

No compile errors.  
No failing integration tests.  
Just users reporting that **“creating an order suddenly doesn’t work.”**

The handler *looked* correct.  
The logic *seemed* straightforward.

But there were **no unit tests** around the CQRS handlers — only controller tests and some happy-path integration tests.

That’s the moment most teams realize:

### CQRS without handler-level unit tests is just hope-driven development.

This article is about fixing that.

## What We Are Testing (And What We Are Not)

Before writing a single test, let’s align on **scope**.

## ✅ We WILL test

- Command & Query **handlers**
- Business rules
- Validation behavior
- Side effects (repository calls, domain events, etc.)
- Error scenarios

## ❌ We will NOT test

- Controllers
- EF Core
- Database behavior
- MediatR itself

Why?

Because **CQRS handlers are pure application logic** — they should be:

- Fast to test
- Deterministic
- Independent of infrastructure

## The Sample Scenario (Simple but Real)

We’ll build and test a very common scenario:

## Use Case

### Create an Order

Business rules:

- Order must have at least one item
- Total price must be greater than zero
- Order gets saved
- OrderCreated domain event is raised

We’ll test **only the handler**, not the API.

## Project Structure (Clean Architecture)

```c
src
 ├── Domain
 │    ├── Orders
 │    │    ├── Order.cs
 │    │    ├── OrderItem.cs
 │    │    └── OrderCreatedDomainEvent.cs
 │
 ├── Application
 │    ├── Orders
 │    │    └── CreateOrder
 │    │         ├── CreateOrderCommand.cs
 │    │         ├── CreateOrderHandler.cs
 │    │         └── CreateOrderValidator.cs
 │
 ├── Infrastructure
 │
tests
 └── Application.Tests
      └── Orders
           └── CreateOrderHandlerTests.cs
```

This separation is **critical** for clean unit testing.

## The Command

```c
public sealed record CreateOrderCommand(
    Guid CustomerId,
    IReadOnlyList<OrderItemDto> Items
) : IRequest<Guid>;
```

DTO:

```c
public sealed record OrderItemDto(
    Guid ProductId,
    int Quantity,
    decimal Price
);
```

## The Domain Entity

```c
public sealed class Order
{
    private readonly List<OrderItem> _items = new();
public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal TotalPrice => _items.Sum(i => i.Price * i.Quantity);
    private Order() { }
    public static Order Create(Guid customerId, IEnumerable<OrderItem> items)
    {
        if (!items.Any())
            throw new DomainException("Order must contain at least one item");
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId
        };
        order._items.AddRange(items);
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id));
        return order;
    }
}
```

This logic **must be tested indirectly** via the handler.

## Repository Contract (Mock-Friendly)

```c
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken cancellationToken);
}
```

Notice:

- No EF Core
- No DbContext
- No implementation details

Perfect for mocking.

## The CQRS Handler

```c
public sealed class CreateOrderHandler 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
public CreateOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }
    public async Task<Guid> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var items = request.Items.Select(i =>
            new OrderItem(i.ProductId, i.Quantity, i.Price));
        var order = Order.Create(request.CustomerId, items);
        await _orderRepository.AddAsync(order, cancellationToken);
        return order.Id;
    }
}
```

This handler:

- Contains **business orchestration**
- Has **one dependency**
- Is **fully testable**

Now the fun part begins.

## Test Project Setup

## NuGet Packages

```c
dotnet add package xunit
dotnet add package Moq
dotnet add package FluentAssertions
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio
```

## Writing the First Unit Test

## Test Class Skeleton

```c
public sealed class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly CreateOrderHandler _handler;
public CreateOrderHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new CreateOrderHandler(_orderRepositoryMock.Object);
    }
}
```

## Test Case #1: Happy Path

### “When a valid command is sent, the order should be saved and an ID returned.”

```c
[Fact]
public async Task Handle_Should_Create_Order_And_Save_It()
{
    // Arrange
    var command = new CreateOrderCommand(
        Guid.NewGuid(),
        new List<OrderItemDto>
        {
            new(Guid.NewGuid(), 2, 50)
        });
// Act
    var orderId = await _handler.Handle(command, CancellationToken.None);
    // Assert
    orderId.Should().NotBeEmpty();
    _orderRepositoryMock.Verify(
        r => r.AddAsync(
            It.IsAny<Order>(),
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

## Why this test matters

- No database
- No controller
- No framework noise
- Just **pure business behavior**

## Why FluentAssertions Changes Everything

Compare:

```c
Assert.NotEqual(Guid.Empty, orderId);
```

vs

```c
orderId.Should().NotBeEmpty();
```

The second reads like **documentation**, not code.

That matters when your test suite grows to hundreds of tests.

## What We Achieved in Part 1

✅ Clean CQRS handler  
✅ Domain logic isolated  
✅ Mocked dependencies  
✅ Fast, readable unit tests  
✅ Zero infrastructure involved

And we’ve only scratched the surface.

## Coming in Part 2

- Testing **business rule failures**
- Verifying **domain behavior**
- FluentAssertions deep patterns
- Testing exception scenarios
- Avoiding over-mocking traps

## Unit Testing CQRS Handlers With Moq, FluentAssertions, and xUnit

## Part 2 — Testing Business Rules, Exceptions, and Domain Behavior

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*ov6s0h-aKQnx3_AUQHj4GQ.jpeg)

## Recap

In **Part 1**, we:

- Built a `CreateOrderCommand` and handler
- Isolated domain logic in `Order.Create`
- Mocked `IOrderRepository`
- Wrote the **happy path test**

Now we tackle the harder cases:

- Invalid commands
- Domain rule violations
- Exception handling
- Multiple domain events
- Advanced FluentAssertions patterns

## Step 1: Testing Business Rule Violations

Remember the domain rule:

### An order must contain at least one item.

Let’s test it.

```c
[Fact]
public async Task Handle_Should_Throw_When_Order_Has_No_Items()
{
    // Arrange
    var command = new CreateOrderCommand(
        Guid.NewGuid(),
        new List<OrderItemDto>() // empty list
    );
// Act
    Func<Task> act = async () => 
        await _handler.Handle(command, CancellationToken.None);
    // Assert
    await act.Should().ThrowAsync<DomainException>()
        .WithMessage("Order must contain at least one item");
    _orderRepositoryMock.Verify(
        r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
        Times.Never);
}
```

✅ Key takeaways:

- No database hit
- Validation is **enforced at the domain layer**
- Mocked repository **never called**

## Step 2: Handling Multiple Domain Events

Imagine each order triggers multiple events:

```c
public sealed class OrderCreatedDomainEvent : IDomainEvent { }
public sealed class OrderTotalUpdatedEvent : IDomainEvent { }
```

Unit test to verify events:

```c
[Fact]
public async Task Handle_Should_Raise_DomainEvents()
{
    // Arrange
    var command = new CreateOrderCommand(
        Guid.NewGuid(),
        new List<OrderItemDto>
        {
            new(Guid.NewGuid(), 1, 100)
        });
// Act
    var orderId = await _handler.Handle(command, CancellationToken.None);
    // Assert
    var orderCaptured = _orderRepositoryMock.Invocations
        .Select(i => i.Arguments[0])
        .OfType<Order>()
        .Single();
    orderCaptured.DomainEvents.Should().ContainSingle(e => e is OrderCreatedDomainEvent);
    orderCaptured.DomainEvents.Should().ContainSingle(e => e is OrderTotalUpdatedEvent);
}
```

**FluentAssertions** makes this readable like documentation.

## Step 3: Testing Exception Scenarios

Suppose the repository throws:

```c
_orderRepositoryMock
    .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
    .ThrowsAsync(new InvalidOperationException("Database unavailable"));
```

Unit test:

```c
[Fact]
public async Task Handle_Should_Propagate_Repository_Exception()
{
    // Arrange
    var command = new CreateOrderCommand(
        Guid.NewGuid(),
        new List<OrderItemDto>
        {
            new(Guid.NewGuid(), 1, 50)
        });
_orderRepositoryMock
        .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("Database unavailable"));
    // Act
    Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Database unavailable");
}
```

✅ Why this matters:

- You’re not testing EF Core — only behavior
- Exceptions bubble correctly
- Ensures your MediatR pipeline will behave predictably

## Step 4: Avoid Over-Mocking

Common mistake:

- Mocking everything including `Order.Create`
- Testing mocks instead of **real domain behavior**

✅ Rule:

### Only mock external dependencies. Never mock your own domain entities or aggregates.

## Step 5: Testing Commands vs Queries

CQRS gives clarity:

- Commands → Test **state changes**, side effects, domain events
- Queries → Test **read model**, projections, DTO mapping

Example query test:

```c
public sealed class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderReadRepository> _readRepo;
    private readonly GetOrderByIdHandler _handler;
public GetOrderByIdQueryHandlerTests()
    {
        _readRepo = new Mock<IOrderReadRepository>();
        _handler = new GetOrderByIdHandler(_readRepo.Object);
    }
    [Fact]
    public async Task Handle_Should_Return_OrderDto()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), ...);
        _readRepo.Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);
        var dto = await _handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);
        dto.Should().NotBeNull();
        dto.Id.Should().Be(order.Id);
    }
}
```

Notice: **no EF Core**, just interfaces and in-memory objects.

## Step 6: Advanced FluentAssertions Patterns

```c
orderCaptured.DomainEvents.Should().ContainSingle<OrderCreatedDomainEvent>()
    .Which.OrderId.Should().Be(orderCaptured.Id);
```
- Reads like plain English
- Verifies **properties** of domain events
- Avoids brittle casts or `OfType<T>()` hacks

## Part 2 Takeaways

✅ Commands should be **fully testable without DB**  
✅ FluentAssertions = readable, maintainable tests  
✅ Domain exceptions are **first-class citizens**  
✅ Over-mocking is a trap  
✅ Queries are simple, focus on read-only behavior

## Coming in Part 3

- Testing **validation with FluentValidation**
- Integration with **MediatR pipeline behaviors**
- Mocking multiple dependencies
- End-to-end in-memory scenario tests
- Patterns for **highly maintainable, scalable unit tests**

## Unit Testing CQRS Handlers With Moq, FluentAssertions, and xUnit

## Part 3 — Full Pipeline Testing, Validation, and Best Practices

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*Ymmnc5QntyiMPm5OBuZSCg.jpeg)

## Recap

So far we have:

- **Part 1:** Happy path tests, basic repository mocking, clean handler setup
- **Part 2:** Business rule violations, domain events, exception propagation, avoiding over-mocking

Now we tackle:

- Validation using **FluentValidation**
- MediatR **pipeline behaviors**
- Testing multiple dependencies and side effects
- End-to-end in-memory scenario
- Final pro tips, conclusion, and best practices

## Step 1: Adding FluentValidation

## Validator for CreateOrderCommand

```c
public sealed class CreateOrderValidator 
    : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(c => c.CustomerId).NotEmpty();
        RuleFor(c => c.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");
RuleForEach(c => c.Items).SetValidator(new OrderItemValidator());
    }
}
public sealed class OrderItemValidator 
    : AbstractValidator<OrderItemDto>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}
```

This allows us to **separate domain validation from handler logic**, while keeping tests fast.

## Step 2: Testing Validation

```c
[Fact]
public async Task Handle_Should_Fail_Validation_When_Command_Is_Invalid()
{
    // Arrange
    var command = new CreateOrderCommand(Guid.Empty, new List<OrderItemDto>());
var validator = new CreateOrderValidator();
    var validationResult = await validator.ValidateAsync(command);
    // Assert
    validationResult.IsValid.Should().BeFalse();
    validationResult.Errors.Should().ContainSingle(e => e.ErrorMessage == "Order must contain at least one item");
}
```

✅ This ensures **invalid commands never reach the handler**.

## Step 3: Testing With MediatR Pipeline Behaviors

MediatR supports **pre-processing, validation, logging, caching**, etc.

## Example: Validation Behavior

```c
public sealed class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
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
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var failures = _validators.SelectMany(v => v.Validate(context).Errors)
                                      .Where(f => f != null)
                                      .ToList();
            if (failures.Any())
                throw new ValidationException(failures);
        }
        return await next();
    }
}
```

## Testing the Pipeline

```c
[Fact]
public async Task Pipeline_Should_Throw_ValidationException_For_Invalid_Command()
{
    var validators = new List<IValidator<CreateOrderCommand>> { new CreateOrderValidator() };
    var pipeline = new ValidationBehavior<CreateOrderCommand, Guid>(validators);
var invalidCommand = new CreateOrderCommand(Guid.Empty, new List<OrderItemDto>());
    Func<Task<Guid>> act = () => pipeline.Handle(invalidCommand, () => Task.FromResult(Guid.NewGuid()), CancellationToken.None);
    await act.Should().ThrowAsync<ValidationException>()
        .WithMessage("*Order must contain at least one item*");
}
```

This confirms **your pipeline validates commands before hitting the handler**.

## Step 4: Testing Multiple Dependencies and Side Effects

Imagine the handler also publishes events:

```c
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent domainEvent) where TEvent : IDomainEvent;
}
```

Unit test:

```c
[Fact]
public async Task Handle_Should_Publish_DomainEvents()
{
    // Arrange
    var eventPublisherMock = new Mock<IDomainEventPublisher>();
    var handler = new CreateOrderHandler(_orderRepositoryMock.Object, eventPublisherMock.Object);
var command = new CreateOrderCommand(Guid.NewGuid(), new List<OrderItemDto>
    {
        new(Guid.NewGuid(), 1, 50)
    });
    // Act
    var orderId = await handler.Handle(command, CancellationToken.None);
    // Assert
    eventPublisherMock.Verify(
        x => x.PublishAsync(It.IsAny<OrderCreatedDomainEvent>()), Times.Once);
}
```

✅ Ensures all **side effects** are tested without touching infrastructure.

## Step 5: End-to-End In-Memory Scenario

Sometimes you want **all pieces together**:

- Handler
- Validator
- Pipeline behavior
- Mocked repository
- Mocked publisher
```c
[Fact]
public async Task CreateOrder_EndToEnd_Succeeds()
{
    // Arrange
    var repoMock = new Mock<IOrderRepository>();
    var publisherMock = new Mock<IDomainEventPublisher>();
    var validators = new List<IValidator<CreateOrderCommand>> { new CreateOrderValidator() };
    var pipeline = new ValidationBehavior<CreateOrderCommand, Guid>(validators);
var handler = new CreateOrderHandler(repoMock.Object, publisherMock.Object);
    var command = new CreateOrderCommand(Guid.NewGuid(), new List<OrderItemDto>
    {
        new(Guid.NewGuid(), 1, 100)
    });
    // Act
    Guid orderId = await pipeline.Handle(command, () => handler.Handle(command, CancellationToken.None), CancellationToken.None);
    // Assert
    orderId.Should().NotBeEmpty();
    repoMock.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    publisherMock.Verify(p => p.PublishAsync(It.IsAny<OrderCreatedDomainEvent>()), Times.Once);
}
```

✅ Combines **validation, handler logic, and side effects** in a single fast-running test.

## Step 6: Pro Tips & Best Practices

1. **Test handlers, not EF Core** — keep tests fast and deterministic
2. **Never over-mock domain entities** — always test real domain logic
3. **Use FluentAssertions** — readability = maintainability
4. **Pipeline behaviors are testable** — validate commands before handlers
5. **Keep unit tests isolated** — integration tests cover database, messaging, etc.
6. **Use in-memory scenarios sparingly** — best for full-path checks without hitting real infrastructure
7. **Name tests like a story** — “Handle\_Should\_DoX\_When\_Y”

## Conclusion & Final Thoughts

- Unit testing **CQRS handlers** turns hope-driven development into **reliable code**
- Mock external dependencies (repositories, publishers), keep **domain logic real**
- FluentAssertions and xUnit make tests **readable and maintainable**
- MediatR pipelines can be tested in isolation
- Combining **validation, events, and side effects** in fast tests gives full confidence

### By structuring your tests this way, every change becomes predictable, safe, and easily refactorable — a hallmark of production-ready Clean Architecture with.NET 10.

## Thank you for being a part of the community

*Before you go:*

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*d9QTaaaxboQP_gKSLedW_w.png)

👉 Be sure to **clap** and **follow** the writer ️👏 **️️**

👉 Follow us: [**Linkedin**](https://www.linkedin.com/in/code-to-deploy-3784391b9/) | [**Medium**](https://medium.com/codetodeploy)

👉 CodeToDeploy Tech Community is live on Discord — [**Join now!**](https://discord.gg/ZpwhHq6D)

[![CodeToDeploy](https://miro.medium.com/v2/resize:fill:96:96/1*s4SuUoJSUCQqhZfIZuM85A.png)](https://medium.com/codetodeploy?source=post_page---post_publication_info--4739550477c0---------------------------------------)

[![CodeToDeploy](https://miro.medium.com/v2/resize:fill:128:128/1*s4SuUoJSUCQqhZfIZuM85A.png)](https://medium.com/codetodeploy?source=post_page---post_publication_info--4739550477c0---------------------------------------)

[Last published 3 hours ago](https://medium.com/codetodeploy/top-10-cloudsecurity-tips-in-2026-b76ef938ea74?source=post_page---post_publication_info--4739550477c0---------------------------------------)

A Publication Where Tech People Learn, Build, And Grow. Follow To Join Our 500k+ Monthly Readers

✨ Finding life lessons in lines of code. I write about debugging our thoughts and refactoring our habits for a better life. Let's grow together.

## More from the list: "⭐️"

Curated by[Eric Ziko](https://medium.com/@eric.ziko?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)[Hamed Shirbandi](https://medium.com/@hamed.shirbandi?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [Mutation Testing with Stryker in.NET Projects](https://medium.com/@hamed.shirbandi/mutation-testing-with-stryker-in-net-projects-ff1f05ddce8f?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

Oct 17, 2023

Sep 18, 2025[Joe Njenga](https://medium.com/@joe.njenga?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [Everything Claude Code: The Repo That Won Anthropic Hackathon (Here’s a Breakdown)](https://medium.com/@joe.njenga/everything-claude-code-the-repo-that-won-anthropic-hackathon-33b040ba62f3?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

2d ago[Jordan Rowles](https://medium.com/@jordansrowles?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [Building Your Own BETTER Mediator Pattern in Modern.NET](https://medium.com/@jordansrowles/building-your-own-better-mediator-pattern-in-modern-net-163917ce41df?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

1d ago## [5 Underused C# Features That Make Defensive Code Obsolete](https://medium.com/codetodeploy/5-underused-c-features-that-make-defensive-code-obsolete-f6ef31975b2f?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

3d ago[Ashish Singh](https://medium.com/@ashishnoob?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [7 Homebrew Tools on macOS You’ll Regret Not Knowing Sooner](https://medium.com/@ashishnoob/7-homebrew-tools-on-macos-youll-regret-not-knowing-sooner-0bc7a70e7ba4?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

Jan 15[Syed Qutub Uddin Khan](https://medium.com/@s.quddin?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [No “NullReferenceException” in.NET Anymore! Mark Your Nullable Reference Type Warnings As Errors](https://medium.com/@s.quddin/no-nullreferenceexception-in-net-anymore-mark-your-nullable-reference-type-warnings-as-errors-e3d588602617?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

Dec 29, 2024[sharmila subbiah](https://medium.com/@malarsharmila?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [Enhancing C# Code with Nullable Reference Types](https://medium.com/@malarsharmila/enhancing-c-code-with-nullable-reference-types-d37ed45ebc33?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

Mar 11, 2024## [Safer Code with C# 8 Non-Null Reference Types](https://medium.com/swlh/safer-code-with-c-8-non-null-reference-types-cd5241e5714?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

Sep 13, 2019[DotNet Full Stack Dev](https://medium.com/@dotnetfullstackdev?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)## [“No Dockerfile, Still a Container”: How.NET 10 Publishes Container Images Natively (Step-by-Step)](https://medium.com/@dotnetfullstackdev/no-dockerfile-still-a-container-how-net-10-publishes-container-images-natively-step-by-step-a44238e926d7?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

4d ago

[View list](https://medium.com/@eric.ziko/list/e81e743d93d3?source=post_page---list_recirc--4739550477c0-----------e81e743d93d3----------------------------)

## More from Mori and CodeToDeploy

## Recommended from Medium

[

See more recommendations

](https://medium.com/?source=post_page---read_next_recirc--4739550477c0---------------------------------------)w