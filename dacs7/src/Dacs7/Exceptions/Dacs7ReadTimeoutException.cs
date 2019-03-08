// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7ReadTimeoutException : Exception
    {
        public Dacs7ReadTimeoutException(ushort id) : base($"Read operation timeout for job {id}")
        {
        }
    }
}