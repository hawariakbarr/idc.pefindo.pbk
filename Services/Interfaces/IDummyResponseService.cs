using System.Text.Json;

namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service interface for loading and managing dummy API responses
/// </summary>
public interface IDummyResponseService
{
    /// <summary>
    /// Load dummy responses from file
    /// </summary>
    Task LoadDummyResponsesAsync();

    /// <summary>
    /// Get dummy response for getToken based on scenario
    /// </summary>
    string GetTokenResponse(string scenario = "success");

    /// <summary>
    /// Get dummy response for validateToken based on scenario
    /// </summary>
    string GetValidateTokenResponse(string scenario = "success");

    /// <summary>
    /// Get dummy response for search based on scenario
    /// </summary>
    string GetSearchResponse(string scenario = "perfectMatch");

    /// <summary>
    /// Get dummy response for generateReport based on scenario
    /// </summary>
    string GetGenerateReportResponse(string scenario = "success");

    /// <summary>
    /// Get dummy response for getReport based on scenario
    /// </summary>
    string GetReportResponse(string scenario = "successComplete");

    /// <summary>
    /// Get dummy response for downloadReport based on scenario
    /// </summary>
    string GetDownloadReportResponse(string scenario = "success");

    /// <summary>
    /// Get dummy response for downloadPdfReport
    /// </summary>
    byte[] GetPdfReportResponse(string scenario = "success");

    /// <summary>
    /// Get dummy response for bulk operations
    /// </summary>
    string GetBulkResponse(string scenario = "success");

    /// <summary>
    /// Check if dummy responses are loaded
    /// </summary>
    bool IsLoaded { get; }
}