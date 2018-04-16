using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7
{
    public class DataValue
    {
        private ReadItem _meta;
        private byte _returnCode;
        private Memory<byte> _data;
        private object _value;


        byte ReturnCode => _returnCode;
        public Type Type => _meta.ResultType;
        public Memory<byte> Data => _data;

        public object Value => _value ?? (_value = ReadItem.ConvertMemoryToData(_meta, _data));

        public T GetValue<T>()
        {
            var expected = typeof(T);
            if (expected != _meta.ResultType)
            {
                throw new InvalidOperationException("Generic type is not Equal to Type");
            }
            return (T)Value;
        }

        internal DataValue(ReadItem meta, S7DataItemSpecification data)
        {
            _meta = meta;
            _returnCode = data.ReturnCode;
            _data = data.Data;
        }

    }
}
