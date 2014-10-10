using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace TFTP
{
    class Server
    {
        private bool _running;
        private IPEndPoint _endPoint;
        public Semaphore semaphore { get; set; }
        private UdpClient _listener;

        //need to figure out the port thing.  Do I access connections on all ports?  only port 69?
        public Server()
        {
            _endPoint = new IPEndPoint(IPAddress.Any, 69);
            _running = true;
            //selected 25 arbitrarially
            semaphore = new Semaphore(25, 25);
            _listener = new UdpClient(_endPoint);
            loop();
        }

        private void loop()
        {
            while(_running)
            {
                //I think this blocks, need to check to make sure
                //TODO reimplement semaphore
                byte[] bytes = _listener.Receive(ref _endPoint);
                Thread t = new Thread(() => new RequestHandler(_endPoint.Address,_endPoint.Port,bytes));
                t.IsBackground = true;
                t.Start();
            }
        }
    }
}
