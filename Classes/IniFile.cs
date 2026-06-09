using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
namespace AppCleaner;
[AttributeUsage(AttributeTargets.Property)]
public sealed class SavedAttribute : Attribute
{
}
    public sealed class IniFile
{
    public static string DefaultFilePath =>
        Path.Combine(
            AppContext.BaseDirectory,
            $"{Path.GetFileNameWithoutExtension(Application.ExecutablePath)}.ini");
    private readonly string _filePath;
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);
    public IniFile()
        : this(DefaultFilePath)
    {
    }
    public IniFile(string filePath)
    {
        _filePath = filePath;
        Load();
    }
    public string Read(string section, string key, string defaultValue = "")
    {
        return _sections.TryGetValue(section, out var values) &&
               values.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }
    public void Write(string section, string key, object? value)
    {
        if (!_sections.TryGetValue(section, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = values;
        }
        values[key] = ConvertToIniString(value);
    }
    public void SaveObject(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        string section = obj.GetType().Name;
        foreach (var property in GetSavedProperties(obj.GetType()))
        {
            var value = property.GetValue(obj);
            if (value is Dictionary<ComboTodoItems, ActionSettings> settings)
            {
                SaveActionSettings(property.Name, settings);
                continue;
            }
            Write(section, property.Name, value);
        }
        Save();
    }
    public void LoadObject(object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        string section = obj.GetType().Name;
        foreach (var property in GetSavedProperties(obj.GetType()))
        {
            if (property.GetValue(obj) is Dictionary<ComboTodoItems, ActionSettings> settings)
            {
                LoadActionSettings(property.Name, settings);
                continue;
            }
            var text = Read(section, property.Name);
            if (string.IsNullOrEmpty(text))
                continue;
            try
            {
                var value = ConvertFromString(text, property.PropertyType);
                if (value is string stringValue && IsPathLikeProperty(property))
                    value = NormalizePathSeparators(stringValue);
                property.SetValue(obj, value);
            }
            catch
            {
                // Некорректное значение в ini не должно ломать запуск приложения.
            }
        }
    }
    public void SaveActionSettings(string propertyName, Dictionary<ComboTodoItems, ActionSettings> settings)
    {
        foreach (var pair in settings)
        {
            string section = $"{propertyName}.{pair.Key}";
            Write(section, nameof(ActionSettings.SearchValue), pair.Value.SearchValue);
            Write(section, nameof(ActionSettings.PlaceValue), pair.Value.PlaceValue);
        }
    }
    public void LoadActionSettings(string propertyName, Dictionary<ComboTodoItems, ActionSettings> settings)
    {
        foreach (var pair in settings)
        {
            string section = $"{propertyName}.{pair.Key}";
            pair.Value.SearchValue =
                NormalizePathSeparators(Read(section, nameof(ActionSettings.SearchValue)));
            pair.Value.PlaceValue =
                NormalizePathSeparators(Read(section, nameof(ActionSettings.PlaceValue)));
        }
    }
    public void Save()
    {
        var lines = new List<string>();
        foreach (var section in _sections)
        {
            lines.Add($"[{section.Key}]");
            foreach (var pair in section.Value)
                lines.Add($"{pair.Key}={Escape(pair.Value)}");
            lines.Add(string.Empty);
        }
        File.WriteAllLines(_filePath, lines, Encoding.UTF8);
    }
    private void Load()
    {
        if (!File.Exists(_filePath))
            return;
        string currentSection = string.Empty;
        foreach (var rawLine in File.ReadAllLines(_filePath, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line[1..^1];
                if (!_sections.ContainsKey(currentSection))
                    _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }
            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;
            if (!_sections.TryGetValue(currentSection, out var values))
            {
                values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _sections[currentSection] = values;
            }
            values[parts[0].Trim()] = Unescape(parts[1]);
        }
    }
    private static IEnumerable<PropertyInfo> GetSavedProperties(Type type)
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.CanRead)
            .Where(x => x.GetCustomAttribute<SavedAttribute>() is not null)
            .Where(x => IsSupportedIniType(x.PropertyType));
    }
    private static bool IsSupportedIniType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string)
            || type == typeof(int)
            || type == typeof(bool)
            || type == typeof(long)
            || type == typeof(double)
            || type == typeof(decimal)
            || type == typeof(List<string>)
            || type.IsEnum)
            return true;
        return IsActionSettingsDictionary(type);
    }
    private static bool IsActionSettingsDictionary(Type type)
    {
        if (!type.IsGenericType)
            return false;
        if (type.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            return false;
        var args = type.GetGenericArguments();
        return args[0] == typeof(ComboTodoItems)
            && args[1] == typeof(ActionSettings);
    }
    private static string ConvertToIniString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            List<string> list => string.Join("|", NormalizeStringList(list)),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
    private static object? ConvertFromString(string text, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        var realType = nullableType ?? targetType;
        if (nullableType != null && string.IsNullOrWhiteSpace(text))
            return null;
        if (realType == typeof(string))
            return text;
        if (realType == typeof(List<string>))
            return ParseStringList(text);
        if (realType.IsEnum)
        {
            if (realType == typeof(PatternType))
                return PatternTypeExtensions.FromDisplayName(text);
            if (Enum.TryParse(realType, text, true, out var enumValue))
                return enumValue;
            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var enumIndex))
                return Enum.ToObject(realType, enumIndex);
            return Activator.CreateInstance(realType);
        }
        if (realType == typeof(bool))
            return bool.Parse(text);
        return Convert.ChangeType(text, realType, CultureInfo.InvariantCulture);
    }
    private static List<string> ParseStringList(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();
        if (text.TrimStart().StartsWith("["))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(text);
                return NormalizeStringList(list);
            }
            catch
            {
                // Если JSON повреждён, пробуем прочитать как обычную строку ниже.
            }
        }
        return NormalizeStringList(text.Split('|', StringSplitOptions.RemoveEmptyEntries));
    }
    private static List<string> NormalizeStringList(IEnumerable<string>? values)
    {
        return values?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => NormalizePathSeparators(x.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? new List<string>();
    }
    private static bool IsPathLikeProperty(PropertyInfo property)
    {
        return property.Name.Contains("Path", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Folder", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("File", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Directory", StringComparison.OrdinalIgnoreCase)
            || property.Name.Contains("Value", StringComparison.OrdinalIgnoreCase);
    }
    private static string NormalizePathSeparators(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        bool isUnc = value.StartsWith(@"\\");
        while (value.Contains(@"\\"))
            value = value.Replace(@"\\", @"\");
        if (isUnc && !value.StartsWith(@"\\"))
            value = @"\" + value;
        return value;
    }
    public static string Escape(string value)
    {
        return value
            .Replace("%", "%25")
            .Replace("\r", "%0D")
            .Replace("\n", "%0A");
    }
    public static string Unescape(string value)
    {
        return value
            .Replace("%0D", "\r", StringComparison.OrdinalIgnoreCase)
            .Replace("%0A", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("%25", "%", StringComparison.OrdinalIgnoreCase);
    }
}
