using idc.pefindo.pbk.Models;

namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service for performing similarity validation between input data and search results
/// </summary>
public interface ISimilarityValidationService
{
    /// <summary>
    /// Validates similarity for smart search results
    /// </summary>
    /// <param name="inputData">Original input data</param>
    /// <param name="searchData">Search result data from Pefindo</param>
    /// <param name="appNo">Application number for logging</param>
    /// <param name="nameThreshold">Name similarity threshold</param>
    /// <returns>Similarity validation result</returns>
    Task<SimilarityValidationResult> ValidateSearchSimilarityAsync(
        IndividualRequest inputData, 
        PefindoSearchData searchData, 
        string appNo, 
        double nameThreshold);
    
    /// <summary>
    /// Validates similarity for custom report data
    /// </summary>
    /// <param name="inputData">Original input data</param>
    /// <param name="reportData">Report data from Pefindo</param>
    /// <param name="appNo">Application number for logging</param>
    /// <param name="nameThreshold">Name similarity threshold</param>
    /// <param name="motherNameThreshold">Mother name similarity threshold</param>
    /// <returns>Similarity validation result</returns>
    Task<SimilarityValidationResult> ValidateReportSimilarityAsync(
        IndividualRequest inputData, 
        PefindoDebiturInfo reportData, 
        string appNo, 
        double nameThreshold, 
        double motherNameThreshold);
}

/// <summary>
/// Result of similarity validation
/// </summary>
public class SimilarityValidationResult
{
    public bool IsMatch { get; set; }
    public double NameSimilarity { get; set; }
    public double MotherNameSimilarity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
