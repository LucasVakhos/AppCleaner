#nullable disable
namespace AppCleaner;

public class ComboTodoAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;
    public PatternType Pattern { get; set; } = PatternType.CS;
    public string SearchLabel { get; set; } = "Cканировать папку:";
    public string PlaceLabel { get; set; } = "Папка для найденного:";
    public OperationTypes OperationTypes { get; set; } = OperationTypes.ProcessFiles;
    public bool UseBakup { get; set; } = false;

}
