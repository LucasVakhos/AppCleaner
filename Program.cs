using System.Runtime.Loader;

namespace AppCleaner;

static class Program
{
    /// <summary>
    /// Основная точка входа в приложение.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Подключаем resolver для загрузки DLL из папки "libs"
        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            try
            {
                var libsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs");
                var assemblyPath = Path.Combine(libsPath, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }
            }
            catch
            {
                // Игнорируем ошибки - попробует загрузить стандартным способом
            }
            return null;
        };

        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
        {
            try
            {
                var assemblyName = new System.Reflection.AssemblyName(e.Name).Name;
                var libsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs");
                var assemblyPath = Path.Combine(libsPath, $"{assemblyName}.dll");
                
                if (File.Exists(assemblyPath))
                {
                    return System.Reflection.Assembly.LoadFrom(assemblyPath);
                }
            }
            catch
            {
                // Игнорируем ошибки - попробует загрузить стандартным способом
            }
            return null;
        };

        // Настройка конфигурации приложения

        //Позволяет отслеживать источник. Удалите следующую строку в релизной версии проекта.
        DevExpress.Utils.Localization.XtraLocalizer.EnableTraceSource();

        //Раскомментируйте следующую строку в релизной версии.
        //DevExpress.Utils.Localization.XtraLocalizer.UserResourceManager = DXLocalization.ResourceManager;
        ApplicationConfiguration.Initialize();
        //string libsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libs");
        //Console.WriteLine("=== Содержимое папки Libs ===");
        //if (Directory.Exists(libsPath))
        //{
        //    foreach (var file in Directory.GetFiles(libsPath, "*.dll"))
        //    {
        //        Console.WriteLine($"  - {Path.GetFileName(file)}");
        //    }
        //}
        //else
        //{
        //    Console.WriteLine("Папка Libs не существует!");
        //}
        // Запуск основного окна
        Application.Run(new MainForm());
    }
}
