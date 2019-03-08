// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols
{
    internal class CallbackHandler<T>
    {
        public ushort Id { get; }
        public Exception Exception { get; set; }
        public AsyncAutoResetEvent<T> Event { get; }

        public CallbackHandler()
        {
        }

        public CallbackHandler(ushort id)
        {
            Id = id;
            Event = new AsyncAutoResetEvent<T>();
            Exception = null;
        }

    }
}
