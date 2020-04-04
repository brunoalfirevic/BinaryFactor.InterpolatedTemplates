// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    partial class InterpolatedTemplateProcessor
    {
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
    }
}
