using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace AppCleaner;
    public static class PatternTypeExtensions
{
    public static string GetDisplayName(this PatternType value)
    {
        var field = typeof(PatternType).GetField(value.ToString());

        var attr = field?.GetCustomAttribute<DisplayAttribute>();

        return attr?.Name ?? value.ToString();
    }
    public static PatternType FromDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return PatternType.CS;

        foreach (var pattern in Enum.GetValues<PatternType>())
        {
            if (string.Equals(pattern.GetDisplayName(), value, StringComparison.OrdinalIgnoreCase))
                return pattern;

            if (string.Equals(pattern.ToString(), value, StringComparison.OrdinalIgnoreCase))
                return pattern;
        }

        return PatternType.CS;
    }
}