namespace BinaryFactor.SmartIndentation
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class HtmlFormatter : SmartIndentationFormatter
    {
        public HtmlFormatter(int? tabWidth = null, IFormatProvider? defaultFormatProvider = null) 
            : base(tabWidth, defaultFormatProvider)
        {
        }

        protected override bool ShouldFormatWithSmartIndentation(FormatData formatData, out IEnumerable<FormattableString>? formattableStrings, out bool removeEntireLineIfEmpty)
        {
            if (formatData.HasFormatSpecifier("raw"))
            {
                formattableStrings = null;
                removeEntireLineIfEmpty = false;
                return false;
            }

            if (formatData.HasFormatSpecifier("pretty", out var rest))
            { 
                var arg = base.FormatDefault(rest);
                formattableStrings = new[] { FormattableStringFactory.Create(arg) };
                removeEntireLineIfEmpty = true;
                return true;
            }

            return base.ShouldFormatWithSmartIndentation(formatData, out formattableStrings, out removeEntireLineIfEmpty);
        }

        protected override string FormatDefault(FormatData formatData)
        {
            return base.FormatDefault(formatData);
        }
    }
}
