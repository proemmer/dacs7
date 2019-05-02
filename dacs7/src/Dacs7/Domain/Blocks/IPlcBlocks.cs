// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Metadata
{

    public interface IPlcBlocks
    {
        int Number { get; }
        byte Flags { get; }
        string Language { get; }
    }
}
