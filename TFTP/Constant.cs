using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFTP
{
    class Constant
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
        //in millisecond
        public const int timeout = 10000;
        public const int maxTimouts = 10;
    }
}
