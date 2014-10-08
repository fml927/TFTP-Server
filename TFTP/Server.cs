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
        private bool _running;
        private IPEndPoint _endPoint;
        private Semaphore _semaphore;

        //need to figure out the port thing.  Do I access connections on all ports?  only port 69?
        public Server(int port)
        {
            _endPoint = new IPEndPoint(IPAddress.Any,port);
            _running = true;
            //selected 25 arbitrarially
            _semaphore = new Semaphore(0, 25);
            loop();
        }
        
        public void loop()
        {
            while(_running)
            {
                UdpClient newClient = new UdpClient(_endPoint);
                //I think this blocks, need to check to make sure
                byte[] bytes = newClient.Receive(ref _endPoint);
                Thread t = new Thread(new ParameterizedThreadStart(handleClient));
                t.Start(new Shared.HandleClientParams { client = newClient, bytes = bytes });
            }
        }
        public void handleClient(object o)
        {
            Shared.HandleClientParams param = (Shared.HandleClientParams)o;
            _semaphore.WaitOne();
            _semaphore.Release();
        }
    
    }
}
