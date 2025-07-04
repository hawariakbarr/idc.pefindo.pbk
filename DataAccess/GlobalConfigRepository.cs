using System.Data;
using System.Data.Common;
using idc.pefindo.pbk.DataAccess;

namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Implementation of global configuration repository
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
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();

            command.CommandText = "SELECT * FROM public.api_checkglobalconfig(@p_code)";
            command.CommandType = CommandType.Text;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@p_code";
            parameter.Value = configCode;
            command.Parameters.Add(parameter);

            // Cast to DbCommand to access ExecuteScalarAsync
            var result = await ((DbCommand)command).ExecuteScalarAsync();

            _logger.LogDebug("Retrieved config value for {ConfigCode}: {Value}", configCode, result);
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving config value for code {ConfigCode}", configCode);
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
        
        _logger.LogDebug("Retrieved {Count} config values", result.Count);
        return result;
    }
}
