using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Main service orchestrating the complete individual processing workflow
/// 
/// WORKFLOW:
/// 1. Validate cycle day rules
/// 2. Authenticate with Pefindo API
/// 3. Perform smart search
/// 4. Validate search similarity
/// 5. Generate detailed report
/// 6. Validate report similarity
/// 7. Aggregate and store summary data
/// </summary>
public interface IIndividualProcessingService
{
    /// <summary>
    /// Processes a complete individual credit assessment request
    /// </summary>
    /// <param name="request">Individual request data</param>
    /// <returns>Processed individual response</returns>
    Task<IndividualResponse> ProcessIndividualRequestAsync(IndividualRequest request);
}

/// <summary>
/// Implementation of individual processing service
/// </summary>
public class IndividualProcessingService : IIndividualProcessingService
{
    private readonly ICycleDayValidationService _cycleDayValidationService;
    private readonly ILogger<IndividualProcessingService> _logger;

    public IndividualProcessingService(
        ICycleDayValidationService cycleDayValidationService,
        ILogger<IndividualProcessingService> logger)
    {
        _cycleDayValidationService = cycleDayValidationService;
        _logger = logger;
    }

    public async Task<IndividualResponse> ProcessIndividualRequestAsync(IndividualRequest request)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting individual processing for app_no: {AppNo}", request.CfLosAppNo);

            // Step 1: Cycle Day Validation
            _logger.LogDebug("Step 1: Validating cycle day for app_no: {AppNo}", request.CfLosAppNo);
            var isCycleDayValid = await _cycleDayValidationService.ValidateCycleDayAsync(request.Tolerance);
            if (!isCycleDayValid)
            {
                _logger.LogWarning("Cycle day validation failed for app_no: {AppNo}", request.CfLosAppNo);
                throw new InvalidOperationException("Request rejected due to cycle day validation failure");
            }

            // TODO: Implement remaining steps
            // Step 2: Get Pefindo Token
            // Step 3: Request SmartSearch from Pefindo
            // Step 4: Validate SmartSearch Result + Run Similarity Check
            // Step 5: Request Custom Report from Pefindo
            // Step 6: Run Report Similarity Check
            // Step 7: Parse & Map JSON Response from PBK
            // Step 8: Request PDF Report from PBK
            // Step 9: Persist Data + Run Customer Summary Aggregation
            
            // For now, return a mock response
            var response = new IndividualResponse
            {
                Data = new IndividualData
                {
                    AppNo = request.CfLosAppNo,
                    IdNumber = request.IdNumber,
                    Status = "SUCCESS",
                    Message = "Individual processing completed (mock implementation)",
                    CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    ResponseStatus = "ALL_CORRECT"
                }
            };

            stopwatch.Stop();
            _logger.LogInformation("Individual processing completed for app_no: {AppNo} in {ElapsedMs}ms", 
                request.CfLosAppNo, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error processing individual request for app_no: {AppNo} after {ElapsedMs}ms", 
                request.CfLosAppNo, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
