using idc.pefindo.pbk.Models;
using idc.pefindo.pbk.Services.Interfaces;
using idc.pefindo.pbk.Services.Interfaces.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Token management service with database caching and proper async support
/// </summary>
public class TokenManagerService : ITokenManagerService
{
    private readonly IPefindoApiService _pefindoApiService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<TokenManagerService> _logger;
    private readonly IErrorLogger _errorLogger;
    private readonly ICorrelationService _correlationService;

    private readonly string TOKEN_CACHE_KEY = "pefindo_pbk_token";
    private readonly int DEFAULT_CACHE_BUFFER_MINUTES; // Refresh token 5 minutes before expiry

    // Fallback cache duration if parsing valid_date fails
    private readonly int FALLBACK_CACHE_MINUTES;

    public TokenManagerService(
        IPefindoApiService pefindoApiService,
        IMemoryCache memoryCache,
        ILogger<TokenManagerService> logger,
        IErrorLogger errorLogger,
        ICorrelationService correlationService,
        IConfiguration configuration)
    {
        _pefindoApiService = pefindoApiService;
        _memoryCache = memoryCache;
        _logger = logger;
        _errorLogger = errorLogger;
        _correlationService = correlationService;

        DEFAULT_CACHE_BUFFER_MINUTES = int.Parse(configuration["TokenCaching:BufferMinutes"] ?? "3");
        FALLBACK_CACHE_MINUTES = int.Parse(configuration["TokenCaching:FallbackCacheMinutes"] ?? "40");

    }

    public async Task<string> GetValidTokenAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            // Check if we have a valid cached token
            if (_memoryCache.TryGetValue(TOKEN_CACHE_KEY, out TokenCacheEntry? cachedEntry))
            {
                if (cachedEntry != null && !cachedEntry.IsExpired)
                {
                    // Double-check with API validation if close to expiry
                    if (cachedEntry.TimeUntilExpiry.TotalMinutes > DEFAULT_CACHE_BUFFER_MINUTES)
                    {
                        _logger.LogDebug("Using cached valid token. Expires in {Minutes} minutes, correlation: {CorrelationId}",
                            cachedEntry.TimeUntilExpiry.TotalMinutes, correlationId);
                        return cachedEntry.Token;
                    }

                    // Close to expiry, validate with API
                    var isValid = await _pefindoApiService.ValidateTokenAsync(cachedEntry.Token);
                    if (isValid)
                    {
                        _logger.LogDebug("Using cached token validated with API, correlation: {CorrelationId}", correlationId);
                        return cachedEntry.Token;
                    }

                    _logger.LogDebug("Cached token failed API validation, requesting new token, correlation: {CorrelationId}", correlationId);
                }
                else
                {
                    _logger.LogDebug("Cached token expired, requesting new token, correlation: {CorrelationId}", correlationId);
                }
            }
            else
            {
                _logger.LogDebug("No cached token found, requesting new token, correlation: {CorrelationId}", correlationId);
            }

            // Get new token from API
            return await RefreshTokenAsync();
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("TokenManagerService.GetValidToken", "Error getting valid token", ex, correlationId);
            throw;
        }
    }

    public async Task<string> RefreshTokenAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Requesting new token from Pefindo API, correlation: {CorrelationId}", correlationId);

            // DEBUG: 
            bool isDummy = true;

            JsonSerializerOptions _jsonOptions = new()
             {
                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                 PropertyNameCaseInsensitive = true
             };

            // If you want to use a dummy token for testing, set isDummy to true
            var tokenObject = new PefindoTokenResponse
            {
                Code = "01",
                Message = "Token Aktif",
                Status = "Success",
                Data = new PefindoTokenData
                {
                    Token = "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiOiAiY2xpZW50X2lkIn0.eyJzdWIiOiJwYmtfdXNlciIsImF1dGhvcml0aWVzIjpbIl9fcm9vdF9fIl0sImlzcyI6InBlZmluZG8iLCJqdGkiOiI5N2QyYjE3Ny1mYjQxLTQ4MzAtOTc5YS1iZDY3M2U4Mjk3YjkiLCJleHBpcmVkX3VzZXJuYW1lIjoiUEJLU1dPUkQxMjM0IiwiaWF0IjoxNjg5NTU4MDk5fQ.7b7",
                    ValidDate = "20301231235959" // Example valid date in expected format
                }
            };

            var tokenResponse = isDummy
                ? JsonSerializer.Serialize(tokenObject, _jsonOptions) 
                : await _pefindoApiService.GetTokenAsync();
            
            // Create cache entry with proper expiry
            var cacheEntry = CreateTokenCacheEntry(tokenResponse);

            // Cache with absolute expiry
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = cacheEntry.ExpiryDate,
                Priority = CacheItemPriority.High,
                Size = 1
            };

            _memoryCache.Set(TOKEN_CACHE_KEY, cacheEntry, cacheOptions);

            _logger.LogInformation("New token obtained and cached until {ExpiryDate}, correlation: {CorrelationId}",
                cacheEntry.ExpiryDate, correlationId);

            return cacheEntry.Token;
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("TokenManagerService.RefreshToken", "Error refreshing token", ex, correlationId);
            throw;
        }
    }

    public async Task InvalidateTokenAsync()
    {
        var correlationId = _correlationService.GetCorrelationId();

        try
        {
            _logger.LogInformation("Invalidating cached token, correlation: {CorrelationId}", correlationId);
            _memoryCache.Remove(TOKEN_CACHE_KEY);
        }
        catch (Exception ex)
        {
            await _errorLogger.LogErrorAsync("TokenManagerService.InvalidateToken", "Error invalidating token", ex, correlationId);
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        try
        {
            if (_memoryCache.TryGetValue(TOKEN_CACHE_KEY, out TokenCacheEntry? cachedEntry))
            {
                if (cachedEntry != null && !cachedEntry.IsExpired)
                {
                    // For recently cached tokens, trust the cache
                    if (cachedEntry.TimeUntilExpiry.TotalMinutes > DEFAULT_CACHE_BUFFER_MINUTES)
                    {
                        return true;
                    }

                    // For tokens close to expiry, validate with API
                    return await _pefindoApiService.ValidateTokenAsync(cachedEntry.Token);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            var correlationId = _correlationService.GetCorrelationId();
            await _errorLogger.LogErrorAsync("TokenManagerService.IsTokenValid", "Error checking token validity", ex, correlationId);
            return false;
        }
    }

    public TokenCacheEntry? GetTokenCacheInfo()
    {
        if (_memoryCache.TryGetValue(TOKEN_CACHE_KEY, out TokenCacheEntry? cachedEntry))
        {
            return cachedEntry;
        }

        return null;
    }

    /// <summary>
    /// Creates a token cache entry with proper expiry parsing
    /// </summary>
    private TokenCacheEntry CreateTokenCacheEntry(string tokenResponse)
    {
        try
        {
            // Parse the token response JSON to extract token and valid_date
            var response = System.Text.Json.JsonSerializer.Deserialize<PefindoTokenResponse>(tokenResponse);

            if (response == null)
            {
                throw new InvalidOperationException("Failed to parse token response");
            }

            string token;
            DateTime expiryDate;
            string validDateOriginal = "";

            // Handle new format with data object
            if (response.Data != null && !string.IsNullOrEmpty(response.Data.Token))
            {
                token = response.Data.Token;
                validDateOriginal = response.Data.ValidDate;
                expiryDate = ParseValidDate(response.Data.ValidDate);
            }
            // Handle old format directly
            else if (!string.IsNullOrEmpty(response.Data?.Token))
            {
                token = response.Data.Token;
                validDateOriginal = response.Data.ValidDate;
                expiryDate = ParseValidDate(response.Data.ValidDate);
            }
            else
            {
                throw new InvalidOperationException("No token found in response");
            }

            return new TokenCacheEntry
            {
                Token = token,
                ExpiryDate = expiryDate,
                CachedAt = DateTime.UtcNow,
                ValidDateOriginal = validDateOriginal
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating token cache entry, using fallback expiry");

            // Fallback: try to extract token from response and use default expiry
            var token = ExtractTokenFromResponse(tokenResponse);
            return new TokenCacheEntry
            {
                Token = token,
                ExpiryDate = DateTime.UtcNow.AddMinutes(FALLBACK_CACHE_MINUTES),
                CachedAt = DateTime.UtcNow,
                ValidDateOriginal = "fallback"
            };
        }
    }

    /// <summary>
    /// Parses the valid_date string from Pefindo API response
    /// Format appears to be: "2024261509242633" (yyyyDDDHHmmssff where DDD is day of year)
    /// </summary>
    private DateTime ParseValidDate(string validDate)
    {
        try
        {
            if (string.IsNullOrEmpty(validDate))
            {
                throw new ArgumentException("Valid date is null or empty");
            }

            // Expected format: "2024261509242633"
            // 2024 = year
            // 261 = day of year
            // 050924 = time (HH:mm:ss)
            // 2633 = additional precision (possibly milliseconds)

            if (validDate.Length < 10)
            {
                throw new ArgumentException($"Valid date format is too short: {validDate}");
            }

            // Extract components
            var year = int.Parse(validDate.Substring(0, 4));
            var dayOfYear = int.Parse(validDate.Substring(4, 3));

            // Validate day of year
            if (dayOfYear < 1 || dayOfYear > 366)
            {
                throw new ArgumentException($"Invalid day of year: {dayOfYear}");
            }

            // Create date from year and day of year
            var date = new DateTime(year, 1, 1).AddDays(dayOfYear - 1);

            // Extract time if available
            if (validDate.Length >= 13)
            {
                var hour = int.Parse(validDate.Substring(7, 2));
                var minute = int.Parse(validDate.Substring(9, 2));
                var second = int.Parse(validDate.Substring(11, 2));

                date = date.AddHours(hour).AddMinutes(minute).AddSeconds(second);
            }

            _logger.LogDebug("Parsed valid_date '{ValidDate}' to {ExpiryDate}", validDate, date);
            return date;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse valid_date '{ValidDate}', using fallback expiry", validDate);
            return DateTime.UtcNow.AddMinutes(FALLBACK_CACHE_MINUTES);
        }
    }

    /// <summary>
    /// Fallback method to extract token from response string
    /// </summary>
    private string ExtractTokenFromResponse(string tokenResponse)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(tokenResponse);

            // Try new format first
            if (document.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("token", out var tokenElement))
            {
                return tokenElement.GetString() ?? throw new InvalidOperationException("Token is null");
            }

            // Try old format
            if (document.RootElement.TryGetProperty("token", out var oldTokenElement))
            {
                return oldTokenElement.GetString() ?? throw new InvalidOperationException("Token is null");
            }

            throw new InvalidOperationException("No token found in response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract token from response: {Response}", tokenResponse);
            throw;
        }
    }
}
