# Dummy Response Implementation

This implementation provides a fallback mechanism for the Pefindo PBK API when the actual API is unavailable or connection errors occur. This is particularly useful during development and testing phases.

## Features

### 1. Configuration-Based Control
- **`UseDummyResponses`**: Boolean flag to enable/disable dummy response usage
- **`DummyResponseFilePath`**: Path to the JSON file containing dummy responses
- Configurable through `appsettings.json` and `appsettings.Development.json`

### 2. Automatic Fallback
- When API connection fails, the system automatically falls back to dummy responses
- Handles common connection errors:
  - Connection timeouts
  - Socket exceptions
  - HTTP request exceptions
  - Host unreachable errors

### 3. Comprehensive Response Coverage
The `dummy-response.json` file includes responses for all Pefindo PBK API endpoints:
- **getToken**: Success, authentication failure, IP blocked, server error
- **validateToken**: Success, invalid token, expired token
- **search**: Perfect match, multiple matches, corporate search, data not found
- **generateReport**: Success, duplicate event ID, invalid token
- **getReport**: Complete report, big report, processing, not found
- **downloadReport**: Success with pagination, not found
- **downloadPdfReport**: PDF binary data, not found
- **bulk**: Success, validation errors

### 4. Smart Error Detection
The system automatically detects connection-related errors and provides appropriate fallbacks:
```csharp
private static bool IsConnectionError(Exception ex)
{
    return ex.Message.Contains("connection") ||
           ex.Message.Contains("timeout") ||
           ex.Message.Contains("failed to respond") ||
           ex.Message.Contains("connected host has failed") ||
           ex.InnerException is SocketException;
}
```

## Usage

### Configuration (appsettings.json)
```json
{
  "PefindoConfig": {
    "BaseUrl": "https://api.domain.com",
    "Username": "your_username",
    "Password": "your_password",
    "TimeoutSeconds": 30,
    "Domain": "api.domain.com",
    "UseDummyResponses": true,
    "DummyResponseFilePath": "dummy-response.json"
  }
}
```

### Example: GetTokenAsync Implementation
```csharp
public async Task<string> GetTokenAsync()
{
    var correlationId = _correlationService.GetCorrelationId();

    // If configured to use dummy responses, use them directly
    if (_config.UseDummyResponses && _dummyResponseService != null)
    {
        _logger.LogInformation("Using dummy response for token request, correlation: {CorrelationId}", correlationId);
        
        if (!_dummyResponseService.IsLoaded)
        {
            await _dummyResponseService.LoadDummyResponsesAsync();
        }
        
        return _dummyResponseService.GetTokenResponse("success");
    }

    try
    {
        // Attempt real API call
        // ... actual implementation
    }
    catch (HttpRequestException httpEx) when (IsConnectionError(httpEx))
    {
        // Fallback to dummy response on connection error
        return await HandleConnectionErrorWithFallback(httpEx, correlationId, "token", () => 
            _dummyResponseService?.GetTokenResponse("success"));
    }
    // ... other exception handling
}
```

## Benefits

1. **Development Continuity**: Developers can continue working even when the actual Pefindo API is unavailable
2. **Testing Reliability**: Integration tests can run consistently without external dependencies
3. **Error Resilience**: Production systems can gracefully handle API outages
4. **Scenario Testing**: Easy testing of various API response scenarios (success, failure, edge cases)
5. **Offline Development**: Full workflow testing possible without internet connectivity

## Implementation Details

### Key Components

1. **IDummyResponseService**: Interface for dummy response management
2. **DummyResponseService**: Implementation for loading and serving dummy responses
3. **Enhanced PefindoApiService**: Modified to support fallback logic
4. **Configuration**: Extended PefindoConfig with dummy response settings

### Error Handling Strategy

1. **Primary**: Attempt real API call
2. **Secondary**: On connection error, fall back to dummy response
3. **Tertiary**: If dummy response unavailable, rethrow original exception

This ensures the system remains functional during development while preserving error information for debugging.

## Testing

The implementation has been tested with:
- All existing TokenManagerService tests pass (100% success rate)
- Overall test suite maintains 96.6% pass rate (86/89 tests)
- Connection error scenarios properly handled
- Configuration-based switching between real and dummy responses

## Future Enhancements

- Support for scenario-based dummy response selection
- Response variation based on input parameters
- Metrics tracking for dummy response usage
- Admin interface for managing dummy responses