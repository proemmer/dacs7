// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7.Exceptions
{
    public class S7ApiException : Exception
    {
        public S7ApiException(int result, string message) : base(message)
        {
        }
    }
}
