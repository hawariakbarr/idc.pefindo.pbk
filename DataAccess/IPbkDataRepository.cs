using System.Text.Json;
using System.Text.Json.Nodes;
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
    ///
    /// </summary>
    /// <param name="eventId"></param>
    /// <param name="appNo"></param>
    /// <param name="inquiryId"></param>
    /// <param name="reportResponse"></param>
    /// <param name="pdfPath"></param>
    /// <returns></returns>

    Task<int> StoreReportDataWithJsonAsync(string eventId, string appNo, int inquiryId, JsonNode? reportResponse, string? pdfPath = null);

    /// <summary>
    /// Store aggregated summary data
    /// </summary>
    Task<int> StoreSummaryDataAsync(string appNo, IndividualData summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null);

    /// <summary>
    /// Store aggregated summary data with JsonNode for flexible handling
    /// </summary>
    /// <param name="appNo"></param>
    /// <param name="summaryData"></param>
    /// <param name="pefindoId"></param>
    /// <param name="searchId"></param>
    /// <param name="eventId"></param>
    /// <returns></returns>
    Task<int> StoreSummaryDataWithJsonAsync(string appNo, JsonNode? summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null);


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

    /// <summary>
    /// Get PBK info identity with encryption support
    /// </summary>
    Task<PbkInfoIdentity?> GetPbkInfoIdentityWithEncryptionAsync(string idType, string idNo, string encryptKey);

    /// <summary>
    /// Get summary perorangan identity with encryption support
    /// </summary>
    Task<SummaryPeroranganIdentity?> GetSummaryPeroranganIdentityWithEncryptionAsync(string idNo, string encryptKey);

    /// <summary>
    /// Duplicate and get summary data with flexible return type
    /// </summary>
    Task<JsonNode?> DuplicateAndGetSummaryData(string appNo, string idNo, string encryptKey);
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

/// <summary>
/// PBK info identity model for encrypted data
/// </summary>
public class PbkInfoIdentity
{
    public long PfId { get; set; }
    public string PfBrwCode { get; set; } = string.Empty;
    public string PfIdentityType { get; set; } = string.Empty;
    public string PfIdentityNo { get; set; } = string.Empty;
    public DateTime PfReqDate { get; set; }
    public DateTime PfResDate { get; set; }
    public int PfStatus { get; set; }
    public string PfReqUsr { get; set; } = string.Empty;
}

/// <summary>
/// Summary perorangan identity model for encrypted data
/// </summary>
public class SummaryPeroranganIdentity
{
    public long IspId { get; set; }
    public string IspAppNo { get; set; } = string.Empty;
    public long IspPId { get; set; }
    public string IspSearchId { get; set; } = string.Empty;
    public DateTime IspCreatedDate { get; set; }
}
