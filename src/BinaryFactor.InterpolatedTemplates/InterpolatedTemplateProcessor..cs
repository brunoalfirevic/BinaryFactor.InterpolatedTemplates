// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    partial class InterpolatedTemplateProcessor
    {
        private readonly int tabWidth;
        private readonly IFormatProvider defaultFormatProvider;

        public InterpolatedTemplateProcessor(int? tabWidth = null, IFormatProvider? defaultFormatProvider = null)
        {
            this.tabWidth = tabWidth ?? 4;
            this.defaultFormatProvider = defaultFormatProvider ?? CultureInfo.InvariantCulture;
        }

        public string Render(object? o)
        {
            return DoRender(Renderable.CreateTemplate($"{o}"), ambientIndentation: "");
        }

        protected virtual Renderable CreateRenderable(FormatArg formatArg)
        {
            return formatArg.Arg switch
            {
                FormattableString fs => Renderable.CreateTemplate(fs),
                IEnumerable<FormattableString?> fss => Renderable.CreateTemplate(fss),
                _ => Renderable.CreateData(formatArg, conformToAmbientIndentation: false),
            };
        }

        protected virtual string RenderData(FormatArg formatArg)
        {
            return formatArg.Arg switch
            {
                IFormattable formattable => formattable.ToString(formatArg.FormatSpecifier, this.defaultFormatProvider),
                _ => formatArg.Arg?.ToString() ?? ""
            };
        }

        protected virtual string PreprocessTemplate(string template)
        {
            return template;
        }

        private string DoRender(Renderable template, string ambientIndentation)
        {
            return template switch
            {
                RenderableComposite cr => DoRender(cr, ambientIndentation),
                RenderableTemplate rt => DoRender(rt, ambientIndentation),
                RenderableData rd => DoRender(rd, ambientIndentation),
                RenderableBlank _ => "",
                _ => throw new ArgumentException(),
            };
        }

        private string DoRender(RenderableComposite renderable, string ambientIndentation)
        {
            var renderResults = renderable.Renderables
                .Where(renderable => !(renderable is RenderableBlank))
                .Select(renderable => DoRender(renderable, ambientIndentation));

            var combinator = renderable.Combinator ??
                             ((strings, ambientIndentation) => string.Join(Environment.NewLine + ambientIndentation, strings));

            return combinator(renderResults, ambientIndentation);
        }

        private string DoRender(RenderableTemplate renderable, string ambientIndentation)
        {
            static string Replace(string str, string replacement, int startPosition, int endPosition)
            {
                return str
                    .Remove(startPosition, endPosition - startPosition + 1)
                    .Insert(startPosition, replacement);
            }

            var content = renderable.TemplateString;

            content = PreprocessTemplate(content);

            content = IndentationProcessor.ProcessStringIndentation(content, this.tabWidth, ambientIndentation);

            content = GuidReplacingFormatter.Format(content, renderable.TemplateArguments, out var formatReplacements);

            foreach (var keyValue in formatReplacements)
            {
                var guid = keyValue.Key;
                var formatArg = keyValue.Value;

                var position = StringFindAnalysis.FindWithAnalysis(content, guid);

                var formatArgRenderable = CreateRenderable(formatArg);

                if (position.OccupiesEntireLine && formatArgRenderable is RenderableBlank)
                {
                    content = Replace(content, "", position.LineReplaceStartPosition, position.LineReplaceEndPosition);
                }
                else
                {
                    var newAmbientIndentation = position.IsFoundOnFirstLine
                        ? ambientIndentation + position.Indentation
                        : position.Indentation;

                    var replacement = DoRender(formatArgRenderable, newAmbientIndentation);

                    content = Replace(content, replacement, position.FoundPosition, position.FoundEndPosition);
                }
            }

            return content;
        }

        private string DoRender(RenderableData renderable, string ambientIndentation)
        {
            var content = RenderData(renderable.FormatArg);

            if (renderable.ConformToAmbientIndentation)
                content = IndentationProcessor.ProcessStringIndentation(content, this.tabWidth, ambientIndentation);

            return content;
        }
    }
}
