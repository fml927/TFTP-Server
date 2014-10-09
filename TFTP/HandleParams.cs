using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace TFTP
{
    class HandleParams
    {
        public byte[] Bytes { get; set; }
        public int Port { get; set; }
        public IPAddress Address {get; set; }
    }
}
