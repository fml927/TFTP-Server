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
        
        private void loop()
        {
            while(_running)
            {
                UdpClient newClient = new UdpClient(_endPoint);
                //I think this blocks, need to check to make sure
                byte[] bytes = newClient.Receive(ref _endPoint);
                Thread t = new Thread(new ParameterizedThreadStart(handleClient));
                t.Start(new HandleParams { client = newClient, bytes = bytes });
            }
        }
        private void handleClient(object o)
        {
            HandleParams param = (HandleParams)o;
            _semaphore.WaitOne();
            //check to see if read/write request is valid
            if (true)
            {
                short block = 0;
                //while transmitting or receiving
                while (true)
                {

                }
            }
            //transmit error unknown transfer ID
            else transmitError(param.client, Constants.ErrorCode.UnknownTransferID);
            param.client.Close();
            _semaphore.Release();
        }
        private void transmitError(UdpClient client, Constants.ErrorCode error)
        {
            string errorString = "";
            byte[] errorBytes;
            byte[] toSend;
            switch(error)
            {
                case Constants.ErrorCode.AccessViolation:
                    errorString = "Access violation.";
                    break;
                case Constants.ErrorCode.DiskFull:
                    errorString = "Disk full or allocation exceeded.";
                    break;
                case Constants.ErrorCode.FileAlreadyExists:
                    errorString = "File already exists.";
                    break;
                case Constants.ErrorCode.FileNotFound:
                    errorString = "File not found.";
                    break;
                case Constants.ErrorCode.IllegalOpeation:
                    errorString = "Illegal TFTP operation.";
                    break;
                case Constants.ErrorCode.NoSuchUser:
                    errorString = "No such user.";
                    break;
                case Constants.ErrorCode.NotDefined:
                    errorString = "Not defined";
                    break;
                case Constants.ErrorCode.UnknownTransferID:
                    errorString =  "Unknown transfer ID.";
                    break;
            }
            errorBytes = Helpers.GetBytes(errorString);

            toSend = new byte[errorBytes.Length+5];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Error;
            toSend[2] = 0;
            toSend[3] = (byte)error;
            Array.Copy(errorBytes, 0, toSend, 4, errorBytes.Length);
            toSend[toSend.Length - 1] = 0;

            client.Send(toSend,toSend.Length);
        }

        private void transmitAwk(UdpClient client, int block)
        {
            byte[] toSend = new byte[4];
            toSend[0] = 0;
            toSend[1] = (byte)Constants.OpCode.Acknowledge;
            Array.Copy(BitConverter.GetBytes(block),0,toSend,2,2);
            client.Send(toSend,4);
        }
    
    }
}
