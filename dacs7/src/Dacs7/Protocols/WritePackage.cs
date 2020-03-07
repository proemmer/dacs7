// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Protocols.SiemensPlc;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal sealed class WritePackage
    {
        private const int _writeItemHeaderSize = SiemensPlcProtocolContext.WriteParameterItem + SiemensPlcProtocolContext.WriteDataItem;
        private const int _minimumSize = _writeItemHeaderSize + 1;
        private readonly int _maxSize;
        private readonly List<WriteItem> _items = new List<WriteItem>();



        public bool Handled { get; private set; }

        public bool Full => Free < _minimumSize;

        public int Size { get; private set; } = SiemensPlcProtocolContext.WriteHeader + SiemensPlcProtocolContext.WriteParameter;

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
            var itemSize = _writeItemHeaderSize + size;

            if (Free >= itemSize)
            {
                _items.Add(item);
                Size += itemSize;
                if (Size % 2 != 0)
                {
                    Size++; // set the next item to a even address
                }

                return true;
            }
            return false;
        }
    }

}
