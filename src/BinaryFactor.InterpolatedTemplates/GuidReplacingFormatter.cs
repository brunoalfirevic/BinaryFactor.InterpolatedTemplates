// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
{
    using System;
    using System.Collections.Generic;

    partial class InterpolatedTemplateProcessor
    {
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

            public static string Format(string formatString, object?[] arguments, out IDictionary<string, FormatArg> formatArgs)
            {
                var guidReplacingFormatter = new GuidReplacingFormatter();

                var result = string.Format(guidReplacingFormatter, formatString, arguments);

                formatArgs = guidReplacingFormatter.FormatReplacements;
                return result;
            }
        }
    }
}
