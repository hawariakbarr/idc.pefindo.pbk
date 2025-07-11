using idc.pefindo.pbk.Models;
using System.Text.Json.Nodes;

namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service for aggregating and mapping Pefindo data to final response format
/// </summary>
public interface IDataAggregationService
{
    /// <summary>
    /// Aggregates all Pefindo data into final individual response
    /// </summary>
    /// <param name="request">Original request data</param>
    /// <param name="searchResponse">Search results from Pefindo</param>
    /// <param name="reportResponse">Report data from Pefindo</param>
    /// <param name="processingContext">Additional processing context</param>
    /// <returns>Complete individual response</returns>
    Task<IndividualData> AggregateIndividualDataAsync(
        IndividualRequest request,
        PefindoSearchResponse searchResponse,
        PefindoGetReportResponse reportResponse,
        ProcessingContext processingContext);

    /// <summary>
    /// Aggregates all Pefindo data into final individual response using JsonNode for flexible object handling
    /// </summary>
    /// <param name="request">Original request data</param>
    /// <param name="searchResponse">Search results from Pefindo</param>
    /// <param name="reportResponseJson">Report data from Pefindo as JsonNode</param>
    /// <param name="processingContext">Additional processing context</param>
    /// <returns>Complete individual response</returns>
    Task<IndividualData> AggregateIndividualDataWithJsonAsync(
        IndividualRequest request,
        PefindoSearchResponse searchResponse,
        JsonNode? reportResponseJson,
        ProcessingContext processingContext);
}

/// <summary>
/// Context information for data aggregation
/// </summary>
public class ProcessingContext
{
    public string EventId { get; set; } = string.Empty;
    public string? PdfPath { get; set; }
    public DateTime ProcessingStartTime { get; set; }
    public List<string> ProcessingSteps { get; set; } = new();
}
