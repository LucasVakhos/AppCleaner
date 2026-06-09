using System.Text.RegularExpressions;

namespace AppCleaner;

public partial class FileScanner
{
    public void RecoveryMissingUsings(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string firstProjectPath = _store.ProjectFile;
        string secondProjectPath = _store.SampleProjectFile;

        var firstFiles = Directory
            .GetFiles(firstProjectPath, "*.cs", SearchOption.AllDirectories)
            .Where(x => !IsIgnored(x))
            .ToDictionary(
                x => Path.GetRelativePath(firstProjectPath, x),
                x => x,
                StringComparer.OrdinalIgnoreCase);

        cancellationToken.ThrowIfCancellationRequested();

        var secondFiles = Directory
            .GetFiles(secondProjectPath, "*.cs", SearchOption.AllDirectories)
            .Where(x => !IsIgnored(x))
            .ToList();

        foreach (var secondFile in secondFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(secondProjectPath, secondFile);

            if (!firstFiles.TryGetValue(relativePath, out var firstFile))
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            var firstText = File.ReadAllText(firstFile);
            var secondText = File.ReadAllText(secondFile);

            cancellationToken.ThrowIfCancellationRequested();

            var firstUsings = GetUsings(firstText);

            var secondUsings = GetUsings(secondText)
                .Where(u =>
                    u.StartsWith("using System", StringComparison.Ordinal) ||
                    u.StartsWith("using DevExpress", StringComparison.Ordinal))
                .Where(u => !firstUsings.Contains(u))
                .OrderBy(u => u)
                .ToList();

            if (secondUsings.Count == 0)
                continue;

            cancellationToken.ThrowIfCancellationRequested();

            var updatedText = InsertUsings(firstText, secondUsings);

            cancellationToken.ThrowIfCancellationRequested();

            File.WriteAllText(firstFile, updatedText);

            AddToLog($"Updated: {relativePath}");

            foreach (var u in secondUsings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddToLog($"  + {u}");
            }
        }

        AddToLog("RecoveryMissingUsings completed.");
    }

    private static HashSet<string> GetUsings(string text)
    {
        var matches = Regex.Matches(
            text,
            @"^\s*using\s+[\w\.]+;\s*$",
            RegexOptions.Multiline);

        return matches
            .Select(m => m.Value.Trim())
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string InsertUsings(string text, List<string> usingsToAdd)
    {
        var lines = text
            .Replace("\r\n", "\n")
            .Split('\n')
            .ToList();

        var lastUsingIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (Regex.IsMatch(lines[i], @"^\s*using\s+[\w\.]+;\s*$"))
                lastUsingIndex = i;
        }

        if (lastUsingIndex >= 0)
        {
            lines.InsertRange(lastUsingIndex + 1, usingsToAdd);
        }
        else
        {
            lines.InsertRange(0, usingsToAdd);
            lines.Insert(usingsToAdd.Count, "");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsIgnored(string path)
    {
        return path.Contains(@"\bin\", StringComparison.OrdinalIgnoreCase)
            || path.Contains(@"\obj\", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase);
    }
}