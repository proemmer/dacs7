// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System.Collections.Generic;

namespace Dacs7
{
    internal static class InternalExtensions
    {

        /// <summary>
        /// Removes an element from the middle of a queue without disrupting the other elements.
        /// </summary>
        /// <typeparam name="T">The element to remove.</typeparam>
        /// <param name="queue">The queue to modify.</param>
        /// <param name="valueToRemove">The value to remove.</param>
        /// <remarks>
        /// If a value appears multiple times in the queue, only its first entry is removed.
        /// </remarks>
        internal static bool RemoveMidQueue<TQueue>(this Queue<TQueue> queue, TQueue valueToRemove) where TQueue : class
        {
            int originalCount = queue.Count;
            int dequeueCounter = 0;
            bool found = false;
            while (dequeueCounter < originalCount)
            {
                dequeueCounter++;
                TQueue dequeued = queue.Dequeue();
                if (!found && dequeued == valueToRemove)
                {
                    // only find 1 match
                    found = true;
                }
                else
                {
                    queue.Enqueue(dequeued);
                }
            }

            return found;
        }
    }
}
