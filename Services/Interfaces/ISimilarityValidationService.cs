using System.Text.Json.Nodes;
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
    /// Validates similarity for custom report data with additional parameters
    /// </summary>
    /// <param name="ktp">KTP number of the individual</param>
    /// <param name="fullname">Full name of the individual</param>
    /// <param name="dateOfBirth">Date of birth of the individual</param>
    /// <param name="appNo">Application number</param>
    /// <param name="searchData">Search data</param>
    /// <param name="nameThreshold">Name similarity threshold</param>
    /// <returns>Similarity validation result</returns>
    Task<SimilarityValidationResult> ValidateSearchSimilarityAsync(
        string ktp,
        string fullname,
        string dateOfBirth,
        string appNo,
        PefindoSearchData searchData,
        double nameThreshold = 0.8);

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

    /// <summary>
    /// Validates similarity for custom report data
    /// </summary>
    /// <param name="ktp">KTP number of the individual</param>
    /// <param name="fullname">Full name of the individual</param>
    /// <param name="dateOfBirth">Date of birth of the individual</param>
    /// <param name="motherName">Mother's name of the individual</param>
    /// <param name="appNo">Application number for logging</param>
    /// <param name="reportData">Report data from Pefindo</param>
    /// <param name="nameThreshold">Name similarity threshold</param>
    /// <param name="motherThreshold">Mother name similarity threshold</param>
    /// <returns>Similarity validation result</returns>
    Task<SimilarityValidationResult> ValidateReportSimilarityAsync(
        string ktp,
        string fullname,
        string dateOfBirth,
        string motherName,
        string appNo,
        PefindoReportData reportData,
        double nameThreshold = 0.8,
        double motherThreshold = 0.9);


    /// <summary>
    /// Validates similarity for custom report data
    /// </summary>
    /// <param name="ktp">KTP number of the individual</param>
    /// <param name="fullname">Full name of the individual</param>
    /// <param name="dateOfBirth">Date of birth of the individual</param>
    /// <param name="motherName">Mother's name of the individual</param>
    /// <param name="appNo">Application number for logging</param>
    /// <param name="reportData">Report data as JsonNode</param>
    /// <param name="nameThreshold">Name similarity threshold</param>
    /// <param name="motherThreshold">Mother name similarity threshold</param>
    /// <returns>Similarity validation result</returns>
    Task<SimilarityValidationResult> ValidateReportSimilarityAsync(
        string ktp,
        string fullname,
        string dateOfBirth,
        string motherName,
        string appNo,
        JsonNode reportData,
        double nameThreshold = 0.8,
        double motherThreshold = 0.9);

}

/// <summary>
/// Result of similarity validation
/// </summary>
public class SimilarityValidationResult
{
    public bool IsMatch { get; set; }
    public string NameSimilarity { get; set; } = string.Empty;
    public string MotherNameSimilarity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}


public class SimilarityResult
{
    public short ReturnData { get; set; }
    public string Result { get; set; } = string.Empty;
}

public class SimilarityCustrptResult
{
    public short ReturnData { get; set; }
    public string Result { get; set; } = string.Empty;
}
