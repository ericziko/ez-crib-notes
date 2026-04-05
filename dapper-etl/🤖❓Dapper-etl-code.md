---
uid: fa672cf7-de85-4a00-9855-4b5432d389a4
created: '2026-04-05T18:04:00+18:04'
modified: '2026-04-05T20:04:45+20:04'
title: 🤖❓Dapper-etl-code
---

# 🤖❓Dapper-etl-code

## Linked Documentation 

```folder-overview
id: 7c9d2b25-7cad-4d35-93d8-125db1aee481
folderPath: ""
title: 📂🔎 {{folderName}} overview
showTitle: false
depth: 1
includeTypes:
  - folder
  - markdown
style: explorer
disableFileTag: false
sortBy: name
sortByAsc: true
showEmptyFolders: false
onlyIncludeSubfolders: false
storeFolderCondition: true
showFolderNotes: true
disableCollapseIcon: true
alwaysCollapse: true
autoSync: true
allowDragAndDrop: false
hideLinkList: false
hideFolderOverview: true
useActualLinks: true
fmtpIntegration: false
titleSize: 3
isInCallout: false
useWikilinks: false

```

<span class="fv-link-list-start" id="7c9d2b25-7cad-4d35-93d8-125db1aee481"></span>
- [🤖❓Dapper-etl-code - linked-docs](<🤖❓Dapper-etl-code - linked-docs.md>)
- [ARCHITECTURE_DECISION](<ARCHITECTURE_DECISION.md>)
- [ETL-Architecture-Design](<ETL-Architecture-Design.md>)
- [FAQ_AND_EXAMPLES](<FAQ_AND_EXAMPLES.md>)
- [QUICK_START](<QUICK_START.md>)
- [SCHEMA_INSPECTOR_GUIDE](<SCHEMA_INSPECTOR_GUIDE.md>)
<span class="fv-link-list-end" id="7c9d2b25-7cad-4d35-93d8-125db1aee481"></span>

## Design me some modular ETL dapper code behind a testable interface

### PseudoCode 
I need to design some code `c#` that does the following 

```
Pseudo code

EtlTaskHandlerConstructor
- Constructor Takes two dbConnection objects
	- SourceDB
	- TargetDB

EtlTaskHandler
	Begin Transaction
		CopyTable1
			Truncate TargetDB.Table1
			Copy all rows from SourceDB.Table1 -> TargetDB.Table1
	
		CopyTable1
			Truncate TargetDB.Table2
			Copy all rows from SourceDB.Table2 -> TargetDB.Table2
		
		Execute TargetDB.StoredProcedure1
	OnError
		RollBackTransaction	
```

### Spec
- The PseudoCode above will be invoked by a Mediatr request handler
- Assume that the tables all the same columns
- The data code will be using Dapper - copying data between two SQL Server instances
- I would like the code that the handler invokes behind an interface so that I can test the behavior with Moq
- Please design me a modulized reusable solution that makes proper use of interfaces and utility methods to make re-usable dapper wrapping  methods across similar Mediatr handlers
- This code is part of a project in which I am migrating ETL logic from from SSIS packages and the database code that the handler is calling will probably need to be re-used in other contexts with different
  parameters
- Please ask any questions you may have


## [\`Dapper.ETL.Library\` Sql Server integration test project](<🤖🏗️ `Dapper.ETL.Library` Sql Server integration test project.md>)