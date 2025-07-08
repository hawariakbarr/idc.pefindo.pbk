# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is the **IDC Pefindo PBK API** - a .NET 8 Web API that serves as middleware between Core Banking Decision Engine and Pefindo PBK credit bureau services. The API processes individual credit assessment requests through a comprehensive 9-step workflow including cycle day validation, token management, smart search, similarity validation, report generation, and data aggregation.

**Key Business Process**: `CYCLE_DAY_VALIDATION` → `GET_PEFINDO_TOKEN` → `SMART_SEARCH` → `SIMILARITY_CHECK_SEARCH` → `GENERATE_REPORT` → `SIMILARITY_CHECK_REPORT` → `STORE_REPORT_DATA` → `DOWNLOAD_PDF_REPORT` → `DATA_AGGREGATION`

## Development Commands

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run

# Run with specific profile (development)
dotnet run --launch-profile https
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal

# Run specific test project
dotnet test Tests/

# Run single test file
dotnet test Tests/Unit/TokenManagerServiceTests.cs

# Run tests with coverage (if configured)
dotnet test --collect:"XPlat Code Coverage"
```

### Database Operations
```bash
# Start development database
docker-compose -f docker-compose.dev.yml up -d

# Stop development database
docker-compose -f docker-compose.dev.yml down

# View database logs
docker-compose -f docker-compose.dev.yml logs postgres
```

### Development URLs
- **API Base**: `https://localhost:7142` or `http://localhost:5189`
- **Swagger UI**: `https://localhost:7142/swagger` or root `/`
- **Health Check**: `https://localhost:7142/health`
- **pgAdmin**: `http://localhost:5050` (admin@localhost.com / admin123)

## Architecture Overview

### Core Components

1. **IndividualProcessingService**: Orchestrates the complete credit assessment workflow with comprehensive logging at each step
2. **PefindoApiService**: Handles all external API calls to Pefindo PBK services
3. **TokenManagerService**: Manages JWT tokens with caching and automatic refresh
4. **SimilarityValidationService**: Validates name and mother name similarity using configurable thresholds
5. **DataAggregationService**: Aggregates data from multiple sources into final response format

### Data Flow Architecture

```
IndividualController → IndividualProcessingService → [9 Sequential Steps] → IndividualResponse
```

Each processing step includes:
- Request tracking using a unique **Transaction ID**
- Comprehensive logging (start, success, failure)
- Error handling with rollback capability
- Audit trail logging 


### Configuration Management

**Multi-Database Support**: The application supports multiple PostgreSQL databases with encrypted password configuration:
- `idc.core` - Core banking data
- `idc.en` - Enterprise data
- `idc.bk` - Banking data (primary)
- `idc.sync` - Synchronization data

**Global Configuration**: Runtime configuration through `GlobalConfigKeys` including:
- `GC31` - Cycle day validation
- `GC35` - Name similarity threshold
- `GC36` - Mother name similarity threshold
- `GC39` - Token cache duration

### External Dependencies

**Required External Services**:
- **Pefindo PBK API**: Credit bureau services for search and report generation
- **PostgreSQL**: Primary database for data storage
- **EncryptionApi**: External DLL for password encryption (`EncryptionApi.dll`)
- **Helper**: External DLL for utility functions (`Helper.dll`)

### Key Design Patterns

1. **Service-Oriented Architecture**: Clear separation between controllers, services, and data access
2. **Comprehensive Logging**: Structured logging with Serilog, transaction IDs, and audit trails
3. **Configuration-Driven**: Runtime behavior controlled through database configuration
4. **Error Handling**: Graceful error handling with detailed error logging and user-friendly responses
5. **Asynchronous Processing**: Full async/await pattern throughout the pipeline

### Testing Strategy

The project uses **xUnit** with **FluentAssertions** and **Moq** for testing:

- **Unit Tests**: Individual service testing with mocked dependencies
- **Integration Tests**: Full workflow testing with test database
- **Test Structure**: `Tests/Unit/` and `Tests/Integration/` directories
- **Test Helpers**: `TestHelper.cs` and mock services in `Tests/Mocks/`

### Security Considerations

- **Encrypted Passwords**: Database passwords are encrypted using `EncryptionApi`
- **Token Management**: JWT tokens are securely cached with automatic refresh
- **Sensitive Data Sanitization**: `SensitiveDataSanitizer` utility for logging
- **Transaction Tracking**: Request transaction IDs for audit and troubleshooting

## Development Notes

### Key File Locations
- **Main Controller**: `Controllers/IndividualController.cs`
- **Core Service**: `Services/IndividualProcessingService.cs`
- **Configuration**: `Configuration/PefindoConfig.cs`
- **Models**: `Models/PefindoModels.cs` and `Models/RequestResponseModels.cs`
- **Database Access**: `DataAccess/` directory
- **Logging Services**: `Services/Logging/` directory

### Important Constants
- **API Route**: `/idcpefindo/individual`
- **Health Check**: `/idcpefindo/health`
- **PDF Storage**: `Files/pdfs/yyyy-MM/`
- **Log Files**: `logs/app-{date}.txt`

### Dependency Injection
All services are registered in `Program.cs` with scoped lifetime. The application uses comprehensive DI for:
- Database connections
- HTTP clients with custom handlers
- Logging services
- Business logic services
- Configuration objects

### Performance Considerations
- **Memory Cache**: Configured with size limits for token caching
- **Connection Pooling**: PostgreSQL connections with min/max pool sizes
- **Async Processing**: Full async pipeline to avoid blocking
- **Retry Logic**: Built-in retry mechanisms for external API calls
- **Timeout Configuration**: Configurable timeouts for external service calls