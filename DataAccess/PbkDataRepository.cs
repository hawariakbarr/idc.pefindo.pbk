using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Nodes;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Utilities;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Implementation of PBK data repository with proper async support
/// </summary>
public class PbkDataRepository : IPbkDataRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PbkDataRepository> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PbkDataRepository(IDbConnectionFactory connectionFactory, ILogger<PbkDataRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> StoreSearchResultAsync(string appNo, int inquiryId, PefindoSearchResponse searchResponse)
    {
        try
        {
            _logger.LogDebug("Storing search result for app_no: {AppNo}, inquiry_id: {InquiryId}", appNo, inquiryId);

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            var searchJson = JsonSerializer.Serialize(searchResponse, _jsonOptions);
            var similarityScore = searchResponse.Data.FirstOrDefault()?.SimilarityScore ?? 0;

            command.CommandText = @"
                INSERT INTO pefindo.bk_search_results
                (app_no, inquiry_id, search_data, response_status, similarity_score, created_date, updated_date)
                VALUES (@app_no, @inquiry_id, @search_data::jsonb, @response_status, @similarity_score, @created_date, @updated_date)
                ON CONFLICT (app_no, inquiry_id) DO UPDATE SET
                    search_data = @search_data::jsonb,
                    response_status = @response_status,
                    similarity_score = @similarity_score,
                    updated_date = @updated_date
                RETURNING id";

            AddParameter(command, "@app_no", appNo);
            AddParameter(command, "@inquiry_id", inquiryId);
            AddParameter(command, "@search_data", searchJson);
            AddParameter(command, "@response_status", searchResponse.ResponseStatus);
            AddParameter(command, "@similarity_score", similarityScore);
            AddParameter(command, "@created_date", DateTime.UtcNow);
            AddParameter(command, "@updated_date", DateTime.UtcNow);

            var result = await command.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);

            _logger.LogInformation("Search result stored with ID: {Id} for app_no: {AppNo}", id, appNo);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing search result for app_no: {AppNo}", appNo);
            throw;
        }
    }

    public async Task<int> StoreReportDataAsync(string eventId, string appNo, int inquiryId, PefindoGetReportResponse reportResponse, string? pdfPath = null)
    {
        try
        {
            _logger.LogDebug("Storing report data for event_id: {EventId}, app_no: {AppNo}", eventId, appNo);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            var reportJson = JsonSerializer.Serialize(reportResponse, _jsonOptions);

            command.CommandText = @"
                INSERT INTO public.pbk_report_data
                (event_id, app_no, inquiry_id, report_data, report_status, report_type, pdf_path, created_date, updated_date)
                VALUES (@event_id, @app_no, @inquiry_id, @report_data::jsonb, @report_status, @report_type, @pdf_path, @created_date, @updated_date)
                ON CONFLICT (event_id) DO UPDATE SET
                    report_data = @report_data::jsonb,
                    report_status = @report_status,
                    pdf_path = @pdf_path,
                    updated_date = @updated_date
                RETURNING id";

            AddParameter(command, "@event_id", eventId);
            AddParameter(command, "@app_no", appNo);
            AddParameter(command, "@inquiry_id", inquiryId);
            AddParameter(command, "@report_data", reportJson);
            AddParameter(command, "@report_status", reportResponse.Status?.ToUpper() ?? "UNKNOWN");
            AddParameter(command, "@report_type", "JSON");
            AddParameter(command, "@pdf_path", pdfPath);
            AddParameter(command, "@created_date", DateTime.UtcNow);
            AddParameter(command, "@updated_date", DateTime.UtcNow);

            var result = await command.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);

            _logger.LogInformation("Report data stored with ID: {Id} for event_id: {EventId}", id, eventId);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing report data for event_id: {EventId}", eventId);
            throw;
        }
    }

    public async Task<int> StoreReportDataWithJsonAsync(string eventId, string appNo, int inquiryId, JsonNode? reportResponse, string? pdfPath = null)
    {
        try
        {
            _logger.LogDebug("Storing report data for event_id: {EventId}, app_no: {AppNo}", eventId, appNo);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            var reportJson = JsonSerializer.Serialize(reportResponse, _jsonOptions);

            command.CommandText = @"
                INSERT INTO pefindo.bk_report_data
                (event_id, app_no, inquiry_id, report_data, report_status, report_type, pdf_path, created_date, updated_date)
                VALUES (@event_id, @app_no, @inquiry_id, @report_data::jsonb, @report_status, @report_type, @pdf_path, @created_date, @updated_date)
                ON CONFLICT (event_id) DO UPDATE SET
                    report_data = @report_data::jsonb,
                    report_status = @report_status,
                    pdf_path = @pdf_path,
                    updated_date = @updated_date
                RETURNING id";

            AddParameter(command, "@event_id", eventId);
            AddParameter(command, "@app_no", appNo);
            AddParameter(command, "@inquiry_id", inquiryId);
            AddParameter(command, "@report_data", reportJson);
            AddParameter(command, "@report_status", reportResponse?["report"]?["header"]?["status"]?.ToString().ToUpper() ?? "UNKNOWN");
            AddParameter(command, "@report_type", "JSON");
            AddParameter(command, "@pdf_path", pdfPath);
            AddParameter(command, "@created_date", DateTime.UtcNow);
            AddParameter(command, "@updated_date", DateTime.UtcNow);

            var result = await command.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);

            _logger.LogInformation("Report data stored with ID: {Id} for event_id: {EventId}", id, eventId);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing report data for event_id: {EventId}", eventId);
            throw;
        }
    }

    public async Task<int> StoreSummaryDataAsync(string appNo, IndividualData summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null)
    {
        try
        {
            _logger.LogDebug("Storing summary data for app_no: {AppNo}", appNo);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            var summaryJson = JsonSerializer.Serialize(summaryData, _jsonOptions);

            command.CommandText = @"
                INSERT INTO pefindo.bk_summary_data
                (app_no, pefindo_id, search_id, event_id, summary_data, max_overdue, max_overdue_last12months,
                 score, total_facilities, status, response_status, response_message, created_date, updated_date)
                VALUES (@app_no, @pefindo_id, @search_id, @event_id, @summary_data::jsonb, @max_overdue, @max_overdue_last12months,
                        @score, @total_facilities, @status, @response_status, @response_message, @created_date, @updated_date)
                ON CONFLICT (app_no) DO UPDATE SET
                    summary_data = @summary_data::jsonb,
                    max_overdue = @max_overdue,
                    max_overdue_last12months = @max_overdue_last12months,
                    score = @score,
                    total_facilities = @total_facilities,
                    status = @status,
                    response_status = @response_status,
                    response_message = @response_message,
                    updated_date = @updated_date
                RETURNING id";

            AddParameter(command, "@app_no", appNo);
            AddParameter(command, "@pefindo_id", pefindoId);
            AddParameter(command, "@search_id", searchId);
            AddParameter(command, "@event_id", eventId);
            AddParameter(command, "@summary_data", summaryJson);
            AddParameter(command, "@max_overdue", summaryData.MaxOverdue);
            AddParameter(command, "@max_overdue_last12months", summaryData.MaxOverdueLast12Months);
            AddParameter(command, "@score", summaryData.Score);
            AddParameter(command, "@total_facilities", summaryData.TotalFacilities);
            AddParameter(command, "@status", summaryData.Status);
            AddParameter(command, "@response_status", summaryData.ResponseStatus);
            AddParameter(command, "@response_message", summaryData.ResponseMessage);
            AddParameter(command, "@created_date", DateTime.UtcNow);
            AddParameter(command, "@updated_date", DateTime.UtcNow);

            var result = await command.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);

            _logger.LogInformation("Summary data stored with ID: {Id} for app_no: {AppNo}", id, appNo);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing summary data for app_no: {AppNo}", appNo);
            throw;
        }
    }

    public async Task<int> StoreSummaryDataWithJsonAsync(string appNo, JsonNode? summaryData, string? pefindoId = null, string? searchId = null, string? eventId = null)
    {
        try
        {
            _logger.LogDebug("Storing summary data for app_no: {AppNo}", appNo);

            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            var summaryJson = JsonSerializer.Serialize(summaryData, _jsonOptions);

            command.CommandText = @"
                INSERT INTO pefindo.bk_summary_data
                (app_no, pefindo_id, search_id, event_id, summary_data, max_overdue, max_overdue_last12months,
                 score, total_facilities, status, response_status, response_message, created_date, updated_date)
                VALUES (@app_no, @pefindo_id, @search_id, @event_id, @summary_data::jsonb, @max_overdue, @max_overdue_last12months,
                        @score, @total_facilities, @status, @response_status, @response_message, @created_date, @updated_date)
                ON CONFLICT (app_no) DO UPDATE SET
                    summary_data = @summary_data::jsonb,
                    max_overdue = @max_overdue,
                    max_overdue_last12months = @max_overdue_last12months,
                    score = @score,
                    total_facilities = @total_facilities,
                    status = @status,
                    response_status = @response_status,
                    response_message = @response_message,
                    updated_date = @updated_date
                RETURNING id";

            AddParameter(command, "@app_no", appNo);
            AddParameter(command, "@pefindo_id", pefindoId);
            AddParameter(command, "@search_id", searchId);
            AddParameter(command, "@event_id", eventId);
            AddParameter(command, "@summary_data", summaryJson);

            // Fix parameter extraction based on IndividualData model JSON property names
            AddParameter(command, "@max_overdue", Helper.SafeGetStringAsDouble(summaryData?["max_overdue"]));
            AddParameter(command, "@max_overdue_last12months", Helper.SafeGetStringAsDouble(summaryData?["max_overdue_last12months"]));
            AddParameter(command, "@score", Helper.SafeGetStringAsDouble(summaryData?["score"]));
            AddParameter(command, "@total_facilities", Helper.SafeGetStringAsInt(summaryData?["total_facilities"]));
            AddParameter(command, "@status", Helper.SafeGetString(summaryData?["status"]));
            AddParameter(command, "@response_status", Helper.SafeGetString(summaryData?["response_status"]));
            AddParameter(command, "@response_message", Helper.SafeGetString(summaryData?["response_message"]));
            AddParameter(command, "@created_date", DateTime.UtcNow);
            AddParameter(command, "@updated_date", DateTime.UtcNow);

            var result = await command.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);

            _logger.LogInformation("Summary data stored with ID: {Id} for app_no: {AppNo}", id, appNo);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing summary data for app_no: {AppNo}", appNo);
            throw;
        }
    }

    public async Task<IndividualData?> GetSummaryDataAsync(string appNo)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT summary_data
                FROM public.pbk_summary_data
                WHERE app_no = @app_no";

            AddParameter(command, "@app_no", appNo);

            var result = await command.ExecuteScalarAsync();
            if (result != null)
            {
                var summaryData = JsonSerializer.Deserialize<IndividualData>(result.ToString()!, _jsonOptions);
                return summaryData;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving summary data for app_no: {AppNo}", appNo);
            throw;
        }
    }

    public async Task<int> LogProcessingStepAsync(string appNo, string stepName, string status, JsonDocument? stepData = null, string? errorMessage = null, int? processingTimeMs = null)
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT pefindo.log_processing_step(
                    @p_app_no, @p_step_name, @p_step_status, @p_step_data, @p_error_message, @p_processing_time_ms
                )";

            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_step_name", stepName);
            AddParameter(command, "@p_step_status", status);
            AddParameter(command, "@p_step_data", stepData?.RootElement.GetRawText());
            AddParameter(command, "@p_error_message", errorMessage);
            AddParameter(command, "@p_processing_time_ms", processingTimeMs);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging processing step {StepName} for app_no: {AppNo}", stepName, appNo);
            throw;
        }
    }

    public async Task<List<ProcessingLogEntry>> GetProcessingLogAsync(string appNo)
    {
        try
        {
            var logEntries = new List<ProcessingLogEntry>();

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT * FROM public.get_processing_summary(@p_app_no)";

            AddParameter(command, "@p_app_no", appNo);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                logEntries.Add(new ProcessingLogEntry
                {
                    StepName = reader.GetString("step_name"),
                    StepStatus = reader.GetString("step_status"),
                    ErrorMessage = reader.IsDBNull("error_message") ? null : reader.GetString("error_message"),
                    ProcessingTimeMs = reader.IsDBNull("processing_time_ms") ? null : reader.GetInt32("processing_time_ms"),
                    CreatedDate = reader.GetDateTime("created_date")
                });
            }

            return logEntries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving processing log for app_no: {AppNo}", appNo);
            throw;
        }
    }

    public async Task<PbkInfoIdentity?> GetPbkInfoIdentityWithEncryptionAsync(string idType, string idNo, string encryptKey)
    {
        try
        {
            _logger.LogDebug("Getting PBK info identity with encryption for id_type: {IdType}, id_no: {IdNo}", idType, idNo);

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT pf_id, pf_brw_code, pf_identity_type, pf_identity_no, pf_req_date, pf_res_date, pf_status, pf_req_usr
                FROM pefindo.pfd_get_pbkinfo_identity_v2_encrypt(@p_idtype, @p_idno, @p_encrypt_key)";

            AddParameter(command, "@p_idtype", idType);
            AddParameter(command, "@p_idno", idNo);
            AddParameter(command, "@p_encrypt_key", encryptKey);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new PbkInfoIdentity
                {
                    PfId = reader.GetInt64("pf_id"),
                    PfBrwCode = reader.GetString("pf_brw_code"),
                    PfIdentityType = reader.GetString("pf_identity_type"),
                    PfIdentityNo = reader.GetString("pf_identity_no"),
                    PfReqDate = reader.GetDateTime("pf_req_date"),
                    PfResDate = reader.GetDateTime("pf_res_date"),
                    PfStatus = reader.GetInt32("pf_status"),
                    PfReqUsr = reader.GetString("pf_req_usr")
                };

                _logger.LogDebug("Found PBK info identity: pf_id={PfId}, pf_req_date={PfReqDate}", result.PfId, result.PfReqDate);
                return result;
            }

            _logger.LogDebug("No PBK info identity found for id_type: {IdType}, id_no: {IdNo}", idType, idNo);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PBK info identity with encryption for id_type: {IdType}, id_no: {IdNo}", idType, idNo);
            throw;
        }
    }

    public async Task<SummaryPeroranganIdentity?> GetSummaryPeroranganIdentityWithEncryptionAsync(string idNo, string encryptKey)
    {
        try
        {
            _logger.LogDebug("Getting summary perorangan identity with encryption for ktp: {Ktp}", idNo);

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"
                SELECT isp_id, isp_app_no, isp_p_id, isp_search_id, isp_created_date
                FROM pefindo.pfd_get_summary_perorangan_identity_encrypt(@p_idno, @p_encrypt_key)";

            AddParameter(command, "@p_idno", idNo);
            AddParameter(command, "@p_encrypt_key", encryptKey);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var result = new SummaryPeroranganIdentity
                {
                    IspId = reader.GetInt64("isp_id"),
                    IspAppNo = reader.GetString("isp_app_no"),
                    IspPId = reader.GetInt64("isp_p_id"),
                    IspSearchId = reader.GetString("isp_search_id"),
                    IspCreatedDate = reader.GetDateTime("isp_created_date")
                };

                _logger.LogDebug("Found summary perorangan identity: isp_id={IspId}, isp_created_date={IspCreatedDate}", result.IspId, result.IspCreatedDate);
                return result;
            }

            _logger.LogDebug("No summary perorangan identity found for id_no: {IdNo}", idNo);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting summary perorangan identity with encryption for id_no: {IdNo}", idNo);
            throw;
        }
    }

    public async Task<JsonNode?> DuplicateAndGetSummaryData(string appNo, string idNo, string encryptKey)
    {
        try
        {
            _logger.LogDebug("Getting individual data for app_no: {AppNo} and id_no: {IdNo} and encrypt_key: {EncryptKey}", appNo, idNo, encryptKey);

            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Bk);
            using var command = connection.CreateCommand();

            command.CommandText = @"SELECT * FROM pefindo.duplicate_summary_encrypt(@p_app_no, @p_identity_no, @p_encrypt_key)";

            AddParameter(command, "@p_app_no", appNo);
            AddParameter(command, "@p_identity_no", idNo);
            AddParameter(command, "@p_encrypt_key", encryptKey);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ConvertDataReaderRowToJson(reader);
            }

            _logger.LogDebug("No individual data found for app_no: {AppNo}", appNo);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting individual data for app_no: {AppNo}", appNo);
            throw;
        }
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    /// <summary>
    /// Dynamically converts DataReader row to JsonObject
    /// </summary>
    private static JsonObject ConvertDataReaderRowToJson(DbDataReader reader)
    {
        var jsonObject = new JsonObject();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            var columnName = reader.GetName(i);

            if (reader.IsDBNull(i))
            {
                jsonObject[columnName] = null;
            }
            else
            {
                var value = reader.GetValue(i);

                // Convert value based on its type for proper JSON serialization
                jsonObject[columnName] = value switch
                {
                    DateTime dt => dt,
                    decimal dc => dc,
                    double dbl => dbl,
                    float flt => flt,
                    long lg => lg,
                    int intValue => intValue,
                    short st => st,
                    bool bl => bl,
                    string sg => sg,
                    byte[] bytes => Convert.ToBase64String(bytes),
                    _ => value?.ToString()
                };
            }
        }

        return jsonObject;
    }

}
