---
uid: b8c7d6e5-4f3a-2b9c-8d7a-6e5f4c3b2a1d
title: Build Environment Setup and Troubleshooting
created: 2026-04-05
modified: 2026-04-05
tags:
  - build
  - environment
  - troubleshooting
  - dotnet
---

# Build Environment Setup and Troubleshooting

This guide covers setting up your .NET build environment for the Dapper.ETL project.

## Prerequisites

- .NET 9.0 SDK (or 8.0 minimum)
- Git
- Docker (optional, for container-based testing)

## Verify Installation

```bash
dotnet --version
# Should output: 9.0.x or higher

git --version
# Should output: git version 2.x or higher
```

## Quick Build

```bash
# Restore NuGet packages
dotnet restore

# Build solution
dotnet build -c Debug

# Run tests
dotnet test
```

---

## Common Build Issues & Solutions

### Issue 1: MSBuild Temp Directory Creation Failure

**Symptom:**
```
MSBUILD : error MSB1025: An internal failure occurred while running MSBuild.
System.IO.FileNotFoundException: Unable to find the specified file.
```

**Root Cause:** 
.NET/MSBuild cannot create temp subdirectories due to file system restrictions.

**Solution:**

Ensure `/tmp/claude` exists and is writable:

```bash
mkdir -p /tmp/claude
chmod 755 /tmp/claude

# Verify it exists
ls -la /tmp/claude
```

Then rebuild:

```bash
dotnet clean
dotnet restore
dotnet build -c Debug
```

**If still failing**, try with explicit TMPDIR:

```bash
TMPDIR=/tmp/claude dotnet build -c Debug
```

### Issue 2: NuGet Cache Corruption

**Symptom:**
```
error: Failed to restore packages
error: Unable to resolve dependencies
```

**Solution:**

Clear NuGet cache and restore:

```bash
# Clear local cache
dotnet nuget locals all --clear

# Restore fresh
dotnet restore

# Rebuild
dotnet build -c Debug
```

### Issue 3: Docker Socket Connection Failed

**Symptom:**
```
Testcontainers: Cannot connect to Docker daemon
```

**Solution:**

Ensure Docker is running:

```bash
# Check Docker status
docker ps

# If not running, start Docker
# On macOS: Start Docker Desktop app
# On Linux: sudo systemctl start docker
```

### Issue 4: SQL Server Container Won't Start

**Symptom:**
```
Testcontainers: Container failed to start within timeout
```

**Solution:**

1. Check Docker resources:
```bash
docker system df
# Ensure you have sufficient disk space (>5GB)
```

2. Restart Docker daemon:
```bash
# macOS
killall Docker
# Wait 10 seconds, then restart Docker Desktop

# Linux
sudo systemctl restart docker
```

3. Prune old containers:
```bash
docker system prune -a
```

### Issue 5: Port Already in Use

**Symptom:**
```
Port 1433 already in use
error: bind: address already in use
```

**Solution:**

Find and stop the conflicting process:

```bash
# Find process using port
lsof -i :1433

# Stop the container/process
docker stop <container-id>

# Or use different port in Aspire host:
# Edit src/Dapper.ETL.AppHost/Program.cs
var sqlServer = builder.AddSqlServer("sql-server", password: sqlPassword, port: 1434);
```

### Issue 6: Permission Denied on Directories

**Symptom:**
```
System.UnauthorizedAccessException: Access to the path is denied
```

**Solution:**

Fix directory permissions:

```bash
# Ensure bin/obj directories are writable
chmod -R u+w bin obj

# Remove and rebuild
rm -rf bin obj
dotnet build -c Debug
```

### Issue 7: Out of Memory During Build

**Symptom:**
```
OutOfMemoryException
Process killed due to insufficient memory
```

**Solution:**

Reduce parallel build threads:

```bash
# Use single-threaded build
dotnet build -c Debug --no-restore -m:1

# Or increase system swap/memory
```

---

## Environment Variables

### Required for Local Development

None are required if using Aspire host (connection strings auto-injected).

### Optional for Manual Setup

```bash
# For local CLI testing without Aspire
export ConnectionStrings__Source="Server=localhost;Database=TestDbSource;User Id=sa;Password=TestPassword123!;Encrypt=false"
export ConnectionStrings__Target="Server=localhost;Database=TestDbTarget;User Id=sa;Password=TestPassword123!;Encrypt=false"
export ConnectionStrings__Logs="Server=localhost;Database=EtlLogs;User Id=sa;Password=TestPassword123!;Encrypt=false"
export Seq__Url="http://localhost:5341"
```

---

## Build Profiles

### Debug Build
```bash
dotnet build -c Debug
# Includes debug symbols, slower, larger binaries
# Use for development and debugging
```

### Release Build
```bash
dotnet build -c Release
# Optimized, no debug symbols, smaller binaries
# Use for production
```

### No-Restore Build
```bash
dotnet build -c Debug --no-restore
# Assumes packages already restored
# Faster for repeated builds
```

---

## Clean Rebuild

If you encounter persistent issues, perform a full clean rebuild:

```bash
# Remove all build artifacts
rm -rf bin obj
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null

# Clear NuGet cache
dotnet nuget locals all --clear

# Full restore and build
dotnet restore
dotnet build -c Debug
```

---

## Build Verification

After building successfully, verify:

```bash
# Check all projects built
ls -la src/Dapper.ETL.Library/bin/Debug/net9.0/Dapper.ETL.Library.dll
ls -la src/Dapper.ETL.Orchestrator/bin/Debug/net9.0/Dapper.ETL.Orchestrator.dll
ls -la tests/Dapper.ETL.Orchestrator.Tests/bin/Debug/net9.0/Dapper.ETL.Orchestrator.Tests.dll

# Run a quick test
dotnet test tests/Dapper.ETL.Orchestrator.Tests --filter "ClassName=GetStatsCommandTests" -v minimal
```

---

## CI/CD Considerations

For automated builds (GitHub Actions, etc.):

1. Use Ubuntu runners (simpler Docker setup)
2. Pre-warm NuGet cache
3. Use `--no-restore` after initial restore
4. Timeout tests after 5 minutes per test class

Example GitHub Actions step:
```yaml
- name: Build
  run: dotnet build -c Release --no-restore

- name: Test
  run: dotnet test --no-build -v minimal --logger trx --collect:"XPlat Code Coverage"
```

---

## Next Steps

1. **Verify build works**: `dotnet build -c Debug`
2. **Run tests**: `dotnet test`
3. **Start Aspire host**: `cd src/Dapper.ETL.AppHost && dotnet run`
4. **See [ASPIRE_CONTAINERS_GUIDE.md](ASPIRE_CONTAINERS_GUIDE.md) for development workflow**

For additional help, see the [QUICK_START.md](QUICK_START.md) or [ASPIRE_CONTAINERS_GUIDE.md](ASPIRE_CONTAINERS_GUIDE.md).
