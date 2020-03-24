namespace BinaryFactor.SmartIndentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

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
            return Render(Renderable.Create($"{o}"), ambientIndentation: "");
        }

        protected virtual string PreprocessTemplate(string str)
        {
            return str;
        }

        protected virtual string FormatData(FormatArg formatArg)
        {
            return FormatDefault(formatArg);
        }

        protected virtual Renderable GetRenderable(FormatArg formatArg)
        {
            return formatArg.Arg switch
            {
                FormattableString fs => Renderable.Create(fs),
                IEnumerable<FormattableString?> fss => Renderable.Create(fss),
                _ => Renderable.Create(formatArg, conformToAmbientIndentation: false),
            };
        }

        protected string FormatDefault(FormatArg formatArg)
        {
            return formatArg.Arg switch
            {
                IFormattable formattable => formattable.ToString(formatArg.FormatSpecifier, this.defaultFormatProvider),
                _ => formatArg.Arg?.ToString() ?? ""
            };
        }

        private string Render(Renderable template, string ambientIndentation)
        {
            return template switch
            {
                CompoundRenderable cr => Render(cr, ambientIndentation),
                RenderableTemplate rt => Render(rt, ambientIndentation),
                RenderableData rd => Render(rd, ambientIndentation),
                _ => throw new ArgumentException(),
            };
        }

        private string Render(CompoundRenderable renderable, string ambientIndentation)
        {
            var renderResults = renderable
                .Renderables
                .Select((template, i) => Render(template, ambientIndentation));

            var combinator = renderable.Combinator ??
                             ((strings, ambientIndentation) => string.Join(Environment.NewLine + ambientIndentation, strings));

            return combinator(renderResults, ambientIndentation);
        }

        private string Render(RenderableData renderable, string ambientIndentation)
        {
            var content = FormatData(renderable.FormatArg);

            if (renderable.ConformToAmbientIndentation)
                content = IndentationProcessor.ProcessStringIndentation(content, this.tabWidth, ambientIndentation);

            return content;
        }

        private string Render(RenderableTemplate renderable, string ambientIndentation)
        {
            static string Replace(string str, string replacement, int startPosition, int endPosition)
            {
                return str
                    .Remove(startPosition, endPosition - startPosition + 1)
                    .Insert(startPosition, replacement);
            }

            var content = renderable.TemplateString;

            content = PreprocessTemplate(content);

            if (renderable.ConformToAmbientIndentation)
                content = IndentationProcessor.ProcessStringIndentation(content, this.tabWidth, ambientIndentation);

            var guidReplacingFormatter = new GuidReplacingFormatter();

            content = string.Format(guidReplacingFormatter, content, renderable.TemplateArguments);

            foreach (var keyValue in guidReplacingFormatter.FormatReplacements)
            {
                var guid = keyValue.Key;
                var formatArg = keyValue.Value;

                var position = StringFindAnalysis.FindWithAnalysis(content, guid);

                var formatArgRenderable = GetRenderable(formatArg);

                if (position.OccupiesEntireLine && formatArgRenderable.IsEmpty && formatArgRenderable.RemoveEntireLineIfEmpty)
                {
                    content = Replace(content, "", position.LineReplaceStartPosition, position.LineReplaceEndPosition);
                }
                else
                {
                    var newAmbientIndentation = position.IsFoundOnFirstLine
                        ? ambientIndentation + position.Indentation
                        : position.Indentation;

                    var replacement = Render(formatArgRenderable, newAmbientIndentation);

                    content = Replace(content, replacement, position.FoundPosition, position.FoundEndPosition);
                }
            }

            return content;
        }

        protected abstract class Renderable
        {
            public static readonly Renderable Empty = new CompoundRenderable(new List<Renderable>(), null);

            public static Renderable Create(FormattableString formattableString)
            {
                if (formattableString == null)
                    return Empty;
                
                return new RenderableTemplate(
                    formattableString.Format,
                    formattableString.GetArguments());
            }

            public static Renderable Create(IEnumerable<FormattableString?> fss, Func<IEnumerable<string>, string, string>? combinator = null)
            {
                var renderables = fss.Where(fs => fs != null).Select(fs => Create(fs!)).ToList();
                return new CompoundRenderable(renderables, combinator);
            }

            public static Renderable Create(FormatArg formatArg, bool conformToAmbientIndentation = false)
            {
                return new RenderableData(formatArg, conformToAmbientIndentation);
            }

            public abstract bool IsEmpty { get; }

            public bool RemoveEntireLineIfEmpty => true;
        }

        class RenderableTemplate : Renderable
        {
            public RenderableTemplate(string templateString, object?[] templateArguments)
            {
                TemplateString = templateString;
                TemplateArguments = templateArguments;
            }

            public string TemplateString { get; }
            public object?[] TemplateArguments { get; }

            public bool ConformToAmbientIndentation => true;
            public override bool IsEmpty => false;
        }

        class RenderableData : Renderable
        {
            public RenderableData(FormatArg formatArg, bool conformToAmbientIndentation)
            {
                FormatArg = formatArg;
                ConformToAmbientIndentation = conformToAmbientIndentation;
            }

            public FormatArg FormatArg { get; }
            public bool ConformToAmbientIndentation { get; }

            public override bool IsEmpty => false;
        }

        class CompoundRenderable : Renderable
        {
            public CompoundRenderable(IList<Renderable> renderables, Func<IEnumerable<string>, string, string>? combinator)
            {
                Renderables = renderables;
                Combinator = combinator;
            }

            public IList<Renderable> Renderables { get; }
            public Func<IEnumerable<string>, string, string>? Combinator { get; }

            public override bool IsEmpty => Renderables.All(t => t.IsEmpty);
        }

        protected class FormatArg
        {
            public FormatArg(object? arg, string? formatSpecifier)
            {
                Arg = arg;
                FormatSpecifier = formatSpecifier;
            }

            public object? Arg { get; }
            public string? FormatSpecifier { get; }

            public bool HasFormatSpecifier(params string[] tests)
            {
                return tests.Any(test => HasFormatSpecifier(test, out var _));
            }

            public bool HasFormatSpecifier(string test, out FormatArg rest)
            {
                if (string.IsNullOrWhiteSpace(FormatSpecifier))
                {
                    rest = this;
                    return false;
                }

                var trimmedFormat = FormatSpecifier.Trim();

                if (trimmedFormat.Equals(test, StringComparison.OrdinalIgnoreCase))
                {
                    rest = new FormatArg(Arg, null);
                    return true;
                }

                if (trimmedFormat.StartsWith(test + ":", StringComparison.OrdinalIgnoreCase))
                {
                    rest = new FormatArg(Arg, trimmedFormat.Substring(test.Length + 1));
                    return true;
                }

                rest = this;
                return false;
            }
        }

        class GuidReplacingFormatter : IFormatProvider, ICustomFormatter
        {
            public IDictionary<string, FormatArg> FormatReplacements { get; } = new Dictionary<string, FormatArg>();

            public string Format(string? format, object? arg, IFormatProvider? formatProvider)
            {
                var guid = Guid.NewGuid().ToString();

                var formatArg = new FormatArg(arg, format);

                FormatReplacements.Add(guid, formatArg);

                return guid;
            }

            public object? GetFormat(Type? formatType)
            {
                return formatType == typeof(ICustomFormatter) ? this : null;
            }
        }

        class IndentationProcessor
        {
            public static string ProcessStringIndentation(string str, int tabWidth, string ambientIndentation)
            {
                var stringLines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                if (stringLines.Length == 0)
                    return "";

                if (stringLines.Length == 1)
                    return stringLines[0];

                var removedEmptyFirstLine = false;
                var firstIndex = 0;
                if (string.IsNullOrWhiteSpace(stringLines[0]))
                {
                    removedEmptyFirstLine = true;
                    firstIndex = 1;
                }

                var baselineIndentationLength = CalculateBaselineIndentationLength(stringLines, tabWidth, removedEmptyFirstLine);

                for (var i = firstIndex; i < stringLines.Length; i++)
                {
                    var line = stringLines[i];

                    if (removedEmptyFirstLine || i > firstIndex)
                        line = RemoveIndentation(line, baselineIndentationLength, tabWidth);

                    if (i > firstIndex && !string.IsNullOrWhiteSpace(line))
                        line = AddIndentation(line, ambientIndentation);

                    stringLines[i] = line;
                }

                return string.Join(Environment.NewLine, stringLines, firstIndex, stringLines.Length - firstIndex);
            }

            private static int CalculateBaselineIndentationLength(IList<string> lines, int tabWidth, bool removedEmptyFirstLine)
            {
                if (removedEmptyFirstLine)
                {
                    return CalculateIndentationLength(
                        lines
                            .Skip(1)
                            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "",
                        tabWidth);
                }
                else
                {
                    return lines
                        .Skip(1)
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(line => CalculateIndentationLength(line, tabWidth))
                        .DefaultIfEmpty()
                        .Min();
                }
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
    }
}
