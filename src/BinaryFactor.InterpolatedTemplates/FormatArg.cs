// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
{
    using System;
    using System.Linq;

    partial class InterpolatedTemplateProcessor
    {
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
                return HasFormatSpecifier(out var _, tests);
            }

            public bool HasFormatSpecifier(out FormatArg rest, params string[] tests)
            {
                foreach(var test in tests)
                {
                    if (HasOneFormatSpecifier(out rest, test))
                        return true;
                }

                rest = this;
                return false;
            }

            private bool HasOneFormatSpecifier(out FormatArg rest, string test)
            {
                if (string.IsNullOrWhiteSpace(FormatSpecifier))
                {
                    rest = this;
                    return false;
                }

                var trimmedFormat = FormatSpecifier?.Trim() ?? "";

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
    }
}
