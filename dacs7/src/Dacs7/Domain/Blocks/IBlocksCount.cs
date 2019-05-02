// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Metadata
{
    public interface IPlcBlocksCount
    {
        int Ob { get; }
        int Fb { get; }
        int Fc { get; }
        int Sfb { get; }
        int Sfc { get; }
        int Db { get; }
        int Sdb { get; }
    }
}
