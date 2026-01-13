---
title: 📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)
source: https://javascript.plainenglish.io/how-to-create-smart-enums-in-c-with-rich-behavior-net-9-83a6c2a19ebc
author:
  - "[[Mori]]"
published: 2025-11-07
created: 2026-01-07
description: How To Create Smart Enums in C# With Rich Behavior (.NET 9) The Guide I Wish I Had Years Ago If you’ve been writing C# for a while, you already know that enum is one of the first language features …
tags:
  - clippings
uid: a88c758a-a927-4bd7-b34e-57b8a2a86e06
updated: 2026-01-07T23:19
---

# 📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)

*The Guide I Wish I Had Years Ago*

![](<_resources/📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)/ef9f71bec8162034eef11ffb5b15fae7_MD5.webp>)

If you've been writing C# for a while, you already know that `enum` is one of the first language features we learn. It's simple, expressive, and widely used. But as soon as your domain gets more complex, you start hitting the limits of standard enums — and those limits *hurt*.

Maybe you tried to add behavior to an enum value.  
Maybe you wanted custom validation logic.  
Maybe each enum value required different properties.

And maybe you ended up with **if / switch statements everywhere**:

```c
switch (paymentMethod)
{
    case PaymentMethod.CreditCard: // do something
    case PaymentMethod.Paypal: // do something
    case PaymentMethod.Cash: // do something
}
```

Before long, your business rules are scattered across your codebase, hiding inside conditional branches like landmines waiting to explode during future refactoring.

This is what we call:

## Primitive Obsession — treating important domain concepts as mere constants instead of rich domain objects that understand themselves

And that's where **Smart Enums** come in.

# What Are Smart Enums? (Plain English)

A Smart Enum is simply a **class-based enum replacement** that:

- Represents *meaning*, not just numbers.
- Holds **behavior**, not just names.
- Keeps logic **close to the value**, not scattered.

Unlike C# enums, Smart Enums are:

![](<_resources/📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)/9be17f06ec60161946942bba58f8b1d7_MD5.webp>)

Smart Enums are a **core pattern in Domain-Driven Design (DDD).**

# Why Smart Enums Matter in Clean Architecture

In Clean Architecture, the **Domain Layer** should contain **business meaning**, not primitive values.

When you use Smart Enums, your domain becomes:

- **Clearer**
- **Harder to misuse**
- **Easier to test**
- **More expressive**

For example:

```c
Order.Pay(OrderStatus.Paid);
```

Feels more meaningful than:

```c
Order.Pay(3); // What is 3??
```

Smart Enums protect your domain from *incorrect state*.

# Creating Your First Smart Enum (Step-by-Step)

Let's convert this standard enum:

```c
public enum PaymentMethod
{
    CreditCard,
    PayPal,
    Cash
}
```

Into a **Smart Enum**:

```c
public abstract class PaymentMethod
{
    public string Name { get; }
protected PaymentMethod(string name)
    {
        Name = name;
    }
    public static readonly PaymentMethod CreditCard = new CreditCardMethod();
    public static readonly PaymentMethod PayPal = new PayPalMethod();
    public static readonly PaymentMethod Cash = new CashMethod();
    private sealed class CreditCardMethod : PaymentMethod
    {
        public CreditCardMethod() : base("Credit Card") { }
        public override decimal CalculateFee(decimal amount)
        {
            return amount * 0.03m;
        }
    }
    private sealed class PayPalMethod : PaymentMethod
    {
        public PayPalMethod() : base("PayPal") { }
        public override decimal CalculateFee(decimal amount)
        {
            return amount * 0.05m + 2m;
        }
    }
    private sealed class CashMethod : PaymentMethod
    {
        public CashMethod() : base("Cash") { }
        public override decimal CalculateFee(decimal amount)
        {
            return 0m;
        }
    }
    public abstract decimal CalculateFee(decimal amount);
}
```

Now usage:

```c
var method = PaymentMethod.PayPal;
var fee = method.CalculateFee(150m);
Console.WriteLine(fee);
```

No switches. No if-else.  
Behavior lives **inside** the type that owns it.

# Factory Support: Parsing From the Outside World

Smart Enums work well with APIs:

```c
public static PaymentMethod FromName(string name)
{
    return name.ToLower() switch
    {
        "credit" or "creditcard" => PaymentMethod.CreditCard,
        "paypal" => PaymentMethod.PayPal,
        "cash" => PaymentMethod.Cash,
        _ => throw new Exception($"Unknown Payment Method: {name}")
    };
}
```

Usage:

```c
var method = PaymentMethod.FromName("paypal");
```

Now your domain **never sees invalid values**.

# Storing Smart Enums in EF Core (.NET 9)

Create a **Value Converter**:

```c
public class PaymentMethodConverter : ValueConverter<PaymentMethod, string>
{
    public PaymentMethodConverter(ConverterMappingHints hints = null)
        : base(
              method => method.Name,
              name => PaymentMethod.FromName(name),
              hints)
    { }
}
```

Configure it in your DbContext:

```c
builder.Entity<Order>()
    .Property(o => o.PaymentMethod)
    .HasConversion(new PaymentMethodConverter());
```

You just taught EF to store Smart Enums like strings.  
No extra tables needed.

# Adding Behavior Variations

Let's say **Credit Card requires validation**:

```c
public override void Validate(string cardNumber)
{
    if (string.IsNullOrWhiteSpace(cardNumber) || cardNumber.Length < 12)
        throw new InvalidOperationException("Invalid credit card number.");
}
```

Now usage:

```c
PaymentMethod.CreditCard.Validate("123456789012");
```

The business rule travels with the domain concept.  
**This is Clean Architecture.**

# Comparing Smart Enums to C# enum

If your enum is used only as a display value → **Standard enum is fine.**

If your value **affects business logic** →  
You should **absolutely** use Smart Enums.

# Taking Smart Enums Further — Rich Domain Behavior

Let's say different shipping types change delivery time:

```c
public abstract class ShippingMethod
{
    public static readonly ShippingMethod Standard = new StandardShipping();
    public static readonly ShippingMethod Express = new ExpressShipping();
    public static readonly ShippingMethod Overnight = new OvernightShipping();
public abstract int DaysToDeliver { get; }
    private sealed class StandardShipping : ShippingMethod => public override int DaysToDeliver => 5;
    private sealed class ExpressShipping : ShippingMethod => public override int DaysToDeliver => 2;
    private sealed class OvernightShipping : ShippingMethod => public override int DaysToDeliver => 1;
}
```

Usage:

```c
var days = ShippingMethod.Express.DaysToDeliver;
```

Readable. Logical. Protected.

# Smart Enums Remove Switch Hell (Real Example)

# Bad Code

```c
if(order.PaymentMethod == PaymentMethod.PayPal)
{
    // special tax rule...
}
```

# Smart Enum Code

```c
var tax = order.PaymentMethod.CalculateTax(order.Amount);
```

No branching.  
No duplication.  
No future bugs.

# When Smart Enums Become Value Objects

If:

- Two values with same data are considered equal → **Value Object**
- Each value is a unique identity → **Smart Enum**

They often work **together**.

Example: Currency is a Smart Enum.  
Money is a Value Object that contains Currency.

# Where To Place Smart Enums in Clean Architecture

![](<_resources/📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)/53eab8e3c9bfeec0b47e52a5f022a329_MD5.webp>)

So Smart Enums are **Domain Models**.

# Conclusion (Strong and Clear)

Smart Enums are not just a pattern — they are a mindset shift.

They turn domain values into **first-class citizens**.

They:

- Remove switch-case logic
- Centralize business behavior
- Make your models expressive and safe
- Fit perfectly into Clean Architecture and DDD
- Reduce bugs by eliminating invalid states

Once you use them, you never want to go back to primitive values.

Your code becomes clearer.  
Your domain speaks with meaning.  
Your architecture becomes *solid*.

# PART 2 — Taking Smart Enums to a Professional Level

*Advanced Usage, Real Scenarios, Anti-Patterns, Integration, Testing, and Performance Considerations*

![](<_resources/📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)/7b98410228111793b02b99f2940cea1b_MD5.webp>)

Smart Enums are powerful once you understand the basics.  
But the real value appears when your domain starts growing:

- More behaviors
- More rules
- More invariants
- More edge cases
- More teams working on the same codebase

This is the stage where **standard enums collapse completely.**  
And this is exactly where Smart Enums become a **structural backbone** in Clean Architecture.

Let's go deeper.

# Smart Enums as Domain Rule Enforcers

When you design a domain model (in DDD), one of the goals is:

## Make illegal states unrepresentable

Meaning:  
Your domain objects should **not** be able to enter a bad or impossible state.

Smart Enums help enforce that.

Example: Order status.

Bad version:

```c
public enum OrderStatus
{
    Pending,     // 0
    Paid,        // 1
    Shipped,     // 2
    Delivered,   // 3
    Cancelled    // 4
}
```

This enum **allows nonsense**, such as:

- Shipping an order that was never paid
- Delivering an order that was cancelled
- Canceling an order that was delivered 3 weeks ago

With Smart Enums, each status can **define allowed transitions**:

```c
public abstract class OrderStatus
{
    public static readonly OrderStatus Pending = new PendingStatus();
    public static readonly OrderStatus Paid = new PaidStatus();
    public static readonly OrderStatus Shipped = new ShippedStatus();
    public static readonly OrderStatus Delivered = new DeliveredStatus();
    public static readonly OrderStatus Cancelled = new CancelledStatus();
public abstract OrderStatus Pay();
    public abstract OrderStatus Ship();
    public abstract OrderStatus Deliver();
    public abstract OrderStatus Cancel();
    private sealed class PendingStatus : OrderStatus
    {
        public override OrderStatus Pay() => Paid;
        public override OrderStatus Ship() => throw new InvalidOperationException("Cannot ship before payment.");
        public override OrderStatus Deliver() => throw new InvalidOperationException("Cannot deliver before shipping.");
        public override OrderStatus Cancel() => Cancelled;
    }
    private sealed class PaidStatus : OrderStatus
    {
        public override OrderStatus Pay() => throw new InvalidOperationException("Order already paid.");
        public override OrderStatus Ship() => Shipped;
        public override OrderStatus Deliver() => throw new InvalidOperationException("Cannot deliver before shipping.");
        public override OrderStatus Cancel() => Cancelled;
    }
    private sealed class ShippedStatus : OrderStatus
    {
        public override OrderStatus Pay() => throw new InvalidOperationException("Order already paid.");
        public override OrderStatus Ship() => throw new InvalidOperationException("Order already shipped.");
        public override OrderStatus Deliver() => Delivered;
        public override OrderStatus Cancel() => throw new InvalidOperationException("Cannot cancel after shipped.");
    }
    private sealed class DeliveredStatus : OrderStatus
    {
        public override OrderStatus Pay() => throw new InvalidOperationException("Order already delivered.");
        public override OrderStatus Ship() => throw new InvalidOperationException("Order already delivered.");
        public override OrderStatus Deliver() => throw new InvalidOperationException("Order already delivered.");
        public override OrderStatus Cancel() => throw new InvalidOperationException("Cannot cancel after delivery.");
    }
    private sealed class CancelledStatus : OrderStatus
    {
        public override OrderStatus Pay() => throw new InvalidOperationException("Cannot pay for a cancelled order.");
        public override OrderStatus Ship() => throw new InvalidOperationException("Cannot ship a cancelled order.");
        public override OrderStatus Deliver() => throw new InvalidOperationException("Cannot deliver a cancelled order.");
        public override OrderStatus Cancel() => throw new InvalidOperationException("Order already cancelled.");
    }
}
```

Now, your **domain enforces correctness automatically.**  
No conditions. No pipes. No guards repeated across services.

Your Order entity becomes *clean*:

```c
public void Pay()
{
    Status = Status.Pay();
}
```

That's all.

If it's legal → transition happens.  
If it's illegal → exception prevents corruption.

Your code now **models real business laws.**  
It stops being just "data."  
It becomes **knowledge.**

# Smart Enums Encourage Ubiquitous Language

DDD is not just about code.  
It's about **shared meaning** between:

- Developers
- Stakeholders
- Business experts
- Product owners

When you use Smart Enums:

![](<_resources/📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)/4a1f102ee5251843518f1673ed181bd4_MD5.webp>)

Your code becomes **communication**, not just instructions.

This is how teams write software that **lasts**.

# Smart Enums vs. Polymorphism vs. Strategy

Some developers ask:

## "Should I just use a Strategy pattern instead?"

You *can*, but Smart Enums handle a specific niche where:

- Values are known and finite
- The values themselves define behavior
- Identity of the value matters, not instance variability

Example difference:

![](<_resources/📰 How To Create Smart Enums in C With Rich Behavior (.NET 9)/9f75db31cbd58bd09c3c8fae22a2e8e8_MD5.webp>)

Both are good. They solve **different** problems.

# Serialization — JSON Support

To serialize Smart Enums in **Web API**:

```c
public class Order
{
    public Guid Id { get; init; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
}
```

Add a custom converter:

```c
public class OrderStatusJsonConverter : JsonConverter<OrderStatus>
{
    public override OrderStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => OrderStatus.FromName(reader.GetString());
public override void Write(Utf8JsonWriter writer, OrderStatus value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
```

Configure globally:

```c
builder.Services.Configure<MvcOptions>(options =>
{
    options.JsonOptions.JsonSerializerOptions.Converters.Add(new OrderStatusJsonConverter());
});
```

Now your Smart Enum works seamlessly with HTTP input/output.

# Testing Smart Enums (Unit Tests)

You test behaviors, not values.

```c
[Fact]
public void PendingOrder_CanBeCancelled()
{
    var status = OrderStatus.Pending;
    var result = status.Cancel();
    Assert.Equal(OrderStatus.Cancelled, result);
}
[Fact]
public void ShippedOrder_CannotBeCancelled()
{
    var status = OrderStatus.Shipped;
    Assert.Throws<InvalidOperationException>(() => status.Cancel());
}
```

This is clean.  
This is predictable.  
This is how DDD code should feel.

# Performance Considerations

Smart Enums are classes, not IL enums.

This means:

AspectCostAllocation?No — singletonsMethod calls?Virtual dispatch — extremely cheapEF Core persistence?Fast, via converters

Smart Enums are **not slow.**  
They are **domain-accurate.**

# Real Project Folder Placement

```c
/Domain
   /Enums
      PaymentMethod.cs
      OrderStatus.cs
   /Entities
      Order.cs
```

Not in:

- API
- Infrastructure
- DTOs

Because domain rules belong to the domain.

# Part 2 Conclusion

You have now moved from:

✅ Understanding Smart Enum basics  
✅ To structuring real business rules with them  
✅ To integrating them into Clean Architecture  
✅ To persistence  
✅ To testing  
✅ To team communication  
✅ To performance clarity

We are **not done.**

# conclusion

Smart Enums are not just a cleaner alternative to standard C# enums — they are a way to express *meaning* directly in your code. When we move behavior into the value itself, our software becomes easier to reason about, harder to misuse, and much closer to the real rules of the domain. Instead of scattering `if` checks, guard clauses, and business logic across services and handlers, Smart Enums let us define the truth once — at the source.

This leads to something much more important than nice code formatting: **confidence.**  
Confidence that an invalid state cannot slip in silently.  
Confidence that when a rule changes, there is one place to update.  
Confidence that new team members can look at the code and *understand the business*, not just the syntax.

By applying Smart Enums within **Clean Architecture + DDD**, we intentionally push business meaning into the **Domain layer**, where it belongs. We gain stronger boundaries, clearer invariants, simpler testing, and code that reads like it *knows what it is doing.* This is the shift from writing programs to modeling systems — the shift from *procedural condition-driven logic* to *rich domain language.*

And in real projects, that shift is transformative.

When your codebase reflects the real world, everything else gets easier:  
features, refactoring, testing, onboarding, and even communication with non-technical stakeholders. Smart Enums help make that possible — not through magic, but through clarity, structure, and intentional design.

The more your system grows, the more this approach pays off.

Because good architectures don't just *work* — they *age well.*
