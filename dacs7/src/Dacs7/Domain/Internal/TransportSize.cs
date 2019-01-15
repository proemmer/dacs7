namespace Dacs7.Domain
{
    internal enum ItemDataTransportSize
    {
        /* types of 1 byte length */
        Bit = 1,
        Byte = 2,
        Char = 3,
        /* types of 2 bytes length */
        Word = 4,
        Int = 5,
        /* types of 4 bytes length */
        Dword = 6,
        Dint = 7,
        Real = 8,
        /* Special types */
        Date = 9,
        Tod = 10,
        Time = 11,
        S5Time = 12,
        Dt = 15,
        /* Timer or counter */
        Counter = 28,
        Timer = 29,
        IecCounter = 30,
        IecTimer = 31,
        HsCounter = 32
    }

    public enum DataTransportSize
    {
        Null = 0,
        Bit = 3, /* bit access, length is in bits */
        Byte = 4, /* byte/word/dword access, length is in bits */
        Int = 5, /* integer access, length is in bits */
        Dint = 6, /* integer access, length is in bytes */
        Real = 7, /* real access, length is in bytes */
        OctetString = 9 /* octet string, length is in bytes */
    }
    
}
