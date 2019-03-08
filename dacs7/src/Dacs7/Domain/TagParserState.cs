// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

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
