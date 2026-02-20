---
title: Writing .NET Database Integration Tests
source: https://khalidabuhakmeh.com/dotnet-database-integration-tests
author:
  - "[[Khalid Abuhakmeh]]"
published: 2020-03-30
created: 2026-02-13
description: Learn the best way to test your database-driven applications using integration testing. See how easy it is to test SQL using .NET and XUnit.
tags:
  - clippings
uid: 6a8e65c8-eacc-4d96-8681-a32256de1511
---
# Writing .NET Database Integration Tests
Database access is easy, but testing database access is hard whether we’re an experienced.NET developer or new to the party. We may also hear a lot of opinions in our ecosystem about testing around data access scenarios. Should I mock, stub, use an in-memory version, and go all-in with integration testing.

In this post, we will see why integration testing is my recommended path moving forward, discuss the pros and cons, and see how to implement it in our test infrastructure.

## Opinions on Integration Tests

Many folks ask themselves, **should.NET developers be writing.NET database integration tests?**

> Yes! When working with data access,.NET developers should favor database integration tests over all other kinds of approaches. –Khalid

Let’s look at some of the arguments *against* database integration tests and why we shouldn’t get hung up on them.

### DB Integration Tests Are Slow

The first and most common argument against database integration tests is that they are slow. Yes, of course, any IO operation will likely be more time-consuming than an action that occurs in memory. Want to know what’s even faster? No tests at all! Speed, while a concern, should not be the top priority of a test-suite. **The top priority of any test should be correctness.**

Databases are complex systems with many moving parts. When we reach for a *stand-in* replacement, we are likely getting a faint shadow of what the database engine can do. Not using the actual database engine can lead to two severe issues.

The first of the two issues is not utilizing the database engine to its full capabilities. We pick the lowest common feature set to satisfy our need for *fast tests* rather than leaning on the full spectrum of abilities that the database engine has to offer. We have limited what our application could be in favor of a vague idea of *“fast tests”*.

The second, and arguably more dangerous, issue stems from correctness. I mentioned how database engines are complex systems. Most database engines have taken years to build, with large teams of dedicated developers solely focused on understanding the edge cases, strengths, and weaknesses. On top of that, organizations have dedicated even larger teams to building and solving the data-access issue. **Any replacement we build will be an inferior one and invalidate the very tests that make us feel secure.**

### DB Integration Tests Are Hard

The.NET ecosystem hasn’t always been friendly to vendors, not named Microsoft. That has changed lately with.NET Core and Microsofts push to be cloud-first. When talking about databases, many.NET teams still leverage some flavor of SQL database (likely Microsoft SQL Server). Microsoft has made a push to run SQL Server on both Windows and Unix servers with [SQL Server for Linux](https://docs.microsoft.com/en-us/sql/linux/sql-server-linux-setup?view=sql-server-ver15).

The addition of SQL Server for Linux opens a world of new possibilities for all.NET Developers, especially for those wanting to do integration tests. Later in this post, We’ll see how to use a NuGet package along with SQL Server for Linux to have relatively easy integration tests. It may have been not very easy a few years ago, but that has changed dramatically.

We’ll also see that we can write integration tests with other database engines. In this post, we’ll see how we can utilize PostgreSQL in our integration tests.

### 😭 But I Don’t Wanna!

The “I don’t wanna” argument is a fair one, and I understand it. Maybe integration tests still make some developers feel icky. That’s ok, and folks are welcome to be on their journey at their own pace. When they are ready, this post will be here to help. No judgment from the integration testing folks, we promise.

## Writing The Tests

**TL;DR Jump right into the code at this [GitHub repository](https://github.com/khalidabuhakmeh/DatabaseIntegrationTesting).**

Let’s dive into what we’ll need to start integration testing with.NET. Some are marked optional, and there are undoubtedly several approaches to solving this problem. This post outlines one approach.

- [.NET Core](https://dot.net/)
- Database Engine(s)
- Unit Testing Framework – I like XUnit
- Docker (optional)
- Migration Framework (optional)

**Note, Check out my amazingly productive [“Bridging The.NET Cross-Platform Divide With Docker”](https://khalidabuhakmeh.com/bridging-the-dotnet-cross-platform-divide-with-docker) post to start using Docker.**

Our first step is to create a test project.

```
> dotnet new xunit -o DatabaseIntegrationTesting
```

Console

Once we have our test project created, let’s add two NuGet packages for **[ThrowAwayDb](https://www.nuget.org/packages/ThrowawayDb/)**.

```
> dotnet add package ThrowawayDb
> dotnet add package ThrowawayDb.Postgres
```

Console

The first package targets SQL Server, and the second supports the PostgreSQL database. Next, we’ll need two unit test files, one for each database engine.

First, let’s look at the Microsoft SQL Server integration tests.

```csharp
public class SqlServerTests
{
    private readonly ITestOutputHelper testOutputHelper;

    public SqlServerTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }

    private static class Settings
    {
        public const string Username = "sa";
        public const string Password = "Pass123!";
        public const string Host = "localhost,11433";
    }
    
    [Fact]
    public void Can_Select_1_From_Database()
    {
        using var database = ThrowawayDatabase.Create(
            Settings.Username, 
            Settings.Password, 
            Settings.Host
        );
        
        testOutputHelper.WriteLine($"Created database {database.Name}");

        using var connection = new SqlConnection(database.ConnectionString);
        connection.Open();
        using var cmd = new SqlCommand("SELECT 1", connection);
        var result = Convert.ToInt32(cmd.ExecuteScalar());
        
        testOutputHelper.WriteLine(result.ToString());
        
        Assert.Equal(1, result);
    }
}
```

C#

Then, we need to add another class for our PostgreSQL database tests.

```csharp
public class PostgreSqlTests
{
    private static class Settings
    {
        public const string Username = "postgres";
        public const string Password = "Pass123!";
        public const string Host = "localhost";
    }
    
    private readonly ITestOutputHelper testOutputHelper;
    
    public PostgreSqlTests(ITestOutputHelper testOutputHelper)
    {
        this.testOutputHelper = testOutputHelper;
    }
    
    [Fact]
    public void Can_Select_1_From_Database()
    {
        using var database = ThrowawayDatabase.Create(
            Settings.Username, 
            Settings.Password, 
            Settings.Host
        );
        
        testOutputHelper.WriteLine($"Created database {database.Name}");
        using var connection = new NpgsqlConnection(database.ConnectionString);
        connection.Open();
            
        using var cmd = new NpgsqlCommand("SELECT 1", connection);
        var result = Convert.ToInt32(cmd.ExecuteScalar());
        
        Assert.Equal(1, result); 
        
        testOutputHelper.WriteLine(result.ToString());
    }
}
```

C#

Each of these two classes creates a separate database. **The tests can be optimized depending on the level of isolation we want to help increase the speed and performance of our tests**.

Now, let’s run them using the `dotnet test` command.

![dotnet test run](https://res.cloudinary.com/abuhakmeh/image/fetch/c_limit,f_auto,q_auto,w_800/https://khalidabuhakmeh.com/assets/images/posts/database-integration/database-integration-run.png)

We can also run our integration tests inside of [JetBrains Rider](https://jetbrains.com/rider).

![dotnet test run](https://res.cloudinary.com/abuhakmeh/image/fetch/c_limit,f_auto,q_auto,w_800/https://khalidabuhakmeh.com/assets/images/posts/database-integration/database-integration-rider.png)

It works! The great thing about **ThrowawayDb** is that it creates a throwaway database that can provide a level of isolation for our tests. We can get an additional performance boost by running our tests in parallel.

**Note: If you want to learn more about how [Rider works with Databases, check out my JetBrains post here.](https://blog.jetbrains.com/dotnet/2020/03/05/working-databases-jetbrains-rider/)**

## Conclusion

The value of our tests come from their correctness. Having a fast feedback loop is essential, but a misleading feedback loop isn’t worth the speed. We can now automate our testing infrastructure with the advent of technologies like Docker,.NET Core, and SQL Server for Linux. Integration testing has never been easier in.NET, and it is highly encouraged that teams use this approach when dealing with a database engine.

I hope you enjoyed this blog post, and please leave a comment below about your thoughts. I would love to hear them.w