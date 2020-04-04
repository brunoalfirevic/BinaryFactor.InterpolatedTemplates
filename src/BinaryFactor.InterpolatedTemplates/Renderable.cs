namespace BinaryFactor.InterpolatedTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    partial class InterpolatedTemplateProcessor
    {
        protected abstract class Renderable
        {
            public static readonly Renderable Blank = new RenderableBlank();

            public static Renderable CreateTemplate(FormattableString formattableString)
            {
                if (formattableString == null)
                    return Blank;
                
                return new RenderableTemplate(formattableString.Format, formattableString.GetArguments());
            }

            public static Renderable CreateTemplate(IEnumerable<FormattableString?> fss, Func<IEnumerable<string>, string, string>? combinator = null)
            {
                var renderables = fss
                    .Where(fs => fs != null)
                    .Select(fs => CreateTemplate(fs!))
                    .ToList();

                if (!renderables.Any())
                    return Blank;

                return new RenderableComposite(renderables, combinator);
            }

            public static Renderable CreateData(FormatArg formatArg, bool conformToAmbientIndentation = false)
            {
                return new RenderableData(formatArg, conformToAmbientIndentation);
            }
        }
    }
}
