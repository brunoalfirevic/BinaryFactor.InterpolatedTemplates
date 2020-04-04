// Copyright (c) Bruno Alfirević. All rights reserved.
// Licensed under the MIT license. See license.txt in the project root for license information.

namespace BinaryFactor.InterpolatedTemplates.Samples.Sql
{
    using System;

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
