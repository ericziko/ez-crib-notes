---
uid: f47a2c8b-d3f1-4a9e-b8c2-5f1a3e9d7b2c
title: helm-chart-compare
date: 2026-02-24
tags:
  - PowerShell
  - Helm
  - DevOps
  - chart-comparison
  - variable-management
created: 2026-02-24T20:00:00
modified: 2026-02-24T20:00:00
---

# HelmChartTools — PowerShell Module for Helm Chart Comparison

## 🤖💡 What This Does

**HelmChartTools** is a PowerShell module that helps you compare Helm charts, extract variables, and perform bulk replacements across multiple chart files. Perfect for:

- Comparing chart versions or environments to spot differences
- Finding all variables used across a chart or multiple charts
- Replacing variable references (e.g., upgrading from `{{ .Values.global.tag }}` to a new format)
- Generating a registry of all variables and their usage patterns
- Validating that variables are defined consistently across charts

---

## Installation

### Quick Start

Copy the `HelmChartTools/` folder to your modules directory:

```powershell
# Windows
Copy-Item HelmChartTools C:\Users\<Username>\Documents\PowerShell\Modules\

# macOS/Linux
Copy-Item HelmChartTools ~/.local/share/powershell/Modules/
```

Then load it:

```powershell
Import-Module HelmChartTools -Force
Get-Command -Module HelmChartTools    # Verify all 7 functions load
```

Or import from a local path:

```powershell
Import-Module .\HelmChartTools.psm1 -Force
```

> [!TIP]
> Use `-Force` during development to reload after making changes to the module.

---

## Functions at a Glance

| Function | Purpose |
|----------|---------|
| **Get-HelmChartVariables** | Extract all variables from chart files (Helm template vars, env vars, custom patterns) |
| **Compare-HelmCharts** | Full directory diff: files present/missing, file changes, variable differences |
| **Compare-HelmChartValues** | Deep-diff values.yaml files: added keys, removed keys, changed values |
| **Invoke-HelmVariableReplace** | Bulk find-and-replace across chart files (supports `-WhatIf` dry-run) |
| **Export-HelmVariableRegistry** | Generate a registry of all variables (Markdown, CSV, or JSON output) |
| **Get-HelmChartStructure** | Show chart metadata, file structure, variable count |
| **Test-HelmVariableConsistency** | Validate variables are defined and used consistently across multiple charts |

---

## Function Details & Examples

### Get-HelmChartVariables

Extract all variables from a Helm chart directory.

**Syntax:**
```powershell
Get-HelmChartVariables -ChartPath <string>
  [-VariableType <All|HelmValue|HelmBuiltin|GoVar|EnvVar>]
  [-FileFilter <string[]>]
  [-CustomPattern <string>]
```

**What it detects:**
- Helm Values: `{{ .Values.something }}`
- Helm Builtins: `{{ .Release.Name }}`, `{{ .Chart.Version }}`
- Go template variables: `{{ $myVar }}`
- Environment variables: `${VAR_NAME}`, `$ENV:VAR_NAME`

**Example:**
```powershell
# Find all variables in a chart
Get-HelmChartVariables -ChartPath ./my-chart

# Find only Helm Values variables
Get-HelmChartVariables -ChartPath ./my-chart -VariableType HelmValue

# Look in specific file types
Get-HelmChartVariables -ChartPath ./my-chart -FileFilter '*.yaml','*.tpl'
```

**Output:**
```
Name           : global.image.tag
FullMatch      : {{ .Values.global.image.tag }}
Type           : HelmValue
File           : templates/deployment.yaml
LineNumber     : 23
```

> [!NOTE]
> Returns a PSCustomObject array with properties: `Name, FullMatch, Type, File, LineNumber`

---

### Compare-HelmCharts

Compare two Helm chart directories for structural and variable differences.

**Syntax:**
```powershell
Compare-HelmCharts -ReferenceChart <string> -DifferenceChart <string> [-IncludeVariables]
```

**Example:**
```powershell
# Compare two versions of the same chart
Compare-HelmCharts -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0

# Include variable analysis
Compare-HelmCharts -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0 -IncludeVariables
```

**Output:**
```
FilesOnlyInReference  : {.helmignore, Chart.lock}
FilesOnlyInDifference : {}
ChangedFiles          : {templates/deployment.yaml, values.yaml}
VariableDifferences   : {...}
```

---

### Compare-HelmChartValues

Deep diff of `values.yaml` (or any YAML file) between two charts.

**Syntax:**
```powershell
Compare-HelmChartValues -ReferenceChart <string> -DifferenceChart <string>
  [-ValuesFile <string>]
```

**Example:**
```powershell
# Compare default values between two chart versions
Compare-HelmChartValues -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0

# Compare custom values files
Compare-HelmChartValues -ReferenceChart ./my-chart-1.0 -DifferenceChart ./my-chart-2.0 `
  -ValuesFile 'values-prod.yaml'
```

**Output:**
```
Added   : {replicaCount, tolerations, affinity}
Removed : {legacyOption}
Changed : {
  image.tag      : (1.0.0 → 2.0.0)
  image.registry : (gcr.io → docker.io)
}
```

---

### Invoke-HelmVariableReplace

Bulk find-and-replace across chart files. Supports `-WhatIf` for dry runs.

**Syntax:**
```powershell
Invoke-HelmVariableReplace -ChartPath <string> -Find <string> -Replace <string>
  [-FileFilter <string[]>]
  [-IsRegex]
  [-WhatIf]
  [-Confirm]
```

**Example:**
```powershell
# Replace a variable name (literal string match)
Invoke-HelmVariableReplace -ChartPath ./my-chart `
  -Find '{{ .Values.oldName }}' `
  -Replace '{{ .Values.newName }}'

# Dry-run first
Invoke-HelmVariableReplace -ChartPath ./my-chart `
  -Find '{{ .Values.oldName }}' `
  -Replace '{{ .Values.newName }}' `
  -WhatIf

# Use regex (e.g., update image registry)
Invoke-HelmVariableReplace -ChartPath ./my-chart `
  -Find 'gcr\.io/my-project' `
  -Replace 'docker.io/my-org' `
  -IsRegex
```

**Output:**
```
File           : templates/deployment.yaml
LineNumber     : 12
OldText        : image: {{ .Values.oldName }}
NewText        : image: {{ .Values.newName }}
```

> [!TIP]
> Always use `-WhatIf` first to preview changes before committing them.

---

### Export-HelmVariableRegistry

Generate a comprehensive registry of all variables across one or more charts.

**Syntax:**
```powershell
Export-HelmVariableRegistry -ChartPaths <string[]>
  [-Format <Markdown|CSV|JSON>]
  [-OutputPath <string>]
```

**Example:**
```powershell
# Generate markdown registry for a single chart
Export-HelmVariableRegistry -ChartPaths ./my-chart -Format Markdown

# Compare variables across multiple charts
Export-HelmVariableRegistry -ChartPaths @('./my-chart-v1', './my-chart-v2') -Format Markdown

# Save to file
Export-HelmVariableRegistry -ChartPaths ./my-chart -Format CSV -OutputPath ./variables.csv

# JSON for tooling integration
Export-HelmVariableRegistry -ChartPaths ./my-chart -Format JSON | ConvertTo-Json
```

**Output (Markdown):**
```
| Variable | Type | Charts | Files | Count |
|----------|------|--------|-------|-------|
| global.image.tag | HelmValue | my-chart | templates/deployment.yaml | 3 |
| release.name | HelmBuiltin | my-chart | templates/service.yaml | 1 |
```

---

### Get-HelmChartStructure

Display the structure and metadata of a Helm chart.

**Syntax:**
```powershell
Get-HelmChartStructure -ChartPath <string>
```

**Example:**
```powershell
Get-HelmChartStructure -ChartPath ./my-chart
```

**Output:**
```
ChartName              : my-chart
Version                : 1.2.3
Description            : My application Helm chart
Files                  : {@{Name=Chart.yaml; Purpose=Metadata}...}
Dependencies           : {postgresql, redis}
UniqueVariableCount    : 24
```

---

### Test-HelmVariableConsistency

Validate that variables are defined and used consistently across multiple charts.

**Syntax:**
```powershell
Test-HelmVariableConsistency -ChartPaths <string[]> [-ReferenceChart <string>]
```

**Example:**
```powershell
# Check consistency across versions
Test-HelmVariableConsistency -ChartPaths @('./my-chart-v1', './my-chart-v2')

# Use one chart as the reference
Test-HelmVariableConsistency -ChartPaths @('./my-chart-v1', './my-chart-v2') `
  -ReferenceChart './my-chart-v1'
```

**Output:**
```
VariableName : global.image.tag
Issue        : Different default values across charts
Charts       : {my-chart-v1: 1.0.0, my-chart-v2: 2.0.0}
Severity     : Warning
```

---

## Best Practices

### ✅ DO

- Use `-WhatIf` with `Invoke-HelmVariableReplace` before making real changes
- Generate a registry before major refactoring: `Export-HelmVariableRegistry -Format JSON` for version control
- Run `Test-HelmVariableConsistency` after updating charts to catch issues early
- Keep chart comparisons in git so you can track when variables change

### ❌ DON'T

- Use `Invoke-HelmVariableReplace` without `-WhatIf` on production chart directories without a backup
- Assume variable names match across versions — always compare first
- Replace variables without checking `Get-HelmChartStructure` to understand the chart layout
- Ignore consistency warnings from `Test-HelmVariableConsistency`

---

## Common Workflows

### Workflow 1: Upgrading to a New Chart Version

```powershell
# See what changed
Compare-HelmCharts -ReferenceChart ./my-chart-1.0 `
                  -DifferenceChart ./my-chart-2.0 `
                  -IncludeVariables

# Get the new structure
Get-HelmChartStructure -ChartPath ./my-chart-2.0

# Verify consistency in your custom values
Export-HelmVariableRegistry -ChartPaths @('./my-chart-1.0', './my-chart-2.0') -Format Markdown
```

### Workflow 2: Bulk Renaming Variables

```powershell
# Find all uses of old pattern
Get-HelmChartVariables -ChartPath ./my-chart -CustomPattern 'oldRegistryName'

# Dry-run the replacement
Invoke-HelmVariableReplace -ChartPath ./my-chart `
  -Find 'oldRegistryName' `
  -Replace 'newRegistryName' `
  -WhatIf

# Execute if satisfied
Invoke-HelmVariableReplace -ChartPath ./my-chart `
  -Find 'oldRegistryName' `
  -Replace 'newRegistryName'
```

### Workflow 3: Auditing Variable Usage

```powershell
# Extract all variables
Get-HelmChartVariables -ChartPath ./my-chart |
  Group-Object -Property Type |
  Select-Object Name, Count

# Generate a full registry for audit trail
Export-HelmVariableRegistry -ChartPaths ./my-chart -Format CSV -OutputPath ./audit-$(Get-Date -Format yyyy-MM-dd).csv
```

---

## Notes

- The module uses **no external dependencies** — works with PowerShell 5.1 and later
- YAML parsing is done with a simple line-by-line parser (no `powershell-yaml` module required)
- All functions validate inputs and return clear error messages
- File operations are cross-platform safe (uses `Join-Path` internally)

---

## Troubleshooting

### "No variables found"
- Check that your chart files are `.yaml`, `.yml`, or `.tpl` (adjust with `-FileFilter`)
- Verify the chart path exists: `Test-Path ./my-chart`
- Use `-CustomPattern` if you have non-standard variable syntax

### "-WhatIf didn't show changes"
- Ensure the `-Find` pattern matches exactly what's in the files
- Use `-IsRegex` if you need pattern matching instead of literal strings
- Check file permissions (need read access to chart files)

### "Values not being detected"
- YAML parsing is basic — complex YAML structures may not parse completely
- Use `Get-HelmChartVariables` to verify the chart is being read correctly
- Report issues with example chart structures for improvement

---

## See Also

- [how-to-breakup-powershell-modules](../2026-02-20/how-to-breakup-powershell-modules/) — Module structure conventions
- [PowerShell Background Processes Tutorial](../../PowerShell%20Background%20Processes%20Tutorial.md) — Job management
