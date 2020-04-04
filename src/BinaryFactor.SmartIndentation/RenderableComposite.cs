namespace BinaryFactor.SmartIndentation
{
    using System;
    using System.Collections.Generic;

    partial class InterpolatedTemplateProcessor
    {
        class RenderableComposite : Renderable
        {
            public RenderableComposite(IList<Renderable> renderables, Func<IEnumerable<string>, string, string>? combinator)
            {
                Renderables = renderables;
                Combinator = combinator;
            }

            public IList<Renderable> Renderables { get; }
            public Func<IEnumerable<string>, string, string>? Combinator { get; }
        }
    }
}
