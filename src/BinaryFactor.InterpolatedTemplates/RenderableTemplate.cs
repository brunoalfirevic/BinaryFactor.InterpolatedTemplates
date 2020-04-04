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
