---
uid: 01KX5NDSS4M7E9F5FWQCEX7EPM
created: 2026-07-10T02:23:33-07:00
modified: 2026-07-10T03:06:43-07:00
title: WF - Create implmentation plan to convert SSIS packages to Apache Airflow python stack
---

# WF - Create implmentation plan to convert SSIS packages to Apache Airflow python stack
## 🔗 Links
- [Check out this chat](https://chatgpt.com/share/6a50c3c9-d05c-83e8-937a-aede29b6839d)

## Prompt
I am working in a large corporate enterprise and we have to convert a bunch of SSIS packages to Python running inside of OpenShift
- Please create a detailed implementation plan for creating a tool or suite of tools for  
	- Examine our SSIS packages and 
		- inventory the necessary components / functionality that we need to convert
			- We are not using every feature of SSIS - so we only want to figure out how to automate the conversion of the features that we are using
		- Help us figure out automated ways to convert our SSIS packages to Python / Apache Airflow
		- We are new to Python so we need detailed guidance on all the recommended tooling that we should
	- We are amenable to using PowerShell where possible to script any of this out
		- Any other tools / languages / tool chain suggestions are welcome
	- We need strong recommendations on libraries such as Pandas and Polar that we should lean on for this as well as test frameworks that we should 
	  consider 
	- We would also like you to explore the OpenSource and commercial eco-system for existing tools that might aid us in this journey - I am sure that much of this kind of work has been done before
	- Please think hard and write me a detailed guide / specification / implementation plan full of do's, don't, gotcha's guidance, suggestion and tips to help us along this journey
	- We will be interfacing with both SQL Sever and Oracle
	- We work in a bank so consider the constraints of that environment when designing this solution and making suggestions
	- We are experience `c#` software engineers but have zero experience with Python - so please keep that in mind while constructing the guidace
	- Think hard on this

## Response
### SSIS-to-Apache Airflow/Python Migration Factory

#### Enterprise implementation plan for OpenShift, SQL Server, and Oracle

#### 1. Executive recommendation

Do **not** approach this as a one-time "DTSX-to-Python translator." Build a repeatable **migration factory** with four durable capabilities:

1. **A Windows-based SSIS estate collector and C# metadata extractor**
2. **A neutral, versioned SSIS intermediate representation**
3. **A Python analyzer, conversion planner, and deterministic code generator**
4. **A dual-run reconciliation and certification framework**

This architecture lets you automate only the SSIS features actually used by your bank, while preserving an explicit path for unsupported scripts, custom components, Windows dependencies, and complex operational behavior.

The core architectural decisions should be:

- Use **C# and the Microsoft SSIS object model** to inspect packages. Do not make raw DTSX XML parsing the primary approach.
- Use **PowerShell** to discover, export, hash, batch, and invoke the extractor.
- Use **Python** for analysis, reporting, rules, generation, testing, and the target ETL runtime.
- Use **Apache Airflow for orchestration**, not as a row-by-row transformation engine.
- Run substantial transformations in dedicated OpenShift pods through the Kubernetes executor or `KubernetesPodOperator`.
- Prefer, in order:
    
    1. Set-based SQL executed inside SQL Server or Oracle
    2. Native bulk-load and bulk-export mechanisms
    3. Polars and PyArrow for containerized transformations
    4. Pandas for compatibility and smaller workloads
        
- Retain SSIS during migration and use controlled parallel runs until functional, operational, security, and performance equivalence are proven.

SSIS packages contain considerably more than visible tasks: connections, control flow, data flow, event handlers, variables, parameters, configurations, expressions, and precedence behavior all contribute to runtime semantics. Microsoft's ManagedDTS object model can load packages and expose their containers, tasks, transformations, connections, variables, configurations, and precedence constraints, making it a substantially safer extraction layer than ad hoc XML processing. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/integration-services/integration-services-ssis-packages?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/integration-services/integration-services-ssis-packages?view=sql-server-ver17"))

---

### 2. Recommended target architecture

```text
┌──────────────────────────────── Existing estate ──────────────────────────────┐
│                                                                               │
│  Git/File Shares      SSISDB       MSDB/Package Store      SQL Agent/Control-M│
│      .dtsx/.ispac   projects/envs       packages           schedules/proxies  │
└──────────┬──────────────┬──────────────────┬─────────────────────┬────────────┘
           │              │                  │                     │
           └──────────────┴──────────────────┴─────────────────────┘
                                      │
                         Windows Discovery/Extraction Tier
                                      │
                ┌─────────────────────┴──────────────────────┐
                │ PowerShell collector                       │
                │ C# ManagedDTS extractor                    │
                │ Version-specific SSIS assemblies           │
                │ Source hashing and secret redaction         │
                └─────────────────────┬──────────────────────┘
                                      │
                    Canonical SSIS Intermediate Representation
                                      │
                ┌─────────────────────┴──────────────────────┐
                │ Versioned JSON/JSON Schema                 │
                │ Control-flow and data-flow graphs           │
                │ Expressions, SQL, lineage, findings         │
                │ Runtime evidence and ownership metadata     │
                └─────────────────────┬──────────────────────┘
                                      │
                     Python Migration Factory / Compiler
                                      │
      ┌────────────────────┬──────────┴──────────┬──────────────────────┐
      │ Inventory analyzer │ Conversion planner │ Deterministic generator│
      │ Feature heat maps  │ Stage coalescing   │ DAGs, SQL, jobs, tests │
      │ Risk scoring       │ Support decisions  │ Runbooks and manifests │
      └────────────────────┴──────────┬──────────┴──────────────────────┘
                                      │
                              CI/CD certification
                                      │
              ┌───────────────────────┴────────────────────────┐
              │ Lint/type/unit/integration/security tests       │
              │ Build, SBOM, scan, sign, promote by image digest│
              │ SSIS-versus-Python reconciliation               │
              └───────────────────────┬────────────────────────┘
                                      │
                           OpenShift target platform
                                      │
                ┌─────────────────────┴───────────────────────┐
                │ Airflow 3.x control plane                  │
                │ Kubernetes task pods                       │
                │ Shared bank ETL runtime library            │
                │ SQL Server / Oracle / approved file storage│
                │ Vault, logging, metrics, lineage            │
                └─────────────────────────────────────────────┘
```

#### Non-negotiable architectural principles

##### Airflow is the orchestrator

Do not turn every SSIS data-flow transformation into an Airflow task. That would create very large DAGs, excessive scheduler overhead, unnecessary serialization, and brittle intermediate-state handling.

Instead:

- One Airflow task should normally represent a meaningful **execution, retry, security, or resource boundary**.
- An entire SSIS data flow will often become:
    - one set-based SQL statement;
    - one SQL extract, one Polars transformation, and one bulk load;
    - or one containerized ETL job.
- Use TaskGroups for organization, not as a substitute for proper execution boundaries.
- Use dynamic task mapping only for bounded, genuinely parallel units such as a controlled list of files or partitions.

Airflow's Kubernetes executor runs tasks in separate pods, while `KubernetesPodOperator` allows individual tasks to use purpose-built images and resource profiles. Airflow also explicitly recommends remote storage rather than relying on local task files and recommends XCom only for relatively small messages. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/administration-and-deployment/kubernetes.html "https://airflow.apache.org/docs/apache-airflow/stable/administration-and-deployment/kubernetes.html"))

##### Preserve semantics, not visual topology

A successful migration reproduces:

- data outputs;
- side effects;
- reject behavior;
- transaction boundaries;
- retry and restart behavior;
- scheduling semantics;
- audit data;
- alerts;
- runtime parameters;
- security context;
- and performance characteristics.

A pipeline that produces the same row count but changes null handling, truncates Unicode, applies lookup rules in a different order, or cannot safely restart is not equivalent.

##### Generated artifacts must remain deterministic

Given the same:

- package source hash;
- intermediate-representation version;
- rule-set version;
- runtime-library version;
- generator version;

the migration factory must produce byte-for-byte equivalent generated artifacts, excluding explicitly documented timestamps.

Generated files should not be edited directly. All hand-written extensions belong in separate extension modules, SQL overrides, or conversion plugins.

---

### 3. Implementation phases and approval gates

#### Phase 0 — Governance and target architecture

##### Work

Establish the program's operating rules before writing the converter:

- Identify the authoritative SSIS repositories.
- Define data classifications and package-source handling requirements.
- Decide whether any source code or SQL may leave the bank.
- Identify supported authentication mechanisms for SQL Server and Oracle from OpenShift.
- Select the initial Airflow 3.x, Python, provider, OpenShift, and database-driver baselines.
- Define the generated-code ownership model.
- Define production support responsibilities.
- Select the pilot package population.
- Define what "automated conversion" means.

##### Required architecture decisions

Document at least these ADRs:

1. ManagedDTS-based extraction
2. Versioned neutral intermediate representation
3. Airflow as orchestration, not the primary data engine
4. SQL-first transformation strategy
5. Polars/PyArrow as the preferred containerized data stack
6. Kubernetes-isolated execution
7. External secrets only
8. Immutable images and dependency locks
9. Dual-run certification before cutover
10. Explicit handling of unsupported components

##### Exit criteria

- Architecture, security, DBA, operations, and audit representatives approve the operating model.
- The program has named owners for SQL Server, Oracle, Airflow, OpenShift, SSIS, reconciliation, and business acceptance.
- Source-code and vendor-POC data-handling rules are documented.

---

#### Phase 1 — Estate discovery and inventory

##### Work

Discover every place from which an SSIS workload can originate or be scheduled:

- Source-control repositories
- `.dtsx` files
- `.ispac` project archives
- SSISDB projects, folders, environments, and references
- MSDB or package-store deployments
- File shares
- SQL Server Agent jobs and job steps
- Windows Task Scheduler
- Control-M, AutoSys, or other enterprise schedulers
- PowerShell, batch, or command files invoking `dtexec`
- Parent packages that dynamically invoke child packages
- Database procedures that initiate downstream work
- Operational runbooks that include manual execution

Microsoft provides SSISDB catalog views for projects, packages, parameters, environments, references, executions, and operation history. `dtutil` can copy, move, verify, or delete packages from supported stores, and `dtexec` exposes runtime configuration and execution behavior. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/integration-services/catalog/ssis-catalog?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/integration-services/catalog/ssis-catalog?view=sql-server-ver17"))

##### Runtime evidence to gather

Static inspection alone is insufficient. Capture:

- execution count by package;
- last successful and failed execution;
- duration percentiles;
- failure rate;
- retry behavior;
- row counts where available;
- schedule and calendar;
- month-end, quarter-end, and year-end usage;
- owners and support teams;
- source and target systems;
- data classification;
- service account or proxy references;
- business SLA and criticality;
- peak volumes;
- known manual interventions.

When verbose SSISDB logging has been used, `catalog.execution_data_statistics` can expose row-flow statistics between data-flow components. Do not enable verbose logging globally without evaluating catalog growth and runtime overhead; use it selectively to fill inventory gaps. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/integration-services/troubleshooting/debugging-data-flow?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/integration-services/troubleshooting/debugging-data-flow?view=sql-server-ver17"))

##### Exit criteria

- Every known package has a source hash and authoritative location.
- Every production schedule has an owner and package mapping.
- Packages with no observed execution have been reviewed rather than automatically declared obsolete.
- The program can distinguish duplicate, dormant, active, and unowned packages.

---

#### Phase 2 — Build the SSIS extractor and intermediate representation

##### Work

Implement the Windows-side C# extractor and PowerShell discovery wrappers.

##### Exit criteria

- Packages can be loaded without modifying or upgrading them.
- Extraction failures identify the reason: encryption, missing assembly, version mismatch, corruption, or unsupported storage.
- No secret values appear in extracted JSON.
- A representative golden corpus produces stable, version-controlled intermediate representations.

---

#### Phase 3 — Analyzer, feature heat map, and conversion support matrix

##### Work

- Count the exact component types and configurations in use.
- Classify every observed feature.
- Build the dependency graph.
- Assign conversion support and risk.
- Identify the minimum set of converters required to cover the majority of active execution volume.

##### Exit criteria

- The estate has an execution-weighted feature heat map.
- Every package has an automated, assisted, redesign, retire, or blocked disposition.
- Unsupported features are explicit rather than silently ignored.
- The pilot covers both common patterns and representative hard cases.

---

#### Phase 4 — Target runtime, generator, and OpenShift platform

##### Work

- Build the shared Python ETL runtime.
- Implement the first conversion plugins.
- Deploy the Airflow/OpenShift nonproduction platform.
- Establish secrets, logs, metrics, pools, image policies, and database connectivity.
- Generate DAGs, SQL, worker jobs, tests, manifests, and runbooks.

##### Exit criteria

- Generated workflows import into Airflow with no parse errors.
- All task images run under the bank's restricted OpenShift security posture.
- SQL Server and Oracle connectivity, encryption, authentication, and bulk operations pass integration tests.
- Generated code is reproducible.

---

#### Phase 5 — Representative pilot

Select approximately 20 packages spanning:

- simple stored-procedure orchestration;
- SQL Server-to-SQL Server flows;
- Oracle-to-SQL Server or SQL Server-to-Oracle flows;
- file ingestion and export;
- lookups and conditional splits;
- loops;
- parameters and expressions;
- error outputs;
- package chaining;
- at least one Script Task or custom component;
- a high-volume workload;
- a month-end or otherwise operationally critical workload.

Do not choose only easy packages. The pilot should validate the architecture, not merely demonstrate that simple SQL tasks can be generated.

##### Exit criteria

- Data and side-effect reconciliation passes.
- Empty-input, bad-row, retry, restart, and partial-failure cases pass.
- Performance meets the approved target or has an accepted remediation.
- Operations can diagnose and restart the workflows.
- Security testing and production-readiness review pass.

---

#### Phase 6 — Industrialize the migration factory

##### Work

- Refine component mappings from pilot findings.
- Add domain-specific conversion plugins.
- Automate wave planning and package scorecards.
- Add reusable runbooks and dashboards.
- Establish a support process for new component signatures.

##### Exit criteria

- Conversion results are predictable enough for multiple teams.
- Factory changes have backward-compatibility and regression testing.
- Domain teams can extend the solution without editing core compiler internals.

---

#### Phase 7 — Migration waves and cutover

Group packages by:

- shared source and target systems;
- business domain;
- component patterns;
- operational schedule;
- criticality;
- data classification;
- dependencies;
- and owner.

For each wave:

1. Freeze or version the source SSIS package.
2. Generate the target.
3. Complete technical tests.
4. Execute parallel runs.
5. Obtain data-owner and operations approval.
6. Switch scheduling at an exact watermark.
7. Retain a controlled rollback route.
8. Disable SSIS only after the stabilization criteria are met.

---

#### Phase 8 — SSIS decommissioning

Decommissioning includes more than deleting packages:

- SQL Server Agent jobs
- SSISDB projects and environments
- SQL Agent proxies
- Windows service accounts
- file-share permissions
- scheduled tasks
- SSIS licenses or runtimes
- monitoring rules
- runbooks
- firewall entries
- archived secrets
- source repositories
- support documentation

Retain source packages, manifests, reconciliation evidence, and approvals according to the bank's records policy.

---

### 4. Tool-suite specification

#### 4.1 Windows estate collector

Use PowerShell for orchestration because it is effective for:

- Windows filesystem and registry discovery;
- SQL Server administration;
- invoking `dtutil`, `dtexec`, and custom .NET executables;
- hashing files;
- querying SQL Server Agent and SSISDB;
- parallel processing;
- creating signed manifests;
- integration with existing Windows operational controls.

##### Suggested commands

```text
wfssis collect-files
wfssis collect-ssisdb
wfssis collect-schedules
wfssis extract
wfssis analyze
wfssis plan
wfssis generate
wfssis verify
wfssis report
```

The public CLI may be Python/Typer, but the Windows collector should be a signed PowerShell module invoking a signed .NET executable.

##### Illustrative PowerShell wrapper

```powershell
param(
    [Parameter(Mandatory)]
    [string] $PackageRoot,

    [Parameter(Mandatory)]
    [string] $OutputRoot,

    [Parameter(Mandatory)]
    [string] $ExtractorPath
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null

$results = foreach ($file in Get-ChildItem `
        -Path $PackageRoot `
        -Filter "*.dtsx" `
        -File `
        -Recurse) {

    $hash = Get-FileHash -Path $file.FullName -Algorithm SHA256
    $outputFile = Join-Path $OutputRoot "$($hash.Hash).json"

    & $ExtractorPath `
        extract `
        --input $file.FullName `
        --output $outputFile `
        --redact-sensitive

    $status = if ($LASTEXITCODE -eq 0) { "Extracted" } else { "Failed" }

    [pscustomobject]@{
        Path        = $file.FullName
        Sha256      = $hash.Hash
        Status      = $status
        Output      = $outputFile
        CollectedAt = [DateTimeOffset]::UtcNow
    }
}

$results |
    ConvertTo-Json -Depth 8 |
    Set-Content -Path (Join-Path $OutputRoot "manifest.json") -Encoding UTF8
```

Productionize this with:

- Authenticode signing
- PowerShell constrained-language considerations
- central logging
- input allowlists
- explicit exit codes
- package-size limits
- retry rules
- secure temporary directories
- no credential values in command lines

##### Do not use PowerShell to

- implement high-volume data transformations;
- parse DTSX using regular expressions;
- embed production credentials;
- become the long-term Linux/OpenShift ETL runtime.

---

#### 4.2 C# ManagedDTS extractor

This is where the team's C# experience provides a major advantage.

##### Responsibilities

The extractor should:

- Load packages through `Microsoft.SqlServer.ManagedDTS`.
- Walk package containers recursively.
- Inspect executables, tasks, event handlers, precedence constraints, variables, parameters, log providers, and connections.
- Inspect each data flow through its component metadata, inputs, outputs, columns, paths, custom properties, and component class identifiers.
- Retain original SSIS IDs and generate canonical stable IDs.
- Capture the original package source hash.
- Extract SQL and expressions without executing them.
- Detect third-party and custom assemblies.
- Detect package and project deployment models.
- Capture protection levels and sensitivity flags.
- Write only schema-valid, sanitized JSON.

The ManagedDTS `Package` object exposes the package collections, while pipeline component metadata exposes inputs, outputs, columns, custom properties, and component identity. ([Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dts.runtime.package?view=sqlserver-2019 "https://learn.microsoft.com/en-us/dotnet/api/microsoft.sqlserver.dts.runtime.package?view=sqlserver-2019"))

##### Version strategy

Do not assume one extractor binary can safely process every historical package.

Maintain extractor workers for the SSIS versions actually present:

```text
Extractor.Sql2012
Extractor.Sql2016
Extractor.Sql2019
Extractor.Sql2022
Extractor.Sql2025
```

The discovery step should determine the package format or target-server version and route it to the matching worker.

Never call `SaveToXml` or save the loaded package during inventory. Loading a package with newer assemblies can produce upgrade behavior; inventory must be read-only.

##### Fallback parser

Maintain a constrained XML fallback for cases where ManagedDTS cannot fully load a package because of:

- missing custom assemblies;
- unavailable connection-manager providers;
- corrupted metadata;
- unsupported package versions.

The fallback may capture:

- package name and IDs;
- XML element names;
- creation names and class IDs;
- referenced assemblies;
- raw expressions;
- connection-manager names;
- task hierarchy.

It must mark such information as **unresolved** and must never claim full semantic extraction.

##### Package protection

SSIS protection levels may encrypt sensitive data or the entire package using a password or a user key. A package protected by a user key may require the original user context; password-protected packages require the relevant password. The extractor must not circumvent protection and must not emit decrypted secrets. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/integration-services/security/access-control-for-sensitive-data-in-packages?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/integration-services/security/access-control-for-sensitive-data-in-packages?view=sql-server-ver17"))

Represent protected values as:

```json
{
  "present": true,
  "sensitive": true,
  "source": "package_connection_manager",
  "secret_reference": null,
  "value": null,
  "extraction_status": "protected"
}
```

---

#### 4.3 Canonical intermediate representation

The intermediate representation, or IR, is the contract between the C# world and the Python world.

##### Why an IR is essential

Without an IR:

- DTSX version differences leak into every downstream component.
- The generator becomes tightly coupled to Microsoft XML.
- Testing requires full SSIS installations.
- Vendor tools cannot easily be compared.
- Changes to analysis may require re-accessing protected source systems.
- Deterministic conversion becomes difficult.

##### Suggested top-level model

```yaml
schema_version: "1.0"
extractor:
  name: "wfssis-manageddts"
  version: "0.4.0"
source:
  type: "dtsx"
  path_token: "ssis://finance/recon/load_positions"
  sha256: "..."
  target_server_version: "SQLServer2019"
package:
  canonical_id: "pkg_finance_load_positions"
  ssis_id: "{...}"
  name: "LoadPositions"
  protection_level: "DontSaveSensitive"
  deployment_model: "project"
  transaction_option: "Supported"
  checkpoint_usage: "IfExists"
parameters: []
variables: []
connections: []
control_flow:
  nodes: []
  edges: []
event_handlers: []
data_flows: []
expressions: []
sql_assets: []
external_dependencies: []
runtime_evidence: {}
findings: []
conversion:
  support_status: "not_assessed"
  confidence: 0.0
```

##### Required data model characteristics

- Generate JSON Schema from the Python Pydantic model.
- Validate the C# output against that schema.
- Version the schema independently from the extractor.
- Provide migration functions between schema versions.
- Preserve raw source values alongside normalized values.
- Distinguish `unknown`, `not applicable`, and `empty`.
- Record provenance for every finding.
- Do not store secret values.
- Record extraction confidence.
- Record missing assemblies and unresolved components.

##### Graph model

Represent two graph levels:

1. **Control-flow graph**
    
    - tasks;
    - containers;
    - package calls;
    - precedence constraints;
    - success, failure, completion, and expression conditions.
        
2. **Data-flow graph**
    
    - sources;
    - transformations;
    - destinations;
    - normal outputs;
    - error outputs;
    - input/output column lineage.

##### Expression model

Preserve:

- exact original expression;
- location;
- referenced variables and parameters;
- expected SSIS type;
- parsed abstract syntax tree, when supported;
- target expression;
- test vectors;
- parser confidence.

Do not translate expressions by performing global string replacements.

---

#### 4.4 Analyzer and rules engine

The analyzer should answer five questions for every package:

1. What does this package contain?
2. What does it depend on?
3. How often and how critically does it run?
4. How much can be generated safely?
5. What must a human redesign or certify?

##### Component signature

Use more than the display name:

```text
component family
component class ID
creation name
assembly name and version
custom property signature
input/output shape
SSIS target-server version
```

This prevents a third-party "Lookup" or custom "OLE DB Destination" from being treated as a Microsoft built-in component.

##### Support categories

|Category|Meaning|
|---|---|
|**A — Automatic**|Generator supports the exact configuration and produces complete tests.|
|**B — Automatic with review**|Generation is supported, but semantics or operational choices require approval.|
|**C — Framework-assisted manual conversion**|Runtime utilities and templates exist, but custom implementation is needed.|
|**D — Redesign**|Direct translation is unsafe or inappropriate.|
|**E — Retire**|The workload is obsolete, duplicate, or no longer required.|
|**Blocked**|Source cannot be decrypted, loaded, located, or attributed to an owner.|

##### Important rule

A package is not "automatically converted" merely because Python files were generated.

Call it automatic only when:

- all required components have supported configurations;
- generated code imports;
- generated tests pass;
- reconciliation passes;
- no unresolved finding remains;
- operational acceptance passes.

##### Example scoring dimensions

Maintain separate scores rather than one misleading number:

- **Static complexity**
- **Runtime criticality**
- **Data-volume risk**
- **External dependency risk**
- **Windows-dependency risk**
- **Security sensitivity**
- **Testability**
- **Automation coverage**
- **Conversion confidence**

A suggested migration-wave score could weight:

```text
30% business criticality
20% dependency complexity
15% unsupported/custom components
15% data volume and performance sensitivity
10% testability
10% operational readiness
```

Use these as starting weights, then calibrate them against pilot results.

##### Analyzer findings

Examples:

```text
SSIS001  Script Task present
SSIS002  Script Component present
SSIS003  Unknown third-party component
SSIS004  Embedded or protected sensitive property
SSIS005  OLE DB Command performs per-row database operations
SSIS006  Package or container transaction enabled
SSIS007  RetainSameConnection used
SSIS008  Local temporary table crosses task boundaries
SSIS009  Event handler has business side effects
SSIS010  Dynamic SQL command expression
SSIS011  Dynamic connection-string expression
SSIS012  Local Windows path
SSIS013  UNC share dependency
SSIS014  Excel/Access/COM dependency
SSIS015  Fuzzy transformation
SSIS016  Error rows ignored
SSIS017  Truncation disposition is IgnoreFailure
SSIS018  Non-idempotent file deletion
SSIS019  Package checkpoint behavior enabled
SSIS020  Cross-database transaction assumption
```

---

#### 4.5 Conversion planner

The planner sits between the analyzer and code generator.

Its job is to determine the **best target implementation pattern**, not merely the closest visual equivalent.

##### Stage coalescing

For example, this SSIS data flow:

```text
OLE DB Source
  → Data Conversion
  → Derived Column
  → Conditional Split
  → Aggregate
  → OLE DB Destination
```

might become a single SQL operation:

```sql
INSERT INTO target_table (...)
SELECT
    CAST(...),
    CASE ...,
    SUM(...)
FROM source_table
WHERE ...
GROUP BY ...;
```

When source and destination differ, it may instead become:

```text
SQL extract
  → Arrow/Parquet staging
  → Polars lazy transformation
  → target bulk load
  → atomic publish
```

##### Planner inputs

- component graph;
- source and target database;
- data volume;
- supported SQL dialect;
- required transaction semantics;
- security classification;
- file-storage policy;
- available database privileges;
- runtime resource limits;
- supported bulk-load method.

##### Planner output

```yaml
stages:
  - id: extract_positions
    pattern: sql_to_parquet
    image: bank-etl-sqlserver:2.3.1
    resources:
      memory: 2Gi
      cpu: "1"
    inputs: [...]
    outputs: [...]
  - id: transform_positions
    pattern: polars_lazy
    image: bank-etl-polars:2.3.1
  - id: load_positions
    pattern: oracle_direct_path
    image: bank-etl-oracle:2.3.1
dependencies:
  - from: extract_positions
    to: transform_positions
  - from: transform_positions
    to: load_positions
```

---

#### 4.6 Conversion plugin interface

A plugin interface should be small and typed:

```python
from __future__ import annotations

from dataclasses import dataclass
from typing import Protocol

from wfssis_ir.models import ComponentNode, ConversionContext


@dataclass(frozen=True)
class SupportDecision:
    supported: bool
    category: str
    confidence: float
    reasons: tuple[str, ...]
    required_reviews: tuple[str, ...] = ()


@dataclass(frozen=True)
class StagePlan:
    stage_type: str
    configuration: dict[str, object]
    generated_tests: tuple[str, ...]


class ComponentConverter(Protocol):
    component_type: str

    def assess(
        self,
        component: ComponentNode,
        context: ConversionContext,
    ) -> SupportDecision:
        ...

    def plan(
        self,
        component: ComponentNode,
        context: ConversionContext,
    ) -> tuple[StagePlan, ...]:
        ...
```

Every plugin should have:

- positive test fixtures;
- unsupported-configuration fixtures;
- expected generated artifacts;
- property-based tests where practical;
- documentation;
- an owning team;
- a maturity status;
- a versioned compatibility matrix.

---

### 5. What the inventory must capture

#### Package and project metadata

- Name and description
- DTSID and ref IDs
- Package format and target version
- Deployment model
- Protection level
- Package version
- Creator and creation date
- Disabled state
- Maximum concurrent executables
- Delay-validation settings
- Checkpoint configuration
- Transaction settings
- Logging configuration
- Package configurations
- Project and package parameters
- Environment references

SSIS supports both package- and project-deployment models, and legacy package configurations differ from the project-parameter/environment model. This distinction affects how target configuration and secrets should be generated. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/integration-services/packages/deploy-integration-services-ssis-projects-and-packages?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/integration-services/packages/deploy-integration-services-ssis-projects-and-packages?view=sql-server-ver17"))

#### Control-flow components

Inventory all observed:

- Sequence containers
- For Loop containers
- Foreach Loop containers
- Execute SQL tasks
- Data Flow tasks
- Execute Package tasks
- File System tasks
- Script Tasks
- Execute Process tasks
- FTP/SFTP tasks
- Web Service tasks
- XML tasks
- Bulk Insert tasks
- Send Mail tasks
- Analysis Services tasks
- Maintenance tasks
- Third-party and custom tasks

For every node, capture:

- disabled state;
- timeout;
- retry-related custom behavior;
- `FailPackageOnFailure`;
- `FailParentOnFailure`;
- expressions on properties;
- transactions;
- precedence constraints;
- event handlers;
- external resources.

#### Data-flow components

##### Sources and destinations

- OLE DB
- ADO.NET
- ODBC
- Oracle
- Flat file
- Excel
- XML
- Raw file
- Recordset
- CDC
- Third-party connectors
- Custom sources and destinations

##### Transformations

- Derived Column
- Lookup
- Conditional Split
- Data Conversion
- Aggregate
- Sort
- Merge
- Merge Join
- Union All
- Multicast
- Row Count
- Script Component
- Slowly Changing Dimension
- Fuzzy Lookup
- Fuzzy Grouping
- Pivot and Unpivot
- OLE DB Command
- Cache Transform
- CDC Splitter
- custom transformations

##### Data semantics

Capture:

- source and target types;
- precision and scale;
- Unicode versus non-Unicode strings;
- code page;
- collation where relevant;
- truncation dispositions;
- error dispositions;
- nullability;
- sort keys;
- synchronous versus asynchronous outputs;
- lookup cache mode;
- no-match behavior;
- fast-load options;
- identity handling;
- null/default handling;
- batch size;
- maximum insert commit size;
- table-lock behavior.

#### Dynamic behavior

Expressions can alter task and connection properties at runtime, including filenames, connection strings, SQL commands, and table names. SSIS property expressions can be attached to packages, tasks, containers, event handlers, connections, log providers, and data-flow objects. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/integration-services/expressions/use-property-expressions-in-packages?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/integration-services/expressions/use-property-expressions-in-packages?view=sql-server-ver17"))

The inventory must therefore report:

- dynamically generated connection strings;
- dynamically generated SQL;
- dynamically generated object names;
- filename expressions;
- variable scopes;
- `EvaluateAsExpression`;
- environment and parameter references;
- variables modified by scripts;
- variables modified by Execute SQL result sets.

---

### 6. Recommended SSIS-to-target mappings

#### 6.1 Control-flow mappings

|SSIS feature|Recommended target|Important guidance|
|---|---|---|
|Execute SQL Task|`SQLExecuteQueryOperator` for concise control SQL, or a database worker task|Store substantial SQL in versioned `.sql` files. Use bind parameters.|
|Data Flow Task|One or more planned SQL/bulk/Polars stages|Do not create one Airflow task per SSIS component.|
|Sequence Container|Airflow TaskGroup|TaskGroup is organizational; transactions and failure semantics still need explicit design.|
|For Loop Container|Bounded dynamic mapping or one worker-side loop|Do not create massive scheduler-level task counts.|
|Foreach File Loop|File-list task followed by bounded dynamic mapping|Freeze the discovered file list for the run and impose a maximum.|
|Execute Package Task|TaskGroup, child DAG, or temporary SSIS bridge|Choose based on lifecycle and retry independence, not visual similarity.|
|File System Task|Typed Python file operation in a pod|Use remote or persistent storage; local pod disk is ephemeral.|
|Script Task|Manual Python rewrite with extracted requirements and tests|Treat Windows APIs, registry, COM, and embedded credentials as high risk.|
|Execute Process Task|Approved container command|Never interpolate untrusted values into shell strings. Prefer direct argument arrays.|
|FTP Task|Approved SFTP or managed-transfer client/operator|Avoid plain FTP. Preserve rename/archive ordering and partial-file behavior.|
|Send Mail Task|Central notification service or Airflow alert integration|Do not embed SMTP credentials or duplicate notification logic in every DAG.|
|Precedence constraint|Dependency, trigger rule, branch, or short-circuit|Reproduce Success/Failure/Completion and expression-and/or-constraint behavior.|
|Event handler|Explicit cleanup, compensation, audit, or alert task|SSIS event bubbling differs from Airflow callbacks; do not translate mechanically.|
|Variables|Typed workflow/run configuration or task-local values|Preserve scope; separate configuration from runtime state.|
|Parameters|Typed deployment configuration|Secrets become secret references, not default values.|
|Connection manager|Airflow connection ID or Vault-backed runtime connection|Do not generate connection strings into DAG source.|
|Checkpoints|Idempotent state/control table and Airflow retry behavior|Never rely on pod-local checkpoint files.|
|Transactions|Single-database transaction or staging plus atomic publish|Avoid attempting distributed transactions across task pods and databases.|
|Logging|Structured JSON logs, metrics, audit tables, OpenLineage|Include source package ID and target code/image version.|

The current MSSQL and Oracle Airflow providers direct SQL work toward `SQLExecuteQueryOperator`; the older database-specific operators have been deprecated or removed from the recommended path. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow-providers-microsoft-mssql/stable/operators.html "https://airflow.apache.org/docs/apache-airflow-providers-microsoft-mssql/stable/operators.html"))

#### 6.2 Data-flow mappings

|SSIS transformation|Preferred target|
|---|---|
|Derived Column|SQL expression first; otherwise Polars `with_columns`|
|Conditional Split|SQL predicates or Polars filters, preserving first-match behavior|
|Data Conversion|Explicit SQL or Polars casts with overflow/truncation tests|
|Lookup|Set-based SQL join; Polars join when data has already left the database|
|Aggregate|SQL `GROUP BY`; otherwise Polars `group_by`|
|Sort|Database `ORDER BY` or Polars sort only when downstream semantics require ordering|
|Merge / Merge Join|SQL join or Polars join with duplicate and null-key tests|
|Union All|SQL `UNION ALL` or Polars concat|
|Multicast|Reuse a materialized stage; do not reread the source unnecessarily|
|Row Count|Audit metric emitted by the runtime|
|OLE DB Command|Rewrite into staged set-based SQL|
|Slowly Changing Dimension|Explicit set-based SCD pattern with history and late-arriving-data tests|
|Pivot / Unpivot|SQL where supported; otherwise Polars|
|Cache Transform|Controlled reference-data cache or preloaded dimension|
|Error output|Reject/quarantine table or file with run, row, column, and reason metadata|
|Script Component|Manual rewrite with a generated input/output contract|
|Fuzzy Lookup / Grouping|Redesign and obtain business-rule approval|
|CDC components|Dedicated incremental-state pattern using approved source capability|
|Custom component|Conversion plugin or explicit redesign|

##### Critical anti-pattern: OLE DB Command

An OLE DB Command often invokes SQL once per row. Do not reproduce this in Python as a loop of database calls.

Instead:

1. Write affected rows to a staging table.
2. Run one set-based `UPDATE`, `MERGE`, `DELETE`, or stored procedure.
3. Record affected and rejected rows.
4. Commit atomically.

##### Critical gotcha: `RetainSameConnection`

An SSIS package may:

1. create a temporary table in one Execute SQL Task;
2. use the same retained connection in another task;
3. depend on that session-scoped object.

Separate Airflow task pods will not share that database session.

The planner must either:

- coalesce those operations into one target task and one transaction;
- or replace the temporary object with a run-scoped persistent staging table.

---

### 7. Recommended Python stack

I use **Polars** below; that is the library's name.

#### Core recommendations

|Area|Recommendation|Position|
|---|---|---|
|Python|One centrally approved release from Airflow's supported range; Python 3.12 is a conservative initial baseline|Avoid multiple Python versions across workflow teams.|
|Orchestration|Apache Airflow 3.x|Pin core and providers through a tested compatibility manifest.|
|OpenShift execution|KubernetesExecutor plus KubernetesPodOperator for specialized jobs|Prefer task isolation and task-specific resource profiles.|
|Package management|`uv` with a committed lock, internal package repository, and frozen builds|`pip` plus constraints remains acceptable where mandated.|
|Configuration/IR|Pydantic|Generates JSON Schema and validates runtime boundaries.|
|Database abstraction|SQLAlchemy Core 2.x|Use Core, not an ORM, for ETL. Drop to the native driver for bulk operations.|
|SQL parsing|SQLGlot|Use for classification, parsing, lineage assistance, and candidate rewrites—not as proof of semantic equivalence.|
|Tabular processing|Polars|Default DataFrame engine for new containerized transformations.|
|Compatibility|Pandas|Use for vendor libraries or functionality unavailable in Polars.|
|Interchange|PyArrow and Parquet|Preferred cross-stage tabular representation.|
|CLI|Typer|Suitable for typed Python command-line applications.|
|Graph analysis|NetworkX or a small typed graph abstraction|Useful for cycle detection, reachability, and dependency analysis.|
|Templates|Jinja2 with strict undefined values|Validate generated Python with `ast.parse`, Ruff, and DAG import tests.|
|Unit testing|pytest|Default test runner.|
|Property testing|Hypothesis|Strong fit for expression, type-conversion, and boundary testing.|
|Data contracts|Pandera|Default for programmatic Pandas/Polars schema checks.|
|Broader data quality|Great Expectations, selectively|Use only when its persistent validation model adds organizational value.|
|Formatting/linting|Ruff|Avoid maintaining separate formatter/import-sorter/linter stacks.|
|Static typing|mypy|Strict for shared libraries; staged adoption for generated glue code.|
|Integration dependencies|Testcontainers where licensing and infrastructure allow|Otherwise use dedicated, isolated test schemas.|
|Lineage|OpenLineage provider|Integrate with the bank's catalog or lineage backend.|

Current Airflow documentation supports a range of modern Python releases and recommends reproducible installation practices. Airflow is unusual in that its core and provider packages move independently, so the bank should maintain and certify a complete compatibility manifest rather than approving "Airflow" as a single version. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/start.html "https://airflow.apache.org/docs/apache-airflow/stable/start.html"))

---

#### 7.1 Polars versus Pandas

##### Recommended default

Use **Polars** for new, nontrivial in-container tabular transformations.

Reasons include:

- lazy query planning;
- predicate and projection pushdown;
- streaming execution where supported;
- Arrow-based interchange;
- parallel execution;
- explicit schema behavior.

Polars' lazy API performs query optimization and can support streaming, but the team must deliberately use lazy `scan_*` patterns and verify that a particular operation remains streamable. A normal eager collection can still require the entire result in memory. ([Polars User Guide](https://docs.pola.rs/user-guide/lazy/using/ "https://docs.pola.rs/user-guide/lazy/using/"))

##### Use Pandas when

- an approved library only accepts Pandas;
- the data set is small;
- an edge operation is unavailable or unstable in Polars;
- developers are doing exploratory work;
- compatibility is more important than throughput.

Pandas supports chunked SQL reads through `chunksize`, but chunking does not automatically make an arbitrary transformation memory-safe. ([Pandas](https://pandas.pydata.org/docs/reference/api/pandas.read_sql.html "https://pandas.pydata.org/docs/reference/api/pandas.read_sql.html"))

##### Use neither when

- data can be transformed set-wise in SQL;
- the job is a large database-to-database transfer;
- the data exceeds a safe percentage of pod memory;
- a native database bulk operation is available;
- the job would simply read rows into Python and immediately write them elsewhere.

##### Recommended engine decision

```text
Can the source database perform the transformation safely?
    Yes → SQL pushdown.

Is this a large unmodified or lightly modified transfer?
    Yes → native export/staging/bulk load.

Does the transformation fit safely inside the pod memory budget?
    Yes → Polars lazy/streaming.

Does a required library only support Pandas?
    Yes → Pandas, chunking where semantics allow.

Otherwise → partition, redesign, or introduce a distributed engine
            only after a demonstrated requirement.
```

##### Memory rule

Do not use a global row-count threshold. Row widths and operations vary too much.

For each job, benchmark:

- source row width;
- estimated decoded size;
- peak transformation memory;
- join or sort amplification;
- output buffering;
- driver buffering.

Set pod limits so that expected peak working memory remains materially below the container limit, leaving headroom for Python, Arrow buffers, drivers, and operating-system overhead.

---

### 8. SQL Server implementation guidance

#### Baseline driver strategy

For the initial enterprise baseline, use:

- Microsoft ODBC Driver 18;
- `pyodbc`;
- SQLAlchemy's `mssql+pyodbc` dialect;
- Airflow's `OdbcHook` when executing through Airflow hooks.

The MSSQL provider's default hook uses `pymssql`, while the provider explicitly recommends `OdbcHook` for ODBC-specific parameters such as driver and encryption settings. SQLAlchemy supports `fast_executemany` for multi-row inserts through pyodbc. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow-providers-microsoft-mssql/stable/_api/airflow/providers/microsoft/mssql/hooks/mssql/index.html "https://airflow.apache.org/docs/apache-airflow-providers-microsoft-mssql/stable/_api/airflow/providers/microsoft/mssql/hooks/mssql/index.html"))

ODBC Driver 18 enables encryption by default. Production configuration should validate the SQL Server certificate chain; do not normalize `TrustServerCertificate=yes` as an enterprise workaround. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server?view=sql-server-ver17"))

##### Evaluate Microsoft `mssql-python`

Microsoft introduced and supports the `mssql-python` driver, including a bulk-copy pattern using Apache Arrow and Parquet. Because it is comparatively new, treat it as an evaluation track rather than immediately replacing the mature ODBC baseline in a bank. Test:

- on-premises SQL Server versions;
- Kerberos or approved authentication;
- TLS and certificate controls;
- Always Encrypted requirements;
- failover behavior;
- connection pooling;
- query cancellation;
- observability;
- bulk performance;
- SQLAlchemy and Airflow integration;
- support procedures.

([Microsoft Learn](https://learn.microsoft.com/en-us/sql/connect/python/mssql-python/python-sql-driver-mssql-python?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/connect/python/mssql-python/python-sql-driver-mssql-python?view=sql-server-ver17"))

##### Bulk patterns

For large loads, evaluate:

- `bcp`;
- `BULK INSERT`;
- `mssql-python` bulk copy;
- pyodbc `fast_executemany`;
- staging tables followed by set-based publish.

Microsoft provides `bcp` and `sqlcmd` for Linux through `mssql-tools18`, so they can be placed in an approved OpenShift worker image from the bank's internal RPM repository. ([Microsoft Learn](https://learn.microsoft.com/en-us/sql/connect/odbc/microsoft-odbc-driver-for-sql-server?view=sql-server-ver17 "https://learn.microsoft.com/en-us/sql/connect/odbc/microsoft-odbc-driver-for-sql-server?view=sql-server-ver17"))

##### SQL Server-specific tests

Test explicitly:

- identity handling;
- `KEEPNULLS` versus defaults;
- Unicode types;
- `datetime` and `datetimeoffset`;
- decimal precision;
- collation and case behavior;
- temporary tables;
- transaction isolation;
- deadlock retry;
- table-lock behavior;
- triggers;
- `MERGE` or alternative upsert behavior;
- Always Encrypted, where present.

---

### 9. Oracle implementation guidance

Use Oracle-maintained `python-oracledb`.

#### Thin versus Thick mode

Start with **Thin mode** because it does not require Oracle Client libraries. Move a task image to Thick mode only where an identified feature requires it.

Airflow's Oracle hook defaults to Thin mode and exposes Thick-mode configuration when needed. Python-oracledb documents the feature differences and notes that a process cannot switch modes after creating a connection or pool. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow-providers-oracle/stable/_api/airflow/providers/oracle/hooks/oracle/index.html "https://airflow.apache.org/docs/apache-airflow-providers-oracle/stable/_api/airflow/providers/oracle/hooks/oracle/index.html"))

Maintain separate images if both modes are required:

```text
bank-etl-oracle-thin
bank-etl-oracle-thick
```

Do not install Oracle Client in every Airflow component merely because one workflow needs it.

#### Bulk operations

Use:

- `executemany()` for batches;
- `direct_path_load()` for very large supported loads;
- explicit bind typing;
- tuned fetch arrays and prefetching;
- staging and set-based publish.

Oracle's driver documentation recommends `executemany()` rather than repeated `execute()` and provides direct-path loading for large data sets, including Arrow-compatible data-frame inputs. ([Python-oracledb Documentation](https://python-oracledb.readthedocs.io/en/latest/user_guide/tuning.html "https://python-oracledb.readthedocs.io/en/latest/user_guide/tuning.html"))

#### Oracle-specific tests

Test explicitly:

- empty string versus `NULL`;
- `DATE` versus `TIMESTAMP`;
- timezone types;
- `NUMBER` precision and scale;
- NLS-dependent parsing and formatting;
- character sets;
- CLOB/BLOB handling;
- quoted identifiers;
- service name versus SID;
- temporary tables;
- global temporary-table commit behavior;
- sequences and triggers;
- PL/SQL exception and return semantics;
- Oracle wallet and TLS configuration;
- Application Continuity requirements;
- direct-path-load restrictions.

Set an application module/action or equivalent session identifier so DBAs can trace queries to:

- Airflow DAG;
- Airflow task;
- run ID;
- source SSIS package;
- target code version.

---

### 10. Cross-database transfer pattern

For SQL Server-to-Oracle or Oracle-to-SQL Server workloads:

```text
1. Establish a repeatable source watermark or snapshot.
2. Extract in bounded batches.
3. Write approved Parquet/Arrow staging artifacts.
4. Validate schema and control totals.
5. Transform using Polars only where SQL pushdown is not appropriate.
6. Bulk load into a run-scoped target staging table.
7. Validate the staging result.
8. Publish atomically using target-database SQL.
9. Record the watermark and audit metrics.
10. Retain or destroy staging according to data classification.
```

Do not attempt a distributed transaction spanning:

- SQL Server;
- Oracle;
- file/object storage;
- multiple OpenShift pods.

Use idempotent staging and explicit state transitions instead:

```text
CREATED
EXTRACTED
TRANSFORMED
LOADED
VALIDATED
PUBLISHED
COMPLETED
```

Each state transition should be attributable to a run ID and safe to repeat or compensate.

---

### 11. Airflow and OpenShift design

#### 11.1 Recommended Airflow topology

For migration workloads, prefer:

- an external PostgreSQL Airflow metadata database;
- multiple scheduler replicas as supported by the approved Airflow topology;
- separate DAG processor components;
- KubernetesExecutor;
- KubernetesPodOperator for specialized images;
- triggerer for vetted deferrable operators;
- remote log storage;
- immutable, signed Airflow images;
- approved DAG bundles or image-baked DAGs pinned to a commit;
- namespace and service-account separation.

The official Airflow Helm chart recommends an external production database instead of its embedded PostgreSQL dependency and provides configurable pod and container security contexts. ([Apache Airflow](https://airflow.apache.org/docs/helm-chart/stable/production-guide.html "https://airflow.apache.org/docs/helm-chart/stable/production-guide.html"))

#### 11.2 Tenancy and trust boundaries

Treat the ability to author a DAG as the ability to execute code.

Airflow's own security model notes that DAG authors can execute arbitrary code in worker contexts. KubernetesExecutor improves task isolation by running tasks in separate pods, but it does not turn unreviewed DAG code into safe configuration. The triggerer is shared and may execute custom trigger code from multiple workflows, so custom deferrable triggers should be tightly controlled. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/security/security_model.html "https://airflow.apache.org/docs/apache-airflow/stable/security/security_model.html"))

For a bank:

- Separate Airflow deployments or namespaces by material trust boundary.
- Do not give every DAG author access to every connection.
- Use task-specific service accounts.
- Use admission and cluster policies to restrict:
    - registries;
    - images;
    - privileged settings;
    - host mounts;
    - service accounts;
    - resource ranges;
    - namespace access.
- Permit custom operators and triggers only from approved internal packages.

#### 11.3 OpenShift security posture

Build images that run under OpenShift's restricted security context and arbitrary user ID.

Required image properties:

- no root requirement;
- no privileged mode;
- no fixed UID assumption;
- writable directories group-writable for the OpenShift-assigned user;
- read-only root filesystem where practical;
- explicit writable `/tmp`;
- all Linux capabilities dropped;
- privilege escalation disabled;
- seccomp enabled;
- no host network;
- no host path mounts;
- resource requests and limits;
- liveness/readiness/startup probes where applicable.

Although Airflow's chart describes an `anyuid` SCC route, use it only as a documented exception. The safer design is to make Airflow and worker images compatible with the restricted SCC. OpenShift commonly runs containers under an arbitrary UID, which must be considered when assigning filesystem permissions. ([Red Hat Documentation](https://docs.redhat.com/en/documentation/openshift_container_platform/4.18/html-single/images/index "https://docs.redhat.com/en/documentation/openshift_container_platform/4.18/html-single/images/index"))

#### 11.4 Airflow pools and database protection

Create pools such as:

```text
sqlserver_core_read
sqlserver_core_write
oracle_finance_read
oracle_finance_write
file_transfer_external
month_end_priority
```

Use weighted pool slots for especially heavy jobs.

Airflow pools are designed to constrain task parallelism against systems that may otherwise be overwhelmed. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/administration-and-deployment/pools.html "https://airflow.apache.org/docs/apache-airflow/stable/administration-and-deployment/pools.html"))

Also configure:

- `max_active_runs`;
- task concurrency;
- DAG concurrency;
- database-side resource governance;
- query and lock timeouts;
- bounded retries;
- exponential backoff with a maximum;
- a deadlock-specific retry policy.

#### 11.5 XCom and intermediate data

XCom may contain:

- row counts;
- file/object identifiers;
- watermark values;
- job IDs;
- compact status metadata.

XCom must not contain:

- DataFrames;
- result sets;
- passwords;
- access tokens;
- PII;
- unbounded error payloads.

Use approved object storage, persistent volumes, or target staging tables for larger artifacts. Airflow's default XCom backend stores values in the metadata database and recommends object storage for larger values. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/core-concepts/xcoms.html "https://airflow.apache.org/docs/apache-airflow/stable/core-concepts/xcoms.html"))

#### 11.6 Secrets

Recommended hierarchy:

1. Bank enterprise secret manager, such as Vault
2. Airflow secrets backend or External Secrets Operator
3. Short-lived task-specific Kubernetes secret, only where necessary
4. Airflow metadata-database connection value only by approved exception

Airflow supports external secrets backends, including HashiCorp Vault, and External Secrets Operator can synchronize external secret values into Kubernetes resources. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow-providers-hashicorp/stable/secrets-backends/hashicorp-vault.html "https://airflow.apache.org/docs/apache-airflow-providers-hashicorp/stable/secrets-backends/hashicorp-vault.html"))

Do not:

- write secrets into DAGs;
- put passwords in environment manifests;
- pass them on process command lines;
- log complete connection strings;
- return them through XCom;
- embed them in generated test fixtures.

---

### 12. Transitional SSIS bridge

A large estate benefits from a **strangler migration**.

Before every package is rewritten, Airflow may temporarily orchestrate existing SSIS packages through a controlled Windows execution agent.

#### Suggested design

```text
Airflow task
  → authenticated internal execution API
  → Windows SSIS runner service
  → SSISDB execution or dtexec
  → execution ID returned
  → deferrable status polling
  → final result and audit data returned
```

The request should contain:

- package identity;
- approved environment reference;
- logical run date;
- correlation ID;
- nonsecret parameters;
- requesting Airflow deployment and DAG.

It should not contain:

- passwords;
- decrypted connection strings;
- arbitrary command-line text;
- arbitrary package paths.

Benefits:

- Airflow scheduling and observability can be introduced early.
- Dependencies between migrated and unmigrated workflows can be managed.
- Cutovers can occur package by package.
- The orchestration migration is separated from the transformation migration.

Set a sunset date and report every remaining bridge invocation. Otherwise the bridge can become a permanent second platform.

---

### 13. Testing and certification strategy

#### 13.1 Test layers

##### Extractor tests

- Golden DTSX packages for every supported SSIS version
- Control-flow snapshots
- Data-flow snapshots
- Encrypted-package behavior
- Missing custom assembly
- Invalid package
- Package versus project deployment
- Stable canonical IDs
- Secret-redaction tests

##### IR tests

- JSON Schema validation
- Backward-compatible schema migration
- Graph integrity
- Duplicate ID detection
- Unknown-value handling
- Serialization round trips

##### Analyzer tests

- Component-signature matching
- Support classification
- Risk finding generation
- Dependency discovery
- Package scoring
- No silent fallback for unknown configurations

##### Generator tests

- Golden generated artifacts
- Python `ast.parse`
- Ruff
- mypy
- DAG import through Airflow `DagBag`
- Deterministic regeneration
- No hand-written file overwrite
- No unresolved template values

Airflow documents DAG loader tests through `DagBag`, making DAG import and structural validation a practical CI gate. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/best-practices.html "https://airflow.apache.org/docs/apache-airflow/stable/best-practices.html"))

##### Transformation tests

Use:

- pytest fixtures and parametrization;
- Hypothesis for boundary cases;
- Pandera for DataFrame schemas;
- SQL-specific unit fixtures;
- type-aware expected results.

Pytest provides fixtures and parametrized cases, Hypothesis generates property-based inputs, and Pandera supports validation for both Pandas and Polars. ([pytest](https://docs.pytest.org/en/stable/how-to/parametrize.html "https://docs.pytest.org/en/stable/how-to/parametrize.html"))

##### Integration tests

Execute against approved SQL Server and Oracle test instances.

Test:

- authentication;
- certificate validation;
- read and write permissions;
- transaction behavior;
- bulk paths;
- timeouts;
- cancellations;
- failover;
- network interruption;
- invalid data;
- deadlocks;
- task restart.

Use Testcontainers only where database images, licenses, and security policy permit. A bank may instead provide dedicated transient databases or create isolated run-scoped schemas. ([Testcontainers](https://testcontainers.com/guides/getting-started-with-testcontainers-for-python/ "https://testcontainers.com/guides/getting-started-with-testcontainers-for-python/"))

---

#### 13.2 SSIS-versus-target reconciliation

For each converted package, compare:

##### Structure

- expected tables, files, and partitions;
- columns;
- types;
- nullability;
- keys;
- indexes or post-load structures where in scope.

##### Data

- row counts;
- rejected-row counts;
- null counts by column;
- distinct key counts;
- min/max values;
- numeric sums;
- date ranges;
- key-based checksums;
- duplicate-key counts;
- full row-level differences where feasible.

##### Behavior

- empty source;
- duplicate input;
- invalid types;
- truncation;
- lookup misses;
- late-arriving data;
- restart after extraction;
- restart after load;
- repeated execution for the same logical date;
- partial target failure;
- unavailable source;
- alert and reject behavior.

##### Side effects

- archive files;
- renamed files;
- deleted files;
- stored-procedure calls;
- audit records;
- notifications;
- watermarks;
- downstream triggers.

##### Canonical comparisons

Do not rely blindly on database-native hashes across SQL Server and Oracle. Type rendering, encodings, collation, date formatting, and null representation can differ.

Use a canonical type-aware serializer:

```text
integer       → normalized decimal string
decimal       → fixed scale agreed by contract
timestamp     → UTC ISO-8601 with declared precision
date          → ISO date
text          → normalized encoding without destructive trimming
binary        → hex or base64
null          → explicit null marker
```

Hash canonicalized values only after the contract specifies how semantically equivalent representations are normalized.

##### Certification record

Store:

- source package hash;
- extracted IR hash;
- generator version;
- rule-set version;
- Git commit;
- container image digest;
- Airflow DAG version;
- input watermark;
- SSIS execution ID;
- Airflow run ID;
- comparison results;
- performance results;
- approvers;
- exceptions.

---

### 14. CI/CD and repository organization

#### Recommended repository boundaries

##### Migration factory repository

```text
ssis-modernization-factory/
  pyproject.toml
  uv.lock

  windows-extractor/
    SsisExtractor.sln
    src/
    tests/

  powershell/
    WF.Ssis.Inventory.psd1
    WF.Ssis.Inventory.psm1
    tests/

  packages/
    wfssis_ir/
    wfssis_analyzer/
    wfssis_planner/
    wfssis_codegen/
    wfssis_cli/
    wfssis_reconciliation/

  rules/
    components/
    expressions/
    sql/
    data_types/

  templates/
    airflow/
    workers/
    sql/
    runbooks/

  samples/
    golden-packages/
    expected-ir/
    expected-generated/

  docs/
    support-matrix/
    architecture/
    conversion-guides/
```

##### Shared runtime repository

```text
bank-etl-runtime/
  packages/
    bank_etl_core/
    bank_etl_sqlserver/
    bank_etl_oracle/
    bank_etl_files/
    bank_etl_polars/
    bank_etl_audit/
    bank_etl_lineage/
  images/
    base/
    sqlserver/
    oracle-thin/
    oracle-thick/
    polars/
```

##### Domain workflow repositories

```text
finance-data-workflows/
  dags/
  workflows/
    positions/
      manifest/
      generated/
      extensions/
      sql/
      contracts/
      tests/
      runbook/
```

##### Platform repository

```text
airflow-openshift-platform/
  helm/
  policies/
  network-policies/
  service-accounts/
  observability/
  disaster-recovery/
```

This separation limits the blast radius of:

- compiler changes;
- runtime-library changes;
- platform changes;
- domain workflow changes.

#### CI stages

```text
1. Validate source and IR schemas
2. Run extractor and analyzer golden tests
3. Generate artifacts
4. Regenerate and assert no unexpected diff
5. Ruff check and format verification
6. mypy
7. pytest and coverage
8. Airflow DAG import tests
9. SQL static checks
10. Secret scan
11. SAST and dependency scan
12. Build containers
13. Generate SBOM
14. Scan containers
15. Sign images and attestations
16. Deploy to integration
17. Run SQL Server and Oracle integration tests
18. Run reconciliation tests
19. Promote the same image digest
```

Ruff provides formatting and linting, while mypy supplies static type checking for typed Python. ([Astral Docs](https://docs.astral.sh/ruff/ "https://docs.astral.sh/ruff/"))

#### Dependency controls

- All packages come from an internal approved mirror.
- Commit lockfiles.
- Install with frozen resolution.
- Pin container bases by digest.
- Do not run `pip install` when a task starts.
- Scan Airflow core, providers, OS RPMs, Oracle Client, ODBC Driver, and custom wheels.
- Maintain a dependency exception process.
- Test patched dependency overlays against the complete DAG corpus.

---

### 15. Banking security and audit controls

#### Source-code and AI controls

SSIS packages frequently contain:

- SQL;
- object names;
- host names;
- account names;
- file paths;
- business logic;
- sometimes sensitive configuration.

Do not submit packages, SQL, or extracted metadata to a SaaS converter or external generative-AI service without explicit approval.

For any AI-assisted conversion:

- require an approved deployment boundary;
- prohibit vendor training on bank content;
- define retention and deletion;
- capture model and prompt versions;
- scan inputs for secrets;
- require human review;
- require deterministic tests;
- treat output as untrusted source code;
- preserve evidence of how it was produced.

The deterministic rules engine, not an LLM, should decide whether a component is certified as supported.

#### Software supply chain

Align the factory with the bank's secure-development standard and NIST SSDF principles:

- protected source;
- reviewed pull requests;
- separation of duties;
- signed commits where required;
- SAST;
- software composition analysis;
- container scanning;
- SBOM generation;
- signed images;
- provenance attestations;
- deployment-policy verification;
- vulnerability remediation SLAs.

NIST SP 800-218 defines a secure software development framework that can be integrated into existing development lifecycles. CycloneDX provides an SBOM standard, and Cosign can sign and verify OCI container images and related artifacts. ([NIST Computer Security Resource Center](https://csrc.nist.gov/pubs/sp/800/218/final "https://csrc.nist.gov/pubs/sp/800/218/final"))

#### Data controls

- Production data should not be copied to developer workstations.
- Nonproduction test data should be masked or synthetic.
- Temporary Parquet files inherit the source classification.
- Encrypt staging storage in transit and at rest.
- Set explicit staging retention.
- Do not log row contents by default.
- Reject stores require their own access policy and retention.
- Audit queries and reconciliation reports may themselves contain sensitive metadata.

#### Identity and access

- Separate service identities by environment and domain.
- Use least-privilege database roles.
- Separate read, stage-write, publish, and administrative privileges.
- Prefer short-lived credentials.
- Use SQL Server certificate validation and approved authentication.
- Use Oracle wallet/mTLS where applicable.
- Avoid shared generic ETL accounts.
- Prohibit personal credentials in production.
- Review SQL Agent proxy privileges during migration; they often hide capabilities that must be recreated explicitly.

#### Network controls

- Default-deny NetworkPolicies.
- Explicit SQL Server and Oracle egress.
- Explicit DNS and approved secret-manager egress.
- No unrestricted internet access from task pods.
- Approved package repositories are used only at image-build time.
- File-transfer tasks receive only destination-specific egress.

#### Audit trail

For every task, record:

```text
business workflow
DAG ID and task ID
logical data interval
Airflow run ID
source SSIS package and hash
Git commit
container image digest
service identity
source and target systems
input/output/reject counts
watermark
start and end timestamps
outcome
retry number
exception classification
```

---

### 16. Observability and lineage

#### Logs

Use structured logs:

```json
{
  "event": "load_completed",
  "dag_id": "finance_positions",
  "task_id": "load_oracle_stage",
  "run_id": "...",
  "source_package": "LoadPositions",
  "source_package_sha256": "...",
  "rows_read": 1200345,
  "rows_written": 1200312,
  "rows_rejected": 33,
  "duration_ms": 184223,
  "target_table": "FINANCE.POSITION_STAGE"
}
```

Do not log complete SQL parameters where they may contain sensitive data.

#### Metrics

At minimum:

- task duration;
- queue duration;
- rows read/written/rejected;
- bytes read/written;
- database execution duration;
- retries;
- deadlocks;
- connection failures;
- file counts;
- watermark lag;
- reconciliation failures;
- SSIS bridge executions;
- unsupported conversion findings.

#### Lineage

Use OpenLineage as the target interchange format, integrating with the bank's existing data catalog where possible.

The official Airflow OpenLineage provider emits workflow and task events, and supported operators add richer input/output and SQL metadata. Custom ETL operators can provide their own lineage extraction. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow-providers-openlineage/stable/index.html "https://airflow.apache.org/docs/apache-airflow-providers-openlineage/stable/index.html"))

Add custom facets for:

- original SSIS package;
- source hash;
- converter version;
- migration-wave ID;
- reconciliation status;
- business control ID;
- data classification.

---

### 17. Open-source ecosystem assessment

There does not appear to be a mature, widely adopted, open-source, production-ready SSIS-to-Airflow converter that should be adopted wholesale for a bank. There are useful building blocks and reference implementations.

|Tool/project|Potential use|Recommendation|
|---|---|---|
|Microsoft ManagedDTS|Authoritative package object extraction|**Use as the foundation.**|
|SSISDB catalog views and utilities|Deployment, configuration, history, runtime inventory|**Use.**|
|Microsoft DMA Integration Services assessment|Supplemental compatibility findings|Use as a secondary analyzer; its target assumptions differ from Airflow/OpenShift.|
|`gossisMCP`|Broad SSIS package-analysis ideas|Review code and tests; do not assume bank readiness.|
|AWS `dtsx-sql-extractor` sample|Examples of extracting SQL from DTSX|Mine for test cases and ideas only.|
|`neednlab/ssis_analyzer`|Simple connection/source/target lineage examples|Reference only; the public repository is small and has no formal releases.|
|Databricks Lakebridge SSIS support|Open examples of SSIS-to-Databricks translation|Study mapping and unresolved-issue patterns; target is not Airflow/OpenShift.|
|SQLGlot|SQL parsing, classification, and candidate dialect conversion|Use behind mandatory dialect-specific tests.|
|Airflow official providers and Helm chart|Target orchestration and deployment|**Use.**|
|OpenLineage|Runtime lineage interchange|**Use or integrate with the bank's equivalent.**|

Community SSIS analyzers demonstrate that DTSX metadata and lineage extraction are feasible, but their scope and maturity are much smaller than a bank migration factory requires. Databricks Lakebridge has added an SSIS conversion path, but its generated target is Databricks rather than the requested Python/Airflow/OpenShift architecture. ([GitHub](https://github.com/MCPRUNNER/gossisMCP "https://github.com/MCPRUNNER/gossisMCP"))

#### Apache Hop, Airbyte, and similar platforms

These may be useful for selected connector or ingestion use cases, but introducing another general ETL runtime would create:

- another security boundary;
- another dependency model;
- another operational interface;
- another skills requirement;
- another conversion target.

Do not add one merely to reproduce SSIS's graphical model. Consider such a product only where it solves a clearly bounded connector or data-movement problem better than the standardized Python runtime.

---

### 18. Commercial ecosystem assessment

#### Direct and near-direct conversion offerings

|Offering|Publicly advertised capability|Fit for this target|
|---|---|---|
|Travinto|Markets an SSIS-to-Airflow converter and "single-click" migration|Potential direct fit, but public material is high-level. Require an evidence-based POC.|
|Bitwise ETL Converter|Automated ETL assessment/conversion; public examples include SSIS-to-ADF and ETL-to-Glue/PySpark|Possible consulting/accelerator partner, but Airflow/OpenShift output must be demonstrated.|
|Impetus LeapLogic|Automated assessment, transformation, and validation for Databricks/Spark and cloud targets|Useful migration expertise; public target is not this stack.|
|SnowConvert AI|SSIS preview conversion to Snowflake/dbt and Snowflake orchestration patterns|Not a direct target; potentially useful as a benchmark for component analysis and exception reporting.|
|Varigence/Biml|Metadata-driven generation of SSIS and related assets|Useful only where already owned or where it aids metadata extraction; not the strategic target.|
|Astronomer Astro Private Cloud|Commercial self-hosted Airflow platform supporting Kubernetes/OpenShift|Strong platform-support candidate; does not perform SSIS conversion.|

Travinto publicly advertises an SSIS-to-Airflow converter, but the public page does not provide enough detailed evidence to establish component coverage, generated-code quality, testing, or bank deployment characteristics. Treat it as a lead for a controlled POC, not as an assumed solution. ([Travinto](https://travinto.com/products/code-converter/ssis-to-airflow "https://travinto.com/products/code-converter/ssis-to-airflow"))

Bitwise publishes SSIS-to-ADF and other automated ETL migration examples, while LeapLogic publishes SSIS-to-Databricks and broader legacy-modernization capabilities. SnowConvert's SSIS support targets Snowflake/dbt. These offerings may contribute assessment IP or services, but none should be presumed to generate the bank's desired Airflow/OpenShift architecture without demonstration. ([Bitwise Global](https://www.bitwiseglobal.com/insights/accelerated-ssis-etl-migration-to-azure-data-factory "https://www.bitwiseglobal.com/insights/accelerated-ssis-etl-migration-to-azure-data-factory"))

Astronomer's self-hosted Astro Private Cloud documents OpenShift support and is worth evaluating where enterprise Airflow support, lifecycle management, and vendor accountability are priorities. ([Astronomer](https://www.astronomer.io/docs/astro-private-cloud/apc-features "https://www.astronomer.io/docs/astro-private-cloud/apc-features"))

#### Vendor POC scorecard

Use the bank's own 15–20 representative packages and weight the evaluation approximately as follows:

|Area|Weight|Evidence required|
|---|--:|---|
|Security and deployment|25%|On-premises/offline operation, no exfiltration, IAM, audit, SBOM, scanning|
|Semantic coverage|25%|Supported-component matrix and correct output for the bank corpus|
|Validation|20%|Generated tests, row/data reconciliation, restart testing|
|Output maintainability|15%|Readable source, deterministic generation, no proprietary runtime lock-in|
|Target fit|10%|Airflow 3.x, Python, SQL Server, Oracle, OpenShift|
|Commercial and exit terms|5%|Source ownership, escrow if relevant, support, no model training, termination rights|

##### Mandatory vendor questions

- Can the tool run fully inside the bank?
- Are package content, SQL, metadata, and outputs retained?
- Are they used to train any model?
- Can it process protected packages without receiving secrets?
- Does it support third-party components?
- Does it produce a machine-readable unsupported-feature report?
- Is generated Python readable and owned by the bank?
- Is a proprietary runtime required in production?
- Does output support Airflow 3?
- Does output run under restricted OpenShift SCC?
- Does it support the bank's SQL Server and Oracle authentication?
- Are tests and reconciliation generated?
- Can generation be reproduced with a pinned tool version?
- Are an SBOM, vulnerability policy, penetration-test summary, and audit evidence available?
- What happens when the contract ends?

---

### 19. Python onboarding for experienced C# engineers

Your C# background is a significant advantage. The challenge is not basic programming; it is learning Python packaging, runtime behavior, typing limitations, and data-engineering practices.

#### Concept mapping

|C# concept|Python equivalent|
|---|---|
|Solution/project|Workspace and Python packages using `pyproject.toml`|
|NuGet|Internal Python package index plus lockfile|
|`.csproj`|`pyproject.toml`|
|Interface|`typing.Protocol` or abstract base class|
|Record|Frozen dataclass or Pydantic model|
|`using` / `IDisposable`|Context manager and `with`|
|LINQ|SQL, Polars expressions, comprehensions|
|NUnit/xUnit|pytest|
|Roslyn analyzers|Ruff and mypy|
|Strong runtime typing|Type hints plus static checking and runtime boundary validation|
|Dependency injection container|Prefer explicit constructor or function injection|
|`async`/`await`|Available, but not automatically beneficial for batch database ETL|
|`appsettings.json`|Typed configuration plus external secret references|
|Publish artifact|Wheel and container image|

Python type hints are not normally enforced at runtime. Use mypy for static analysis and Pydantic/Pandera at external boundaries. ([Python documentation](https://docs.python.org/3/library/typing.html "https://docs.python.org/3/library/typing.html"))

#### Training sequence

##### Foundation

- Python syntax and data structures
- Functions, modules, imports
- exceptions
- context managers
- iterators and generators
- type hints
- dataclasses
- virtual environments
- `pyproject.toml`

##### Engineering standards

- package layout
- dependency locking
- pytest
- mocking versus real integration tests
- Ruff
- mypy
- logging
- configuration
- error handling

##### Data engineering

- SQLAlchemy Core
- database transactions
- pyodbc
- python-oracledb
- Polars lazy expressions
- PyArrow and Parquet
- explicit schemas
- chunking and streaming
- data reconciliation

##### Airflow

- DAG and task lifecycle
- data intervals
- catchup and backfill
- retries and idempotency
- task mapping
- pools
- trigger rules
- sensors and deferrable operators
- XCom
- Kubernetes pods
- secrets and connections
- DAG testing

#### Coding standards

- Require type hints on public APIs.
- Use pure transformation functions where possible.
- Keep DAG files thin.
- Do not place heavy imports or database calls at DAG parse time.
- Do not use notebooks as production source.
- Do not use global mutable state.
- Do not create implicit database transactions.
- Use context managers for connections and files.
- Avoid framework-heavy dependency injection.
- Use domain-specific exception types.
- Fail explicitly on unexpected schemas.
- Keep SQL in `.sql` files when it is substantial.
- Require docstrings for shared public APIs, not every trivial function.

Airflow best practices warn against expensive top-level DAG code and recommend generating stable, local configuration rather than querying external databases during DAG parsing. ([Apache Airflow](https://airflow.apache.org/docs/apache-airflow/stable/best-practices.html "https://airflow.apache.org/docs/apache-airflow/stable/best-practices.html"))

---

### 20. Highest-risk migration gotchas

|Risk|Required mitigation|
|---|---|
|Encrypted or user-key-protected package|Extract under approved original context or obtain an approved re-export; never bypass protection.|
|Missing custom component assembly|Install into an isolated extractor worker or use fallback metadata extraction and classify as unresolved.|
|Script Task/Component|Extract requirements and available source; manually rewrite and test.|
|COM, Excel, Access, registry, Windows API|Redesign for Linux/OpenShift or isolate a temporary Windows service.|
|UNC/file-share assumptions|Replace with approved storage or mount with explicit identity and locking behavior.|
|`RetainSameConnection`|Coalesce tasks or replace session state with run-scoped persistent staging.|
|Package transaction|Redesign with single-database transaction or staging/atomic publish.|
|Distributed transaction|Replace with idempotent state transitions and compensation.|
|Event-handler business logic|Make it explicit in the DAG; do not bury business actions in callbacks.|
|Error rows ignored|Create explicit reject behavior and acceptance thresholds.|
|First-match Conditional Split|Preserve rule order.|
|Lookup no-match behavior|Test fail, redirect, ignore, and default behavior separately.|
|Unicode and code pages|Define target encoding and test non-ASCII production-like values.|
|Decimal precision|Use `Decimal`, explicit schemas, and overflow tests.|
|Date/time behavior|Define source timezone, logical date, UTC conversion, and precision.|
|Airflow catchup|Set deliberately; do not accidentally backfill years of migrated jobs.|
|Retry of a non-idempotent task|Add run keys, staging, and duplicate protection before enabling retries.|
|Local files between tasks|Replace with remote storage or combine into one task.|
|Huge XCom values|Store the artifact externally and pass only its identifier.|
|Task explosion|Bound dynamic mapping and aggregate fine-grained work inside worker pods.|
|Database overload|Enforce Airflow pools and database-side resource governance.|
|Runtime package installation|Build immutable task images.|
|Generated-code edits|Keep custom code in extension points and fail CI on generated drift.|
|Dormant annual/regulatory workload|Review a complete business calendar before retirement.|
|Row order differences|Compare sets unless ordering is a documented output contract.|
|SQL transpilation|Treat SQLGlot output as a candidate requiring dialect tests.|
|Vendor conversion percentage|Measure certified semantic parity, not generated lines or files.|

---

### 21. Recommended initial factory scope

The first generator release should support only features confirmed by inventory, but a sensible initial candidate scope is:

#### Automatic candidates

- Execute SQL Task
- Data Flow Task with:
    - SQL Server and Oracle relational sources
    - SQL Server and Oracle relational destinations
    - flat-file source and destination
    - Derived Column
    - Data Conversion
    - Conditional Split
    - Lookup
    - Aggregate
    - Sort
    - Union All
    - Merge Join
    - Row Count
    - standard error outputs
- Sequence Container
- simple success/failure/completion precedence
- project and package parameters
- common variable references
- common property expressions
- File System Task
- static Execute Package Task
- standard audit logging

#### Assisted candidates

- For and Foreach loops
- dynamic SQL
- dynamic connection properties
- SCD
- Pivot/Unpivot
- Execute Process
- FTP/SFTP
- package checkpoints
- event handlers
- parent/child package refactoring
- cross-database transfers
- package transactions
- CDC

#### Manual or redesign initially

- Script Tasks
- Script Components
- custom and third-party components
- fuzzy transformations
- Excel/Access/COM automation
- Windows registry interaction
- distributed transactions
- deeply dynamic package construction
- packages whose behavior cannot be reproduced from available source and runtime evidence

---

### 22. Program metrics

Track metrics that reveal actual progress and risk.

#### Inventory

- Percentage of authoritative repositories scanned
- Percentage of schedules attributed
- Percentage of packages with owners
- Percentage of packages with runtime history
- Unknown or unextractable package count
- Embedded-secret findings

#### Coverage

- Component support by occurrence
- Component support weighted by executions
- Package support weighted by business criticality
- SQL Server/Oracle/file pattern coverage
- Unsupported custom-component count

#### Conversion

- Automatically generated and certified
- Generated but requiring review
- Framework-assisted manual
- Redesigned
- Retired
- Blocked

#### Quality

- Unit and integration pass rate
- Reconciliation pass rate
- Defect escape rate
- Restart/idempotency pass rate
- Security findings
- Generated-code drift
- Mean manual engineering effort per pattern

#### Runtime

- Duration versus SSIS
- Database CPU and I/O impact
- OpenShift CPU and memory
- Rows or bytes per second
- Retry rate
- SLA attainment
- Reject rate
- Watermark lag

Do not use "percentage of code converted" as a primary metric. SSIS packages are graphs and metadata; lines of generated Python have little relationship to migration completeness.

---

### 23. Concrete first implementation tranche

The first deliverable should be an **inventory and assessment product**, not the converter.

It should produce, for the complete accessible estate:

1. A package manifest with hashes and authoritative locations
2. A schedule and dependency graph
3. A component-frequency heat map
4. A connection and external-dependency inventory
5. A parameter, expression, and configuration report
6. A custom/script/Windows-dependency report
7. A security finding report
8. Runtime usage and criticality data
9. A package-level migration scorecard
10. A proposed pilot and wave plan
11. A machine-readable intermediate representation
12. An HTML or internal-dashboard view for stakeholders

Only after those results exist should the team finalize the initial conversion-plugin backlog.

The most important success pattern is:

```text
Inventory first
  → build the support matrix
  → implement high-frequency patterns
  → generate deterministic targets
  → validate against SSIS
  → expand the rule set from evidence
```

That creates an asset the bank can reuse across hundreds or thousands of packages, keeps the target stack understandable to engineers, and avoids becoming dependent on a converter that produces superficially plausible but operationally uncertified Python.
