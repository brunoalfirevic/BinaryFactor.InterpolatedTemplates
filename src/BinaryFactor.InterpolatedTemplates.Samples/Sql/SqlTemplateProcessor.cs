namespace BinaryFactor.InterpolatedTemplates.Samples.Sql
{
    using System;
    using System.Collections.Generic;

    public class SqlTemplateProcessor : InterpolatedTemplateProcessor
    {
        private readonly IDictionary<string, SqlParam> sqlParameters;

        public SqlTemplateProcessor(IDictionary<string, SqlParam> sqlParameters)
        {
            this.sqlParameters = sqlParameters;
        }

        protected override Renderable CreateRenderable(FormatArg formatArg)
        {
            return formatArg.Arg switch
            {
                _ when formatArg.HasFormatSpecifier("raw") => Renderable.CreateData(formatArg),

                IEnumerable<FormattableString> fss when formatArg.HasFormatSpecifier("(AND)") =>
                    Renderable.CreateTemplate(fss.AppendOperatorToExpressions($" AND")),

                IEnumerable<FormattableString> fss when formatArg.HasFormatSpecifier("(OR)") =>
                    Renderable.CreateTemplate(fss.AppendOperatorToExpressions($" OR")),

                IEnumerable<FormattableString> fss when formatArg.HasFormatSpecifier(",") =>
                    Renderable.CreateTemplate(fss, (strings, ambientIndentation) => string.Join(", ", strings)),

                FormattableString fs => Renderable.CreateTemplate(fs),

                IEnumerable<FormattableString> fss => Renderable.CreateTemplate(fss),

                _ when formatArg.HasFormatSpecifier("pretty") => Renderable.CreateData(formatArg, conformToAmbientIndentation: true),

                _ => Renderable.CreateData(formatArg)
            };
        }

        protected override string RenderData(FormatArg formatArg)
        {
            if (formatArg.HasFormatSpecifier(out var rest, "raw", "pretty"))
                return base.RenderData(rest);

            if (formatArg.HasFormatSpecifier("inline"))
                return RenderInline(SqlParam.Wrap(formatArg.Arg));

            return RenderParameter(SqlParam.Wrap(formatArg.Arg));
        }

        private string RenderParameter(SqlParam sqlParameter)
        {
            var parameterName = $"param_{this.sqlParameters.Count}";
            this.sqlParameters.Add(parameterName, sqlParameter);
            return "@" + parameterName;
        }

        private string RenderInline(SqlParam sqlParameter)
        {
            switch (sqlParameter.Value)
            {
                case null:
                case DBNull _:
                    return "NULL";

                case string str:
                    return $"'{str.Replace("'", "''")}'";

                case int _:
                case uint _:
                case long _:
                case ulong _:
                    return sqlParameter.Value.ToString();

                case bool b:
                    return b ? "TRUE" : "FALSE";

                default:
                    throw new ArgumentException($"Could not render literal of type '{sqlParameter.Value.GetType()}'");
            }
        }
    }
}
