namespace BinaryFactor.SmartIndentation.Samples.Html
{
    using System.Net;

    public class HtmlTemplateProcessor : InterpolatedTemplateProcessor
    {
        protected override Renderable CreateRenderable(FormatArg formatArg)
        {
            if (formatArg.HasFormatSpecifier(out var rest, "raw"))
                return Renderable.CreateData(rest);

            if (formatArg.HasFormatSpecifier(out rest, "pretty"))
                return Renderable.CreateData(rest, conformToAmbientIndentation: true);

            return base.CreateRenderable(formatArg);
        }

        protected override string RenderData(FormatArg formatArg)
        {
            if (formatArg.HasFormatSpecifier(out var rest, "raw", "prety"))
                return base.RenderData(rest);

            var result = base.RenderData(formatArg);
            return WebUtility.HtmlEncode(result);
        }
    }
}
