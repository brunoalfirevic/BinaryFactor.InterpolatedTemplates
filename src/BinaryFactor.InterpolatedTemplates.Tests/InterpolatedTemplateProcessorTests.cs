// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates.Tests
{
    using Shouldly;
    using System;

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
