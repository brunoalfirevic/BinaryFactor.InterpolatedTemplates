namespace BinaryFactor.SmartIndentation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    static partial class FormattableStringEnumerableExtensions
    {
        public static FormattableString JoinBy(this IEnumerable<FormattableString> enumerable, FormattableString separator)
        {
            if (!enumerable.Any())
                return $"";

            return enumerable.Skip(1).Aggregate(enumerable.First(), (accumulated, fs) => $"{accumulated}{separator}{fs}");
        }

        public static IList<FormattableString> AppendOperatorToExpressions(this IEnumerable<FormattableString> enumerable, FormattableString @operator)
        {
            var count = enumerable.Count();
            if (count > 1)
                enumerable = enumerable.SelectFS(fs => $"({fs})");

            return enumerable.AppendToEach(@operator);
        }

        public static IList<FormattableString> AppendToEach(this IEnumerable<FormattableString> enumerable, FormattableString separator)
        {
            var count = enumerable.Count();
            return enumerable.Select((fs, i) => i == count - 1 ? fs : $"{fs}{separator}").ToList();
        }

        public static IEnumerable<FormattableString> SelectFS<T>(this IEnumerable<T> enumerable, Func<T, FormattableString> selector)
        {
            return enumerable.Select(selector);
        }
    }
}