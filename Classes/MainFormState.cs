namespace AppCleaner.Classes;

public sealed class MainFormState
{
    [Saved]
    public int X { get; set; }
    [Saved]
    public int Y { get; set; }
    [Saved]
    public int Width { get; set; }
    [Saved]
    public int Height { get; set; }
    [Saved]
    public FormWindowState WindowState { get; set; }
}
