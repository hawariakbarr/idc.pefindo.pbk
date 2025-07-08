using FluentAssertions;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using idc.pefindo.pbk.Tests.Integration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace idc.pefindo.pbk.Tests.Unit
{
    public class IndividualProcessingServiceTests
    {
        private readonly Mock<ICycleDayValidationService> _mockCycleDayValidationService;
        private readonly Mock<ITokenManagerService> _mockTokenManagerService;
        private readonly Mock<IPefindoApiService> _mockPefindoApiService;
        private readonly Mock<ISimilarityValidationService> _mockSimilarityValidationService;
        private readonly Mock<IDataAggregationService> _mockDataAggregationService;
        private readonly Mock<IPbkDataRepository> _mockPbkDataRepository;
        private readonly Mock<IGlobalConfigRepository> _mockGlobalConfigRepository;
        private readonly Mock<ILogger<IndividualProcessingService>> _mockLogger;
        private readonly MockCorrelationService _mockCorrelationService;
        private readonly MockCorrelationLogger _mockCorrelationLogger;
        private readonly MockProcessStepLogger _mockProcessStepLogger;
        private readonly MockErrorLogger _mockErrorLogger;
        private readonly MockAuditLogger _mockAuditLogger;
        private readonly IndividualProcessingService _service;

        public IndividualProcessingServiceTests()
        {
            _mockCycleDayValidationService = new Mock<ICycleDayValidationService>();
            _mockTokenManagerService = new Mock<ITokenManagerService>();
            _mockPefindoApiService = new Mock<IPefindoApiService>();
            _mockSimilarityValidationService = new Mock<ISimilarityValidationService>();
            _mockDataAggregationService = new Mock<IDataAggregationService>();
            _mockPbkDataRepository = new Mock<IPbkDataRepository>();
            _mockGlobalConfigRepository = new Mock<IGlobalConfigRepository>();
            _mockLogger = new Mock<ILogger<IndividualProcessingService>>();
            
            // Create mock logging services
            _mockCorrelationService = new MockCorrelationService("test-corr-123", "test-req-456");
            _mockCorrelationLogger = new MockCorrelationLogger();
            _mockProcessStepLogger = new MockProcessStepLogger();
            _mockErrorLogger = new MockErrorLogger();
            _mockAuditLogger = new MockAuditLogger();

            _service = new IndividualProcessingService(
                _mockCycleDayValidationService.Object,
                _mockTokenManagerService.Object,
                _mockPefindoApiService.Object,
                _mockSimilarityValidationService.Object,
                _mockDataAggregationService.Object,
                _mockPbkDataRepository.Object,
                _mockGlobalConfigRepository.Object,
                _mockLogger.Object,
                _mockCorrelationService,
                _mockCorrelationLogger,
                _mockProcessStepLogger,
                _mockErrorLogger,
                _mockAuditLogger
            );
        }

        [Fact]
        public async Task ProcessIndividualRequestAsync_ShouldLogProcessStart_WhenCalled()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupSuccessfulValidation();
            SetupSuccessfulTokenRetrieval();
            SetupSuccessfulSearch();
            SetupSuccessfulSimilarityValidation();
            SetupSuccessfulReportGeneration();
            SetupSuccessfulDataAggregation();

            // Act
            var result = await _service.ProcessIndividualRequestAsync(request);

            // Assert
            var logEntries = _mockCorrelationLogger.GetAllLogEntries();
            logEntries.Should().HaveCount(1);
            logEntries[0].ProcessName.Should().Be("IndividualProcessing");
            logEntries[0].Status.Should().Be("Success");
            logEntries[0].CorrelationId.Should().Be("test-corr-123");
        }

        [Fact]
        public async Task ProcessIndividualRequestAsync_ShouldCompleteAllSteps_WhenSuccessful()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupSuccessfulValidation();
            SetupSuccessfulTokenRetrieval();
            SetupSuccessfulSearch();
            SetupSuccessfulSimilarityValidation();
            SetupSuccessfulReportGeneration();
            SetupSuccessfulDataAggregation();

            // Act
            var result = await _service.ProcessIndividualRequestAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Data.Should().NotBeNull();

            // Verify all logging steps were called
            var stepLogs = _mockProcessStepLogger.GetStepLogs();
            stepLogs.Should().HaveCountGreaterThan(0);
            
            var auditLogs = _mockAuditLogger.GetAuditLogs();
            auditLogs.Should().HaveCountGreaterThan(0);
            auditLogs.Should().Contain(a => a.Action == "PBKProcessingStarted");
            auditLogs.Should().Contain(a => a.Action == "PBKProcessingCompleted");
        }

        [Fact]
        public async Task ProcessIndividualRequestAsync_ShouldLogProcessFailure_WhenCycleDayValidationFails()
        {
            // Arrange
            var request = CreateValidRequest();
            _mockCycleDayValidationService
                .Setup(x => x.ValidateCycleDayAsync(It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act & Assert
            var act = async () => await _service.ProcessIndividualRequestAsync(request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Request rejected due to cycle day validation failure");

            // Verify failure logging
            var logEntries = _mockCorrelationLogger.GetAllLogEntries();
            logEntries.Should().HaveCount(1);
            logEntries[0].Status.Should().Be("Failed");
            logEntries[0].ErrorMessage.Should().Contain("Request rejected due to cycle day validation failure");

            var errorLogs = _mockErrorLogger.GetErrorLogs();
            errorLogs.Should().HaveCountGreaterThan(0);
            errorLogs[0].LogLevel.Should().Be("Error");
        }

        [Fact]
        public async Task ProcessIndividualRequestAsync_ShouldLogProcessFailure_WhenTokenRetrievalFails()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupSuccessfulValidation();
            _mockTokenManagerService
                .Setup(x => x.GetValidTokenAsync())
                .ThrowsAsync(new InvalidOperationException("Token retrieval failed"));

            // Act & Assert
            var act = async () => await _service.ProcessIndividualRequestAsync(request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Token retrieval failed");

            // Verify failure logging
            var logEntries = _mockCorrelationLogger.GetAllLogEntries();
            logEntries.Should().HaveCount(1);
            logEntries[0].Status.Should().Be("Failed");
            logEntries[0].ErrorMessage.Should().Contain("Token retrieval failed");
        }

        [Fact]
        public async Task ProcessIndividualRequestAsync_ShouldLogProcessFailure_WhenSearchFails()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupSuccessfulValidation();
            SetupSuccessfulTokenRetrieval();
            _mockPefindoApiService
                .Setup(x => x.SearchDebtorAsync(It.IsAny<PefindoSearchRequest>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("Search failed"));

            // Act & Assert
            var act = async () => await _service.ProcessIndividualRequestAsync(request);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Search failed");

            // Verify failure logging
            var logEntries = _mockCorrelationLogger.GetAllLogEntries();
            logEntries.Should().HaveCount(1);
            logEntries[0].Status.Should().Be("Failed");
            logEntries[0].ErrorMessage.Should().Contain("Search failed");
        }

        [Fact]
        public async Task ProcessIndividualRequestAsync_ShouldHandlePdfDownloadFailure_Gracefully()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupSuccessfulValidation();
            SetupSuccessfulTokenRetrieval();
            SetupSuccessfulSearch();
            SetupSuccessfulSimilarityValidation();
            SetupSuccessfulReportGeneration();
            SetupSuccessfulDataAggregation();

            // Simulate PDF download failure
            _mockPefindoApiService
                .Setup(x => x.DownloadPdfReportAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("PDF download failed"));

            // Act
            var result = await _service.ProcessIndividualRequestAsync(request);

            // Assert - Should still complete successfully
            result.Should().NotBeNull();
            
            // Verify warning was logged
            var errorLogs = _mockErrorLogger.GetErrorLogs();
            errorLogs.Should().Contain(e => e.LogLevel == "Warning" && e.Message.Contains("PDF download failed"));
        }

        [Fact]
        public void Constructor_ShouldCreateInstance_WithAllDependencies()
        {
            // Act
            var service = new IndividualProcessingService(
                _mockCycleDayValidationService.Object,
                _mockTokenManagerService.Object,
                _mockPefindoApiService.Object,
                _mockSimilarityValidationService.Object,
                _mockDataAggregationService.Object,
                _mockPbkDataRepository.Object,
                _mockGlobalConfigRepository.Object,
                _mockLogger.Object,
                _mockCorrelationService,
                _mockCorrelationLogger,
                _mockProcessStepLogger,
                _mockErrorLogger,
                _mockAuditLogger
            );

            // Assert
            service.Should().NotBeNull();
        }

        private IndividualRequest CreateValidRequest()
        {
            return new IndividualRequest
            {
                CfLosAppNo = "TEST-APP-123",
                Name = "John Doe",
                MotherName = "Jane Smith",
                IdNumber = "1234567890123456",
                Tolerance = 10
            };
        }

        private void SetupSuccessfulValidation()
        {
            _mockCycleDayValidationService
                .Setup(x => x.ValidateCycleDayAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
        }

        private void SetupSuccessfulTokenRetrieval()
        {
            _mockTokenManagerService
                .Setup(x => x.GetValidTokenAsync())
                .ReturnsAsync("valid-token-123");
        }

        private void SetupSuccessfulSearch()
        {
            _mockPefindoApiService
                .Setup(x => x.SearchDebtorAsync(It.IsAny<PefindoSearchRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new PefindoSearchResponse
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
                            NamaDebitur = "John Doe",
                            IdNo = "1234567890123456",
                            IdType = "KTP"
                        }
                    }
                });
        }

        private void SetupSuccessfulSimilarityValidation()
        {
            _mockGlobalConfigRepository
                .Setup(x => x.GetConfigValueAsync("GC35"))
                .ReturnsAsync("0.8");

            _mockGlobalConfigRepository
                .Setup(x => x.GetConfigValueAsync("GC36"))
                .ReturnsAsync("0.7");

            _mockSimilarityValidationService
                .Setup(x => x.ValidateSearchSimilarityAsync(
                    It.IsAny<IndividualRequest>(), 
                    It.IsAny<PefindoSearchData>(), 
                    It.IsAny<string>(), 
                    It.IsAny<double>()))
                .ReturnsAsync(new SimilarityValidationResult
                {
                    IsMatch = true,
                    NameSimilarity = (double)0.95,
                    MotherNameSimilarity = (double)0.90,
                    Message = "Similarity validation passed"
                });

            _mockSimilarityValidationService
                .Setup(x => x.ValidateReportSimilarityAsync(
                    It.IsAny<IndividualRequest>(), 
                    It.IsAny<PefindoDebiturInfo>(), 
                    It.IsAny<string>(), 
                    It.IsAny<double>(), 
                    It.IsAny<double>()))
                .ReturnsAsync(new SimilarityValidationResult
                {
                    IsMatch = true,
                    NameSimilarity = (double)0.95,
                    MotherNameSimilarity = (double)0.90,
                    Message = "Report similarity validation passed"
                });
        }

        private void SetupSuccessfulReportGeneration()
        {
            _mockPefindoApiService
                .Setup(x => x.GenerateReportAsync(It.IsAny<PefindoReportRequest>(), It.IsAny<string>()))
                .ReturnsAsync(new PefindoReportResponse
                {
                    Code = "01",
                    Status = "success",
                    EventId = "event-123"
                });

            _mockPefindoApiService
                .Setup(x => x.GetReportAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new PefindoGetReportResponse
                {
                    Code = "01",
                    Status = "success",
                    EventId = "event-123",
                    Report = new PefindoReportData
                    {
                        Debitur = new PefindoDebiturInfo
                        {
                            IdPefindo = 9999999,
                            NamaDebitur = "John Doe"
                        },
                        ScoreInfo = new PefindoScoreInfo
                        {
                            Score = "750",
                            RiskGrade = "A"
                        }
                    }
                });

            _mockPefindoApiService
                .Setup(x => x.DownloadPdfReportAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 1, 2, 3, 4, 5 });
        }

        private void SetupSuccessfulDataAggregation()
        {
            _mockDataAggregationService
                .Setup(x => x.AggregateIndividualDataAsync(
                    It.IsAny<IndividualRequest>(),
                    It.IsAny<PefindoSearchResponse>(),
                    It.IsAny<PefindoGetReportResponse>(),
                    It.IsAny<ProcessingContext>()))
                .ReturnsAsync(new IndividualData
                {
                    AppNo = "TEST-APP-123",
                    IdNumber = "1234567890123456",
                    Score = "750",
                    Status = "A"
                });

            _mockPbkDataRepository
                .Setup(x => x.StoreSummaryDataAsync(
                    It.IsAny<string>(),
                    It.IsAny<IndividualData>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(1);
        }
    }
}