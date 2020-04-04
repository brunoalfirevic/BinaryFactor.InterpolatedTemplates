using Shouldly;
using System;

namespace BinaryFactor.InterpolatedTemplates.Tests
{
    public class InterpolatedTemplateProcessorTests
    {
        public void TestVanillaTemplates()
        {
            var lines = new FormattableString[]
            {
                $"Write('one');",
                $"Write('two');",
            };

            FormattableString program = $@"
                procedure PrintNumbers()
                begin
                    {lines}
                end;";

            var rendered = new InterpolatedTemplateProcessor().Render(program);

            rendered.ShouldBe(@"procedure PrintNumbers()
begin
    Write('one');
    Write('two');
end;");
        }
    }
}
