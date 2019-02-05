using System;
using System.Collections.Generic;

namespace Dacs7.Helper
{
    internal static class SpecializedLinqExtensions
    {
        public static bool Any<TSource>(this IList<TSource> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Count > 0;
        }

        public static bool Any<TSourceKey, TSourceValue>(this IDictionary<TSourceKey, TSourceValue> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return source.Count > 0;
        }
    }
}
