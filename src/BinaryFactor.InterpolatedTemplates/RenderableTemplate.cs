// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates
{
    partial class InterpolatedTemplateProcessor
    {
        class RenderableTemplate : Renderable
        {
            public RenderableTemplate(string templateString, object?[] templateArguments)
            {
                TemplateString = templateString;
                TemplateArguments = templateArguments;
            }

            public string TemplateString { get; }
            public object?[] TemplateArguments { get; }
        }
    }
}
