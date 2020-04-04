// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
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
