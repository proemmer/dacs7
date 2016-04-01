using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace InacS7Core.Helper
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CollectionExtensions
    {
        //public static TResult[] ConvertAll<T, TResult>(this T[] items, Converter<T, TResult> transformation)
        //{
        //    return Array.ConvertAll<T, TResult>(items, transformation);
        //}
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            if (items == null)
            {
                return;
            }
            foreach (T current in items)
            {
                action(current);
            }
        }
        public static T Find<T>(this T[] items, Predicate<T> predicate)
        {
            return Array.Find<T>(items, predicate);
        }
        public static T[] FindAll<T>(this T[] items, Predicate<T> predicate)
        {
            return Array.FindAll<T>(items, predicate);
        }
        /// <summary>
        ///   Checks whether or not collection is null or empty. Assumes collection can be safely enumerated multiple times.
        /// </summary>
        /// <param name="this"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this IEnumerable @this)
        {
            return @this == null || !@this.GetEnumerator().MoveNext();
        }
    }
}
