# 5 async await Patterns Still Crashing .NET 10 Production in 2026

These are the 5 async traps I now see catch even senior.NET teams, ordered from most dangerous to most subtle. Each one is something you almost certainly have in your codebase right now.

## 1: async void crashes your entire process. And it sneaks in

This is the trap I missed completely in 2023, and it's the most dangerous one in this list.

`async void` does not propagate exceptions to its caller. The exception fires on a different thread and there is no Task to await, no try-catch in the caller that can save you. The default behavior is to crash the entire process.

The real production danger isn't methods explicitly marked `async void`. Senior devs know to avoid those. The danger is the async lambdas that get treated as `Action`, which the compiler converts to `async void` silently.

```cs
// Looks fine. Compiles. Crashes production.
builder.Services.AddHttpClient<PaymentService>(async client =>
{
    var settings = await GetSettingsAsync();
    client.BaseAddress = new Uri(settings.BaseUrl);
});
```

The configuration callback expects an `Action<HttpClient>`. Your `async` lambda has no return type the framework can await, so it becomes `async void`. The first time `GetSettingsAsync` throws, the exception fires on the threadpool with nobody to catch it. Your ASP.NET Core process exits.

This is documented on dotnet/aspnetcore (issue # 13867). The reproduction is small: any `async void` that throws in a Web API endpoint crashes the host. Real production teams have reported this in the wild, most recently a team running into it via an HttpClient factory whose anonymous async configuration method threw on a transient SQL connection failure.

**The fix:**

Never write async lambdas where the consumer expects `Action`. If the API needs a non-async callback, do the async work synchronously inside, or refactor to a method the framework can `await`.

```cs
// Make the configuration synchronous
builder.Services.AddHttpClient<PaymentService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Payments:BaseUrl"]);
});
```

Audit your code for these signatures: `AddHttpClient`, `AddSingleton` / `AddScoped` / `AddTransient` factory overloads, event handler subscriptions, `Task.Run` callers, anywhere you pass a delegate. Look for any `async` keyword in front of a lambda where the consumer expects `Action`. Each one is a production crash waiting for the right exception.

## 2: Your CancellationToken cancels nothing if you don't pass it down

ASP.NET Core hands you a CancellationToken in every controller method. It fires when the client disconnects, the request times out, or the host is shutting down. Most code accepts the token and never passes it anywhere.

```cs
// The token arrives. The token is ignored.
public async Task<List<Order>> GetOrdersAsync(CancellationToken ct)
{
    var orders = await _db.Orders.ToListAsync();          // no token
    foreach (var order in orders)
    {
        await _enricher.EnrichAsync(order);               // no token
    }
    return orders;
}
```

The client times out at 30 seconds. The CancellationToken cancels. Your code keeps running. The database query finishes. The enrichment loop processes every order. Two minutes later your API responds to a connection that closed long ago.

You're not failing fast. You're not freeing a request thread. You're not respecting timeouts. You're paying CPU and database cost for work nobody is waiting for.

**The fix:**

Pass the token to every awaited call inside the method, all the way down to the deepest async dependency.

```cs
public async Task<List<Order>> GetOrdersAsync(CancellationToken ct)
{
    var orders = await _db.Orders.ToListAsync(ct);
    
    foreach (var order in orders)
    {
        ct.ThrowIfCancellationRequested();
        await _enricher.EnrichAsync(order, ct);
    }
    return orders;
}
```

Every `ToListAsync`, `FindAsync`, `SaveChangesAsync`, `HttpClient.GetAsync`, `Task.Delay` accepts a token. Pass it. The framework already created it for you. Refusing to pass it is just throwing the rope away.

## 3: Task.Run inside a controller is the "performance fix" that makes things worse

```cs
// "I made it async! It's faster now!"
public async Task<Result> ProcessAsync(Request request)
{
    var result = await Task.Run(() => HeavyComputation(request));
    return result;
}
```

This is one of the most common patterns I see when teams first try to "make things async." It does not make the request faster. It makes it slower.

What actually happens: the request is on threadpool thread A. `Task.Run` schedules `HeavyComputation` on threadpool thread B. Thread A is now blocked awaiting thread B. Two threadpool threads are tied up doing the work of one. The original request is no faster, the threadpool has less capacity for other requests, and you've added a context switch.

`Task.Run` is for offloading CPU work in a desktop or non-server context where the calling thread is the UI thread you don't want to block. In ASP.NET Core, the calling thread IS a threadpool thread. Offloading from threadpool to threadpool buys you nothing.

**The fix:**

If the work is small, just run it synchronously. The request thread is already a threadpool thread.

```cs
public Result Process(Request request)
{
    return HeavyComputation(request);
}
```

If the work is heavy enough that you don't want it tying up a request thread, get it out of the request entirely. Queue it to a background service via a Channel, a hosted service, or a message broker. Return a 202 Accepted with a job ID. Let the client poll or subscribe for the result.

`Task.Run` inside a controller is almost never the right answer.

## 4: Delete every ConfigureAwait(false) from your ASP.NET Core code

This is the one I had wrong in 2023.

`ConfigureAwait(false)` exists to prevent continuations from resuming on a captured SynchronizationContext. The pattern was critical in WinForms, WPF, and legacy ASP.NET (Framework), where there was a SynchronizationContext you could deadlock against.

ASP.NET Core has no SynchronizationContext.

There is nothing to capture. There is nothing to resume on. `ConfigureAwait(false)` is a no-op in ASP.NET Core code.

This isn't my opinion. Stephen Toub (the.NET team's authority on async) wrote the official ConfigureAwait FAQ on the.NET blog confirming it. David Fowler (the ASP.NET Core team lead) stated explicitly that "most of ASP.NET Core doesn't use ConfigureAwait(false) and that was an explicit decision because it was deemed unnecessary." The ASP.NET Core team dropped it from their own codebase.

```cs
// Visual noise. Does nothing in ASP.NET Core.
var data = await _service.GetDataAsync().ConfigureAwait(false);

// Same behavior. Less code.
var data = await _service.GetDataAsync();
```

**The exception:** if you're writing a NuGet library that might be consumed by a WinForms or legacy ASP.NET Framework caller, keep `ConfigureAwait(false)` in your library code. The library doesn't know what context it's running in. But for code that lives inside an ASP.NET Core service, controller, middleware, hosted service, or anywhere else inside your web app, delete it. Every `.ConfigureAwait(false)` is doing exactly nothing.

If your team has a linter rule enforcing `ConfigureAwait(false)` everywhere, drop the rule. If your team has been adding it on every await out of habit, stop. The code reads cleaner without it.

## 5: Fire-and-forget that outlives the request scope

Most fire-and-forget patterns I see in.NET are written like this:

```cs
public async Task<Result> CreateOrderAsync(Order order)
{
    await _db.Orders.AddAsync(order);
    await _db.SaveChangesAsync();

    // Don't make the user wait for the email
    _ = SendConfirmationEmailAsync(order, _db);
    
    return Result.Ok();
}
```

The discard `_ =` makes the compiler stop warning you. The email sends in the background. The request returns fast. Looks clean.

Two problems hide here.

First, exceptions in the discarded task vanish. If `SendConfirmationEmailAsync` throws, the exception goes nowhere. No log entry. No alert. Customers complain about missing emails. You can't reproduce it.

Second, and worse, you just passed `_db` (your scoped DbContext) to a task that outlives the request. When the request scope completes, the DI container disposes `_db`. Your background task is now using a disposed DbContext. The next time it touches the database, you get an `ObjectDisposedException`. If you're lucky.

**The fix:**

Don't fire-and-forget from inside a request. Either await the work, or queue it to something built to handle background work.

```cs
public async Task<Result> CreateOrderAsync(Order order)
{
    await _db.Orders.AddAsync(order);
    await _db.SaveChangesAsync();

    await _emailQueue.QueueAsync(new SendConfirmationEmail(order.Id));
    return Result.Ok();
}
```

The queue persists the work outside the request scope. A background service picks it up, creates its own DI scope, resolves a fresh DbContext, sends the email, and handles its own exceptions.

If you genuinely want fire-and-forget for something that doesn't need persistence, use a singleton service that doesn't capture scoped dependencies, and wrap the work in a try-catch that logs every exception.

## Your action list

If you've been carrying any of these patterns in production, here's the order to fix them:

1. Audit your DI registration callbacks, factory methods, and event handler subscriptions for `async` lambdas in `Action` slots
2. Add `CancellationToken` to every awaited call inside your services
3. Find every `Task.Run` in a controller and either inline the work or queue it
4. Delete every `ConfigureAwait(false)` from your ASP.NET Core code
5. Replace fire-and-forget patterns inside requests with a queue plus a background worker

Each fix takes 10 minutes. The combined effect is a codebase that fails loudly when it should, respects cancellation, scales correctly under load, and stops carrying patterns that haven't been correct since.NET Framework.

