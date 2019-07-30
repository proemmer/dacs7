// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using System;

namespace Dacs7
{
    public class DataValue
    {
        private readonly ReadItem _meta;
        private object _value;


        public ItemResponseRetValue ReturnCode { get; }

        public bool IsSuccessReturnCode => ReturnCode == ItemResponseRetValue.Success;

        public Type Type => _meta.ResultType;
        public Memory<byte> Data { get; }

        /// <summary>
        /// The value as an object.
        /// </summary>
        public object Value => _value ?? (_value = _meta.ConvertMemoryToData(Data));


        /// <summary>
        /// Get the value converted to the generic type.
        /// In this method there is also an validation included
        /// </summary>
        /// <typeparam name="T">expected result type</typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            var expected = typeof(T);
            if (expected != _meta.ResultType)
            {
                ThrowHelper.ThrowTypesNotMatching(expected, _meta.ResultType);
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
