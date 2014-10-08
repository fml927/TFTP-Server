using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;

namespace TFTP
{
    class Shared
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
            UnknownID = 5,
            FileAlreadyExists = 6,
            NoSuchUser = 7,
        }
        public abstract class Packet
        {
            protected byte[] BlockNumber;
            protected byte[] ModeErrorMessage;

            public abstract byte[] Raw();
        }
        public class ReadPacket : Packet
        {
            public string Filename { get; private set; }
            public string Mode { get; private set; }
            public ReadPacket(byte[] raw)
            {
                
            }
            public override byte[] Raw()
            {
                return new byte[] { 0 };
            }
        }
        public class HandleClientParams
        {
            public UdpClient client { get; set; }
            public byte[] bytes { get; set; }
        }
    }
}
