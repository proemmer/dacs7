using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dacs7
{
    public class DataValue
    {
        private ReadItemSpecification _meta;
        private byte _returnCode;
        private Memory<byte> _data;
        private object _value;


        byte ReturnCode => _returnCode;
        public Type Type => _meta.ResultType;
        public Memory<byte> Data => _data;

        public object Value => _value ?? (_value = ReadItemSpecification.ConvertMemoryToData(_meta, _data));

        public T GetValue<T>()
        {
            var expected = typeof(T);
            if (expected != _meta.ResultType)
            {
                throw new InvalidOperationException("Generic type is not Equal to Type");
            }
            return (T)Value;
        }

        internal DataValue(ReadItemSpecification meta, S7DataItemSpecification data)
        {
            _meta = meta;
            _returnCode = data.ReturnCode;
            _data = data.Data;
        }

    }
}
