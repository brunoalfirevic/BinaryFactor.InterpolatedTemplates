namespace BinaryFactor.InterpolatedTemplates
{
    using System;

    partial class InterpolatedTemplateProcessor
    {
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
