// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
{
    partial class InterpolatedTemplateProcessor
    {
        class RenderableData : Renderable
        {
            public RenderableData(FormatArg formatArg, bool conformToAmbientIndentation)
            {
                FormatArg = formatArg;
                ConformToAmbientIndentation = conformToAmbientIndentation;
            }

            public FormatArg FormatArg { get; }
            public bool ConformToAmbientIndentation { get; }
        }
    }
}
