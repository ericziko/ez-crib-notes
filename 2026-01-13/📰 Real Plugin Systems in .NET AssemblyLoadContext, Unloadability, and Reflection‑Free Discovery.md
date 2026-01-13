---
title: 📰 Real Plugin Systems in .NET AssemblyLoadContext, Unloadability, and Reflection‑Free Discovery
source: https://medium.com/@jordansrowles/real-plugin-systems-in-net-assemblyloadcontext-unloadability-and-reflection-free-discovery-81f920c83644
author:
  - "[[Jordan Rowles]]"
published: 2026-01-01
created: 2026-01-07
description: "Real Plugin Systems in .NET: AssemblyLoadContext, Unloadability, and Reflection‑Free Discovery Plugin systems in .NET sound straightforward in theory. You load a DLL, find a class implementing your …"
tags:
  - clippings
updated: 2026-01-07T22:20
uid: 17f75d6b-9548-4df3-ba3d-4ad74d9da4e9
---

# 📰 Real Plugin Systems in .NET AssemblyLoadContext, Unloadability, and Reflection‑Free Discovery

![|640x360](https://miro.medium.com/v2/resize:fit:640/format:webp/1*y1HaGkr-SAiFo3sHU_UwJQ.png)

Plugin systems in.NET sound straightforward in theory. You load a DLL, find a class implementing your interface, run it, and then unload it when you're done. The demo code always works. The trouble starts when you try to update a plugin while the host is running. Files stay locked. Old plugins remain in memory even after you've called unload. New plugins load but still reference old dependency versions. The bugs only manifest after a few reload cycles, which makes them particularly difficult to track down.

This article covers the production version of plugin systems. Not just how to use `AssemblyLoadContext`, but how to build something that survives real failure modes. We'll look at collectible contexts, shadow copying, metadata-only discovery, and the specific patterns that prevent the most common unloadability problems.

```c
Table of Contents

01. What we're actually trying to achieve
02. The five lies of plugin systems
03. The naive approach (and why it fails)
04. AssemblyLoadContext: the mental model that actually helps
 |- 04.1 Default ALC vs plugin ALC
05. Step 1: Define the contract boundary
06. Step 2: Build a collectible ALC with dependency resolution
07. Step 3: Shadow copy, or accept you can't update plugins
08. Step 4: Load, activate, and not accidently leak
09. Step 5: "Collectible" doesn't mean collected
 |- 09.1 How to actually test unloadability
10. The real unload killers (the stuff that keeps ALCs alive)
11. The static cache problem 
 |- 11.1 The host accidently becomes a plugin type cache
 |- 11.2 Fix patterns
12. Native DLL hell
13. Reflection-free discovery with System.Reflection.Metadata
 |- 13.1 A simple manifest attribute
 |- 13.2 Metadata reader implementation
14. Putting it all together: a small, usable host
15. Hot reload: FileSystemWatcher is not a true oracle
16. When to go out of process
```

## What we're actually trying to achieve

When people say "plugin system", they sometimes mean different things. Some want in-process extensibility where code loads into your process, executes, and then (maybe) unloads. Others want hot reload capabilities where plugins can be updated without restarting the host. There's also isolation, where plugin A can't break plugin B. And there's discovery, where you need to scan hundreds of plugins without actually loading them, which means no file locks and no static constructors firing.

You can do all of these, but you don't get them for free. Every one has a cost, and that cost usually shows up in subtle ways that only manifest in production.

In this article we'll build an in-process system that loads plugins into a collectible `AssemblyLoadContext`, avoids the most common unloadability footguns, uses shadow copying so plugin binaries can be replaced, discovers plugin metadata using `System.Reflection.Metadata` without loading assemblies, and has a realistic testing strategy for verifying that things actually unload.

The source code can be found [here](https://github.com/jordansrowles/Article-Plugin-System).

## The five lies of plugin systems

These are the things your first implementation will assume, and production will punish you for assuming.

The first lie is that if you call `Unload()`, it unloads.

The second is that if an `AssemblyLoadContext` is collectible, it will collect.

The third is that if you don't store the `Assembly` reference, you're not holding references.

The fourth is that native dependencies behave like managed ones.

And the fifth is that discovery is just `Assembly.LoadFrom` plus `GetTypes()`.

Let's build a system that doesn't rely on those lies.

## The naive approach (and why it fails)

The classic first attempt might look a little like this,

```c
public sealed class NaivePluginLoader
{
    private Assembly? _assembly;

    public void Load(string path)
    {
        _assembly = Assembly.LoadFrom(path);
    }
}
```

It fails in three different ways.

First, there's no unload. `Assembly.LoadFrom` loads into a non-collectible context, which means that assembly is effectively process-lifetime.

Second, there are file locks, especially on Windows, because the runtime memory-maps the file.

Third, you get type identity chaos. If you later load the "same" dependency somewhere else, you can end up with two copies of the same assembly name, which means two different types that look identical but aren't assignment-compatible.

So we need `AssemblyLoadContext`.

## AssemblyLoadContext: the mental model that actually helps

An `AssemblyLoadContext` (ALC) is a loader universe. Two types are considered the "same type" only if all of these match: assembly name, assembly version/culture/public key token, and the `AssemblyLoadContext` that loaded it.

That last bullet is where most plugin systems break.

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*B1K2Fd19F2dhHpQGoWaZcw.png)

### Default ALC vs plugin ALC

The default ALC is where your host app lives. A custom ALC is where you want plugin assemblies to live. If you load your shared interfaces and DTOs into both, you now have two `IPlugin` interfaces that look identical but are not assignment-compatible.

So the first real design decision is this: your contracts must live in the default ALC. Everything else flows from that.

## Step 1: Define the contract boundary (the only sane one)

Put your plugin contracts in a dedicated assembly that the host references. Keep it boring. Keep it small. Keep it stable.

```c
namespace Plugin.Abstractions;

public interface IPlugin
{
    string Name { get; }
    ValueTask<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken);
}

public sealed record PluginRequest(string Payload, IReadOnlyDictionary<string, string> Parameters);
public sealed record PluginResult(bool Success, string Output);
```

Two important constraints here. First, don't leak plugin types across the boundary. No `Type` parameters, no `object` bags, no "generic `T` result" that can capture plugin-defined types. Second, keep DTOs simple. Strings, numbers, arrays, and dictionaries. If you need rich schemas, move to a serialisation boundary.

## Step 2: Build a collectible ALC with dependency resolution

This is the standard pattern. `AssemblyDependencyResolver` handles "find dependency next to this plugin" for you.

```c
using System.Reflection;
using System.Runtime.Loader;

public sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginMainAssemblyPath)
        : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginMainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // IMPORTANT: we want contracts to unify with the host.
        // If the plugin folder contains a private copy of Plugin.Abstractions, don't load it here.
        if (assemblyName.Name is "Plugin.Abstractions")
        {
            return null; // fall back to default ALC
        }

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path is null ? null : LoadFromAssemblyPath(path);
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return path is null ? nint.Zero : LoadUnmanagedDllFromPath(path);
    }
}
```

This is necessary, but not sufficient. If you stop here, your next problem will be that the plugin DLL stays locked, even after unload. That brings us to the least glamorous trick in plugin systems.

## Step 3: Shadow copy, or accept you can't update plugins

If you load a DLL from `C:\Plugins\MyPlugin\MyPlugin.dll`, Windows will typically keep that file locked whilst it's memory-mapped. Even if the ALC unloads, the file can remain locked until the GC actually collects the loader and finalisers run.

So the standard production trick is to copy the plugin folder to a unique "shadow" location, load from the shadow copy, and leave the original folder free to replace.

```c
public static class ShadowCopy
{
    public static string CreateShadowCopyDirectory(string pluginDirectory)
    {
        var shadowRoot = Path.Combine(Path.GetTempPath(), "PluginShadows");
        Directory.CreateDirectory(shadowRoot);

        var shadowDir = Path.Combine(
            shadowRoot,
            $"{Path.GetFileName(pluginDirectory)}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");

        CopyDirectory(pluginDirectory, shadowDir);
        return shadowDir;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var dir in Directory.GetDirectories(source))
        {
            CopyDirectory(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
    }
}
```

Yes, it feels gross. It is gross. But it's also the difference between "hot reload works" and "we have to restart prod to update a plugin".

## Step 4: Load, activate, and not accidentally leak the world

The safest shape I've found is to return a handle that owns the `AssemblyLoadContext`, the plugin instance, and the shadow directory, and makes unload a deliberate operation.

```c
using Plugin.Abstractions;
using System.Reflection;
using System.Runtime.CompilerServices;

public sealed class PluginHandle : IAsyncDisposable
{
    private readonly PluginLoadContext _alc;
    private readonly string _shadowDirectory;
    private readonly IPlugin _plugin;
    private bool _disposed;

    public string Name => _plugin.Name;

    // Diagnostics/testing only. A WeakReference is safe to hold in the host.
    // Don't expose the ALC itself unless you enjoy unloading bugs.
    public WeakReference TrackUnloadability() => new(_alc, trackResurrection: true);

    private PluginHandle(PluginLoadContext alc, string shadowDirectory, IPlugin plugin)
    {
        _alc = alc;
        _shadowDirectory = shadowDirectory;
        _plugin = plugin;
    }

    public ValueTask<PluginResult> ExecuteAsync(PluginRequest request, CancellationToken cancellationToken)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PluginHandle));
        return _plugin.ExecuteAsync(request, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // Give plugin a chance to clean up. If you define a richer lifecycle interface,
        // call it here.
        if (_plugin is IAsyncDisposable ad)
            await ad.DisposeAsync();
        else if (_plugin is IDisposable d)
            d.Dispose();

        _alc.Unload();

        // NOTE: deleting the shadow directory is best-effort.
        // If something still has a handle open, deletion will fail on Windows.
        TryDeleteDirectory(_shadowDirectory);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // Ignore. If you care, log it.
        }
    }

    public static PluginHandle LoadFromPluginDirectory(string pluginDirectory)
    {
        var shadowDir = ShadowCopy.CreateShadowCopyDirectory(pluginDirectory);
        var mainAssemblyPath = FindPluginEntryAssemblyPath(pluginDirectory, shadowDir);
        return LoadFromAssemblyPath(mainAssemblyPath, shadowDir);
    }

    private static string FindPluginEntryAssemblyPath(string originalPluginDirectory, string shadowDir)
    {
        // Convention first: if the directory is "Acme.CsvCleaner", prefer "Acme.CsvCleaner.dll".
        var candidate = Path.Combine(shadowDir, $"{Path.GetFileName(originalPluginDirectory)}.dll");
        if (File.Exists(candidate))
            return candidate;

        // Otherwise, fall back to scanning for the assembly-level PluginManifest attribute.
        foreach (var dll in Directory.EnumerateFiles(shadowDir, "*.dll", SearchOption.TopDirectoryOnly))
        {
            if (PluginDiscovery.Discover(dll) is not null)
                return dll;
        }

        throw new InvalidOperationException(
            $"No plugin entry assembly found in '{originalPluginDirectory}'. " +
            "Either name the main DLL the same as the plugin folder, or add [assembly: PluginManifest(...)] to the plugin assembly.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PluginHandle LoadFromAssemblyPath(string pluginMainAssemblyPath, string shadowDir)
    {
        var alc = new PluginLoadContext(pluginMainAssemblyPath);

        var assembly = alc.LoadFromAssemblyPath(pluginMainAssemblyPath);
        var pluginType = FindPluginType(assembly);

        var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;
        return new PluginHandle(alc, shadowDir, plugin);
    }

    private static Type FindPluginType(Assembly assembly)
    {
        var candidates = assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && typeof(IPlugin).IsAssignableFrom(t))
            .ToList();

        if (candidates.Count == 0)
            throw new InvalidOperationException("No IPlugin implementation found.");

        if (candidates.Count > 1)
            throw new InvalidOperationException("Multiple IPlugin implementations found. Use an attribute or explicit entry point.");

        return candidates[0];
    }
}
```

That `NoInlining` attribute looks like superstition. It isn't. It reduces the chance the JIT keeps references alive longer than you expect when you later try to verify that the ALC collected.

## Step 5: "Collectible" doesn't mean collected

This is the part people skip, and then they're shocked when hot reload slowly eats memory. Calling `_alc.Unload()` only means "this ALC is eligible for collection when nothing references it". It doesn't guarantee collection.

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*5qBsFrCpPUGI4B4r4YLQLQ.png)

### How to actually test unloadability

You need a `WeakReference` to the `AssemblyLoadContext` and you need to force GC.

```c
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

public static class Unloadability
{
    public static bool UnloadAndVerify(Func<WeakReference> buildWeakRef, int maxAttempts = 10)
    {
        var weakRef = buildWeakRef();

        for (var i = 0; i < maxAttempts && weakRef.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        return !weakRef.IsAlive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static WeakReference LoadThenUnload(string pluginDirectory)
    {
        var handle = PluginHandle.LoadFromPluginDirectory(pluginDirectory);

        // This is the key: hold only a weak reference to the ALC.
        var alcWeak = handle.TrackUnloadability();

        // Use the plugin so we don't only test the trivial case.
        handle.ExecuteAsync(new PluginRequest("ping", new Dictionary<string, string>()), CancellationToken.None)
            .AsTask().GetAwaiter().GetResult();

        handle.DisposeAsync().AsTask().GetAwaiter().GetResult();
        return alcWeak;
    }
}
```

The more important point is the shape. Do the load/unload in a non-inlined method. Keep references scoped tightly. Force GC a few times. If it doesn't unload under those conditions, it won't unload in your real app.

## The real unload killers (the stuff that keeps ALCs alive)

If your ALC doesn't collect, it's almost always one of these. Host caches that capture plugin types are the most common culprit. Things like serialisers, mappers, DI containers, and logging enrichers all tend to cache type metadata. Static event handlers are another classic problem: the plugin subscribes to a host event and never unsubscribes. Background work is the third major category: the plugin started a timer, task, or thread that still runs. Delegates stored in the host will do it too: the host caches a `Func<...>` created by the plugin. And accidentally shared dependencies can pin things: a library loaded in the default ALC holds references to plugin types.

Let's hit the two biggest ones.

## The static cache problem (done in a way that actually happens)

The most common "how is this leaking?" scenario looks like this. The host has a singleton cache. The plugin passes a plugin-defined type into that cache. The cache is in the default ALC. Congratulations, you just pinned the plugin ALC.

Here's a concrete example with `System.Text.Json`.

### The host accidentally becomes a plugin type cache

```c
public sealed class HostJson
{
    // Lives in default ALC for the lifetime of the host.
    public JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);

    public string Serialize(object value)
        => JsonSerializer.Serialize(value, Options);
}
```

If a plugin calls `hostJson.Serialize(new PluginPrivateType(...))`, the host's long-lived `JsonSerializerOptions` can cache metadata that references the plugin's types. Unload will fail, and you'll chase ghosts.

### Fix pattersn

You have a few honest options. First, don't cross the boundary with plugin types. Convert plugin internals to contract DTOs. Second, make caches plugin-scoped, not host-scoped. Third, use serialisation boundaries deliberately: the plugin returns a JSON string, and the host never sees plugin types.

For plugin systems, option one is usually the right one.

## Native DLL hell (and why "just unload it" is wishful thinking)

Managed assemblies are relatively well-behaved. Native libraries are not.

Even when you load native dependencies through `AssemblyLoadContext.LoadUnmanagedDll`, you can still get stuck with global process loader behaviour, native libraries that keep their own global state, and "version already loaded" problems.

If you have plugins with native dependencies and you need true unload/version isolation, you generally end up with one of these options. First, shadow copy native DLLs too, using unique paths per load. Second, out-of-process plugins, which is the only real "kill it with fire" boundary. Third, live with it and document that native deps are process-lifetime.

On Windows you can at least detect if something is still loaded, like this

```c
using System.Runtime.InteropServices;

public static class NativeModules
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint GetModuleHandle(string lpModuleName);

    public static bool IsLoaded(string moduleName) => GetModuleHandle(moduleName) != nint.Zero;
}
```

It won't save you, but it will stop you lying to yourself.

## Reflection-free discovery with System.Reflection.Metadata

Discovery is where most plugin systems accidentally do the worst possible thing: load every plugin assembly just to read its name. That locks files, runs module initialisers, and sometimes crashes the scanner because one plugin has a missing dependency.

The fix is metadata-only inspection.

### A simple manifest attribute

Put the manifest attribute in your contracts assembly so the name is stable,

```c
namespace Plugin.Abstractions;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class PluginManifestAttribute : Attribute
{
    public PluginManifestAttribute(string id, string version)
    {
        Id = id;
        Version = version;
    }

    public string Id { get; }
    public string Version { get; }
    public string? Description { get; init; }
}
```

In the plugin assembly,

```c
using Plugin.Abstractions;

[assembly: PluginManifest("Acme.CsvCleaner", "1.2.0", Description = "Cleans weird CSVs from vendors")]
```

Now we can scan plugin DLLs without loading them.

### Metadata reader implementation

```c
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

public sealed record DiscoveredPlugin(
    string Id,
    string Version,
    string? Description,
    string AssemblyPath,
    IReadOnlyList<string> AssemblyReferences);

public static class PluginDiscovery
{
    private const string ManifestAttributeFullName = "Plugin.Abstractions.PluginManifestAttribute";

    public static DiscoveredPlugin? Discover(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        using var pe = new PEReader(stream);

        if (!pe.HasMetadata)
            return null;

        var reader = pe.GetMetadataReader();

        var manifest = TryReadManifest(reader);
        if (manifest is null)
            return null;

        var refs = reader.AssemblyReferences
            .Select(h => reader.GetAssemblyReference(h))
            .Select(r => reader.GetString(r.Name))
            .OrderBy(x => x)
            .ToArray();

        return new DiscoveredPlugin(
            Id: manifest.Value.Id,
            Version: manifest.Value.Version,
            Description: manifest.Value.Description,
            AssemblyPath: assemblyPath,
            AssemblyReferences: refs);
    }

    private static (string Id, string Version, string? Description)? TryReadManifest(MetadataReader reader)
    {
        var assemblyDef = reader.GetAssemblyDefinition();

        foreach (var attrHandle in assemblyDef.GetCustomAttributes())
        {
            var attr = reader.GetCustomAttribute(attrHandle);
            var typeName = GetAttributeTypeFullName(reader, attr);
            if (!string.Equals(typeName, ManifestAttributeFullName, StringComparison.Ordinal))
                continue;

            var decoded = attr.DecodeValue(new SimpleAttributeTypeProvider());

            // ctor: (string id, string version)
            var id = (string)decoded.FixedArguments[0].Value!;
            var version = (string)decoded.FixedArguments[1].Value!;

            string? description = null;
            foreach (var named in decoded.NamedArguments)
            {
                if (named.Name == "Description")
                    description = named.Value.Value as string;
            }

            return (id, version, description);
        }

        return null;
    }

    private static string GetAttributeTypeFullName(MetadataReader reader, CustomAttribute attribute)
    {
        EntityHandle container;

        switch (attribute.Constructor.Kind)
        {
            case HandleKind.MemberReference:
                var memberRef = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                container = memberRef.Parent;
                break;

            case HandleKind.MethodDefinition:
                var methodDef = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                container = methodDef.GetDeclaringType();
                break;

            default:
                return string.Empty;
        }

        return container.Kind switch
        {
            HandleKind.TypeReference => GetTypeRefFullName(reader, (TypeReferenceHandle)container),
            HandleKind.TypeDefinition => GetTypeDefFullName(reader, (TypeDefinitionHandle)container),
            _ => string.Empty
        };
    }

    private static string GetTypeRefFullName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var tr = reader.GetTypeReference(handle);
        var ns = reader.GetString(tr.Namespace);
        var name = reader.GetString(tr.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private static string GetTypeDefFullName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var td = reader.GetTypeDefinition(handle);
        var ns = reader.GetString(td.Namespace);
        var name = reader.GetString(td.Name);
        return string.IsNullOrEmpty(ns) ? name : $"{ns}.{name}";
    }

    private sealed class SimpleAttributeTypeProvider : ICustomAttributeTypeProvider<Type>
    {
        public Type GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Boolean => typeof(bool),
            PrimitiveTypeCode.Byte => typeof(byte),
            PrimitiveTypeCode.Int16 => typeof(short),
            PrimitiveTypeCode.Int32 => typeof(int),
            PrimitiveTypeCode.Int64 => typeof(long),
            PrimitiveTypeCode.String => typeof(string),
            _ => typeof(object)
        };

        public Type GetSystemType() => typeof(Type);
        public Type GetSZArrayType(Type elementType) => elementType.MakeArrayType();

        public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            => typeof(object);

        public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            => typeof(object);

        public Type GetTypeFromSerializedName(string name)
            => Type.GetType(name, throwOnError: false) ?? typeof(object);

        public PrimitiveTypeCode GetUnderlyingEnumType(Type type)
            => PrimitiveTypeCode.Int32;

        public bool IsSystemType(Type type)
            => type == typeof(Type);
    }
}
```

This gives you fast discovery with zero assembly loads. And more importantly, you can scan a plugin directory even if some plugins are broken, missing dependencies, or targeting a different runtime.

## Putting it together: a small, usable host

At minimum you want a catalogue (metadata), a loader (returns `PluginHandle`), and a concurrency policy (don't reload whilst executing).

Here's a deliberately small "host" shape

```c
using System.Collections.Concurrent;
using Plugin.Abstractions;

public sealed class PluginHost : IAsyncDisposable
{
    private readonly string _pluginRoot;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly ConcurrentDictionary<string, PluginHandle> _loaded = new();

    public PluginHost(string pluginRoot)
    {
        _pluginRoot = pluginRoot;
    }

    public IEnumerable<DiscoveredPlugin> DiscoverAll()
    {
        foreach (var dll in Directory.EnumerateFiles(_pluginRoot, "*.dll", SearchOption.AllDirectories))
        {
            var discovered = PluginDiscovery.Discover(dll);
            if (discovered is not null)
                yield return discovered;
        }
    }

    public async ValueTask<PluginResult> ExecuteAsync(string pluginId, PluginRequest request, CancellationToken cancellationToken)
    {
        var gate = _locks.GetOrAdd(pluginId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            var handle = _loaded.GetOrAdd(pluginId, _ => Load(pluginId));
            return await handle.ExecuteAsync(request, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask ReloadAsync(string pluginId)
    {
        var gate = _locks.GetOrAdd(pluginId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();

        try
        {
            if (_loaded.TryRemove(pluginId, out var existing))
                await existing.DisposeAsync();

            _loaded[pluginId] = Load(pluginId);
        }
        finally
        {
            gate.Release();
        }
    }

    private PluginHandle Load(string pluginId)
    {
        var pluginDir = Path.Combine(_pluginRoot, pluginId);
        if (!Directory.Exists(pluginDir))
            throw new DirectoryNotFoundException($"Plugin directory not found: {pluginDir}");

        return PluginHandle.LoadFromPluginDirectory(pluginDir);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var handle in _loaded.Values)
            await handle.DisposeAsync();

        foreach (var gate in _locks.Values)
            gate.Dispose();

        _loaded.Clear();
        _locks.Clear();
    }
}
```

This is not the only architecture. But it's a shape that doesn't fight unloadability.

![](https://miro.medium.com/v2/resize:fit:640/format:webp/1*_UXxtSPggujP38tSjP74OA.png)

## Hot reload: FileSystemWatcher is not a truth oracle

If you add a `FileSystemWatcher` and call it done, you're going to get duplicate events, events whilst the file is still being written, and missed events.

The minimal pragmatic approach is to debounce, delay a bit on change, try reload, and if it fails, keep the old plugin.

I'm not dumping a full hot-reload implementation here because it's 80% "event weirdness handling", but the rule is simple: reload should be best-effort and reversible.

## When to stop fighting and go out-of-process

In-process plugins are great until you need one of these: hard isolation where a plugin crash must not crash the host, native libraries with strict version isolation, or security boundaries for untrusted plugins.

At that point, load plugins out-of-process and talk over pipes or sockets. It's more work up front, and less work for the next three years.
