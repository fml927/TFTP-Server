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
    /// <summary>
    /// This class listens to port 69 and creates Request handlers in new threads when client requests are received
    /// </summary>
    class Server
    {
        private IPEndPoint _endPoint;
        public Semaphore _semaphore { get; set; }
        private UdpClient _listener;

        /// <summary>
        /// Creates Server object and begins listening loop
        /// </summary>
        public Server()
        {
            _endPoint = new IPEndPoint(IPAddress.Any, 69);

            //selected 25 arbitrarially
            _semaphore = new Semaphore(32, 32);
            _listener = new UdpClient(_endPoint);
            loop();
        }
 
        /// <summary>
        /// creates Request handlers in new threads when client requests are received
        /// </summary>
        private void loop()
        {
            while(true)
            {
                byte[] bytes = _listener.Receive(ref _endPoint);
                Thread t = new Thread(() => new RequestHandler(_endPoint.Address,_endPoint.Port,bytes,_semaphore));

                //so thread closes with application
                t.IsBackground = true;
                t.Start();
            }
        }
    }
}
