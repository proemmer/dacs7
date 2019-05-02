// Copyright (c) Benjamin Proemmer. All rights reserved.
// See License in the project root for license information.


namespace Dacs7.Domain
{
    internal enum UserDataSubFunctionCpu : byte
    {
        ReadSsl = 0x01,     //"Read SZL"
        Msgs = 0x02,       //"Message service"  Header constant is also different here
        TransStop = 0x03,  //"Transition to STOP"              //PLC changed state to STOP 
        AlarmInd = 0x12,   //"ALARM indication"                //PLC is indicating a ALARM message 
        AlarmInit = 0x13,  //"ALARM initiate"                  //HMI/SCADA initiating ALARM subscription 
        AlarmAck1 = 0x0b,  //"ALARM ack 1"                    //Alarm was acknowledged in HMI/SCADA 
        AlarmAck2 = 0x0c,  //"ALARM ack 2"                     //Alarm was acknowledged in HMI/SCADA 
    }
}
