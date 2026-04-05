-- Create databases
CREATE DATABASE TestDbSource;
CREATE DATABASE TestDbTarget;
CREATE DATABASE EtlLogs;
GO

-- Use TestDbSource
USE TestDbSource;
GO

-- Create SEQUENCE for customer ID (if needed)
CREATE SEQUENCE dbo.CustomerIdSequence START WITH 1 INCREMENT BY 1;

-- Create Customer table
CREATE TABLE dbo.Customer (
    CustomerId INT NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    EmailAddress NVARCHAR(255) NOT NULL
);

-- Use TestDbTarget
USE TestDbTarget;
GO

CREATE SEQUENCE dbo.CustomerEmailIdSequence START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE dbo.LoyaltyRewardIdSequence START WITH 1 INCREMENT BY 1;

CREATE TABLE dbo.CustomerCopy (
    CustomerId INT NOT NULL PRIMARY KEY,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    EmailAddress NVARCHAR(255) NOT NULL
);

CREATE TABLE dbo.CustomerEmailList (
    CustomerEmailId INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR dbo.CustomerEmailIdSequence),
    CustomerId INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    EmailAddress NVARCHAR(255) NOT NULL
);

CREATE TABLE dbo.CustomerLoyaltyRewards (
    LoyaltyRewardId INT NOT NULL PRIMARY KEY DEFAULT (NEXT VALUE FOR dbo.LoyaltyRewardIdSequence),
    CustomerId INT NOT NULL,
    LoyaltyRewordId INT NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL
);

-- Use EtlLogs
USE EtlLogs;
GO

-- Create Serilog log table (schema: Serilog.Sinks.MSSqlServer standard)
CREATE TABLE dbo.Logs (
    Id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    MessageTemplate NVARCHAR(MAX) NOT NULL,
    Level VARCHAR(128) NOT NULL,
    TimeStamp DATETIME2 NOT NULL,
    Exception NVARCHAR(MAX),
    LogEvent NVARCHAR(MAX) -- JSON-serialized properties
);

CREATE INDEX IX_Logs_TimeStamp ON dbo.Logs(TimeStamp DESC);
