---
title: Directory.Build.props Migration Design
date: 2026-04-17
tags:
  - dotnet
  - msbuild
  - build-infrastructure
  - dapper-etl
uid: 987aa721-456c-48ac-883c-f7ecd2054d3a
---

# Directory.Build.props Migration — Design

## Goal

Consolidate shared MSBuild properties, conventions, and test-project package references from all six `*.csproj` files into layered `Directory.Build.props` files. Collapse duplication, enforce consistency, and introduce a proper `src/` + `tests/` folder split. Bump every project to `net9.0`.

## Current state

Six projects, flat layout at `dapper-etl/`:

| Project | TFM | Kind | Notes |
|---|---|---|---|
| `Dapper.ETL.AppHost` | net9.0 | Exe (Aspire host) | Has `<Sdk Name="Aspire.AppHost.Sdk" …>` |
| `Dapper.ETL.Library` | net8.0 | Library | No `ImplicitUsings` |
| `Dapper.ETL.Orchestrator` | net8.0 | Exe | `ImplicitUsings=enable` |
| `Dapper.ETL.Orchestrator.Tests` | net8.0 | xunit test | `IsTestProject`, `IsPackable=false`, `ImplicitUsings=enable` |
| `Dapper.ETL.Tests` | net8.0 | xunit test | `IsTestProject`; no `IsPackable`, no `ImplicitUsings` |
| `SQLLite.Integration.Tests` | net8.0 | xunit test | No `IsTestProject`, no `LangVersion`; `ImplicitUsings=enable`, `IsPackable=false` |

`Directory.Packages.props` already exists at the repo root with central package management enabled.

## Target layout

```
dapper-etl/
├── Directory.Build.props          (NEW — root conventions)
├── Directory.Packages.props       (EXISTING location — contents trimmed)
├── Dapper.ETL.sln                 (UPDATED — new project paths)
├── src/
│   ├── Directory.Build.targets    (NEW — src-only: library doc generation)
│   ├── Dapper.ETL.AppHost/
│   ├── Dapper.ETL.Library/
│   └── Dapper.ETL.Orchestrator/
└── tests/
    ├── Directory.Build.props      (NEW — IsTestProject + shared test packages)
    ├── Dapper.ETL.Tests/
    ├── Dapper.ETL.Orchestrator.Tests/
    └── SQLLite.Integration.Tests/
```

## File contents

### `dapper-etl/Directory.Build.props` (root)

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <AnalysisLevel>latest</AnalysisLevel>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <PropertyGroup Condition="'$(CI)' == 'true' OR '$(TF_BUILD)' == 'True' OR '$(GITHUB_ACTIONS)' == 'true'">
    <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
  </PropertyGroup>
</Project>
```

### `dapper-etl/Directory.Packages.props` (trimmed)

Drop `<ManagePackageVersionsCentrally>` and `<CentralPackageTransitivePinningEnabled>` — now in root `Directory.Build.props`. Keep every `<PackageVersion>` item exactly as-is.

### `dapper-etl/src/Directory.Build.targets`

```xml
<Project>
  <PropertyGroup Condition="'$(OutputType)' != 'Exe'">
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

This lives in `Directory.Build.targets` (not `.props`) because `$(OutputType)` is set inside the per-project csproj body; `Directory.Build.props` is imported *before* the csproj body, so `OutputType` would not yet be defined. `Directory.Build.targets` is imported *after*, where the condition evaluates correctly.

CS1591 (missing XML doc on public member) is silenced to prevent `TreatWarningsAsErrors=true` from failing on undocumented public members.

### `dapper-etl/tests/Directory.Build.props`

```xml
<Project>
  <PropertyGroup>
    <IsTestProject>true</IsTestProject>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Moq" />
    <PackageReference Include="coverlet.collector">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Verify.Xunit" />
  </ItemGroup>
</Project>
```

### Per-project csproj after migration

Each csproj keeps only what is *unique to that project*. Everything hoisted above is removed.

**`src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Sdk Name="Aspire.AppHost.Sdk" Version="9.0.0" />
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <IsAspireHost>true</IsAspireHost>
    <UserSecretsId>dapper-etl-apphost</UserSecretsId>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" />
    <PackageReference Include="Aspire.Hosting" />
    <PackageReference Include="Aspire.Hosting.SqlServer" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Dapper.ETL.Library\Dapper.ETL.Library.csproj" IsAspireProjectResource="false" />
    <ProjectReference Include="..\Dapper.ETL.Orchestrator\Dapper.ETL.Orchestrator.csproj" IsAspireProjectResource="true" />
  </ItemGroup>
</Project>
```

**`src/Dapper.ETL.Library/Dapper.ETL.Library.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Dapper" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Serilog" />
  </ItemGroup>
</Project>
```

**`src/Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Configuration" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Serilog" />
    <PackageReference Include="Serilog.Extensions.Logging" />
    <PackageReference Include="Serilog.Enrichers.Environment" />
    <PackageReference Include="Serilog.Sinks.Console" />
    <PackageReference Include="Serilog.Sinks.MSSqlServer" />
    <PackageReference Include="Serilog.Sinks.Seq" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
    <PackageReference Include="OpenTelemetry" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="Spectre.Console.Cli" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
  </ItemGroup>
</Project>
```

**`tests/Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Testcontainers" />
    <PackageReference Include="Testcontainers.MsSql" />
    <PackageReference Include="OpenTelemetry" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Dapper.ETL.Orchestrator\Dapper.ETL.Orchestrator.csproj" />
    <ProjectReference Include="..\..\src\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
  </ItemGroup>
</Project>
```

**`tests/Dapper.ETL.Tests/Dapper.ETL.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <PackageReference Include="Dapper" />
    <PackageReference Include="Microsoft.Data.SqlClient" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
    <PackageReference Include="Serilog" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
  </ItemGroup>
</Project>
```

**`tests/SQLLite.Integration.Tests/SQLLite.Integration.Tests.csproj`**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Dapper" />
    <PackageReference Include="Microsoft.Data.Sqlite" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
  </ItemGroup>
</Project>
```

## Migration steps

1. **Folder moves** — `git mv` each project folder into `src/` or `tests/`. Preserves history.
   - **REMINDER after this step**: surface the out-of-scope items (below) so the user can decide whether to schedule any of them next.
2. **Update `Dapper.ETL.sln`** — rewrite each project's relative path (six edits). Verify GUIDs and configurations are untouched.
3. **Fix `ProjectReference`s** — only test projects need changes. Test projects now sit at `tests/<Name>/`, so references to `src/` targets become `..\..\src\<Name>\<Name>.csproj`. App-side references between sibling `src/*` folders stay unchanged (`..\<Name>\<Name>.csproj`).
4. **Write the three new build files**: root `Directory.Build.props`, `src/Directory.Build.targets`, `tests/Directory.Build.props`.
5. **Trim `Directory.Packages.props`** — remove the two `<Manage…>` properties.
6. **Rewrite each csproj** to the slim versions shown above.
7. **Audit path references** outside the project files:
   - `docker-compose.yml`
   - `scripts/`
   - `BUILD_SETUP.md`, `ASPIRE_CONTAINERS_GUIDE.md`, `QUICK_START.md`, `FAQ_AND_EXAMPLES.md`
   - Any `launchSettings.json`, coverage tool configs, VS/Rider run configurations
8. **Build + test pass** — `dotnet restore && dotnet build && dotnet test` from `dapper-etl/`. Zero warnings expected (because of `TreatWarningsAsErrors=true`).
9. **Aspire smoke test** — launch the AppHost to confirm Aspire dashboard + project discovery still resolves the relocated Orchestrator.
10. **Commit.**

## Risks

- **net9.0 bump** on Library, Orchestrator, and all test projects. Package compatibility looks fine (Aspire 9.0, M.Extensions.* 9.0/10.0, xunit 2.7.1, Test.Sdk 17.9.0 all support net9.0), but must be verified with a full build+test pass.
- **`TreatWarningsAsErrors=true`** will flip latent warnings into build failures on first try. Triage: fix real issues; only `<NoWarn>` specific codes as a last resort.
- **`EnforceCodeStyleInBuild` + `AnalysisLevel=latest`** may surface IDE-style analyzer diagnostics (IDE0xxx) as errors. Same triage.
- **Aspire AppHost** — the `ProjectReference` with `IsAspireProjectResource="true"` and the dashboard's project discovery need a smoke-test launch after moves.
- **`SQLLite.Integration.Tests`** currently lacks `IsTestProject` and `LangVersion`; both get added for free via the props — behavior should improve, not regress.
- **`GenerateDocumentationFile=true`** on the Library will emit `CS1591` warnings; silenced in `src/Directory.Build.props`.

## Out of scope (reminder after step 1)

- Rename `SQLLite.Integration.Tests` → `SQLite.Integration.Tests` (typo).
- Introduce or consolidate `.editorconfig`; reconcile with `Dapper.ETL.sln.DotSettings.user`.
- Add analyzers beyond the built-in .NET SDK set (e.g., Roslynator, StyleCop, Meziantou.Analyzer).
- Adjust `Directory.Packages.props` package versions (pure hoisting, no upgrades).

## Success criteria

- Every `*.csproj` contains only project-unique properties and `PackageReference`s.
- `dotnet build` and `dotnet test` pass from a clean checkout with zero warnings.
- Aspire AppHost launches and discovers the Orchestrator.
- Solution file opens cleanly in Visual Studio / Rider with all six projects resolving.
- No path-based documentation or script references are broken.
