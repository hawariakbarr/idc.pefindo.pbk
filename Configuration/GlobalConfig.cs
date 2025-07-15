namespace idc.pefindo.pbk.Configuration;


/// <summary>
/// Global configuration keys used throughout the application
/// </summary>
public class GlobalConfig
{
    // Cycle day configuration
    public string CycleDay { get; set; } = string.Empty;

    // Similarity check configuration
    public string SimilarityCheckVersion { get; set; } = string.Empty;
    public string TableVersion { get; set; } = string.Empty;
    public string NameThreshold { get; set; } = string.Empty;
    public string MotherNameThreshold { get; set; } = string.Empty;

    // Facility configuration
    public const string FacilityThreshold = "GC32";


    // Token management
    public const string TokenCacheMinutes = "GC39";

    // Logging configuration
    public const string LogRetentionDays = "GC40";
    public const string LogLevel = "GC41";
    public const string EnableAuditLogging = "GC42";

    // Performance configuration
    public const string MaxConcurrentRequests = "GC50";
    public const string RequestTimeoutSeconds = "GC51";
    public const string ApiRetryAttempts = "GC52";


}
