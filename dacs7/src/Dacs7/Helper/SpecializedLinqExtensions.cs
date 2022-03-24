// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;
using System.Collections.Generic;

namespace Dacs7.Helper
{
    internal static class SpecializedLinqExtensions
    {
        public static bool Any<TSource>(this IList<TSource> source)
        {
            if (source == null)
            {
                ThrowArgumenNullException(nameof(source));
            }

            return source.Count > 0;
        }


        public static bool Any<TSourceKey, TSourceValue>(this IDictionary<TSourceKey, TSourceValue> source)
        {
            if (source == null)
            {
                ThrowArgumenNullException(nameof(source));
            }

            return source.Count > 0;
        }


        private static void ThrowArgumenNullException(string source)
        {
            throw new ArgumentNullException(source);
        }
    }
}
