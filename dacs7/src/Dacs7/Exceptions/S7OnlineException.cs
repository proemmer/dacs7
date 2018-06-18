using Dacs7.Communication;
using System;

namespace Dacs7.Exceptions
{
    public class S7OnlineException : Exception
    {

        public int ErrorNumber { get; set; }
        public string ErrorText => GetErrTxt(ErrorNumber);

        public S7OnlineException()
        {
            ErrorNumber = Native.SCP_get_errno();
        }



        private string GetErrTxt(int number)
        {
            switch (number)
            {
                case 0:
                    return "Last job executed correctly";

                case 202:
                    return "Lack of resources in driver or in the librar";
                case 203:
                    return "Configuration error";
                case 205:
                    return "Job not currently permitted";
                case 206:
                    return "Parameter error";
                case 207:
                    return "Device already/not yet open.";
                case 208:
                    return "CP not reacting";
                case 209:
                    return "Error in firmware";
                case 210:
                    return "Lack of memory for driver";
                case 215:
                    return "No message";
                case 216:
                    return "Error accessing application buffer";
                case 219:
                    return "Timeout expired";
                case 225:
                    return "Maximum number of logons exceeded";
                case 226:
                    return "Job aborted";
                case 233:
                    return "An auxiliary program could not be started";
                case 234:
                    return "No authorization exists for this function";
                case 304:
                    return "Initialization not yet completed";
                case 305:
                    return "Function not implemented";
                case 4865:
                    return "CP name does not exist";
                case 4866:
                    return "CP name not configured";
                case 4867:
                    return "Channel name does not exist";
                case 4868:
                    return "Channel name not configured";
                default:
                    return "Undefined error";
            }
        }
    }
}
