using AppCleaner.Ext;

namespace AppCleaner;

public sealed class ActionSettings
{
    public PathFilterType SearchPathType { get; init; }
    public PathFilterType PlacePathType { get; init; }
    [Saved]
    public string? SearchValue { get; set; }
    [Saved]
    public string? PlaceValue { get; set; }
}
