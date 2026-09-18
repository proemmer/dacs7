// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal sealed class ReadPackage
    {
        private readonly int _maxSize;
        private int _sizeRequest = SiemensPlcProtocolContext.ReadHeader + SiemensPlcProtocolContext.ReadParameter;
        private int _sizeResponse = SiemensPlcProtocolContext.ReadAckHeader + SiemensPlcProtocolContext.ReadAckParameter;
        private int _byteSizeResponse = SiemensPlcProtocolContext.ReadAckHeader + SiemensPlcProtocolContext.ReadAckParameter;
        private bool _fillByteRequired;
        private readonly List<ReadItem> _items = new();


        public bool Handled { get; private set; }

        public bool Full => Free < SiemensPlcProtocolContext.ReadItemSize;

        public int Size { get; private set; }

        public int Free => _maxSize - Size;

        public IEnumerable<ReadItem> Items => _items;

        public ReadPackage(int pduSize)
        {
            _maxSize = pduSize; // minimum header = 12 read   14 readack
        }

        public ReadPackage Return()
        {
            Handled = true;
            return this;
        }

        public bool TryAdd(ReadItem item)
        {
            // This rule counts the number of items instead of bytes. It is kept, so every package which fits into the pdu is built as before.
            ushort size = item.NumberOfItems;
            int newReqSize = _sizeRequest + SiemensPlcProtocolContext.ReadItemSize;
            int newRespSize = _sizeResponse + size + SiemensPlcProtocolContext.ReadItemAckHeader;
            int readItemSize = Math.Max(newReqSize, newRespSize);

            // The response in bytes has to fit into the pdu, items with an odd length are followed by a fill byte.
            int newByteSizeResponse = _byteSizeResponse + (_fillByteRequired ? 1 : 0) + SiemensPlcProtocolContext.ReadItemAckHeader + item.ByteLength;

            if (Free >= readItemSize && newByteSizeResponse <= _maxSize)
            {
                _items.Add(item);
                _sizeRequest = newReqSize;
                _sizeResponse = newRespSize;
                _byteSizeResponse = newByteSizeResponse;
                _fillByteRequired = item.ByteLength % 2 != 0;
                Size = readItemSize;
                return true;
            }
            return false;
        }
    }

}
