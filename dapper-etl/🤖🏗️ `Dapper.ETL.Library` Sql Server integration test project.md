---
uid: f019c724-ea92-40be-b4b2-bcfb3c2d2431
title: 🤖🏗️ `Dapper.ETL.Library` Sql Server integration test project
created: 2026-04-05T20:04:00+20:04
modified: 2026-04-05T21:04:73+21:04
---

# 🤖🏗️ `Dapper.ETL.Library` Sql Server integration test project

## Summary
- I need you to add code  to the `Dapper.ETL.sln` to exercise and test the code written in the `Dapper.ETL.Library`
  using SQL Server

## Spec
I would like to create a full SQL Server integration test `Dapper.ETL.Library` using Docker containers.
- I am using [OrbStack · Fast, light, simple Docker & Linux](https://orbstack.dev/) - which I am running locally

### Aspire project
- I would like you to add Microsoft Aspire the `Dapper.ETL.sln`
- Please add OpenTelemetry the libraries where appropriate so that I can see stats show up on the aspire dashboard when I run the ETL process 

### ETL.Orchestrator
- I would like you create a .NET console application in charge of an ETL process between the
  two databases
- Uses Serilog to write logs to database
- Also have Serilog write logs to LoggingDatabase
- Also have Serilog write logs to Seq 
- Use Spectre console for the ETL.Orchestrator
	- It should expose the following commands
		- RunETL
		- SeedSourceCustomers(number of records to create)
	- Please suggest any other commands you think it should expose

#### ETL Transforms

```
- Begin Transaction
	- Truncate TargetDatabase.TestDbTarget.CustomerCopy
	- Copy SourceDatabase.TestDbTarget.Customer -> TargetDatabase.TestDbTarget.CustomerCopy (one to one mapping all columns match)
	- Copy SourceDatabase.TestDbTarget.Customer -> TargetDatabase.TestDbTarget.CustomerEmailList (Slight mapping)
	- Copy SourceDatabase.TestDbTarget.Customer -> TargetDatabase.TestDbTarget.CustomerLoyaltyRewards (Slight mapping)

```

### Databases
- I would you to create three  Separate SQL server containers to the project databases each running in a separate docker container through OrbStack

#### SourceDatabase
**Name:** TestDbSource

##### Tables
###### Customer
CustomerId
LoyaltyRewordId
FirstName
LastName
EmailAddress

#### TargetDatabase 
**Name:** TestDbTarget

##### Tables
###### CustomerCopy
CustomerId
LoyaltyRewordId
FirstName
LastName
EmailAddress

###### CustomerEmailList
CustomerEmailId
CustomerId
FirstName
LastName
EmailAddress

###### CustomerLoyaltyRewards
LoyaltyRewardId
CustomerId
LoyaltyRewordId
FirstName
LastName

### LoggingDatabase
Name: EtlLogs
Description: Please to keep logs 
