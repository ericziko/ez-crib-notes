# Directory.Build.props Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoist shared MSBuild properties, analyzers, and test-project package references out of every `*.csproj` into layered `Directory.Build.props` / `Directory.Build.targets` files, introduce a `src/` + `tests/` folder split, and bump every project to `net9.0`.

**Architecture:** Three new build files — root `dapper-etl/Directory.Build.props` (universal conventions), `dapper-etl/src/Directory.Build.targets` (library doc generation), `dapper-etl/tests/Directory.Build.props` (test-project conventions + shared package refs). The existing `dapper-etl/Directory.Packages.props` stays at the root with its `<PackageVersion>` list intact but has its two `<Manage…>` meta-properties moved into the root `Directory.Build.props`. All six projects move into `src/` (3) or `tests/` (3). Migration proceeds in ten commits, each one leaving the solution build-and-test-green.

**Tech Stack:** .NET 9 SDK, MSBuild, C# 13 (`LangVersion=latest`), xunit, Aspire.Hosting 9.0, Verify.Xunit, coverlet.collector, Moq, Testcontainers.

**Working directory:** All commands run from `dapper-etl/` unless stated otherwise.

**Git note:** This repository's `.git/` lives at the parent (`ez-crib-notes/`). If the sandbox blocks `git` index writes, rerun with sandbox disabled; commits are intentional and require it.

---

## Reference — final file contents

These appear verbatim in later tasks; keeping one copy here for orientation. If a task asks you to write one of these files, use the version shown in the task (authoritative), not this reference.

- Root `Directory.Build.props` — universal MSBuild properties.
- `src/Directory.Build.targets` — `GenerateDocumentationFile` only for non-Exe projects.
- `tests/Directory.Build.props` — `IsTestProject`, `IsPackable=false`, shared test `PackageReference`s.
- `Directory.Packages.props` — unchanged `<PackageVersion>` list; two meta-properties removed.

---

### Task 1: Baseline snapshot

**Files:**
- No files modified.

Capture the current green state so later diffs are comparable. If baseline fails, stop and surface the failure before making changes.

- [ ] **Step 1: Restore**

Run: `dotnet restore Dapper.ETL.sln`
Expected: exit 0, no errors.

- [ ] **Step 2: Build**

Run: `dotnet build Dapper.ETL.sln --no-restore`
Expected: exit 0. Record the build-summary line (e.g., `Build succeeded. 0 Warning(s) 0 Error(s)`). If warnings exist, record the count — Task 6 will enable warnings-as-errors and you must not regress past this baseline.

- [ ] **Step 3: Test**

Run: `dotnet test Dapper.ETL.sln --no-build`
Expected: exit 0, all tests pass. Record total test count.

- [ ] **Step 4: Confirm no uncommitted work**

Run: `git status`
Expected: `nothing to commit, working tree clean`. If dirty, stop and ask the user.

---

### Task 2: Create `src/` and `tests/` folders; move project folders

**Files:**
- Create (directory): `src/`
- Create (directory): `tests/`
- Move (git): `Dapper.ETL.AppHost/` → `src/Dapper.ETL.AppHost/`
- Move (git): `Dapper.ETL.Library/` → `src/Dapper.ETL.Library/`
- Move (git): `Dapper.ETL.Orchestrator/` → `src/Dapper.ETL.Orchestrator/`
- Move (git): `Dapper.ETL.Tests/` → `tests/Dapper.ETL.Tests/`
- Move (git): `Dapper.ETL.Orchestrator.Tests/` → `tests/Dapper.ETL.Orchestrator.Tests/`
- Move (git): `SQLLite.Integration.Tests/` → `tests/SQLLite.Integration.Tests/`

- [ ] **Step 1: Create the two folders**

Run:
```bash
mkdir -p src tests
```

- [ ] **Step 2: Move the three app projects with `git mv` (preserves history)**

Run:
```bash
git mv Dapper.ETL.AppHost src/Dapper.ETL.AppHost
git mv Dapper.ETL.Library src/Dapper.ETL.Library
git mv Dapper.ETL.Orchestrator src/Dapper.ETL.Orchestrator
```

- [ ] **Step 3: Move the three test projects with `git mv`**

Run:
```bash
git mv Dapper.ETL.Tests tests/Dapper.ETL.Tests
git mv Dapper.ETL.Orchestrator.Tests tests/Dapper.ETL.Orchestrator.Tests
git mv SQLLite.Integration.Tests tests/SQLLite.Integration.Tests
```

- [ ] **Step 4: Confirm the moves**

Run: `git status`
Expected: six `renamed:` entries (one per project). No other changes.

- [ ] **Step 5: Commit the folder reorganization**

Run:
```bash
git add -A
git commit -m "Move projects into src/ and tests/ folders"
```

---

### Task 3: Update `Dapper.ETL.sln` project paths

**Files:**
- Modify: `Dapper.ETL.sln`

The `.sln` file references each project by a relative path in a `Project(…)` line. After Task 2's moves, every path is stale.

- [ ] **Step 1: Inspect the current sln Project lines**

Run: `grep -n '^Project(' Dapper.ETL.sln`
Expected: six lines, each like `Project("{GUID}") = "Dapper.ETL.AppHost", "Dapper.ETL.AppHost\Dapper.ETL.AppHost.csproj", "{PROJ-GUID}"`.

- [ ] **Step 2: Rewrite the six paths**

For each project, change the csproj path from `<Name>\<Name>.csproj` to `src\<Name>\<Name>.csproj` (or `tests\<Name>\<Name>.csproj`).

Edit `Dapper.ETL.sln` and perform these exact path substitutions (use forward-slash on non-Windows, `sed`-safe):

| Old path | New path |
|---|---|
| `Dapper.ETL.AppHost\Dapper.ETL.AppHost.csproj` | `src\Dapper.ETL.AppHost\Dapper.ETL.AppHost.csproj` |
| `Dapper.ETL.Library\Dapper.ETL.Library.csproj` | `src\Dapper.ETL.Library\Dapper.ETL.Library.csproj` |
| `Dapper.ETL.Orchestrator\Dapper.ETL.Orchestrator.csproj` | `src\Dapper.ETL.Orchestrator\Dapper.ETL.Orchestrator.csproj` |
| `Dapper.ETL.Orchestrator.Tests\Dapper.ETL.Orchestrator.Tests.csproj` | `tests\Dapper.ETL.Orchestrator.Tests\Dapper.ETL.Orchestrator.Tests.csproj` |
| `Dapper.ETL.Tests\Dapper.ETL.Tests.csproj` | `tests\Dapper.ETL.Tests\Dapper.ETL.Tests.csproj` |
| `SQLLite.Integration.Tests\SQLLite.Integration.Tests.csproj` | `tests\SQLLite.Integration.Tests\SQLLite.Integration.Tests.csproj` |

Do NOT touch any line that begins with `\t{GUID}` inside a `GlobalSection` — only the `Project(…) = "name", "path", "{guid}"` lines change.

- [ ] **Step 3: Verify sln parses and all projects resolve**

Run: `dotnet sln list`
Expected: six project paths, each with the new `src/` or `tests/` prefix.

---

### Task 4: Fix `ProjectReference` paths in test csprojs

**Files:**
- Modify: `tests/Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj`
- Modify: `tests/Dapper.ETL.Tests/Dapper.ETL.Tests.csproj`
- Modify: `tests/SQLLite.Integration.Tests/SQLLite.Integration.Tests.csproj`

Test projects now sit at `tests/<Name>/`, so references to app projects that live at `src/<Name>/` become `..\..\src\<Name>\<Name>.csproj`. Sibling references inside `src/` (AppHost → Library, Orchestrator → Library) stay at `..\<Name>\<Name>.csproj` and need no change.

- [ ] **Step 1: Update `tests/Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj`**

Find:
```xml
<ProjectReference Include="..\Dapper.ETL.Orchestrator\Dapper.ETL.Orchestrator.csproj" />
<ProjectReference Include="..\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
```

Replace with:
```xml
<ProjectReference Include="..\..\src\Dapper.ETL.Orchestrator\Dapper.ETL.Orchestrator.csproj" />
<ProjectReference Include="..\..\src\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
```

- [ ] **Step 2: Update `tests/Dapper.ETL.Tests/Dapper.ETL.Tests.csproj`**

Find:
```xml
<ProjectReference Include="..\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
```

Replace with:
```xml
<ProjectReference Include="..\..\src\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
```

- [ ] **Step 3: Update `tests/SQLLite.Integration.Tests/SQLLite.Integration.Tests.csproj`**

Find:
```xml
<ProjectReference Include="../Dapper.ETL.Library/Dapper.ETL.Library.csproj" />
```

Replace with:
```xml
<ProjectReference Include="..\..\src\Dapper.ETL.Library\Dapper.ETL.Library.csproj" />
```

(Also normalize the slashes to backslashes to match the rest of the repo's style.)

- [ ] **Step 4: Verify build still works after folder reorg**

Run: `dotnet restore Dapper.ETL.sln && dotnet build Dapper.ETL.sln --no-restore`
Expected: exit 0. Same warning count as Task 1 baseline.

- [ ] **Step 5: Verify tests still pass**

Run: `dotnet test Dapper.ETL.sln --no-build`
Expected: exit 0, same test count as Task 1 baseline.

- [ ] **Step 6: Commit**

Run:
```bash
git add Dapper.ETL.sln tests/
git commit -m "Fix sln + test ProjectReference paths after folder reorg"
```

---

### Task 5: Out-of-scope reminder checkpoint

**Files:**
- No files modified.

Per the design doc's explicit user request: after the folder-move step, surface the out-of-scope items before continuing. The executing agent MUST print the list below and pause for user acknowledgment before proceeding to Task 6. Do not silently continue.

- [ ] **Step 1: Print the reminder and wait for acknowledgment**

Print this verbatim:

> **Out-of-scope items** (NOT part of this migration — flagged for your awareness):
> 1. Rename `SQLLite.Integration.Tests` → `SQLite.Integration.Tests` (typo in `SQLLite`).
> 2. Introduce or consolidate `.editorconfig`; reconcile with `Dapper.ETL.sln.DotSettings.user`.
> 3. Add analyzers beyond the built-in .NET SDK set (e.g., Roslynator, StyleCop, Meziantou.Analyzer).
> 4. Upgrade package versions in `Directory.Packages.props` (this migration hoists only; it does not bump versions).
>
> Do you want to schedule any of these next, or should I continue with the Directory.Build.props hoisting?

Wait for user response before proceeding. If the user wants to interleave any of these, stop this plan and surface to the operator.

---

### Task 6: Create root `Directory.Build.props` (universal conventions only, no quality gates yet)

**Files:**
- Create: `Directory.Build.props`
- Modify: `Directory.Packages.props`

Split the high-risk changes across multiple tasks. This task does: define a single root TFM, central language/nullable settings, and move the two `<Manage…>` meta-properties out of `Directory.Packages.props`. It does NOT yet enable warnings-as-errors, analyzers, style enforcement, or the net9.0 bump — those are tasks 7 and 8.

- [ ] **Step 1: Create `Directory.Build.props` at the repo root**

Write `Directory.Build.props` with exactly these contents:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
</Project>
```

(`net8.0` is deliberate here — the net9.0 bump happens in Task 7 so each risk lands in its own commit.)

- [ ] **Step 2: Trim `Directory.Packages.props`**

Open `Directory.Packages.props` and remove the `<PropertyGroup>` that holds the two `<Manage…>` properties. Keep everything else identical. Final file:

```xml
<Project>
  <ItemGroup>
    <!-- Aspire (AppHost) -->
    <PackageVersion Include="Aspire.Hosting" Version="9.0.0" />
    <PackageVersion Include="Aspire.Hosting.AppHost" Version="9.0.0" />
    <PackageVersion Include="Aspire.Hosting.SqlServer" Version="9.0.0" />
    <!-- Data access -->
    <PackageVersion Include="Dapper" Version="2.1.15" />
    <PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.1" />
    <PackageVersion Include="Microsoft.Data.Sqlite" Version="9.0.0" />
    <!-- Microsoft.Extensions.* -->
    <PackageVersion Include="Microsoft.Extensions.Configuration" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.5" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Configuration.Json" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <!-- Logging / observability -->
    <PackageVersion Include="Serilog" Version="4.3.1" />
    <PackageVersion Include="Serilog.Extensions.Logging" Version="10.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.1.1" />
    <PackageVersion Include="Serilog.Sinks.MSSqlServer" Version="9.0.4-dev-00160" />
    <PackageVersion Include="Serilog.Sinks.Seq" Version="9.0.0" />
    <PackageVersion Include="OpenTelemetry" Version="1.9.0" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.9.0" />
    <!-- CLI UX -->
    <PackageVersion Include="Spectre.Console.Cli" Version="0.49.1" />
    <!-- Test framework / tooling -->
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.9.0" />
    <PackageVersion Include="xunit" Version="2.7.1" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.6" />
    <PackageVersion Include="coverlet.collector" Version="10.0.0" />
    <PackageVersion Include="Moq" Version="4.20.70" />
    <PackageVersion Include="Verify.Xunit" Version="24.0.0" />
    <PackageVersion Include="Testcontainers" Version="3.9.0" />
    <PackageVersion Include="Testcontainers.MsSql" Version="3.9.0" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Remove hoisted properties from each csproj**

For each of the six csproj files, delete the `<LangVersion>`, and `<Nullable>` lines. A csproj property overrides the root props, so leaving them works — but the point is de-duplication.

**TargetFramework handling is split across Task 6 and Task 7**:
- In this task (6), root props defaults to `net8.0`. **Keep** AppHost's `<TargetFramework>net9.0</TargetFramework>` override so it stays on net9.0 while the other five projects align to net8.0 via the root props.
- Remove `<TargetFramework>net8.0</TargetFramework>` from the five net8.0 csprojs (Library, Orchestrator, and the three test projects).
- In Task 7, root props flips to `net9.0` and AppHost's override is removed.

Apply these edits:

- `src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj`: delete `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`. **Keep** `<TargetFramework>net9.0</TargetFramework>` until Task 7.
- `src/Dapper.ETL.Library/Dapper.ETL.Library.csproj`: delete `<TargetFramework>net8.0</TargetFramework>`, `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`.
- `src/Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj`: delete `<TargetFramework>net8.0</TargetFramework>`, `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`. Keep `<OutputType>Exe</OutputType>` and `<ImplicitUsings>enable</ImplicitUsings>`.
- `tests/Dapper.ETL.Tests/Dapper.ETL.Tests.csproj`: delete `<TargetFramework>net8.0</TargetFramework>`, `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`. Keep `<IsTestProject>true</IsTestProject>` for now (Task 11 hoists it).
- `tests/Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj`: delete `<TargetFramework>net8.0</TargetFramework>`, `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`. Keep `<ImplicitUsings>enable</ImplicitUsings>`, `<IsTestProject>true</IsTestProject>`, `<IsPackable>false</IsPackable>`.
- `tests/SQLLite.Integration.Tests/SQLLite.Integration.Tests.csproj`: delete `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`. (This file never had `<LangVersion>` — after this task it gains `LangVersion=latest` via the root props, which is a deliberate improvement.) Keep `<ImplicitUsings>enable</ImplicitUsings>`, `<IsPackable>false</IsPackable>`.

- [ ] **Step 4: Verify build**

Run: `dotnet restore Dapper.ETL.sln && dotnet build Dapper.ETL.sln --no-restore`
Expected: exit 0. Same warning count as Task 1 baseline.

- [ ] **Step 5: Verify tests**

Run: `dotnet test Dapper.ETL.sln --no-build`
Expected: exit 0, same test count.

- [ ] **Step 6: Commit**

Run:
```bash
git add Directory.Build.props Directory.Packages.props src tests
git commit -m "Hoist TFM/LangVersion/Nullable/central-pkg meta into root Directory.Build.props"
```

---

### Task 7: Bump target framework to `net9.0` globally

**Files:**
- Modify: `Directory.Build.props`
- Modify: `src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj`

- [ ] **Step 1: Flip root `Directory.Build.props` to net9.0**

Open `Directory.Build.props` and change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>net9.0</TargetFramework>`. Final file:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Remove AppHost's redundant TargetFramework**

Open `src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj` and delete the line `<TargetFramework>net9.0</TargetFramework>`. The root props now supplies it.

- [ ] **Step 3: Verify net9.0 SDK is installed**

Run: `dotnet --list-sdks`
Expected: at least one entry matching `9.0.*`. If not, stop and surface to the user.

- [ ] **Step 4: Restore**

Run: `dotnet restore Dapper.ETL.sln`
Expected: exit 0. NuGet should resolve net9.0 targets for every project.

- [ ] **Step 5: Build**

Run: `dotnet build Dapper.ETL.sln --no-restore`
Expected: exit 0. Any new warnings over baseline must be investigated: capture them, surface to the user, and decide fix-or-NoWarn per warning before continuing.

- [ ] **Step 6: Test**

Run: `dotnet test Dapper.ETL.sln --no-build`
Expected: exit 0, same test count as baseline.

- [ ] **Step 7: Commit**

Run:
```bash
git add Directory.Build.props src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj
git commit -m "Bump all projects to net9.0 via root Directory.Build.props"
```

---

### Task 8: Enable warnings-as-errors, style enforcement, and latest analysis level

**Files:**
- Modify: `Directory.Build.props`

This is the highest-risk task: it converts any previously-silent warning into a build break. Expect to iterate.

- [ ] **Step 1: Add the three quality-gate properties**

Edit `Directory.Build.props` so the `<PropertyGroup>` reads:

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
</Project>
```

- [ ] **Step 2: Build and collect failures**

Run: `dotnet build Dapper.ETL.sln --no-incremental 2>&1 | tee /tmp/build-task8.log`
Expected: either (a) exit 0 — proceed to Step 4, or (b) exit nonzero with one or more `error CSxxxx` / `error IDExxxx` / `error CAxxxx` messages that used to be warnings.

- [ ] **Step 3: Triage each error — fix in code if small, otherwise `<NoWarn>` the specific code**

For each error in `/tmp/build-task8.log`:
- If the warning signals a real issue (unused variable, nullability hole, obvious code-style drift), fix the code inline.
- If the warning is analyzer noise or cosmetic (e.g., IDE0001 "simplify name") and fixing is out of scope, append the specific code to a `<NoWarn>` in `Directory.Build.props`. Do NOT silence whole categories.

Example — suppressing a single code:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);IDE0001</NoWarn>
</PropertyGroup>
```

Re-run Step 2 until the build succeeds.

- [ ] **Step 4: Run tests**

Run: `dotnet test Dapper.ETL.sln --no-build`
Expected: exit 0.

- [ ] **Step 5: Commit**

If only `Directory.Build.props` changed:
```bash
git add Directory.Build.props
git commit -m "Enable TreatWarningsAsErrors, EnforceCodeStyleInBuild, AnalysisLevel=latest"
```

If source files were edited to fix new errors, include them in the same commit and mention the fixes in the body.

---

### Task 9: Add CI-build detection

**Files:**
- Modify: `Directory.Build.props`

- [ ] **Step 1: Append the conditional `<PropertyGroup>`**

Edit `Directory.Build.props` so the final file reads:

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

(Preserve any `<NoWarn>` properties added during Task 8 — do not drop them.)

- [ ] **Step 2: Verify local build is still a non-CI build**

Run: `dotnet build Dapper.ETL.sln --no-restore -v:minimal 2>&1 | grep -i 'ContinuousIntegrationBuild' || echo 'no CI flag set'`
Expected: `no CI flag set` (or empty — property is unset locally).

- [ ] **Step 3: Verify the CI path activates when the env var is set**

Run: `CI=true dotnet build Dapper.ETL.sln --no-restore -p:ContinuousIntegrationBuild=true -v:minimal >/dev/null && echo 'CI build OK'`
Expected: `CI build OK`. This at minimum proves the property is accepted by MSBuild without syntax errors.

- [ ] **Step 4: Commit**

Run:
```bash
git add Directory.Build.props
git commit -m "Add ContinuousIntegrationBuild detection via CI/TF_BUILD/GITHUB_ACTIONS"
```

---

### Task 10: Create `src/Directory.Build.targets` for library doc generation

**Files:**
- Create: `src/Directory.Build.targets`

Uses `.targets` (not `.props`) because it must be imported *after* each csproj sets `$(OutputType)`.

- [ ] **Step 1: Create the file**

Write `src/Directory.Build.targets`:

```xml
<Project>
  <PropertyGroup Condition="'$(OutputType)' != 'Exe'">
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

- [ ] **Step 2: Verify the targets file applies only to the Library**

Run:
```bash
dotnet build src/Dapper.ETL.Library/Dapper.ETL.Library.csproj --no-restore -v:minimal 2>&1 | grep -i "DocumentationFile" | head
```
Expected: at least one line referencing `Dapper.ETL.Library.xml` — confirms doc gen is on for the library.

Then:
```bash
dotnet build src/Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj --no-restore -v:minimal 2>&1 | grep -i "DocumentationFile" | head
```
Expected: no matches (or, if matched, only an empty/unset property) — confirms doc gen is OFF for the Exe project.

- [ ] **Step 3: Verify full solution build still green**

Run: `dotnet build Dapper.ETL.sln --no-restore`
Expected: exit 0, no new warnings (CS1591 is silenced in the targets file).

- [ ] **Step 4: Commit**

Run:
```bash
git add src/Directory.Build.targets
git commit -m "Enable GenerateDocumentationFile for non-Exe src projects via Directory.Build.targets"
```

---

### Task 11: Create `tests/Directory.Build.props` AND trim test csprojs in one commit

**Files:**
- Create: `tests/Directory.Build.props`
- Modify: `tests/Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj`
- Modify: `tests/Dapper.ETL.Tests/Dapper.ETL.Tests.csproj`
- Modify: `tests/SQLLite.Integration.Tests/SQLLite.Integration.Tests.csproj`

Hoisting test packages without simultaneously trimming the csprojs produces `NU1504` / `NU1505` duplicate-package errors under central package management. All four file changes MUST land together.

- [ ] **Step 1: Create `tests/Directory.Build.props`**

Write `tests/Directory.Build.props`:

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

- [ ] **Step 2: Rewrite `tests/Dapper.ETL.Orchestrator.Tests/Dapper.ETL.Orchestrator.Tests.csproj`**

Replace the entire file with:

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

- [ ] **Step 3: Rewrite `tests/Dapper.ETL.Tests/Dapper.ETL.Tests.csproj`**

Replace the entire file with:

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

- [ ] **Step 4: Rewrite `tests/SQLLite.Integration.Tests/SQLLite.Integration.Tests.csproj`**

Replace the entire file with:

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

- [ ] **Step 5: Restore and build**

Run: `dotnet restore Dapper.ETL.sln && dotnet build Dapper.ETL.sln --no-restore`
Expected: exit 0. No `NU1504`/`NU1505` (duplicate package) errors.

- [ ] **Step 6: Run tests**

Run: `dotnet test Dapper.ETL.sln --no-build`
Expected: exit 0, same test count as baseline.

- [ ] **Step 7: Commit**

Run:
```bash
git add tests/
git commit -m "Hoist test-project conventions and shared packages into tests/Directory.Build.props"
```

---

### Task 12: Final slimming pass on app csprojs

**Files:**
- Modify: `src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj`
- Modify: `src/Dapper.ETL.Library/Dapper.ETL.Library.csproj`
- Modify: `src/Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj`

Task 6 already removed TFM/LangVersion/Nullable. This task reaches the final, minimal csproj shown in the design doc — mostly a verification that nothing else hoistable remains in the app projects.

- [ ] **Step 1: Rewrite `src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj`**

Replace the entire file with:

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

- [ ] **Step 2: Rewrite `src/Dapper.ETL.Library/Dapper.ETL.Library.csproj`**

Replace the entire file with:

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

- [ ] **Step 3: Rewrite `src/Dapper.ETL.Orchestrator/Dapper.ETL.Orchestrator.csproj`**

Replace the entire file with:

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

- [ ] **Step 4: Build + test**

Run: `dotnet build Dapper.ETL.sln && dotnet test Dapper.ETL.sln --no-build`
Expected: build exit 0, tests exit 0.

- [ ] **Step 5: Commit**

Run:
```bash
git add src/
git commit -m "Slim app csprojs to project-unique properties and references"
```

---

### Task 13: Aspire AppHost smoke test

**Files:**
- No files modified.

- [ ] **Step 1: Launch the AppHost**

Run (from `dapper-etl/`):
```bash
dotnet run --project src/Dapper.ETL.AppHost/Dapper.ETL.AppHost.csproj
```
Expected: Aspire dashboard announces `Login to the dashboard at http://localhost:<port>/login?t=<token>`, and the Orchestrator resource transitions to `Running` within ~30 seconds.

- [ ] **Step 2: Confirm both resources are registered**

With the AppHost still running, `curl -s` the dashboard or open it. Expected entries: `Dapper.ETL.Orchestrator` project resource, plus whatever SQL Server resource the AppHost wires up (per `ASPIRE_CONTAINERS_GUIDE.md`).

If the dashboard fails to resolve the Orchestrator's project path, the `ProjectReference` + `IsAspireProjectResource="true"` combination is the likely culprit — stop and debug before proceeding.

- [ ] **Step 3: Shut down**

Ctrl+C. Confirm clean shutdown (no orphaned containers: `docker ps`).

---

### Task 14: Audit external path references

**Files:** (read-only discovery; modify as needed)
- Read: `docker-compose.yml`
- Read: every file under `scripts/`
- Read: `BUILD_SETUP.md`, `ASPIRE_CONTAINERS_GUIDE.md`, `QUICK_START.md`, `FAQ_AND_EXAMPLES.md`, `ARCHITECTURE_DECISION.md`, `ETL-Architecture-Design.md`, `SCHEMA_INSPECTOR_GUIDE.md`

- [ ] **Step 1: Find stale path references**

Run (from `dapper-etl/`, Grep tool):
Pattern: `Dapper\.ETL\.(AppHost|Library|Orchestrator|Orchestrator\.Tests|Tests)` with `output_mode=files_with_matches`, excluding `docs/superpowers/`.

Then: pattern `SQLLite\.Integration\.Tests`, same exclusions.

- [ ] **Step 2: For each file found, verify any path references point to the new locations**

For each hit:
- If the match is a prose mention of the project *name*, no change needed.
- If it's a relative path or shell command that targets the project folder or csproj, update the path to include `src/` or `tests/`.
- If it's a `docker-compose.yml` `build.context` or `volume` mount that resolves to one of the old folder paths, update it.

Examples of patterns that need updating:
- `dotnet run --project Dapper.ETL.Orchestrator` → `dotnet run --project src/Dapper.ETL.Orchestrator`
- `./Dapper.ETL.Library` → `./src/Dapper.ETL.Library`
- `cd Dapper.ETL.AppHost` → `cd src/Dapper.ETL.AppHost`

- [ ] **Step 3: Check `scripts/` for any that invoke dotnet with a project path**

Run (Grep): pattern `\.csproj` in `scripts/`. For each hit, confirm the path is still valid or update it.

- [ ] **Step 4: Verify docs are consistent**

Run (Grep): pattern `src/|tests/` in top-level `*.md` files under `dapper-etl/`. Spot-check that any code blocks or file-tree diagrams showing project layout match the new structure. Update diagrams as needed.

- [ ] **Step 5: Commit doc/script updates (if any were needed)**

Run:
```bash
git add docker-compose.yml scripts/ *.md
git commit -m "Update path references to new src/ and tests/ layout"
```

(Skip the commit if no files changed.)

---

### Task 15: Final verification

**Files:**
- No files modified.

- [ ] **Step 1: Full clean + restore + build + test**

Run:
```bash
dotnet clean Dapper.ETL.sln
dotnet restore Dapper.ETL.sln
dotnet build Dapper.ETL.sln --no-restore
dotnet test Dapper.ETL.sln --no-build
```
Expected: all four exit 0. Build summary should report `0 Warning(s)` (because `TreatWarningsAsErrors=true` + any still-valid `<NoWarn>` suppressions).

- [ ] **Step 2: Verify csproj slimness**

For each csproj, inspect with the Read tool. Confirm:
- No `<TargetFramework>` element in any csproj (it lives in root `Directory.Build.props`).
- No `<LangVersion>` in any csproj.
- No `<Nullable>` in any csproj.
- Test csprojs contain no `<IsTestProject>`, `<IsPackable>`, `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `Moq`, `coverlet.collector`, or `Verify.Xunit` `PackageReference`s (those are hoisted).

- [ ] **Step 3: Verify tree structure**

Run: `ls src tests`
Expected:
```
src:
Dapper.ETL.AppHost  Dapper.ETL.Library  Dapper.ETL.Orchestrator  Directory.Build.targets

tests:
Dapper.ETL.Orchestrator.Tests  Dapper.ETL.Tests  Directory.Build.props  SQLLite.Integration.Tests
```

- [ ] **Step 4: Verify root has both Directory.Build.props and Directory.Packages.props**

Run: `ls Directory.*.props`
Expected: `Directory.Build.props` and `Directory.Packages.props`.

- [ ] **Step 5: Verify git log is clean and intentional**

Run: `git log --oneline -20`
Expected: a contiguous sequence of commits implementing Tasks 2, 4, 6, 7, 8, 9, 10, 11, 12, (optional 14). Each message is descriptive, no `wip` / `fix` / `oops` noise.

- [ ] **Step 6: Report success**

If every step passes, the migration is complete. Hand off to the user for review before merging to `main`.
