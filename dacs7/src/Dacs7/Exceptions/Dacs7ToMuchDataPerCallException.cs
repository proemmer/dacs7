// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7
{
    public class Dacs7ToMuchDataPerCallException : Exception
    {
        public Dacs7ToMuchDataPerCallException(int expected, int actual) :
            base($"There is too much data ({actual} bytes) for a single job, please split jobs to a maximum of {expected} bytes per call!")
        {
        }

        public Dacs7ToMuchDataPerCallException()
        {
        }

        public Dacs7ToMuchDataPerCallException(string message) : base(message)
        {
        }

        public Dacs7ToMuchDataPerCallException(string message, Exception innerException) : base(message, innerException)
        {
        }

    }
}
