using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using idc.pefindo.pbk.Configuration;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using EncryptionApi.Services;

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
    private readonly IEncryptionService _encryptionService;
    private readonly GlobalConfig _globalConfig;
    private readonly PDPConfig _pdpConfig;

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
        IAuditLogger auditLogger,
        IOptions<GlobalConfig> globalConfigOptions,
        IEncryptionService encryptionService,
        IOptions<PDPConfig> pdpConfigOptions)
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
        _globalConfig = globalConfigOptions.Value;
        _encryptionService = encryptionService;
        _pdpConfig = pdpConfigOptions.Value;
    }

    /// <summary>
    /// Alternative processing method using JsonNode for flexible object handling
    /// </summary>
    public async Task<JsonNode?> ProcessIndividualRequestWithJsonAsync(IndividualRequest request)
    {
        var globalStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var processingStartTime = DateTime.UtcNow;
        var appNo = request.CfLosAppNo;
        var correlationId = _correlationService.GetCorrelationId();
        var requestId = _correlationService.GetRequestId();
        var processingResults = new ProcessingResults();

        // Determine ID type based on request type
        var idType = request.TypeData?.ToUpper() switch
        {
            "PERSONAL" => "KTP",
            _ => "KTP" // Default to KTP for PERSONAL
        };

        try
        {
            _logger.LogInformation("Starting complete individual processing with JSON for app_no: {AppNo}, correlation: {CorrelationId}",
                appNo, correlationId);

            // Log process start to master correlation log
            await _correlationLogger.LogProcessStartAsync(correlationId, requestId, "IndividualProcessingJson", "system", null);

            // Audit log the start of processing
            await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingJsonStarted",
                "IndividualRequest", appNo, null, request);

            var stepOrder = 1;

            // Step 1: Cycle Day Validation with PDP Security
            var cycleDayValid = await ExecuteStepWithLogging<bool>("CYCLE_DAY_VALIDATION", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 1: Validating cycle day with PDP for app_no: {AppNo}", appNo);
                var isValid = await _cycleDayValidationService.ValidateCycleDayWithPDPAsync(idType.ToLower(), request.IdNumber, request.Tolerance);

                var logData = new { cycle_day_valid = isValid, tolerance = request.Tolerance, validation_method = "PDP" };
                return (logData, isValid);
            });

            // If cycle day validation true, try to get existing data
            if (cycleDayValid)
            {
                _logger.LogWarning("PDP cycle day validation failed for app_no: {AppNo}, attempting to retrieve existing data", appNo);

                var decryptedSymmetricKey = _encryptionService.DecryptString(_pdpConfig.SymmetricKey);
                var existingData = await _pbkDataRepository.DuplicateAndGetSummaryData(appNo, request.IdNumber, decryptedSymmetricKey);

                if (existingData != null)
                {
                    _logger.LogInformation("Found existing summary data for app_no: {AppNo}, returning cached data", appNo);

                    // Log process completion
                    await _correlationLogger.LogProcessCompleteAsync(correlationId, "Success - Cycle Day  Get Data");
                    await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingJsonCompletedFromCache",
                        "IndividualResponse", appNo, null, new { status = "success", data_source = "cache" });

                    _logger.LogInformation("Individual processing with JSON completed from cache for app_no: {AppNo} in {ElapsedMs}ms, correlation: {CorrelationId}",
                        appNo, globalStopwatch.ElapsedMilliseconds, correlationId);

                    return new JsonObject
                    {
                        ["status"] = "success",
                        ["data_source"] = "cycle_day_cache",
                        ["app_no"] = appNo,
                        ["data"] = existingData,
                        ["processing_time_ms"] = globalStopwatch.ElapsedMilliseconds,
                        ["timestamp"] = DateTime.UtcNow
                    };
                }

                throw new InvalidOperationException("Request rejected due to cycle day validation failure and no existing data found");
            }

            // Step 2: Get Pefindo Token (sama seperti method asli)
            processingResults.Token = await ExecuteStepWithLogging<string>("GET_PEFINDO_TOKEN", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 2: Getting Pefindo API token for app_no: {AppNo}", appNo);
                var token = await _tokenManagerService.GetValidTokenAsync();
                var logData = new { token_acquired = true };
                return (logData, token);
            });

            // Step 3: Request SmartSearch from Pefindo PBK (sama seperti method asli)
            processingResults.SearchResponse = await ExecuteStepWithLogging<PefindoSearchResponse>("SMART_SEARCH", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 3: Performing smart search for app_no: {AppNo}", appNo);

                var searchRequest = new PefindoSearchRequest
                {
                    Type = request.TypeData ?? "PERSONAL",
                    ProductId = 1,
                    InquiryReason = 1,
                    ReferenceCode = appNo,
                    Params = new List<PefindoSearchParam>
                    {
                        new()
                        {
                            IdType = idType,
                            IdNo = request.IdNumber,
                            Name = request.Name,
                            DateOfBirth = request.DateOfBirth
                        }
                    }
                };

                var response = await _pefindoApiService.SearchDebtorAsync(searchRequest, processingResults.Token);

                if (response.Code != "01" || !response.Status.Equals("success", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new InvalidOperationException($"Smart search failed: {response.Message}");
                }

                if (response.Data.Count == 0)
                {
                    throw new InvalidOperationException("No matching records found in smart search");
                }

                // Store search results
                await _pbkDataRepository.StoreSearchResultAsync(appNo, response.InquiryId, response);

                var logData = new
                {
                    search_results_count = response.Data.Count,
                    inquiry_id = response.InquiryId
                };

                return (logData, response);
            });

            // Step 4: Validate SmartSearch Result + Run Similarity Check (sama seperti method asli)
            processingResults.SelectedSearchData = await ExecuteStepWithLogging<PefindoSearchData>("SMARTSEARCH_SIMILARITY_CHECK", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 4: Validating search results and running similarity check for app_no: {AppNo}", appNo);

                var bestMatch = processingResults.SearchResponse.Data.First();

                var nameThreshold = await GetNameThresholdAsync();
                var motherNameThreshold = await GetMotherNameThresholdAsync();

                var similarityResult = await _similarityValidationService.ValidateSearchSimilarityAsync(
                    request.IdNumber, request.Name, request.DateOfBirth,
                    appNo, bestMatch, nameThreshold);

                // Only throw exception for system errors, not business logic no-match
                if (similarityResult.Status == "Invalid Data" || similarityResult.Status == "Unknown")
                {
                    _logger.LogWarning("Similarity validation failed for app_no: {AppNo}. Status: {Status}, Message: {Message}",
                        appNo, similarityResult.Status, similarityResult.Message);
                    throw new InvalidOperationException(similarityResult.Message);
                }

                if (!similarityResult.IsMatch)
                {
                    _logger.LogWarning("Search similarity validation failed for app_no: {AppNo}. Status: {Status}, Message: {Message}",
                        appNo, similarityResult.Status, similarityResult.Message);
                    throw new InvalidOperationException(similarityResult.Message);
                }

                var logData = new
                {
                    selected_id = bestMatch.IdPefindo,
                    name_similarity = similarityResult.NameSimilarity,
                    mother_name_similarity = similarityResult.MotherNameSimilarity,
                    similarity_match = similarityResult.IsMatch
                };

                return (logData, bestMatch);
            });

            // Step 5: Request Custom Report from Pefindo PBK (menggunakan JSON)
            processingResults.EventId = Guid.NewGuid().ToString();
            processingResults.ReportResponseJson = await ExecuteStepWithLogging<JsonNode?>("GENERATE_REPORT_JSON", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 5: Generating custom report as JSON for app_no: {AppNo}, event_id: {EventId}", appNo, processingResults.EventId);

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

                if (generateResponse.Code != "01" || !generateResponse.Status.Equals("success", StringComparison.CurrentCultureIgnoreCase))
                {
                    throw new InvalidOperationException($"Report generation failed: {generateResponse.Message}");
                }

                // Wait for report and get as JsonNode
                var reportJsonResponse = await WaitForReportCompletionAsJson(processingResults.EventId, processingResults.Token);

                // Audit log report generation
                await _auditLogger.LogActionAsync(correlationId, "system", "CreditReportGenerated",
                    "CreditReport", processingResults.EventId, null, new { event_id = processingResults.EventId, status = "ready" });

                var logData = new
                {
                    event_id = processingResults.EventId,
                    report_status = "ready"
                };

                return (logData, reportJsonResponse);
            });

            // Step 6: Run Report Similarity Check
            await ExecuteStepWithLogging("REPORT_SIMILARITY_CHECK", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 6: Validating report similarity for app_no: {AppNo}", appNo);

                if (processingResults.ReportResponseJson?["report"]?["debitur"] == null)
                {
                    _logger.LogWarning("Report data is incomplete for similarity check for app_no: {AppNo}", appNo);
                    throw new InvalidOperationException("Report data is incomplete for similarity check");
                }

                var nameThreshold = await GetNameThresholdAsync();
                var motherNameThreshold = await GetMotherNameThresholdAsync();

                // Check if the similarity service has a JsonNode overload, otherwise use the alternative approach
                var similarityResult = await _similarityValidationService.ValidateReportSimilarityAsync(
                     request.IdNumber, request.Name, request.DateOfBirth, request.MotherName,
                     appNo, processingResults.ReportResponseJson!, nameThreshold, motherNameThreshold);

                // Log audit for both match and no-match scenarios
                var auditAction = similarityResult.IsMatch ? "ReportSimilarityValidationPassed" : "ReportSimilarityValidationFailed";
                await _auditLogger.LogActionAsync(correlationId, "system", auditAction,
                    "ReportSimilarity", appNo, null, similarityResult);

                // Only throw exception for system errors, not business logic no-match
                if (similarityResult.Status == "Invalid Data" || similarityResult.Status == "Unknown")
                {
                    _logger.LogWarning("Report similarity validation failed for app_no: {AppNo}. Status: {Status}, Message: {Message}",
                        appNo, similarityResult.Status, similarityResult.Message);
                    throw new InvalidOperationException(similarityResult.Message);
                }

                if (!similarityResult.IsMatch)
                {
                    _logger.LogWarning("Report similarity validation failed for app_no: {AppNo}. Status: {Status}, Message: {Message}",
                        appNo, similarityResult.Status, similarityResult.Message);

                    throw new InvalidOperationException(similarityResult.Message);
                }

                return new
                {
                    is_match = similarityResult.IsMatch,
                    name_similarity = similarityResult.NameSimilarity,
                    mother_name_similarity = similarityResult.MotherNameSimilarity,
                    name_threshold = nameThreshold,
                    mother_threshold = motherNameThreshold
                };
            });

            // Step 7: Download PDF Report (Optional, using JSON method)
            try
            {
                processingResults.PdfPath = await ExecuteStepWithLogging<string?>("DOWNLOAD_PDF_REPORT_JSON", stepOrder++, appNo, async () =>
                {
                    _logger.LogDebug("Step 7: Downloading PDF report as JSON for app_no: {AppNo}", appNo);

                    if (string.IsNullOrEmpty(processingResults.EventId))
                    {
                        throw new InvalidOperationException("Event ID is required for PDF download");
                    }

                    var pdfJsonResponse = await _pefindoApiService.DownloadPdfReportWithJsonAsync(processingResults.EventId, processingResults.Token);

                    if (pdfJsonResponse?["binaryData"] == null)
                    {
                        throw new InvalidOperationException("PDF binary data not found in response");
                    }

                    var base64Data = pdfJsonResponse["binaryData"]!.ToString();
                    var pdfBytes = Convert.FromBase64String(base64Data);

                    // Save PDF to file system
                    var pdfPath = await SavePdfReportAsync(appNo, processingResults.EventId, pdfBytes);

                    var logData = new
                    {
                        pdf_downloaded = true,
                        pdf_size_bytes = pdfBytes.Length,
                        pdf_path = pdfPath,
                        method = "json"
                    };

                    return (logData, pdfPath);
                });
            }
            catch (Exception ex)
            {
                await _errorLogger.LogWarningAsync("IndividualProcessingService.DownloadPdfJson",
                    $"PDF download with JSON failed for app_no: {appNo}, continuing without PDF", correlationId);
                _logger.LogWarning(ex, "PDF download with JSON failed for app_no: {AppNo}, continuing without PDF", appNo);
                processingResults.PdfPath = null;
            }


            // Step 8: Store Report Data (menggunakan data JSON)
            await ExecuteStepWithLogging("STORE_REPORT_DATA_JSON", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 8: Storing report data as JSON for app_no: {AppNo}", appNo);

                if (processingResults.ReportResponseJson == null)
                {
                    throw new InvalidOperationException("Report response JSON is null, cannot store report data");
                }

                await _pbkDataRepository.StoreReportDataWithJsonAsync(processingResults.EventId, appNo, processingResults.SearchResponse.InquiryId, processingResults.ReportResponseJson, processingResults.PdfPath);

                return new { report_data_stored = true };
            });

            // Step 9: Aggregate Data and Generate Final Response (menggunakan JSON method)
            var individualData = await ExecuteStepWithLogging("DATA_AGGREGATION_JSON", stepOrder++, appNo, async () =>
            {
                _logger.LogDebug("Step 9: Aggregating data using JSON for app_no: {AppNo}", appNo);

                // Get summary data from database
                JsonNode? summaryData = null;
                try
                {
                    var decryptedSymmetricKey = _encryptionService.DecryptString(_pdpConfig.SymmetricKey);
                    summaryData = await _pbkDataRepository.DuplicateAndGetSummaryData(appNo, request.IdNumber, decryptedSymmetricKey);
                    if (summaryData != null)
                    {
                        _logger.LogDebug("Retrieved summary data from database for app_no: {AppNo}", appNo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to retrieve summary data for app_no: {AppNo}, continuing with report data only", appNo);
                }

                var processingContext = new ProcessingContext
                {
                    EventId = processingResults.EventId,
                    PdfPath = processingResults.PdfPath,
                    ProcessingStartTime = processingStartTime
                };

                // var individualData = await _dataAggregationService.AggregateIndividualDataWithJsonAsync(
                //     request, processingResults.SearchResponse, processingResults.ReportResponseJson, processingContext);

                var individualData = await _dataAggregationService.AggregateIndividualWithReturnJsonAsync(
                    request, processingResults.SearchResponse, processingResults.ReportResponseJson, processingContext);

                var logData = new
                {
                    aggregation_completed = true,
                    summary_data_available = summaryData != null,
                    total_processing_time_ms = globalStopwatch.ElapsedMilliseconds,
                    individual_data = individualData
                };
                return (logData, individualData);
            });

            // Create final aggregated data using JSON method
            var processingContextFinal = new ProcessingContext
            {
                EventId = processingResults.EventId,
                PdfPath = processingResults.PdfPath,
                ProcessingStartTime = processingStartTime
            };

            // Step 10: Store final summary data
            _logger.LogDebug("Step 10: Storing final summary data for app_no: {AppNo}", appNo);

            // await _pbkDataRepository.StoreSummaryDataAsync(
            // appNo, individualData, processingResults.SelectedSearchData.IdPefindo.ToString(),
            // processingResults.SearchResponse.InquiryId.ToString(), processingResults.EventId);

            await _pbkDataRepository.StoreSummaryDataWithJsonAsync(
            appNo, individualData, processingResults.SelectedSearchData.IdPefindo.ToString(),
            processingResults.SearchResponse.InquiryId.ToString(), processingResults.EventId);

            globalStopwatch.Stop();


            var response = new JsonObject
            {
                ["status"] = "success",
                ["data"] = individualData
            };

            // Log process completion to master correlation log
            await _correlationLogger.LogProcessCompleteAsync(correlationId, "Success");

            // Audit log successful completion
            await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingJsonCompleted",
                "IndividualResponse", appNo, null, new { status = "success", processing_time_ms = globalStopwatch.ElapsedMilliseconds });

            _logger.LogInformation("Individual processing with JSON completed successfully for app_no: {AppNo} in {ElapsedMs}ms, correlation: {CorrelationId}",
                appNo, globalStopwatch.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            globalStopwatch.Stop();

            // Log process failure to master correlation log
            await _correlationLogger.LogProcessFailAsync(correlationId, "Failed", ex.Message);

            // Log comprehensive error information
            await _errorLogger.LogErrorAsync("IndividualProcessingService.ProcessRequestWithJson",
                $"Individual processing with JSON failed for app_no: {appNo}", ex, correlationId);

            // Audit log the failure
            await _auditLogger.LogActionAsync(correlationId, "system", "PBKProcessingJsonFailed",
                "IndividualRequest", appNo, null, new
                {
                    error_message = ex.Message,
                    processing_time_ms = globalStopwatch.ElapsedMilliseconds
                });

            _logger.LogError(ex, "Individual processing with JSON failed for app_no: {AppNo} after {ElapsedMs}ms, correlation: {CorrelationId}",
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

    private async Task<JsonNode?> WaitForReportCompletionAsJson(string eventId, string token)
    {
        var maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(3);

        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(retryDelay);

            var getReportJsonResponse = await _pefindoApiService.GetReportAsJsonAsync(eventId, token);

            // Check status dari JsonNode
            var code = getReportJsonResponse?["code"]?.ToString();

            if (code == "01") // Report ready
            {
                return getReportJsonResponse;
            }
            else if (code == "32") // Still processing
            {
                _logger.LogDebug("Report still processing, retry {Retry}/{MaxRetries}", i + 1, maxRetries);
                continue;
            }
            else if (code == "36") // Big report
            {
                // Handle big report by downloading in chunks
                return await _pefindoApiService.DownloadReportAsJsonAsync(eventId, token);
            }
            else
            {
                var message = getReportJsonResponse?["message"]?.ToString();
                throw new InvalidOperationException($"Report retrieval failed: {message}");
            }
        }

        throw new TimeoutException($"Report generation timed out after {maxRetries} retries");
    }

    private async Task<double> GetNameThresholdAsync()
    {
        var config = await _globalConfigRepository.GetConfigValueAsync(_globalConfig.NameThreshold);
        return double.TryParse(config, out var threshold) ? threshold : 0.8;
    }

    private async Task<double> GetMotherNameThresholdAsync()
    {
        var config = await _globalConfigRepository.GetConfigValueAsync(_globalConfig.MotherNameThreshold);
        return double.TryParse(config, out var threshold) ? threshold : 0.9;
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
        public JsonNode? ReportResponseJson { get; set; } // New field for JSON handling
        public string? PdfPath { get; set; }
    }
}
