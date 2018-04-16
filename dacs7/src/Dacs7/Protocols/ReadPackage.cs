using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal class ReadPackage
    {
        private int _maxSize;
        private bool _returned;

        private int _sizeRequest = SiemensPlcProtocolContext.ReadHeader + SiemensPlcProtocolContext.ReadParameter;
        private int _sizeResponse = SiemensPlcProtocolContext.ReadAckHeader + SiemensPlcProtocolContext.ReadAckParameter;
        private int _size;

        private List<ReadItemSpecification> _items = new List<ReadItemSpecification>();


        public bool Handled => _returned;

        public bool Full => Free < SiemensPlcProtocolContext.ReadItemSize;

        public int Size => _size;

        public int Free => _maxSize - Size;

        public IEnumerable<ReadItemSpecification> Items => _items;


        public ReadPackage(int pduSize)
        {
            // minimum header = 12 read   14 readack
            _maxSize = pduSize;
        }

        public ReadPackage Return()
        {
            _returned = true;
            return this;
        }

        public bool TryAdd(ReadItemSpecification item)
        {
            var size = item.Length;
            if (Free >= size)
            {
                _items.Add(item);
                _sizeRequest += SiemensPlcProtocolContext.ReadItemSize;
                _sizeResponse += size + SiemensPlcProtocolContext.ReadItemAckHeader;
                _size = Math.Max(_sizeRequest, _sizeResponse);
                return true;
            }
            return false;
        }
    }

}
