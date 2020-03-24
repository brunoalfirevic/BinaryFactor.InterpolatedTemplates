namespace BinaryFactor.SmartIndentation.Samples
{
    using System.Net;

    public class HtmlFormatter : SmartIndentationFormatter
    {
        public HtmlFormatter() 
        {
        }

        protected override Renderable GetRenderable(FormatArg formatArg)
        {
            if (formatArg.HasFormatSpecifier("raw", out var rest))
            {
                return Renderable.Create(rest);
            }

            if (formatArg.HasFormatSpecifier("pretty", out rest))
            {
                return Renderable.Create(rest, preprocess: true, conformToAmbientIndentation: true);
            }

            return base.GetRenderable(formatArg);
        }

        protected override string FormatData(FormatArg formatArg)
        {
            var result = base.FormatData(formatArg);
            return WebUtility.HtmlEncode(result);
        }
    }
}
