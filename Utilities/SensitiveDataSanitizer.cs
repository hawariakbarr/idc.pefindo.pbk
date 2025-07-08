using System.Text.RegularExpressions;

namespace idc.pefindo.pbk.Utilities;

public static class SensitiveDataSanitizer
{
    private static readonly Regex[] SensitivePatterns = {
        new(@"""password""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""token""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""authorization""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""creditCardNumber""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""ssn""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"""apiKey""\s*:\s*""[^""]*""", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    };

    private static readonly Dictionary<Regex, string> Replacements = new()
    {
        { SensitivePatterns[0], @"""password"":""***""" },
        { SensitivePatterns[1], @"""token"":""***""" },
        { SensitivePatterns[2], @"""authorization"":""***""" },
        { SensitivePatterns[3], @"""creditCardNumber"":""****-****-****-****""" },
        { SensitivePatterns[4], @"""ssn"":""***-**-****""" },
        { SensitivePatterns[5], @"""apiKey"":""***""" }
    };

    public static string SanitizeForLogging(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return content ?? string.Empty;

        var sanitized = content;
        foreach (var (pattern, replacement) in Replacements)
        {
            sanitized = pattern.Replace(sanitized, replacement);
        }

        return sanitized;
    }

    public static string SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return url ?? string.Empty;

        // Remove query parameters that might contain sensitive data
        var uri = new Uri(url);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}
