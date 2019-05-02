// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using System;

namespace Dacs7.Alarms
{
    public interface IPlcAlarmAssotiatedValue
    {
        int Length { get; }
        Memory<byte> Data { get; }
    }
}