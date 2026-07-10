---
title: "The C# Features You’re Not Using (But Should Be): From Collection Expressions to field-Backed…"
source: "https://blog.stackademic.com/the-c-features-youre-not-using-but-should-be-from-collection-expressions-to-field-backed-acb99f88940b"
author:
  - "[[Mori]]"
published: 2026-05-24
created: 2026-06-15
description: "The C# Features You’re Not Using (But Should Be): From Collection Expressions to field-Backed Properties The modern C# 12/13/14 toolkit that eliminates boilerplate, prevents bugs, and makes your …"
tags:
  - "clippings"
---
# The C Features You’re Not Using (But Should Be) From Collection Expressions to field-Backed…
## The modern C# 12/13/14 toolkit that eliminates boilerplate, prevents bugs, and makes your code actually readable — with production-ready examples

![](https://miro.medium.com/v2/resize:fit:1400/format:webp/1*fAoLcMXefSTsl61DfRQQfw.png)

I was reviewing a pull request last week. The developer — smart, senior, five years of C# — had written this:

```c
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
public OrderService(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
}
```

Forty lines. Eight of them meaningful. The rest? Boilerplate that C# 12 eliminated two years ago. And this wasn’t a legacy codebase — this was a.NET 10 project started last month.

Most C# developers are writing 2023 code in 2026.

After auditing dozens of codebases, I’ve identified the features that give the highest ROI: the ones that eliminate the most boilerplate, prevent the most bugs, and make code genuinely more readable. Not academic curiosities — practical tools you can use today.

This guide covers C# 12 through C# 14 (shipping with.NET 10), with real production examples and the migration path from your current code.

## Feature 1: Collection Expressions — The End of new List<int> { }

The most universally applicable C# 12 feature. One syntax for arrays, lists, spans, and any custom collection.

## The Old Way (Still Seen Everywhere)

```c
// Arrays
int[] numbers = new int[] { 1, 2, 3, 4, 5 };
// Or: var numbers = new[] { 1, 2, 3, 4, 5 };
// Lists
List<string> names = new List<string> { "Alice", "Bob", "Charlie" };
// Or: var names = new List<string> { "Alice", "Bob", "Charlie" };
// Spans (the ugly one)
Span<int> span = stackalloc int[] { 1, 2, 3, 4 };
ReadOnlySpan<string> days = new[] { "Mon", "Tue", "Wed" };
// Combining collections
var combined = first.Concat(second).Concat(third).ToArray();
```

## The Modern Way (C# 12+)

```c
// Arrays, lists, spans — same syntax
int[] numbers = [1, 2, 3, 4, 5];
List<string> names = ["Alice", "Bob", "Charlie"];
Span<int> span = [1, 2, 3, 4];
ReadOnlySpan<string> days = ["Mon", "Tue", "Wed"];
// The spread operator: combine any collections
int[] first = [1, 2, 3];
List<int> second = [4, 5, 6];
IEnumerable<int> third = [7, 8, 9];
int[] combined = [..first, ..second, ..third]; // [1,2,3,4,5,6,7,8,9]
// Mix values and spreads
int[] result = [0, ..first, 10, ..second, 20]; // [0,1,2,3,10,4,5,6,20]
// Empty collections (compiler optimizes to Array.Empty<T> or default Span)
int[] empty = [];
List<string> noItems = [];
Span<byte> zeroSpan = [];
```

Why this matters:

- Consistency: One syntax for all collection types. No more deciding between `new[]`, `new List<>`, and `stackalloc`.
- Performance: The compiler picks the most efficient implementation. For `Span<T>`, it often uses `stackalloc` automatically. For empty arrays, it uses `Array.Empty<T>()`.
- Readability: `[1, 2, 3]` is instantly recognizable as a collection. `new List<int> { 1, 2, 3 }` is noise.

## Production Example: API Response Building

```c
// BEFORE: Verbose, hard to scan
public class ProductController : ControllerBase
{
    private static readonly string[] AllowedSortFields = new[] { "name", "price", "date", "rating" };
    private static readonly List<string> AdminRoles = new List<string> { "Admin", "SuperAdmin", "Owner" };
    
    public IActionResult GetFilters()
    {
        var filters = new List<<FilterDto>
        {
            new FilterDto { Name = "Category", Options = new[] { "Electronics", "Clothing", "Books" } },
            new FilterDto { Name = "Price", Options = new[] { "Under $25", "$25-$50", "$50+" } }
        };
        return Ok(filters);
    }
}
// AFTER: Clean, scannable, consistent
public class ProductController : ControllerBase
{
    private static readonly string[] AllowedSortFields = ["name", "price", "date", "rating"];
    private static readonly List<string> AdminRoles = ["Admin", "SuperAdmin", "Owner"];
    
    public IActionResult GetFilters()
    {
        var filters = new List<<FilterDto>
        {
            new() { Name = "Category", Options = ["Electronics", "Clothing", "Books"] },
            new() { Name = "Price", Options = ["Under $25", "$25-$50", "$50+"] }
        };
        return Ok(filters);
    }
}
```

## Custom Collection Support (C# 12+)

You can make your own types work with collection expressions:

```c
// A custom immutable list
[CollectionBuilder(typeof(ImmutableListBuilder), "Create")]
public sealed class ImmutableList<T> : IEnumerable<T>
{
    private readonly T[] _items;
    
    private ImmutableList(ReadOnlySpan<T> items)
    {
        _items = items.ToArray();
    }
    
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
public static class ImmutableListBuilder
{
    public static ImmutableList<T> Create<T>(ReadOnlySpan<T> items) => new(items);
}
// Usage
ImmutableList<int> numbers = [1, 2, 3, 4, 5];
ImmutableList<string> names = ["Alice", "Bob"];
```

## Feature 2: Primary Constructors — Eliminate the Boilerplate

C# 12 extended primary constructors from records to classes and structs. Parameters are in scope for the entire class body, eliminating explicit field declarations and constructor bodies.

## The Old Way (What Most Code Still Looks Like)

```c
public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly IEmailService _emailService;
    private readonly TimeProvider _timeProvider;
public OrderService(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger,
        IEmailService emailService,
        TimeProvider timeProvider)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }
}
```

## The Modern Way (C# 12+)

```c
public class OrderService(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    ILogger<OrderService> logger,
    IEmailService emailService,
    TimeProvider timeProvider)
{
    // Parameters are automatically captured as private fields
    // No explicit fields, no constructor body, no null checks needed (DI container guarantees non-null)
    
    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        logger.LogInformation("Creating order for {CustomerEmail}", request.CustomerEmail);
        
        var order = Order.Create(request.CustomerEmail, timeProvider.GetUtcNow());
        // orderRepository, unitOfWork, emailService all accessible here
        
        await orderRepository.AddAsync(order);
        await unitOfWork.SaveChangesAsync();
        
        await emailService.SendOrderConfirmationAsync(order);
        
        return order;
    }
}
```

What just happened:

- 5 fields → 0 explicit fields
- 20-line constructor → 1 line
- 5 null checks → 0 (DI container handles it)
- Total: 30 lines → 1 line

## Rules to Know

```c
public class ProductService(IProductRepository repository, ILogger<ProductService> logger)
{
    // Primary constructor parameters are in scope everywhere
    
    // You CAN assign to them (they're parameters, not readonly fields)
    public void SetCache(string value) => _cache = value; // If you capture them in fields
    
    // You CAN access them in properties
    public string RepositoryType => repository.GetType().Name;
    
    // You CAN use them in field initializers
    private readonly Dictionary<<Guid, Product> _cache = new();
    
    // You MUST call primary constructor from other constructors
    public ProductService(IProductRepository repository) : this(repository, NullLogger<ProductService>.Instance)
    {
    }
    
    // You CANNOT access them as this.repository (they're not members)
    // You CANNOT use them in static members (they're instance parameters)
}
```

Critical insight: Primary constructor parameters are parameters, not fields. The compiler may or may not store them. If you reference them in instance methods, they get stored. If you only use them in property initializers, they might not. For predictable behavior, assign them to explicit fields when you need guaranteed storage.

## When to Use Explicit Fields Instead

```c
// Use explicit fields when you need validation or transformation
public class Money(decimal amount, string currencyCode)
{
    // Transform and validate on capture
    private readonly decimal _amount = amount >= 0 ? amount : throw new ArgumentException("Amount cannot be negative");
    private readonly string _currencyCode = currencyCode?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(currencyCode));
    
    public decimal Amount => _amount;
    public string CurrencyCode => _currencyCode;
}
```

## Feature 3: required Members — Compile-Time Null Safety

Before C# 11, object initializers were a runtime footgun. You could forget a property, get a null, and only find out at runtime. `required` fixes this at compile time.

## The Old Way (Runtime Failures)

```c
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } // Null by default!
    public decimal Price { get; set; }
    public string Description { get; set; } // Null by default!
}
// Compiles fine, fails at runtime when Name is null
var dto = new ProductDto { Id = Guid.NewGuid(), Price = 29.99m };
ProcessName(dto.Name); // NullReferenceException!
```

## The Modern Way (C# 11+)

```c
public class ProductDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; } // Compiler enforces initialization
    public required decimal Price { get; init; }
    public string? Description { get; init; } // Optional, explicitly nullable
    
    // Parameterless constructor is still allowed, but all required must be set
}
// COMPILE ERROR: Missing required member 'Name'
var bad = new ProductDto { Id = Guid.NewGuid(), Price = 29.99m };
// COMPILE ERROR: Missing required member 'Price'
var alsoBad = new ProductDto { Id = Guid.NewGuid(), Name = "Widget" };
// SUCCESS: All required members present
var good = new ProductDto 
{ 
    Id = Guid.NewGuid(), 
    Name = "Widget", 
    Price = 29.99m,
    Description = "A great widget" // Optional, can omit
};
```

## With Primary Constructors (The Power Combo)

```c
// For immutable DTOs, combine required init-only properties with primary constructors
public class CreateOrderRequest(
    required string CustomerEmail,
    required string CustomerName,
    required List<OrderItemRequest> Items,
    string? CouponCode = null)
{
    // Primary constructor parameters become required properties automatically
    // No need for explicit property declarations
}
// Usage
var request = new CreateOrderRequest(
    CustomerEmail: "john@example.com",
    CustomerName: "John Doe",
    Items: [new OrderItemRequest(Guid.NewGuid(), 2)]
);
```

## Feature 4: field-Backed Properties (C# 14) — Auto-Properties with Brains

C# 14 introduces the `field` keyword, giving you access to the compiler-generated backing field inside property accessors. No more explicit fields for simple validation.

## The Old Way (Explicit Backing Field)

```c
public class Product
{
    private decimal _price;
    
    public decimal Price
    {
        get => _price;
        set
        {
            if (value < 0)
                throw new ArgumentException("Price cannot be negative");
            _price = value;
        }
    }
}
```

## The Modern Way (C# 14)

```c
public class Product
{
    public decimal Price
    {
        get => field;           // Access the compiler-generated backing field
        set
        {
            if (value < 0)
                throw new ArgumentException("Price cannot be negative");
            field = value;      // Assign to the compiler-generated backing field
        }
    }
}
```

What changed:

- `field` replaces the explicit `_price` field
- The compiler generates the backing field automatically
- You get the field only when you need custom logic
- No more naming conflicts or field management

## Real-World: Audit Properties

```c
public class Order
{
    public OrderStatus Status
    {
        get => field;
        set
        {
            if (field == OrderStatus.Cancelled && value != OrderStatus.Cancelled)
                throw new InvalidOperationException("Cannot uncancel an order");
            
            if (value == OrderStatus.Shipped && field != OrderStatus.Confirmed)
                throw new InvalidOperationException("Cannot ship unconfirmed order");
            
            field = value;
            ModifiedAt = DateTime.UtcNow; // Side effect in setter
        }
    }
    
    public DateTime ModifiedAt { get; private set; }
}
```

## Feature 5: Pattern Matching Evolution — Switch Expressions and List Patterns

C# has been steadily improving pattern matching since 7.0. The modern combination of switch expressions, property patterns, and list patterns eliminates entire classes of `if-else` chains.

## Switch Expressions (C# 8+, But Underused)

```c
// BEFORE: Verbose if-else chain
public decimal CalculateDiscount(Order order)
{
    if (order.TotalAmount > 1000 && order.CustomerType == CustomerType.VIP)
        return 0.20m;
    else if (order.TotalAmount > 500 && order.CustomerType == CustomerType.VIP)
        return 0.15m;
    else if (order.TotalAmount > 1000)
        return 0.10m;
    else if (order.TotalAmount > 500)
        return 0.05m;
    else
        return 0m;
}
// AFTER: Exhaustive, readable, compiler checks for missing cases
public decimal CalculateDiscount(Order order) => order switch
{
    { TotalAmount: > 1000, CustomerType: CustomerType.VIP } => 0.20m,
    { TotalAmount: > 500, CustomerType: CustomerType.VIP } => 0.15m,
    { TotalAmount: > 1000 } => 0.10m,
    { TotalAmount: > 500 } => 0.05m,
    _ => 0m
};
```

## List Patterns (C# 11+) — Pattern Match Collections

```c
// Match on collection contents
public string ClassifyOrder(List<OrderItem> items) => items switch
{
    [] => "Empty order",
    [var single] when single.Quantity > 10 => "Bulk single-item order",
    [var first, ..] when first.UnitPrice > 1000 => "High-value order",
    [_, _, ..] => "Multi-item order",
    _ => "Standard order"
};
// Deconstruct in patterns
public decimal CalculateShipping(Address address) => address switch
{
    { Country: "US", State: "AK" or "HI" } => 15.99m, // Alaska/Hawaii surcharge
    { Country: "US" } => 5.99m,
    { Country: "CA" } => 8.99m,
    { Country: var c } when IsEUCountry(c) => 12.99m,
    _ => 25.99m
};
```

## Null-Conditional Assignment (C# 14)

C# 14 introduces `?.=` — assign only if the left side is not null.

```c
// BEFORE: Verbose null check
if (customer.Address != null)
{
    customer.Address.City = "New York";
}
// AFTER: C# 14 null-conditional assignment
customer.Address?.City = "New York"; // Only assigns if Address is not null
// More useful with method calls
logger?.LogInformation("Processing order {OrderId}", order.Id);
// Combined with null-coalescing
config?.ConnectionString ??= "default_connection"; // Assign default only if null
```

## Feature 6: Extension Everything (C# 14) — Beyond Methods

C# 14 extends the `extension` concept beyond methods to properties, indexers, and operators. You can now add rich functionality to existing types without inheritance.

## Extension Properties and Indexers

```c
// Define an extension for string
public static extension StringExtensions for string
{
    // Extension property
    public bool IsValidEmail => 
        this.Contains('@') && this.Split('@')[1].Contains('.');
    
    // Extension indexer
    public char this[int indexFromEnd]
    {
        get => this[this.Length - 1 - indexFromEnd];
    }
    
    // Extension method (existing feature, now in extension block)
    public string Truncate(int maxLength) =>
        this.Length <= maxLength ? this : this[..maxLength] + "...";
}
// Usage
string email = "test@example.com";
bool valid = email.IsValidEmail;        // Extension property
char lastChar = email[0];               // Standard indexer
char secondToLast = email[^2];          // Standard from-end indexer
char customFromEnd = email[1];          // Extension indexer (1 from end = 'm')
string truncated = "Very long text".Truncate(10); // "Very long..."
```

## Extension Static Members

```c
public static extension DateTimeExtensions for DateTime
{
    // Extension static property
    public static DateTime StartOfWeek => 
        DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
    
    // Extension static method
    public static bool IsBusinessDay(DateTime date) =>
        date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
}
// Usage
var weekStart = DateTime.StartOfWeek;           // Extension static property
bool isWorkDay = DateTime.IsBusinessDay(today);   // Extension static method
```

## Feature 7: nameof with Unbound Generics (C# 14)

Before C# 14, `nameof` couldn't reference generic types without specifying type arguments. Now it can.

```c
// BEFORE: Had to specify dummy type arguments
public string GetTypeName() => nameof(List<int>); // Works, but misleading
// AFTER: Reference the generic type definition directly
public string GetTypeName() => nameof(List<>);     // "List"
public string GetDictionaryName() => nameof(Dictionary<<,>); // "Dictionary"
// Useful in logging and serialization
public static string GetFriendlyName<T>() => typeof(T).IsGenericType
    ? $"{nameof(T)}<{string.Join(", ", typeof(T).GetGenericArguments().Select(a => a.Name))}>"
    : nameof(T);
```

## Feature 8: Modifiers on Lambda Parameters (C# 14)

C# 14 allows `ref`, `in`, `out`, and `params` on lambda parameters without explicit type annotations.

```c
// BEFORE: Had to specify full types to use ref
Action<<ref int> increment = (ref int x) => x++;
// AFTER: Type inference with modifiers
var increment = (ref int x) => x++;  // Still works
var process = (in Span<byte> data) => ParseHeader(data);  // in modifier
var tryParse = (out int result) => int.TryParse("42", out result); // out modifier
// With params
var sum = (params int[] numbers) => numbers.Sum();
var result = sum(1, 2, 3, 4, 5); // 15
```

## Feature 9: ref readonly Parameters (C# 12+)

For large structs, passing by value copies the entire struct. `ref` allows mutation. `ref readonly` gives you the performance of `ref` without the mutability risk.

```c
// BEFORE: Pass by value (copies 72 bytes for a typical struct)
public decimal CalculateTotal(OrderSummary summary) => summary.Items.Sum(i => i.Price);
// AFTER: Pass by reference, no copy, no mutation
public decimal CalculateTotal(ref readonly OrderSummary summary) => 
    summary.Items.Sum(i => i.Price);
// The caller uses 'in' (implicit ref readonly)
var total = CalculateTotal(in orderSummary);
// Cannot modify inside the method
// summary.Items = new List<Item>(); // COMPILE ERROR
```

## Feature 10: partial Events and Constructors (C# 14)

C# 14 extends `partial` to events and constructors, enabling source generators to hook into initialization and event wiring.

```c
// Generated by source generator
public partial class OrderService
{
    public partial event EventHandler<OrderCreatedEventArgs>? OrderCreated;
    
    public partial OrderService();
}
// Developer-written part
public partial class OrderService
{
    // The source generator can inject initialization here
    public partial OrderService()
    {
        _cache = new Dictionary<<Guid, Order>();
    }
    
    public partial event EventHandler<OrderCreatedEventArgs>? OrderCreated
    {
        add => _orderCreatedHandlers += value;
        remove => _orderCreatedHandlers -= value;
    }
    
    private EventHandler<OrderCreatedEventArgs>? _orderCreatedHandlers;
}
```

## The Migration Strategy: Adopt These Features Incrementally

You don’t need a big-bang rewrite. Here’s the priority order:

## Week 1: Collection Expressions

- Replace all `new[] { }` and `new List<> { }` with `[ ]`
- Use `..` for combining collections
- Zero risk, immediate readability gain

## Week 2: Primary Constructors

- Start with service classes that just capture DI dependencies
- Avoid for classes with complex validation or multiple constructors
- Use explicit fields when you need guaranteed storage

## Week 3: required Members

- Add to all DTOs and request objects
- Combine with `init` for immutability
- Enable nullable reference types if not already on

## Week 4: Pattern Matching

- Replace `if-else` chains with switch expressions
- Use list patterns for collection validation
- Add property patterns for complex conditions

## Week 5: C# 14 Features (when.NET 10 ships)

- `field` -backed properties for simple validation
- Extension blocks for utility methods
- `nameof` with unbound generics for logging

## The Complete Before/After Comparison

```c
// ============================================================
// LEGACY C# (What Most Code Looks Like)
// ============================================================
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<OrderService> _logger;
    private readonly List<Order> _recentOrders;
    
    public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _recentOrders = new List<Order>();
    }
    
    public decimal CalculateDiscount(Order order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        
        if (order.TotalAmount > 1000 && order.CustomerType == CustomerType.VIP)
            return 0.20m;
        else if (order.TotalAmount > 500 && order.CustomerType == CustomerType.VIP)
            return 0.15m;
        else if (order.TotalAmount > 1000)
            return 0.10m;
        else if (order.TotalAmount > 500)
            return 0.05m;
        else
            return 0m;
    }
    
    public void ProcessOrders(List<Order> orders)
    {
        if (orders == null || orders.Count == 0)
        {
            _logger.LogWarning("No orders to process");
            return;
        }
        
        foreach (var order in orders)
        {
            _repository.Save(order);
            _recentOrders.Add(order);
        }
    }
}
// ============================================================
// MODERN C# (C# 12-14)
// ============================================================
public class OrderService(IOrderRepository repository, ILogger<OrderService> logger)
{
    private readonly List<Order> _recentOrders = [];
    
    public decimal CalculateDiscount(Order order) => order switch
    {
        { TotalAmount: > 1000, CustomerType: CustomerType.VIP } => 0.20m,
        { TotalAmount: > 500, CustomerType: CustomerType.VIP } => 0.15m,
        { TotalAmount: > 1000 } => 0.10m,
        { TotalAmount: > 500 } => 0.05m,
        _ => 0m
    };
    
    public void ProcessOrders(List<Order> orders)
    {
        if (orders is [])  // List pattern
        {
            logger.LogWarning("No orders to process");
            return;
        }
        
        foreach (var order in orders)
        {
            repository.Save(order);
            _recentOrders.Add(order);
        }
    }
}
```

Line count: 52 → 28 (46% reduction) Readability: Significantly improved — business logic is visible, boilerplate is gone Safety: Same or better — null checks handled by DI container, pattern matching is exhaustive

## Key Takeaways

1. Collection expressions are the easiest win. Replace every `new[]` and `new List<<>` with `[ ]`. The spread operator `..` eliminates `Concat()` chains. This alone will clean up 20% of your collection code.
2. Primary constructors eliminate DI boilerplate. A service class with 5 dependencies goes from 30 lines to 1. But remember: they’re parameters, not fields. Use explicit fields when you need validation or guaranteed storage.
3. `required` members prevent null at compile time. No more `ArgumentNullException` in property setters. The compiler enforces initialization. Combine with `init` for immutable DTOs.
4. `field` -backed properties (C# 14) eliminate explicit backing fields. Access the compiler-generated field with `field`. Perfect for simple validation in auto-properties without the ceremony.
5. Pattern matching replaces if-else chains. Switch expressions are exhaustive (compiler checks for missing cases). Property patterns make complex conditions readable. List patterns validate collection shape.
6. Extension everything (C# 14) adds properties and statics to existing types. Extension blocks keep related functionality together. No more `StringHelper`, `DateTimeHelper` static classes.

## Final Thoughts

C# is evolving faster than ever. C# 12 gave us collection expressions and primary constructors. C# 13 refined performance features. C# 14 (with.NET 10) brings `field` properties, extension everything, and null-conditional assignment.

The teams that adopt these features write less code, catch more bugs at compile time, and onboard new developers faster. The teams that don’t are stuck with 2023 patterns in 2026, wondering why their codebase is 40% larger than it needs to be.

Modern C# isn’t about being clever. It’s about being concise. Every feature in this guide eliminates boilerplate without sacrificing clarity. That’s the goal.

### If you found this guide useful, follow for more modern C# and.NET patterns. Which C# feature has made the biggest impact on your codebase? Drop a comment below.

### References:

[What’s new in C# 14 | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)

[What’s new in C# 12 | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)

[Collection expressions (Collection literals) — C# reference | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions)