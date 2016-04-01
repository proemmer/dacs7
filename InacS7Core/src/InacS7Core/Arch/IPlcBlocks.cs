using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InacS7Core.Arch
{

    public interface IPlcBlocks
    {
        int Number { get; }
        byte Flags { get; }
        string Language { get; }
    }
}
