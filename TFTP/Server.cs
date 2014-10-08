using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace TFTP
{
    class Server
    {
        private bool running;
        private UdpClient server;
        private IPEndPoint endPoint;

        //need to figure out the port thing.  Do I access connections on all ports?  only port 69?
        public Server(int port)
        {
            endPoint = new IPEndPoint(IPAddress.Any,port);
            running = true;
            server = new UdpClient(endPoint);
        }
        
        public void loop()
        {
            while(running)
            {

            }
        }
        public void handleClient()
        {

        }
    
    }
}
