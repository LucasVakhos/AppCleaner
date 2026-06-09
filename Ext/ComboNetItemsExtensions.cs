namespace AppCleaner
{
    public static class ComboNetItemsExtensions
    {
        public static string ToTargetFramework(this ComboNetItems item)
        {
            return item switch
            {
                ComboNetItems.net50 => "net5.0-windows",
                ComboNetItems.net60 => "net6.0-windows",
                ComboNetItems.net70 => "net7.0-windows",
                ComboNetItems.net80 => "net8.0-windows",
                ComboNetItems.net90 => "net9.0-windows",
                ComboNetItems.net100 => "net10.0-windows",
                _ => "net8.0-windows"
            };
        }
    }
}
