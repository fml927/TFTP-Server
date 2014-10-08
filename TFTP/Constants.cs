using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTP
{
    class Constants
    {
        public enum OpCode
        {
            Read = 1,
            Write = 2,
            Data = 3,
            Acknowledge = 4,
            Error = 5
        }
        public enum ErrorCode
        {
            NotDefined = 0,
            FileNotFound = 1,
            AccessViolation = 2,
            DiskFull = 3,
            IllegalOpeation = 4,
            UnknownTransferID = 5,
            FileAlreadyExists = 6,
            NoSuchUser = 7,
        }
    }
}
