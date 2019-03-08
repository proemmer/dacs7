// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

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
