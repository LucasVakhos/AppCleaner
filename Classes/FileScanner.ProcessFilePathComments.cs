// Classes\FileScanner.ProcessFilePathComments.cs
namespace AppCleaner;

public partial class FileScanner
{
    private void AddFilePathCommentToCsFiles(CancellationToken cancellationToken)
    {
        var rootFolder = _store.SearchFolder;

        var files = GetFilesForOperation(
            rootFolder,
            "*.cs",
            cancellationToken,
            preferProjectFiles: false,
            includeDesignerFiles: false);

        _store.SetProgressMaximum(files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var relativePath = Path
                    .GetRelativePath(rootFolder, file)
                    .Replace("/", "\\");

                var comment = $"//{relativePath}";

                var encoding = DetectFileEncoding(file);
                var text = File.ReadAllText(file, encoding);

                // уже есть такой comment
                if (text.StartsWith(comment + Environment.NewLine))
                {
                    CountProcessedFile(file);
                    continue;
                }

                CreateBackup(file);

                File.WriteAllText(
                    file,
                    comment + Environment.NewLine + text,
                    encoding);

                AddToLog(comment);
            }
            catch (Exception ex)
            {
                AddToLog($"ERROR {file}: {ex.Message}");
            }
            finally
            {
                CountProcessedFile(file);
            }
        }
    }
}