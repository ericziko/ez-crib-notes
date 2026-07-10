---
uid: 384ae152-aa92-4362-8115-29aa37a2c3a8
---
# Apache Airflow for SSIS Developers — A Migration Guide

*A practical overview for engineers moving ETL work from SQL Server Integration Services (SSIS) into Python. Written for someone who knows SSIS well but has never touched Airflow, Pandas, or Polars.*

---

## Table of Contents

1. [The 60-second summary](#the-60-second-summary)
2. [Part 1 — Apache Airflow](#part-1--apache-airflow)
   - [What Airflow actually is (in SSIS terms)](#what-airflow-actually-is-in-ssis-terms)
   - [The mental-model shift](#the-mental-model-shift-orchestration-vs-transformation)
   - [Concept map: SSIS → Airflow](#concept-map-ssis--airflow)
   - [Why move to Airflow (and why not)](#why-move-to-airflow-and-why-not)
   - [Core Airflow building blocks](#core-airflow-building-blocks)
   - [How a migration actually works](#how-a-migration-actually-works)
   - [Example 1 — Simple table-to-table load](#example-1--a-simple-ssis-package-in-airflow)
   - [Example 2 — Control flow, containers, branching & loops](#example-2--control-flow-containers-branching--loops)
3. [Part 2 — Pandas & Polars](#part-2--pandas--polars)
   - [Pandas overview](#pandas-overview)
   - [Polars overview](#polars-overview)
   - [Pandas vs Polars — which to choose](#pandas-vs-polars--which-to-choose)
   - [SSIS Data Flow components → Pandas/Polars](#ssis-data-flow-components--pandaspolars)
   - [Worked example — a full Data Flow rebuilt](#worked-example--a-full-ssis-data-flow-rebuilt)
4. [Putting it together & recommended path](#putting-it-together--a-recommended-migration-path)

---

## The 60-second summary

- **SSIS bundles two jobs into one tool:** *orchestration* (the Control Flow — the order things run in, looping, error handling) and *transformation* (the Data Flow — the actual moving and reshaping of rows).
- **In the Python world these two jobs are split:**
  - **Apache Airflow replaces the Control Flow** — it's the orchestrator/scheduler. It decides *what runs, in what order, when, and what happens on failure*. It is **not** a data-transformation engine.
  - **Pandas / Polars (or SQL, or Spark) replace the Data Flow** — they do the actual row-level work that SSIS Data Flow components (Lookup, Derived Column, Conditional Split, Aggregate…) used to do.
- **A `.dtsx` package becomes:** an Airflow **DAG** (a Python file defining tasks + dependencies) that calls out to Python transformation code and/or SQL.
- **The SQL Server Agent job / SSIS Catalog schedule becomes:** Airflow's built-in **scheduler**.
- The biggest change is cultural: **you write code, and that code lives in Git.** You get version control, code review, unit testing, and real CI/CD — things that were painful or impossible with the SSIS drag-and-drop designer.

---

# Part 1 — Apache Airflow

## What Airflow actually is (in SSIS terms)

Apache Airflow is an open-source **workflow orchestrator**. You describe your pipelines as code (Python), and Airflow schedules them, runs them, retries them on failure, tracks their history, and gives you a web UI to monitor everything.

The single most important sentence to internalize:

> **Airflow is the SSIS Control Flow. It is *not* the SSIS Data Flow.**

In SSIS, when you open a package you see two design surfaces:

| SSIS design surface | What it does | Python-world equivalent |
|---|---|---|
| **Control Flow** tab | Order of operations, containers, precedence constraints, Execute SQL Task, Send Mail, error handling | **Airflow DAG** (this is Airflow's whole job) |
| **Data Flow** tab | Sources, destinations, Lookups, Derived Columns, Conditional Splits, Aggregates | **Pandas / Polars / SQL / Spark** (Airflow only *triggers* this) |

Airflow doesn't have a "Data Flow" of its own. When an Airflow task needs to reshape data, it runs Python code (Pandas/Polars) or fires SQL at a database. Airflow's job is only to make sure that code runs in the right order, at the right time, with retries and alerting.

An Airflow pipeline is called a **DAG** — a *Directed Acyclic Graph*. That's just a fancy name for "boxes with arrows that don't loop back on themselves" — which is exactly what your SSIS Control Flow already is. Each box is a **task**; each arrow is a **dependency** (the equivalent of an SSIS precedence constraint).

## The mental-model shift: orchestration vs transformation

This split trips up every SSIS developer at first, so it's worth dwelling on.

In SSIS, a **Data Flow Task** is a single box in the Control Flow, but inside it lives an entire pipeline of buffered, streaming transformations. SSIS moves data through memory buffers row-by-row and rarely lands it to disk.

In Airflow, there is no equivalent streaming buffer engine. Instead you choose **where the transformation happens**, and there are two dominant patterns:

1. **ELT / "push-down" (recommended default):** Extract and Load the data into a database (or data warehouse / lakehouse), then transform it *with SQL inside that database*. Airflow just orchestrates the SQL. This is usually the fastest and cheapest option because databases are extremely good at set-based operations — and it maps naturally to your existing T-SQL skills.
2. **ETL in Python:** Pull data into an Airflow worker's memory as a **DataFrame** (Pandas or Polars), transform it in Python, then write it back out. This is the closest analog to an SSIS Data Flow, and it's the right choice for complex row logic, calling APIs, ML featurization, or when the transformation doesn't map cleanly to SQL.

Most real migrations use **both**: SQL push-down where it's natural, Pandas/Polars where the logic is genuinely procedural. (That's why Part 2 of this document covers Pandas/Polars in depth.)

## Concept map: SSIS → Airflow

Keep this table next to you for the first month.

| SSIS concept | Airflow equivalent | Notes |
|---|---|---|
| **Package (`.dtsx`)** | **DAG** (a `.py` file) | One package ≈ one DAG. |
| **Control Flow** | **DAG structure** (tasks + dependencies) | The graph itself. |
| **Task** (e.g. Execute SQL Task) | **Task** (an *operator* instance, e.g. `SQLExecuteQueryOperator`) | The unit of work. |
| **Precedence Constraint** (green/red arrow) | **Task dependency** (`a >> b`) + **trigger rules** | Green arrow = success (default). Red arrow = `trigger_rule="all_failed"`. Constraint expressions = branching. |
| **Data Flow Task** | A Python task running **Pandas/Polars**, *or* a SQL task doing push-down | Airflow has no built-in data flow engine. |
| **Sequence Container** | **TaskGroup** | Purely for grouping/visual organization. |
| **For Loop / Foreach Loop Container** | **Dynamic Task Mapping** (`.expand()`) or a Python loop that builds tasks | Foreach over files/rows = dynamic mapping. |
| **Connection Manager** | **Connection** (stored in Airflow, used via a **Hook**) | Centrally managed, encrypted, referenced by ID. |
| **Package/Project Parameter** | **DAG `params`**, **Airflow Variables**, or Jinja templating | Runtime configuration. |
| **Variable** (SSIS variable) | **XCom** (task-to-task values) or **Airflow Variable** (global) | XCom = small values passed between tasks. |
| **Expression** (SSIS expression language) | **Jinja templating** (`{{ ds }}`, `{{ params.x }}`) or plain Python | Airflow templates are far more powerful. |
| **Event Handler** (OnError, OnPostExecute) | **Callbacks** (`on_failure_callback`, `on_success_callback`) | Plus retries and SLAs. |
| **Checkpoints / restartability** | **Task-level retries** + **idempotent tasks** + "clear & rerun" | You rerun individual failed tasks from the UI. |
| **SSIS Catalog (SSISDB)** | **Airflow metadata database** | Stores run history, logs, state. |
| **SQL Server Agent schedule** | **Airflow Scheduler** (`schedule=` on the DAG) | Cron, presets, or data-driven. |
| **Deploy to SSIS Catalog** | **Deploy DAG file to the `dags/` folder** (via Git/CI) | It's just code deployment. |
| **Data Viewer (debug)** | Logs + `print`/`logging` + local test runs | Debug by running Python. |
| **Logging to SSISDB** | Task instance **logs** in the UI (and shippable to S3/GCS/ELK) | Every task run has its own log. |

## Why move to Airflow (and why not)

Be honest with your team. Airflow is powerful but it is *not* strictly "better SSIS." It's a different philosophy.

### Reasons it's a strong choice

- **Pipelines are code in Git.** Real diffs, code review, branching, and rollback. No more comparing two `.dtsx` XML blobs or emailing packages around.
- **Testable.** You can unit-test your transformation functions with `pytest`. SSIS testing was mostly "run it and eyeball the output."
- **Cross-platform & open source.** Runs on Linux/containers/Kubernetes; no SQL Server licensing tie-in. Connects to virtually anything (databases, cloud storage, REST APIs, Spark, dbt, Snowflake, BigQuery, Kafka…) via **providers**.
- **Dynamic pipelines.** You can generate tasks in a loop. Need to process one task per file in a folder, or per table in a config list? A few lines of Python instead of a Foreach container with expressions.
- **Great observability.** A web UI showing every run, every task, timing (Gantt view), logs, and history. Retries and alerting are first-class.
- **Huge ecosystem & community.** It's the de-facto standard open-source orchestrator, and it's the engine behind managed services (AWS **MWAA**, Google Cloud **Composer**, **Astronomer**), so you're not locked in.
- **Scales out.** Workers scale horizontally (Celery/Kubernetes executors) far beyond a single SSIS server.

### Honest trade-offs / costs

- **You must write code.** There is no drag-and-drop designer. This is the biggest adjustment for a classic SSIS shop.
- **It's an orchestrator, not a transformer.** Airflow won't move your data for you — you still have to build the transformation logic (SQL/Pandas/Polars). Don't try to shovel gigabytes of data *through* an Airflow worker's RAM the way SSIS streamed through buffers; that's an anti-pattern.
- **Operational overhead.** You (or a managed service) run a scheduler, a metadata database, a web server, and workers. SSIS "just" needed SQL Server. Managed offerings (MWAA/Composer/Astronomer) remove most of this pain.
- **Not for sub-second / streaming.** Airflow is a *batch scheduler* (minimum practical granularity ~minutes). For real-time streaming you'd use Kafka/Flink/Spark Streaming, not Airflow.
- **Learning curve.** Concepts like idempotency, the execution/data-interval model, XComs, and executors take a few weeks to click.

### The version note (important in 2025+)

- Use a modern Airflow. **Airflow 2.x** introduced the **TaskFlow API** (the clean `@task` decorator style used throughout this doc). **Airflow 3.0** (released April 2025) is the current major line — it brings DAG versioning, a new React UI, a stricter task-execution model (workers no longer talk directly to the metadata DB), scheduler-managed backfills, and **Assets** (data-aware scheduling, formerly called "Datasets").
- A couple of naming changes you'll hit: the DAG argument is now `schedule=` (older `schedule_interval=` is deprecated), and time references use `logical_date` / **data intervals** rather than the old `execution_date`.
- In Airflow 3 the modern import path is `from airflow.sdk import dag, task`; in Airflow 2 it's `from airflow.decorators import dag, task`. The examples below note both.

## Core Airflow building blocks

A quick glossary before the code:

- **DAG** — the pipeline definition (a Python file). Has an ID, a `schedule`, a `start_date`, and default arguments.
- **Task** — one node in the DAG. Created either from the **TaskFlow API** (`@task` on a Python function) or by instantiating an **Operator**.
- **Operator** — a pre-built task template. Common ones:
  - `PythonOperator` / `@task` — run arbitrary Python (your Pandas/Polars code).
  - `SQLExecuteQueryOperator` — run SQL against any connection (the modern generic replacement for `MsSqlOperator`, `PostgresOperator`, etc.).
  - `BashOperator` — run a shell command.
  - `EmptyOperator` — a no-op placeholder (handy as a "start"/"join" node, like an SSIS sequence anchor).
  - **Sensors** (`@task.sensor`, `FileSensor`, `SqlSensor`…) — *wait* for a condition (a file to land, a row to appear). This replaces SSIS "For Loop that polls" patterns.
- **Hook** — a reusable client for an external system (e.g. `MsSqlHook`, `PostgresHook`, `S3Hook`). This is how you talk to databases from Python code. A Hook uses a **Connection** for its credentials.
- **Connection** — stored, encrypted credentials/endpoint, referenced by a `conn_id`. The direct equivalent of an SSIS Connection Manager.
- **Variable** — a global key/value setting (like a project parameter / environment config).
- **XCom** ("cross-communication") — the mechanism for one task to pass a *small* value to another (an ID, a row count, a file path). **Not** for passing large datasets — land those in a database or object store and pass the *pointer*.
- **Provider** — an installable package that bundles operators/hooks for a system (e.g. `apache-airflow-providers-microsoft-mssql` for SQL Server, `...-common-sql`, `...-amazon`, `...-snowflake`).
- **`schedule`** — when the DAG runs. Accepts cron (`"0 6 * * *"`), presets (`"@daily"`, `"@hourly"`), a `timedelta`, or **Assets** for data-driven triggering.
- **Trigger rule** — how a task decides to run based on its parents' states. Default `all_success` (all green). Others: `all_failed`, `one_failed`, `all_done`, `none_failed_min_one_success`. This is how you model SSIS red/green precedence constraints and "cleanup always runs" logic.

## How a migration actually works

A pragmatic, low-risk path (details in the final section):

1. **Inventory** your packages: sources, destinations, and — critically — separate the *orchestration* logic (Control Flow) from the *transformation* logic (Data Flow).
2. **Re-platform the orchestration** as DAGs first. Even before rewriting transformations, an Airflow DAG can just call your existing stored procedures or even trigger the legacy SSIS packages during a transition period.
3. **Push transformations down to SQL** wherever the Data Flow was really just "SELECT … JOIN … WHERE … GROUP BY into a table." Most SSIS Data Flows are secretly a SQL query.
4. **Rebuild genuinely procedural logic in Pandas/Polars** (row-by-row scripting, API enrichment, complex SCD handling, file parsing).
5. **Make every task idempotent** (safe to rerun) — this replaces SSIS checkpoints and is the single most important habit to adopt.
6. **Test, run in parallel, reconcile** outputs against SSIS, then cut over.

---

## Example 1 — A simple SSIS package in Airflow

### The SSIS package we're replacing

A classic daily load:

- **Control Flow:** one **Execute SQL Task** ("truncate staging") → one **Data Flow Task**.
- **Data Flow:** **OLE DB Source** (query yesterday's orders from an OLTP DB) → **Derived Column** (add a `load_date`, compute `net_amount`) → **Lookup** (enrich with a `customer_region` from a dimension) → **OLE DB Destination** (insert into a reporting warehouse table).
- **Schedule:** SQL Agent job, daily at 6 AM.

### Approach A — SQL push-down (the recommended default)

If the source and target are reachable by the same database (or via a linked/staged copy), the entire Data Flow is really just SQL. Airflow orchestrates it. This is the closest thing to "SSIS but as code," and it reuses your T-SQL skills.

```python
# dags/daily_orders_load.py
from datetime import datetime
from airflow.sdk import dag, task            # Airflow 3.x  (use airflow.decorators in 2.x)
from airflow.providers.common.sql.operators.sql import SQLExecuteQueryOperator

MSSQL_CONN_ID = "reporting_dwh"   # <-- an Airflow Connection, = an SSIS Connection Manager

@dag(
    dag_id="daily_orders_load",
    schedule="0 6 * * *",             # daily at 06:00 — replaces the SQL Agent schedule
    start_date=datetime(2026, 1, 1),
    catchup=False,                    # don't backfill history on first deploy
    default_args={"retries": 2},      # automatic retry, like a more capable checkpoint
    tags=["ssis-migration", "orders"],
)
def daily_orders_load():

    # Control Flow task #1: Execute SQL Task -> "truncate staging"
    truncate_staging = SQLExecuteQueryOperator(
        task_id="truncate_staging",
        conn_id=MSSQL_CONN_ID,
        sql="TRUNCATE TABLE staging.orders;",
    )

    # Control Flow task #2 (was the Data Flow): the whole Source->DerivedColumn->Lookup->Dest
    # becomes ONE set-based SQL statement running inside the database.
    load_orders = SQLExecuteQueryOperator(
        task_id="load_orders",
        conn_id=MSSQL_CONN_ID,
        sql="""
            INSERT INTO reporting.fact_orders
                (order_id, customer_id, customer_region, order_date,
                 gross_amount, discount, net_amount, load_date)
            SELECT
                o.order_id,
                o.customer_id,
                c.region                          AS customer_region,   -- was the Lookup
                o.order_date,
                o.gross_amount,
                o.discount,
                o.gross_amount - o.discount       AS net_amount,        -- was the Derived Column
                CAST('{{ ds }}' AS date)          AS load_date          -- {{ ds }} = the run date
            FROM oltp.orders           AS o
            JOIN dim.customer          AS c ON c.customer_id = o.customer_id
            WHERE o.order_date = '{{ ds }}';      -- {{ ds }} templates the run's logical date
        """,
    )

    truncate_staging >> load_orders    # the precedence constraint (green arrow)

daily_orders_load()
```

Notes for the SSIS reader:

- `{{ ds }}` is **Jinja templating** — Airflow substitutes the DAG run's date (`YYYY-MM-DD`) at runtime. It's the equivalent of an SSIS expression/variable like `@[System::StartTime]`, but for the *scheduled* interval, which makes backfills and reruns deterministic.
- `truncate_staging >> load_orders` is the whole Control Flow. The `>>` operator *is* the precedence-constraint arrow.
- `retries: 2` means each task auto-retries. Combined with idempotent SQL (truncate+insert), a failed run is safely rerun with one click — this replaces SSIS checkpoints.

### Approach B — ETL in Python (the closest analog to a Data Flow)

When the logic is more procedural, or source and target are *different* systems, pull the data into a DataFrame, transform, and write it back. This mirrors an SSIS Data Flow one component at a time.

```python
# dags/daily_orders_load_python.py
from datetime import datetime
import pandas as pd
from airflow.sdk import dag, task
from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook

SRC_CONN = "oltp_prod"          # OLE DB Source connection manager
DST_CONN = "reporting_dwh"      # OLE DB Destination connection manager

@dag(
    dag_id="daily_orders_load_python",
    schedule="0 6 * * *",
    start_date=datetime(2026, 1, 1),
    catchup=False,
    default_args={"retries": 2},
    tags=["ssis-migration", "orders"],
)
def daily_orders_load_python():

    @task
    def extract(logical_date=None) -> str:
        """OLE DB Source: pull yesterday's orders + the customer dimension."""
        src = MsSqlHook(mssql_conn_id=SRC_CONN)
        run_date = logical_date.strftime("%Y-%m-%d")

        orders = src.get_pandas_df(
            sql="SELECT order_id, customer_id, order_date, gross_amount, discount "
                "FROM oltp.orders WHERE order_date = %s",
            parameters=[run_date],
        )
        customers = src.get_pandas_df(
            "SELECT customer_id, region FROM dim.customer"
        )

        # Land intermediate data to disk and pass only the *path* via XCom
        # (never push a big DataFrame through XCom).
        orders.to_parquet(f"/tmp/orders_{run_date}.parquet")
        customers.to_parquet(f"/tmp/customers_{run_date}.parquet")
        return run_date

    @task
    def transform(run_date: str) -> str:
        """Derived Column + Lookup, done in Pandas."""
        orders = pd.read_parquet(f"/tmp/orders_{run_date}.parquet")
        customers = pd.read_parquet(f"/tmp/customers_{run_date}.parquet")

        # Derived Column: net_amount and load_date
        orders["net_amount"] = orders["gross_amount"] - orders["discount"]
        orders["load_date"] = run_date

        # Lookup: enrich with customer_region (a left join keeps unmatched rows,
        # like a Lookup set to "redirect/ignore no-match" rather than "fail").
        enriched = orders.merge(
            customers.rename(columns={"region": "customer_region"}),
            on="customer_id",
            how="left",
        )
        enriched.to_parquet(f"/tmp/orders_enriched_{run_date}.parquet")
        return run_date

    @task
    def load(run_date: str):
        """OLE DB Destination: truncate staging + bulk insert."""
        df = pd.read_parquet(f"/tmp/orders_enriched_{run_date}.parquet")
        dst = MsSqlHook(mssql_conn_id=DST_CONN)

        # Idempotent: delete this date's rows first, then insert (safe to rerun).
        dst.run("DELETE FROM reporting.fact_orders WHERE load_date = %s",
                parameters=[run_date])
        # SQLAlchemy engine from the Hook lets Pandas write the whole frame.
        engine = dst.get_sqlalchemy_engine()
        df.to_sql("fact_orders", engine, schema="reporting",
                  if_exists="append", index=False, chunksize=10_000)

    # TaskFlow wires the dependencies automatically from the function calls:
    load(transform(extract()))

daily_orders_load_python()
```

Notes:

- The three `@task` functions are the Data Flow, split into **Extract → Transform → Load** stages. Airflow infers the dependency graph from how you *call* them (`load(transform(extract()))`).
- **Rule of thumb:** pass *pointers* (a file path, a date, a table name) through XComs — never the data itself. Big data goes to disk / object storage / a database between tasks.
- Idempotency: `DELETE … WHERE load_date = … ; append` makes a rerun produce the same result as a first run. Adopt this everywhere.

---

## Example 2 — Control flow, containers, branching & loops

Now something that shows off what Airflow does better than SSIS: a package with a **Foreach Loop Container**, a **conditional branch**, a grouped set of steps, and "always-runs" cleanup with email-on-failure.

### The SSIS package we're replacing

- A **Foreach Loop Container** iterating over CSV files dropped in a folder, importing each into a staging table.
- A **Script/Expression** that checks a row count and **branches**: if there are new rows, run the downstream processing; otherwise skip it.
- A **Sequence Container** grouping "validate → aggregate → publish."
- **Event Handlers**: send an email on any error; a **cleanup** step (archive files) that must run whether the package succeeded or failed.

### The Airflow DAG

```python
# dags/file_ingest_pipeline.py
from datetime import datetime
from pathlib import Path
from airflow.sdk import dag, task, Label
from airflow.providers.standard.operators.empty import EmptyOperator
from airflow.utils.trigger_rule import TriggerRule

INBOX = Path("/data/inbox")
ARCHIVE = Path("/data/archive")

def notify_failure(context):
    """on_failure_callback == an SSIS OnError event handler."""
    ti = context["task_instance"]
    # send_email(...) / Slack / PagerDuty — hook up your alerting here
    print(f"ALERT: task {ti.task_id} failed for run {context['run_id']}")

@dag(
    dag_id="file_ingest_pipeline",
    schedule="@hourly",
    start_date=datetime(2026, 1, 1),
    catchup=False,
    default_args={"retries": 1, "on_failure_callback": notify_failure},
    tags=["ssis-migration", "files"],
)
def file_ingest_pipeline():

    start = EmptyOperator(task_id="start")

    @task
    def list_files() -> list[str]:
        """Discovery step for the Foreach loop."""
        return [str(p) for p in INBOX.glob("*.csv")]

    @task
    def import_file(path: str) -> int:
        """Body of the Foreach Loop Container — runs ONCE PER FILE, in parallel.
        Returns the number of rows imported from this file."""
        import pandas as pd
        from airflow.providers.microsoft.mssql.hooks.mssql import MsSqlHook
        df = pd.read_csv(path)
        MsSqlHook("staging_db").get_sqlalchemy_engine()  # ... df.to_sql(...) here
        return len(df)

    # DYNAMIC TASK MAPPING: this single line replaces the Foreach Loop Container.
    # Airflow fans out one parallel `import_file` task per discovered file.
    row_counts = import_file.expand(path=list_files())

    @task.branch
    def has_new_rows(counts: list[int]) -> str:
        """Conditional Split / precedence-constraint EXPRESSION as a branch.
        Return the task_id of the path to take."""
        total = sum(counts)
        return "process_group.validate" if total > 0 else "skip_processing"

    branch = has_new_rows(row_counts)
    skip = EmptyOperator(task_id="skip_processing")

    # Sequence Container == a TaskGroup (visual grouping of related steps).
    @task
    def validate(): ...
    @task
    def aggregate(): ...
    @task
    def publish(): ...

    from airflow.sdk import TaskGroup   # (airflow.utils.task_group in 2.x)
    with TaskGroup(group_id="process_group") as process_group:
        validate() >> aggregate() >> publish()

    # Cleanup runs regardless of success/failure/skip == an SSIS OnPostExecute /
    # "always" handler, achieved with a trigger rule.
    @task(trigger_rule=TriggerRule.ALL_DONE)
    def archive_files(paths: list[str]):
        for p in paths:
            Path(p).rename(ARCHIVE / Path(p).name)

    files = list_files()
    start >> files
    # Wire branch outcomes (the two arrows out of the conditional):
    branch >> Label("has data") >> process_group
    branch >> Label("empty") >> skip
    # Archive after either branch completes, no matter the outcome:
    [process_group, skip] >> archive_files(files)

file_ingest_pipeline()
```

What this demonstrates vs. SSIS:

| SSIS feature here | How Airflow did it |
|---|---|
| Foreach Loop Container over files | **`import_file.expand(...)`** — dynamic task mapping. One *parallel*, independently-retriable task per file (SSIS ran them sequentially inside one container). |
| Precedence constraint with an **expression** (row count > 0) | **`@task.branch`** returning the next task's id (`BranchPythonOperator`). |
| Sequence Container | **`TaskGroup`** — grouping + a collapsible box in the UI. |
| OnError event handler + email | **`on_failure_callback`** in `default_args`. |
| "Cleanup always runs" | **`trigger_rule=ALL_DONE`** — runs after parents finish regardless of state. |
| Red/green arrows | Trigger rules (`all_success`, `all_failed`, `one_failed`, `all_done`, …). |

The dynamic-mapping line is the headline: expressing "do this once per file, in parallel, each retriable on its own" took *one line*. That pattern is genuinely painful in SSIS.

---

# Part 2 — Pandas & Polars

When a transformation is too procedural for SQL push-down, you'll do it in Python with a **DataFrame** library. A DataFrame is an in-memory table (rows and typed columns) — think of it as "the data flowing through an SSIS Data Flow, but that you can inspect and manipulate as a variable." The two libraries you'll use are **Pandas** and **Polars**.

## Pandas overview

**Pandas** is the original, ubiquitous Python DataFrame library (built on NumPy). It's been the backbone of Python data work for over a decade.

- **What it is:** an in-memory table with labeled columns (`DataFrame`) and a labeled 1-D array type (`Series`). Rich API for filtering, joining (`merge`), grouping (`groupby`), reshaping (`pivot`/`melt`), time-series, and I/O for CSV/Excel/Parquet/SQL/JSON.
- **Why SSIS folks like it:** enormous ecosystem, endless tutorials/Stack Overflow answers, reads Excel and flat files effortlessly, integrates with everything (Airflow hooks return Pandas frames via `get_pandas_df`, and virtually every Python data tool speaks Pandas).
- **The catch:**
  - **Single-threaded** for most operations and **memory-hungry** — a rough rule is it needs several times the dataset's on-disk size in RAM. It's happiest below a few GB / low tens of millions of rows on a single machine.
  - **Eager** execution: every step runs immediately, so it won't automatically optimize a chain of operations.
  - Historically string/NULL handling was awkward (NumPy object dtype, `NaN` for missing). Modern Pandas (2.x) added an optional **Arrow** backend that improves this a lot.
- **Bottom line:** the safe default for small-to-medium data, prototyping, Excel-heavy work, and anywhere the ecosystem matters more than raw speed.

## Polars overview

**Polars** is a newer, high-performance DataFrame library written in **Rust**, built on Apache **Arrow** memory. It's designed for speed and larger-than-Pandas datasets on a single machine.

- **What it is:** a DataFrame library with a similar conceptual model to Pandas but a different, more consistent **expression-based API**.
- **Why it's fast:**
  - **Multi-threaded by default** — uses all your CPU cores automatically (Pandas uses one).
  - **Lazy execution mode** (`pl.scan_csv(...)`, `.lazy()`): you describe the whole pipeline and Polars' **query optimizer** rearranges it (predicate/projection pushdown, etc.) before running — much like a SQL engine planning a query. This is the single biggest reason it beats Pandas on large jobs.
  - **Arrow-native** columnar memory: efficient, with proper typed nulls (no `NaN`-means-missing confusion) and excellent string handling.
  - **Streaming/out-of-core**: can process datasets larger than RAM in chunks.
- **Why SSIS folks should care:** for the big nightly loads that used to justify SSIS's buffered engine, Polars often gives you SSIS-like (or better) throughput from a single Python process — frequently **5–30× faster than Pandas** and using a fraction of the memory.
- **The catch:** younger ecosystem (fewer third-party integrations, though growing fast), a different API you have to learn, and slightly less Stack Overflow coverage. Some Airflow hooks return Pandas by default (you convert with `pl.from_pandas(...)` or read via `pl.read_database`).
- **Bottom line:** the strong choice for performance-critical or large batch transformations — exactly the workloads that pushed you to SSIS in the first place.

## Pandas vs Polars — which to choose

| Dimension | Pandas | Polars |
|---|---|---|
| **Maturity / ecosystem** | Huge, battle-tested | Younger but growing fast |
| **Speed** | Single-threaded, slower | Multi-threaded, very fast |
| **Memory use** | High | Low |
| **Lazy/optimized execution** | No (eager only) | Yes (`LazyFrame` + optimizer) |
| **Larger-than-RAM data** | No (needs Dask/Spark) | Yes (streaming) |
| **API feel** | Flexible, sometimes inconsistent | Consistent, expression-based |
| **NULL / string handling** | Historically weak (`NaN`); better with Arrow backend | Clean, Arrow-native |
| **Excel / niche I/O & 3rd-party libs** | Best-in-class | Improving |
| **Best for** | Small–medium data, prototyping, Excel, ecosystem | Large data, performance, new code |

**Practical guidance for your migration:**

- **Default to SQL push-down** for anything set-based (that's most Data Flows). Don't move data into Python unless you have a reason.
- When you *do* go to Python: **start with Pandas** for quick/small conversions and where you need the ecosystem (Excel, a specific library). **Reach for Polars** when data volume or runtime hurts, or for brand-new large pipelines.
- You don't have to pick globally — choose per task. They interoperate (`pl.from_pandas`, `df.to_pandas`) since both sit on Arrow.

## SSIS Data Flow components → Pandas/Polars

This is the cheat-sheet you'll use most. It maps each SSIS Data Flow transformation to its Pandas and Polars equivalent.

| SSIS Data Flow component | Pandas | Polars |
|---|---|---|
| **OLE DB / ADO.NET Source** | `hook.get_pandas_df(sql)` / `pd.read_sql` | `pl.read_database(sql, conn)` |
| **Flat File Source** | `pd.read_csv(path)` | `pl.read_csv(path)` / `pl.scan_csv` (lazy) |
| **Excel Source** | `pd.read_excel(path)` | `pl.read_excel(path)` |
| **OLE DB Destination** | `df.to_sql(table, engine, if_exists="append")` | `df.write_database(table, conn)` |
| **Flat File Destination** | `df.to_csv(path, index=False)` | `df.write_csv(path)` |
| **Derived Column** | `df["c"] = expr` / `df.assign(...)` | `df.with_columns((expr).alias("c"))` |
| **Data Conversion / Cast** | `df["c"].astype("int64")` | `df.with_columns(pl.col("c").cast(pl.Int64))` |
| **Conditional Split** | boolean mask: `df[df.x > 0]` | `df.filter(pl.col("x") > 0)` |
| **Lookup** | `df.merge(dim, on="k", how="left")` | `df.join(dim, on="k", how="left")` |
| **Merge Join** | `df1.merge(df2, on="k", how="inner/outer")` | `df1.join(df2, on="k", how="inner/outer")` |
| **Union All** | `pd.concat([df1, df2])` | `pl.concat([df1, df2])` |
| **Multicast** | just reuse the DataFrame variable | same |
| **Aggregate** | `df.groupby("k").agg(...)` | `df.group_by("k").agg(...)` |
| **Sort** | `df.sort_values(["a","b"])` | `df.sort(["a","b"])` |
| **Sort → "remove duplicates"** | `df.drop_duplicates(subset=[...])` | `df.unique(subset=[...])` |
| **Row Count** | `len(df)` | `df.height` / `len(df)` |
| **Percentage/Row Sampling** | `df.sample(frac=0.1)` | `df.sample(fraction=0.1)` |
| **Pivot** | `df.pivot_table(...)` | `df.pivot(...)` |
| **Unpivot** | `df.melt(...)` | `df.unpivot(...)` |
| **Character Map / string ops** | `df["s"].str.upper()` | `pl.col("s").str.to_uppercase()` |
| **Derived Column with conditionals** | `np.where(cond, a, b)` | `pl.when(cond).then(a).otherwise(b)` |
| **Script Component (arbitrary logic)** | any Python / `df.apply` (last resort) | `map_elements` / expressions (last resort) |
| **OLE DB Command (per-row SQL)** | ⚠️ anti-pattern — vectorize instead | ⚠️ anti-pattern — vectorize instead |
| **Slowly Changing Dimension** | `merge` + masks (shown below) | `join` + `when/then` (shown below) |

> **Key habit change:** SSIS trained you to think row-by-row (buffers streaming one record at a time). Pandas and Polars want you to think **set-based / vectorized** — operate on whole columns at once. Avoid `df.apply(...)` row loops the same way you'd avoid a cursor in T-SQL; they're the slow path. `np.where` / `pl.when().then()` replace most "Script Component" and per-row logic.

## Worked example — a full SSIS Data Flow rebuilt

Here's a realistic Data Flow rebuilt in **both** libraries side by side, so you can feel the difference. The SSIS Data Flow being replaced:

1. **Flat File Source** — read `sales.csv`.
2. **Data Conversion** — cast `amount` to decimal, `order_date` to date.
3. **Derived Column** — `net = amount - discount`; `channel = "online" if web_flag else "store"`.
4. **Lookup** — join to `customers.csv` to add `region` (redirect no-match rows to a reject file).
5. **Conditional Split** — keep only `net > 0`; send `net <= 0` rows to an audit table.
6. **Aggregate** — total `net` and order count by `region` + `channel`.
7. **OLE DB Destination** — write the summary to `reporting.sales_summary`.

### Pandas version

```python
import numpy as np
import pandas as pd

# 1. Flat File Source
sales = pd.read_csv("sales.csv")
customers = pd.read_csv("customers.csv")

# 2. Data Conversion (Cast)
sales["amount"] = sales["amount"].astype("float64")
sales["order_date"] = pd.to_datetime(sales["order_date"])

# 3. Derived Column
sales["net"] = sales["amount"] - sales["discount"]
sales["channel"] = np.where(sales["web_flag"] == 1, "online", "store")

# 4. Lookup (left join keeps unmatched rows so we can split off rejects)
merged = sales.merge(customers[["customer_id", "region"]],
                     on="customer_id", how="left")
rejects = merged[merged["region"].isna()]          # no-match -> reject path
rejects.to_csv("rejects.csv", index=False)
matched = merged[merged["region"].notna()]

# 5. Conditional Split
good  = matched[matched["net"] > 0]
audit = matched[matched["net"] <= 0]               # -> audit path

# 6. Aggregate
summary = (good.groupby(["region", "channel"], as_index=False)
                .agg(total_net=("net", "sum"),
                     order_count=("order_id", "count")))

# 7. OLE DB Destination
# summary.to_sql("sales_summary", engine, schema="reporting",
#                if_exists="append", index=False)
print(summary)
```

### Polars version (eager)

```python
import polars as pl

# 1. Flat File Source
sales = pl.read_csv("sales.csv")
customers = pl.read_csv("customers.csv")

# 2 + 3. Cast + Derived Columns, all in one expression block
sales = sales.with_columns(
    pl.col("amount").cast(pl.Float64),
    pl.col("order_date").str.to_date(),
    (pl.col("amount") - pl.col("discount")).alias("net"),
    pl.when(pl.col("web_flag") == 1).then(pl.lit("online"))
      .otherwise(pl.lit("store")).alias("channel"),
)

# 4. Lookup
merged = sales.join(customers.select(["customer_id", "region"]),
                    on="customer_id", how="left")
merged.filter(pl.col("region").is_null()).write_csv("rejects.csv")   # reject path
matched = merged.filter(pl.col("region").is_not_null())

# 5. Conditional Split
good  = matched.filter(pl.col("net") > 0)
audit = matched.filter(pl.col("net") <= 0)

# 6. Aggregate
summary = (good.group_by(["region", "channel"])
                .agg(pl.col("net").sum().alias("total_net"),
                     pl.col("order_id").count().alias("order_count")))

# 7. Destination
# summary.write_database("reporting.sales_summary", connection=conn_uri)
print(summary)
```

### Polars version (lazy — the fast one for big files)

For large inputs, use the **lazy** API so Polars optimizes the whole plan and streams the data — this is where it dramatically outperforms both Pandas and (often) an SSIS Data Flow:

```python
import polars as pl

summary = (
    pl.scan_csv("sales.csv")                       # lazy: nothing runs yet
      .with_columns(
          (pl.col("amount") - pl.col("discount")).alias("net"),
          pl.when(pl.col("web_flag") == 1).then(pl.lit("online"))
            .otherwise(pl.lit("store")).alias("channel"),
      )
      .join(pl.scan_csv("customers.csv").select(["customer_id", "region"]),
            on="customer_id", how="inner")          # only matched rows
      .filter(pl.col("net") > 0)
      .group_by(["region", "channel"])
      .agg(pl.col("net").sum().alias("total_net"),
           pl.col("order_id").count().alias("order_count"))
      .collect(streaming=True)                       # NOW it runs, optimized + streamed
)
```

The lazy plan lets Polars push the filter and column-selection down to the CSV read (reading less data), fuse operations, and run across all cores — the kind of optimization the SSIS engine did internally, but here it's explicit, portable, and testable.

### The same transformation as pure SQL push-down (for comparison)

Remember Approach A: if the data is already in a database, none of the above Python is needed — the whole Data Flow is one statement, and Airflow just runs it:

```sql
INSERT INTO reporting.sales_summary (region, channel, total_net, order_count)
SELECT c.region,
       CASE WHEN s.web_flag = 1 THEN 'online' ELSE 'store' END AS channel,
       SUM(s.amount - s.discount) AS total_net,
       COUNT(*)                   AS order_count
FROM   staging.sales   AS s
JOIN   dim.customer    AS c ON c.customer_id = s.customer_id
WHERE (s.amount - s.discount) > 0
GROUP BY c.region,
         CASE WHEN s.web_flag = 1 THEN 'online' ELSE 'store' END;
```

Three ways to skin the same cat. Choosing between them is the core skill of the migration: **SQL when it's set-based and the data is in a DB; Polars when it's big and Python-bound; Pandas when it's small or ecosystem-bound.**

### Bonus — a Slowly Changing Dimension (Type 1) in Pandas

SCDs were a whole SSIS wizard. The Type-1 "overwrite" pattern (update existing, insert new) is a simple merge:

```python
# existing dimension (from the DB) vs. today's incoming records
existing = dst.get_pandas_df("SELECT customer_id, name, region FROM dim.customer")
incoming = pd.read_csv("customers_today.csv")

merged = incoming.merge(existing, on="customer_id", how="left",
                        suffixes=("", "_old"), indicator=True)

new_rows     = merged[merged["_merge"] == "left_only"]       # INSERT these
changed_rows = merged[(merged["_merge"] == "both") &
                      ((merged["name"]   != merged["name_old"]) |
                       (merged["region"] != merged["region_old"]))]  # UPDATE these
# then bulk-insert new_rows and issue UPDATEs (or MERGE) for changed_rows
```

For **Type 2** (keep history), you'd add `valid_from` / `valid_to` / `is_current` columns and close out the old row before inserting the new version — the same logic the SSIS SCD wizard generated, but now it's readable, diffable code you control.

---

## Putting it together — a recommended migration path

1. **Stand up Airflow the easy way.** Use a managed service to skip the ops burden: **AWS MWAA**, **Google Cloud Composer**, or **Astronomer** (the last has great local-dev tooling via the `astro` CLI). For local development, `astro dev` or the official Docker Compose spins up Airflow in minutes.
2. **Set up Connections** in Airflow mirroring your SSIS Connection Managers (one `conn_id` per source/target). Store secrets in a secrets backend, not in code.
3. **Migrate orchestration first.** Turn each package's Control Flow into a DAG. During transition, tasks can even call your *existing* stored procs or trigger legacy SSIS packages — decouple the schedule from the logic first.
4. **Classify every Data Flow:**
   - *Set-based & data already in a DB* → **SQL push-down** (`SQLExecuteQueryOperator`). This will be the majority.
   - *Procedural / cross-system / API / file-parsing* → **Pandas or Polars** task.
   - *Large & performance-critical* → **Polars (lazy/streaming)** or a real engine (Spark/dbt/warehouse).
5. **Make every task idempotent** (delete-then-insert by partition/date, `MERGE`, or truncate+reload). This is your replacement for SSIS checkpoints and makes reruns/backfills safe.
6. **Test it.** Unit-test transformation functions with `pytest`; validate DAG integrity in CI. This is a capability you simply didn't have in SSIS — use it.
7. **Run in parallel & reconcile.** Run Airflow beside SSIS for a cycle or two, compare row counts and checksums, then cut over package by package.
8. **Adopt the culture:** everything in Git, pull requests for pipeline changes, CI/CD to deploy DAGs, alerting via callbacks. This is where the real long-term payoff over SSIS comes from.

### A few "gotchas" to save you pain

- **Don't stream big data through Airflow workers.** XComs are for small values (IDs, counts, paths). Land large datasets in a DB/object store and pass the pointer. This is the #1 mistake SSIS migrants make (treating a task like an SSIS buffer).
- **Idempotency is non-negotiable.** Assume any task can and will be retried. Design writes so a rerun is harmless.
- **Understand the schedule/data-interval model early.** A DAG "run for 2026-06-30" typically *executes* just after that interval ends and processes that day's data via `{{ ds }}`/`logical_date`. This makes backfills deterministic — but it surprises SSIS folks who expect "runs = right now."
- **Vectorize; don't loop rows.** `df.apply()` / `iterrows()` in Pandas are the equivalent of a T-SQL cursor. Use column expressions, `np.where`, `merge`, `groupby` (Pandas) or `with_columns`/`when-then`/`join`/`group_by` (Polars).
- **Prefer push-down.** The database is almost always faster at joins/aggregations than pulling data into a Python worker. Only pull into Pandas/Polars when you have a real reason.

---

*Suggested next step: pick one small, self-contained SSIS package, rebuild it as a DAG using Approach A (SQL push-down), and run it in parallel with the original for a week. You'll learn 80% of Airflow from that one exercise.*
