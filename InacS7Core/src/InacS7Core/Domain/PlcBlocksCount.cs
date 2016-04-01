

using InacS7Core.Arch;

namespace InacS7Core.Domain
{
    internal class PlcBlocksCount : IPlcBlocksCount
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
