// Copyright (c) Benjamin Proemmer. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License in the project root for license information.

namespace Dacs7.Domain
{

    internal enum FunctionCode : byte
    {
        Cpu = 0x00,   //CPU service
        SetupComm = 0xF0, //Setup communication
        ReadVar = 0x04,   //Read Var
        WriteVar = 0x05,  //Write var

        RequestDownload = 0x1A, //Request download
        DownloadBlock = 0x1B,   //Download block
        DownloadEnded = 0x1C,   //Download ended
        StartUpload = 0x1D,     //Start upload
        Upload = 0x1E,          //Upload
        EndUpload = 0x1F,       //End upload
        PlcControl = 0x28,      //PLC Control
        PlcStop = 0x29          //PLC Stop

    }
}