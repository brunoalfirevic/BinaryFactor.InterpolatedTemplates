namespace BinaryFactor.SmartIndentation
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
