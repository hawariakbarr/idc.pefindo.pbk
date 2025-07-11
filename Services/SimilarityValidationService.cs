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
                var result = new SimilarityValidationResult();

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

                //TODO : Implement the logic for handling different ReturnData values
                //TODO : Adjust the logic based on the actual database function implementation
                if (tempResult.ReturnData == 1)
                {
                    var result = new SimilarityValidationResult
                    {
                        IsMatch = true,
                        NameSimilarity = tempResult.Result,
                        Status = "Success",
                        Message = "Search similarity check completed successfully"
                    };

                    _logger.LogInformation("Search similarity validation completed for {AppNo} using {Database}. Match: {IsMatch}, Similarity: {Similarity}",
                    appNo, DatabaseKeys.En, result.IsMatch, result.NameSimilarity);

                    return result;
                }
                else
                {
                    _logger.LogWarning("chksimilarity_v4_param returned no match for app_no: {AppNo} | retrundata: {ReturnData} | result: {Result}",
                        appNo, tempResult.ReturnData, tempResult.Result);
                }
            }

            throw new InvalidOperationException("No result returned from chksimilarity_v4_param");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling chksimilarity_v4_param for app_no: {AppNo}", appNo);
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
            AddParameter(command, "@p_ktp_source", reportData.IdDebiturGoldenRecord.ToString());
            AddParameter(command, "@p_fullname_source", reportData.NamaLengkapDebitur);
            AddParameter(command, "@p_dateofbirth_source", reportData.TanggalLahir);
            AddParameter(command, "@p_mothername_source", reportData.NamaGadisIbuKandung);
            AddParameter(command, "@p_mothername", inputData.MotherName);
            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_ktp_source", reportData.IdDebiturGoldenRecord.ToString());
            AddParameter(command, "@p_fullname_source", reportData.NamaLengkapDebitur);
            AddParameter(command, "@p_dateofbirth_source", reportData.TanggalLahir);
            AddParameter(command, "@p_mothername_source", reportData.NamaGadisIbuKandung);
            AddParameter(command, "@p_name_threshold", nameThreshold);
            AddParameter(command, "@p_mother_threshold", motherNameThreshold);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new SimilarityValidationResult();

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

    public async Task<SimilarityValidationResult> ValidateReportSimilarityAsync(
        string ktp,
        string fullname,
        string dateOfBirth,
        string motherName,
        string appNo,
        PefindoReportData reportData,
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
            AddParameter(command, "@p_ktp_source", reportData.Header.Ktp);
            AddParameter(command, "@p_fullname_source", reportData.Debitur.NamaLengkapDebitur);
            AddParameter(command, "@p_dateofbirth_source", reportData.Debitur.TanggalLahir);
            AddParameter(command, "@p_mothername_source", reportData.Debitur.NamaGadisIbuKandung);
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

                //TODO : Implement the logic for handling different ReturnData values
                //TODO : Adjust the logic based on the actual database function implementation

                if (tempResult.ReturnData == 1)
                {
                    var result = new SimilarityValidationResult
                    {
                        IsMatch = true,
                        NameSimilarity = tempResult.Result,
                        MotherNameSimilarity = tempResult.Result,
                        Status = "Success",
                        Message = "Report similarity check completed successfully"
                    };

                    _logger.LogInformation("Report similarity validation completed for {AppNo}. Match: {IsMatch}, Name: {NameSim}, Mother: {MotherSim}",
                        appNo, result.IsMatch, result.NameSimilarity, result.MotherNameSimilarity);

                    return result;
                }
                else
                {
                    _logger.LogWarning("Report similarity validation returned no match for app_no: {AppNo} | retrundata: {ReturnData} | result: {Result}",
                        appNo, tempResult.ReturnData, tempResult.Result);
                }
            }

            throw new InvalidOperationException("No result returned from chksimilarity_custrpt_v4_param");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling chksimilarity_custrpt_v4_param for app_no: {AppNo}", appNo);
            throw;
        }
    }

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

                //TODO : Implement the logic for handling different ReturnData values
                //TODO : Adjust the logic based on the actual database function implementation

                if (tempResult.ReturnData == 1)
                {
                    var result = new SimilarityValidationResult
                    {
                        IsMatch = true,
                        NameSimilarity = tempResult.Result,
                        MotherNameSimilarity = tempResult.Result,
                        Status = "Success",
                        Message = "Report similarity check completed successfully"
                    };

                    _logger.LogInformation("Report similarity validation completed for {AppNo}. Match: {IsMatch}, Name: {NameSim}, Mother: {MotherSim}",
                        appNo, result.IsMatch, result.NameSimilarity, result.MotherNameSimilarity);

                    return result;
                }
                else
                {
                    _logger.LogWarning("Report similarity validation returned no match for app_no: {AppNo} | retrundata: {ReturnData} | result: {Result}",
                        appNo, tempResult.ReturnData, tempResult.Result);
                }
            }

            throw new InvalidOperationException("No result returned from chksimilarity_custrpt_v4_param");
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
