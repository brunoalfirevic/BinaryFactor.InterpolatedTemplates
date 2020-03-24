using System;

namespace BinaryFactor.SmartIndentation.Samples
{
    public class SqlParam
    {
        public SqlParam(object? value)
            : this(value, value?.GetType())
        {
        }

        public SqlParam(object? value, Type? type)
        {
            Value = value;
            Type = type;
        }

        public object? Value { get; }
        public Type? Type { get; }

        public static SqlParam Wrap(object? value) => value is SqlParam sqlParam ? sqlParam : new SqlParam(value);
    }
}
