namespace idc.pefindo.pbk.Configuration;

/// <summary>
/// Configuration settings for Pefindo PBK API integration
/// </summary>
public class PefindoAPIConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string Domain { get; set; } = string.Empty;
    public bool UseDummyResponses { get; set; } = false;
    public string DummyResponseFilePath { get; set; } = "dummy-response.json";
}

