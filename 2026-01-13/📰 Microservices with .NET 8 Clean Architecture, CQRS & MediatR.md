---
title: 📰 Microservices with .NET 8 Clean Architecture, CQRS & MediatR
source: https://medium.com/illumination/microservices-with-net-8-clean-architecture-cqrs-mediatr-1752dc9c1ebf
author:
  - "[[Venkataramana]]"
published: 2025-08-01
created: 2026-01-07
description: "Microservices with .NET 8 Clean Architecture, CQRS & MediatR Architecture Overview Core Principles Single Responsibility: Each microservice owns a specific business capability Decentralized: Services …"
tags:
  - clippings
updated: 2026-01-07T22:41
---

# 📰 Microservices with .NET 8 Clean Architecture, CQRS & MediatR

![](<_resources/📰 Microservices with .NET 8 Clean Architecture, CQRS & MediatR/6f7351890dfb53a47a2c5dbe39fde4d1_MD5.webp>)

Microservices with Clean Architecture Image by author

## Architecture Overview

## Core Principles

- **Single Responsibility**: Each microservice owns a specific business capability
- **Decentralized**: Services manage their own data and business logic
- **Failure Isolation**: Service failures don't cascade across the system
- **Technology Diversity**: Services can use different technologies as needed
- **Independent Deployment**: Services deploy independently without coordination

## Technology Stack

- **.NET 8**: Latest LTS version with improved performance and features
- **Clean Architecture**: Dependency inversion and separation of concerns
- **CQRS**: Command Query Responsibility Segregation for scalable read/write operations
- **MediatR**: In-process messaging for decoupled request handling
- **Entity Framework Core**: ORM for data access
- **ASP.NET Core Web API**: RESTful API endpoints
- **Docker**: Containerization for consistent deployments

## Clean Architecture Structure

## Layer Responsibilities

### Domain Layer (Core)

- **Entities**: Business objects with identity
- **Value Objects**: Immutable objects representing descriptive aspects
- **Domain Events**: Represent something that happened in the domain
- **Repository Interfaces**: Contracts for data access
- **Domain Services**: Business logic that doesn't belong to entities

### Application Layer

- **Use Cases**: Application-specific business rules
- **Commands & Queries**: CQRS implementation
- **Handlers**: Process commands and queries via MediatR
- **DTOs**: Data transfer objects for service boundaries
- **Application Services**: Orchestrate domain operations

### Infrastructure Layer

- **Repository Implementations**: Data access implementations
- **External Services**: Third-party integrations
- **Message Brokers**: Event publishing/subscribing
- **Caching**: Redis, In-memory caching
- **Configuration**: Settings and connection strings

### Presentation Layer (API)

- **Controllers**: HTTP endpoints
- **Middleware**: Cross-cutting concerns
- **Filters**: Request/response processing
- **Model Binding**: Request data mapping

## CQRS Pattern Implementation

## Command Structure

```rb
// Command Definition
public record CreateProductCommand(
    string Name,
    decimal Price,
    string Description,
    int CategoryId
) : IRequest<ProductDto>;

// Command Handler
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;
    private readonly IMediator _mediator;
    public CreateProductCommandHandler(IProductRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Price, request.Description, request.CategoryId);
        
        await _repository.AddAsync(product);
        
        // Publish domain event
        await _mediator.Publish(new ProductCreatedEvent(product.Id, product.Name), cancellationToken);
        
        return ProductDto.FromEntity(product);
    }
}
```

## Query Structure

```rb
// Query Definition
public record GetProductByIdQuery(int ProductId) : IRequest<ProductDto>;

// Query Handler
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductReadRepository _repository;
    public GetProductByIdQueryHandler(IProductReadRepository repository)
    {
        _repository = repository;
    }
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.ProductId);
        return ProductDto.FromEntity(product);
    }
}
```

## Benefits of CQRS

- **Scalability**: Separate read and write models can be optimized independently
- **Performance**: Query models can be denormalized for fast reads
- **Flexibility**: Different storage mechanisms for commands and queries
- **Maintainability**: Clear separation of read and write operations

## MediatR Integration

## Configuration

```rb
// Program.cs
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

## Pipeline Behaviors

```rb
// Validation Behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();
            if (failures.Count != 0)
                throw new ValidationException(failures);
        }
        return await next();
    }
}
```

## Domain Events

```rb
// Domain Event
public record ProductCreatedEvent(int ProductId, string ProductName) : INotification;

// Event Handler
public class ProductCreatedEventHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;
    private readonly IEventPublisher _eventPublisher;
    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger, IEventPublisher eventPublisher)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
    }
    public async Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Product created: {ProductId} - {ProductName}", 
            notification.ProductId, notification.ProductName);
        
        // Publish to message broker for other microservices
        await _eventPublisher.PublishAsync(new ProductCreatedIntegrationEvent(
            notification.ProductId, 
            notification.ProductName), 
            cancellationToken);
    }
}
```

## Implementation Best Practices

## 1\. Service Boundaries

- **Domain-Driven Design**: Align services with bounded contexts
- **Data Ownership**: Each service owns its data exclusively
- **Business Capability**: Services should represent complete business functions
- **Team Ownership**: Assign services to specific development teams

## 2\. API Design

```rb
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        return Ok(result);
    }
}
```

## 3\. Error Handling

```rb
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = exception switch
        {
            ValidationException => new { Status = 400, Message = exception.Message },
            NotFoundException => new { Status = 404, Message = exception.Message },
            _ => new { Status = 500, Message = "An error occurred processing your request" }
        };
        context.Response.StatusCode = response.Status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

## 4\. Configuration Management

```rb
public class DatabaseSettings
{
    public string ConnectionString { get; set; }
    public int CommandTimeout { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
}

// Program.cs
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));
builder.Services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>().Value;
    options.UseSqlServer(settings.ConnectionString, 
        opts => opts.CommandTimeout(settings.CommandTimeout));
    
    if (settings.EnableSensitiveDataLogging)
        options.EnableSensitiveDataLogging();
});
```

## Communication Patterns

## 1\. Synchronous Communication (HTTP)

- **Use Cases**: Real-time queries, immediate consistency requirements
- **Implementation**: HttpClient with Polly for resilience
- **Considerations**: Tight coupling, potential for cascading failures

```rb
public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
    public ProductService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
    public async Task<ProductDto> GetProductAsync(int productId)
    {
        var response = await _retryPolicy.ExecuteAsync(() => 
            _httpClient.GetAsync($"/api/products/{productId}"));
        
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ProductDto>(content);
    }
}
```

## 2\. Asynchronous Communication (Events)

- **Use Cases**: Event-driven workflows, eventual consistency
- **Implementation**: Message brokers (RabbitMQ, Azure Service Bus)
- **Benefits**: Loose coupling, resilience, scalability

```rb
public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent;
}

public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IIntegrationEvent
    {
        using var channel = _connection.CreateModel();
        
        var eventName = @event.GetType().Name;
        var body = JsonSerializer.SerializeToUtf8Bytes(@event);
        
        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Persistent
        
        channel.BasicPublish(
            exchange: "microservices_exchange",
            routingKey: eventName,
            basicProperties: properties,
            body: body);
        
        _logger.LogInformation("Published event {EventName} with ID {EventId}", eventName, @event.Id);
    }
}
```

## Data Management

## 1\. Database per Service

```rb
// Catalog Service Context
public class CatalogDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
// Product Configuration
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
    }
}
```

## 2\. Event Sourcing (Optional)

```rb
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
    public class Product : AggregateRoot
    {
    private Product() { } // For EF Core
    public static Product Create(string name, decimal price, string description, int categoryId)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Price = price,
            Description = description,
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        };
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, product.Name));
        return product;
    }
}
```

## 3\. Saga Pattern for Distributed Transactions

```rb
public class OrderProcessingSaga
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderProcessingSaga> _logger;
    public async Task Handle(OrderCreatedEvent @event)
    {
        try
        {
            // Step 1: Reserve inventory
            await _mediator.Send(new ReserveInventoryCommand(@event.OrderId, @event.Items));
            
            // Step 2: Process payment
            await _mediator.Send(new ProcessPaymentCommand(@event.OrderId, @event.TotalAmount));
            
            // Step 3: Confirm order
            await _mediator.Send(new ConfirmOrderCommand(@event.OrderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order processing failed for {OrderId}", @event.OrderId);
            
            // Compensating actions
            await _mediator.Send(new CancelOrderCommand(@event.OrderId));
            await _mediator.Send(new ReleaseInventoryCommand(@event.OrderId));
        }
    }
}
```

## Security Considerations

## 1\. Authentication & Authorization

```rb
// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireClaim("role", "admin"));
});
// Controller Authorization
[Authorize(Policy = "RequireAdminRole")]
[HttpPost]
public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
{
    // Implementation
}
```

## 2\. API Gateway Security

```rb
public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing");
            return;
        }
        var validApiKey = _configuration["ApiKey"];
        if (apiKey != validApiKey)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }
        await _next(context);
    }
}
```

## 3\. Input Validation

```rb
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Product name is required and must not exceed 100 characters");
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than zero");
        RuleFor(x => x.CategoryId)
            .GreaterThan(0)
            .WithMessage("Valid category is required");
    }
}
```

## Monitoring and Observability

## 1\. Logging

```rb
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid();
        _logger.LogInformation("Starting request {RequestName} with ID {RequestId}: {@Request}",
            requestName, requestId, request);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            _logger.LogInformation("Completed request {RequestName} with ID {RequestId} in {ElapsedMs}ms",
                requestName, requestId, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} with ID {RequestId} failed after {ElapsedMs}ms",
                requestName, requestId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

## 2\. Health Checks

```rb
builder.Services.AddHealthChecks()
    .AddDbContext<CatalogDbContext>()
    .AddRabbitMQ(builder.Configuration.GetConnectionString("RabbitMQ"))
    .AddRedis(builder.Configuration.GetConnectionString("Redis"));
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## 3\. Metrics and Tracing

```rb
// OpenTelemetry Configuration
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddJaegerExporter())
    .WithMetrics(meterProviderBuilder =>
        meterProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddPrometheusExporter());
```

## Deployment Strategies

## 1\. Docker Configuration

```rb
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Catalog.API/Catalog.API.csproj", "Catalog.API/"]
COPY ["Catalog.Application/Catalog.Application.csproj", "Catalog.Application/"]
COPY ["Catalog.Domain/Catalog.Domain.csproj", "Catalog.Domain/"]
COPY ["Catalog.Infrastructure/Catalog.Infrastructure.csproj", "Catalog.Infrastructure/"]
RUN dotnet restore "Catalog.API/Catalog.API.csproj"
COPY . .
WORKDIR "/src/Catalog.API"
RUN dotnet build "Catalog.API.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "Catalog.API.csproj" -c Release -o /app/publish
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

## 2\. Docker Compose

```rb
version: '3.8'
services:
  catalog-api:
    build:
      context: .
      dockerfile: Services/Catalog/Catalog.API/Dockerfile
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=catalog-db;Database=CatalogDb;User Id=sa;Password=Your_password123;
    depends_on:
      - catalog-db
      - rabbitmq
    networks:
      - microservices-network
    catalog-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "Your_password123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    networks:
      - microservices-network
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    networks:
      - microservices-network
networks:
  microservices-network:
    driver: bridge
```

## 3\. Kubernetes Deployment

```rb
apiVersion: apps/v1
kind: Deployment
metadata:
  name: catalog-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: catalog-api
  template:
    metadata:
      labels:
        app: catalog-api
    spec:
      containers:
      - name: catalog-api
        image: catalog-api:latest
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: catalog-secrets
              key: connection-string
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: catalog-api-service
spec:
  selector:
    app: catalog-api
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
  type: LoadBalancer
```

## Key Recommendations

## Performance Optimization

1. **Caching Strategy**: Implement Redis for frequently accessed data
2. **Database Optimization**: Use read replicas for query separation
3. **Connection Pooling**: Configure appropriate connection pool sizes
4. **Async Operations**: Use async/await throughout the application

## Resilience Patterns

1. **Circuit Breaker**: Prevent cascading failures
2. **Retry Logic**: Handle transient failures gracefully
3. **Timeout Policies**: Prevent indefinite waits
4. **Bulkhead Isolation**: Isolate critical resources

## Testing Strategy

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test service interactions
3. **Contract Tests**: Verify API contracts between services
4. **End-to-End Tests**: Test complete user workflows

## DevOps Practices

1. **CI/CD Pipelines**: Automated build, test, and deployment
2. **Infrastructure as Code**: Version-controlled infrastructure
3. **Blue-Green Deployments**: Zero-downtime deployments
4. **Feature Flags**: Safe feature rollouts

This architecture provides a solid foundation for building scalable, maintainable microservices using.NET 8 with Clean Architecture principles, CQRS pattern, and MediatR for clean separation of concerns and improved testability.
