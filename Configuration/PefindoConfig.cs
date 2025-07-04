namespace idc.pefindo.pbk.Configuration;

/// <summary>
/// Configuration settings for Pefindo PBK API integration
/// </summary>
public class PefindoConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string Domain { get; set; } = string.Empty;
}

/// <summary>
/// Database connection configuration
/// </summary>
public class DatabaseConfig
{
    public string DefaultConnection { get; set; } = string.Empty;
}

/// <summary>
/// Similarity checking configuration
/// </summary>
public class SimilarityConfig
{
    public double DefaultNameThreshold { get; set; } = 0.8;
    public double DefaultMotherNameThreshold { get; set; } = 0.7;
    public int DefaultSimilarityVersion { get; set; } = 3;
    public int DefaultTableVersion { get; set; } = 2;
}

/// <summary>
/// Cycle day validation configuration
/// </summary>
public class CycleDayConfig
{
    public string ConfigCode { get; set; } = "GC31";
    public int DefaultCycleDay { get; set; } = 7;
}

/// <summary>
/// Global configuration keys used throughout the application
/// </summary>
public static class GlobalConfigKeys
{
    public const string CycleDay = "GC31";
    public const string SimilarityCheckVersion = "GC33";
    public const string TableVersion = "GC34";
    public const string NameThreshold = "GC35";
    public const string MotherNameThreshold = "GC36";
    public const string ApiTimeoutSeconds = "GC37";
    public const string RetryAttempts = "GC38";
    public const string TokenCacheMinutes = "GC39";
    public const string MaxReportSizeMb = "GC40";
}
