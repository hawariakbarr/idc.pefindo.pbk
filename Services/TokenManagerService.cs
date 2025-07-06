using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Configuration;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Token management service with database caching and proper async support
/// </summary>
public class TokenManagerService : ITokenManagerService
{
    private readonly IPefindoApiService _pefindoApiService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IGlobalConfigRepository _globalConfigRepository;
    private readonly ILogger<TokenManagerService> _logger;
    
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public TokenManagerService(
        IPefindoApiService pefindoApiService,
        IDbConnectionFactory connectionFactory,
        IGlobalConfigRepository globalConfigRepository,
        ILogger<TokenManagerService> logger)
    {
        _pefindoApiService = pefindoApiService;
        _connectionFactory = connectionFactory;
        _globalConfigRepository = globalConfigRepository;
        _logger = logger;
    }

    public async Task<string> GetValidTokenAsync()
    {
        try
        {
            // Check if we have a valid cached token
            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            {
                var isValid = await _pefindoApiService.ValidateTokenAsync(_cachedToken);
                if (isValid)
                {
                    _logger.LogDebug("Using cached valid token");
                    return _cachedToken;
                }
            }

            // Try to get token from database cache
            var dbToken = await GetTokenFromDatabaseAsync();
            if (!string.IsNullOrEmpty(dbToken))
            {
                var isValid = await _pefindoApiService.ValidateTokenAsync(dbToken);
                if (isValid)
                {
                    _logger.LogDebug("Using database cached token");
                    _cachedToken = dbToken;
                    _tokenExpiry = DateTime.UtcNow.AddMinutes(await GetTokenCacheMinutesAsync());
                    return dbToken;
                }
            }

            // Get new token from API
            _logger.LogInformation("Requesting new token from Pefindo API");
            var newToken = await _pefindoApiService.GetTokenAsync();
            
            // Cache the new token
            await CacheTokenInDatabaseAsync(newToken);
            _cachedToken = newToken;
            _tokenExpiry = DateTime.UtcNow.AddMinutes(await GetTokenCacheMinutesAsync());
            
            _logger.LogInformation("New token obtained and cached");
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting valid token");
            throw;
        }
    }

    public async Task InvalidateTokenAsync()
    {
        try
        {
            _logger.LogInformation("Invalidating cached token");
            _cachedToken = null;
            _tokenExpiry = DateTime.MinValue;
            
            await ClearTokenFromDatabaseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating token");
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(_cachedToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                return false;
            }

            return await _pefindoApiService.ValidateTokenAsync(_cachedToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking token validity");
            return false;
        }
    }

    private async Task<string?> GetTokenFromDatabaseAsync()
    {
        try
        {
            _logger.LogDebug("Attempting to get token from database");

            _logger.LogDebug("Creating database connection...");
            using var connection = await _connectionFactory.CreateConnectionAsync();
            _logger.LogDebug("Database connection created successfully");

            using var command = connection.CreateCommand();
            _logger.LogDebug("Database command created");

            //command.CommandText = @"
            //SELECT token_value 
            //FROM public.pbk_token_cache 
            //WHERE expires_at > NOW() 
            //ORDER BY created_date DESC 
            //LIMIT 1";

            _logger.LogDebug("Executing database query...");
            var result = await command.ExecuteScalarAsync();
            _logger.LogDebug("Query executed, result: {Result}", result);

            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving token from database. Connection string: {ConnectionString}",
                "Host info logged for debugging");
            return null;
        }
    }

    private async Task CacheTokenInDatabaseAsync(string token)
    {
        try
        {
            var tokenHash = ComputeTokenHash(token);
            var expiresAt = DateTime.UtcNow.AddMinutes(await GetTokenCacheMinutesAsync());
            
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = @"
                INSERT INTO public.pbk_token_cache (token_hash, token_value, expires_at, created_date)
                VALUES (@hash, @token, @expires, @created)
                ON CONFLICT (token_hash) DO UPDATE SET
                    token_value = @token,
                    expires_at = @expires,
                    created_date = @created";
            
            var hashParam = command.CreateParameter();
            hashParam.ParameterName = "@hash";
            hashParam.Value = tokenHash;
            command.Parameters.Add(hashParam);
            
            var tokenParam = command.CreateParameter();
            tokenParam.ParameterName = "@token";
            tokenParam.Value = token;
            command.Parameters.Add(tokenParam);
            
            var expiresParam = command.CreateParameter();
            expiresParam.ParameterName = "@expires";
            expiresParam.Value = expiresAt;
            command.Parameters.Add(expiresParam);
            
            var createdParam = command.CreateParameter();
            createdParam.ParameterName = "@created";
            createdParam.Value = DateTime.UtcNow;
            command.Parameters.Add(createdParam);
            
            await command.ExecuteNonQueryAsync();
            
            _logger.LogDebug("Token cached in database with expiry: {ExpiresAt}", expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching token in database");
        }
    }

    private async Task ClearTokenFromDatabaseAsync()
    {
        try
        {
            using var connection = await _connectionFactory.CreateConnectionAsync();
            using var command = connection.CreateCommand();
            
            command.CommandText = "DELETE FROM public.pbk_token_cache WHERE expires_at <= NOW()";
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing tokens from database");
        }
    }

    private async Task<int> GetTokenCacheMinutesAsync()
    {
        try
        {
            var configValue = await _globalConfigRepository.GetConfigValueAsync(GlobalConfigKeys.TokenCacheMinutes);
            return int.TryParse(configValue, out var minutes) ? minutes : 60; // Default 60 minutes
        }
        catch
        {
            return 60; // Fallback default
        }
    }

    private static string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
