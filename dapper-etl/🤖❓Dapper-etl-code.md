# 🤖❓dapper-etl-code.md
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
