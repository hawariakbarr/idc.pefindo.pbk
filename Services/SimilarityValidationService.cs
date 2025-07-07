using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using System.Data;
using System.Data.Common;

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

    public async Task<SimilarityValidationResult> ValidateSearchSimilarityAsync(
        IndividualRequest inputData,
        PefindoSearchData searchData,
        string appNo,
        double nameThreshold)
    {
        try
        {
            _logger.LogDebug("Validating search similarity for app_no: {AppNo} using database: {Database}",
                appNo, DatabaseKeys.En);

            // Use idc.en database for similarity validation
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.En);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT is_match, name_similarity, status, message 
                FROM public.chksimilarity_v4_param(
                    @p_ktp, @p_fullname, @p_dateofbirth, @p_app_no,
                    @p_ktp_source, @p_fullname_source, @p_dateofbirth_source, @p_name_threshold
                )";

            AddParameter(command, "@p_ktp", inputData.IdNumber);
            AddParameter(command, "@p_fullname", inputData.Name);
            AddParameter(command, "@p_dateofbirth", inputData.DateOfBirth);
            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_ktp_source", searchData.IdNo);
            AddParameter(command, "@p_fullname_source", searchData.NamaDebitur);
            AddParameter(command, "@p_dateofbirth_source", searchData.TanggalLahir);
            AddParameter(command, "@p_name_threshold", nameThreshold);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new SimilarityValidationResult
                {
                    IsMatch = reader.GetBoolean("is_match"),
                    NameSimilarity = reader.GetDecimal("name_similarity").ToDouble(),
                    Status = reader.GetString("status"),
                    Message = reader.GetString("message")
                };

                _logger.LogInformation("Search similarity validation completed for {AppNo} using {Database}. Match: {IsMatch}, Similarity: {Similarity}",
                    appNo, DatabaseKeys.En, result.IsMatch, result.NameSimilarity);

                return result;
            }

            throw new InvalidOperationException("No result returned from similarity validation function");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating search similarity for app_no: {AppNo} using database: {Database}",
                appNo, DatabaseKeys.En);
            throw;
        }
    }

    public async Task<SimilarityValidationResult> ValidateReportSimilarityAsync(
        IndividualRequest inputData, 
        PefindoDebiturInfo reportData, 
        string appNo, 
        double nameThreshold, 
        double motherNameThreshold)
    {
        try
        {
            _logger.LogDebug("Validating report similarity for app_no: {AppNo} using database: {Database}",
                appNo, DatabaseKeys.En);

            // Use idc.en database for similarity validation
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.En);
            using var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT is_match, name_similarity, mother_name_similarity, status, message 
                FROM public.chksimilarity_custrpt_v4_param(
                    @p_ktp, @p_fullname, @p_dateofbirth, @p_mothername, @p_app_no,
                    @p_ktp_source, @p_fullname_source, @p_dateofbirth_source, @p_mothername_source,
                    @p_name_threshold, @p_mother_threshold
                )";
            
            AddParameter(command, "@p_ktp", inputData.IdNumber);
            AddParameter(command, "@p_fullname", inputData.Name);
            AddParameter(command, "@p_dateofbirth", inputData.DateOfBirth);
            AddParameter(command, "@p_mothername", inputData.MotherName);
            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_ktp_source", reportData.IdPefindo.ToString());
            AddParameter(command, "@p_fullname_source", reportData.NamaDebitur);
            AddParameter(command, "@p_dateofbirth_source", reportData.TanggalLahir);
            AddParameter(command, "@p_mothername_source", reportData.NamaGadisIbuKandung);
            AddParameter(command, "@p_name_threshold", nameThreshold);
            AddParameter(command, "@p_mother_threshold", motherNameThreshold);
            
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var result = new SimilarityValidationResult
                {
                    IsMatch = reader.GetBoolean("is_match"),
                    NameSimilarity = reader.GetDecimal("name_similarity").ToDouble(),
                    MotherNameSimilarity = reader.GetDecimal("mother_name_similarity").ToDouble(),
                    Status = reader.GetString("status"),
                    Message = reader.GetString("message")
                };
                
                _logger.LogInformation("Report similarity validation completed for {AppNo}. Match: {IsMatch}, Name: {NameSim}, Mother: {MotherSim}",
                    appNo, result.IsMatch, result.NameSimilarity, result.MotherNameSimilarity);
                
                return result;
            }
            
            throw new InvalidOperationException("No result returned from similarity validation function");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating report similarity for app_no: {AppNo}", appNo);
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
