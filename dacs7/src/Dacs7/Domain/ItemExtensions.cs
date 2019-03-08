// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7
{
    public static class ItemExtensions
    {

        public static WriteItem From(this ReadItem ri, Memory<byte> data)
        {
            var result = ri.Clone();
            result.Data = data;
            return result;
        }
    }
}
