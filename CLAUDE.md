# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Prompt Rules

---

**Based on my analysis, here are the 7 implementation approaches I considered:**

### Implementation Approaches Analysis:

1. **Direct String Manipulation Approach**

   - Split asset strings by newlines, manipulate array indices, rejoin
   - **Pros**: Simple, minimal dependencies
   - **Cons**: Error-prone, no type safety, hard to maintain

2. **JSON Array Parsing Approach**

   - Parse assets as JSON arrays, manipulate, serialize back
   - **Pros**: Type-safe with structs
   - **Cons**: Assumes JSON format, complex error handling

3. **Utility Helper Functions Approach**

   - Create reusable shuffle/reverse/custom reorder functions
   - **Pros**: Modular, testable, reusable
   - **Cons**: More initial code

4. **Generic Slice Manipulation Approach**

   - Use Go generics for type-safe slice operations
   - **Pros**: Type-safe, elegant
   - **Cons**: Complexity, Go version requirements

5. **Record-Centric Approach**

   - Fetch full record, modify in memory, update back
   - **Pros**: Full context, validation possible
   - **Cons**: More database calls, potential race conditions

6. **Field-Specific Update Approach**

   - Update only the specific asset field (Asset Images/Videos)
   - **Pros**: Minimal updates, efficient
   - **Cons**: Limited context

7. **Transaction-Based Approach**
   - Wrap operations in transaction-like behavior
   - **Pros**: Consistency, rollback capability
   - **Cons**: NoSQLDB may not support transactions

---

### Optimal Approaches Selected:

**Primary: Field-Specific Update with Helper Functions (#3 + #6)**

- Clean separation of concerns
- Efficient database operations
- Maintainable and testable code

**Secondary: Record-Centric with Validation (#5)**

- Full record validation
- Better error handling
- More defensive programming

---

**The Field-Specific Update approach is optimal because:**

1. **Efficiency**: Only updates the specific field needed
2. **Maintainability**: Helper functions are reusable
3. **Reliability**: Matches the TypeScript implementation pattern
4. **Logging**: TaggedLogger integration for debugging

---

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

### VS Code Debug Configuration

The project includes comprehensive VS Code configuration for debugging and development:

**Debug Configurations** (`.vscode/launch.json`):

- **`.NET Core Launch (web)`**: Default configuration with both HTTP/HTTPS, auto-opens Swagger
- **`.NET Core Launch (HTTPS)`**: HTTPS-only mode for secure development
- **`.NET Core Launch (HTTP)`**: HTTP-only mode for basic testing
- **`.NET Core Attach`**: Attach to running processes for debugging

**Available Tasks** (`.vscode/tasks.json`):

- `build` (Ctrl+Shift+B): Default build task
- `watch`: Hot reload development mode
- `test`: Run all unit tests
- `run-dev`: Run with HTTPS profile
- `run-http`: Run with HTTP profile
- `docker-dev-up`: Start development database
- `docker-dev-down`: Stop development database

**Quick Start**:

```bash
# Start debugging: Press F5 or Run > Start Debugging
# Build project: Ctrl+Shift+B
# Run tasks: Ctrl+Shift+P > "Tasks: Run Task"
```

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

### Processing Methods

The API supports **two processing methods** for handling different data format requirements:

#### 1. **Standard Processing Method**: `ProcessIndividualRequestAsync`

- Uses strongly-typed models (`PefindoGetReportResponse`)
- Direct byte array handling for PDF downloads
- Full model validation and type safety
- Optimized for performance with minimal serialization overhead

#### 2. **JSON Processing Method**: `ProcessIndividualRequestWithJsonAsync`

- Uses flexible JsonNode for dynamic data handling
- JSON-compatible PDF download with Base64 encoding
- Adaptable to varying API response structures
- Enhanced dummy response compatibility

**Key Differences in Step 8 (PDF Download)**:

- **Standard**: `DownloadPdfReportAsync()` → `byte[]` → Direct file save
- **JSON**: `DownloadPdfReportWithJsonAsync()` → `JsonNode` with Base64 → Convert to bytes → File save

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
6. **Dual Processing Pipeline**: Support for both typed and JSON-based processing

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
- **Data Aggregation**: `Services/DataAggregationService.cs` - Recently updated for model compatibility
- **API Service**: `Services/PefindoApiService.cs` - Enhanced with JSON PDF download support
- **Configuration**: `Configuration/PefindoConfig.cs`
- **Models**: `Models/PefindoModels.cs` and `Models/RequestResponseModels.cs`
- **Database Access**: `DataAccess/` directory
- **Logging Services**: `Services/Logging/` directory
- **VS Code Config**: `.vscode/` directory with launch.json, tasks.json, settings.json

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

## Recent Updates & Compatibility Notes

### JSON-Compatible PDF Download Enhancement (Latest)

The `PefindoApiService.cs` has been enhanced with a new JSON-compatible PDF download method:

**New Method**: `DownloadPdfReportWithJsonAsync(string eventId, string token)`

- **Return Type**: `Task<JsonNode?>` with JSON structure: `{"binaryData": "base64_content"}`
- **Compatibility**: Handles both dummy responses (raw PDF string) and real API responses
- **Base64 Conversion**: Automatic conversion from raw PDF data to Base64 for consistency
- **Error Handling**: Comprehensive error handling with fallback to dummy responses

**Integration with JSON Processing**:

- Used in `ProcessIndividualRequestWithJsonAsync` Step 8
- Proper logging integration with `ExecuteStepWithLogging`
- Graceful error handling with process continuation on PDF download failure

**Helper Method**: `ConvertDummyPdfResponseToBase64(string dummyResponse)`

- Converts raw PDF string from dummy responses to Base64 format
- Ensures data consistency between dummy and real API responses
- Used in all error fallback scenarios

### DataAggregationService Model Compatibility (Previous)

The `DataAggregationService.cs` has been updated to ensure full compatibility with the latest `PefindoModels.cs` structure:

**Key Changes Made**:

- **Property Mapping Updates**: Fixed property name mismatches (e.g., `max` → `TunggakanTerburuk`, `KualitasKredit` → `KolektabilitasTerburuk`)
- **Data Type Handling**: Added proper string-to-numeric parsing for facility data
- **Scoring Integration**: Enhanced score extraction from `PefindoScoring` array with fallback handling
- **Credit Quality Analysis**: Updated credit quality mapping to handle both string and numeric kolektabilitas values
- **Collection Name Updates**: Changed `Facilities` → `Fasilitas` throughout the service

**New Helper Methods**:

- `GetScoreFromReport()`: Extracts credit scores from scoring array or report data
- `GetWorstCreditQualityMonth()`: Determines month of worst credit quality from facility history
- Enhanced `MapCreditQualityToNumber()`: Handles both text and numeric quality indicators

**Compatibility Notes**:

- All facility processing methods now use string-based data fields from `PefindoFasilitas`
- Proper null checking and default value handling for missing data
- Backward compatibility maintained for existing API responses

### Model Structure Updates

The `PefindoModels.cs` contains comprehensive data models for:

- **Authentication**: Token management with data object structure
- **Search Operations**: Individual debtor search with similarity scoring
- **Report Generation**: Complete report data including facilities, collateral, and scoring
- **Data Aggregation**: Rich debtor information with financial metrics and credit history

**Important Model Classes**:

- `PefindoDebiturInfo`: Contains 100+ financial and credit-related properties
- `PefindoFasilitas`: Facility details with collateral and guarantor information
- `PefindoScoring`: Credit scoring with reason codes and risk grades
- `PefindoGetReportResponse`: Main response wrapper with comprehensive report data
