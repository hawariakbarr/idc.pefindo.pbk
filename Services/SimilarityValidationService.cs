using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using System.Data;
using System.Data.Common;
using System.Text.Json.Nodes;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Implementation of similarity validation service using database functions with proper async support
/// </summary>
public class SimilarityValidationService : ISimilarityValidationService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SimilarityValidationService> _logger;

    public SimilarityValidationService(
        IDbConnectionFactory connectionFactory,
        ILogger<SimilarityValidationService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <summary>
    /// Validates search similarity using the chksimilarity_v4_param function
    /// </summary>
    /// <param name="ktp">KTP number</param>
    /// <param name="fullname">Full name of the individual</param>
    /// <param name="dateOfBirth">Date of birth in string format</param>
    /// <param name="appNo">Application number</param>
    /// <param name="searchData">Search data containing source information</param>
    /// <param name="nameThreshold">Threshold for name similarity</param>
    /// <returns>SimilarityValidationResult containing match status and similarity score</returns>
    public async Task<SimilarityValidationResult> ValidateSearchSimilarityAsync(
        string ktp,
        string fullname,
        string dateOfBirth,
        string appNo,
        PefindoSearchData searchData,
        double nameThreshold = 0.8)
    {
        try
        {
            _logger.LogDebug("Calling chksimilarity_v4_param for app_no: {AppNo}", appNo);

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.En);
            using var command = connection.CreateCommand();

            command.CommandText = @"
            SELECT * FROM public.chksimilarity_v4_param(
                @p_ktp,
                @p_fullname,
                @p_dateofbirth,
                @p_app_no,
                @p_ktp_source,
                @p_fullname_source,
                @p_dateofbirth_source,
                @p_name_threshold
            )";

            AddParameter(command, "@p_ktp", ktp);
            AddParameter(command, "@p_fullname", fullname);
            AddParameter(command, "@p_dateofbirth", dateOfBirth);
            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_ktp_source", searchData.IdNo);
            AddParameter(command, "@p_fullname_source", searchData.NamaDebitur);
            AddParameter(command, "@p_dateofbirth_source", searchData.TanggalLahir);
            AddParameter(command, "@p_name_threshold", nameThreshold);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var tempResult = new SimilarityResult
                {
                    ReturnData = reader.GetInt16(reader.GetOrdinal("retrundata")),
                    Result = reader.GetString(reader.GetOrdinal("result"))
                };

                // Handle different ReturnData values based on business logic
                var result = tempResult.ReturnData switch
                {
                    1 => new SimilarityValidationResult
                    {
                        IsMatch = true,
                        NameSimilarity = tempResult.Result,
                        Status = "Success",
                        Message = "Search similarity check completed successfully - data matches"
                    },
                    -1 => new SimilarityValidationResult
                    {
                        IsMatch = false,
                        NameSimilarity = tempResult.Result,
                        Status = "No Match",
                        Message = "Search similarity check completed - data does not match similarity threshold"
                    },
                    _ => new SimilarityValidationResult
                    {
                        IsMatch = false,
                        NameSimilarity = tempResult.Result,
                        Status = "Invalid Data",
                        Message = "Search similarity check failed - invalid input data"
                    }
                };

                _logger.LogInformation("Search similarity validation completed for {AppNo} using {Database}. ReturnData: {ReturnData}, Match: {IsMatch}, Similarity: {Similarity}",
                    appNo, DatabaseKeys.En, tempResult.ReturnData, result.IsMatch, result.NameSimilarity);

                return result;
            }

            // Only throw exception if no result from database (true error condition)
            throw new InvalidOperationException($"Database function chksimilarity_v4_param returned no data for app_no: {appNo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling chksimilarity_v4_param for app_no: {AppNo}", appNo);
            throw;
        }
    }

    /// <summary>
    /// Validates report similarity using the chksimilarity_custrpt_v4_param function
    /// /// </summary>
    /// <param name="ktp">KTP number</param>
    /// <param name="fullname">Full name of the individual</param>
    /// <param name="dateOfBirth">Date of birth in string format</param>
    /// <param name="motherName">Mother's name</param>
    /// <param name="appNo">Application number</param>
    /// <param name="reportData">Report data in JSON format</param>
    /// <param name="nameThreshold">Threshold for name similarity</param>
    /// <param name="motherThreshold">Threshold for mother's name similarity</param>
    /// <returns>SimilarityValidationResult containing match status and similarity scores</returns>
    public async Task<SimilarityValidationResult> ValidateReportSimilarityAsync(
        string ktp,
        string fullname,
        string dateOfBirth,
        string motherName,
        string appNo,
        JsonNode reportData,
        double nameThreshold = 0.8,
        double motherThreshold = 0.9)
    {
        try
        {
            _logger.LogDebug("Calling chksimilarity_custrpt_v4_param for app_no: {AppNo}", appNo);

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.En);
            using var command = connection.CreateCommand();

            command.CommandText = @"
            SELECT * FROM public.chksimilarity_custrpt_v4_param(
                @p_ktp,
                @p_fullname,
                @p_dateofbirth,
                @p_mothername,
                @p_app_no,
                @p_ktp_source,
                @p_fullname_source,
                @p_dateofbirth_source,
                @p_mothername_source,
                @p_name_threshold,
                @p_mother_threshold
            )";

            AddParameter(command, "@p_ktp", ktp);
            AddParameter(command, "@p_fullname", fullname);
            AddParameter(command, "@p_dateofbirth", dateOfBirth);
            AddParameter(command, "@p_mothername", motherName);
            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_ktp_source", reportData["report"]?["header"]?["ktp"]?.ToString() ?? string.Empty);
            AddParameter(command, "@p_fullname_source", reportData["report"]?["debitur"]?["nama_lengkap_debitur"]?.ToString() ?? string.Empty);

            // the value from these field will be = "1990-03-01T00:00:00Z"
            // so we need to convert it to DateTime
            if (DateTime.TryParse(reportData["report"]?["debitur"]?["tanggal_lahir"]?.ToString(), out var dateOfBirthValue))
            {
                AddParameter(command, "@p_dateofbirth_source", dateOfBirthValue.ToString("yyyy-MM-dd"));
            }
            else
            {
                AddParameter(command, "@p_dateofbirth_source", string.Empty);
            }

            AddParameter(command, "@p_mothername_source", reportData["report"]?["debitur"]?["nama_gadis_ibu_kandung"]?.ToString() ?? string.Empty);
            AddParameter(command, "@p_name_threshold", nameThreshold);
            AddParameter(command, "@p_mother_threshold", motherThreshold);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var tempResult = new SimilarityCustrptResult
                {
                    ReturnData = reader.GetInt16(reader.GetOrdinal("retrundata")),
                    Result = reader.GetString(reader.GetOrdinal("result"))
                };

                // Handle different ReturnData values based on business logic
                var result = tempResult.ReturnData switch
                {
                    1 => new SimilarityValidationResult
                    {
                        IsMatch = true,
                        NameSimilarity = tempResult.Result,
                        MotherNameSimilarity = tempResult.Result,
                        Status = "Success",
                        Message = "Report similarity check completed successfully - data matches"
                    },
                    -1 => new SimilarityValidationResult
                    {
                        IsMatch = false,
                        NameSimilarity = tempResult.Result,
                        MotherNameSimilarity = tempResult.Result,
                        Status = "No Match",
                        Message = "Report similarity check completed - data does not match similarity threshold"
                    },
                    _ => new SimilarityValidationResult
                    {
                        IsMatch = false,
                        NameSimilarity = tempResult.Result,
                        MotherNameSimilarity = tempResult.Result,
                        Status = "Invalid Data",
                        Message = "Report similarity check failed - invalid input data"
                    }
                };

                _logger.LogInformation("Report similarity validation completed for {AppNo}. ReturnData: {ReturnData}, Match: {IsMatch}, Name: {NameSim}, Mother: {MotherSim}",
                    appNo, tempResult.ReturnData, result.IsMatch, result.NameSimilarity, result.MotherNameSimilarity);

                return result;
            }

            // Only throw exception if no result from database (true error condition)
            throw new InvalidOperationException($"Database function chksimilarity_custrpt_v4_param returned no data for app_no: {appNo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling chksimilarity_custrpt_v4_param for app_no: {AppNo}", appNo);
            throw;
        }
    }


    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}

/// <summary>
/// Extension method for converting decimal to double
/// </summary>
public static class DecimalExtensions
{
    public static double ToDouble(this decimal value)
    {
        return (double)value;
    }
}
