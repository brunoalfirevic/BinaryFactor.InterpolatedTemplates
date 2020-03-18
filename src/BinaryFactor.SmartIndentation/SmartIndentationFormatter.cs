namespace BinaryFactor.SmartIndentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;

    partial class SmartIndentationFormatter
    {
        private readonly int tabWidth;
        private readonly IFormatProvider defaultFormatProvider;

        public SmartIndentationFormatter(int? tabWidth = null, IFormatProvider? defaultFormatProvider = null)
        {
            this.tabWidth = tabWidth ?? 4;
            this.defaultFormatProvider = defaultFormatProvider ?? CultureInfo.InvariantCulture;
        }

        public string Format(object? o)
        {
            return Format($"{o}", "", firstLineNeedsAmbientIndentation: false);
        }

        protected virtual string PreprocessFormatString(string str)
        {
            return str;
        }

        protected virtual bool ShouldFormatWithSmartIndentation(FormatData formatData, out IEnumerable<FormattableString>? formattableStrings, out bool removeEntireLineIfEmpty)
        {
            switch (formatData.Arg)
            {
                case FormattableString fs:
                {
                    formattableStrings = new[] { fs };
                    removeEntireLineIfEmpty = true;
                    return true;
                }

                case IEnumerable<FormattableString> fss:
                {
                    formattableStrings = fss;
                    removeEntireLineIfEmpty = true;
                    return true;
                }

                default:
                {
                    formattableStrings = null;
                    removeEntireLineIfEmpty = false;
                    return false;
                }
            }
        }

        protected virtual string FormatDefault(FormatData formatData)
        {
            return formatData.Arg switch
            {
                IFormattable formattable => formattable.ToString(formatData.FormatSpecifier, this.defaultFormatProvider),
                _ => formatData.Arg?.ToString() ?? ""
            };
        }

        private string Format(FormattableString formattableString, string ambientIndentation, bool firstLineNeedsAmbientIndentation)
        {
            var processedFormatString = PreprocessFormatString(formattableString.Format);

            processedFormatString = IndentationProcessor.ProcessStringIndentation(processedFormatString, this.tabWidth, ambientIndentation, firstLineNeedsAmbientIndentation);

            var guidReplacingFormatter = new GuidReplacingFormatter();

            var result = string.Format(guidReplacingFormatter, processedFormatString, formattableString.GetArguments());

            foreach (var formatReplacement in guidReplacingFormatter.FormatReplacements)
            {
                var replacementInstructions = FormatArgument(result, formatReplacement, ambientIndentation, firstLineNeedsAmbientIndentation);
                result = replacementInstructions.ExecuteReplacement(result);
            }

            return result;
        }

        private ReplacementInstructions FormatArgument(string formatString, FormatData formatData, string ambientIndentation, bool firstLineNeededAmbientIndentation)
        {
            var findResult = StringFindAnalysis.FindWithAnalysis(formatString, formatData.Guid);

            if (ShouldFormatWithSmartIndentation(formatData, out var fss, out var removeEntireLineIfEmpty))
            {
                fss = fss.Where(fs => fs != null);

                if (removeEntireLineIfEmpty && findResult.OccupiesEntireLine && !fss.Any())
                    return new ReplacementInstructions("", findResult.LineReplaceStartPosition, findResult.LineReplaceEndPosition);

                var newAmbientIndentation = !findResult.IsFoundOnFirstLine || firstLineNeededAmbientIndentation
                    ? findResult.Indentation
                    : ambientIndentation + findResult.Indentation;

                var replacement = string.Join(
                    Environment.NewLine,
                    fss.Select((fs, i) => Format(fs, newAmbientIndentation, i > 0)));

                return new ReplacementInstructions(replacement, findResult.FoundPosition, findResult.FoundEndPosition);
            }
            else
            {
                var replacement = FormatDefault(formatData);
                return new ReplacementInstructions(replacement, findResult.FoundPosition, findResult.FoundEndPosition);
            }
        }

        protected class Template
        {
            public Template(FormattableString formattableString)
            {
                TemplateString = formattableString.Format;
                TemplateArguments = formattableString.GetArguments() ?? new object[] { };
            }

            public Template(string plainString)
            {
                TemplateString = plainString;
                TemplateArguments = new object[] { };
            }

            public string TemplateString { get; }
            public IList<object?> TemplateArguments { get; }
        }

        protected class FormatData
        {
            public FormatData(string guid, string? formatSpecifier, object? arg, IFormatProvider? formatProvider)
            {
                Guid = guid;
                FormatSpecifier = formatSpecifier;
                Arg = arg;
                FormatProvider = formatProvider;
            }

            public string Guid { get; }
            public string? FormatSpecifier { get; }
            public object? Arg { get; }
            public IFormatProvider? FormatProvider { get; }

            public bool HasFormatSpecifier(params string[] tests)
            {
                return tests.Any(test => HasFormatSpecifier(test, out var _));
            }

            public bool HasFormatSpecifier(string test, out FormatData rest)
            {
                if (string.IsNullOrWhiteSpace(FormatSpecifier))
                {
                    rest = this;
                    return false;
                }

                var trimmedFormat = FormatSpecifier.Trim();

                if (trimmedFormat.Equals(test, StringComparison.OrdinalIgnoreCase))
                {
                    rest = new FormatData(Guid, null, Arg, FormatProvider);
                    return true;
                }

                if (trimmedFormat.StartsWith(test + ":", StringComparison.OrdinalIgnoreCase))
                {
                    rest = new FormatData(Guid, trimmedFormat.Substring(test.Length + 1), Arg, FormatProvider);
                    return true;
                }

                rest = this;
                return false;
            }
        }

        class GuidReplacingFormatter : IFormatProvider, ICustomFormatter
        {
            public IList<FormatData> FormatReplacements { get; } = new List<FormatData>();

            public string Format(string? format, object? arg, IFormatProvider? formatProvider)
            {
                var guid = Guid.NewGuid().ToString();
                
                var formatData = new FormatData(guid, format, arg, formatProvider);

                FormatReplacements.Add(formatData);

                return guid;
            }

            public object? GetFormat(Type? formatType)
            {
                return formatType == typeof(ICustomFormatter) ? this : null;
            }
        }

        class IndentationProcessor
        {
            public static string ProcessStringIndentation(string str, int tabWidth, string ambientIndentation, bool firstLineNeedsAmbientIndentation)
            {
                var stringLines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if (stringLines.Length == 0)
                    return "";

                if (stringLines.Length == 1)
                {
                    var line = stringLines[0];
                    if (firstLineNeedsAmbientIndentation && !string.IsNullOrWhiteSpace(line))
                        line = AddIndentation(line, ambientIndentation);

                    return line;
                }

                var removedEmptyFirstLine = false;
                var firstIndex = 0;
                if (string.IsNullOrWhiteSpace(stringLines[0]))
                {
                    removedEmptyFirstLine = true;
                    firstIndex = 1;
                }

                var baselineIndentationLength = CalculateIndentationLength(
                    stringLines
                        .Skip(removedEmptyFirstLine ? 0 : 1)
                        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "",
                    tabWidth);

                for (var i = firstIndex; i < stringLines.Length; i++)
                {
                    var line = stringLines[i];

                    if (removedEmptyFirstLine || i > firstIndex)
                        line = RemoveIndentation(line, baselineIndentationLength, tabWidth);

                    if ((firstLineNeedsAmbientIndentation || i > firstIndex) && !string.IsNullOrWhiteSpace(line))
                        line = AddIndentation(line, ambientIndentation);

                    stringLines[i] = line;
                }

                return string.Join(Environment.NewLine, firstIndex, stringLines.Length - firstIndex);
            }

            private static string AddIndentation(string str, string indentation)
            {
                return indentation + str;
            }

            private static string RemoveIndentation(string line, int indentationLengthToRemove, int tabWidth)
            {
                var i = 0;
                var indentationLength = 0;

                while (i < line.Length && char.IsWhiteSpace(line[i]) && indentationLength < indentationLengthToRemove)
                {
                    indentationLength += CharWidth(line[i], indentationLength, tabWidth);
                    i++;
                }

                return line.Remove(0, i);
            }

            private static int CalculateIndentationLength(string line, int tabWidth)
            {
                var i = 0;
                var result = 0;

                while (i < line.Length && char.IsWhiteSpace(line[i]))
                {
                    result += CharWidth(line[i], result, tabWidth);
                    i++;
                }

                return result;
            }

            static int CharWidth(char c, int indentation, int tabWidth)
            {
                return c == '\t'
                    ? tabWidth - (indentation % tabWidth)
                    : 1;
            }
        }

        class StringFindAnalysis
        {
            public StringFindAnalysis(
                int foundPosition,
                int foundEndPosition,
                int lineReplaceStartPosition,
                int lineReplaceEndPosition,
                string indentation,
                bool occupiesEntireLine,
                bool isFoundOnFirstLine)
            {
                FoundPosition = foundPosition;
                FoundEndPosition = foundEndPosition;
                LineReplaceStartPosition = lineReplaceStartPosition;
                LineReplaceEndPosition = lineReplaceEndPosition;
                Indentation = indentation;
                OccupiesEntireLine = occupiesEntireLine;
                IsFoundOnFirstLine = isFoundOnFirstLine;
            }

            public int FoundPosition { get; }
            public int FoundEndPosition { get; }
            public int LineReplaceStartPosition { get; }
            public int LineReplaceEndPosition { get; set; }
            public string Indentation { get; }
            public bool OccupiesEntireLine { get; }
            public bool IsFoundOnFirstLine { get; }

            public static StringFindAnalysis FindWithAnalysis(string str, string searchStr)
            {
                int FindLineStart(int fromPosition, out string indentation, out bool foundNonWhitespaceBeforeOccurence)
                {
                    indentation = "";
                    foundNonWhitespaceBeforeOccurence = false;

                    var lineStart = fromPosition - 1;

                    while (lineStart >= 0 && str[lineStart] != '\r' && str[lineStart] != '\n')
                    {
                        if (!char.IsWhiteSpace(str[lineStart]))
                            foundNonWhitespaceBeforeOccurence = true;

                        indentation = (char.IsWhiteSpace(str[lineStart]) ? str[lineStart] : ' ') + indentation;
                        lineStart--;
                    }
                    lineStart++;

                    return lineStart;
                }

                int FindLineEnd(int fromPosition, out bool foundNonWhitespaceAfterOccurence)
                {
                    foundNonWhitespaceAfterOccurence = false;

                    var lineEnd = fromPosition + 1;

                    while (lineEnd < str.Length && str[lineEnd] != '\r' && str[lineEnd] != '\n')
                    {
                        if (!char.IsWhiteSpace(str[lineEnd]))
                            foundNonWhitespaceAfterOccurence = true;

                        lineEnd++;
                    }
                    lineEnd--;

                    return lineEnd;
                }

                void EatCharacter(ref int position, char c, Func<int, int> newPositionGetter)
                {
                    var newPosition = newPositionGetter(position);

                    if (newPosition >= 0 && newPosition < str.Length && str[newPosition] == c)
                        position = newPosition;
                }

                var foundPosition = str.IndexOf(searchStr, StringComparison.Ordinal);

                var lineReplaceStart = FindLineStart(foundPosition, out var indentation, out var foundNonWhitespaceBeforeOccurence);
                var lineReplaceEnd = FindLineEnd(foundPosition + searchStr.Length - 1, out var foundNonWhitespaceAfterOccurence);

                var isFoundOnFirstLine = lineReplaceStart == 0;
                var isFoundOnLastLine = lineReplaceEnd == str.Length - 1;

                switch ((isFoundOnFirstLine, isFoundOnLastLine))
                {
                    case (_, isFoundOnLastLine: false):
                        EatCharacter(ref lineReplaceEnd, '\r', i => i + 1);
                        EatCharacter(ref lineReplaceEnd, '\n', i => i + 1);
                        break;

                    case (isFoundOnFirstLine: false, isFoundOnLastLine: true):
                        EatCharacter(ref lineReplaceStart, '\n', i => i - 1);
                        EatCharacter(ref lineReplaceStart, '\r', i => i - 1);
                        break;
                }

                return new StringFindAnalysis(
                    foundPosition: foundPosition,
                    foundEndPosition: foundPosition + searchStr.Length - 1,
                    lineReplaceStartPosition: lineReplaceStart,
                    lineReplaceEndPosition: lineReplaceEnd,
                    indentation: indentation,
                    occupiesEntireLine: !foundNonWhitespaceBeforeOccurence && !foundNonWhitespaceAfterOccurence,
                    isFoundOnFirstLine);
            }
        }

        class ReplacementInstructions
        {
            public ReplacementInstructions(string replacement, int replacePositionStart, int replacePositionEnd)
            {
                Replacement = replacement;
                ReplacePositionStart = replacePositionStart;
                ReplacePositionEnd = replacePositionEnd;
            }

            public string Replacement { get; }
            public int ReplacePositionStart { get; }
            public int ReplacePositionEnd { get; }

            public string ExecuteReplacement(string str)
            {
                return str
                    .Remove(ReplacePositionStart, ReplacePositionEnd - ReplacePositionStart + 1)
                    .Insert(ReplacePositionStart, Replacement);
            }
        }
    }
}
