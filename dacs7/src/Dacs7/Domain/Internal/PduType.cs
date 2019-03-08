﻿// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Domain
{
    internal enum PduType : byte
    {
        Job = 0x01,
        Ack = 0x02,
        AckData = 0x03,
        UserData = 0x07
    }
}
