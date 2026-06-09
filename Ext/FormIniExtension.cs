using AppCleaner.Classes;
namespace AppCleaner;

public static class FormIniExtension
{
    public static void SaveState(this Form form)
    {
        var bounds = form.WindowState == FormWindowState.Normal
            ? form.Bounds
            : form.RestoreBounds;
        var state = new MainFormState
        {
            X = bounds.X,
            Y = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            WindowState = form.WindowState
        };
        var ini = new IniFile();
        ini.SaveObject(state);
        ini.Save();
    }
    public static void LoadState(this Form form)
    {
        var state = new MainFormState();
        var ini = new IniFile();
        ini.LoadObject(state);
        if (state.Width <= 0 || state.Height <= 0)
            return;
        form.StartPosition = FormStartPosition.Manual;
        form.Bounds = new Rectangle(
            state.X,
            state.Y,
            state.Width,
            state.Height);
        form.WindowState = state.WindowState;
    }
}
