// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7
{
    public class Dacs7TypeNotSupportedException : Exception
    {
        public Type NotSupportedType { get; private set; }

        public Dacs7TypeNotSupportedException(Type notSupportedType) : base($"The Type {notSupportedType.Name} is not supported for read or write operations!") => NotSupportedType = notSupportedType;
    }
}
