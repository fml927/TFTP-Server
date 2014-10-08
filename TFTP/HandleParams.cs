using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace TFTP
{
    class HandleParams
    {
        public UdpClient client { get; set; }
        public byte[] bytes { get; set; }
    }
}
