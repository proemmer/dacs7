using Dacs7.Protocols.SiemensPlc;
using System.Collections.Generic;

namespace Dacs7.Protocols
{
    internal class WritePackage
    {
        private readonly int _minimumSize = SiemensPlcProtocolContext.WriteParameterItem + SiemensPlcProtocolContext.WriteDataItem + 1;
        private readonly int _maxSize;
        private List<WriteItem> _items = new List<WriteItem>();


        public bool Handled { get; private set; }

        public bool Full => Free < _minimumSize;

        public int Size { get; private set; } = SiemensPlcProtocolContext.WriteHeader + SiemensPlcProtocolContext.WriteParameter;

        public int Free => _maxSize - Size;

        public IEnumerable<WriteItem> Items => _items;


        public WritePackage(int pduSize)
        {
            // minimum header = 12 read   14 readack
            _maxSize = pduSize;
        }

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
