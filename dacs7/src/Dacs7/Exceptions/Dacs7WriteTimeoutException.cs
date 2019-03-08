﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7
{
    [Serializable]
    public class Dacs7WriteTimeoutException : Exception
    {
        public Dacs7WriteTimeoutException(ushort id) : base($"Write operation timeout for job {id}")
        {
        }
    }
}