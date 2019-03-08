// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Alarms
{
    public interface IPlcAlarmAssotiatedValue
    {
        int Length { get; }
        Memory<byte> Data { get;  }
    }
}