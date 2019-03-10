// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Protocols.Rfc1006;
using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal sealed class ReadPackage
    {
        private readonly int _maxSize;
        private int _sizeRequest = Rfc1006ProtocolContext.DataHeaderSize + SiemensPlcProtocolContext.ReadHeader + SiemensPlcProtocolContext.ReadParameter;
        private int _sizeResponse = Rfc1006ProtocolContext.DataHeaderSize + SiemensPlcProtocolContext.ReadAckHeader + SiemensPlcProtocolContext.ReadAckParameter;
        private readonly List<ReadItem> _items = new List<ReadItem>();


        public bool Handled { get; private set; }

        public bool Full => Free < SiemensPlcProtocolContext.ReadItemSize;

        public int Size { get; private set; }

        public int Free => _maxSize - Size;

        public IEnumerable<ReadItem> Items => _items;

        public ReadPackage(int pduSize) => _maxSize = pduSize; // minimum header = 12 read   14 readack


        public ReadPackage Return()
        {
            Handled = true;
            return this;
        }

        public bool TryAdd(ReadItem item)
        {
            var size = item.NumberOfItems;
            if (Free >= size)
            {
                _items.Add(item);
                _sizeRequest += SiemensPlcProtocolContext.ReadItemSize;
                _sizeResponse += size + SiemensPlcProtocolContext.ReadItemAckHeader;
                Size = Math.Max(_sizeRequest, _sizeResponse);
                return true;
            }
            return false;
        }
    }

}
