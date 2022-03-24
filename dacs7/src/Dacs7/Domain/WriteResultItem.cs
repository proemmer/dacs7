// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

namespace Dacs7
{

    public class WriteResultItem : WriteRequestItem
    {
        public ItemResponseRetValue ReturnCode { get; private set; }

        public WriteResultItem(WriteRequestItem req, ItemResponseRetValue returnCode) : base(req.Area, req.DbNumber, req.NumberOfItems, req.Offset, req.TransportSize, req.ElementSize, req.Address, req.Data)
        {
            ReturnCode = returnCode;
        }
    }
}
