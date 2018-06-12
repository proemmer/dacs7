using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using System;

namespace Dacs7
{
    public class DataValue
    {
        private ReadItem _meta;
        private object _value;


        ItemResponseRetValue ReturnCode { get; }

        public Type Type => _meta.ResultType;
        public Memory<byte> Data { get; }

        public object Value => _value ?? (_value = _meta.ConvertMemoryToData(Data));

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
            ReturnCode = (ItemResponseRetValue)data.ReturnCode;
            Data = data.Data;
        }

    }
}
