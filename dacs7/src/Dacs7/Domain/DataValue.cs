using Dacs7.Protocols.SiemensPlc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7
{

    public delegate string DataValueFormatter(object value, string formattedValue);


    public class DataValue
    {
        private ReadItem _meta;
        private object _value;


        ItemResponseRetValue ReturnCode { get; }

        public Type Type => _meta.ResultType;
        public Memory<byte> Data { get; }

        public object Value => _value ?? (_value = ReadItem.ConvertMemoryToData(_meta, Data));

        public T GetValue<T>()
        {
            var expected = typeof(T);
            if (expected != _meta.ResultType)
            {
                throw new InvalidOperationException("Generic type is not Equal to Type");
            }
            return (T)Value;
        }

        public string GetValueAsString(DataValueFormatter formatter = null)
        {
            var result = FormattedResult();
            return formatter != null ? formatter(Value, result) : result;
        }

        internal DataValue(ReadItem meta, S7DataItemSpecification data)
        {
            _meta = meta;
            ReturnCode = (ItemResponseRetValue)data.ReturnCode;
            Data = data.Data;
        }


        private string FormattedResult()
        {
            if(Type.IsArray)
            {
                return (Value as IEnumerable<object>).Aggregate((a, b) => $"{a.ToString()} {b.ToString()}").ToString();
            }
            return Value.ToString();
        }

    }
}
