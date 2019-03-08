// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal class WritePackage
    {
        private readonly int _minimumSize = SiemensPlcProtocolContext.WriteParameterItem + SiemensPlcProtocolContext.WriteDataItem + 1;
        private readonly int _maxSize;
        private readonly List<WriteItem> _items = new List<WriteItem>();


        public bool Handled { get; private set; }

        public bool Full => Free < _minimumSize;

        public int Size { get; private set; } = Rfc1006ProtocolContext.DataHeaderSize + SiemensPlcProtocolContext.WriteHeader + SiemensPlcProtocolContext.WriteParameter;

        public int Free => _maxSize - Size;

        public IEnumerable<WriteItem> Items => _items;


        public WritePackage(int pduSize) => _maxSize = pduSize; // minimum header = 12 read   14 readack

        public WritePackage Return()
        {
            Handled = true;
            return this;
        }

        public bool TryAdd(WriteItem item)
        {
            var size = item.NumberOfItems;
            if (Free >= size)
            {
                _items.Add(item);
                Size += SiemensPlcProtocolContext.WriteParameterItem + SiemensPlcProtocolContext.WriteDataItem + size;
                if (Size % 2 != 0) Size++;
                return true;
            }
            return false;
        }
    }

}
