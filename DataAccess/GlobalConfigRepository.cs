using System.Data.Common;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Updated GlobalConfigRepository using configuration-based database access
/// </summary>
public class GlobalConfigRepository : IGlobalConfigRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<GlobalConfigRepository> _logger;

    public GlobalConfigRepository(IDbConnectionFactory connectionFactory, ILogger<GlobalConfigRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<string?> GetConfigValueAsync(string configCode)
    {
        try
        {
            // Use DatabaseKeys.Bk for global config
            using var connection = await _connectionFactory.CreateConnectionAsync(DatabaseKeys.Core);
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM public.api_checkglobalconfig(@p_code)";
            command.CommandType = System.Data.CommandType.Text;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p_code";
            parameter.Value = configCode;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();

            _logger.LogDebug("Retrieved config value for {ConfigCode} from {DatabaseKey}: {Value}",
                configCode, DatabaseKeys.Bk, result);
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving config value for code {ConfigCode} from {DatabaseKey}",
                configCode, DatabaseKeys.Bk);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> GetMultipleConfigsAsync(params string[] configCodes)
    {
        var result = new Dictionary<string, string>();

        foreach (var code in configCodes)
        {
            var value = await GetConfigValueAsync(code);
            if (value != null)
            {
                result[code] = value;
            }
        }

        _logger.LogDebug("Retrieved {Count} config values from {DatabaseKey}", result.Count, DatabaseKeys.Bk);
        return result;
    }
}