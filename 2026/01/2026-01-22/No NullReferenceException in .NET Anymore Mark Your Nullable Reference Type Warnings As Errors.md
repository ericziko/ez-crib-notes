---
title: 'No "NullReferenceException" in .NET Anymore! Mark Your Nullable Reference Type Warnings As Errors'
source: https://medium.com/@s.quddin/no-nullreferenceexception-in-net-anymore-mark-your-nullable-reference-type-warnings-as-errors-e3d588602617
author:
  - "[[Syed Qutub Uddin Khan]]"
published: 2024-12-29
created: 2026-01-22T00:00:00
description: No “NullReferenceException” in .NET Anymore! Mark Your Nullable Reference Type Warnings As Errors As software engineers, we tend to make our easy lives more difficult just by overcomplicating …
tags:
  - clippings
  - nullable
uid: cfee204f-90b4-4d88-afe7-78f49f960f82
modified: 2026-01-22T08:10:32
---

# No "NullReferenceException" in .NET Anymore! Mark Your Nullable Reference Type Warnings As Errors

As software engineers, we tend to make our easy lives more difficult just by overcomplicating simple stuff to remain entertained. We love to explore and apply design patterns for the satisfaction of achieving overly bloated architectures. Yet, we forget to look at the basics that are always staring us in the face. You know, the pesky warnings.

As software engineers, we strive to write good code. Warnings are there to be considered, yet we sometimes tend to ignore them. I am not saying that warnings are always a must, but I am emphasizing the importance of these warnings as they exist to guide us.

## What are Nullable Reference Types?

Nullable Reference Types, or NRT for short, is a C# 8 language feature that essentially dictates the developer to further classify their reference type declarations as null assignable or not, similar to nullable structs (e.g., int?, bool?). These explicit? markers in the context of a struct represent whether the declared variable can hold null or not. If a type with? is declared, it indicates that this variable can hold null and needs to be handled accordingly. For types that don't, you can be assured that there will not be a null there.

Here's an example of how it looks

```cs
public class Person
{
    public string Name { get; set; } // Non-nullable reference type
    public string? MiddleName { get; set; } // Nullable reference type
    public string? LastName { get; set; } // Nullable reference type
```

```cs
public Person(string name, string? middleName, string? lastName)
    {
        Name = name; // Compiler ensures 'name' is not null
        MiddleName = middleName; // 'middleName' can be null
        LastName = lastName; // 'lastName' can be null
    }    public void PrintNames()
    {
        Console.WriteLine($"Name: {Name}");
        if (MiddleName != null)
        {
            Console.WriteLine($"Middle Name: {MiddleName}");
        }
        else
        {
            Console.WriteLine("Middle Name: N/A");
        }
        Console.WriteLine($"Last Name: {LastName ?? "N/A"}");
    }
}public class Program
{
    public static void Main()
    {
        var person = new Person("John", null, "Doe");
        person.PrintNames();
    }
}
```

Notice the "?" at the end of reference type. That's how you express NRT.

## Background

Although nullable struct types (?) have been a great help for us for a very long time, we never had nullable reference types to begin with. Not to mention the ongoing debate over the "Is null a big mistake?" ideology. Other languages like TypeScript had this feature from the start, and Dart introduced sound null safety (same feature, fancier name).

## Why Use NRT?

Because of the pesky NullReferenceException, a runtime exception that must have haunted most developers. Wouldn't it be cool if it could be handled at compile time? This leads to the introduction of this feature. However, C# is a mature language with a lot of consumers. Introducing it as a compiler error would have bombarded entire codebases with red lines. That's why this feature was introduced as warnings, not errors. Moreover, you need to subclassify your whole codebase with? or not. Rust's Options type (null pattern) is another way to approach this problem — akin to the HasValue and Value properties of a nullable value type and explicitly handling SOME and NONE enum types.

### 1. Converts Runtime NullReferenceException to Compile Time

If used properly, this feature guides developers to manage their code with stricter rules that ensure proper null handling by static analyzers. The feature feels more substantial if enabled as errors, moving the runtime NullReferenceException to compile time.

### 2. Enforces proper null case handling

With proper static analyzer hints in place, declaring where to expect nulls and where not to, developers become not only confident in their code but also more responsible. A variable that can't have null must not be checked/handled for null. Similarly, a variable that can be null must be checked/handled for null.

### 3. Empowers Developers to subclassify Reference Types based on nulls

Previously, we couldn't classify whether a variable could hold a null or not throughout the code flow. This led to either NullReferenceException or needless optional chaining operations. Now, with variables subclassified, unexpected NullReferenceException can be handled, and needless optional chaining can be avoided. Developers can also reflect their intended expectations for declared variables.

### 4. Ensures proper documentation for library consumers

The new? type declarations are also picked up by static analyzers. Functions, properties — you name it — will reflect these linter hints and tooling hints (autocomplete, hover docs, etc.).

### 5. Type Hinting for Code Generation (GraphQL, EF Core)

NRT not only helps developers but also paves the way for code generators to utilize this additional information to generate more accurate code. For example, EF Core considers non-nullable string types as NOT NULL database columns (varchar, text, etc.) for migrations. Similarly, HotChocolate GraphQL takes NRT declarations into account, ensuring both the server and client know what is nullable or non-nullable.

### 6. API Development with End-to-End null safety

With the ability to declare nullable and non-nullable types, library authors and consumers can write code with more context. Functions with string? or class? return types or input parameters are a big help for both. Properly implemented, ArgumentNullException may not even be required (though it's still good practice because of few caveats we will discuss more). Tools like.NET HotChocolate GraphQL relay null type expectations to clients, allowing APIs to be consumed without needless optional chaining. [ASP.NET](http://asp.net/) Core also considers these types for REST API input validations by default (no more \[Required\] annotations).

### 7. Unexpected null helps Error detection

When used properly, NRT leads to null-safe situations. However, there are cases where the compiler cannot detect null appearances. Think of it as Test Driven Development, you can only assure cases for those you wrote but not allthe real world cases can be written for a complex app. (Remember the MS Windows global blue screen crash, the bug passed the cases and was pushed to prod), Similarly null may appear when using reflection. Unexpected nulls in non-nullable types help identify bugs and allow developers to manage or report them effectively.

## Why You Should Mark NRT Warnings as Errors

This feature was introduced as warnings to simplify backward compatibility and migration. However, marking these warnings as errors has significant benefits:

### 1. Stricter Compiler Errors Lead to Null-Safe Code

By mitigating or eliminating NullReferenceException, you move the issue to compile-time, ensuring stricter rules for null handling.

### 2. Developer discipline and Team code standards

My motto is: "Solidify the process, not the people, because no one's perfect." Enforcing compiler rules directs developers to write better, null-safe code. This can become a standard across the team, leading to better code practices.

### 3. Less burden for PR Reviewers

Automation reduces manual work. With NRT warnings marked as errors, PR reviewers don't need to manually check for NullReferenceException risks, saving time and effort.

## How To Mark NRT Warnings as Errors

### 1. Project File (`.csproj`)

Add or update the following in your `.csproj` file:

```cs
<PropertyGroup>
  <Nullable>enable</Nullable>
  <WarningsAsErrors>nullable</WarningsAsErrors>
</PropertyGroup>
```

- `<Nullable>enable</Nullable>`: Enables nullable reference type analysis.
- 1<WarningsAsErrors>nullable</WarningsAsErrors>`: Treats nullable warnings as errors.

### 2. `.editorconfig`

Add the following to the `.editorconfig` file in your project `root/.sln` file directory for whole solution:

```cs
# Treat nullable warnings as errors for all C# files
[*.cs]
dotnet_analyzer_diagnostic.severity.nullable = error
```

Combine this with `<Nullable>enable</Nullable>` in your `.csproj`. I myself use this pattern because it's enabled throughot the whole solution. Also i can pick and choose which warnings i want to mark as errors and which i want to ignore.

Here's an example `.editorconfig`

```cs
# New Rule Set
# Description:
```

```cs
# Code files
[*.{cs,vb}]dotnet_diagnostic.CS8600.severity = error
dotnet_diagnostic.CS8601.severity = error
dotnet_diagnostic.CS8602.severity = error
dotnet_diagnostic.CS8603.severity = error
dotnet_diagnostic.CS8604.severity = error
dotnet_diagnostic.CS8613.severity = error
dotnet_diagnostic.CS8614.severity = error
dotnet_diagnostic.CS8618.severity = warning
dotnet_diagnostic.CS8619.severity = error
dotnet_diagnostic.CS8620.severity = error
dotnet_diagnostic.CS8622.severity = error
dotnet_diagnostic.CS8625.severity = error
dotnet_diagnostic.CS8629.severity = error
dotnet_diagnostic.CS8633.severity = error
dotnet_diagnostic.CS8767.severity = error
```

### 3. Visual Studio (Optional)

- Right-click the project in Solution Explorer → Properties.
- Go to the Build tab → Set Treat Warnings as Errors → Specific Warnings: Add nullable.

## Things to Look out for in NRT

Although NRT warnings have lots of benefits but it comes with it's issues. The big ones is massive code edits are required to silence these warnings for large projects when migrated over. Not to mention the agony of working in a NRT enabled project with a libary that did not enable NRT (Every reference type variable is considered nullable by default for that libary by default) and we need to handle all the null cases even where it's not required but detected required.

### 1. Partially backwards compatible

Even though this feature is designed to be backwards compatible. Meaning you can technically get away with enabling the feature and do no edits, the code will compile (like TypeScript, write vanilla JavaScript and it can compile). In case of C#, the problem comes afterwards. The [ASP.NET](http://asp.net/) Core and EF Core by default takes the NRT enabled feature context in account and will break the code flow by considered all reference rtpes required/non-nullable. Invalid required validation may appear on input parameters of REST APIs for optional fields. new migrations may alter the column types to required.

In short, you now have to sub-classify your variables in order to fix the RUNTIME issue you just encountered. (So much for backwards compatibility right?).

### 2. NRT does not work for Dynamic code

NRT is not considered when doing dynamic code. Meaning these are only static warnings or errors, they do not mean that null won't appear at all. Reflection may lead these situations. A great tip to be cautious of is Mapping libraries (like Automapper, Mapster etc) that uses reflection or serializers (Newtonsoft.Json). Also in EF core, if lazy loading is not enabled and you are using Eager Loading.If a required relation data is not Included in the query, it will be null but the compiler will not indicate that, leading to NullReferenceException.

EF Core example

```cs
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Author? Author { get; set; } // Nullable relation
}
```

```cs
// Query without including Author
var post = context.Posts.FirstOrDefault(p => p.Id == 1);
Console.WriteLine(post.Author.Name); // NullReferenceException if lazy loading is off
```

### 3. Unneeded Warnings

You may encounter situations in which you will be given unneeded warnings that you as developer know are not needed but yet you need to comply. For these situations, either use! to bypass or the #nullable. I personally have a set of NRT warnings marked as error but not all of them based on my development style. You will mainly encounter these issues when using EF Core LINQ queries that uses the Expression tree. In these scenarios, `!` sign can be used to dictate that "I know it can't be null, s5op complaining".

## Warning in LINQ

```cs
var users = dbContext.Users
    .Where(u => u.Name != null) // Warning: Possible null reference
    .ToList();
```

### Using! to Suppress Warning

```cs
var users = dbContext.Users
    .Where(u => u.Name! != null) // Suppress warning
    .ToList();
```

## Ignoring Nullable Warnings with #nullable

```cs
#nullable disable
var users = dbContext.Users
    .Where(u => u.Name != null)
    .ToList();
#nullable restore
```
