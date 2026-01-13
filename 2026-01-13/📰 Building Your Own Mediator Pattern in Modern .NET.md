---
title: 📰 Building Your Own Mediator Pattern in Modern .NET
source: https://jordansrowles.medium.com/building-your-own-mediator-pattern-in-modern-net-804995c44a1b
author:
  - "[[Jordan Rowles]]"
published: 2025-12-23
created: 2026-01-07
description: “” is published by Jordan Rowles.
tags:
  - clippings
updated: 2026-01-07T22:48
---

# 📰 Building Your Own Mediator Pattern in Modern .NET

Why not just use MediatR?  
The basic components  
Building the mediator  
Wiring it up with dependency injection  
… Manual  
… Automatically (reflection)  
Using the mediator  
Performance considerations  
.. Going further with compiled expressions  
Notifications vs Requests (Optional)  
The pipeline  
.. Behaviour: Validation  
.. Behaviour: Transactions  
.. Behaviour: Caching  
.. Behaviour: Metrics  
Wrapping up

The mediator pattern is one those patterns that sounds more complicated than it actually is. At its core, it's about reducing coupling between components by introducing a middleman that handles messaging. Instead of having components talk directly to each other (what we call tight coupling), they send messages to a mediator, which routes the messages to the appropriate handlers.

What's the difference between the publisher/subscriber pattern (that I wrote about before), and mediator? As opposed to pub/sub, mediator is often bi-directional messaging with components that know about each other, it can maintain state, and can route direct messages to specific components.

So why bother with mediator? Well alternatively, we have direct dependencies everywhere, or we need to have events firing off all over the place.

Mediator kind of sits in the middle. A component says "I need to create an order" by sending a message to the mediator, the mediator finds the handler for that message type and executes it. The component doesn't care what happens next. That's a de-coupled system.

## Why not just use MediatR?

MediatR is great, and battle-tested, but it moved to a commercial license in early 2025. Still free for open source projects, but commercial use requires a license. Some alternatives worth looking at include,

- Wolverine — fully featured, support for all the big message bus providers, includes saga/workflows.
- Mediator — uses source generators to create dispatch code at compile time. No reflection means better performance, and more importantly, AOT support.
- Immediate.Handlers — super minimal, intentionally simple.

That being said, let's create our own.

## The basic components

A mediator implementation needs three things,

1. Requests/Messages that are sent through the mediator
2. Handlers that know how to processes specific request types
3. The mediator that routes the messages to handlers

Here's the class structure we're building

![](<_resources/📰 Building Your Own Mediator Pattern in Modern .NET/5a7a3d32eb295a39c57d018922069afd_MD5.webp>)

First we'll define an interface for the requests. A blank interface is what we call a "marker interface",

```csharp
public interface IRequest<TResponse>
{
}
```

A request can be any class that implements `IRequest<TResponse>`, where `TResponse` is the type you'll get back when the request is handled. So for example,

```csharp
public record CreateOrderRequest(string CustomerId, List<OrderItem> Items) 
    : IRequest<OrderResult>;

public record OrderResult(string OrderId, decimal Total);
```

We use records for this kind of stuff because, typically, the data structures should be immutable (why do you need to change a request after it's come through?). So no reason to make them mutable unless there's a reason.

We'll create an interface for the handlers,

```csharp
public interface IRequestHandler<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
```

A handler is any class that implements this interface, that takes types for the request and response. The `CancellationToken` is there because in the real world, you need to be able to cancel a long-running operation. Here's an implementation,

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, OrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;

    public CreateOrderHandler(IOrderRepository orderRepository, IInventoryService inventoryService)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
    }

    public async Task<OrderResult> HandleAsync(
        CreateOrderRequest request, 
        CancellationToken cancellationToken)
    {
        // Validate inventory
        await _inventoryService.ReserveItemsAsync(request.Items, cancellationToken);

        // Create order
        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString(),
            CustomerId = request.CustomerId,
            Items = request.Items,
            Total = request.Items.Sum(i => i.Price * i.Quantity)
        };

        await _orderRepository.SaveAsync(order, cancellationToken);

        return new OrderResult(order.OrderId, order.Total);
    }
}
```

Nothing super fancy. The handler has dependencies (repositories, services, whatever), it does some work, and it returns a result. The key thing is that whoever sends the `CreateOrderRequest` doesn't know anything about `IOrderRepository` or `IInventoryService`. De-coupled.

## Building the mediator

Now for the mediator itself. It needs to accept a request > find the appropriate handler for that type > execute the handler > return a response.

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default);
}

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>)
            .MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for request type {requestType.Name}");
        }

        var handleMethod = handlerType.GetMethod("HandleAsync");
        if (handleMethod is null)
        {
            throw new InvalidOperationException(
                $"HandleAsync method not found on handler for {requestType.Name}");
        }

        var resultTask = (Task<TResponse>)handleMethod.Invoke(
            handler, 
            new object[] { request, cancellationToken })!;

        return await resultTask;
    }
}
```

When you call `SendAsync<TResponse>`, the medaitor

1. Gets the actual type of the request object (`CreateOrderRequest`)
2. Constructs the handler interface type (`IRequestHandler<CreateOrderRequest, OrderResult>`)
3. Asks the DI container for an instance of that handler type
4. Uses reflection to invoke the `HandleAsync ` method
5. Return the result

The reflection bit is necessary (for this design anyway) because we're dealing with generic types at runtime. It's not the fastest thing, but for most things, it's fine, and you could always use source generators.

## Wiring it up with dependency injection

### Manually

We could manually wire everything up, like so

```csharp
builder.Services.AddSingleton<IMediator, Mediator>();

// Register handlers
builder.Services.AddTransient<IRequestHandler<CreateOrderRequest, OrderResult>, CreateOrderHandler>();
builder.Services.AddTransient<IRequestHandler<GetOrderRequest, OrderResult>, GetOrderHandler>();
builder.Services.AddTransient<IRequestHandler<CancelOrderRequest, bool>, CancelOrderHandler>();
```

But that's messy, and tedious. If you've got 50 handlers, then it can get long winded.

### Automatically (reflection)

We can use reflection to auto wire the handlers based on the interface, like so

```csharp
public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(
        this IServiceCollection services, 
        params Assembly[] assemblies)
    {
        services.AddSingleton<IMediator, Mediator>();

        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            services.AddTransient(interfaceType, handlerType);
        }

        return services;
    }
}
```

Now, when we register the mediator, all we have to do is

```csharp
builder.Services.AddMediator(typeof(Program).Assembly);
```

This scans the current assembly, finds all classes that implement `IRequestHandler<,>` (`,` denoting an open generic), and registers them with the DI container.

## Using the mediator

Now in your controllers or services or wherever, you can inject `IMediator` and send requests

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request, 
        CancellationToken cancellationToken)
    {
        var result = await _mediator.SendAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(
        string orderId, 
        CancellationToken cancellationToken)
    {
        var request = new GetOrderRequest(orderId);
        var result = await _mediator.SendAsync(request, cancellationToken);
        return Ok(result);
    }
}
```

The controller doesn't know anything about CreateOrderHandler or its dependencies. It just sends a request and returns a response. If you need to modify behaviour, you do so in the handler, not the controller.

## Performance considerations

The reflection based approach I shows works well, but it's not lightning fast. Every call to SendAsync uses reflection to find and invoke the handler method. For most applications, the overhead is negligible (microseconds). But if you're processing high volumes, you can cache the handler lookup and invocation logic.

The key insight is that we need to cache the delegate but still resolve the handler from the service provider on each request. Handlers might be registered as transient or scoped, so we can't cache the handler instance itself.

Here's a more optimised example

```csharp
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<Type, object> _handlerInvokerCache;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _handlerInvokerCache = new ConcurrentDictionary<Type, object>();
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        var handlerInvoker = (Func<IRequest<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>>)
            _handlerInvokerCache.GetOrAdd(requestType, rt =>
            {
                return CreateHandlerInvoker<TResponse>(rt);
            });

        return await handlerInvoker(request, _serviceProvider, cancellationToken);
    }

    private Func<IRequest<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>> 
        CreateHandlerInvoker<TResponse>(Type requestType)
    {
        var responseType = typeof(TResponse);
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handleMethod = handlerType.GetMethod("HandleAsync")!;

        return (request, serviceProvider, cancellationToken) =>
        {
            var handler = serviceProvider.GetService(handlerType);
            if (handler is null)
            {
                throw new InvalidOperationException(
                    $"No handler registered for request type {requestType.Name}");
            }

            var resultTask = (Task<TResponse>)handleMethod.Invoke(
                handler, 
                new object[] { request, cancellationToken })!;

            return resultTask;
        };
    }
}
```

This caches the invocation delegate but still resolves the handler from the service provider on each request. The reflection cost is now only paid once per request type, not on every call, only the first.

Database and networking throughput would still be bigger bottlenecks though.

### Going further with compiled expressions

The cached delegate approach still uses `MethodInfo.Invoke` under the hood. That's alright for most things, but `Invoke` has some overhead, it has to box value types, create parameter arrays, and do runtime type checking.

You could squeeze even more performance by using compiled expressions.

Instead of calling `handleMethod.Invoke(handler, args)`, we build an expression tree that represents a direct method call, then compile it to a delegate. The compiled deletegate is essentially the same as if you'd have written `handler.HandleAsync(request, cancellationToken)` directly in the code. No boxing, parameter arrays, or runtime type checking.

Here's what the expression based invoker looks like,

```csharp
private Func<object, object, CancellationToken, Task<TResponse>> 
    CreateCompiledInvoker<TResponse>(Type requestType)
{
    var responseType = typeof(TResponse);
    var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
    var handleMethod = handlerType.GetMethod("HandleAsync")!;

    // Parameters for our delegate
    var handlerParam = Expression.Parameter(typeof(object), "handler");
    var requestParam = Expression.Parameter(typeof(object), "request");
    var tokenParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

    // Cast the parameters to their actual types
    var handlerCast = Expression.Convert(handlerParam, handlerType);
    var requestCast = Expression.Convert(requestParam, requestType);

    // Build the method call: ((THandler)handler).HandleAsync((TRequest)request, cancellationToken)
    var methodCall = Expression.Call(handlerCast, handleMethod, requestCast, tokenParam);

    // Compile to a delegate
    var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<TResponse>>>(
        methodCall,
        handlerParam,
        requestParam,
        tokenParam);

    return lambda.Compile();
}
```

And then the updated `CreateHandlerInvoker`,

```csharp
private Func<IRequest<TResponse>, IServiceProvider, CancellationToken, Task<TResponse>> 
    CreateHandlerInvoker<TResponse>(Type requestType)
{
    var responseType = typeof(TResponse);
    var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
    
    // Create the compiled invoker once
    var compiledInvoker = CreateCompiledInvoker<TResponse>(requestType);

    return (request, serviceProvider, cancellationToken) =>
    {
        var handler = serviceProvider.GetService(handlerType);
        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No handler registered for request type {requestType.Name}");
        }

        // Direct delegate call, no reflection
        return compiledInvoker(handler, request, cancellationToken);
    };
}
```

This approach gives you a strongly typed delegate that calls the handler method directly. The compilation happens once when the first request of that type comes into the mediator, the every subsequent request call uses the compiled delegate.

Again though, most of this will be going from like 700 nanoseconds to 10 nanoseconds. Youll need to measure, to understand where to optimise.

## Notifications vs Requests (optional)

So far we've only dealt with requests that produce responses. But sometimes, we don't want a response, we just need to send a notification that multiple handlers can process. (Think domain events for example).

We can implement this with a separate notification interface,

```csharp
public interface INotification
{
}

public interface INotificationHandler<TNotification> where TNotification : INotification
{
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
```

Then add a `PublishAsync` method to the mediator

```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default);

    Task PublishAsync<TNotification>(
        TNotification notification, 
        CancellationToken cancellationToken = default)
        where TNotification : INotification;
}
```

The implementation then finds all handlers for the notification type and executes them,

```csharp
public async Task PublishAsync<TNotification>(
    TNotification notification, 
    CancellationToken cancellationToken = default)
    where TNotification : INotification
{
    var notificationType = notification.GetType();
    var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

    var handlers = _serviceProvider.GetServices(handlerType);

    var tasks = handlers.Select(handler =>
    {
        var handleMethod = handlerType.GetMethod("HandleAsync")!;
        return (Task)handleMethod.Invoke(handler, new object[] { notification, cancellationToken })!;
    });

    await Task.WhenAll(tasks);
}
```

Important to note we use `GetServices` instead of `GetService`. This get all registered handlers for the notification type. The handlers run in parallel with `Task.WhenAll`. If you want sequential, use a `foreach` loop instead.

Here's an example of a handler,

```csharp
public record OrderCreatedNotification(string OrderId, string CustomerId) : INotification;

public class SendOrderEmailHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly IEmailService _emailService;

    public SendOrderEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task HandleAsync(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        await _emailService.SendOrderConfirmationAsync(
            notification.CustomerId, 
            notification.OrderId, 
            cancellationToken);
    }
}

public class LogOrderCreatedHandler : INotificationHandler<OrderCreatedNotification>
{
    private readonly ILogger<LogOrderCreatedHandler> _logger;

    public LogOrderCreatedHandler(ILogger<LogOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order {OrderId} created for customer {CustomerId}", 
            notification.OrderId, 
            notification.CustomerId);

        await Task.CompletedTask;
    }
}
```

In your handler for CreateOrderRequest, after creating the order, you publish the notification,

```csharp
public async Task<OrderResult> HandleAsync(
    CreateOrderRequest request, 
    CancellationToken cancellationToken)
{
    // ... create order logic ...

    await _mediator.PublishAsync(
        new OrderCreatedNotification(order.OrderId, order.CustomerId), 
        cancellationToken);

    return new OrderResult(order.OrderId, order.Total);
}
```

Now the mediator has pub/sub capabilities. For simple scenarios, notifications are overkill. But it's still a really clean pattern.

## The pipeline

So far our mediator just routes requests to handlers. But you'd often want to do something before and after the handler runs like logging, validations, transactions, caching, metrics, anything else. You can add this to every handler, but it's repetitive and error-prone.

A pipeline will solve what we call "cross-cutting concerns". Think of it like the middleware in ASP.NET Core, but for the mediator. Each behaviour gets a chance to run before calling the next thing in the chain, and after it returns. The handler sits at the end of the chain, like this,

![](<_resources/Building Your Own Mediator Pattern in Modern .NET/b12991d6585d202c8d2f66b32d795226_MD5.webp>)

(side note, this looks pretty, as well as being well designed!)

Let's define the contract stuff,

```csharp
public interface IPipelineBehavior<TRequest, TResponse> 
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken);
}

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
```

The next delegate is key here, it represents either the next behaviour in the chain, or the actual end delegate. Your behaviour calls next() to continue the pipeline. If you don't call it, the handler never runs (that's actually useful for short circuiting by the way, on things like a validation failure).

Now we need to modify the mediator to support the behaviours,

```csharp
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        IRequest<TResponse> request, 
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        
        // Get the handler
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var handler = _serviceProvider.GetService(handlerType) 
            ?? throw new InvalidOperationException(
                $"No handler registered for request type {requestType.Name}");

        // Get all pipeline behaviors for this request type
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var behaviors = _serviceProvider
            .GetServices(behaviorType)
            .Cast<object>()
            .Reverse()
            .ToList();

        // Build the pipeline
        var handleMethod = handlerType.GetMethod("HandleAsync")!;
        RequestHandlerDelegate<TResponse> pipeline = () =>
        {
            var task = (Task<TResponse>)handleMethod.Invoke(
                handler, 
                new object[] { request, cancellationToken })!;
            return task;
        };

        // Wrap with behaviors (in reverse order so first registered runs first)
        foreach (var behavior in behaviors)
        {
            var currentPipeline = pipeline;
            var behaviorHandleMethod = behaviorType.GetMethod("HandleAsync")!;
            
            pipeline = () =>
            {
                var task = (Task<TResponse>)behaviorHandleMethod.Invoke(
                    behavior, 
                    new object[] { request, currentPipeline, cancellationToken })!;
                return task;
            };
        }

        return await pipeline();
    }
}
```

The trick is building the pipeline from the inside out.

We start with a delegate that calls the handler, then wrap it with each behaviour in reverse order. When you execute the pipeline, the first registered behaviour runs first, calls `next()`, which cascades until it hits the handler.

And for the sake of absolute laziness, let's update the service registration to auto-discover the behaviours,

```csharp
public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(
        this IServiceCollection services, 
        params Assembly[] assemblies)
    {
        services.AddSingleton<IMediator, Mediator>();

        // Register handlers
        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            services.AddTransient(interfaceType, handlerType);
        }

        // Register pipeline behaviors
        var behaviorTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
            .ToList();

        foreach (var behaviorType in behaviorTypes)
        {
            var interfaces = behaviorType.GetInterfaces()
                .Where(i => i.IsGenericType && 
                           i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

            foreach (var interfaceType in interfaces)
            {
                services.AddTransient(interfaceType, behaviorType);
            }
        }

        return services;
    }
}
```

Now you can create behaviours that apply to all requests, specific request types, or requests that implement certain interfaces. Let's put this to use and create some.

But some important information first, the order in which we register them matter. For the ones I'm building today, a sensible order would be metrics, then validation, then caching, then transaction. You'll control the order through the DI registration, like so

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
```

### Behaviour: Validation

Probably one of the most common use cases for pipeline behaviours. You want to validate the request before it reaches the handler, and if validation fails, short-circuit and return an error.

We'll use FluentValidation for this, because I'm not writing a sub-article to just roll my own. That's a different beast outside the article scope.

So taking this example validator

```csharp
public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer ID is required");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than zero");

                item.RuleFor(x => x.Price)
                    .GreaterThan(0)
                    .WithMessage("Price must be greater than zero");
            });
    }
}
```

And the validation behaviour

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

The behaviour takes all validators registered for the request type. If there are no validators, it just calls next(). Otherwise, it runs all the validators, collects the failures, and throws if there is any.

To register validators automatically you can do something like,

```csharp
public static IServiceCollection AddMediator(
    this IServiceCollection services, 
    params Assembly[] assemblies)
{
    // ... existing registration code ...

    // Register validators from FluentValidation
    services.AddValidatorsFromAssemblies(assemblies);

    // Register the validation behavior as an open generic
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    return services;
}
```

The `AddValidatorsFromAssemblies` extension comes from the `FluentValidation.DependencyInjectionExtensions` package. It scans the assemblies and registers all `IValidator<T>` implementations.

One thing to note: the validation behaviour is registered as an open generic. That means a single registration covers all request types. The DI container will construct `ValidationBehavior<CreateOrderRequest, OrderResult>` when you send a `CreateOrderRequest`, and inject any validators that match that request type.

### Handling validation failures

Throwing `ValidationException` is one approach, but it means the caller has to catch exceptions. If you prefer returning a result object instead of throwing, you can modify it like this

```csharp
public interface IRequest<TResponse>
{
}

public record Result<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public List<string> Errors { get; init; } = new();

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(IEnumerable<string> errors) => new() { IsSuccess = false, Errors = errors.ToList() };
}
```

Then adjust the validation behaviour to return a failure result instead of throwing. This is a design choice; both approaches are valid depending on your preferences.

### Behaviour: Transactions

For commands that modify data, you often want to wrap the entire operation in a transaction. If something fails partway through, everything rolls back. No partial state.

Here's a transaction behaviour using EF Core

```csharp
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(AppDbContext dbContext, ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip for queries (we'll mark them with an interface)
        if (request is IQuery<TResponse>)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        // If there's already a transaction, don't start a new one
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            _logger.LogDebug("Using existing transaction for {RequestName}", requestName);
            return await next();
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _logger.LogDebug("Starting transaction for {RequestName}", requestName);

            var response = await next();

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogDebug("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rolling back transaction for {RequestName}", requestName);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

First, we skip queries. There's no point wrapping a read operation in a transaction (and it can actually cause performance issues with snapshot isolation). We use a marker interface `IQuery<TResponse>` to identify queries,

```csharp
public interface IQuery<TResponse> : IRequest<TResponse>
{
}

// Commands use the standard IRequest
public record CreateOrderRequest(string CustomerId, List<OrderItem> Items) 
    : IRequest<OrderResult>;

// Queries implement IQuery
public record GetOrderRequest(string OrderId) 
    : IQuery<OrderResult>;
```

Second, we check if there's already a transaction. If a handler calls another handler (nested mediator calls), we don't want to start nested transactions. EF Core doesn't support true nested transactions, so we just piggyback on the existing one.

Third, we call `SaveChangesAsync` before committing. The handler might have made changes to tracked entities without explicitly saving. This ensures everything gets persisted within the transaction.

### Transaction scope vs explicit transactions

You could also use `TransactionScope` instead of EF Core

```csharp
public async Task<TResponse> HandleAsync(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    using var scope = new TransactionScope(
        TransactionScopeOption.Required,
        new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
        TransactionScopeAsyncFlowOption.Enabled);

    var response = await next();
    
    scope.Complete();
    return response;
}
```

`TransactionScope` is more flexible since it works across multiple DbContexts and even non-EF data sources. But it's also more complex and has gotchas with async code (hence `TransactionScopeAsyncFlowOption.Enabled`).

For most applications, the explicit EF Core transaction approach is simpler and sufficient.

### Behvaiour: Caching

For idempotent queries that don't change between calls, caching can significantly reduce load on your database. The idea is simple: before executing the handler, check if we have a cached response. If so, return it. If not, execute the handler and cache the result.

First, a marker interface for cacheable requests,

```csharp
public interface ICacheableRequest<TResponse> : IRequest<TResponse>
{
    string CacheKey { get; }
    TimeSpan? CacheDuration { get; }
}
```

The request itself defines its cache key (so you can include relevant parameters) and optionally how long to cache. Here's an example,

```csharp
public record GetOrderRequest(string OrderId) : ICacheableRequest<OrderResult>
{
    public string CacheKey => $"order:{OrderId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetProductCatalogRequest() : ICacheableRequest<List<Product>>
{
    public string CacheKey => "product-catalog";
    public TimeSpan? CacheDuration => TimeSpan.FromHours(1);
}
```

Now the caching behaviour,

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only cache if the request implements ICacheableRequest
        if (request is not ICacheableRequest<TResponse> cacheableRequest)
        {
            return await next();
        }

        var cacheKey = cacheableRequest.CacheKey;

        // Try to get from cache
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cachedResponse;
        }

        _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);

        // Execute handler
        var response = await next();

        // Cache the response
        var duration = cacheableRequest.CacheDuration ?? DefaultCacheDuration;
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration)
            .SetSize(1);

        _cache.Set(cacheKey, response, cacheOptions);

        _logger.LogDebug("Cached response for {CacheKey} with duration {Duration}", cacheKey, duration);

        return response;
    }
}
```

Important: Remember to register `IMemoryCache` in your DI container

```csharp
services.AddMemoryCache();
```

### Cache invalidation

Here's the hard part. The cache doesn't know when underlying data changes. If someone creates an order, the `GetOrderRequest` cache for that order ID won't exist yet (fine), but if someone updates an order, the cached response is stale.

One approach is to publish a notification when data changes, and have a handler that invalidates relevant cache entries

```csharp
public record OrderUpdatedNotification(string OrderId) : INotification;

public class InvalidateOrderCacheHandler : INotificationHandler<OrderUpdatedNotification>
{
    private readonly IMemoryCache _cache;

    public InvalidateOrderCacheHandler(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task HandleAsync(OrderUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _cache.Remove($"order:{notification.OrderId}");
        return Task.CompletedTask;
    }
}
```

Then in your update handler,

```csharp
public async Task<OrderResult> HandleAsync(
    UpdateOrderRequest request, 
    CancellationToken cancellationToken)
{
    // ... update logic ...

    await _mediator.PublishAsync(
        new OrderUpdatedNotification(request.OrderId), 
        cancellationToken);

    return result;
}
```

This is a simple approach. For more complex scenarios, you might want a distributed cache (Redis), cache tags for bulk invalidation, or event-driven invalidation from the database itself.

A few things to watch/look out for:

1. Don't cache user specific data without including User ID in the key, otherwise one user can get someone elses data
2. Be careful with large responses, memory cache has limits
3. Cache duration is a tradeoff, longer duration means better performance, but staler data
4. Dont cache commands, only idempotent queries

### Behaviour: Metrics

Observability is always important. You want to know how long requests take, which handlers are failing, and where your bottlenecks are. A metrics behaviour collects this data for every request.

```csharp
public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<MetricsBehavior<TRequest, TResponse>> _logger;

    public MetricsBehavior(ILogger<MetricsBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation(
            "[{RequestId}] Handling {RequestName}", 
            requestId, 
            requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "[{RequestId}] Handled {RequestName} in {ElapsedMs}ms",
                requestId,
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "[{RequestId}] Failed {RequestName} after {ElapsedMs}ms",
                requestId,
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
```

This gives you basic logging with timing. For production, you'd want to integrate with a proper metrics system. Here's an example using the `System.Diagnostics.Metrics` API (available in.NET 6+)

```csharp
public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly Meter Meter = new("MyApp.Mediator");
    private static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>("mediator.requests.total");
    private static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>("mediator.requests.errors");
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>("mediator.requests.duration", "ms");

    private readonly ILogger<MetricsBehavior<TRequest, TResponse>> _logger;

    public MetricsBehavior(ILogger<MetricsBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var tags = new TagList { { "request_type", requestName } };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();
            RequestCounter.Add(1, tags);
            RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            RequestCounter.Add(1, tags);
            ErrorCounter.Add(1, tags);
            RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, tags);

            throw;
        }
    }
}
```

This exposes metrics that can be scraped by Prometheus, collected by Azure Monitor, or whatever observability platform you're using. You get:

- Total request count per request type
- Error count per request type
- Request duration histogram per request type

From these, you can derive rates, percentiles, error ratios, and build dashboards.

## Wrapping Up

We've gone from a basic mediator that just routes requests to handlers, to something with real production features: pipeline behaviours for cross-cutting concerns, validation, transactions, caching, and metrics. The core pattern is still simple, but we've layered on the functionality you'd actually need in a real application.

Is it better than just using \*insert favorite mediator library here\*? No, probably not. MediatR has years of production use, optimisations, and features we didn't cover. But building it yourself helps you understand what's happening under the hood. When you call `_mediator.Send(new CreateOrderRequest(...))`, you know exactly what happens: the mediator builds a pipeline of behaviours, wraps them around the handler, and executes the chain. No magic.

The pipeline behaviour pattern is particularly powerful. Once you have it, adding new cross-cutting concerns is just a matter of implementing another behaviour. Need request/response logging? Behaviour. Need to add correlation IDs to requests? Behaviour. Need to retry failed requests? Behaviour. The pattern scales nicely.

And honestly? For small to medium projects, a simple mediator like this is probably all you need. You get the decoupling benefits without the complexity of a full-featured library. You can extend it as needed, optimise the bits that matter, and keep the code straightforward.

That said, if you're building something serious, just use \*insert favorite mediator library here\*. Most of them handle edge cases you haven't thought of yet, and it's one less thing to maintain. But if you're learning, or you want full control, or you just enjoy building things from scratch (I guess half the fun is in understanding how it works), building your own mediator is a worthwhile exercise.
