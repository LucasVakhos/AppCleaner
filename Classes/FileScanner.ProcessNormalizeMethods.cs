using System.Text;
using System.Text.RegularExpressions;

namespace AppCleaner
{
    public partial class FileScanner
    {
        private async Task NormalizeMethodSignaturesFolderAsync(CancellationToken cancellationToken)
        {
            var files = Directory
                .EnumerateFiles(_store.SearchFolder, "*.cs", SearchOption.AllDirectories)
                .Where(file => !IsDesignerFile(file))
                .ToArray();

            _store.SetProgressMaximum(files.Length);
            AddToLog($"Файлов для нормализации сигнатур: {files.Length}");

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await NormalizeMethodSignaturesFileAsync(file, cancellationToken))
                        AddToLog($"[Исправлен] {file}");
                    else
                        AddToLog($"[Пропуск] {file}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AddToLog($"[Ошибка нормализации] {file} - {ex.Message}");
                }
                finally
                {
                    CountProcessedFile(file);
                }
            }

            AddToLog("Нормализация сигнатур завершена.");
        }

        private async Task<bool> NormalizeMethodSignaturesFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var encoding = DetectFileEncoding(filePath);
            var source = await File.ReadAllTextAsync(filePath, encoding, cancellationToken);

            var normalized = FixCollapsedCSharpMembers(source);
            normalized = NormalizeMethodSignatures(normalized);

            if (string.Equals(source, normalized, StringComparison.Ordinal))
                return false;

            CreateBackup(filePath);
            await File.WriteAllTextAsync(filePath, normalized, encoding, cancellationToken);

            return true;
        }

        private static string FixCollapsedCSharpMembers(string source)
        {
            var newline = GetNewLine(source);

            source = Regex.Replace(
                source,
                @";\s+(?=(public|private|protected|internal)\s+)",
                ";" + newline + "    ",
                RegexOptions.Multiline);

            source = Regex.Replace(
                source,
                @"\}\s+(?=(public|private|protected|internal)\s+)",
                "}" + newline + newline + "    ",
                RegexOptions.Multiline);

            source = Regex.Replace(
                source,
                @"(?<=[^\r\n])\s+(?=(public|private|protected|internal)\s+(override|virtual|static|async|sealed|partial|new|unsafe|extern|abstract)?\s*[\w<>\[\],?.]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\()",
                newline + "    ",
                RegexOptions.Multiline);

            return source;
        }

        private static string NormalizeMethodSignatures(string source)
        {
            var lines = SplitLines(source);
            var result = new StringBuilder();

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (!IsMethodSignatureStart(line))
                {
                    result.Append(line);
                    continue;
                }

                var signatureLines = new List<string> { line };
                var balance = CountCharIgnoringStrings(line, '(') - CountCharIgnoringStrings(line, ')');

                while (balance > 0 && i + 1 < lines.Count)
                {
                    i++;

                    var nextLine = lines[i];
                    signatureLines.Add(nextLine);

                    balance += CountCharIgnoringStrings(nextLine, '(') - CountCharIgnoringStrings(nextLine, ')');

                    if (IsUnsafeSignatureLine(nextLine))
                        break;
                }

                var signature = string.Concat(signatureLines);

                if (!IsRealMultilineMethodSignature(signature))
                {
                    foreach (var signatureLine in signatureLines)
                        result.Append(signatureLine);

                    continue;
                }

                result.Append(NormalizeSignatureToSingleLine(signature));
            }

            return result.ToString();
        }

        private static List<string> SplitLines(string source)
        {
            var matches = Regex.Matches(source, @".*?(?:\r\n|\n|\r|$)", RegexOptions.Singleline);

            return matches
                .Select(match => match.Value)
                .Where(value => value.Length > 0)
                .ToList();
        }

        private static bool IsMethodSignatureStart(string line)
        {
            var value = line.TrimStart();

            if (!Regex.IsMatch(value, @"^(public|private|protected|internal)\b"))
                return false;

            if (Regex.IsMatch(value, @"\b(class|record|struct|interface|enum|delegate|event)\b"))
                return false;

            if (value.Contains("=>", StringComparison.Ordinal))
                return false;

            if (value.Contains(';'))
                return false;

            if (!value.Contains('('))
                return false;

            if (value.Contains(')'))
                return false;

            return Regex.IsMatch(
                value,
                @"^(public|private|protected|internal)(\s+(static|virtual|override|abstract|async|sealed|extern|unsafe|new|partial))*\s+[\w<>\[\],?.]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\(");
        }

        private static bool IsRealMultilineMethodSignature(string signature)
        {
            var value = Regex.Replace(signature.Trim(), @"\s+", " ");

            if (Regex.IsMatch(value, @"\b(class|record|struct|interface|enum|delegate|event)\b"))
                return false;

            if (value.Contains("=>", StringComparison.Ordinal))
                return false;

            if (value.Contains(';'))
                return false;

            if (!value.EndsWith("{", StringComparison.Ordinal))
                return false;

            return Regex.IsMatch(
                value,
                @"^(public|private|protected|internal)(\s+(static|virtual|override|abstract|async|sealed|extern|unsafe|new|partial))*\s+[\w<>\[\],?.]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\(.*\)\s*\{$");
        }

        private static bool IsUnsafeSignatureLine(string line)
        {
            var value = line.Trim();

            if (value.Contains("=>", StringComparison.Ordinal))
                return true;

            if (value.EndsWith(";", StringComparison.Ordinal))
                return true;

            if (Regex.IsMatch(value, @"\b(class|record|struct|interface|enum|delegate|event)\b"))
                return true;

            return false;
        }

        private static string NormalizeSignatureToSingleLine(string signature)
        {
            var newline = GetNewLine(signature);
            var indent = Regex.Match(signature, @"^[ \t]*").Value;

            signature = signature.Trim();

            if (signature.EndsWith("{", StringComparison.Ordinal))
                signature = signature[..^1].TrimEnd();

            signature = Regex.Replace(signature, @"\s+", " ");
            signature = Regex.Replace(signature, @"\s*,\s*", ", ");
            signature = Regex.Replace(signature, @"\s*\(\s*", "(");
            signature = Regex.Replace(signature, @"\s*\)\s*", ")");

            return $"{indent}{signature}{newline}{indent}{{{newline}";
        }

        private static string GetNewLine(string value)
        {
            if (value.Contains("\r\n", StringComparison.Ordinal))
                return "\r\n";

            if (value.Contains('\n', StringComparison.Ordinal))
                return "\n";

            if (value.Contains('\r', StringComparison.Ordinal))
                return "\r";

            return Environment.NewLine;
        }

        private static int CountCharIgnoringStrings(string value, char target)
        {
            var count = 0;
            var inString = false;
            var escape = false;

            foreach (var ch in value)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escape = true;
                    continue;
                }

                if (ch == '"')
                {
                    inString = !inString;
                    continue;
                }

                if (!inString && ch == target)
                    count++;
            }

            return count;
        }
    }
}