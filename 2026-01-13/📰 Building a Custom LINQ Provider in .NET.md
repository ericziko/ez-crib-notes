---
title: 📰 Building a Custom LINQ Provider in .NET
source: https://medium.com/@jordansrowles/building-a-custom-linq-provider-in-net-a987dc983381
author:
  - "[[Jordan Rowles]]"
published: 2025-12-30
created: 2026-01-07
description: Building a Custom LINQ Provider in .NET LINQ providers are one of those dark corners of .NET that most developers never need to touch. And honestly? That’s probably for the best. But if you’re …
tags:
  - clippings
updated: 2026-01-07T22:32
uid: e977ed1f-b887-4d97-89f9-afb6f7ef6da3
---

# 📰 Building a Custom LINQ Provider in .NET

LINQ providers are one of those dark corners of.NET that most developers never need to touch. And honestly? That's probably for the best. But if you're reading this, you've probably hit one of those rare cases where the existing providers don't quite fit, or you're just curious about how Entity Framework Core, LINQ to XML, and all those other query providers actually work under the hood.

The good news, building a LINQ provider isn't as horrifying as it sounds. The bad news, it's still pretty horrifying. But let's crack on.

## What is a LINQ provider anyway?

When you write a LINQ query, you're not actually executing code in the traditional sense. You're building an expression tree, a data structure that represents what you want to do, not how to do it. The LINQ provider's job is to take that expression tree and translate it into something that actually executes.

For `IEnumerable<T>`, the "translation" is trivial, it just compiles the expression to IL and runs it. But for `IQueryable<T>`, the provider can do whatever it wants with that expression tree. Entity Framework translates it to SQL. MongoDB's driver translates it to their query format. You could translate it to a REST API call, a file system search, or interpretive dance instructions if you were so inclined.

The key interfaces are:

- `IQueryable<T>` - The queryable data source
- `IQueryProvider` - The thing that executes queries
- `Expression` - The tree representing the query

We need to look at each one.

## The Core Interfaces

### `IQueryable<T>`

```csharp
public interface IQueryable<T> : IQueryable, IEnumerable<T>
{
    // Inherited from IQueryable
    Expression Expression { get; }
    Type ElementType { get; }
    IQueryProvider Provider { get; }
}
```

That's it. Three properties. `Expression` holds the current state of the query (what WHERE clauses have been added, what ORDER BY, etc). `ElementType` is just `typeof(T)`. `Provider` is what actually does the work.

When you chain LINQ methods, each one returns a new `IQueryable<T>` with an updated `Expression`. The actual execution happens when you enumerate (foreach, ToList, etc, …).

### IQueryProvider

```csharp
public interface IQueryProvider
{
    IQueryable CreateQuery(Expression expression);
    IQueryable<TElement> CreateQuery<TElement>(Expression expression);
    object? Execute(Expression expression);
    TResult Execute<TResult>(Expression expression);
}
```

`CreateQuery` is called when you chain LINQ methods, it wraps the new expression in a fresh queryable. `Execute` is called when you actually want results.

The non-generic versions exist for edge cases and interop. In practice, you'll implement all four but the generic versions do the real work.

### Expression Trees

This is where it gets interesting. When you write something like,

```csharp
users.Where(u => u.Age > 18)
```

The lambda `u => u.Age > 18` isn't compiled to IL. It's converted to an `Expression<Func<User, bool>>`, which is a data structure you can inspect at runtime.

![](<_resources/📰 Building a Custom LINQ Provider in .NET/50ddc29665ec58e2db6c646823f3751d_MD5.webp>)

You can walk this tree, inspect each node, and decide what to do with it. That's how EF Core knows to generate `WHERE Age > 18` instead of loading every user into memory.

## Building a Simple Query Provider

Let's build something concrete. We'll create a provider that queries an in-memory list but logs every operation. Not particularly useful in production, but it demonstrates the mechanics.

First, our queryable implementation,

```csharp
public class LoggingQueryable<T> : IQueryable<T>, IOrderedQueryable<T>
{
    private readonly LoggingQueryProvider _provider;
    private readonly Expression _expression;

    public LoggingQueryable(IEnumerable<T> source)
    {
        _provider = new LoggingQueryProvider(source.AsQueryable());
        _expression = Expression.Constant(this);
    }

    internal LoggingQueryable(LoggingQueryProvider provider, Expression expression)
    {
        _provider = provider;
        _expression = expression;
    }

    public Type ElementType => typeof(T);
    public Expression Expression => _expression;
    public IQueryProvider Provider => _provider;

    public IEnumerator<T> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

Nothing fancy here. We hold a reference to our provider and an expression. The constructor that takes `IEnumerable<T>` is the entry point. That's how you wrap a data source.

Now the provider itself

```csharp
public class LoggingQueryProvider : IQueryProvider
{
    private readonly IQueryable _source;

    public LoggingQueryProvider(IQueryable source)
    {
        _source = source;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetSequenceElementType();
        var queryableType = typeof(LoggingQueryable<>).MakeGenericType(elementType);
        var ctor = queryableType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(LoggingQueryProvider), typeof(Expression) },
            null);
        return (IQueryable)ctor!.Invoke(new object[] { this, expression });
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        Console.WriteLine($"[LINQ] Creating query: {expression}");
        return new LoggingQueryable<TElement>(this, expression);
    }

    public object? Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        Console.WriteLine($"[LINQ] Executing: {expression}");
        
        // Rewrite the expression to use our inner source
        var rewriter = new SourceRewriter(_source);
        var rewritten = rewriter.Visit(expression);
        
        return _source.Provider.Execute<TResult>(rewritten);
    }
}
```

The magic happens in `Execute`. We take the incoming expression, rewrite it to point at our underlying data source, then delegate to that source's provider.

We need a helper method to extract the element type from a sequence type

```csharp
public static class TypeExtensions
{
    public static Type GetSequenceElementType(this Type type)
    {
        var enumerableType = type.GetInterfaces()
            .Concat(new[] { type })
            .FirstOrDefault(t => t.IsGenericType && 
                                  t.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        
        return enumerableType?.GetGenericArguments()[0] ?? type;
    }
}
```

And the expression rewriter,

```csharp
public class SourceRewriter : ExpressionVisitor
{
    private readonly IQueryable _newSource;

    public SourceRewriter(IQueryable newSource)
    {
        _newSource = newSource;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        // Replace LoggingQueryable<T> with our actual source
        if (node.Value is IQueryable queryable && 
            queryable.GetType().IsGenericType &&
            queryable.GetType().GetGenericTypeDefinition() == typeof(LoggingQueryable<>))
        {
            return Expression.Constant(_newSource);
        }
        
        return base.VisitConstant(node);
    }
}
```

Now you can use it, like so,

```csharp
var users = new List<User>
{
    new("Alice", 25),
    new("Bob", 17),
    new("Charlie", 30)
};

var queryable = new LoggingQueryable<User>(users);

var adults = queryable
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .ToList();

// Output:
// [LINQ] Creating query: System.Collections.Generic.List\`1[User].Where(u => (u.Age >= 18))
// [LINQ] Creating query: ...Where(...).OrderBy(u => u.Name)
// [LINQ] Executing: ...Where(...).OrderBy(...).ToList()
```

Each chained method creates a new query expression, and the final `ToList()` triggers execution.

## The Expression Visitor Pattern

`ExpressionVisitor` is the workhorse of custom LINQ providers. It's a base class that walks expression trees, letting you override specific node types to transform them.

Here's the basic structure of the visitor,

```csharp
public class MyExpressionVisitor : ExpressionVisitor
{
    // Override specific Visit methods to handle different node types
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Handle +, -, ==, >, <, &&, ||, etc.
        return base.VisitBinary(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Handle method calls like .Where(), .Select(), .Contains()
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // Handle property/field access like u.Age or u.Name
        return base.VisitMember(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        // Handle literal values
        return base.VisitConstant(node);
    }
}
```

The visitor pattern is recursive. When you call `Visit(expression)`, it figures out what type of expression it is, calls the appropriate `VisitXxx` method, and by default, recursively visits all children.

Let's build something more useful, a visitor that translates LINQ expressions to a simple query string format

```csharp
public class QueryStringBuilder : ExpressionVisitor
{
    private readonly StringBuilder _sb = new();
    private readonly Dictionary<string, object> _parameters = new();
    private int _parameterIndex = 0;

    public string Query => _sb.ToString();
    public IReadOnlyDictionary<string, object> Parameters => _parameters;

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case "Where":
                Visit(node.Arguments[0]); // Visit the source first
                _sb.Append(" WHERE ");
                Visit(node.Arguments[1]); // Then the predicate
                break;
                
            case "OrderBy":
                Visit(node.Arguments[0]);
                _sb.Append(" ORDER BY ");
                Visit(node.Arguments[1]);
                _sb.Append(" ASC");
                break;
                
            case "OrderByDescending":
                Visit(node.Arguments[0]);
                _sb.Append(" ORDER BY ");
                Visit(node.Arguments[1]);
                _sb.Append(" DESC");
                break;
                
            case "Take":
                Visit(node.Arguments[0]);
                _sb.Append(" LIMIT ");
                Visit(node.Arguments[1]);
                break;
                
            case "Skip":
                Visit(node.Arguments[0]);
                _sb.Append(" OFFSET ");
                Visit(node.Arguments[1]);
                break;
                
            default:
                throw new NotSupportedException($"Method {node.Method.Name} is not supported");
        }
        
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        _sb.Append('(');
        Visit(node.Left);
        
        _sb.Append(node.NodeType switch
        {
            ExpressionType.Equal => " = ",
            ExpressionType.NotEqual => " <> ",
            ExpressionType.GreaterThan => " > ",
            ExpressionType.GreaterThanOrEqual => " >= ",
            ExpressionType.LessThan => " < ",
            ExpressionType.LessThanOrEqual => " <= ",
            ExpressionType.AndAlso => " AND ",
            ExpressionType.OrElse => " OR ",
            _ => throw new NotSupportedException($"Binary operator {node.NodeType} is not supported")
        });
        
        Visit(node.Right);
        _sb.Append(')');
        
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // Check if this is a captured variable (closure)
        if (node.Expression is ConstantExpression constant)
        {
            var value = GetMemberValue(node, constant.Value);
            AddParameter(value);
        }
        else
        {
            // It's a property access on the lambda parameter
            _sb.Append(node.Member.Name);
        }
        
        return node;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is IQueryable)
        {
            // This is the source, just output FROM
            _sb.Append("FROM ");
            _sb.Append(node.Type.GetSequenceElementType().Name);
        }
        else
        {
            AddParameter(node.Value);
        }
        
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        // Just visit the body, skip the parameter declaration
        Visit(node.Body);
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType == ExpressionType.Quote)
        {
            // Quote wraps lambdas in expression trees
            Visit(node.Operand);
        }
        else if (node.NodeType == ExpressionType.Not)
        {
            _sb.Append("NOT ");
            Visit(node.Operand);
        }
        else
        {
            Visit(node.Operand);
        }
        
        return node;
    }

    private void AddParameter(object? value)
    {
        var paramName = $"@p{_parameterIndex++}";
        _parameters[paramName] = value ?? DBNull.Value;
        _sb.Append(paramName);
    }

    private static object? GetMemberValue(MemberExpression member, object? container)
    {
        return member.Member switch
        {
            FieldInfo field => field.GetValue(container),
            PropertyInfo prop => prop.GetValue(container),
            _ => throw new NotSupportedException()
        };
    }
}
```

Now you can translate LINQ expressions, like so

```csharp
var minAge = 18;
var query = users
    .Where(u => u.Age >= minAge && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10);

var builder = new QueryStringBuilder();
builder.Visit(query.Expression);

Console.WriteLine(builder.Query);
// FROM User WHERE ((Age >= @p0) AND (IsActive = @p1)) ORDER BY Name ASC LIMIT @p2

Console.WriteLine(string.Join(", ", builder.Parameters.Select(p => $"{p.Key}={p.Value}")));
// @p0=18, @p1=True, @p2=10
```

The captured variable `minAge` gets properly converted to a parameter. This is exactly how EF Core handles your LINQ queries. It walks the expression tree, translates each node to SQL, and extracts parameters.

## Building a Practical Example: A File-Based Provider

Let's build something you might actually use. A LINQ provider that queries JSON files on disk

```csharp
public record Person(string Name, int Age, string City);

// Usage:
var people = new JsonFileQueryable<Person>("people.json");
var londoners = people.Where(p => p.City == "London" && p.Age > 21).ToList();
```

First, the queryable

```csharp
public class JsonFileQueryable<T> : IQueryable<T>, IOrderedQueryable<T>
{
    private readonly JsonFileQueryProvider _provider;
    private readonly Expression _expression;

    public JsonFileQueryable(string filePath)
    {
        _provider = new JsonFileQueryProvider(filePath, typeof(T));
        _expression = Expression.Constant(this);
    }

    internal JsonFileQueryable(JsonFileQueryProvider provider, Expression expression)
    {
        _provider = provider;
        _expression = expression;
    }

    public Type ElementType => typeof(T);
    public Expression Expression => _expression;
    public IQueryProvider Provider => _provider;

    public IEnumerator<T> GetEnumerator()
    {
        return _provider.Execute<IEnumerable<T>>(_expression).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

The provider

```csharp
public class JsonFileQueryProvider : IQueryProvider
{
    private readonly string _filePath;
    private readonly Type _elementType;

    public JsonFileQueryProvider(string filePath, Type elementType)
    {
        _filePath = filePath;
        _elementType = elementType;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        var elementType = expression.Type.GetSequenceElementType();
        var queryableType = typeof(JsonFileQueryable<>).MakeGenericType(elementType);
        var ctor = queryableType.GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(JsonFileQueryProvider), typeof(Expression) },
            null);
        return (IQueryable)ctor!.Invoke(new object[] { this, expression });
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new JsonFileQueryable<TElement>(this, expression);
    }

    public object? Execute(Expression expression)
    {
        return Execute<object>(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        // Load the data from the file
        var json = File.ReadAllText(_filePath);
        var listType = typeof(List<>).MakeGenericType(_elementType);
        var data = JsonSerializer.Deserialize(json, listType) as IEnumerable;
        
        // Create a queryable from the data
        var queryable = data!.AsQueryable();
        
        // Rewrite the expression tree to use our loaded data
        var rewriter = new JsonSourceRewriter(queryable, _elementType);
        var rewritten = rewriter.Visit(expression);
        
        // Execute using LINQ to Objects
        return queryable.Provider.Execute<TResult>(rewritten);
    }
}

public class JsonSourceRewriter : ExpressionVisitor
{
    private readonly IQueryable _newSource;
    private readonly Type _elementType;

    public JsonSourceRewriter(IQueryable newSource, Type elementType)
    {
        _newSource = newSource;
        _elementType = elementType;
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is IQueryable queryable && 
            queryable.GetType().IsGenericType &&
            queryable.GetType().GetGenericTypeDefinition() == typeof(JsonFileQueryable<>))
        {
            return Expression.Constant(_newSource);
        }
        
        return base.VisitConstant(node);
    }
}
```

This is a naive implementation, it loads the entire file and filters in memory. But you can see how you could extend it to parse the WHERE clause and do smarter filtering before loading everything.

## Translating Common Operations

### Where

```csharp
// LINQ:
users.Where(u => u.Age > 18)

// Expression:
MethodCallExpression
├── Method: Queryable.Where<User>
├── Arguments[0]: ConstantExpression (the source)
└── Arguments[1]: UnaryExpression (Quote)
    └── Operand: LambdaExpression
        ├── Parameters: [u]
        └── Body: BinaryExpression (GreaterThan)
            ├── Left: MemberExpression (u.Age)
            └── Right: ConstantExpression (18)
```

![](<_resources/📰 Building a Custom LINQ Provider in .NET/0f7077772a0a45b44dcd32f514febab4_MD5.webp>)

### Select

```csharp
// LINQ:
users.Select(u => new { u.Name, u.Age })

// Expression:
MethodCallExpression
├── Method: Queryable.Select<User, Anonymous>
├── Arguments[0]: (source)
└── Arguments[1]: (Quote)
    └── Operand: LambdaExpression
        └── Body: NewExpression
            ├── Constructor: Anonymous..ctor(string, int)
            └── Arguments: [MemberExpression (u.Name), MemberExpression (u.Age)]
```

![](<_resources/📰 Building a Custom LINQ Provider in .NET/9e103a4b4f5e8dc09df127ee88b62b20_MD5.webp>)

### OrderBy/ThenBy

```csharp
// LINQ:
users.OrderBy(u => u.Name).ThenByDescending(u => u.Age)

// Expression:
MethodCallExpression (ThenByDescending)
├── Arguments[0]: MethodCallExpression (OrderBy)
│   └── ...
└── Arguments[1]: (key selector lambda)
```

![](<_resources/📰 Building a Custom LINQ Provider in .NET/d6691a58daa7e8db4339f1cdb5756902_MD5.webp>)

### Join

This one's a bit more complex,

```csharp
// LINQ:
users.Join(orders, u => u.Id, o => o.UserId, (u, o) => new { u.Name, o.Total })

// Expression:
MethodCallExpression
├── Method: Queryable.Join<User, Order, int, Anonymous>
├── Arguments[0]: (outer source - users)
├── Arguments[1]: (inner source - orders)
├── Arguments[2]: (outer key selector: u => u.Id)
├── Arguments[3]: (inner key selector: o => o.UserId)
└── Arguments[4]: (result selector: (u, o) => new { u.Name, o.Total })
```

If you're building a database provider, you'd translate this to a SQL JOIN. For other backends, you might need to fetch both collections and join in memory.

![](<_resources/📰 Building a Custom LINQ Provider in .NET/e4a01bee1140aad91b30daa54f1b9782_MD5.webp>)

## Testing your providers

Testing LINQ providers is tricky because you're testing query translation, not just data manipulation. Here's my approach,

### Unit Tests for Expression Translation

```csharp
[Test]
public void TranslatesSimpleWhereClause()
{
    var queryable = new TestQueryable<User>(Array.Empty<User>());
    var query = queryable.Where(u => u.Age > 18);
    
    var builder = new QueryStringBuilder();
    builder.Visit(query.Expression);
    
    Assert.That(builder.Query, Does.Contain("WHERE"));
    Assert.That(builder.Query, Does.Contain("Age > @p0"));
    Assert.That(builder.Parameters["@p0"], Is.EqualTo(18));
}

[Test]
public void TranslatesMultipleConditionsWithAnd()
{
    var queryable = new TestQueryable<User>(Array.Empty<User>());
    var query = queryable.Where(u => u.Age > 18 && u.IsActive);
    
    var builder = new QueryStringBuilder();
    builder.Visit(query.Expression);
    
    Assert.That(builder.Query, Does.Contain("AND"));
}

[Test]
public void HandlesCapturedVariables()
{
    var minAge = 21;
    var queryable = new TestQueryable<User>(Array.Empty<User>());
    var query = queryable.Where(u => u.Age >= minAge);
    
    var builder = new QueryStringBuilder();
    builder.Visit(query.Expression);
    
    Assert.That(builder.Parameters["@p0"], Is.EqualTo(21));
}
```

### Integration Tests Against Known Data

```csharp
[Test]
public void WhereReturnsCorrectResults()
{
    var users = new List<User>
    {
        new("Alice", 25, "London"),
        new("Bob", 17, "Paris"),
        new("Charlie", 30, "London")
    };
    
    var queryable = new MyCustomQueryable<User>(users);
    var adults = queryable.Where(u => u.Age >= 18).ToList();
    
    Assert.That(adults, Has.Count.EqualTo(2));
    Assert.That(adults.Select(u => u.Name), Is.EquivalentTo(new[] { "Alice", "Charlie" }));
}

[Test]
public void OrderByAndWhereWorkTogether()
{
    var users = new List<User>
    {
        new("Charlie", 30, "London"),
        new("Alice", 25, "London"),
        new("Bob", 17, "Paris")
    };
    
    var queryable = new MyCustomQueryable<User>(users);
    var result = queryable
        .Where(u => u.City == "London")
        .OrderBy(u => u.Name)
        .ToList();
    
    Assert.That(result[0].Name, Is.EqualTo("Alice"));
    Assert.That(result[1].Name, Is.EqualTo("Charlie"));
}
```

### Edge Case Tests

```csharp
[Test]
public void EmptySourceReturnsEmpty()
{
    var queryable = new MyCustomQueryable<User>(Array.Empty<User>());
    var result = queryable.Where(u => u.Age > 0).ToList();
    
    Assert.That(result, Is.Empty);
}

[Test]
public void NullableComparisonsWork()
{
    var users = new List<User>
    {
        new("Alice", 25, null),
        new("Bob", 30, "London")
    };
    
    var queryable = new MyCustomQueryable<User>(users);
    var result = queryable.Where(u => u.City != null).ToList();
    
    Assert.That(result, Has.Count.EqualTo(1));
}

[Test]
public void StringContainsWorks()
{
    var users = new List<User>
    {
        new("Alice Smith", 25, "London"),
        new("Bob Jones", 30, "London")
    };
    
    var queryable = new MyCustomQueryable<User>(users);
    var result = queryable.Where(u => u.Name.Contains("Smith")).ToList();
    
    Assert.That(result, Has.Count.EqualTo(1));
}
```

## When to Build a Custom Provider vs Use Existing Solutions

Honestly? Almost never build your own LINQ provider. Here's when you might consider it:

Build your own when:

- You're integrating with a proprietary data source that has no existing provider
- You need very specific query optimisations that existing providers don't support
- You're building a domain-specific language on top of LINQ
- You're building an educational tool or experimenting

Don't build your own when:

- There's already a provider that works (EF Core, Dapper, LINQKit, etc.)
- You just need to filter in-memory collections (use LINQ to Objects)
- You think it'll be "fun" (it won't be, for long)
- You need production-ready code quickly

A custom LINQ provider is one of those things that's easy to get 80% working and incredibly difficult to get 100% right. The remaining 20% is where all the edge cases live: nullable types, method overloads, implicit conversions, generic type inference, and expression tree quirks you've never seen before.

If you're querying a REST API, consider using `HttpClient` with filters as query parameters. If you're querying a NoSQL database, use their SDK. If you're querying files, just load them and use LINQ to Objects.

That said, understanding how LINQ providers work makes you better at using them. You'll understand why certain queries don't translate, why EF Core complains about client evaluation, and how to structure your queries for better performance.

## Conclusion

Building a custom LINQ provider is a deep dive into how.NET's expression tree machinery works. You've got `IQueryable<T>` holding an expression tree, `IQueryProvider` doing the translation and execution, and `ExpressionVisitor` letting you walk and transform those trees.

The pattern is always the same, chain LINQ methods to build up an expression tree, then when you enumerate, the provider translates that tree into whatever your backend understands. For EF Core, that's SQL. For your custom provider, it could be anything.

Should you build one? Probably not. I wouldn't 99.9999% of the time. The existing ecosystem covers most use cases, and getting all the edge cases right is genuinely hard work. But understanding how they work makes you better at using them, and that's worth something.

If you do build one, start simple. Get `Where` working first. Then `Select`. Then `OrderBy`. Don't try to support everything at once, you'll lose your mind. And write lots of tests, because expression trees will surprise you in ways you didn't expect.

Good luck. You'll need it.
