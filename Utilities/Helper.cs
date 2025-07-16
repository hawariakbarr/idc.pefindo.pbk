using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace idc.pefindo.pbk.Utilities;

public static class Helper
{

    public static int SafeGetInt(JsonNode? node)
    {
        if (node == null) return 0;

        if (int.TryParse(node.ToString(), out var result))
            return result;

        return 0;
    }

    public static decimal SafeGetDecimal(JsonNode? node)
    {
        if (node == null) return 0;

        if (decimal.TryParse(node.ToString(), out var result))
            return result;

        return 0;
    }

    public static bool SafeGetBool(JsonNode? node)
    {
        if (node == null) return false;

        if (bool.TryParse(node.ToString(), out var result))
            return result;

        return false;
    }

    // Helper methods untuk safe conversion dari string ke numeric types
    public static double SafeGetStringAsDouble(JsonNode? node)
    {
        if (node == null) return 0;

        var stringValue = node.ToString();
        if (string.IsNullOrEmpty(stringValue)) return 0;

        if (double.TryParse(stringValue, out var result))
            return result;

        return 0;
    }

    public static int SafeGetStringAsInt(JsonNode? node)
    {
        if (node == null) return 0;

        var stringValue = node.ToString();
        if (string.IsNullOrEmpty(stringValue)) return 0;

        if (int.TryParse(stringValue, out var result))
            return result;

        return 0;
    }

    public static string SafeGetString(JsonNode? node)
    {
        if (node == null) return string.Empty;

        var stringValue = node.ToString();
        return stringValue ?? string.Empty;
    }
}
