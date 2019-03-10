// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.


namespace Dacs7.Metadata
{
    internal sealed class PlcBlocksCount : IPlcBlocksCount
    {
        public int Ob { get; set; }
        public int Fb { get; set; }
        public int Fc { get; set; }
        public int Sfb { get; set; }
        public int Sfc { get; set; }
        public int Db { get; set; }
        public int Sdb { get; set; }
    }

}
