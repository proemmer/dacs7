// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

using Dacs7.Domain;
using Dacs7.Protocols.SiemensPlc;
using System;

namespace Dacs7
{
    public class DataValue
    {
        private ReadItem _meta;
        private object _value;


        public ItemResponseRetValue ReturnCode { get; }

        public bool IsSuccessReturnCode => ReturnCode == ItemResponseRetValue.Success;

        public Type Type => _meta.ResultType;
        public Memory<byte> Data { get; }

        public object Value => _value ?? (_value = _meta.ConvertMemoryToData(Data));

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
