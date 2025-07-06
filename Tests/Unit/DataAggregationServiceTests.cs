using Xunit;
using Microsoft.Extensions.Logging;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Tests;
using Moq;
using idc.pefindo.pbk.Services.Interfaces;

namespace idc.pefindo.pbk.Tests.Unit;

public class DataAggregationServiceTests
{
    private readonly Mock<ILogger<DataAggregationService>> _mockLogger;
    private readonly DataAggregationService _service;

    public DataAggregationServiceTests()
    {
        _mockLogger = new Mock<ILogger<DataAggregationService>>();
        _service = new DataAggregationService(_mockLogger.Object);
    }

    [Fact]
    public async Task AggregateIndividualDataAsync_WithValidData_ShouldReturnCompleteResponse()
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        
        var searchResponse = new PefindoSearchResponse
        {
            Code = "01",
            Status = "success",
            InquiryId = 12345,
            Data = new List<PefindoSearchData>
            {
                new()
                {
                    IdPefindo = 9999999,
                    SimilarityScore = 95.5m,
                    NamaDebitur = "Test User Complete",
                    IdNo = "1234567890123456"
                }
            }
        };

        var reportResponse = new PefindoGetReportResponse
        {
            Code = "01",
            Status = "success",
            Report = new PefindoReportData
            {
                Debitur = new PefindoDebiturInfo
                {
                    IdPefindo = 9999999,
                    NamaDebitur = "Test User Complete",
                    JmlFasilitas = 3,
                    MaxCurrDpd = 0,
                    MaxOverdueLast12Months = 0,
                    JmlPlafon = 100000m,
                    TotalAngsuranAktif = 25000m
                },
                ScoreInfo = new PefindoScoreInfo
                {
                    Score = "750",
                    RiskGrade = "A",
                    RiskDesc = "Low Risk"
                }
            }
        };

        var processingContext = new ProcessingContext
        {
            EventId = "test-event-12345",
            ProcessingStartTime = DateTime.UtcNow
        };

        // Act
        var result = await _service.AggregateIndividualDataAsync(
            request, searchResponse, reportResponse, processingContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.CfLosAppNo, result.AppNo);
        Assert.Equal(request.IdNumber, result.IdNumber);
        Assert.Equal("9999999", result.PefindoId);
        Assert.Equal("750", result.Score);
        Assert.Equal("0", result.MaxOverdue);
        Assert.Equal("3", result.TotalFacilities);
        Assert.Equal("SUCCESS", result.Status);
    }

    [Fact]
    public async Task AggregateIndividualDataAsync_WithNullReportData_ShouldHandleGracefully()
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        var searchResponse = new PefindoSearchResponse { Data = new List<PefindoSearchData>() };
        var reportResponse = new PefindoGetReportResponse { Report = null };
        var processingContext = new ProcessingContext();

        // Act
        var result = await _service.AggregateIndividualDataAsync(
            request, searchResponse, reportResponse, processingContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("0", result.Score); // Default values when no report data
        Assert.Equal("0", result.MaxOverdue);
        Assert.Equal("SUCCESS", result.Status);
    }
}
