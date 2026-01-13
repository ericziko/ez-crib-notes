---
title: 📰 Microsoft just released System.CommandLine — And I made it even easier to use
source: https://pieterjandeclippel.medium.com/microsoft-just-released-system-commandline-and-i-made-it-even-easier-to-use-5a0193e97162
author:
  - "[[Pieterjan De Clippel]]"
published: 2025-11-14
created: 2026-01-07
description: Microsoft just released System.CommandLine — And I made it even easier to use The long awaited System.CommandLine has just been released. This NuGet package allows developers to build their CLI …
tags:
  - clippings
updated: 2026-01-07T22:50
---

# 📰 Microsoft just released System.CommandLine — And I made it even easier to use

The long awaited [System.CommandLine](https://www.nuget.org/packages/System.CommandLine) has just been released. This NuGet package allows developers to build their CLI tools as a console-application more easily. Here's a basic example

To make it easier to debug, we can use the following `Properties/launchSettings.json`

Eventually we can use the following in the csproj file:

The code above works as expected, but is pretty messy. We can do better

## Using a source-generator

Install the following packages in the project

- [MintPlayer.CliGenerator](https://www.nuget.org/packages/MintPlayer.CliGenerator)
- [MintPlayer.CliGenerator.Attributes](https://www.nuget.org/packages/MintPlayer.CliGenerator.Attributes)

After this we can rewrite this mess of sequential code in a more structured way

For the source-generator to work correctly you'll need to add the `GeneratePathProperty="true"` on the `MintPlayer.CliGenerator.Attributes` package reference.

Behind the scenes Visual Studio (and.NET) will generate additional code to wire up the CLI-tool.

## Expanding

You have 2 options to expand this tool. You can either add nested classes, or use the `[CliParentCommand]` attribute. The latter option would allow you to easily split your code over multiple nested files (`DependentUpon`). In below snippet I added a Farewell subcommand, just by adding a nested class. No other action is needed to wire it up:

Now we can add another launch profile to debug this command:

Updated file

## Results

![](<_resources/📰 Microsoft just released System.CommandLine — And I made it even easier to use/e430a1e23273b3b4c6ab9319372eeb7c_MD5.webp>)

Greet Miss Diane 3 times

![](<_resources/📰 Microsoft just released System.CommandLine — And I made it even easier to use/4ca98c4053cee71a564c2ea087f611d9_MD5.webp>)

Shout at Mister Jones

![](<_resources/📰 Microsoft just released System.CommandLine — And I made it even easier to use/e6b1a0d415a3c6d679f68efdc649eeb5_MD5.webp>)

Shout 5 times at Jack

![](<_resources/📰 Microsoft just released System.CommandLine — And I made it even easier to use/eda5030add2168ca2cdd316c23456be5_MD5.webp>)

Say goodbye to Alice

## A more recommended approach

Don't want all your commands in the same file? Seems obvious. You can split your code per class, and apply the `[CliParentCommand(...)]` attribute to indicate under what command this one should be nested

Or you can even split off the entrypoint:

To top it all off, we can arrange nested files in the solution explorer:

## Dependency injection

You can easily register your classes inside the `Program.cs` and inject them inside a command. But there's a better way to do that. Just install following packages:

- [MintPlayer.SourceGenerators](https://www.nuget.org/packages/MintPlayer.SourceGenerators)
- [MintPlayer.SourceGenerators.Attributes](https://www.nuget.org/packages/MintPlayer.SourceGenerators.Attributes)

And again add `GeneratePathProperty="true"` on the Attributes package

Now let's add a service

When we add the `[Register(...)]` attribute to a class, the source-generator automatically generates code we can use in our Program class:

Add AddDemoServices()

At last we can Inject the `IDemoService` in the command and use it

Make sure the class is partial

You can check out the generated code in the Solution Explorer under Analyzers

![](<_resources/📰 Microsoft just released System.CommandLine — And I made it even easier to use/66f11c6ccb89fa2ced4a2839fb636165_MD5.webp>)

## Source code

The code is hosted here:

- [Demo](https://github.com/PieterjanDeClippel/DotnetGreeter)
- [Source generator](https://github.com/MintPlayer/MintPlayer.Dotnet.Tools/tree/master/SourceGenerators/Cli)

.NET + Angular Developer
