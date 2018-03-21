// Copyright (c) insite-gmbh. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using System;

namespace Dacs7.Protocols.SiemensPlc
{
    public class ReadItemSpecification
    {
        public PlcArea Area { get; set; }
        public ushort DbNumber { get; set; }
        public ushort Offset { get; set; }
        public ushort Length { get; set; }
        public Type VarType { get; set; }


        public virtual WriteItemSpecification Clone()
        {
            return new WriteItemSpecification
            {
                Area = Area,
                DbNumber = DbNumber,
                Offset = Offset,
                Length = Length,
                VarType = VarType,
            };
        }
    }
}
