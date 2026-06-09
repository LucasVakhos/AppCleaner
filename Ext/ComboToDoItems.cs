//ComboTodoItems
#nullable disable
namespace AppCleaner;

public enum ComboTodoItems
{
    [ComboTodo(
        Name = "Удалить пустые строки...",
        UseBakup = true
        )]
    DeleteEmpty,
    [ComboTodo(
        Name = "Удалить строки #region #endregion...",
        UseBakup = true
        )]
    DeleteRegionRows,
    [ComboTodo(
        Name = "Найти и заменить...",
        UseBakup = true
        )]
    FindAndReplace,
    [ComboTodo(
        Name = "Найти Class или значение в Class и добавить в папку проекта...", 
        OperationTypes = OperationTypes.ProcessFiles
        )]
    FindValueOrClassAddScaveToProject,
    [ComboTodo(
        Name = "Удалить лишние ссылки на namespace...",
        UseBakup = true
        )]
    ClearNameSpace,
    [ComboTodo(
        Name = "Собрать все namespace проекта..."
        )]
    CollectAllNameSpaces,
    [ComboTodo(
        Name = "Собрать нужные using Packages проекта..."
        )]
    CollectUsingPackages,
    [ComboTodo(
        Name = "Удалить *.bak-файлы...", 
        OperationTypes = OperationTypes.ProcessFiles
        )]
    DeleteBakFiles,
    [ComboTodo(
        Name = "Удалить файлы не входящие в проект...", 
        OperationTypes = OperationTypes.ProcessFiles,
        UseBakup = true, 
        SearchLabel = "Cканировать Project:"
        )]
    DeleteNonProjectFiles,
    [ComboTodo(
        Name = "Синхронизировать файл проекта с образцом файла проекта ...",
        UseBakup = true, 
        SearchLabel = "Cканировать Project:", 
        PlaceLabel = "Образец Project:"
        )]
    SyncProjectFileWithSample,
    [ComboTodo(
        Name = "Конвертировать старый .csproj в SDK-style...", 
        SearchLabel = "Старый Project:", 
        PlaceLabel = "Новый Project:",
        UseBakup = true
        )]
    ConvertOldCsprojToSdkStyle,
    [ComboTodo(
        Name = "Перевести английский текст на русский в файлах проекта (включая комментарии)...", 
        Pattern = PatternType.CS,
        UseBakup = true
        )]
    TranslateEnToRu,
    [ComboTodo(
        Name = "Нормализовать сигнатуры методов...",
        OperationTypes = OperationTypes.ProcessFiles,
        UseBakup = true
        )]
    NormalizeMethodSignatures,
    [ComboTodo(
        Name = "Восстановление файлов CSharp из Bak..."
        )]
    RestoreCSharpFilesFromBak,
    [ComboTodo(
        Name = "Восстановление using в указанном проекте...",
        UseBakup = true, 
        SearchLabel = "Recovery project:", 
        PlaceLabel = "Sample project:"
        )]
    RestoreMissingUsings,
    [ComboTodo(
        Name = "Добавить комментарий /*Путь к файлу*/ к файлам .сs в папке...", 
        OperationTypes = OperationTypes.ProcessFiles
        )]
    AddFilePathCommentToCsFiles
}
