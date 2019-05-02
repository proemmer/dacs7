// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.

namespace Dacs7.Domain
{
    public enum TagParserState
    {
        Nothing,
        Area,
        Offset,
        Type,
        NumberOfItems,
        TypeValidation,
        Success
    }
}
