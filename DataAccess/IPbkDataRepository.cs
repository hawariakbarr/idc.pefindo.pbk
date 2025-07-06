using System.Text.Json;
using idc.pefindo.pbk.Models;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Repository for PBK-related data operations
/// </summary>
public interface IPbkDataRepository
{
    /// <summary>
    /// Store search results from Pefindo
    /// </summary>
    Task<int> StoreSearchResultAsync(string appNo, int inquiryId, PefindoSearchResponse searchResponse);
    
    /// <summary>
    /// Store complete report data from Pefindo
    /// </summary>
    Task<int> StoreReportDataAsync(string eventId, string appNo, int inquiryId, PefindoGetReportResponse reportResponse, string? pdfPath = null);
    
    /// <summary>
    /// Store aggregated summary data
    /// </summary>
    Task<int> StoreSummaryDataAsync(string appNo, IndividualData summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null);
    
    /// <summary>
    /// Retrieve summary data by application number
    /// </summary>
    Task<IndividualData?> GetSummaryDataAsync(string appNo);
    
    /// <summary>
    /// Log processing step with timing and status
    /// </summary>
    Task<int> LogProcessingStepAsync(string appNo, string stepName, string status, JsonDocument? stepData = null, string? errorMessage = null, int? processingTimeMs = null);
    
    /// <summary>
    /// Get processing log for an application
    /// </summary>
    Task<List<ProcessingLogEntry>> GetProcessingLogAsync(string appNo);
}

/// <summary>
/// Processing log entry model
/// </summary>
public class ProcessingLogEntry
{
    public string StepName { get; set; } = string.Empty;
    public string StepStatus { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public DateTime CreatedDate { get; set; }
}
