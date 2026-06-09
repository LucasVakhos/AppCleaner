using GTranslate.Translators;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace AppCleaner
{
    public partial class FileScanner
    {
        private readonly YandexTranslator _translator = new();
        private const string textExt = ".txt";

        private async Task TranslateEnToRuFolderAsync(CancellationToken cancellationToken)
        {
            var files = Directory
                .EnumerateFiles(_store.SearchFolder, "*.*", SearchOption.AllDirectories)
                .Where(file =>
                    file.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .Where(file => !IsDesignerFile(file))
                .ToArray();

            _store.SetProgressMaximum(files.Length);
            AddToLog($"Файлов для перевода: {files.Length}");

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (await TranslateFileAsync(file, cancellationToken))
                        AddToLog($"[Переведён] {file}");
                    else
                        AddToLog($"[Пропуск] {file}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    AddToLog($"[Ошибка перевода] {file} - {ex.Message}");
                }
                finally
                {
                    CountProcessedFile(file);
                }
            }

            AddToLog("Перевод завершён.");
        }

        private async Task<bool> TranslateFileAsync(string filePath, CancellationToken cancellationToken)
        {
            var encoding = DetectFileEncoding(filePath);
            var source = await File.ReadAllTextAsync(filePath, encoding, cancellationToken);
            var translated = source;

            if (IsText(filePath))
            {
                translated = await TranslateTextAsync(translated, cancellationToken);
            }
            else if (IsCSharp(filePath))
            {
                translated = await TranslateCSharpCommentsAsync(translated, cancellationToken);
            }
            else
            {
                translated = await TranslateTagTextAsync(translated, cancellationToken);
                translated = await TranslateRazorAttributesAsync(translated, cancellationToken);
                translated = await TranslateCSharpStringLiteralsAsync(translated, cancellationToken);
                translated = await TranslateCSharpCommentsAsync(translated, cancellationToken);
            }

            if (string.Equals(source, translated, StringComparison.Ordinal))
                return false;

            CreateBackup(filePath);
            await File.WriteAllTextAsync(filePath, translated, encoding, cancellationToken);

            return true;
        }

        private static bool IsText(string filePath)
        {
            return Path.GetExtension(filePath).Equals(textExt, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCSharp(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".cs", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> TranslateTextAsync(string source, CancellationToken cancellationToken)
        {
            return await TranslateAsync(source, cancellationToken);
        }

        private async Task<string> TranslateRazorAttributesAsync(string source, CancellationToken cancellationToken)
        {
            var regex = new Regex(
                @"(?<name>\b(?:Text|LabelText|Caption|Title|Placeholder|NullText|ErrorMessage|Message)\s*=\s*"")(?<text>[^""@{}<>]*[A-Za-z][^""{}<>]*)(?<end>"")",
                RegexOptions.Compiled);

            return await ReplaceMatchesAsync(source, regex, "text", cancellationToken);
        }

        private async Task<string> TranslateTagTextAsync(string source, CancellationToken cancellationToken)
        {
            var regex = new Regex(
                @"(?<=>)(?<text>[^<>]*[A-Za-z][^<>]*)(?=<)",
                RegexOptions.Compiled | RegexOptions.Singleline);

            return await ReplaceMatchesAsync(source, regex, "text", cancellationToken);
        }

        private async Task<string> TranslateCSharpStringLiteralsAsync(string source, CancellationToken cancellationToken)
        {
            var regex = new Regex(
                @"(?<prefix>(?:ErrorMessage|Name|Message|Title|Description|DisplayName)\s*=\s*""|throw\s+new\s+\w+Exception\s*\(\s*""|message\s*=\s*"")(?<text>(?:\\""|[^""])*[A-Za-z](?:\\""|[^""])*)(?<end>"")",
                RegexOptions.Compiled);

            return await ReplaceMatchesAsync(source, regex, "text", cancellationToken);
        }

        private async Task<string> TranslateCSharpCommentsAsync(string source, CancellationToken cancellationToken)
        {
            source = await TranslateXmlDocCommentsAsync(source, cancellationToken);
            source = await TranslateSlashCommentsAsync(source, cancellationToken);
            source = await TranslateBlockCommentsAsync(source, cancellationToken);

            return source;
        }

        private async Task<string> TranslateXmlDocCommentsAsync(string source, CancellationToken cancellationToken)
        {
            var regex = new Regex(
                @"(?<prefix>^[ \t]*///\s?)(?<text>[A-Za-z][^\r\n]*)",
                RegexOptions.Compiled | RegexOptions.Multiline);

            return await ReplaceMatchesAsync(source, regex, "text", cancellationToken);
        }

        private async Task<string> TranslateSlashCommentsAsync(string source, CancellationToken cancellationToken)
        {
            var regex = new Regex(
                @"(?<prefix>^[ \t]*//\s?)(?<text>[A-Za-z][^\r\n]*)",
                RegexOptions.Compiled | RegexOptions.Multiline);

            return await ReplaceMatchesAsync(source, regex, "text", cancellationToken);
        }

        private async Task<string> TranslateBlockCommentsAsync(string source, CancellationToken cancellationToken)
        {
            var regex = new Regex(
                @"(?<prefix>/\*)(?<text>[\s\S]*?[A-Za-z][\s\S]*?)(?<end>\*/)",
                RegexOptions.Compiled);

            return await ReplaceMatchesAsync(source, regex, "text", cancellationToken);
        }

        private async Task<string> ReplaceMatchesAsync(string source, Regex regex, string groupName, CancellationToken cancellationToken)
        {
            var result = new StringBuilder();
            var lastIndex = 0;

            foreach (Match match in regex.Matches(source))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result.Append(source, lastIndex, match.Index - lastIndex);

                var group = match.Groups[groupName];
                var text = group.Value;

                if (ShouldTranslateText(text))
                {
                    var leading = Regex.Match(text, @"^\s*").Value;
                    var trailing = Regex.Match(text, @"\s*$").Value;
                    var cleanText = text.Trim();
                    var translated = await TranslateAsync(cleanText, cancellationToken);

                    result.Append(match.Value[..(group.Index - match.Index)]);
                    result.Append(leading);
                    result.Append(translated);
                    result.Append(trailing);
                    result.Append(match.Value[(group.Index - match.Index + group.Length)..]);
                }
                else
                {
                    result.Append(match.Value);
                }

                lastIndex = match.Index + match.Length;
            }

            result.Append(source, lastIndex, source.Length - lastIndex);
            return result.ToString();
        }

        private async Task<string> TranslateAsync(string phrase, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(phrase))
                return phrase;

            var result = await _translator.TranslateAsync(phrase, "ru");

            cancellationToken.ThrowIfCancellationRequested();

            return WebUtility.HtmlEncode(result.Translation);
        }

        private static bool ShouldTranslateText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var value = text.Trim();

            if (value.Length < 2)
                return false;

            if (!Regex.IsMatch(value, @"[A-Za-z]"))
                return false;

            if (Regex.IsMatch(value, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                return false;

            if (value.Contains('@') || value.Contains('{') || value.Contains('}') || value.Contains("=>") || value.Contains("nameof", StringComparison.Ordinal))
                return false;

            if (Regex.IsMatch(value, @"\b[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\b"))
                return false;

            if (value.Contains("://", StringComparison.Ordinal))
                return false;

            if (Regex.IsMatch(value, @"^[A-Za-z0-9_./:-]+$") && !Regex.IsMatch(value, @"\s"))
                return false;

            if (Regex.IsMatch(value, @"^(Date|TemperatureC|TemperatureF|Summary|FieldName|Value|Text|Caption|Name|Options|Password|Email|UserName|Id)$", RegexOptions.IgnoreCase))
                return false;

            if (value.StartsWith("/") || value.StartsWith("~/"))
                return false;

            if (value.Contains('=') && !value.Contains(' '))
                return false;

            return true;
        }
    }
}