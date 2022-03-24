

using Papper.Attributes;

namespace Customer.Data.DB_SpindlePos_BST1
{



    public class DB_SpindlePos_BST1_S58_Pos
    {
        [ReadOnly(true)]
        public bool In_Pos { get; set; }
        public int PosX { get; set; }
        public int PosC { get; set; }
        public int HystereseX { get; set; }
        public int HystereseC { get; set; }
        public ushort Reserve { get; set; }
    }



    public class DB_SpindlePos_BST1_B48_Pos
    {
        [ReadOnly(true)]
        public bool In_Pos { get; set; }
        public int PosX { get; set; }
        public int PosC { get; set; }
        public int HystereseX { get; set; }
        public int HystereseC { get; set; }
        public ushort Reserve { get; set; }
    }



    public class DB_SpindlePos_BST1_N57_Pos
    {
        [ReadOnly(true)]
        public bool In_Pos { get; set; }
        public int PosX { get; set; }
        public int PosC { get; set; }
        public int HystereseX { get; set; }
        public int HystereseC { get; set; }
        public ushort Reserve { get; set; }
    }



    public class DB_SpindlePos_BST1_Pos_LD_Loesen
    {
        [ReadOnly(true)]

        [ArrayBounds(1, 32, 0)]
        public bool[] Verschraubung_IO { get; set; }
    }



    public class DB_SpindlePos_BST1_Pos_LD_Verschrauben
    {
        [ReadOnly(true)]

        [ArrayBounds(1, 32, 0)]
        public bool[] Verschraubung_IO { get; set; }
    }



    public class DB_SpindlePos_BST1_S55_Pos
    {
        [ReadOnly(true)]
        public bool In_Pos { get; set; }
        public int PosX { get; set; }
        public int PosC { get; set; }
        public int HystereseX { get; set; }
        public int HystereseC { get; set; }
        public ushort Reserve { get; set; }
    }


    [Mapping("DB_SpindlePos_BST1", "DB1993", 0)]
    public class DB_SpindlePos_BST1
    {
        [ReadOnly(true)]
        public int ActPosX { get; set; }
        [ReadOnly(true)]
        public int ActPosC { get; set; }
        [ReadOnly(true)]
        public bool SetReferenceX { get; set; }
        [ReadOnly(true)]
        public bool SetReferenceC { get; set; }
        [ReadOnly(true)]
        public short Naechste_Pos { get; set; }
        [ReadOnly(true)]
        public bool Gesamt_IO_LD_Loesen { get; set; }
        [ReadOnly(true)]
        public bool Gesamt_IO_LD_verschraub { get; set; }
        [ReadOnly(true)]
        public bool FP_Verschraubung_IO { get; set; }
        [ReadOnly(true)]
        public bool IMP_Verschraubung_IO { get; set; }

        [ArrayBounds(14, 99, 0)]
        public byte[] Reserve { get; set; }

        [ArrayBounds(1, 32, 0)]
        public DB_SpindlePos_BST1_S58_Pos[] S58_Pos { get; set; }

        [ArrayBounds(740, 799, 0)]
        public byte[] Reserve2 { get; set; }

        [ArrayBounds(1, 32, 0)]
        public DB_SpindlePos_BST1_B48_Pos[] B48_Pos { get; set; }

        [ArrayBounds(1440, 1499, 0)]
        public byte[] Reserve3 { get; set; }

        [ArrayBounds(1, 32, 0)]
        public DB_SpindlePos_BST1_N57_Pos[] N57_Pos { get; set; }

        [ArrayBounds(2140, 2999, 0)]
        public byte[] Reserve4 { get; set; }
        [ReadOnly(true)]
        public DB_SpindlePos_BST1_Pos_LD_Loesen Pos_LD_Loesen { get; set; }
        [ReadOnly(true)]
        public DB_SpindlePos_BST1_Pos_LD_Verschrauben Pos_LD_Verschrauben { get; set; }

        [ArrayBounds(3008, 3499, 0)]
        public byte[] Reserve5 { get; set; }

        [ArrayBounds(1, 32, 0)]
        public DB_SpindlePos_BST1_S55_Pos[] S55_Pos { get; set; }

        [ArrayBounds(4140, 4499, 0)]
        public byte[] Reserve6 { get; set; }

    }

}

