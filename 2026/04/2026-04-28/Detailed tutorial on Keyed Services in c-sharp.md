
# Detailed tutorial on Keyed Services in c-sharp

🤖❓ I am a `c#` developer and I am new to using KeyedServices in with dependency injection.
Please write me a through tutorial on using keyed services including tips, tricks and best practices


> [!ai] Chat-GPT

# Comprehensive Tutorial: Keyed Services in C# Dependency Injection

## Table of Contents
1. [Introduction](<#introduction>)
2. [Prerequisites](<#prerequisites>)
3. [Basic Concepts](<#basic-concepts>)
4. [Setup & Configuration](<#setup--configuration>)
5. [Practical Examples](<#practical-examples>)
6. [Advanced Patterns](<#advanced-patterns>)
7. [Best Practices](<#best-practices>)
8. [Common Pitfalls](<#common-pitfalls>)

---

## Introduction

**Keyed Services** (introduced in .NET 8) allow you to register multiple implementations of the same interface with different keys, enabling you to resolve the correct implementation at runtime based on a key rather than just the type.

### Why Use Keyed Services?

```csharp
// ❌ Without Keyed Services - Difficult to manage multiple implementations
services.AddTransient<ILogger, ConsoleLogger>();
services.AddTransient<ILogger, FileLogger>();  // This overwrites the previous!

// ✅ With Keyed Services - Clean and explicit
services.AddKeyedTransient<ILogger>("console", (sp, key) => new ConsoleLogger());
services.AddKeyedTransient<ILogger>("file", (sp, key) => new FileLogger());
```

---

## Prerequisites

- **.NET 8 or later** (Keyed Services are a .NET 8+ feature)
- Basic understanding of Dependency Injection
- Visual Studio or VS Code

```xml
<!-- Ensure your .csproj targets .NET 8+ -->
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

---

## Basic Concepts

### Key Components

| Component | Purpose |
|-----------|---------|
| **Service Type** | The interface/class being registered |
| **Key** | String identifier for the implementation |
| **Implementation** | The concrete class providing the service |
| **Lifetime** | Transient, Scoped, or Singleton |

### Core Attribute: `[FromKeyedServices]`

This attribute tells the DI container which keyed implementation to inject.

```csharp
public class MyService
{
    public MyService([FromKeyedServices("redis")] ICache cache)
    {
        // Inject the "redis" implementation
    }
}
```

---

## Setup & Configuration

### Step 1: Define Your Interface

```csharp
public interface INotificationService
{
    Task SendAsync(string message);
}
```

### Step 2: Create Multiple Implementations

```csharp
public class EmailNotificationService : INotificationService
{
    public async Task SendAsync(string message)
    {
        Console.WriteLine($"📧 Sending email: {message}");
        await Task.Delay(100);
    }
}

public class SmsNotificationService : INotificationService
{
    public async Task SendAsync(string message)
    {
        Console.WriteLine($"📱 Sending SMS: {message}");
        await Task.Delay(100);
    }
}

public class SlackNotificationService : INotificationService
{
    public async Task SendAsync(string message)
    {
        Console.WriteLine($"💬 Posting to Slack: {message}");
        await Task.Delay(100);
    }
}
```

### Step 3: Register Keyed Services

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Method 1: Using AddKeyedTransient
builder.Services.AddKeyedTransient<INotificationService, EmailNotificationService>("email");
builder.Services.AddKeyedTransient<INotificationService, SmsNotificationService>("sms");
builder.Services.AddKeyedTransient<INotificationService, SlackNotificationService>("slack");

// Method 2: Using factory delegate (more control)
builder.Services.AddKeyedTransient<INotificationService>(
    "email",
    (sp, key) => new EmailNotificationService()
);

// Method 3: With configuration
builder.Services.AddKeyedScoped<INotificationService>(
    "email",
    (sp, key) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var emailConfig = config.GetSection("Email");
        return new EmailNotificationService();
    }
);

var app = builder.Build();
```

---

## Practical Examples

### Example 1: Simple Service Injection

```csharp
public class NotificationController
{
    private readonly INotificationService _emailService;
    private readonly INotificationService _smsService;

    public NotificationController(
        [FromKeyedServices("email")] INotificationService emailService,
        [FromKeyedServices("sms")] INotificationService smsService)
    {
        _emailService = emailService;
        _smsService = smsService;
    }

    public async Task SendNotifications()
    {
        await _emailService.SendAsync("Hello via Email");
        await _smsService.SendAsync("Hello via SMS");
    }
}
```

### Example 2: Dynamic Resolution

```csharp
public class NotificationFacade
{
    private readonly KeyedService<INotificationService> _notificationFactory;

    public NotificationFacade(KeyedService<INotificationService> notificationFactory)
    {
        _notificationFactory = notificationFactory;
    }

    public async Task SendByChannelAsync(string channel, string message)
    {
        var service = _notificationFactory(channel);
        await service.SendAsync(message);
    }
}

// Or using IKeyedServiceProvider
public class NotificationFacadeV2
{
    private readonly IKeyedServiceProvider _keyedServiceProvider;

    public NotificationFacadeV2(IKeyedServiceProvider provider)
    {
        _keyedServiceProvider = provider;
    }

    public async Task SendAsync(string key, string message)
    {
        var service = _keyedServiceProvider.GetKeyedService<INotificationService>(key);
        if (service != null)
        {
            await service.SendAsync(message);
        }
        else
        {
            throw new InvalidOperationException($"No notification service found for key: {key}");
        }
    }
}
```

### Example 3: Configuration-Driven Keys

```csharp
public record NotificationSettings
{
    public string DefaultChannel { get; set; } = "email";
    public List<string> AvailableChannels { get; set; } = new();
}

public class ConfigurableNotificationService
{
    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly NotificationSettings _settings;

    public ConfigurableNotificationService(
        IKeyedServiceProvider serviceProvider,
        IOptions<NotificationSettings> options)
    {
        _serviceProvider = serviceProvider;
        _settings = options.Value;
    }

    public async Task SendAsync(string message, string? channel = null)
    {
        var channelToUse = channel ?? _settings.DefaultChannel;

        var service = _serviceProvider.GetKeyedService<INotificationService>(channelToUse);
        if (service == null)
            throw new InvalidOperationException($"Channel not found: {channelToUse}");

        await service.SendAsync(message);
    }

    public List<INotificationService> GetAllServices()
    {
        return _settings.AvailableChannels
            .Select(channel => _serviceProvider.GetKeyedService<INotificationService>(channel))
            .Where(s => s != null)
            .ToList()!;
    }
}
```

### Example 4: Data Processing Pipeline

```csharp
public interface IDataProcessor
{
    object Process(object data);
}

public class JsonProcessor : IDataProcessor
{
    public object Process(object data) => System.Text.Json.JsonSerializer.Serialize(data);
}

public class XmlProcessor : IDataProcessor
{
    public object Process(object data) => $"<data>{data}</data>";
}

public class DataProcessingPipeline
{
    private readonly IKeyedServiceProvider _serviceProvider;

    public DataProcessingPipeline(IKeyedServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object ProcessData(object data, string format)
    {
        var processor = _serviceProvider.GetKeyedService<IDataProcessor>(format.ToLower());
        if (processor == null)
            throw new InvalidOperationException($"Unsupported format: {format}");

        return processor.Process(data);
    }
}

// Registration
services.AddKeyedTransient<IDataProcessor, JsonProcessor>("json");
services.AddKeyedTransient<IDataProcessor, XmlProcessor>("xml");
services.AddTransient<DataProcessingPipeline>();
```

---

## Advanced Patterns

### Pattern 1: Enum-Based Keys

```csharp
public enum CacheType
{
    InMemory,
    Redis,
    Distributed
}

public interface ICache
{
    T Get<T>(string key);
    void Set<T>(string key, T value);
}

public class EnumKeyedCacheService
{
    private readonly IKeyedServiceProvider _serviceProvider;

    public EnumKeyedCacheService(IKeyedServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ICache GetCache(CacheType type)
    {
        var cache = _serviceProvider.GetKeyedService<ICache>(type.ToString());
        return cache ?? throw new InvalidOperationException($"Cache type not found: {type}");
    }
}

// Registration
services.AddKeyedSingleton<ICache, InMemoryCache>(CacheType.InMemory.ToString());
services.AddKeyedSingleton<ICache, RedisCache>(CacheType.Redis.ToString());
```

### Pattern 2: Factory with Multiple Implementations

```csharp
public interface IPaymentGateway
{
    Task<bool> ProcessPaymentAsync(decimal amount);
}

public record PaymentGatewayConfig
{
    public string Name { get; set; }
    public string ApiKey { get; set; }
}

builder.Services.AddKeyedTransient<IPaymentGateway>(
    "stripe",
    (sp, key) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var apiKey = config["Payment:Stripe:ApiKey"];
        return new StripeGateway(apiKey);
    }
);

builder.Services.AddKeyedTransient<IPaymentGateway>(
    "paypal",
    (sp, key) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var apiKey = config["Payment:PayPal:ApiKey"];
        return new PayPalGateway(apiKey);
    }
);
```

### Pattern 3: Composite Pattern with Keyed Services

```csharp
public interface IValidator
{
    ValidationResult Validate(object obj);
}

public class CompositeValidator : IValidator
{
    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly string[] _validatorKeys;

    public CompositeValidator(
        IKeyedServiceProvider serviceProvider,
        IOptions<ValidationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _validatorKeys = options.Value.ValidatorKeys;
    }

    public ValidationResult Validate(object obj)
    {
        var errors = new List<string>();

        foreach (var key in _validatorKeys)
        {
            var validator = _serviceProvider.GetKeyedService<IValidator>(key);
            if (validator != null)
            {
                var result = validator.Validate(obj);
                errors.AddRange(result.Errors);
            }
        }

        return new ValidationResult { Errors = errors };
    }
}
```

### Pattern 4: Generic Keyed Services

```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(int id);
    Task AddAsync(T entity);
}

// Register with generic type and key
builder.Services.AddKeyedScoped(
    typeof(IRepository<>),
    "sqlserver",
    typeof(SqlServerRepository<>)
);

builder.Services.AddKeyedScoped(
    typeof(IRepository<>),
    "mongodb",
    typeof(MongoDbRepository<>)
);

// Usage
public class DataService
{
    private readonly IKeyedServiceProvider _serviceProvider;

    public async Task SaveUserAsync(User user, string repositoryType)
    {
        var repository = _serviceProvider
            .GetKeyedService(typeof(IRepository<User>), repositoryType) as IRepository<User>;
        
        await repository?.AddAsync(user)!;
    }
}
```

---

## Best Practices

### ✅ DO's

#### 1. Use Clear, Descriptive Keys

```csharp
// ✅ Good
services.AddKeyedTransient<ILogger>("filesystem-logger");
services.AddKeyedTransient<ILogger>("console-logger");

// ❌ Avoid
services.AddKeyedTransient<ILogger>("1");
services.AddKeyedTransient<ILogger>("2");
```

#### 2. Define Keys as Constants

```csharp
public static class ServiceKeys
{
    public const string EmailNotification = "email";
    public const string SmsNotification = "sms";
    public const string SlackNotification = "slack";
    
    public const string SqlServerRepository = "sqlserver";
    public const string MongoDbRepository = "mongodb";
}

// Usage
services.AddKeyedTransient<INotificationService, EmailNotificationService>(
    ServiceKeys.EmailNotification);

public class NotificationController
{
    public NotificationController(
        [FromKeyedServices(ServiceKeys.EmailNotification)] INotificationService emailService)
    {
    }
}
```

#### 3. Use Strongly-Typed Keys When Possible

```csharp
public enum NotificationChannel
{
    Email,
    Sms,
    Slack
}

public interface IKeyedServiceProvider<TKey> where TKey : Enum
{
    T GetKeyedService<T>(TKey key);
}

// Custom extension
public static class KeyedServiceExtensions
{
    public static void AddNotificationServices(this IServiceCollection services)
    {
        services.AddKeyedTransient<INotificationService>(
            NotificationChannel.Email.ToString(),
            (_, _) => new EmailNotificationService());
    }
}
```

#### 4. Document Your Keys

```csharp
/// <summary>
/// Registers notification services with their respective keys.
/// 
/// Available keys:
/// - "email": Sends notifications via email
/// - "sms": Sends notifications via SMS
/// - "slack": Sends notifications via Slack
/// </summary>
public static IServiceCollection AddNotificationServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddKeyedTransient<INotificationService, EmailNotificationService>("email");
    services.AddKeyedTransient<INotificationService, SmsNotificationService>("sms");
    services.AddKeyedTransient<INotificationService, SlackNotificationService>("slack");

    return services;
}
```

#### 5. Validate Keys at Startup

```csharp
public class ServiceKeyValidator
{
    public static void ValidateKeysExist(IServiceProvider serviceProvider)
    {
        var requiredKeys = new[] 
        { 
            ServiceKeys.EmailNotification, 
            ServiceKeys.SmsNotification 
        };

        foreach (var key in requiredKeys)
        {
            var service = serviceProvider.GetKeyedService<INotificationService>(key);
            if (service == null)
                throw new InvalidOperationException($"Required service key not registered: {key}");
        }
    }
}

// In Program.cs after building the app
var app = builder.Build();
ServiceKeyValidator.ValidateKeysExist(app.Services);
```

#### 6. Handle Missing Keys Gracefully

```csharp
public class ResilientNotificationService
{
    private readonly IKeyedServiceProvider _serviceProvider;
    private readonly ILogger<ResilientNotificationService> _logger;
    private readonly string _fallbackKey;

    public ResilientNotificationService(
        IKeyedServiceProvider serviceProvider,
        ILogger<ResilientNotificationService> logger,
        IOptions<NotificationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fallbackKey = options.Value.FallbackNotificationKey ?? ServiceKeys.EmailNotification;
    }

    public async Task SendAsync(string key, string message)
    {
        try
        {
            var service = _serviceProvider.GetKeyedService<INotificationService>(key)
                ?? throw new InvalidOperationException($"Service key not found: {key}");
            
            await service.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send via {Key}, using fallback {FallbackKey}", 
                key, _fallbackKey);
            
            var fallbackService = _serviceProvider.GetKeyedService<INotificationService>(_fallbackKey)!;
            await fallbackService.SendAsync($"[FALLBACK] {message}");
        }
    }
}
```

---

## Common Pitfalls

### ❌ Pitfall 1: Forgetting `[FromKeyedServices]` Attribute

```csharp
// ❌ WRONG - DI will try to resolve INotificationService without a key
public class MyService
{
    public MyService(INotificationService service) { }
}

// ✅ CORRECT
public class MyService
{
    public MyService([FromKeyedServices("email")] INotificationService service) { }
}
```

### ❌ Pitfall 2: Using Wrong Key Type

```csharp
// ❌ Registration with string
services.AddKeyedTransient<ICache, RedisCache>("redis");

// ❌ Trying to use with enum key (will fail)
public MyService([FromKeyedServices(CacheType.Redis)] ICache cache) { }
```

### ❌ Pitfall 3: Not Handling Null Returns

```csharp
// ❌ RISKY - GetKeyedService can return null
var service = serviceProvider.GetKeyedService<INotificationService>("invalid-key");
await service.SendAsync("message"); // NullReferenceException!

// ✅ SAFE
var service = serviceProvider.GetKeyedService<INotificationService>("invalid-key");
if (service == null)
    throw new InvalidOperationException("Service not found");
await service.SendAsync("message");

// ✅ Alternative using GetRequiredKeyedService
var service = serviceProvider.GetRequiredKeyedService<INotificationService>("email");
```

### ❌ Pitfall 4: Circular Dependencies with Keyed Services

```csharp
// ❌ AVOID - Can cause circular dependency
public class ServiceA
{
    public ServiceA([FromKeyedServices("b")] IService service) { }
}

public class ServiceB
{
    public ServiceB([FromKeyedServices("a")] IService service) { }
}

// ✅ Use factory pattern instead
services.AddKeyedTransient<IService>("delayed", (sp, key) =>
{
    // Resolution is deferred until needed
    return new ServiceB(/* lazy resolution */);
});
```

### ❌ Pitfall 5: Mixing Keyed and Non-Keyed Registrations

```csharp
// ❌ CONFUSING - Both keyed and non-keyed
services.AddTransient<ICache, RedisCache>();
services.AddKeyedTransient<ICache, InMemoryCache>("inmemory");

// ✅ CLEAR - All keyed or all non-keyed
services.AddKeyedTransient<ICache, RedisCache>("redis");
services.AddKeyedTransient<ICache, InMemoryCache>("inmemory");
```

---

## Complete
