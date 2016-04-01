namespace InacS7Core.Domain
{

    public enum FunctionCode : byte
    {
        Cpu  =    0x00,   //CPU service
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