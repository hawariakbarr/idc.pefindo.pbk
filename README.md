# IDC Pefindo PBK API

A .NET 8 Web API that serves as middleware between Core Banking Decision Engine and Pefindo PBK credit bureau services. This API processes individual credit assessment requests through a comprehensive 9-step workflow including cycle day validation, token management, smart search, similarity validation, report generation, and data aggregation.

## 🚀 Features

- **Complete Credit Assessment Workflow**: 9-step process from validation to final report
- **Dual Processing Pipeline**: Standard typed processing and flexible JSON processing methods
- **JSON-Compatible PDF Download**: Enhanced PDF handling with Base64 encoding for JSON workflows
- **Token Management**: Secure JWT token handling with automatic refresh and caching
- **Multi-Database Support**: PostgreSQL connections to multiple IDC databases
- **Comprehensive Logging**: Structured logging with application IDs and audit trails
- **Health Monitoring**: Built-in health checks and monitoring endpoints
- **Similarity Validation**: Configurable name and mother name similarity checking
- **PDF Report Generation**: Automated PDF report download and storage with dual format support
- **Data Aggregation**: Multi-source data consolidation for final responses
- **Dummy Response Compatibility**: Enhanced testing with automatic data format conversion

## 📋 Business Process Flow

The API implements a sequential 9-step workflow:

1. **CYCLE_DAY_VALIDATION** - Validates request timing against cycle day configuration
2. **GET_PEFINDO_TOKEN** - Retrieves and manages JWT authentication tokens
3. **SMART_SEARCH** - Performs intelligent customer search in Pefindo PBK
4. **SIMILARITY_CHECK_SEARCH** - Validates search results against similarity thresholds
5. **GENERATE_REPORT** - Generates credit assessment reports
6. **SIMILARITY_CHECK_REPORT** - Validates report data similarity
7. **STORE_REPORT_DATA** - Persists report data to database
8. **DOWNLOAD_PDF_REPORT** - Downloads and stores PDF reports
9. **DATA_AGGREGATION** - Consolidates all data into final response

## 🛠️ Technology Stack

- **.NET 8** - Web API framework
- **ASP.NET Core** - Web application framework
- **PostgreSQL** - Primary database system
- **Npgsql** - PostgreSQL client for .NET
- **EncryptionApi.dll** - Password encryption utility
- **Serilog** - Structured logging
- **xUnit** - Unit testing framework
- **Swashbuckle.AspNetCore** - Swagger/OpenAPI documentation
- **Microsoft.Extensions.DependencyInjection** - Dependency injection
- **FluentValidation** - Request validation
- **Swagger/OpenAPI** - API documentation
- **Docker** - Development environment
- **Moq** - Mocking framework for testing
- **FluentAssertions** - Test assertions

## 🏗️ Architecture

### Core Components

- **IndividualController**: Main API endpoint for credit assessment requests
- **IndividualProcessingService**: Orchestrates the complete workflow with dual processing methods
- **PefindoApiService**: Handles external Pefindo PBK API calls with JSON and standard PDF download support
- **TokenManagerService**: Manages JWT tokens with caching
- **SimilarityValidationService**: Validates name similarities
- **DataAggregationService**: Consolidates multi-source data with enhanced model compatibility

### Processing Methods

The API supports **two processing methods** for handling different data format requirements:

#### **Standard Processing Method**: `ProcessIndividualRequestAsync`
- **Strongly-typed models** (`PefindoGetReportResponse`)
- **Direct byte array handling** for PDF downloads
- **Full model validation** and type safety
- **Optimized performance** with minimal serialization overhead
- **Best for**: Production environments with consistent API responses

#### **JSON Processing Method**: `ProcessIndividualRequestWithJsonAsync`
- **Flexible JsonNode** for dynamic data handling
- **JSON-compatible PDF download** with Base64 encoding
- **Adaptable to varying** API response structures
- **Enhanced dummy response** compatibility with automatic conversion
- **Best for**: Development, testing, and environments with variable response formats

#### **Key Differences in Step 8 (PDF Download)**:
- **Standard**: `DownloadPdfReportAsync()` → `byte[]` → Direct file save
- **JSON**: `DownloadPdfReportWithJsonAsync()` → `JsonNode` with Base64 → Convert to bytes → File save

### Key Design Patterns

- **Service-Oriented Architecture**: Clear separation of concerns
- **Dual Processing Pipeline**: Support for both typed and JSON-based processing
- **Dependency Injection**: Comprehensive DI container configuration
- **Async/Await Pattern**: Full asynchronous processing pipeline
- **Configuration-Driven Behavior**: Runtime configuration through database
- **Correlation ID Tracking**: Request tracing throughout the pipeline

## 🚦 Getting Started

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL 15+
- Docker (for development database)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd idc.pefindo.pbk
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Start development database**
   ```bash
   docker-compose -f docker-compose.dev.yml up -d
   ```

4. **Build the project**
   ```bash
   dotnet build
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

## 🔧 Configuration

### Database Configuration

The application supports multiple PostgreSQL databases with encrypted password configuration:

- `idc.core` - Core banking data
- `idc.en` - Enterprise data
- `idc.bk` - Banking data (primary)
- `idc.sync` - Synchronization data

### Global Configuration Keys

Runtime behavior is controlled through database configuration:

- `GC31` - Cycle day validation settings
- `GC35` - Name similarity threshold
- `GC36` - Mother name similarity threshold
- `GC39` - Token cache duration

### Environment Configuration

Update `appsettings.json` and `appsettings.Development.json` with your specific configurations:

```json
{
  "PefindoAPIConfig": {
    "BaseUrl": "https://api.pefindo.com",
    "Username": "your_username",
    "Password": "your_password",
    "TimeoutSeconds": 30
  },
  "SimilarityConfig": {
    "DefaultNameThreshold": 0.8,
    "DefaultMotherNameThreshold": 0.7
  }
}
```

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run with Verbose Output
```bash
dotnet test --verbosity normal
```

### Run Specific Test Categories
```bash
# Unit tests only
dotnet test Tests/Unit/

# Integration tests only
dotnet test Tests/Integration/

# Specific test file
dotnet test Tests/Unit/TokenManagerServiceTests.cs
```

### Test Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 API Documentation

### Endpoints

- **POST** `/idcpefindo/individual` - Process individual credit assessment
- **GET** `/idcpefindo/health` - Health check endpoint
- **GET** `/swagger` - Interactive API documentation

### Development URLs

- **API Base**: `https://localhost:7142` or `http://localhost:5189`
- **Swagger UI**: `https://localhost:7142/swagger`
- **Health Check**: `https://localhost:7142/health`
- **pgAdmin**: `http://localhost:5050` (admin@localhost.com / admin123)

## 🔐 Security

### Security Features

- **Encrypted Database Passwords**: Using `EncryptionApi.dll`
- **JWT Token Security**: Secure token management with automatic refresh
- **Sensitive Data Sanitization**: Automatic sanitization in logs
- **Correlation ID Tracking**: Request tracing for audit and security

### Security Best Practices

- Database passwords are encrypted at rest
- No sensitive data is logged or exposed
- JWT tokens are cached securely with configurable expiration
- All external API calls are logged for audit purposes

## 📁 Project Structure

```
├── Controllers/           # API controllers
├── Services/             # Business logic services
│   ├── Interfaces/       # Service contracts
│   └── Logging/          # Logging services
├── Models/               # Data models and DTOs
│   └── Validators/       # Request validators
├── DataAccess/           # Database access layer
├── Configuration/        # Configuration classes
├── Middleware/           # Custom middleware
├── Utilities/            # Utility functions
├── Tests/               # Unit and integration tests
│   ├── Unit/            # Unit tests
│   ├── Integration/     # Integration tests
│   └── Mocks/           # Mock implementations
└── Files/               # File storage (PDFs, etc.)
```

## 🔄 Development Workflow

### Database Operations

```bash
# Start development database
docker-compose -f docker-compose.dev.yml up -d

# Stop development database
docker-compose -f docker-compose.dev.yml down

# View database logs
docker-compose -f docker-compose.dev.yml logs postgres
```

### Build and Run

```bash
# Development build
dotnet build

# Run with development profile
dotnet run --launch-profile https

# Run tests
dotnet test
```

## 📝 Logging

The application implements a **comprehensive multi-layered logging system** combining structured application logging (Serilog) with specialized database logging for complete audit trails and monitoring.

### Logging Architecture

#### **1. Database Logging System**

The project implements **5 specialized PostgreSQL logging tables** in the `pefindo` schema:

- **`bk_log_entries`** - Master correlation logging table with transaction tracking
- **`bk_http_request_logs`** - Complete HTTP request/response logging with performance metrics
- **`bk_process_step_logs`** - Step-by-step workflow tracking with timing and input/output data
- **`bk_error_logs`** - Structured error logging with stack traces and context
- **`bk_audit_logs`** - Business audit trail with entity change tracking and old/new values

#### **2. Specialized Logging Services**

Located in `Services/Logging/`:

##### **AuditLogger**
- **Business audit trail logging** with user actions and entity changes
- **Change tracking** with old/new values in JSON format
- **IP address and user agent tracking** for compliance
- **Entity state monitoring** for data integrity

##### **ErrorLogger**
- **Structured error logging** with multiple severity levels (Error, Warning, Critical)
- **Stack trace capture** with full exception details
- **Additional context data** stored as JSON for debugging
- **Correlation ID integration** for error tracking

##### **ProcessStepLogger**
- **Step-by-step workflow tracking** for the 9-step credit assessment process
- **Performance timing** with start/complete/fail status tracking
- **Input/output data capture** for each process step
- **Execution duration monitoring** for performance analysis

##### **HttpRequestLogger**
- **Complete HTTP request/response logging** for external API calls
- **Service name, method, URL, headers, and body logging**
- **Performance metrics** with request duration tracking
- **Success/failure status** with detailed error information

##### **CorrelationLogger**
- **Master correlation logging** to `bk_log_entries` table
- **Process lifecycle tracking** (start/complete/fail) for entire workflow
- **High-level process status** (InProgress → Success/Failed)
- **Master correlation table** linking all detailed logs via correlation_id

##### **CorrelationService**
- **Transaction ID and correlation ID management** throughout the pipeline
- **Request tracking** from entry to completion
- **Cross-service correlation** for distributed logging
- **Unique identifier generation** for audit trails

#### **3. Structured Application Logging (Serilog)**

##### **Configuration**
- **Console logging** with structured output templates
- **File logging** with daily rolling files (`logs/app-{date}.txt`, `logs/dev-{date}.txt`)
- **Log enrichment** with machine name, thread ID, environment, and application name
- **Correlation ID integration** in all log messages

##### **Log Levels**
- **Information** - Normal operation flow and business logic
- **Warning** - Potential issues, fallbacks, or performance concerns
- **Error** - Errors with stack traces and recovery actions
- **Debug** - Detailed debugging information and variable states
- **Critical** - System-critical errors requiring immediate attention

#### **4. Middleware Integration**

##### **CorrelationMiddleware**
- **Extracts/generates correlation IDs** from request headers
- **Injects correlation context** into all requests
- **Adds correlation IDs to response headers** for client tracking
- **Creates structured logging scope** for request isolation

##### **GlobalExceptionMiddleware**
- **Handles all unhandled exceptions** with structured error responses
- **Provides consistent error formatting** for API consumers
- **Logs exceptions with full context** including request details
- **Maintains audit trail** for error tracking

##### **HttpLoggingHandler**
- **Intercepts all HTTP client requests** to external services
- **Logs complete request/response data** to database
- **Implements sensitive data sanitization** for security
- **Adds correlation headers** to outbound requests

#### **5. Sensitive Data Protection**

##### **SensitiveDataSanitizer Utility**
Located in `Utilities/SensitiveDataSanitizer.cs`:

- **Regex-based sanitization** for passwords, tokens, and API keys
- **URL sanitization** removing sensitive query parameters
- **Credit card number masking** with industry-standard patterns
- **SSN masking** for compliance with data protection regulations
- **Comprehensive pattern matching** for various sensitive data types

#### **6. Comprehensive Workflow Logging**

##### **Master Process Logging**
The `IndividualProcessingService` implements **complete workflow tracking**:

- **Process start logging** to `bk_log_entries` when workflow begins
- **Process completion logging** with Success/Failed status
- **Error message capture** for failed processes
- **Master correlation table** provides high-level process overview

##### **Process Step Integration**
The `IndividualProcessingService` implements **enterprise-grade step-by-step logging**:

- **9 sequential steps** with individual start/complete/fail logging
- **ExecuteStepWithLogging wrapper methods** for consistent logging
- **Performance timing** for each step with duration tracking
- **Input/output data capture** with JSON serialization
- **Error handling** with detailed context and recovery information

##### **Audit Trail Features**
- **Business action logging** throughout the complete workflow
- **Entity change tracking** with before/after state comparison
- **User action correlation** with IP addresses and timestamps
- **Compliance-ready audit logs** for regulatory requirements

#### **7. Logging Configuration**

##### **Development Environment**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

##### **Production Considerations**
- **Log retention policies** with configurable cleanup schedules
- **Performance monitoring** with metrics collection
- **Security compliance** with sensitive data sanitization
- **Scalability** with efficient database indexing and partitioning

#### **8. Monitoring and Observability**

##### **Performance Metrics**
- **Request duration tracking** for all API endpoints
- **Database query performance** monitoring
- **External service response times** with SLA tracking
- **Memory usage and garbage collection** metrics

##### **Health Monitoring**
- **Service health checks** with detailed status reporting
- **Database connectivity** monitoring with connection pool metrics
- **External service availability** with retry and circuit breaker patterns
- **System resource utilization** tracking

##### **Alerting Integration**
- **Error rate monitoring** with threshold-based alerts
- **Performance degradation** detection
- **Security event logging** with audit trail correlation
- **Compliance reporting** with automated audit trail generation

### **Master Correlation Logging Usage:**

```csharp
// Process start (automatically logged in IndividualProcessingService)
await _correlationLogger.LogProcessStartAsync(correlationId, requestId, "IndividualProcessing", "system");

// Process completion (automatically logged on success)
await _correlationLogger.LogProcessCompleteAsync(correlationId, "Success");

// Process failure (automatically logged on exception)
await _correlationLogger.LogProcessFailAsync(correlationId, "Failed", exception.Message);
```

### **Query Pattern for Complete Audit Trail:**

```sql
-- Get master process status
SELECT * FROM pefindo.bk_log_entries WHERE correlation_id = 'corr-123';

-- Get all related detailed logs
SELECT * FROM pefindo.bk_process_step_logs WHERE correlation_id = 'corr-123' ORDER BY step_order;
SELECT * FROM pefindo.bk_http_request_logs WHERE correlation_id = 'corr-123' ORDER BY request_time;
SELECT * FROM pefindo.bk_error_logs WHERE correlation_id = 'corr-123' ORDER BY created_at;
SELECT * FROM pefindo.bk_audit_logs WHERE correlation_id = 'corr-123' ORDER BY timestamp;
```

## 🚀 Deployment

### Build for Production

```bash
dotnet publish -c Release -o ./publish
```

### Environment Variables

Set the following environment variables for production:

- `ASPNETCORE_ENVIRONMENT=Production`
- `DBEncryptedPassword=<encrypted_password>`
- Database connection strings
- Pefindo API credentials

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Standards

- Follow .NET coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages
- Ensure all tests pass before submitting

## 📈 Monitoring

### Health Checks

The API includes comprehensive health checks:

- Database connectivity
- External API availability
- Service dependencies
- Memory and performance metrics

### Performance Monitoring

- Request/response timing
- Database query performance
- External API response times
- Memory usage tracking

## 🆘 Troubleshooting

### Common Issues

1. **Database Connection Errors**
   - Verify PostgreSQL is running
   - Check connection strings in `appsettings.json`
   - Ensure encrypted password is correct

2. **External API Failures**
   - Check Pefindo API credentials
   - Verify network connectivity
   - Review API timeout settings

3. **Token Issues**
   - Clear token cache
   - Verify API credentials
   - Check token expiration settings

### Debug Logging

Enable debug logging by setting log level to `Debug` in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

## 📋 Dependencies

### External Dependencies

- **Pefindo PBK API** - Credit bureau services
- **PostgreSQL** - Database system
- **EncryptionApi.dll** - Password encryption

### NuGet Packages

Key packages used in the project:

- **Microsoft.AspNetCore** - Web API framework
- **Npgsql** - PostgreSQL client
- **Serilog** - Structured logging
- **FluentValidation** - Request validation
- **Swashbuckle.AspNetCore** - API documentation
- **xUnit** - Testing framework
- **Moq** - Mocking framework

## 📄 License

This project is proprietary and confidential. All rights reserved.

## 📞 Support

For support and questions:

- Create an issue in the repository
- Contact the development team
- Review the API documentation at `/swagger`

## 🔄 Recent Updates

### **JSON-Compatible PDF Download Enhancement** (Latest)

Enhanced the `PefindoApiService` with new JSON-compatible PDF download capabilities:

#### **New Method**: `DownloadPdfReportWithJsonAsync`
- **Return Format**: `{"binaryData": "base64_encoded_pdf_content"}`
- **Compatibility**: Handles both dummy responses and real API responses
- **Base64 Conversion**: Automatic conversion from raw PDF data to Base64
- **Error Handling**: Comprehensive fallback to dummy responses

#### **Integration Features**:
- **Step 8 Enhancement**: Integrated into `ProcessIndividualRequestWithJsonAsync`
- **Consistent Logging**: Uses `ExecuteStepWithLogging` for uniform audit trails
- **Graceful Degradation**: Process continues even if PDF download fails
- **Helper Method**: `ConvertDummyPdfResponseToBase64` ensures data consistency

#### **Benefits**:
- ✅ **Fixed Base64 Conversion Error**: Resolved "invalid Base-64 string" issues
- ✅ **Enhanced Testing**: Improved dummy response compatibility
- ✅ **Flexible Data Handling**: Support for varying response formats
- ✅ **Maintainable Code**: Clean separation with helper methods

### **Previous Enhancements**

#### **DataAggregationService Model Compatibility**
- Updated property mappings for latest `PefindoModels.cs` structure
- Enhanced credit quality analysis and scoring integration
- Added comprehensive null checking and default value handling

#### **Comprehensive Logging System**
- 5 specialized PostgreSQL logging tables
- Master correlation tracking with detailed audit trails
- Step-by-step process monitoring with performance metrics

---

*This README provides comprehensive information about the IDC Pefindo PBK API. For detailed technical documentation, please refer to the Swagger UI at `/swagger` when running the application.*
