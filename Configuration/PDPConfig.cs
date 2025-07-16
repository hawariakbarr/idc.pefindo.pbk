namespace idc.pefindo.pbk.Configuration;

/// <summary>
/// Configuration for Personal Data Protection (PDP) security features
/// </summary>
public class PDPConfig
{
    /// <summary>
    /// Indicates whether PDP security is active
    /// true = PDP security enabled with encryption
    /// false = PDP security disabled
    /// </summary>
    public bool IsActive { get; set; } = false;

    /// <summary>
    /// Symmetric key used for encryption and decryption of personal data
    /// Must be a valid base64-encoded key
    /// </summary>
    public string SymmetricKey { get; set; } = string.Empty;

    /// <summary>
    /// Validates the PDP configuration settings
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise</returns>
    public bool IsValid()
    {
        if (!IsActive)
            return true; // If not active, no validation needed

        if (string.IsNullOrWhiteSpace(SymmetricKey))
            return false;

        // Validate base64 format
        try
        {
            Convert.FromBase64String(SymmetricKey);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
