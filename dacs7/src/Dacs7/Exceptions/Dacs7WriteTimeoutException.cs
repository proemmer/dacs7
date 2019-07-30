// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7
{
    public class Dacs7WriteTimeoutException : Exception
    {
        public Dacs7WriteTimeoutException(ushort id) : base($"Write operation timeout for job {id}")
        {
        }

        public Dacs7WriteTimeoutException()
        {
        }

        public Dacs7WriteTimeoutException(string message) : base(message)
        {
        }

        public Dacs7WriteTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}