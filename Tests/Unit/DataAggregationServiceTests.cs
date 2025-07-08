using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Tests;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
        Assert.NotNull(result.CreatedDate);
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

    [Fact]
    public async Task AggregateIndividualDataAsync_WithEmptySearchData_ShouldHandleGracefully()
    {
        // Arrange
        var request = TestHelper.CreateValidIndividualRequest();
        var searchResponse = new PefindoSearchResponse
        {
            Code = "31",
            Status = "failed",
            Message = "Data tidak ditemukan",
            Data = new List<PefindoSearchData>()
        };
        var reportResponse = new PefindoGetReportResponse();
        var processingContext = new ProcessingContext();

        // Act
        var result = await _service.AggregateIndividualDataAsync(
            request, searchResponse, reportResponse, processingContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("N/A", result.PefindoId);
        Assert.Equal("SUCCESS", result.Status);
        Assert.Equal("Data tidak ditemukan", result.ResponseMessage);
    }

    [Fact]
    public async Task AggregateIndividualDataAsync_WithComplexReportData_ShouldMapAllFields()
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
                    NamaDebitur = "Test User",
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
                    NamaDebitur = "Test User",
                    JmlFasilitas = 5,
                    MaxCurrDpd = 30,
                    MaxOverdueLast12Months = 60,
                    JmlPlafon = 500000m,
                    TotalAngsuranAktif = 75000m,
                    BakiDebetNonAgunan = 250000m,
                    WoContract = 2,
                    WoAgunan = 1,
                    KualitasKreditTerburuk = "Kurang Lancar",
                    BulanKualitasTerburuk = "202301",
                    BakiDebetKualitasTerburuk = 100000m,
                    KualitasKreditTerakhir = "Lancar",
                    BulanKualitasKreditTerakhir = "202312"
                },
                ScoreInfo = new PefindoScoreInfo
                {
                    Score = "650",
                    RiskGrade = "B",
                    RiskDesc = "Medium Risk"
                }
            }
        };

        var processingContext = new ProcessingContext
        {
            EventId = "test-event-67890",
            ProcessingStartTime = DateTime.UtcNow
        };

        // Act
        var result = await _service.AggregateIndividualDataAsync(
            request, searchResponse, reportResponse, processingContext);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("650", result.Score);
        Assert.Equal("30", result.MaxOverdue);
        Assert.Equal("60", result.MaxOverdueLast12Months);
        Assert.Equal("5", result.TotalFacilities);
        Assert.Equal("75000", result.TotalAngsuranAktif);
        Assert.Equal("2", result.WoContract);
        Assert.Equal("1", result.WoAgunan);
        Assert.Equal("250000", result.BakiDebetNonAgunan);
        Assert.Equal("500000", result.Plafon);
        Assert.Equal("Kurang Lancar", result.KualitasKreditTerburuk);
        Assert.Equal("202301", result.BulanKualitasTerburuk);
        //Assert.Equal("100000", result.BakiDebetKualitasTerburuk);
        Assert.Equal("Lancar", result.KualitasKreditTerakhir);
        Assert.Equal("202312", result.BulanKualitasKreditTerakhir);
        //Assert.Contains("test-event-67890", result.DetailReport ?? "");
    }
}
