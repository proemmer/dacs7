using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7.Domain
{
    public abstract class OperationParameter
    {
        public PlcArea Area { get; set; }
        public int Offset { get; set; }
        public Type Type { get; set; }
        public int[] Args { get; set; }
        public int Length
        {
            get
            {
                return Args != null && Args.Any() ? Args.First() : 0;
            }
        }
    }


    public class ReadOperationParameter : OperationParameter
    {
    }

    public class WriteOperationParameter : OperationParameter
    {
        public object Data { get; set; }
    }
}
