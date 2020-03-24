using System;
using System.Collections.Generic;

namespace BinaryFactor.SmartIndentation.Samples
{
    public class SqlFormatter: SmartIndentationFormatter
    {
        private readonly IDictionary<string, SqlParam> sqlParameters;

        public SqlFormatter(IDictionary<string, SqlParam> sqlParameters)
        {
            this.sqlParameters = sqlParameters;
        }

        protected override Renderable GetRenderable(FormatArg formatArg)
        {
            return formatArg.Arg switch
            {
                _ when formatArg.HasFormatSpecifier("raw") => Renderable.Create(formatArg),
                FormattableString fs => Renderable.Create(fs),
                IEnumerable<FormattableString> fss => Renderable.Create(fss),
                _ when formatArg.HasFormatSpecifier("pretty") => Renderable.Create(formatArg, conformToAmbientIndentation: true),
                _ => Renderable.Create(formatArg)
            };
        }

        protected override string FormatData(FormatArg formatArg)
        {
            if (formatArg.HasFormatSpecifier("raw", out var rest) || formatArg.HasFormatSpecifier("pretty", out rest))
                return base.FormatData(rest);

            if (formatArg.HasFormatSpecifier("inline"))
                return FormatInline(SqlParam.Wrap(formatArg.Arg));

            return FormatParameter(SqlParam.Wrap(formatArg.Arg));
        }

        private string FormatParameter(SqlParam sqlParameter)
        {
            var parameterName = $"param_{this.sqlParameters.Count}";
            this.sqlParameters.Add(parameterName, sqlParameter);
            return "@" + parameterName;
        }

        private string FormatInline(SqlParam sqlParameter)
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
                    throw new ArgumentException($"Could not print literal of type '{sqlParameter.Value.GetType()}'");
            }
        }
    }
}
