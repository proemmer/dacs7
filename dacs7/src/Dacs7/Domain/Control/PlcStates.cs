// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Control
{
    public enum PlcStates
    {
        Unknown = 0x00,
        Stop = 0x04,
        Run = 0x08
    }


    public class PlcStateInfo
    {
        /// <summary>
        /// Current state of the plc
        /// </summary>
        public PlcStates State { get; internal set; }

        /// <summary>
        /// Previous State of the plc
        /// </summary>
        public PlcStates PreviousState { get; internal set; }

        /// <summary>
        /// timestamp is only valid if PreviousState is not unknown
        /// </summary>
        public DateTime Timestamp { get; internal set; }
    }
}
