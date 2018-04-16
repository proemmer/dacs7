using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal class WritePackage
    {
        private int _minimumSize = SiemensPlcProtocolContext.WriteParameterItem + SiemensPlcProtocolContext.WriteDataItem + 1;
        private int _maxSize;
        private bool _returned;

        private int _size = SiemensPlcProtocolContext.WriteHeader + SiemensPlcProtocolContext.WriteParameter;

        private List<WriteItem> _items = new List<WriteItem>();


        public bool Handled => _returned;

        public bool Full => Free < _minimumSize;

        public int Size => _size;

        public int Free => _maxSize - Size;

        public IEnumerable<WriteItem> Items => _items;


        public WritePackage(int pduSize)
        {
            // minimum header = 12 read   14 readack
            _maxSize = pduSize;
        }

        public WritePackage Return()
        {
            _returned = true;
            return this;
        }

        public bool TryAdd(WriteItem item)
        {
            var size = item.Length;
            if (Free >= size)
            {
                _items.Add(item);
                _size += SiemensPlcProtocolContext.WriteParameterItem + SiemensPlcProtocolContext.WriteDataItem + size;
                if (_size % 2 != 0) _size++;
                return true;
            }
            return false;
        }
    }

}
