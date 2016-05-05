namespace Dacs7.Domain
{
    public enum PlcArea : byte
    {
        SI = 0x03,   //System info of 200 family 
        SF = 0x05,   //System flags of 200 family
        AI = 0x06,   //analog inputs of 200 family
        AQ = 0x07,   //analog outputs of 200 family
        DP = 0x80,   //direct peripheral access
        IB = 0x81,   //InputByte
        QB = 0x82,   //OutputByte
        FB = 0x83,   //FlagByte
        DB = 0x84,   //Data Block
        DI = 0x85,   //instance data blocks
        LO = 0x86,   //local data (should not be accessible over network)
        PR = 0x87,   //previous local data (should not be accessible over network)
        CT = 0x1C,   //S7 counters
        TM = 0x1D,   //S7 timers
        CI = 0x1E,   //IEC counters (200 family)
        TI = 0x1F    //IEC timers (200 family)
    };
}
