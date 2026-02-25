# Testing HelmChartTools with Sample Charts

This folder contains sample Helm charts (`sample-chart-v1` and `sample-chart-v2`) for testing the HelmChartTools module.

## Chart Overview

### sample-chart-v1
- **Version**: 1.0.0
- **App Version**: 1.0.0
- **Image Registry**: gcr.io
- **Replicas**: 3
- **Database**: PostgreSQL 12.1.2
- **Ingress**: Disabled

### sample-chart-v2
- **Version**: 2.0.0
- **App Version**: 2.0.0
- **Image Registry**: docker.io
- **Replicas**: 5
- **Database**: PostgreSQL 13.0.0 (updated)
- **Cache**: Redis 17.0.0 (new)
- **Ingress**: Enabled
- **Resources**: Increased from 512Mi to 1Gi memory
- **Autoscaling**: Enabled

## Quick Start

```powershell
# Load the module
Import-Module .\HelmChartTools\HelmChartTools.psm1 -Force

# Navigate to the chart comparison folder
cd .\2026-02-24\helm-chart-compare
```

---

## Test Scenarios

### 1. Extract Variables from a Single Chart

```powershell
# Get all variables from v1
Get-HelmChartVariables -ChartPath ./sample-chart-v1

# Get only Helm Values variables
Get-HelmChartVariables -ChartPath ./sample-chart-v1 -VariableType HelmValue

# Get Helm Builtin variables (Release, Chart, etc.)
Get-HelmChartVariables -ChartPath ./sample-chart-v1 -VariableType HelmBuiltin

# Get only from specific file types
Get-HelmChartVariables -ChartPath ./sample-chart-v1 -FileFilter '*.yaml'
```

**Expected Output**: List of variables like `{{ .Values.replicaCount }}`, `{{ .Chart.Version }}`, etc.

---

### 2. Compare Two Chart Versions

```powershell
# Basic comparison (files only)
Compare-HelmCharts -ReferenceChart ./sample-chart-v1 -DifferenceChart ./sample-chart-v2

# Include variable analysis
Compare-HelmCharts -ReferenceChart ./sample-chart-v1 -DifferenceChart ./sample-chart-v2 -IncludeVariables
```

**Expected Output**:
- `FilesOnlyInDifference`: `templates/ingress.yaml` (new file in v2)
- `ChangedFiles`: `Chart.yaml`, `values.yaml`, `templates/deployment.yaml`, `templates/configmap.yaml`

---

### 3. Deep Diff of Values Files

```powershell
# Compare default values between versions
Compare-HelmChartValues -ReferenceChart ./sample-chart-v1 -DifferenceChart ./sample-chart-v2
```

**Expected Output**:
- **Added keys**: `ingress.tls`, `redis.*`, `affinity.*`, `nodeSelector.*`
- **Removed keys**: None (v2 is backwards compatible)
- **Changed values**:
  - `replicaCount`: 3 → 5
  - `image.registry`: gcr.io → docker.io
  - `image.tag`: 1.0.0 → 2.0.0
  - `resources.limits.memory`: 512Mi → 1Gi
  - `postgresql.persistence.size`: 10Gi → 20Gi
  - `autoscaling.enabled`: false → true

---

### 4. Bulk Find and Replace

```powershell
# Dry-run: see what would be replaced
Invoke-HelmVariableReplace -ChartPath ./sample-chart-v1 `
  -Find 'gcr.io' `
  -Replace 'docker.io' `
  -WhatIf

# Actually replace (modifies files!)
Invoke-HelmVariableReplace -ChartPath ./sample-chart-v1 `
  -Find 'gcr.io' `
  -Replace 'docker.io'

# Regex replacement
Invoke-HelmVariableReplace -ChartPath ./sample-chart-v1 `
  -Find 'replicaCount: \d+' `
  -Replace 'replicaCount: 10' `
  -IsRegex `
  -WhatIf
```

> **Note**: Always use `-WhatIf` first to preview changes!

---

### 5. Generate Variable Registry

```powershell
# Markdown format (default)
Export-HelmVariableRegistry -ChartPaths ./sample-chart-v1 -Format Markdown

# Save to file
Export-HelmVariableRegistry -ChartPaths ./sample-chart-v1 `
  -Format Markdown `
  -OutputPath ./v1-variables.md

# CSV for spreadsheet
Export-HelmVariableRegistry -ChartPaths ./sample-chart-v1 `
  -Format CSV `
  -OutputPath ./v1-variables.csv

# Compare variables across both versions
Export-HelmVariableRegistry -ChartPaths @('./sample-chart-v1', './sample-chart-v2') `
  -Format Markdown
```

---

### 6. Examine Chart Structure

```powershell
# Get chart metadata and structure
Get-HelmChartStructure -ChartPath ./sample-chart-v1

# Compare structures
$v1 = Get-HelmChartStructure -ChartPath ./sample-chart-v1
$v2 = Get-HelmChartStructure -ChartPath ./sample-chart-v2

Write-Host "V1 Variables: $($v1.UniqueVariableCount)"
Write-Host "V2 Variables: $($v2.UniqueVariableCount)"
Write-Host "V1 Files: $($v1.TotalFileCount)"
Write-Host "V2 Files: $($v2.TotalFileCount)"
```

**Expected Output**:
```
ChartName           : myapp
Version             : 1.0.0
Description         : A simple web application Helm chart
Files               : {Chart.yaml, values.yaml, ...}
Dependencies        : {postgresql}
UniqueVariableCount : 23
TotalFileCount      : 7
```

---

### 7. Test Variable Consistency

```powershell
# Check consistency between versions
Test-HelmVariableConsistency -ChartPaths @('./sample-chart-v1', './sample-chart-v2')

# Use v1 as the reference
Test-HelmVariableConsistency -ChartPaths @('./sample-chart-v1', './sample-chart-v2') `
  -ReferenceChart ./sample-chart-v1
```

**Expected Output**:
- Some variables changed (e.g., database host, redis additions)
- New ingress variables in v2
- Value changes in postgresql and new redis section

---

## Variables Found in Sample Charts

### Helm Values ({{ .Values.* }})
- `{{ .Values.replicaCount }}`
- `{{ .Values.image.registry }}`
- `{{ .Values.image.repository }}`
- `{{ .Values.image.tag }}`
- `{{ .Values.image.pullPolicy }}`
- `{{ .Values.service.type }}`
- `{{ .Values.service.port }}`
- `{{ .Values.service.targetPort }}`
- And many more...

### Helm Builtins
- `{{ .Release.Name }}`
- `{{ .Release.Namespace }}`
- `{{ .Chart.Name }}`
- `{{ .Chart.Version }}`
- `{{ .Chart.AppVersion }}`

### Go Template Variables
- `{{ include "myapp.fullname" . }}`
- `{{ include "myapp.labels" . }}`
- `{{ include "myapp.selectorLabels" . }}`
- `{{ include "myapp.serviceAccountName" . }}`
- `{{ $name := ... }}`
- `{{ range ... }}`

---

## Cleanup Notes

If you modify files with `Invoke-HelmVariableReplace`, you can restore originals:

```powershell
# Reset the charts from git
git checkout HEAD -- sample-chart-v1/ sample-chart-v2/
```

---

## Tips

- **Always use `-WhatIf`** with `Invoke-HelmVariableReplace` first
- Use **Verbose** flag to see detailed output: `Get-HelmChartVariables -ChartPath ./sample-chart-v1 -Verbose`
- Compare results across **multiple** charts to find inconsistencies
- Generate registries **before** making bulk changes for audit trail
- Check `templates/_helpers.tpl` to see how Helm template functions work

---

## See Also

- `helm-chart-compare.md` — Full module documentation
- `HelmChartTools/` — The module itself
