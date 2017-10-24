namespace Dacs7.Domain
{

    internal enum UserDataFunctionType : byte
    {
        Request = 0x04,
        Response = 0x08
    }

    internal enum UserDataParamTypeType : byte
    {
        Request = 0x11,
        Response = 0x12
    }

    internal enum UserDataFunctionGroup : byte
    {
        Prog = 0x1,
        Cyclic = 0x2,
        Block = 0x3,
        Cpu = 0x4,
        Sec = 0x5, /* Security functions e.g. plc password */
        Time = 0x7
    }


    internal enum UserDataSubFunctionProg : byte
    {
        Reqdiagdata1  =  0x01,   // Request diag data (Type 1)   /* Start online block view */
        Vartab1 = 0x02,          // VarTab                        /* Variable table */
        Erase = 0x0c,            // Read diag data              /* online block view */
        Readdiagdata = 0x0e,     // Remove diag data             /* Stop online block view */
        Removediagdata = 0x0f,   // Erase" 
        Force = 0x10,            // Forces
        Reqdiagdata2 = 0x13      // Request diag data (Type 2)     /* Start online block view */
    }

    internal enum UserDataSubFunctionCyclic : byte
    {
        Mem = 0x01,             //"Memory"                        /* read data from memory (DB/M/etc.) */
        Unsubscribe = 0x04,     //"unsubscribe"                   /* unsubcribe (disable) cyclic data */
    }

    internal enum UserDataSubFunctionBlock : byte
    {
        List = 0x01,   //List blocks
        ListType = 0x02,  // List blocks of type
        BlockInfo = 0x03  //Get block info
    }

    internal enum UserDataSubFunctionCpu : byte
    {
        ReadSsl = 0x01,     //"Read SZL"
        Msgs = 0x02,       //"Message service"  Header constant is also different here
        TransStop = 0x03,  //"Transition to STOP"              /* PLC changed state to STOP */
        AlarmInd = 0x11,   //"ALARM indication"                /* PLC is indicating a ALARM message */
        AlarmInit = 0x13,  //"ALARM initiate"                  /* HMI/SCADA initiating ALARM subscription */
        AlarmAck1 = 0x0b,  //"ALARM ack 1"                    /* Alarm was acknowledged in HMI/SCADA */
        AlarmAck2 = 0x0c,  //"ALARM ack 2"                     /* Alarm was acknowledged in HMI/SCADA */
    }

    internal enum UserDataSubFunctionSecurity : byte
    {
        Password = 0x1, //PLC password
    }

    internal enum UserDataSubFunctionTime : byte
    {
        Read = 0x1, //Read clock
        Set = 0x2,  //Set clock
        ReadDf = 0x3,  //Read clock (following)
        Set2 = 0x4, //Set clock
    }
}
