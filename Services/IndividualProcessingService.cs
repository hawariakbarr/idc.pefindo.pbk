using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using idc.pefindo.pbk.Configuration;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Complete IndividualProcessingService with comprehensive logging
/// </summary>
public class IndividualProcessingService : IIndividualProcessingService
{
    private readonly ICycleDayValidationService _cycleDayValidationService;
    private readonly ITokenManagerService _tokenManagerService;
    private readonly IPefindoApiService _pefindoApiService;
    private readonly ISimilarityValidationService _similarityValidationService;
    private readonly IDataAggregationService _dataAggregationService;
    private readonly IPbkDataRepository _pbkDataRepository;
    private readonly IGlobalConfigRepository _globalConfigRepository;
    private readonly ILogger<IndividualProcessingService> _logger;

    // New logging services
    private readonly ICorrelationService _correlationService;
    private readonly ICorrelationLogger _correlationLogger;
    private readonly IProcessStepLogger _processStepLogger;
    private readonly IErrorLogger _errorLogger;
    private readonly IAuditLogger _auditLogger;

    public IndividualProcessingService(
        ICycleDayValidationService cycleDayValidationService,
        ITokenManagerService tokenManagerService,
        IPefindoApiService pefindoApiService,
        ISimilarityValidationService similarityValidationService,
        IDataAggregationService dataAggregationService,
        IPbkDataRepository pbkDataRepository,
        IGlobalConfigRepository globalConfigRepository,
        ILogger<IndividualProcessingService> logger,
        ICorrelationService correlationService,
        ICorrelationLogger correlationLogger,
        IProcessStepLogger processStepLogger,
        IErrorLogger errorLogger,
        IAuditLogger auditLogger)
    {
        _cycleDayValidationService = cycleDayValidationService;
        _tokenManagerService = tokenManagerService;
        _pefindoApiService = pefindoApiService;
        _similarityValidationService = similarityValidationService;
        _dataAggregationService = dataAggregationService;
        _pbkDataRepository = pbkDataRepository;
        _globalConfigRepository = globalConfigRepository;
        _logger = logger;
        _correlationService = correlationService;
        _correlationLogger = correlationLogger;
        _processStepLogger = processStepLogger;
        _errorLogger = errorLogger;
        _auditLogger = auditLogger;
    }

    public async Task<IndividualResponse> ProcessIndividualRequestAsync(IndividualRequest request)
    {
        var globalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var processingStartTime = DateTime.UtcNow;
        var appNo = request.CfLosAppNo;
        var correlationId = _correlationService.GetCorrelationId();
        var requestId = _correlationService.GetRequestId();
        var processingResults = new ProcessingResults();

        try
        {
            _logger.LogInformation("Starting complete individual processing for app_no: {AppNo}, correlation: {CorrelationId}",
                appNo, correlationId);

            // Log process start to master correlation log
            await _correlationLogger.LogProcessStartAsync(correlationId, requestId, "IndividualProcessing", "system", null);

            //// Audit log the start of processing
            await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingStarted",
                "IndividualRequest", appNo, null, request);

            var stepOrder = 1;

            // Step 1: Cycle Day Validation
            await ExecuteStepWithLogging("CYCLE_DAY_VALIDATION", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 1: Validating cycle day for app_no: {AppNo}", appNo);
                var isValid = await _cycleDayValidationService.ValidateCycleDayAsync(request.Tolerance);
                if (!isValid)
                {
                    throw new InvalidOperationException("Request rejected due to cycle day validation failure");
                }
                return new { cycle_day_valid = true, tolerance = request.Tolerance };
            });

            // Step 2: Get Pefindo Token
            processingResults.Token = await ExecuteStepWithLogging<string>("GET_PEFINDO_TOKEN", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 2: Getting Pefindo API token for app_no: {AppNo}", appNo);
                var authToken = await _tokenManagerService.GetValidTokenAsync();
                return (new { token_obtained = true }, authToken);
            });

            // Step 3: Request SmartSearch from Pefindo PBK
            processingResults.SearchResponse = await ExecuteStepWithLogging<PefindoSearchResponse>("SMART_SEARCH", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 3: Performing smart search for app_no: {AppNo}", appNo);

                var searchRequest = new PefindoSearchRequest
                {
                    Type = "PERSONAL",
                    ProductId = 1,
                    InquiryReason = 1,
                    ReferenceCode = appNo,
                    Params = new List<PefindoSearchParam>
                    {
                        new()
                        {
                            IdType = "KTP",
                            IdNo = request.IdNumber,
                            Name = request.Name,
                            DateOfBirth = request.DateOfBirth
                        }
                    }
                };

                var response = await _pefindoApiService.SearchDebtorAsync(searchRequest, processingResults.Token);

                if (response.Code != "01" || response.Status != "success")
                {
                    throw new InvalidOperationException($"Smart search failed: {response.Message}");
                }

                if (response.Data.Count==0)
                {
                    throw new InvalidOperationException("No debtor data found in smart search");
                }

                // Store search results
                await _pbkDataRepository.StoreSearchResultAsync(appNo, response.InquiryId, response);

                // Audit log successful search
                await _auditLogger.LogActionAsync(correlationId, "system", "SmartSearchCompleted",
                    "SearchResponse", appNo, null, new { inquiry_id = response.InquiryId, results_count = response.Data.Count });

                var logData = new
                {
                    inquiry_id = response.InquiryId,
                    results_count = response.Data.Count,
                    response_status = response.ResponseStatus
                };

                return (logData, response);
            });

            // Step 4: Validate SmartSearch Result + Run Similarity Check
            processingResults.SelectedSearchData = await ExecuteStepWithLogging<PefindoSearchData>("SIMILARITY_CHECK_SEARCH", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 4: Validating search similarity for app_no: {AppNo}", appNo);

                var nameThreshold = await GetNameThresholdAsync();
                var bestMatch = processingResults.SearchResponse.Data.OrderByDescending(d => d.SimilarityScore).First();

                var similarityResult = await _similarityValidationService.ValidateSearchSimilarityAsync(
                    request, bestMatch, appNo, nameThreshold);

                if (!similarityResult.IsMatch)
                {
                    throw new InvalidOperationException($"Search similarity check failed: {similarityResult.Message}");
                }

                // Audit log similarity validation
                await _auditLogger.LogActionAsync(correlationId, "system", "SimilarityValidationPassed",
                    "SearchSimilarity", appNo, null, similarityResult);

                var logData = new
                {
                    is_match = similarityResult.IsMatch,
                    name_similarity = similarityResult.NameSimilarity,
                    threshold = nameThreshold,
                    selected_pefindo_id = bestMatch.IdPefindo
                };

                return (logData, bestMatch);
            });

            // Step 5: Request Custom Report from Pefindo PBK
            processingResults.EventId = Guid.NewGuid().ToString();
            processingResults.ReportResponse = await ExecuteStepWithLogging<PefindoGetReportResponse>("GENERATE_REPORT", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 5: Generating custom report for app_no: {AppNo}, event_id: {EventId}", appNo, processingResults.EventId);

                var reportRequest = new PefindoReportRequest
                {
                    InquiryId = processingResults.SearchResponse.InquiryId,
                    EventId = processingResults.EventId,
                    GeneratePdf = "1",
                    Language = "01",
                    Ids = new List<PefindoReportIdParam>
                    {
                        new()
                        {
                            IdType = processingResults.SelectedSearchData.IdType,
                            IdNo = processingResults.SelectedSearchData.IdNo,
                            IdPefindo = processingResults.SelectedSearchData.IdPefindo
                        }
                    }
                };

                var generateResponse = await _pefindoApiService.GenerateReportAsync(reportRequest, processingResults.Token);

                if (generateResponse.Code != "01" || generateResponse.Status != "success")
                {
                    throw new InvalidOperationException($"Report generation failed: {generateResponse.Message}");
                }

                // Wait for report to be ready and retrieve it
                var reportResponse = await WaitForReportCompletion(processingResults.EventId, processingResults.Token);

                // Audit log report generation
                await _auditLogger.LogActionAsync(correlationId, "system", "CreditReportGenerated",
                    "CreditReport", processingResults.EventId, null, new { event_id = processingResults.EventId, status = "ready" });

                var logData = new
                {
                    event_id = processingResults.EventId,
                    report_status = "ready"
                };

                return (logData, reportResponse);
            });

            // Step 6: Run Report Similarity Check
            await ExecuteStepWithLogging("SIMILARITY_CHECK_REPORT", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 6: Validating report similarity for app_no: {AppNo}", appNo);

                if (processingResults.ReportResponse.Report?.Debitur == null)
                {
                    throw new InvalidOperationException("Report data is incomplete for similarity check");
                }

                var nameThreshold = await GetNameThresholdAsync();
                var motherNameThreshold = await GetMotherNameThresholdAsync();

                var similarityResult = await _similarityValidationService.ValidateReportSimilarityAsync(
                    request, processingResults.ReportResponse.Report.Debitur, appNo, nameThreshold, motherNameThreshold);

                if (!similarityResult.IsMatch)
                {
                    throw new InvalidOperationException($"Report similarity check failed: {similarityResult.Message}");
                }

                // Audit log report similarity validation
                await _auditLogger.LogActionAsync(correlationId, "system", "ReportSimilarityValidationPassed",
                    "ReportSimilarity", appNo, null, similarityResult);

                return new
                {
                    is_match = similarityResult.IsMatch,
                    name_similarity = similarityResult.NameSimilarity,
                    mother_name_similarity = similarityResult.MotherNameSimilarity,
                    name_threshold = nameThreshold,
                    mother_threshold = motherNameThreshold
                };
            });

            // Step 7: Store Report Data
            await ExecuteStepWithLogging("STORE_REPORT_DATA", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 7: Storing report data for app_no: {AppNo}", appNo);

                await _pbkDataRepository.StoreReportDataAsync(processingResults.EventId, appNo, processingResults.SearchResponse.InquiryId, processingResults.ReportResponse);

                return new { report_stored = true, event_id = processingResults.EventId };
            });

            // Step 8: Download PDF Report (Optional)
            try
            {
                processingResults.PdfPath = await ExecuteStepWithLogging<string>("DOWNLOAD_PDF_REPORT", stepOrder++, appNo, async () =>
                {
                    _logger.LogDebug("Step 8: Downloading PDF report for app_no: {AppNo}", appNo);

                    var pdfBytes = await _pefindoApiService.DownloadPdfReportAsync(processingResults.EventId, processingResults.Token);

                    // Save PDF to file system
                    var pdfPath = await SavePdfReportAsync(appNo, processingResults.EventId, pdfBytes);

                    var logData = new
                    {
                        pdf_downloaded = true,
                        pdf_size_bytes = pdfBytes.Length,
                        pdf_path = pdfPath
                    };

                    return (logData, pdfPath);
                });
            }
            catch (Exception ex)
            {
                await _errorLogger.LogWarningAsync("IndividualProcessingService.DownloadPdf",
                    $"PDF download failed for app_no: {appNo}, continuing without PDF", correlationId);
                _logger.LogWarning(ex, "PDF download failed for app_no: {AppNo}, continuing without PDF", appNo);
            }

            // Step 9: Aggregate Data and Generate Final Response
            await ExecuteStepWithLogging("DATA_AGGREGATION", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 9: Aggregating final data for app_no: {AppNo}", appNo);

                var processingContext = new ProcessingContext
                {
                    EventId = processingResults.EventId,
                    PdfPath = processingResults.PdfPath,
                    ProcessingStartTime = processingStartTime
                };

                var aggregatedData = await _dataAggregationService.AggregateIndividualDataAsync(
                    request, processingResults.SearchResponse, processingResults.ReportResponse, processingContext);

                // Store final summary data
                await _pbkDataRepository.StoreSummaryDataAsync(
                    appNo, aggregatedData, processingResults.SelectedSearchData.IdPefindo.ToString(),
                    processingResults.SearchResponse.InquiryId.ToString(), processingResults.EventId);

                return new
                {
                    aggregation_completed = true,
                    total_processing_time_ms = globalStopwatch.ElapsedMilliseconds
                };
            });

            // Create final aggregated data
            var processingContextFinal = new ProcessingContext
            {
                EventId = processingResults.EventId,
                PdfPath = processingResults.PdfPath,
                ProcessingStartTime = processingStartTime
            };

            var individualData = await _dataAggregationService.AggregateIndividualDataAsync(
                request, processingResults.SearchResponse, processingResults.ReportResponse, processingContextFinal);

            globalStopwatch.Stop();

            var response = new IndividualResponse { Data = individualData };

            // Log process completion to master correlation log
            await _correlationLogger.LogProcessCompleteAsync(correlationId, "Success");

            // Audit log successful completion
            await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingCompleted",
                "IndividualResponse", appNo, null, new { status = "success", processing_time_ms = globalStopwatch.ElapsedMilliseconds });

            _logger.LogInformation("Individual processing completed successfully for app_no: {AppNo} in {ElapsedMs}ms, correlation: {CorrelationId}",
                appNo, globalStopwatch.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            globalStopwatch.Stop();

            // Log process failure to master correlation log
            await _correlationLogger.LogProcessFailAsync(correlationId, "Failed", ex.Message);

            // Log comprehensive error information
            await _errorLogger.LogErrorAsync("IndividualProcessingService.ProcessRequest",
                $"Individual processing failed for app_no: {appNo}", ex, correlationId);

            // Audit log the failure
            await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingFailed",
                "IndividualRequest", appNo, null, new
                {
                    error_message = ex.Message,
                    processing_time_ms = globalStopwatch.ElapsedMilliseconds
                });

            _logger.LogError(ex, "Individual processing failed for app_no: {AppNo} after {ElapsedMs}ms, correlation: {CorrelationId}",
                appNo, globalStopwatch.ElapsedMilliseconds, correlationId);

            throw;
        }
    }

    private async Task ExecuteStepWithLogging(string stepName, int stepOrder, string appNo, Func<Task<object>> stepAction)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var requestId = _correlationService.GetRequestId();
        var stepStopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _processStepLogger.LogStepStartAsync(correlationId, requestId, stepName, stepOrder);

            var stepResult = await stepAction();

            stepStopwatch.Stop();
            await _processStepLogger.LogStepCompleteAsync(correlationId, requestId, stepName, stepResult, (int)stepStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            await _processStepLogger.LogStepFailAsync(correlationId, requestId, stepName, ex, null, (int)stepStopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<T> ExecuteStepWithLogging<T>(string stepName, int stepOrder, string appNo, Func<Task<(object, T)>> stepAction)
    {
        var correlationId = _correlationService.GetCorrelationId();
        var requestId = _correlationService.GetRequestId();
        var stepStopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _processStepLogger.LogStepStartAsync(correlationId, requestId, stepName, stepOrder);

            var (stepResult, returnValue) = await stepAction();

            stepStopwatch.Stop();
            await _processStepLogger.LogStepCompleteAsync(correlationId, requestId, stepName, stepResult, (int)stepStopwatch.ElapsedMilliseconds);

            return returnValue;
        }
        catch (Exception ex)
        {
            stepStopwatch.Stop();
            await _processStepLogger.LogStepFailAsync(correlationId, requestId, stepName, ex, null, (int)stepStopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<PefindoGetReportResponse> WaitForReportCompletion(string eventId, string token)
    {
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(3);

        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(retryDelay);

            var getReportResponse = await _pefindoApiService.GetReportAsync(eventId, token);

            if (getReportResponse.Code == "01") // Report ready
            {
                return getReportResponse;
            }
            else if (getReportResponse.Code == "32") // Still processing
            {
                _logger.LogDebug("Report still processing, retry {Retry}/{MaxRetries}", i + 1, maxRetries);
                continue;
            }
            else if (getReportResponse.Code == "36") // Big report
            {
                // Handle big report by downloading in chunks
                return await _pefindoApiService.DownloadReportAsync(eventId, token);
            }
            else
            {
                throw new InvalidOperationException($"Report retrieval failed: {getReportResponse.Message}");
            }
        }

        throw new TimeoutException($"Report generation timed out after {maxRetries} retries");
    }

    private async Task<double> GetNameThresholdAsync()
    {
        var config = await _globalConfigRepository.GetConfigValueAsync(GlobalConfigKeys.NameThreshold);
        return double.TryParse(config, out var threshold) ? threshold : 0.8;
    }

    private async Task<double> GetMotherNameThresholdAsync()
    {
        var config = await _globalConfigRepository.GetConfigValueAsync(GlobalConfigKeys.MotherNameThreshold);
        return double.TryParse(config, out var threshold) ? threshold : 0.7;
    }

    private async Task<string> SavePdfReportAsync(string appNo, string eventId, byte[] pdfBytes)
    {
        try
        {
            // Create PDF storage directory
            var pdfDirectory = Path.Combine("Files", "pdfs", DateTime.UtcNow.ToString("yyyy-MM"));
            Directory.CreateDirectory(pdfDirectory);

            // Generate PDF filename
            var fileName = $"{appNo}_{eventId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
            var filePath = Path.Combine(pdfDirectory, fileName);

            // Save PDF file
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            _logger.LogInformation("PDF report saved: {FilePath} ({Size} bytes)", filePath, pdfBytes.Length);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving PDF report for app_no: {AppNo}", appNo);
            throw;
        }
    }

    /// <summary>
    /// Helper class to store processing results between steps
    /// </summary>
    private class ProcessingResults
    {
        public string Token { get; set; } = string.Empty;
        public PefindoSearchResponse SearchResponse { get; set; } = new();
        public PefindoSearchData SelectedSearchData { get; set; } = new();
        public string EventId { get; set; } = string.Empty;
        public PefindoGetReportResponse ReportResponse { get; set; } = new();
        public string? PdfPath { get; set; }
    }
}
