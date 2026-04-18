namespace Dapper.ETL.Orchestrator.Tests.Models;

using Dapper.ETL.Orchestrator.Models;
using Dapper.ETL.Orchestrator.Services;
using Xunit;

public class ModelsTests
{
    [Fact]
    public void Test_EtlResult_Properties_CanBeSetAndRead()
    {
        var result = new EtlResult { Success = true, RowsCopied = 42, Duration = 100, ErrorMessage = "err" };
        Assert.True(result.Success);
        Assert.Equal(42, result.RowsCopied);
        Assert.Equal(100, result.Duration);
        Assert.Equal("err", result.ErrorMessage);
    }

    [Fact]
    public void Test_EtlResult_DefaultValues_AreCorrect()
    {
        var result = new EtlResult();
        Assert.False(result.Success);
        Assert.Equal(0, result.RowsCopied);
        Assert.Equal(0, result.Duration);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Test_EtlRunInfo_Properties_CanBeSetAndRead()
    {
        var now = DateTime.UtcNow;
        var info = new EtlRunInfo { RunAt = now, DurationMs = 500, RowsCopied = 10, Success = true, ErrorMessage = null };
        Assert.Equal(now, info.RunAt);
        Assert.Equal(500, info.DurationMs);
        Assert.Equal(10, info.RowsCopied);
        Assert.True(info.Success);
        Assert.Null(info.ErrorMessage);
    }

    [Fact]
    public void Test_MetricsData_DefaultCapturedAt_IsRecentUtcNow()
    {
        var before = DateTime.UtcNow;
        var data = new MetricsData();
        var after = DateTime.UtcNow;
        Assert.InRange(data.CapturedAt, before, after);
    }

    [Fact]
    public void Test_MetricsData_Properties_CanBeSetAndRead()
    {
        var data = new MetricsData
        {
            TotalRowsCopied = 100,
            LastRunDurationMs = 200,
            ThroughputRowsPerSec = 50.0,
            ErrorCount = 1,
            ValidationsPassed = 3,
            ValidationsFailed = 0
        };
        Assert.Equal(100, data.TotalRowsCopied);
        Assert.Equal(200, data.LastRunDurationMs);
        Assert.Equal(50.0, data.ThroughputRowsPerSec);
        Assert.Equal(1, data.ErrorCount);
        Assert.Equal(3, data.ValidationsPassed);
        Assert.Equal(0, data.ValidationsFailed);
    }

    [Fact]
    public void Test_ValidationResult_Properties_CanBeSetAndRead()
    {
        var result = new ValidationResult
        {
            Success = false,
            TablesChecked = 3,
            TablesPassed = 2,
            TablesFailed = 1,
            ErrorMessage = "mismatch"
        };
        Assert.False(result.Success);
        Assert.Equal(3, result.TablesChecked);
        Assert.Equal(2, result.TablesPassed);
        Assert.Equal(1, result.TablesFailed);
        Assert.Equal("mismatch", result.ErrorMessage);
    }

    [Fact]
    public void Test_LogEntry_Record_ConstructsAndDeconstructs()
    {
        var ts = DateTime.UtcNow;
        var entry = new LogEntry(ts, "Info", "Hello {Name}", "{\"Name\":\"World\"}");
        Assert.Equal(ts, entry.TimeStamp);
        Assert.Equal("Info", entry.Level);
        Assert.Equal("Hello {Name}", entry.MessageTemplate);
        Assert.Equal("{\"Name\":\"World\"}", entry.Properties);
    }

    [Fact]
    public void Test_ValidationDetail_Record_ConstructsAndDeconstructs()
    {
        var detail = new ValidationDetail("Customer", 100, 100, 100.0, "OK");
        Assert.Equal("Customer", detail.TableName);
        Assert.Equal(100, detail.SourceCount);
        Assert.Equal(100, detail.TargetCount);
        Assert.Equal(100.0, detail.MatchPercent);
        Assert.Equal("OK", detail.Status);
    }
}
