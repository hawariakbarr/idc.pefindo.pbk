using idc.pefindo.pbk.Models;
using System.Text.Json.Nodes;

namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service for interacting with Pefindo PBK API
/// </summary>
public interface IPefindoApiService
{
    /// <summary>
    /// Authenticate with Pefindo API and get access token
    /// </summary>
    /// <returns>Access token for API calls</returns>
    Task<string> GetTokenAsync();

    /// <summary>
    /// Validate an existing token
    /// </summary>
    /// <param name="token">Token to validate</param>
    /// <returns>True if token is valid</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Perform smart search for debtor information
    /// </summary>
    /// <param name="request">Search request parameters</param>
    /// <param name="token">Valid access token</param>
    /// <returns>Search results</returns>
    Task<PefindoSearchResponse> SearchDebtorAsync(PefindoSearchRequest request, string token);

    /// <summary>
    /// Generate a detailed credit report
    /// </summary>
    /// <param name="request">Report generation request</param>
    /// <param name="token">Valid access token</param>
    /// <returns>Report generation response</returns>
    Task<PefindoReportResponse> GenerateReportAsync(PefindoReportRequest request, string token);

    /// <summary>
    /// Retrieve generated report data
    /// </summary>
    /// <param name="eventId">Event ID from report generation</param>
    /// <param name="token">Valid access token</param>
    /// <returns>Complete report data</returns>
    Task<PefindoGetReportResponse> GetReportAsync(string eventId, string token);

    /// <summary>
    /// Download large report data (for big reports)
    /// </summary>
    /// <param name="eventId">Event ID from report generation</param>
    /// <param name="token">Valid access token</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="maxRecords">Maximum records per page</param>
    /// <returns>Report data chunk</returns>
    Task<PefindoGetReportResponse> DownloadReportAsync(string eventId, string token, int? page = null, int? maxRecords = null);

    /// <summary>
    /// Download PDF report
    /// </summary>
    /// <param name="eventId">Event ID from report generation</param>
    /// <param name="token">Valid access token</param>
    /// <returns>PDF file content as byte array</returns>
    Task<byte[]> DownloadPdfReportAsync(string eventId, string token);

    /// <summary>
    /// Download PDF report as JsonNode with binary data for JSON processing
    /// </summary>
    /// <param name="eventId">Event ID from report generation</param>
    /// <param name="token">Valid access token</param>
    /// <returns>JSON object containing PDF binary data as base64 string</returns>
    Task<JsonNode?> DownloadPdfReportWithJsonAsync(string eventId, string token);

    /// <summary>
    /// Retrieve generated report data as JsonNode for flexible object handling
    /// </summary>
    /// <param name="eventId">Event ID from report generation</param>
    /// <param name="token">Valid access token</param>
    /// <returns>Report data as JsonNode</returns>
    Task<JsonNode?> GetReportAsJsonAsync(string eventId, string token);

    /// <summary>
    /// Download large report data as JsonNode for flexible object handling
    /// </summary>
    /// <param name="eventId">Event ID from report generation</param>
    /// <param name="token">Valid access token</param>
    /// <param name="page">Page number for pagination</param>
    /// <param name="maxRecords">Maximum records per page</param>
    /// <returns>Report data as JsonNode</returns>
    Task<JsonNode?> DownloadReportAsJsonAsync(string eventId, string token, int? page = null, int? maxRecords = null);
}
